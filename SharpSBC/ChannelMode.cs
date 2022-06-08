using static SharpSBC.Native;

namespace SharpSBC
{
    public enum ChannelMode: byte
    {
        Mono = SBC_MODE_MONO,
        DualChannel = SBC_MODE_DUAL_CHANNEL,
        JointStereo = SBC_MODE_JOINT_STEREO,
        Stereo = SBC_MODE_STEREO
    }
}