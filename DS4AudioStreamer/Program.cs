// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DS4AudioStreamer;
using DS4AudioStreamer.Sound;
using FFT.CRC;

Console.Write("Raising priority... ");

try
{
    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
    Console.WriteLine("Done!");
}
catch
{
    Console.WriteLine("Failed! (ignoring)");
} 

var hidDevices = DeviceEnumerator.FindDevices();

var usedDevice = hidDevices.FirstOrDefault();

if (null == usedDevice)
{
    Console.WriteLine("No device found");
    return;
}

usedDevice.OpenDevice(false);

var audioStream = new SbcAudioStream();
var captureWorker = new NewCaptureWorker(audioStream, usedDevice, 1);

audioStream.Start();

while (usedDevice.IsConnected && audioStream.Capturing)
{
    
    Thread.Sleep(100);
}
