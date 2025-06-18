using System.Collections.Generic;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    public class WireNut : MonoBehaviour
    {
        public GangBoxConductor[] CompatibleConductors => compatibleConductors;
        public HashSet<GangBoxConductor> Conductors { get; private set; }
        [field: SerializeField] public Task TerminationTask { get; private set; }

        [SerializeField] private GangBoxConductor[] compatibleConductors;

        private void Awake()
        {
            Conductors = new HashSet<GangBoxConductor>(compatibleConductors);
        }
    }
}
