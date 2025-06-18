using UnityEngine;

namespace VARLab.TradesElectrical
{
    public class GangBoxConductor : InteractableWire
    {
        [field: SerializeField] public MountableName ConnectedBox { get; private set; }
    }
}
