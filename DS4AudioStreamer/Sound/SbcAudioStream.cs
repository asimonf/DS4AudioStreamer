using NAudio.CoreAudioApi;
using NAudio.Wave;
using SharpSBC;
using static SharpSampleRate.SampleRate;

namespace DS4AudioStreamer.Sound
{
    public class SbcAudioStream: IDisposable
    {
        private const int STREAM_SAMPLE_RATE = 32000;
        private const int CHANNEL_COUNT = 2;
        
        private readonly WasapiCapture _captureDevice;
        
        private readonly CircularBuffer<byte> _audioData;
        private readonly CircularBuffer<byte> _sbcAudioData;
        private readonly byte[] _resampledAudioBuffer;
        private readonly byte[] _reformattedAudioBuffer;

        private readonly IntPtr _resamplerState;
        private readonly double _resampleRatio;
        
        private readonly SbcEncoder _encoder;
        private readonly byte[] _sbcPreBuffer;
        private readonly byte[] _sbcPostBuffer;
        
        public CircularBuffer<byte> SbcAudioData => _sbcAudioData;

        public int FrameSize => (int) _encoder.FrameSize;

        public int CurrentFrameCount => (_sbcAudioData.CurrentLength / FrameSize);

        public SbcAudioStream()
        {
            // Encoder
            _encoder = new SbcEncoder(
                STREAM_SAMPLE_RATE,
                8,
                48,
                SbcEncoder.ChannelMode.JointStereo,
                true,
                16
            );
            
            _sbcPreBuffer = new byte[_encoder.CodeSize];
            _sbcPostBuffer = new byte[_encoder.FrameSize];
            
            // Capture Device
            _captureDevice = new MyLoopbackCapture(0);
            _captureDevice.DataAvailable += CaptureDeviceOnDataAvailable;

            // Buffers
            var bufferSize = _captureDevice.WaveFormat.ConvertLatencyToByteSize(32);
            _audioData = new CircularBuffer<byte>(bufferSize);
            _sbcAudioData = new CircularBuffer<byte>(bufferSize);
            _resampledAudioBuffer = new byte[bufferSize];
            _reformattedAudioBuffer = new byte[bufferSize];
            
            // Resampler
            _resamplerState = src_new(Quality.SRC_SINC_BEST_QUALITY, CHANNEL_COUNT, out var error);
            _resampleRatio = STREAM_SAMPLE_RATE / (double) _captureDevice.WaveFormat.SampleRate;
            if (IntPtr.Zero == _resamplerState)
            {
                throw new Exception(src_strerror(error));
            }
        }

        public void Start()
        {
            _captureDevice.StartRecording();
        }

        public void WaitUntil(int frameCount)
        {
            while (_sbcAudioData.CurrentLength < frameCount * FrameSize)
                Thread.Yield();
        }

        public void Stop()
        {
            _captureDevice.StopRecording();
        }

        private unsafe void CaptureDeviceOnDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {   
                var sampleCount = e.BytesRecorded / 4;
                
                var reformatedSampleCount = sampleCount;
                
                fixed (byte* srcPtr = e.Buffer) 
                fixed (byte* resamplePtr = _resampledAudioBuffer)
                fixed (byte* reformatPtr = _reformattedAudioBuffer)    
                {
                    if (STREAM_SAMPLE_RATE != _captureDevice.WaveFormat.SampleRate)
                    {
                        var frames = sampleCount / CHANNEL_COUNT;
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
                
                        reformatedSampleCount = convert.output_frames_gen * CHANNEL_COUNT;
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

                    long length;
                
                    fixed (byte* srcPtr = _sbcPreBuffer)
                    fixed (byte* destPtr = _sbcPostBuffer)
                    {
                        _encoder.Encode(srcPtr, destPtr, _encoder.CodeSize, out length);
                    }
                    
                    if (length <= 0)
                        Console.WriteLine("Not encoded");
                    else
                        _sbcAudioData.CopyFrom(_sbcPostBuffer, (int) length);                        
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: {0}", exception);
            }
        }

        public void Dispose()
        {
            _captureDevice.Dispose();
            _encoder.Dispose();
        }
    }
}