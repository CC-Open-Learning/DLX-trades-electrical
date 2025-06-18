using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public enum ToastMessageType
    {
        Error,
        Success,
        Info
    }

    [RequireComponent(typeof(UIDocument))]
    public class ToastMessageUI : MonoBehaviour
    {
        /// <summary> Used as a flag to indicate that a toast message will never time out </summary>
        public static readonly int NoTimeout = -1;

        private Label msgLabel;
        private VisualElement rootElement;

        private VisualElement infoIcon;
        private VisualElement successIcon;
        private VisualElement errorIcon;
        private VisualElement messageContainer;
        private VisualElement bodyMain;

        /// <summary> Reference to the currently running hide coroutine, if it exists</summary>
        private IEnumerator currentCoroutine;

        private readonly Color Red = new(0.69f, 0.0f, 0.125f, 1.0f);
        private readonly Color Green = new(0.29f, 0.75f, 0.49f, 1.0f);
        private readonly Color Blue = new(0.02f, 0.44f, 0.87f, 1.0f);

        private void Start()
        {
            rootElement = GetComponent<UIDocument>().rootVisualElement;
            msgLabel = rootElement.Q<Label>("Message");
            infoIcon = rootElement.Q<VisualElement>("InfoIcon");
            errorIcon = rootElement.Q<VisualElement>("ErrorIcon");
            successIcon = rootElement.Q<VisualElement>("SuccessIcon");
            messageContainer = rootElement.Q<VisualElement>("Container");
            bodyMain = rootElement.Q<VisualElement>("BodyMain");
            HideVisualComponents();
        }

        /// <summary>
        ///     Display a toast message with or without a timeout
        /// </summary>
        /// <param name="type">Toast message type</param>
        /// <param name="message">Message text</param>
        /// <param name="timeout">
        ///     Timeout value in seconds. Any negative value will not auto
        ///     hide the toast. <see cref="Hide" /> must be called to hide in this case.
        /// </param>
        public void Show(ToastMessageType type, string message, float timeout = 5f)
        {
            StopCurrentCoroutine();
            Display(type, message, timeout);
        }

        /// <summary>
        ///     Hide the toast message
        /// </summary>
        public void Hide()
        {
            StopCurrentCoroutine();
            HideVisualComponents();
        }

        /// <summary>
        ///     Displays the toast message with the specified type and message.
        ///     If a timeout is provided, the message will automatically hide after the specified duration.
        ///     <param name="msgType">The type of the toast message (e.g., Error, Success, Info).</param>
        ///     <param name="message">The message content to display.</param>
        ///     <param name="timeout">The duration to wait before hiding the message.</param>
        /// </summary>
        private void Display(ToastMessageType msgType, string message, float timeout)
        {
            if (timeout > 0)
            {
                currentCoroutine = HideAfterTimeout(timeout);
                StartCoroutine(currentCoroutine);
            }

            // Ensure all previous toast content such as the icon is hidden before enabling
            // the context-specific information
            HideVisualComponents();

            msgLabel.text = message;
            switch (msgType)
            {
                case ToastMessageType.Error:
                    bodyMain.style.backgroundColor = Red;
                    errorIcon.style.display = DisplayStyle.Flex;
                    break;
                case ToastMessageType.Success:
                    bodyMain.style.backgroundColor = Green;
                    successIcon.style.display = DisplayStyle.Flex;
                    break;
                case ToastMessageType.Info:
                    bodyMain.style.backgroundColor = Blue;
                    infoIcon.style.display = DisplayStyle.Flex;
                    break;
                default:
                    return;
            }

            rootElement.style.display = DisplayStyle.Flex;
            msgLabel.style.display = DisplayStyle.Flex;
            messageContainer.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        ///     Hides all visual components of the toast message
        /// </summary>
        private void HideVisualComponents()
        {
            //set all icons to not display
            infoIcon.style.display = DisplayStyle.None;
            successIcon.style.display = DisplayStyle.None;
            errorIcon.style.display = DisplayStyle.None;

            //set text to not display
            msgLabel.style.display = DisplayStyle.None;

            //set container to not display
            rootElement.style.display = DisplayStyle.None;
            messageContainer.style.display = DisplayStyle.None;
        }

        /// <summary>
        ///     Coroutine that waits for a specified timeout then hides the toast message
        /// </summary>
        /// <param name="timeout">The duration to wait before hiding the toast message</param>
        private IEnumerator HideAfterTimeout(float timeout)
        {
            yield return new WaitForSeconds(timeout);
            HideVisualComponents();
            currentCoroutine = null;
        }

        /// <summary>
        ///     Stops the current hide coroutine if it exists
        /// </summary>
        private void StopCurrentCoroutine()
        {
            if (currentCoroutine == null) return;

            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
    }
}
