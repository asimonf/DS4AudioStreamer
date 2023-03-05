using static SharpSBC.Native;

namespace SharpSBC
{
    public enum BlockCount: int
    {
        Blk4 = SBC_BLK_4,
        Blk8 = SBC_BLK_8,
        Blk12 = SBC_BLK_12,
        Blk16 = SBC_BLK_16,
    }
}