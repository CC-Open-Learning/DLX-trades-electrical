using UnityEngine;

namespace VARLab.TradesElectrical
{
    public class Device : Mountable
    {
        [field: SerializeField]
        public MountableName PreConnectionMountable { get; private set; } = MountableName.None;

        public Mountable PreConnectionMountableInstance { get; set; }
    }
}
