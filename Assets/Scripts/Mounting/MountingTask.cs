using Cinemachine;
using Newtonsoft.Json;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class MountingTask : MonoBehaviour
    {
        [field: SerializeField] public Task TaskName { get; private set; } = Task.None;

        public virtual MountableName RequestedMountableName { get; set; } = MountableName.None;

        public Mountable SelectedMountable
        {
            get => selectedMountable;
            set
            {
                selectedMountable = value;
                selectedMountableName = value ? value.Name : MountableName.None;
            }
        }

        // This will be used for CloudSave loading purposes only. Use this to construct the
        // Mountable as needed.
        public MountableName SelectedMountableName => selectedMountableName;

        [field: SerializeField]
        public CinemachineVirtualCamera TaskPreviewCamera { get; private set; }

        public Mountable PreviewingMountable { get; set; }

        public abstract bool IsCorrect();

        private Mountable selectedMountable;
        [JsonProperty] private MountableName selectedMountableName = MountableName.None;
    }
}
