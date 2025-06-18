using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    [RequireComponent(typeof(WireMountingTask))]
    public class DeviceMountingTask : MountingTask
    {
        public const Task FirstDeviceConnectionMember = Task.ConnectFan; // Should it default to fan?

        [field: SerializeField, Tooltip("Location to instantiate before connecting conductors")]
        public MountLocation InteractionLocation { get; private set; }

        [field: SerializeField, Tooltip("Location to instantiate after connecting conductors")]
        public MountLocation InstalledLocation { get; private set; }
        
        [field: SerializeField]
        public MountableName CorrectDevice { get; private set; }
        
        public MountableName RequestedDevice { get; set; } = MountableName.None;

        [field: SerializeField, Tooltip("Tasks devices that will be hidden when installing")] 
        public List<DeviceMountingTask> SiblingTasks { get; private set; }
        
        private WireMountingTask wireMountingTask;

        // TODO change this because get and set are unrelated
        public override MountableName RequestedMountableName
        {
            get => base.RequestedMountableName;
            set => wireMountingTask.RequestedMountableName = value;
        }

        public override bool IsCorrect()
        {
            if (SelectedMountable.Name == CorrectDevice)
            {
                return true;
            }

            return false;
        }

        public bool IsWireTaskCorrect()
        {
            return wireMountingTask.IsCorrect();
        }

        public void ResetTask()
        {
            wireMountingTask.ResetTask();
        }

        private void Awake()
        {
            wireMountingTask = GetComponent<WireMountingTask>();
        }
    }
}
