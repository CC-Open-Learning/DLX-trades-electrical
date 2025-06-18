using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VARLab.CloudSave;
using Newtonsoft.Json;

namespace VARLab.TradesElectrical
{
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class WireMountingTask : MountingTask, ICloudSerialized
    {
        [field: SerializeField] public MountableName HostBoxName { get; private set; } = MountableName.None;

        [field: SerializeField, Tooltip("Override termination/connection sequence")]
        public MountableName[] OverriddenSequence { get; private set; }

        [field: Header("Overridden Conductor Names")] 
        [field: SerializeField] public MountableName OverriddenBondName { get; private set; } = MountableName.None;
        [field: SerializeField] public MountableName OverriddenNeutralName { get; private set; } = MountableName.None;
        [field: SerializeField] public MountableName OverriddenHotName { get; private set; } = MountableName.None;

        public TerminationOptionType RequestedOptionType { get; set; }
        public Mountable HostBoxInstance { get; set; }

        public Dictionary<MountableName, TerminationOptionType> CorrectOptionsMap { get; private set; }
        public Dictionary<MountableName, InteractableWire> SelectedOptionsMap { get; private set; }
        // This will be used to save the current state of the wire termination options
        [JsonProperty] public Dictionary<MountableName, TerminationOptionType> SavedWireTerminationOptions;

        public Dictionary<MountableName, WireTerminationOption> SelectedOrbOptions;
        public List<InteractableWire> ConductorCorrectlyCompleted { get; set; } = new();

        [Header("Expected Selections")] 
        [SerializeField] private TerminationOptionType correctOptionBondWire;
        [SerializeField] private TerminationOptionType correctOptionNeutralWire;
        [SerializeField] private TerminationOptionType correctOptionHotWire;

        private void Awake()
        {
            CorrectOptionsMap = new Dictionary<MountableName, TerminationOptionType>()
            {
                { MountableName.BondWire, correctOptionBondWire },
                { MountableName.NeutralWire, correctOptionNeutralWire },
                { MountableName.HotWire, correctOptionHotWire }
            };
            
            SelectedOptionsMap = new Dictionary<MountableName, InteractableWire>()
            {
                { MountableName.BondWire, null},
                { MountableName.NeutralWire, null},
                { MountableName.HotWire, null}
            };
            
            SelectedOrbOptions = new Dictionary<MountableName, WireTerminationOption>()
            {
                { MountableName.BondWire, null },
                { MountableName.NeutralWire, null },
                { MountableName.HotWire, null }
            };
        }

        public void OnSerialize()
        {
            // Initialize SavedWireTerminationOptions if it's null
            SavedWireTerminationOptions ??= new Dictionary<MountableName, TerminationOptionType>()
            {
                { MountableName.BondWire, TerminationOptionType.None },
                { MountableName.NeutralWire, TerminationOptionType.None },
                { MountableName.HotWire, TerminationOptionType.None }
            };

            // If SelectedInteractableWiresOptionsMap is null, we'll just use the last saved options
            if (SelectedOptionsMap == null)
            {
                return;
            }

            // Try get value from SelectedInteractableWiresOptionsMap for BondWire, NeutralWire, HotWire
            if (SelectedOptionsMap.TryGetValue(MountableName.BondWire, out var bondWire) && bondWire)
            {
                SavedWireTerminationOptions[MountableName.BondWire] = bondWire.Option;
            }
            if (SelectedOptionsMap.TryGetValue(MountableName.NeutralWire, out var neutralWire) && neutralWire)
            {
                SavedWireTerminationOptions[MountableName.NeutralWire] = neutralWire.Option;
            }
            if (SelectedOptionsMap.TryGetValue(MountableName.HotWire, out var hotWire) && hotWire)
            {
                SavedWireTerminationOptions[MountableName.HotWire] = hotWire.Option;
            }
        }

        /// <summary>
        /// Checks if the task is fully correct.
        /// </summary>
        public override bool IsCorrect()
        {
            bool isAllCorrect = false;
            foreach (var kvp in CorrectOptionsMap)
            {
                MountableName mountableName = kvp.Key;
                TerminationOptionType correctOption = kvp.Value;

                if (SelectedOptionsMap.TryGetValue(mountableName, out InteractableWire wire))
                {
                    // Compare the TerminationOptionType with the Option property of InteractableWire
                    if (wire.Option == correctOption)
                    {
                        isAllCorrect = true;
                    }
                    else
                    {
                        isAllCorrect = false;
                        break;
                    }
                }
            }

            return isAllCorrect;

        }

        /// <summary>
        /// Determine if all wires are terminated in a box
        /// </summary>
        /// <returns>Return true if all the wires are terminated in the relevant box</returns>
        public virtual bool IsComplete()
        {
            return SelectedOrbOptions.Values.All(option => option);
        }

        public MountableName GetOverridenName(MountableName overrideToRetrieve)
        {
            MountableName overriddenName = overrideToRetrieve switch
            {
                MountableName.BondWire => OverriddenBondName,
                MountableName.NeutralWire => OverriddenNeutralName,
                MountableName.HotWire => OverriddenHotName,
                _ => MountableName.None
            };
            return overriddenName;
        }
        
        public void ResetTask()
        {
            SelectedOptionsMap = new Dictionary<MountableName, InteractableWire>()
            {
                { MountableName.BondWire, null},
                { MountableName.NeutralWire, null},
                { MountableName.HotWire, null}
            };

            SelectedOrbOptions = new Dictionary<MountableName, WireTerminationOption>()
            {
                { MountableName.BondWire, null },
                { MountableName.NeutralWire, null },
                { MountableName.HotWire, null }
            };

            HostBoxInstance = null;
            SelectedMountable = null;
            RequestedMountableName = MountableName.None;
            PreviewingMountable = null;
        }
    }
}
