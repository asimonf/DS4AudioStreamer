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
            : base(captureDevice, true, buffer)
        {
        }

        public static MMDevice GetDefaultLoopbackCaptureDevice() => new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

        protected override AudioClientStreamFlags GetAudioClientStreamFlags() => AudioClientStreamFlags.Loopback | base.GetAudioClientStreamFlags();
    }
}