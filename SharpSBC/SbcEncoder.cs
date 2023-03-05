using System;
using static SharpSBC.Native;

namespace SharpSBC
{
    public class SbcEncoder : IDisposable
    {
        private sbc_t _sbc;

        public ulong CodeSize { get; }
        public ulong FrameSize { get; }

        public SbcEncoder(
            int sampleRate,
            SubBandCount subBandsCount,
            int bitPool,
            ChannelMode channelMode,
            AllocationMode snr,
            BlockCount blocks
        )
        {
            _sbc = new sbc_t();

            var sbcInit = sbc_init(ref _sbc, 0);
            if (sbcInit < 0)
                throw new Exception("Could not init SBC Encoder");

            _sbc.frequency = sampleRate switch
            {
                16000 => SBC_FREQ_16000,
                32000 => SBC_FREQ_32000,
                44100 => SBC_FREQ_44100,
                48000 => SBC_FREQ_48000,
                _ => _sbc.frequency
            };

            _sbc.subbands = (byte)subBandsCount;

            _sbc.mode = (byte)channelMode;

            _sbc.endian = SBC_LE;

            _sbc.bitpool = (byte)bitPool;
            _sbc.allocation = (byte)snr;

            _sbc.blocks = (byte)blocks;

            CodeSize = sbc_get_codesize(ref _sbc);
            FrameSize = sbc_get_frame_length(ref _sbc);
        }

        public long Encode(ReadOnlySpan<byte> src, ReadOnlySpan<byte> dst, ulong dstSize, out ulong encoded)
        {
            ulong tmp;
            long len;

            unsafe
            {
                fixed (byte* psrc = src)
                fixed (byte* pdst = dst)
                {
                    len = sbc_encode(ref _sbc, psrc, CodeSize, pdst, dstSize, &tmp);
                }
            }

            encoded = tmp;

            return len;
        }

        public void Dispose()
        {
            sbc_finish(ref _sbc);
        }
    }
}