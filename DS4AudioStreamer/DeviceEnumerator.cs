using DS4AudioStreamer.HidLibrary;
using DS4Windows;

namespace DS4AudioStreamer
{
    static class DeviceEnumerator
    {
        internal const int SonyVid = 0x054C;

        private static VidPidInfo[] knownDevices =
        {
            new(SonyVid, 0x5C4, "DS4 v.1"),
            new(SonyVid, 0x09CC, "DS4 v.2"),
        };

        public static List<HidDevice> FindDevices()
        {
            var hDevices = HidDevices.EnumerateDS4(knownDevices);
            var tempList = hDevices.ToList();
            return tempList;
        }
    }
}
