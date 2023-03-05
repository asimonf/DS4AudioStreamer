using System;
using static SharpSBC.Native;

namespace SharpSBC
{
    public class SbcDecoder: IDisposable
    {
        private sbc_t _sbc;

        public ulong CodeSize { get; }

        public SbcDecoder()
        {
            _sbc = new sbc_t();

            var sbcInit = sbc_init(ref _sbc, 0);
            if (sbcInit < 0)
                throw new Exception("");

            CodeSize = sbc_get_codesize(ref _sbc);
        }
        
        public long Decode(ReadOnlySpan<byte> src, ReadOnlySpan<byte> dst, ulong dstSize, out ulong encoded)
        {
            ulong tmp;
            long len;

            unsafe
            {
                fixed (byte* psrc = src)
                fixed (byte* pdst = dst)
                {
                    len = sbc_decode(ref _sbc, psrc, CodeSize, pdst, dstSize, &tmp);
                }

                encoded = tmp;
            }

            return len;
        }

        public void Dispose()
        {
            sbc_finish(ref _sbc);
        }
    }
}