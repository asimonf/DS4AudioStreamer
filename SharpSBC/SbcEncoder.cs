using System;
using System.Runtime.InteropServices;
using static SharpSBC.Native;

namespace SharpSBC
{
    public class SbcEncoder: IDisposable
    {
        public enum ChannelMode: byte
        {
            Mono = SBC_MODE_MONO,
            DualChannel = SBC_MODE_DUAL_CHANNEL,
            JointStereo = SBC_MODE_JOINT_STEREO,
            Stereo = SBC_MODE_STEREO
        }
        
        private sbc_t _sbc;

        public ulong CodeSize { get; }
        public ulong FrameSize { get; }

        public SbcEncoder(int sampleRate, int subBands, int bitPool, ChannelMode channelMode, bool snr, int blocks)
        {
            _sbc = new sbc_t();

            var sbcInit = sbc_init(ref _sbc, 0);
            if (sbcInit < 0)
                throw new Exception("");

            _sbc.frequency = sampleRate switch
            {
                16000 => SBC_FREQ_16000,
                32000 => SBC_FREQ_32000,
                44100 => SBC_FREQ_44100,
                48000 => SBC_FREQ_48000,
                _ => _sbc.frequency
            };
            
            _sbc.subbands = (byte) (subBands == 4 ? SBC_SB_4 : SBC_SB_8);

            _sbc.mode = (byte) channelMode;
            
            _sbc.endian = SBC_LE;
            
            _sbc.bitpool = (byte) bitPool;
            _sbc.allocation = (byte) (snr ? SBC_AM_SNR : SBC_AM_LOUDNESS);

            _sbc.blocks = blocks switch
            {
                4 => SBC_BLK_4,
                8 => SBC_BLK_8,
                12 => SBC_BLK_12,
                _ => SBC_BLK_16
            };

            CodeSize = sbc_get_codesize(ref _sbc);
            FrameSize = sbc_get_frame_length(ref _sbc);
        }
        
        public unsafe long Encode(byte* src, byte* dst, ulong dstSize, out long encoded)
        {
            long tmp;
            var len = sbc_encode(ref _sbc, src, CodeSize, dst, dstSize, &tmp);
            encoded = tmp;
            
            return len;
        }
        
        public void Dispose()
        {
            sbc_finish(ref _sbc);
        }
    }
}