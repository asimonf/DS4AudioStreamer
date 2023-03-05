using DS4Windows;
using FFT.CRC;

namespace DS4AudioStreamer.Sound
{
    public class NewCaptureWorker
    {
        private readonly HidDevice _hidDevice;

        private readonly byte[] _outputBuffer = new byte[640];

        private readonly SbcAudioStream _stream;
        
        private readonly Thread _workerThread;

        public NewCaptureWorker(
            HidDevice hidDevice
        ) {
            _hidDevice = hidDevice;
            
            hidDevice.OpenFileStream(_outputBuffer.Length);
            
            NativeMethods.HidD_SetNumInputBuffers(hidDevice.SafeReadHandle.DangerousGetHandle(), 2);

            _stream = new SbcAudioStream();
            _workerThread = new Thread(_worker);
        }
        
        private unsafe void _worker()
        {
            byte btHeader = 0xa2;
            var btHeaderSpan = new ReadOnlySpan<byte>(&btHeader, 1);
            ushort lilEndianCounter = 0;

            while (true)
            {
                var data = _stream.SbcAudioData;
                var frameSize = _stream.FrameSize;

                while (_stream.CurrentFrameCount >= 2)
                {
                    int framesAvailable, size, protocol;
                    
                    if (_stream.CurrentFrameCount >= 4)
                    {
                        framesAvailable = 4;
                        protocol = 0x17;
                        size = 462;
                    }
                    else
                    {
                        framesAvailable = 2;
                        protocol = 0x14;
                        size = 270;
                    }
                
                    Array.Fill<byte>(_outputBuffer, 0);
            
                    _outputBuffer[0] = (byte) protocol;
                    _outputBuffer[1] = 0x40;  // Unknown
                    _outputBuffer[2] = 0xa2;  // Unknown
            
                    _outputBuffer[3] = (byte) (lilEndianCounter & 0xFF);
                    _outputBuffer[4] = (byte) ((lilEndianCounter >> 8) & 0xFF);
            
                    // _outputBuffer[5] = 0x02; // 0x02 Speaker Mode On / 0x24 Headset Mode On
                    _outputBuffer[5] = 0x24; // 0x02 Speaker Mode On / 0x24 Headset Mode On
                
                    lilEndianCounter += (ushort) framesAvailable;

                    data.CopyTo(_outputBuffer, 6, framesAvailable * frameSize);                    
            
                    var crc = CRC32Calculator.SEED;
                    CRC32Calculator.Add(ref crc, btHeaderSpan);
                    CRC32Calculator.Add(ref crc, new ReadOnlySpan<byte>(_outputBuffer, 0, size - 4));
                    crc = CRC32Calculator.Finalize(crc);
            
                    _outputBuffer[size - 4] = (byte) crc;
                    _outputBuffer[size - 3] = (byte) (crc >> 8);
                    _outputBuffer[size - 2] = (byte) (crc >> 16);
                    _outputBuffer[size - 1] = (byte) (crc >> 24);

                    _hidDevice.FileStream.Write(_outputBuffer, 0, size);
                    _hidDevice.FileStream.Flush();
                }
            }
        }

        public void Start()
        {
            _stream.Start();
            _workerThread.Start();
        }
    }
}