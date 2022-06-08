using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FFT.CRC;
using NAudio.Wave;
using SharpSampleRate;
using SharpSBC;

namespace DS4AudioStreamer.Sound
{
    public class CaptureWorker: IDisposable
    {
        private const byte Frames = 2;
        // private const byte Frames = 4;
        private const byte Protocol = 0x15;
        // private const byte Protocol = 0x19;
        private const int BtOutputReportLength = 334;
        // private const int BtOutputReportLength = 548;
        
        private const byte ModeType = 0xC0 | 0x04;
        private const byte TransactionType = 0xA2;
        private const byte FeaturesSwitch = 0xF3;
        private const byte FlashOn = 0x00;
        private const byte FlashOff = 0x00;
        private const byte VolLeft = 0x48;
        private const byte VolRight = 0x48;
        private const byte VolMic = 0x00;
        private const byte VolSpeaker = 0x90; // Volume Built-in Speaker / 0x4D == Uppercase M (Mute?)

        private byte _powerRumbleWeak = 0x00;
        private byte _powerRumbleStrong = 0x00;
        private byte _lightbarRed = 0xFF;
        private byte _lightbarGreen = 0x00;
        private byte _lightbarBlue = 0xFF;

        private readonly byte[] _outputBtCrc32Head = { 0xA2 };
        private readonly Socket _socket;
        private readonly object _syncRoot;
        private readonly byte _id;
        private readonly CircularBuffer<byte> _buffer;
        private readonly SbcEncoder _encoder;
        private MyLoopbackCapture _captureDevice; 
        private bool _capturing;

        public CaptureWorker(Socket socket, object syncRoot, byte id)
        {
            _captureDevice = new MyLoopbackCapture(20);
            _captureDevice.DataAvailable += LoopbackCaptureOnDataAvailable;
            
            _socket = socket;
            _syncRoot = syncRoot;
            _id = id;
            _encoder = new SbcEncoder(
                _captureDevice.WaveFormat.SampleRate,
                8,
                51,
                SbcEncoder.ChannelMode.JointStereo,
                true,
                16
            );
            _buffer = new CircularBuffer<byte>(80000);
        }

        public void Start()
        {
            _capturing = true;
            _captureDevice.StartRecording();
        }

        private void LoopbackCaptureOnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (!_capturing || e.BytesRecorded <= 0) return;
            
            try
            {
                lock (_buffer)
                {
                    _buffer.CopyFrom(e.Buffer, e.BytesRecorded);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public unsafe void Playback()
        {
            var minData = (int)_encoder.CodeSize * Frames * 2; // 2x because they are 32 bit samples
            var audioData = new byte[minData * 2];
            var s16AudioData = new byte[minData * 2];
            
            const int bufferSize = BtOutputReportLength + 1;
            var outputBuffer = new byte[bufferSize];
            ushort lilEndianCounter = 0;
            
            const int indexBuffer = 81;

            // var sendTarget = new IPEndPointStruct(new IPHolder(IPAddress.Parse("192.168.7.2")), 27000);

            while (_capturing)
            {
                Thread.Yield();
                
                lock (_buffer)
                {
                    if (_buffer.CurrentLength < minData)
                        continue;
                    
                    _buffer.CopyTo(audioData, minData);                    
                }
                
                fixed (byte* s16AudioDataPtr = s16AudioData)
                {
                    fixed (byte* audioDataPtr = audioData)
                    {
                        SampleRate.src_float_to_short_array((float*) audioDataPtr, (short*) s16AudioDataPtr,
                            minData / 2);
                    }
                    
                    fixed (byte* outputBufferPtr = &outputBuffer[indexBuffer])
                    {
                        var source = s16AudioDataPtr;
                        var dest = outputBufferPtr;
                
                        for (int i = 0; i < Frames; i++)
                        {
                            _encoder.Encode(source , dest, (ulong)minData / 2, out var encoded);
                            source += _encoder.CodeSize;
                            dest += encoded;
                        }
                    }
                }
                
                outputBuffer[0] = Protocol;
                outputBuffer[1] = ModeType;
                outputBuffer[2] = TransactionType;
                outputBuffer[3] = FeaturesSwitch;
                outputBuffer[4] = 0x04; // Unknown
                outputBuffer[5] = 0x00;
                outputBuffer[6] = _powerRumbleWeak;
                outputBuffer[7] = _powerRumbleStrong;
                outputBuffer[8] = _lightbarRed;
                outputBuffer[9] = _lightbarGreen;
                outputBuffer[10] = _lightbarBlue;
                outputBuffer[11] = FlashOn;
                outputBuffer[12] = FlashOff;
                outputBuffer[13] = 0x00; outputBuffer[14] = 0x00; outputBuffer[15] = 0x00; outputBuffer[16] = 0x00; /* Start Empty Frames */
                outputBuffer[17] = 0x00; outputBuffer[18] = 0x00; outputBuffer[19] = 0x00; outputBuffer[20] = 0x00; /* Start Empty Frames */
                outputBuffer[21] = VolLeft;
                outputBuffer[22] = VolRight;
                outputBuffer[23] = VolMic;
                outputBuffer[24] = VolSpeaker;
                outputBuffer[25] = 0x85;
                
                outputBuffer[78] = (byte)(lilEndianCounter & 0xFF);
                outputBuffer[79] = (byte)((lilEndianCounter >> 8) & 0xFF);
                
                //outputBuffer[80] = 0x02; // 0x02 Speaker Mode On / 0x24 Headset Mode On
                outputBuffer[80] = 0x24; // 0x02 Speaker Mode On / 0x24 Headset Mode On
                
                // Generate CRC-32 data for output buffer and add it to output report
                
                var crc = CRC32Calculator.SEED;
                byte btHeader = 0xa2;
                CRC32Calculator.Add(ref crc, new ReadOnlySpan<byte>(&btHeader, 1));
                CRC32Calculator.Add(ref crc, new ReadOnlySpan<byte>(outputBuffer, 0, outputBuffer.Length - 5));
                crc = CRC32Calculator.Finalize(crc);
                
                outputBuffer[^5] = (byte)crc;
                outputBuffer[^4] = (byte)(crc >> 8);
                outputBuffer[^3] = (byte)(crc >> 16);
                outputBuffer[^2] = (byte)(crc >> 24);
                outputBuffer[^1] = _id;
                
                lilEndianCounter += Frames;
                
                // if (prevAllocations < GC.CollectionCount())
                // {
                //     prevAllocations = GC.GetAllocatedBytesForCurrentThread();
                //     Console.WriteLine(prevAllocations);                    
                // }
                
                lock (_syncRoot)
                {
                    // SocketHandler.SendTo(_socket, outputBuffer, 0, outputBuffer.Length, 0, ref sendTarget);
                }
            }
        }

        public void StopPlayback()
        {
            _capturing = false;
        }

        public void Dispose()
        {
            _encoder.Dispose();
            _captureDevice.Dispose();
        }

        // public void SubmitFeedback(DualShock4FeedbackReceivedEventArgs args)
        // {
        //     _powerRumbleStrong = args.SmallMotor;
        //     _powerRumbleWeak = args.LargeMotor;
        //     _lightbarBlue = args.LightbarColor.Blue;
        //     _lightbarGreen = args.LightbarColor.Green;
        //     _lightbarRed = args.LightbarColor.Red;
        // }
    }
}