using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VARLab.TradesElectrical
{
    public class SceneInteractions : MonoBehaviour
    {
        /// <value> Default Unity object layer name </summary>
        public const string LayerDefault = "Default";

        /// <value> Layer name for objects that should ignore raycasts </value>
        public const string LayerIgnoreRaycast = "Ignore Raycast";


        [field: SerializeField] public UnityEvent<MountLocation> ClickOnMountLocation { get; private set; }
        [field: SerializeField] public UnityEvent<Mountable> ClickOnMountable { get; private set; } = new();
        [field: SerializeField] public UnityEvent<InteractableWire> ClickOnWire { get; private set; } = new();

        [field: SerializeField]
        public UnityEvent<WireTerminationOption> ClickOnTerminationOption { get; private set; } = new();

        private const int LeftMouseButton = 0;

        private Camera mainCam;

        // This flag is used to avoid unintentional mouse clicks on location markers when panning
        private Transform currentLocationMarker;

        // This flag is used to avoid unintentional mouse clicks on mounted objects when panning
        private Mountable currentMountable;

        public HashSet<Collider> boxColliders { get; private set; }= new HashSet<Collider>();
        private List<Collider> supplyCableColliders = new List<Collider>();
        public HashSet<Collider> supplyCablesCollidersToIgnore = new HashSet<Collider>();
        private bool isPanning = false;
        
        private void Start()
        {
            mainCam = Camera.main;
            FetchSupplyCableColliders();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(LeftMouseButton))
            {
                bool clickTakenOnUI = EventSystem.current.currentSelectedGameObject;
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out RaycastHit hit) || clickTakenOnUI)
                {
                    // Flagging GameObjects captured on mouse button down to make sure that the
                    // mouse pointer is still on the same object when mouse button up. This will avoid
                    // unintentional clicks when panning.
                    isPanning = true;
                    return;
                }
            }
            else if (Input.GetMouseButtonUp(LeftMouseButton))
            {
                bool clickTakenOnUI = EventSystem.current.currentSelectedGameObject;
                if (!clickTakenOnUI && !isPanning)
                {
                    ProcessMouseClickUp(Input.mousePosition);
                }

                // Reset flags that used to avoid unintentional mouse clicks
                currentLocationMarker = null;
                currentMountable = null;
                isPanning = false;
            }
        }

        public void ProcessMouseClickUp(Vector3 mousePosition)
        {
            Ray ray = mainCam.ScreenPointToRay(mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit))
            {
                return;
            }
            if(hit.transform.TryGetComponent(out Mountable mountable))
            {
                currentMountable = mountable;
            }
            currentLocationMarker = hit.transform;
            HandleInteraction(hit);
            
        }
        
        /// <summary>
        ///     Enables or disables the colliders for box mountables.
        ///     Invoked by
        ///     <see cref="WireMounter.SetEnableOtherInteractables " />
        ///     <see cref="EditMountedObject.hideEditDialog" />
        ///     <see cref="BoxMounter.PreviewingMountLocations" />
        /// </summary>
        /// <param name="enable">A boolean indicating whether to enable/disable the colliders.</param>
        public void SetEnableBoxMountablesColliders(bool enable)
        {
            foreach (var box in boxColliders)
            {
                box.gameObject.layer = LayerMask.NameToLayer(enable ? LayerDefault : LayerIgnoreRaycast);
            }
        }

        /// <summary>
        ///     Enables or disables the colliders for cable mountables.
        ///     Invoked by
        ///     <see cref="WireMounter.SetEnableOtherInteractables " />
        /// </summary>
        /// <param name="enable">A boolean indicating whether to enable/disable the colliders.</param>
        public void SetEnableCableMountablesColliders(bool enable)
        {
            foreach (var collider in supplyCableColliders)
            {
                if(supplyCablesCollidersToIgnore.Contains(collider)){continue;}
                collider.gameObject.layer = LayerMask.NameToLayer(enable ? LayerDefault : LayerIgnoreRaycast);
            }
        }

        /// <summary>
        ///     Adds a BoxCollider to the HashSet collection
        /// </summary>
        /// <param name="newMountable">The mountable object that contains the BoxCollider component</param>
        public void AddBoxMountableCollider(Mountable newMountable)
        {
            BoxCollider boxCollider = newMountable.GetComponent<BoxCollider>();

            if (boxCollider)
            {
                boxColliders.Add(boxCollider);
            }
        }

        /// <summary>
        ///     Removes a BoxCollider from the HashSet collection
        /// </summary>
        /// <param name="mountable">The mountable object that contains the BoxCollider component</param>
        public void RemoveBoxMountableCollider(Mountable mountable)
        {
            BoxCollider boxCollider = mountable.GetComponent<BoxCollider>();

            if (boxCollider)
            {
                boxColliders.Remove(boxCollider);
            }
        }

        /// <summary>
        ///     Fetches all supply cable colliders from the active <see cref="CableMountingTask" /> objects.
        /// </summary>
        private void FetchSupplyCableColliders()
        {
            CableMountingTask[] cableMountingTasks =
                FindObjectsByType<CableMountingTask>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            MeshCollider collider;

            foreach (CableMountingTask cableTask in cableMountingTasks)
            {
                foreach (CableMountingTask.CableMountableContainer cable in cableTask.MountableOptions)
                {
                    if (cable.UnsecureCable.gameObject.TryGetComponent<MeshCollider>(out collider))
                    {
                        supplyCableColliders.Add(collider);
                    }
                    if (cable.SecuredCables.gameObject.TryGetComponent<MeshCollider>(out collider))
                    {
                        supplyCableColliders.Add(collider);
                    }
                }
            }
        }

        /// <summary>
        ///     Handles interactions in the Scene by detecting clicks on various interactable objects
        /// </summary>
        private void HandleInteraction(RaycastHit hit)
        {
            if (hit.transform.parent.TryGetComponent(out MountLocation mountLocation) &&
                hit.transform == currentLocationMarker)
            {
                // Clicked on a mount location option (Yellow orb)
                ClickOnMountLocation?.Invoke(mountLocation);
            }
            else if (hit.transform.TryGetComponent(out InteractableWire wire))
            {
                // Clicked on a wire/conductor in the scene
                ClickOnWire?.Invoke(wire);
            }
            else if (hit.transform.TryGetComponent(out Mountable mountable) &&
                     mountable == currentMountable)
            {
                // Clicked on a mountable in the scene
                ClickOnMountable?.Invoke(mountable);
            }
            else if (hit.transform.TryGetComponent(out WireTerminationOption terminationOption))
            {
                // Clicked on a wire termination option in a box (small yellow orb)
                ClickOnTerminationOption?.Invoke(terminationOption);
            }
        }
    }
}