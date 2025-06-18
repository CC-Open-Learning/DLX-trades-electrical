using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace VARLab.TradesElectrical
{
    public class BoxMounter : Mounter, IReplaceable, IMovable
    {
        [SerializeField] private GameObject locationMarker;
        [SerializeField] private EquipmentFactory factory;

        [field: SerializeField]
        public UnityEvent<BoxMountingTask> PreviewingMountLocations { get; private set; } = new();

        [field: SerializeField]
        public UnityEvent<ToastMessageType, string, float> OpenToastMessage { get; private set; } = new();

        [field: SerializeField] 
        public UnityEvent<Mountable> BoxMounted { get; private set; } = new();
        
        [field: SerializeField] 
        public UnityEvent<Mountable> BoxDestroyed { get; private set; } = new();

        [field: SerializeField] public UnityEvent<Transform> LookAtMountedObject = new();
        
        public UnityEvent AskForConfirmation = new();

        private const string LocationSelectionToastPrefix = "Select a location to preview the ";

        [Header("Events")]
        [SerializeField] private UnityEvent<BoxMountingTask> previewMounting = new();
        [SerializeField] private UnityEvent<MountingTask> mountingFinalized = new();
        [SerializeField] private UnityEvent<MountLocation> playMountingAnimation = new();
        [SerializeField] private UnityEvent<MeasurementMarkers, Collider> showMeasurements = new();
        [SerializeField] private UnityEvent hideMeasurements = new();

        private MountLocation[] mountLocations;
        private GameObject objectToMount;
        private BoxMountingTask previewingMountingTask;
        public TaskHandler TaskHandler;

        private void Awake()
        {
            mountLocations =
                FindObjectsByType<MountLocation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            HideAllMountLocations(shouldDestroy: true);
        }

        public void PreviewMountLocations(MountingTask mountingTask)
        {
            if (mountingTask is not BoxMountingTask boxMountingTask)
            {
                Debug.LogError($"Trying to preview the mount locations for an invalid task");
                return;
            }

            previewingMountingTask = boxMountingTask;
            foreach (MountLocation location in boxMountingTask.LocationOptions)
            {
                if(!TaskHandler.IsGCFeedbackInvoked)
                {
                    location.Enable(true);
                }
                else
                {
                    location.Enable(location != boxMountingTask.SelectedLocation);
                }

                MountableName requestedMountable = boxMountingTask.RequestedMountableName;
                if (boxMountingTask.RequestedMountableName != MountableName.None && !location.MarkerObj)
                {
                    location.SpawnOptionMarker(locationMarker, requestedMountable);
                }
            }
            
            PreviewingMountLocations?.Invoke(boxMountingTask);
            OpenToastMessage?.Invoke(ToastMessageType.Info,
                LocationSelectionToastPrefix + boxMountingTask.TaskName.ToDescription(), ToastMessageUI.NoTimeout);
        }

        /// <summary>
        /// Invoked from <see cref="BoxMountingSelectionMenuController.Cancelled"/>
        /// Invoked from <see cref="BoxMountingSelectionMenuController.MenuOpened"/>
        /// Invoked from <see cref="ConfirmSelectionMenuController.InstallConfirmButtonPressed"/>
        /// </summary>
        public void HideAllMountLocations(bool shouldDestroy)
        {
            foreach (MountLocation location in mountLocations)
            {
                location.Enable(false);

                if (shouldDestroy)
                {
                    location.DestroyMarker();
                }
            }
        }

        public override void OnMountPreview(MountingTask mountingTask)
        {
            BoxMountingTask boxMountingTask = mountingTask as BoxMountingTask;
            if (!boxMountingTask)
            {
                Debug.LogError("Trying to preview an invalid box");
                return;
            }

            PreviewMountedObject(boxMountingTask);
        }

        public override void OnMountConfirm()
        {
            Mountable mountedItem = objectToMount.GetComponentInChildren<Mountable>();
            previewingMountingTask.SelectedMountable = mountedItem;
            previewingMountingTask.SelectedLocation = previewingMountingTask.RequestedLocation;
            BoxMounted?.Invoke(mountedItem);
            
            mountingFinalized?.Invoke(previewingMountingTask);
            playMountingAnimation?.Invoke(previewingMountingTask.SelectedLocation);
            hideMeasurements?.Invoke();
            HideAllMountLocations(shouldDestroy: true);
            previewingMountingTask.RequestedLocation = null;
            previewingMountingTask = null;
        }

        public override void OnMountRedo()
        {
            hideMeasurements?.Invoke();
            HideAllMountLocations(shouldDestroy: false);

            previewingMountingTask.RequestedLocation.DestroyMountable();
            previewingMountingTask.RequestedLocation = null;

            PreviewMountLocations(previewingMountingTask);
        }

        public void OnMountableReplace(MountingTask mountingTask)
        {
            BoxMountingTask task = (BoxMountingTask)mountingTask;
            if (!task)
            {
                Debug.LogError("Trying to swap an invalid box!");
                return;
            }

            if (mountingTask.RequestedMountableName != MountableName.None)
            {
                AutomaticMountablePlace(task);
                mountingFinalized?.Invoke(task);
                playMountingAnimation?.Invoke(task.SelectedLocation);

                // When editing in the scene, the camera is already looking at the
                // previously mounted object, so there's no need to redirect the camera view.
                if (!IsEditingViaScene)
                {
                    LookAtMountedObject?.Invoke(task.SelectedMountable.gameObject.transform);
                }
            }
        }

        public void OnMountableMove(MountingTask mountingTask)
        {
            if (mountingTask is not BoxMountingTask boxMountingTask)
            {
                Debug.LogError("Trying to move a box using an invalid task type");
                return;
            }

            Mountable currentItem = mountingTask.SelectedMountable;
            boxMountingTask.SelectedLocation = null;
            BoxDestroyed?.Invoke(currentItem);
            Transform currentlyMountedBoxTransform = currentItem.transform.parent;
            Destroy(currentlyMountedBoxTransform.gameObject);
            PreviewMountLocations(mountingTask);
        }

        /// <summary>
        /// Invoked when a learner clicks on a location option (yellow orb)
        /// </summary>
        /// <param name="clickedLocation">Selected location</param>
        public void OnMountLocationClick(MountLocation clickedLocation)
        {
            previewingMountingTask.RequestedLocation = clickedLocation;
            OnMountPreview(previewingMountingTask);
        }

        private void PreviewMountedObject(BoxMountingTask boxMountingTask)
        {
            // Refer to the SelectedLocation if it is not null. RequestedLocation will
            // be used when mounting a box for the first time or when moving it.
            MountLocation location = boxMountingTask.SelectedLocation ?
                boxMountingTask.SelectedLocation :
                boxMountingTask.RequestedLocation;

            if (!location.MountedItem) // Previewing for the first time (when mounting)
            {
                PlaceMountable(boxMountingTask.RequestedMountableName, location);
                HideAllMountLocations(shouldDestroy: false);
                AskForConfirmation?.Invoke();
            }

            previewMounting?.Invoke(boxMountingTask);
            ShowMeasurementsAfterColliderEnables(location);
        }

        private void PlaceMountable(MountableName objName, MountLocation location)
        {
            objectToMount = factory.Spawn(objName);
            objectToMount.name = $"{objName} - {location.name}";
            objectToMount.SetActive(true);

            location.PlaceMountable(objectToMount.transform, objName);

            // Merge meshes if applicable
            foreach (MeshMerger merger in objectToMount.GetComponentsInChildren<MeshMerger>(true))
            {
                merger.MergeMeshes();
            }
        }
        
        private void DestroyMountableInTask(BoxMountingTask mountingTask)
        {
            Mountable oldMountable = mountingTask.SelectedMountable;
            BoxDestroyed?.Invoke(oldMountable);

            mountingTask.SelectedLocation.DestroyMountable();
            mountingTask.SelectedMountable = null;
        }
        
        /// <summary>
        /// Measurement system is using mounted object's collider bounds to draw measurement lines.
        /// To get bounds, the collider should be active and enabled. However, colliders are not
        /// immediately become active once the host GameObject become active. Therefore, we are
        /// giving a bit of time to enable colliders to show measurements.
        /// </summary>
        private void ShowMeasurementsAfterColliderEnables(MountLocation mountLocation)
        {
            StartCoroutine(ShowMeasurementsAfterDelayCoroutine(mountLocation));
        }

        private IEnumerator ShowMeasurementsAfterDelayCoroutine(MountLocation mountLocation)
        {
            yield return new WaitForSeconds(0.1f);
            showMeasurements?.Invoke(mountLocation.Measurements,
                                     mountLocation.MountedItem.GetComponentInChildren<Collider>());
        }
        
        /// <summary>
        /// Automatically places mountable without any user input
        /// Invoked by <see cref="TaskSkipper.placeMountables"/>
        /// </summary>
        /// <param name="objName"></param>
        /// <param name="location"></param>
        public void AutomaticMountablePlace(MountingTask mountingTask)
        {
            BoxMountingTask task = (BoxMountingTask)mountingTask;
            if (task.SelectedMountable != null)
            {
                DestroyMountableInTask(task);
            }
            MountLocation mountLoc = task.SelectedLocation;
            mountLoc.Enable(true);
            mountLoc.SpawnOptionMarker(locationMarker, task.RequestedMountableName);

            PlaceMountable(task.RequestedMountableName, task.SelectedLocation);
            task.SelectedMountable = objectToMount.GetComponentInChildren<Mountable>();
            mountLoc.DestroyMarker();
            BoxMounted?.Invoke(task.SelectedMountable);
        }

        public override void OnLoadComplete(MountingTask mountingTask)
        {
            if (mountingTask is not BoxMountingTask task)
            {
                Debug.LogError("Trying to load an invalid box task");
                return;
            }

            if (task.SelectedMountableName != MountableName.None && task.SelectedLocation != null)
            {
                task.RequestedMountableName = task.SelectedMountableName;
                AutomaticMountablePlace(task);
                mountingFinalized?.Invoke(task);
            }
        }
    }
}
