using UnityEngine;
using UnityEngine.Events;

namespace VARLab.TradesElectrical
{
    public class DeviceMounter : Mounter, IReplaceable
    {
        [Header("Events")]
        public UnityEvent<DeviceMountingTask> MountingFinalized = new();

        [Header("References")]
        [SerializeField] private EquipmentFactory factory;
        [SerializeField] private WireMounter wireMounter;


        private DeviceMountingTask currentDeviceTask;
        private MountableName requestedDeviceName;
        private GameObject interactingDeviceObj;
        private GameObject postConnectionDeviceObj;

        private void Start()
        {
            wireMounter.DeviceWireConnectionFinalized.AddListener(ConductorConnectionCompleted);
        }

        public override void OnMountPreview(MountingTask mountingTask)
        {
            if (mountingTask is not DeviceMountingTask deviceMountingTask) 
            {
                Debug.LogError($"Device Mounter reeceived an invalid mounting task: {mountingTask.name}");
                return;
            }

            // A device has already been installed by this task
            if (!currentDeviceTask && deviceMountingTask.InstalledLocation.MountedItem)
            {
                OnMountableReplace(deviceMountingTask);
            }

            currentDeviceTask = deviceMountingTask;
            PrepareToPreview(deviceMountingTask);
        }

        public override void OnMountConfirm()
        {
            wireMounter.OnMountConfirm();
        }

        public override void OnMountRedo()
        {
            wireMounter.OnMountRedo();
        }

        public void OnMountableReplace(MountingTask mountingTask)
        {
            DeviceMountingTask deviceMountingTask = mountingTask as DeviceMountingTask;
            if (!deviceMountingTask)
            {
                Debug.LogError("Trying to replace an invalid device");
                return;
            }

            deviceMountingTask.InstalledLocation.gameObject.SetActive(true);
            deviceMountingTask.InstalledLocation.DestroyMountable();
            deviceMountingTask.InstalledLocation.gameObject.SetActive(false);

            wireMounter.ReleaseFetchedComponents(deviceMountingTask.InteractionLocation.MountedItem);
            deviceMountingTask.InteractionLocation.gameObject.SetActive(true);
            deviceMountingTask.InteractionLocation.DestroyMountable();
            deviceMountingTask.InteractionLocation.gameObject.SetActive(false);

            deviceMountingTask.ResetTask();
        }

        private void ConductorConnectionCompleted()
        {

            if (postConnectionDeviceObj)
            {
                // Hide currently interacting object
                interactingDeviceObj.SetActive(false);

                // Show the final installed device after connecting
                postConnectionDeviceObj.SetActive(true);

                interactingDeviceObj = postConnectionDeviceObj;
            }

            MountLocation location = currentDeviceTask.InstalledLocation;
            location.gameObject.SetActive(true);
            location.PlaceMountable(interactingDeviceObj.transform, requestedDeviceName);
            location.gameObject.SetActive(false);

            SetDeviceConductorsInteractable(interactingDeviceObj, false);

            currentDeviceTask.SelectedMountable = interactingDeviceObj.GetComponentInChildren<Device>();
            SetSharedHostBoxSwitchesActive(currentDeviceTask, true);
            MountingFinalized?.Invoke(currentDeviceTask);
            
            Reset();
        }
        
        /// <summary>
        /// Go through list of tasks that shares same host box and enables or disables based on parameter passed 
        /// </summary>
        /// <param name="mountingTask"></param>
        /// <param name="enable"></param>
        private void SetSharedHostBoxSwitchesActive(DeviceMountingTask mountingTask, bool enable)
        {
            if (mountingTask.SiblingTasks.Count != 0)
            {
                foreach (var otherTask in mountingTask.SiblingTasks)
                {
                    bool isOtherSwitchInstalled = otherTask.InstalledLocation.MountedItem != null;
                    if (isOtherSwitchInstalled)
                    {
                        SetSelectedDeviceEnable(otherTask, enable);
                    } 
                }
            }
        }

        private void Reset()
        {
            currentDeviceTask = null;
            requestedDeviceName = MountableName.None;
            interactingDeviceObj = null;
            postConnectionDeviceObj = null;
        }

        private void PrepareToPreview(DeviceMountingTask mountingTask)
        {
            SetSharedHostBoxSwitchesActive(mountingTask, false);
            var wireMountingTask = mountingTask.GetComponent<WireMountingTask>();
            if (!interactingDeviceObj)
            {
                PreviewDevice(mountingTask);

                wireMounter.FetchComponentsFromObject(interactingDeviceObj);
                wireMounter.PrepareToPreview(wireMountingTask);
            }
            else
            {
                wireMounter.ShowTerminationOptions(wireMountingTask);
            }
        }
        
        /// <summary>
        /// Disables the device that is installed on task
        /// </summary>
        /// <param name="mountingTask"></param>
        /// <param name="enable"></param>
        public void SetSelectedDeviceEnable(DeviceMountingTask mountingTask, bool enable)
        {
            try
            {
                mountingTask.InstalledLocation.MountedItem.gameObject.SetActive(enable);
            }
            catch
            {
                Debug.LogWarning("No Device is installed for this task");
            }
        }
        
        private void PreviewDevice(DeviceMountingTask mountingTask)
        {
            MountableName deviceName = mountingTask.RequestedDevice;
            MountLocation location = mountingTask.InteractionLocation;

            location.gameObject.SetActive(true);

            interactingDeviceObj = factory.Spawn(deviceName);
            Device device = interactingDeviceObj.GetComponentInChildren<Device>();
            if (device.PreConnectionMountable != MountableName.None)
            {
                // Spawn and place pre connection mountable instead. Keep the
                // original device hidden to use at the end.
                postConnectionDeviceObj = interactingDeviceObj;
                postConnectionDeviceObj.SetActive(false);
                postConnectionDeviceObj.name = $"{deviceName} - Post Connection";

                interactingDeviceObj = factory.Spawn(device.PreConnectionMountable);
                device.PreConnectionMountableInstance = interactingDeviceObj.GetComponentInChildren<Mountable>();
            }

            interactingDeviceObj.name = $"{deviceName}";
            location.PlaceMountable(interactingDeviceObj.transform, deviceName);

            location.gameObject.SetActive(false);
        }


        /// <summary>
        ///     Allows or prevents interactions with the set of <see cref="InteractableWire"/> components 
        ///     on a given device object
        /// </summary>
        /// <remarks>
        ///     If no InteractableWires are found, no action is taken
        /// </remarks>
        /// <param name="device"></param>
        /// <param name="interactable"></param>
        public static void SetDeviceConductorsInteractable(GameObject device, bool interactable)
        {
            foreach (var wire in device.GetComponentsInChildren<InteractableWire>())
            {
                string layerMaskName = interactable ? SceneInteractions.LayerDefault : SceneInteractions.LayerIgnoreRaycast;
                wire.gameObject.layer = LayerMask.NameToLayer(layerMaskName);
            }
        }

        public override void OnLoadComplete(MountingTask mountingTask)
        {
            currentDeviceTask = mountingTask as DeviceMountingTask;

            PreviewDevice(mountingTask as DeviceMountingTask);
            wireMounter.FetchComponentsFromObject(interactingDeviceObj);
            ConductorConnectionCompleted();
        }
    }
}
