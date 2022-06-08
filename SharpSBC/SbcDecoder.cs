using System;
using static SharpSBC.Native;

namespace SharpSBC
{
    public unsafe class SbcDecoder: IDisposable
    {
        private sbc_t _sbc;

        public ulong Codesize { get; }

        public SbcDecoder()
        {
            _sbc = new sbc_t();

            var sbcInit = sbc_init(ref _sbc, 0);
            if (sbcInit < 0)
                throw new Exception("");
            
        }
        
        public long Decode(byte* src, byte* dst, ulong dstSize, out long encoded)
        {
            long tmp;
            var len = sbc_encode(ref _sbc, src, Codesize, dst, dstSize, &tmp);
            encoded = tmp;

            return len;
        }

        public void Dispose()
        {
            sbc_finish(ref _sbc);
        }
    }
}