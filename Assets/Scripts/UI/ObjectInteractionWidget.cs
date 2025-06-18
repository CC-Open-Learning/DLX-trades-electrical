using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     This class is a MonoBehaviour that manages the UI and behaviour of the Edit Mounted Object UI in our
    ///     simulation.
    /// </summary>
    /// <remarks>
    ///     This class could be further consolidated with the <see cref="MenuController"/> base class which
    ///     provides initialization, open/close functionality, and fields for document root
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ObjectInteractionWidget : MonoBehaviour
    {
        public const string MoveButtonName = "BtnMove";
        public const string ReplaceButtonName = "BtnReplace";
        public const string GoBackButtonName = "BtnGoBack";

        private readonly Dictionary<Button, Action> ButtonDictionary = new();


        public enum ButtonType
        {
            Move,
            Replace,
            GoBack,
        }

        // Serialized Unity Events
        [field: SerializeField] public UnityEvent WidgetOpened { get; private set; } = new();
        [field: SerializeField] public UnityEvent WidgetClosed { get; private set; } = new();


        // Utility variables
        private VisualElement rootElement;
        private Button btnMove;
        private Button btnReplace;
        private Button btnGoBack;



        /// <summary>
        ///     Handles the MonoBehavior.Start Event to initialize UI elements
        ///     and hide the menu.
        /// </summary>
        private void Start()
        {
            InitUI();
            Hide();
        }

        public void Open()
        {
            rootElement.style.display = DisplayStyle.Flex;
            WidgetOpened?.Invoke();
        }

        public void Close()
        {
            Hide();
            ResetButtons();
            WidgetClosed?.Invoke();
        }

        public void Hide()
        {
            rootElement.style.display = DisplayStyle.None;
        }

        public void AddButton(ButtonType type, Action callback)
        {
            Button button = GetButtonFromType(type);
            button.clicked += callback;
        
            ButtonDictionary.TryAdd(button, callback);
            button.style.display = DisplayStyle.Flex;
        }

        public void SetButtonText(ButtonType type, string text)
        {
            Button button = GetButtonFromType(type);
            if (!ButtonDictionary.ContainsKey(button))
            {
                Debug.LogWarning($"Add {type} button before setting text");
                return;
            }

            Label buttonLabel = button.Q<Label>("Text");
            buttonLabel.text = text;
        }

        public void SetButtonEnabled(ButtonType type, bool enable)
        {
            Button button = GetButtonFromType(type);
            if (!ButtonDictionary.ContainsKey(button))
            {
                Debug.LogWarning($"Add {type} button before setting it enable/disable");
                return;
            }

            button.SetEnabled(enable);
        }

        private void ResetButtons()
        {
            if (ButtonDictionary.Count == 0)
            {
                return;
            }

            foreach (var (button, callback) in ButtonDictionary)
            {
                // Unsubscribe from subscribed button click events
                button.clicked -= callback;
                // Hide each button
                button.style.display = DisplayStyle.None;
                button.SetEnabled(true);
            }

            ButtonDictionary.Clear();
        }

        private Button GetButtonFromType(ButtonType type)
        {
            return type switch
            {
                ButtonType.Move => btnMove,
                ButtonType.Replace => btnReplace,
                ButtonType.GoBack => btnGoBack,
                _ => null
            };
        }

        /// <summary>
        ///     Initializes the user interface elements of the menu.
        /// </summary>
        private void InitUI()
        {
            rootElement = GetComponent<UIDocument>().rootVisualElement;

            btnMove = rootElement.Q<Button>(MoveButtonName);
            SetupButton(btnMove);
            btnReplace = rootElement.Q<Button>(ReplaceButtonName);
            SetupButton(btnReplace);
            btnGoBack = rootElement.Q<Button>(GoBackButtonName);
            SetupButton(btnGoBack);
        }

        /// <summary>
        ///     Sets up the behavior of the specified button.
        /// </summary>
        /// <remarks>
        ///     Button style configuration is applied in the accompanying "Widget.uss" style sheet
        /// </remarks>
        /// <param name="button">The button to be set up.</param>
        private void SetupButton(Button button)
        {
            // Hide each button initially
            button.style.display = DisplayStyle.None;

            button.clicked += () => Close();
        }
    }
}