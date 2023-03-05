using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace DS4AudioStreamer.Sound
{
    public class MyLoopbackCapture: WasapiCapture
    {
        public MyLoopbackCapture(int buffer)
            : this(WasapiLoopbackCapture.GetDefaultLoopbackCaptureDevice(), buffer)
        {
        }

        public MyLoopbackCapture(MMDevice captureDevice, int buffer)
            : base(captureDevice, false, buffer)
        {
        }

        public static MMDevice GetDefaultLoopbackCaptureDevice() => new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        public override WaveFormat WaveFormat
        {
            get => base.WaveFormat;
            set => throw new InvalidOperationException("WaveFormat cannot be set for WASAPI Loopback Capture");
        }

        protected override AudioClientStreamFlags GetAudioClientStreamFlags() => AudioClientStreamFlags.Loopback;
    }
}