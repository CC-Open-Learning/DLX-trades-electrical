using UnityEngine;

namespace VARLab.TradesElectrical
{
    public class GangBoxTerminationOption : WireTerminationOption
    {
        [field: SerializeField] public MountableName ConnectedBox { get; private set; }
    }
}
