using UnityEngine;

namespace VARLab.TradesElectrical
{
    public class InteractableWire : Mountable
    {
        [field: SerializeField] public TerminationOptionType Option { get; private set; }
        [field: SerializeField] public Task TerminationTask { get; private set; }
    }
}
