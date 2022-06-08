using System;
using DS4Windows;
using FFT.CRC;

namespace DS4AudioStreamer.Sound
{
    public class NewCaptureWorker
    {
        private readonly HidDevice _hidDevice;
        private readonly byte _id;

        private readonly byte[] _outputBuffer = new byte[640];
        private ushort _lilEndianCounter = 0;

        public NewCaptureWorker(
            SbcAudioStream audioStream, 
            HidDevice hidDevice, 
            byte id
        ) {
            _hidDevice = hidDevice;
            _id = id;
            
            hidDevice.OpenFileStream(_outputBuffer.Length);
            
            NativeMethods.HidD_SetNumInputBuffers(hidDevice.SafeReadHandle.DangerousGetHandle(), 2);

            audioStream.SbcFramesAvailable += AudioStreamOnSbcFramesAvailable;
        }

        private unsafe void AudioStreamOnSbcFramesAvailable(byte[] data, int framesAvailable, int dataLength)
        {
            int protocol, size;
            
            switch (framesAvailable)
            {
                case 4:
                    protocol = 0x17;
                    size = 462;
                    break;
                case 2:
                    protocol = 0x14;
                    size = 270;
                    break;
                default:
                    return;
            }
            
            Array.Fill<byte>(_outputBuffer, 0);
            
            _outputBuffer[0] = (byte) protocol;
            _outputBuffer[1] = 0x40;  // Unknown
            _outputBuffer[2] = 0xa2;  // Unknown
            
            _outputBuffer[3] = (byte) (_lilEndianCounter & 0xFF);
            _outputBuffer[4] = (byte) ((_lilEndianCounter >> 8) & 0xFF);
            
            // _outputBuffer[5] = 0x02; // 0x02 Speaker Mode On / 0x24 Headset Mode On
            _outputBuffer[5] = 0x24; // 0x02 Speaker Mode On / 0x24 Headset Mode On

            _lilEndianCounter += (ushort) framesAvailable;
            
            Buffer.BlockCopy(data, 0, _outputBuffer, 6, dataLength);
            
            var crc = CRC32Calculator.SEED;
            byte btHeader = 0xa2;
            CRC32Calculator.Add(ref crc, new ReadOnlySpan<byte>(&btHeader, 1));
            CRC32Calculator.Add(ref crc, new ReadOnlySpan<byte>(_outputBuffer, 0, size - 4));
            crc = CRC32Calculator.Finalize(crc);
            
            _outputBuffer[size - 4] = (byte) crc;
            _outputBuffer[size - 3] = (byte) (crc >> 8);
            _outputBuffer[size - 2] = (byte) (crc >> 16);
            _outputBuffer[size - 1] = (byte) (crc >> 24);
            _outputBuffer[size] = _id;

            _hidDevice.FileStream.Write(_outputBuffer, 0, size + 1);
            _hidDevice.FileStream.Flush();
        }
    }
}