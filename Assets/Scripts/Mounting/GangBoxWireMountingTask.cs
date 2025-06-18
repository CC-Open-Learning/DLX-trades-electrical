using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VARLab.CloudSave;
using Newtonsoft.Json;

namespace VARLab.TradesElectrical
{
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class GangBoxWireMountingTask : WireMountingTask
    {
        [Serializable]
        public class ConductorSelection
        {
            public MountableName Conductor;
            public TerminationOptionType TerminationOption;
            [NonSerialized] public GangBoxTerminationOption currentSelectedOrbOption;
            
            public ConductorSelection()
            {
                Conductor = MountableName.None;
                TerminationOption = TerminationOptionType.None;
            }

            public ConductorSelection(MountableName conductor)
            {
                Conductor = conductor;
                TerminationOption = TerminationOptionType.None;
            }

            public ConductorSelection(MountableName conductor, TerminationOptionType option)
            {
                Conductor = conductor;
                TerminationOption = option;
            }
            
            public ConductorSelection(MountableName conductor, TerminationOptionType optionType, GangBoxTerminationOption option)
            {
                Conductor = conductor;
                TerminationOption = optionType;
                currentSelectedOrbOption = option;
            }
        }

        // Key = Connected box, Value = Conductors and correct options
        public Dictionary<MountableName, ConductorSelection> GangBoxCorrectOptionsMap { get; private set; }

        // Key = Connected box, Value = Conductors and selected options
        [JsonProperty]
        public Dictionary<MountableName, ConductorSelection> GangBoxSelectedOptionsMap;

        [NonSerialized] public WireNut PreviewingWireNut;

        [Header("Expected Selections for Gang Box")] 
        [SerializeField] private ConductorSelection outletCorrect;
        [SerializeField] private ConductorSelection fanCorrect;
        [SerializeField] private ConductorSelection lightCorrect;
        [SerializeField] private ConductorSelection panelCorrect;
        
        private void Awake()
        {
            GangBoxCorrectOptionsMap = new Dictionary<MountableName, ConductorSelection>
            {
                { MountableName.DeviceBox, outletCorrect },
                { MountableName.FanBox, fanCorrect },
                { MountableName.OctagonalBracketBox, lightCorrect },
                { MountableName.Panel, panelCorrect },
            };

            GangBoxSelectedOptionsMap = new Dictionary<MountableName, ConductorSelection>
            {
                { MountableName.DeviceBox, new ConductorSelection(MountableName.DeviceBox) },
                { MountableName.FanBox, new ConductorSelection(MountableName.FanBox) },
                { MountableName.OctagonalBracketBox, new ConductorSelection(MountableName.OctagonalBracketBox) },
                { MountableName.Panel, new ConductorSelection(MountableName.Panel) },
            };
        }

        public override bool IsCorrect()
        {
            bool isAllCorrect = false;
            foreach (var kvp in GangBoxCorrectOptionsMap)
            {
                MountableName mountableName = kvp.Key;
                ConductorSelection correctSelection = kvp.Value;
                if (correctSelection.Conductor == GangBoxSelectedOptionsMap[mountableName].Conductor &&
                    correctSelection.TerminationOption == GangBoxSelectedOptionsMap[mountableName].TerminationOption)
                {
                    isAllCorrect = true;
                }
                else
                {
                    isAllCorrect = false;
                    break;

                }
            }

            return isAllCorrect;
        }

        public override bool IsComplete()
        {
            return GangBoxSelectedOptionsMap.Values.All(
                    conductor => conductor.TerminationOption != TerminationOptionType.None);
        }
    }
}
