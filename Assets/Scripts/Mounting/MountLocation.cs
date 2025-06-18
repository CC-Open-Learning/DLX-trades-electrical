using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    [RequireComponent(typeof(BoxCollider))]
    public class MountLocation : MonoBehaviour
    {
        [field: SerializeField] public MountingSide DefaultMountingSide { get; private set; }
        [SerializeField] private MountingSetting[] mountingSideOverrides;
        [field:SerializeField] public CinemachineVirtualCamera CameraPosition { get; private set; }    

        public GameObject MountedItem { get; private set; }
        public MeasurementMarkers Measurements { get; private set; } = null;
        public GameObject MarkerObj { get; private set; } = null;

        private readonly Dictionary<MountableName, MountingSide> overriddenSides = new();
        private Collider locationCollider;

        private void Awake()
        {
            locationCollider = GetComponent<BoxCollider>();
            Measurements = GetComponent<MeasurementMarkers>();

            foreach (MountingSetting setting in mountingSideOverrides)
            {
                overriddenSides.Add(setting.mountable, setting.side);
            }
        }

        public void Enable(bool enable)
        {
            gameObject.SetActive(enable);
        }

        public void DestroyMarker()
        {
            if (MarkerObj)
            {
                Destroy(MarkerObj);
                MarkerObj = null;
            }
        }

        public void PlaceMountable(Transform mountTransform, MountableName mountableName)
        {
            if (MountedItem)
            {
                Debug.LogWarning($"Can't place {mountableName}. {MountedItem.name} is already exists on this location.");
                return;
            }

            Vector3 colliderHalfSize = locationCollider.bounds.size / 2;
            Vector3 offsetSide = Vector3.Scale(colliderHalfSize, transform.right);
            Vector3 offsetFront = Vector3.Scale(colliderHalfSize, transform.forward);

            MountingSide mountingSide = overriddenSides.ContainsKey(mountableName) ?
                overriddenSides[mountableName] : DefaultMountingSide;
            if (mountingSide == MountingSide.Left)
            {
                offsetSide *= -1;
            }
            else if (mountingSide == MountingSide.Front)
            {
                offsetSide = Vector3.zero;
            }

            mountTransform.position = transform.position + offsetSide + offsetFront;
            mountTransform.forward = transform.forward;

            if (mountingSide == MountingSide.Left)
            {
                mountTransform.Rotate(0f, 0f, 180f);
            }

            MountedItem = mountTransform.gameObject;
        }

        public void DestroyMountable()
        {
            if (!MountedItem)
            {
                Debug.LogWarning("Trying to destroy a Mountable that has not been mounted");
                return;
            }

            Destroy(MountedItem);
            MountedItem = null;
        }

        public void SpawnOptionMarker(GameObject markerRef, MountableName mountableName)
        {
            const float OffsetBoost = 0.08f;

            BoxCollider thisCollider = transform.GetComponent<BoxCollider>();
            Vector3 colliderHalfSize = thisCollider.bounds.size / 2;

            Vector3 offsetSide = Vector3.zero;
            Vector3 offsetFront = Vector3.Scale(colliderHalfSize, transform.forward);
            MountingSide mountingSide = overriddenSides.ContainsKey(mountableName) ?
                overriddenSides[mountableName] : DefaultMountingSide;

            switch (mountingSide)
            {
                case MountingSide.Left:
                    offsetSide = Vector3.Scale(colliderHalfSize, -transform.right);
                    offsetSide += (-transform.right * OffsetBoost);
                    break;
                case MountingSide.Right:
                    offsetSide = Vector3.Scale(colliderHalfSize, transform.right);
                    offsetSide += (transform.right * OffsetBoost);
                    break;
                case MountingSide.Front:
                    offsetFront += (transform.forward * OffsetBoost);
                    break;
            }

            MarkerObj = Instantiate(markerRef, transform, true);
            MarkerObj.name = "Location Marker";
            MarkerObj.SetActive(true);
            MarkerObj.transform.position = transform.position + offsetSide + offsetFront;
        }
    }
}
