using UnityEngine;

namespace VARLab.TradesElectrical
{
    public enum TerminationOptionType
    {
        // Common for all boxes
        None,
        BentOut,
        TuckedNeat,
        TiedToBond,
        // Options specific for Gang Box
        TiedToWireNutAlone = 20,
        TiedToWireNutWithOthers,
        // Options specific to device connections
        TiedToNeutral = 50,
        TiedToHot,
        TiedToLoad,
        TiedToLine,
        TiedToBondLugSameSide,
        TiedToBondLugOppositeSide,
    }

    public class WireTerminationOption : MonoBehaviour
    {
        [field: SerializeField] public TerminationOptionType Option { get; private set; }
        [field: SerializeField] public Task TerminationTask { get; private set; }
    }
}
