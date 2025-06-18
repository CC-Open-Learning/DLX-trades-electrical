using System;

namespace VARLab.TradesElectrical
{
    public enum MountingSide
    {
        None = -1,
        Front,
        Back,
        Left,
        Right,
        Up,
        Down
    }

    [Serializable]
    public struct MountingSetting
    {
        public MountableName mountable;
        public MountingSide side;
    }
}
