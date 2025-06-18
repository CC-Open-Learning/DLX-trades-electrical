using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VARLab.CORECinema;

namespace VARLab.TradesElectrical
{
    public class CameraController : MonoBehaviour
    {
        [field: SerializeField] public CinemachineVirtualCamera DefaultCamera { get; private set; }

        [field: SerializeField] public UnityEvent PreviewingMountable { get; private set; } = new();

        [Tooltip("Time in seconds for camera to wait before beginning fade back to default camera")]
        public float CameraWaitTime = 1f;

        [Tooltip("Time in seconds for camera to fade to black for transitions")]
        public float CameraFadeTime = 3f;

        private CinemachineCameraPriorityManager camPriorityManager;

        private CinemachineFade fade;

        private CinemachineBrain cinemachineBrain;

        public CameraPovHandler CameraPovHandler { get; private set; }

        /// <summary>
        ///     Direction vector initially stored from the "Camera Follow Target" and used when applying camera recentering 
        ///     for camera transitions (ie when returning from the breaker panel to the drywall-covered bathroom scene
        /// </summary>
        private Vector3 initialRecenterForward;

        private void Awake()
        {
            camPriorityManager = Camera.main.GetComponent<CinemachineCameraPriorityManager>();
            CameraPovHandler = gameObject.GetComponent<CameraPovHandler>();
            fade = Camera.main.GetComponent<CinemachineFade>();
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();
            initialRecenterForward = DefaultCamera.Follow.forward;
        }

        /// <summary>
        ///     Invoked by <see cref="WireMounter.Previewing" />
        /// </summary>
        public void PreviewedMountingLocations(BoxMountingTask boxMountingTask)
        {
            if (boxMountingTask.TaskPreviewCamera == null)
            {
                Debug.LogWarning($"{boxMountingTask.name} has no given location preview camera");
            }

            SwitchCamera(boxMountingTask.TaskPreviewCamera);
        }

        /// <summary>
        ///     Invoked by <see cref="BoxMounter.previewMounting" />
        /// </summary>
        public void PreviewedMounting(BoxMountingTask boxMountingTask)
        {
            // Refer to the SelectedLocation if it is not null. RequestedLocation will
            // be used when mounting a box for the first time or when moving it.
            MountLocation mountLocation = boxMountingTask.SelectedLocation ?
                boxMountingTask.SelectedLocation :
                boxMountingTask.RequestedLocation;
            if (mountLocation.CameraPosition == null)
            {
                Debug.LogWarning($"{mountLocation.name} location has no given preview camera");
            }

            SwitchCamera(mountLocation.CameraPosition);

            LookAtWithLock(mountLocation.transform);
        }

        /// <summary>
        ///     Invoked by <see cref="CableMounter.previewingCableSelection" />
        /// </summary>
        public void PreviewedCable(CableMountingTask mountingTask)
        {
            SwitchCamera(mountingTask.TaskPreviewCamera);
            LookAtWithLock(mountingTask.PreviewingMountable.gameObject.transform);
        }

        public void PreviewedTerminateBox(WireMountingTask terminateCableTask)
        {
            // Triggers the camera fade action and prevents a camera jump cut beforehand
            if (terminateCableTask.TaskName == Task.ConnectBreaker)
            {
                PreviewedPanelBox(terminateCableTask);
                return;
            }

            // Custom behaviour above is necessary for Breaker tasks, all others use the default workflow below
            SwitchCamera(terminateCableTask.TaskPreviewCamera);
            LookAtWithLock(terminateCableTask.TaskPreviewCamera.transform);
        }

        public void PreviewedPanelBox(MountingTask task)
        {
            StartCoroutine(CameraFadeTransitionCoroutine(task.TaskPreviewCamera));
        }

        /// <summary>
        ///     Invoked by <see cref="BoxMounter.LookAtMountedObject" />
        /// </summary>
        public void LookAtWithoutLock(Transform target)
        {
            LookAtWithLock(target);
            StartCoroutine(nameof(WaitUntilRecenterFinalizes));
        }

        public void SwitchCamera(CinemachineVirtualCamera camera)
        {
            camPriorityManager.ActivateCamera(camera);
        }

        public void SwitchDefaultCamera()
        {
            CameraPovHandler.EnableRecenterCamera(false);
            SwitchCamera(DefaultCamera);
        }

        public void SwitchDefaultCamera(MountingTask task)
        {
            if (task && task.TaskName == Task.ConnectBreaker) 
            {
                CameraPovHandler.EnableRecenterCamera(false);
                SwitchDefaultCameraWithFade();
                return;
            }

            SwitchDefaultCamera();
        }

        public void SwitchDefaultCameraWithFade()
        {
            StartCoroutine(CameraFadeTransitionCoroutine(DefaultCamera, CameraWaitTime));
        }

        private void LookAtWithLock(Transform target)
        {
            CameraPovHandler.SetPovCamRecenterVector(target.position, DefaultCamera.transform.position);
            CameraPovHandler.EnableRecenterCamera(true);
        }

        /// <summary>
        ///     Performs a camera fade transition where the screen fades to black, the camera
        ///     is instantly changed, and then the screen fades back to normal at the new camera position.
        ///     
        ///     This allows for smooth camera transitions between disconnected areas in the scene
        /// </summary>
        /// <param name="vcam"></param>
        private IEnumerator CameraFadeTransitionCoroutine(CinemachineVirtualCamera vcam, float initialWait = 0f)
        {
            if (vcam.Equals(cinemachineBrain.ActiveVirtualCamera))
            {
                // Ignore fade transition if we are already at the target camera
                yield break;
            }

            yield return new WaitForSeconds(initialWait);

            // Fade out and wait
            FadeOutTrigger(CameraFadeTime);
            yield return new WaitForSeconds(CameraFadeTime);

            SwitchCamera(vcam);

            // Reset default camera while it is not active using the cached "initialRecenterForward" vector
            CameraPovHandler.SetPovCamRecenterVector(initialRecenterForward);
            CameraPovHandler.EnableRecenterCamera(!vcam.Equals(DefaultCamera));

            // Fade back in
            FadeInTrigger(CameraFadeTime);

        }

        /// <summary>
        ///     Coroutine that waits until the camera's rotation closely matches the follow target's rotation
        ///     (until recentering finalizes)
        /// </summary>
        /// <returns>An IEnumerator for coroutine control</returns>
        private IEnumerator WaitUntilRecenterFinalizes()
        {
            // Define a threshold angle to determine when the camera's rotation is sufficiently aligned
            const float threshold = 1.0f;

            // Wait until the camera's rotation closely matches the follow target's rotation
            yield return new WaitUntil(() =>
            {
                Quaternion followTargetRotation = DefaultCamera.Follow.transform.rotation;
                Quaternion mainCameraRotation = Camera.main.transform.rotation;

                // Calculate the angle difference to assess alignment
                float angle = Quaternion.Angle(followTargetRotation, mainCameraRotation);

                // Check if the angle difference is within an acceptable range, indicating stability
                return angle < threshold;
            });

            // Once aligned, disable the camera recentering to allow free look
            CameraPovHandler.EnableRecenterCamera(false);
        }
        
        public void FadeInTrigger(float time)
        {
            fade.FadeIn(time);
        }
        
        public void FadeOutTrigger(float time)
        {
            fade.FadeOut(time);
        }
        
    }
}