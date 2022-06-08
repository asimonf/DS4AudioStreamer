using System;
using System.Runtime.InteropServices;

namespace SharpSBC
{
    public static unsafe class Native
    {
        private const string LibraryName = "libs/libsbc.dll";

        /* sampling frequency */
        public const int SBC_FREQ_16000 = 0x00;
        public const int SBC_FREQ_32000 = 0x01;
        public const int SBC_FREQ_44100 = 0x02;
        public const int SBC_FREQ_48000 = 0x03;

        /* blocks */
        public const int SBC_BLK_4 = 0x00;
        public const int SBC_BLK_8 = 0x01;
        public const int SBC_BLK_12 = 0x02;
        public const int SBC_BLK_16 = 0x03;

        /* channel mode */
        public const int SBC_MODE_MONO = 0x00;
        public const int SBC_MODE_DUAL_CHANNEL = 0x01;
        public const int SBC_MODE_STEREO = 0x02;
        public const int SBC_MODE_JOINT_STEREO = 0x03;

        /* allocation method */
        public const int SBC_AM_LOUDNESS = 0x00;
        public const int SBC_AM_SNR = 0x01;

        /* subbands */
        public const int SBC_SB_4 = 0x00;
        public const int SBC_SB_8 = 0x01;

        /* data endianess */
        public const int SBC_LE = 0x00;
        public const int SBC_BE = 0x01;

        public struct sbc_t
        {
            public uint flags;

            public byte frequency;
            public byte blocks;
            public byte subbands;
            public byte mode;
            public byte allocation;
            public byte bitpool;
            public byte endian;

            public IntPtr priv;
            public IntPtr priv_alloc_base;
        }

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_init(sbc_t* sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_init(ref sbc_t sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_reinit(sbc_t* sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_reinit(ref sbc_t sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_init_msbc(sbc_t* sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_init_msbc(ref sbc_t sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_reinit_msbc(sbc_t* sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_reinit_msbc(ref sbc_t sbc, uint flags);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_init_a2dp(sbc_t* sbc, uint flags, void* conf, ulong conf_len);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_init_a2dp(ref sbc_t sbc, uint flags, void* conf, ulong conf_len);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_reinit_a2dp(sbc_t* sbc, uint flags, void* conf, ulong conf_len);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern int sbc_reinit_a2dp(ref sbc_t sbc, uint flags, void* conf, ulong conf_len);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long sbc_parse(sbc_t* sbc, void* input, ulong input_len);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long sbc_parse(ref sbc_t sbc, void* input, ulong input_len);

        /* Decodes ONE input block into ONE output block */
        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long sbc_decode(sbc_t* sbc, void* input, ulong input_len, void* output, ulong output_len,
            ulong* written);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long sbc_decode(ref sbc_t sbc, void* input, ulong input_len, void* output,
            ulong output_len, ulong* written);

        /* Encodes ONE input block into ONE output block */
        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long sbc_encode(sbc_t* sbc, void* input, ulong input_len, void* output, ulong output_len,
            long* written);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern long sbc_encode(ref sbc_t sbc, void* input, ulong input_len, void* output,
            ulong output_len, long* written);

        /* Returns the compressed block size in bytes */
        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern ulong sbc_get_frame_length(sbc_t* sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern ulong sbc_get_frame_length(ref sbc_t sbc);

        /* Returns the time one input/output block takes to play in msec*/
        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint sbc_get_frame_duration(sbc_t* sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint sbc_get_frame_duration(ref sbc_t sbc);

        /* Returns the uncompressed block size in bytes */
        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern ulong sbc_get_codesize(sbc_t* sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern ulong sbc_get_codesize(ref sbc_t sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string sbc_get_implementation_info(sbc_t* sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string sbc_get_implementation_info(ref sbc_t sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void sbc_finish(sbc_t* sbc);

        [DllImport(LibraryName, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void sbc_finish(ref sbc_t sbc);
    }
}