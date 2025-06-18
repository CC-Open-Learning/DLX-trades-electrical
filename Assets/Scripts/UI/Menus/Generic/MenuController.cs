using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{

    [RequireComponent(typeof(UIDocument))]
    public class MenuController : MonoBehaviour
    {
        public UIDocument Document;
        public VisualElement Root;
        public bool OpenOnStart = false;

        /// <summary>
        ///     The close button in the top-right of the window. 
        ///     Acts as the 'cancel' button in instances where it exists
        /// </summary>
        protected Button closeButton;

        [Header("Menu Events")]

        /// <summary> Invoked when the window is opened </summary>
        /// <remarks> Existing menu controllers may be using 'menuOpened' events</remarks>
        [FormerlySerializedAs("menuOpened")]
        [FormerlySerializedAs("<MenuOpened>k__BackingField")]
        public UnityEvent Opened = new();

        /// <summary> Invoked when the window is closed </summary>
        /// <remarks> Existing menu controllers may be using 'menuClosed' events</remarks>
        [FormerlySerializedAs("menuClosed")]
        [FormerlySerializedAs("<MenuClosed>k__BackingField")]
        public UnityEvent Closed = new();


        [Header("Navigation")]
        /// <summary> Invoked when the window is closed via the "Close" button</summary>
        public UnityEvent CloseButtonPressed = new();

        /// <summary> Indicates the current state of the menu </summary>
        public virtual bool IsOpen => Root != null && Root.style.display == DisplayStyle.Flex;


        public virtual void OnValidate()
        {
            if (!Document)
            {
                Document = GetComponent<UIDocument>();
            }
        }

        public virtual void Start()
        {
            if (!Document)
            {
                Document = GetComponent<UIDocument>();
            }

            Root = Document.rootVisualElement;
            Initialize();

            // Set display based on initial IsOpen property
            Display(OpenOnStart);
        }

        /// <summary>
        ///      Configures visual elements and their interactions
        /// </summary>
        public virtual void Initialize()
        {
            closeButton = Root.Q<Button>(UIHelper.CloseButtonId);
            if (closeButton != null)
            {
                closeButton.clicked += () =>
                {
                    CloseButtonPressed?.Invoke();
                    Close();
                };
            }
        }

        /// <summary>
        ///     Sets the current display state according to the <paramref name="enabled"/> parameter
        /// </summary>
        /// <param name="enabled">Indicates whether or not the window should be shown</param>
        public virtual void Display(bool enabled)
        {
            if (enabled) { Open(); }
            else { Close(); }
        }

        /// <summary>
        ///     Toggles the current state of display
        /// </summary>
        public virtual void Toggle() => Display(!IsOpen);

        public virtual void Open()
        {
            Root.style.display = DisplayStyle.Flex;
            Opened?.Invoke();
        }

        public virtual void Close()
        {
            Root.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }
    }
}
