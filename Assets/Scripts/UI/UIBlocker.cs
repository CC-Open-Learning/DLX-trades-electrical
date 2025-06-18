using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     UI controller which handles blocking mouse behaviour to scene elements when
    ///     a UI document is displayed
    /// </summary>
    public class UIBlocker : MonoBehaviour
    {
        public CameraPovHandler CameraMovement;

        private void Start()
        {
            // Removes need of using intialized menu for all UI elements 
            InitializeMouseEvents();
        }

        /// <summary>
        ///     Registers mouse-enter and mouse-leave callbacks to block clicks through the UI
        /// </summary>
        private void InitializeMouseEvents()
        {
            foreach (var document in GetComponentsInChildren<UIDocument>())
            {
                RegisterMouseEnterCallback(document.rootVisualElement);
                RegisterMouseLeaveCallback(document.rootVisualElement);
            }
        }

        /// <summary>
        ///     Enable or disable camera interactions
        /// </summary>
        /// <param name="enabled"></param>
        public void SetCameraMovementEnabled(bool enabled)
        {
            CameraMovement.EnableCameraPanAndZoom(enabled);
        }

        /// <summary>
        /// Method that registers the MouseEnterCallback on the MouseEnter UI event.
        /// </summary>
        public void RegisterMouseEnterCallback(VisualElement elem)
        {
            // Registers a callback on the MouseEnterEvent which will call MouseEnterCallback
            elem.RegisterCallback<MouseEnterEvent, VisualElement>(HandleMouseEnter, elem);
        }

        /// <summary>
        /// Method that registers the MouseLeaveCallback on the MouseLeave UI event.
        /// </summary>
        public void RegisterMouseLeaveCallback(VisualElement elem)
        {
            // Registers a callback on the MouseLeaveEvent which will call MouseLeaveCallback
            elem.RegisterCallback<MouseLeaveEvent, VisualElement>(HandleMouseLeave, elem);
        }

        /// <summary>
        ///     Callback to be called when the Mouse enters the panel.
        /// </summary>
        private void HandleMouseEnter(MouseEnterEvent evt, VisualElement elem)
        {
            // check if the target of the event is the background element (because UI events "bubble" through the UI tree, so it could be an event for a different element).
            if (evt.target == elem)
            {
                SetCameraMovementEnabled(false);
            }
        }

        /// <summary>
        ///     Callback to be called when the Mouse leaves the panel.
        /// </summary>
        private void HandleMouseLeave(MouseLeaveEvent evt, VisualElement elem)
        {
            // check if the target of the event is the background element (because UI events "bubble" through the UI tree, so it could be an event for a different element).
            if (evt.target == elem)
            {
                SetCameraMovementEnabled(true);
            }
        }
    }
}
