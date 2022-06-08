using System;
using System.Diagnostics;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SharpSBC;
using static SharpSampleRate.SampleRate;

namespace DS4AudioStreamer.Sound
{
    public class SbcAudioStream: IDisposable
    {
        public delegate void OnSbcFramesAvailable(byte[] data, int framesAvailable, int dataLength);
        
        private const int StreamSampleRate = 32000;
        private const int ChannelCount = 2;
        
        private readonly MyLoopbackCapture _captureDevice;
        
        private readonly CircularBuffer<byte> _audioData;
        private readonly CircularBuffer<byte> _sbcAudioData;
        private readonly byte[] _resampledAudioBuffer;
        private readonly byte[] _reformattedAudioBuffer;

        private readonly IntPtr _resamplerState;
        private readonly double _resampleRatio;
        
        private readonly SbcEncoder _encoder;
        private readonly byte[] _sbcPreBuffer;
        private readonly byte[] _sbcPostBuffer;
        
        private readonly byte[] _sbcEventData;

        private bool _corrected = false;

        public bool Capturing => _captureDevice.CaptureState == CaptureState.Capturing;

        private readonly Stopwatch _watch = new Stopwatch();
        private double _lastLap;

        private int _bufferLatencySize;

        public event OnSbcFramesAvailable SbcFramesAvailable;

        public SbcAudioStream()
        {
            // Encoder
            _encoder = new SbcEncoder(
                StreamSampleRate,
                8,
                42,
                SbcEncoder.ChannelMode.JointStereo,
                true,
                16
            );
            
            _sbcPreBuffer = new byte[_encoder.CodeSize];
            _sbcPostBuffer = new byte[_encoder.FrameSize];
            _sbcEventData = new byte[_encoder.FrameSize * 4];
            
            // Capture Device
            _captureDevice = new MyLoopbackCapture(0);
            _captureDevice.DataAvailable += CaptureDeviceOnDataAvailable;

            _bufferLatencySize = (int) (_encoder.FrameSize * 8); // Collect at least 8 SBC frames
            
            // Buffers
            var bufferSize = _captureDevice.WaveFormat.ConvertLatencyToByteSize(8) * 16;
            _audioData = new CircularBuffer<byte>(bufferSize);
            _sbcAudioData = new CircularBuffer<byte>(bufferSize);
            _resampledAudioBuffer = new byte[bufferSize];
            _reformattedAudioBuffer = new byte[bufferSize];
            
            // Resampler
            _resamplerState = src_new(Quality.SRC_SINC_BEST_QUALITY, 2, out var error);
            _resampleRatio = StreamSampleRate / (double) _captureDevice.WaveFormat.SampleRate;
            if (IntPtr.Zero == _resamplerState)
            {
                throw new Exception(src_strerror(error));
            }
        }

        public void Start()
        {
            _captureDevice.StartRecording();
            _watch.Start();
            _lastLap = _watch.Elapsed.TotalMilliseconds;
        }

        public void WaitUntil(int size)
        { 
            int currentLength;

            lock (_sbcAudioData)
            {
                currentLength = _sbcAudioData.CurrentLength;
            }
            
            while (currentLength < size)
            {
                lock (_sbcAudioData)
                    currentLength = _sbcAudioData.CurrentLength;
            
                Thread.Yield();
            }
        }

        public void WaitUntilFull()
        { 
            WaitUntil(_bufferLatencySize * 2);
        }

        public void Stop()
        {
            _captureDevice.StopRecording();
            _watch.Stop();
        }

        private unsafe void CaptureDeviceOnDataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {   
                var sampleCount = e.BytesRecorded / 4;
                var reformatedSampleCount = sampleCount;
                
                fixed (byte* srcPtr = e.Buffer) 
                fixed (byte* resamplePtr = _resampledAudioBuffer)
                fixed (byte* reformatPtr = _reformattedAudioBuffer)    
                {
                    if (StreamSampleRate != _captureDevice.WaveFormat.SampleRate)
                    {
                        var frames = sampleCount / ChannelCount;
                        var convert = new SRC_DATA
                        {
                            data_in = (float*)srcPtr,
                            data_out = (float*)resamplePtr,
                            input_frames = frames,
                            output_frames = frames,
                            src_ratio = _resampleRatio
                        };
                    
                        var res = src_process(_resamplerState, ref convert);
                    
                        if (res != 0) throw new Exception(src_strerror(res));
                    
                        if (convert.input_frames != convert.input_frames_used) Console.WriteLine("Not all frames used (?)");
                
                        reformatedSampleCount = convert.output_frames_gen * ChannelCount;
                        src_float_to_short_array((float*)resamplePtr, (short*)reformatPtr, reformatedSampleCount);
                    }
                    else
                    {
                        src_float_to_short_array((float*)srcPtr, (short*)reformatPtr, sampleCount);
                    }
                }
                
                _audioData.CopyFrom(_reformattedAudioBuffer, reformatedSampleCount * 2);

                while (_audioData.CurrentLength >= (int) _encoder.CodeSize)
                {
                    _audioData.CopyTo(_sbcPreBuffer, (int) _encoder.CodeSize);
                
                    fixed (byte* srcPtr = _sbcPreBuffer)
                    fixed (byte* destPtr = _sbcPostBuffer)
                    {
                        _encoder.Encode(srcPtr, destPtr, _encoder.CodeSize, out var encoded);
                
                        if (encoded <= 0)
                        {
                            Console.WriteLine("Not encoded");
                        }
                    }
                    
                    lock (_sbcAudioData)
                        _sbcAudioData.CopyFrom(_sbcPostBuffer, _sbcPostBuffer.Length);
                }

                var framesAvailable = _sbcAudioData.CurrentLength / (int) _encoder.FrameSize;

                if (framesAvailable >= 4)
                    framesAvailable = 4;
                else if (framesAvailable is >= 2 and < 4)
                    framesAvailable = 2;
                else
                {
                    Console.WriteLine("Underflow {0}", framesAvailable);
                    return;
                }

                var sbcFrameDataLength = framesAvailable * (int) _encoder.FrameSize;
                
                _sbcAudioData.CopyTo(_sbcEventData, sbcFrameDataLength);

                SbcFramesAvailable?.Invoke(_sbcEventData, framesAvailable, sbcFrameDataLength);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: {0}", exception);
            }
        }

        public void Dispose()
        {
            _captureDevice?.Dispose();
            _encoder?.Dispose();
        }
    }
}