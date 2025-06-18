using Cinemachine;
using UnityEngine;
using VARLab.CORECinema;

namespace VARLab.TradesElectrical
{
    public class CameraPovHandler : MonoBehaviour
    {
        private const float NoInput = 0f;

        [field: SerializeField] public CinemachineVirtualCamera DefaultCamera { get; private set; }

        public CinemachinePOV Pov { get; private set; }

        /// <summary>
        ///     Public reference to the <see cref="CinemachinePan"/> controller
        ///     which is used by all of the Task Preview cameras
        /// </summary>
        public CinemachinePan TaskCameraPanSettings => cmBrainPan;

        private MouseHandler mouseHandler = new();
        private CinemachineCameraPriorityManager camPriorityManager;
        private CinemachineBrain cmBrain;
        private CinemachinePan cmBrainPan;
        private CinemachineZoom cmBrainZoom;

        private void Awake()
        {
            cmBrain = Camera.main.GetComponent<CinemachineBrain>();
            cmBrainPan = cmBrain.GetComponent<CinemachinePan>();
            cmBrainZoom = cmBrain.GetComponent<CinemachineZoom>();
            camPriorityManager = Camera.main.GetComponent<CinemachineCameraPriorityManager>();
            Pov = DefaultCamera.GetCinemachineComponent<CinemachinePOV>();
        }
 
        private void Update()
        {
            mouseHandler.HandleMouseInteractions();
        }
        
        private void OnEnable()
        {
            mouseHandler.MouseHold += HandleCameraPanning;
            mouseHandler.ShortClick += StopCameraPanning;
            mouseHandler.LongClick += StopCameraPanning;
        }

        private void OnDisable()
        {
            mouseHandler.MouseHold -= HandleCameraPanning;
            mouseHandler.ShortClick -= StopCameraPanning;
            mouseHandler.LongClick -= StopCameraPanning;
        }
        
        /// <summary>
        ///     Defines a platform-specific camera panning sensitivity modifier.
        /// </summary>
        /// <remarks>
        ///     This is due to the fact that the WebGL player seems to nearly quadruple 
        ///     the magnitude of mouse X and Y inputs as the cursor moves, so the 
        ///     sensitivity needs to be reduced by that factor.
        /// </remarks>
        public float PlatformSensitivityModifier =>
            Application.platform == RuntimePlatform.WebGLPlayer
                ? 0.25f
                : 1f;
        [Range(0.1f, 4f)] 
        public float CameraPanSensitivity = 1f;

        /// <summary>
        ///     Used to indicate the frame that the application gains focus again.
        ///     Helps to avoid camera jitter due to strange cached mouse inputs.
        /// </summary>
        /// <remarks>
        ///     Eventually would be good to replace the use of the <see cref="Input"/> 
        ///     system in favour of the newer Input Action system. It seems the issue
        ///     that this fixes may be resolved naturally.
        /// </remarks>
        private bool focusGainedFrame = false;
        private bool isPanningEnabled = true;

        /// <summary>
        ///     Handles camera panning as the mouse is moved across the screen. Will
        ///     be triggered when the mouse is held down.
        /// </summary>
        /// <remarks>
        ///     Camera panning should be separated out into its own behaviour,
        ///     like CORE Cinema but using these camera control patterns
        /// </remarks>
        private void HandleCameraPanning()
        {
            if (!isPanningEnabled) { return; }

            // Skip this frame to avoid the camera jumping when focus is regained
            if (focusGainedFrame)
            {
                focusGainedFrame = false;
                return;
            }

            float modifier = CameraPanSensitivity * PlatformSensitivityModifier;

            Pov.m_HorizontalAxis.Value -= Input.GetAxis("Mouse X") * modifier;
            Pov.m_VerticalAxis.Value += Input.GetAxis("Mouse Y") * modifier;
        }

        
        /// <summary>
        ///     Calculate and assign a vector to the POV camera to recenter the camera.
        ///     Use <see cref="RecenterCamera(bool)" /> method to recenter the camera once the vector is set.
        ///     As an example, if we want to recenter camera on point A and our current position is point B,
        ///     use A as the endpoint and B as origin.
        /// </summary>
        /// <param name="endpoint">Vector end</param>
        /// <param name="origin">Vector start</param>
        /// Taken from PointClickNavigation package
        public void SetPovCamRecenterVector(Vector3 endpoint, Vector3 origin)
        {
            DefaultCamera.Follow.forward = endpoint - origin;
        }

        /// <summary>
        ///     Assigns the provided vector to the POV camera to recenter the camera.
        ///     Use <see cref="RecenterCamera(bool)" /> method to recenter the camera once the vector is set.
        /// </summary>
        /// <param name="forward">The forward vector used for camera recentering</param>
        public void SetPovCamRecenterVector(Vector3 forward)
        {
            DefaultCamera.Follow.forward = forward;
        }

        public void EnableRecenterCamera(bool status)
        {
            Pov.m_HorizontalRecentering.m_enabled = status;
            Pov.m_VerticalRecentering.m_enabled = status;
        }

        /// <summary>
        ///     Invoked by <see cref="UIHandler.EnableSceneInteraction" />
        /// </summary>
        /// <param name="status"></param>
        public void EnableCameraPanAndZoom(bool status)
        {
            if (!status) StopCameraPanning();
            cmBrainPan.enabled = status;
            cmBrainZoom.enabled = status;
            isPanningEnabled = status;
        }

        /// <summary>
        ///     Resets the input on the POV camera when the mouse is
        ///     no longer being dragged across the screen.
        /// </summary>
        public void StopCameraPanning()
        {
            Pov.m_HorizontalAxis.m_InputAxisValue = NoInput;
            Pov.m_VerticalAxis.m_InputAxisValue = NoInput;
        }
    }
}
