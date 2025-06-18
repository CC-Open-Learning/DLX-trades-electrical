using Newtonsoft.Json;
using UnityEngine;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    [CloudSaved, JsonObject(MemberSerialization.OptIn)]
    public class BoxMountingTask : MountingTask, ICloudSerialized, ICloudDeserialized
    {
        [field: SerializeField] public MountableName[] MountableOptions { get; private set; }
        [field: SerializeField] public MountLocation[] LocationOptions { get; private set; }

        [field: SerializeField, Header("Expected Selections")]
        public MountableName CorrectMountable { get; private set; }

        [field: SerializeField] public MountLocation CorrectLocation { get; private set; }

        public MountLocation RequestedLocation { get; set; } = null;
        public MountLocation SelectedLocation { get; set; } = null;

        public MountableAction Action { get; set; } = MountableAction.Move | MountableAction.Replace;

        [JsonProperty] private int locationIndex = -1;

        public override bool IsCorrect()
        {
            bool isLocationCorrect = SelectedLocation == CorrectLocation;
            bool isMountableCorrect = SelectedMountable.Name == CorrectMountable;

            return isLocationCorrect && isMountableCorrect;
        }

        public void OnSerialize()
        {
            for (int i = 0; i < LocationOptions.Length; i++)
            {
                if (SelectedLocation != LocationOptions[i]) { continue; }

                locationIndex = i;
                break;
            }
        }

        public void OnDeserialize()
        {
            // Restore selected location if we have a saved ID
            if (locationIndex != -1)
            {
                SelectedLocation = LocationOptions[locationIndex];
            }
        }
    }
}