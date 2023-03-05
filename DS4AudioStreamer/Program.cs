using DS4AudioStreamer;
using DS4AudioStreamer.Sound;

var hidDevices = DeviceEnumerator.FindDevices();

var usedDevice = hidDevices.FirstOrDefault();

if (null == usedDevice)
{
    Console.WriteLine("No device found");
    return;
}

usedDevice.OpenDevice(true);

if (!usedDevice.IsOpen)
{
    Console.WriteLine("Could not open device exclusively :(");
    usedDevice.OpenDevice(false);
}

var captureWorker = new NewCaptureWorker(usedDevice, 1);
captureWorker.Start();

while (usedDevice.IsConnected)
{
    Thread.Sleep(100);
}
