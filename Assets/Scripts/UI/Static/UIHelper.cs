using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public static class UIHelper
    {
        // Element Identifiers

        /// <summary> Windows should use this label to identify their top-level VisualElement container </summary>
        public const string WindowRootId = "Window";

        /// <summary> Windows should use this label to indentify the content body of the window </summary>
        public const string ContentRootId = "Content";

        /// <summary> Windows should use this label to identify the "Close" button in their header bar </summary>
        public const string CloseButtonId = "BtnClose";

        /// <summary> Windows should use this label to identify the "Back" button in their navigation area </summary>
        public const string BackButtonId = "BtnBack";
        
        /// <summary> Windows should use this label to identify the "Confirm" button in their dialogs </summary>
        public const string ConfirmButtonId = "BtnConfirm";

        /// <summary> Windows should use this label to identify the "Cancel" button in their dialogs </summary>
        public const string CancelButtonId = "BtnCancel";

        /// <summary> Windows should use this label to identify the container holding navigation buttons </summary>
        public const string NavigationContainerId = "BackBtnContainer";

        // Status Labels for Task Management

        public const string StatusNotAvailable = "NOT AVAILABLE";
        public const string StatusInProgress = "IN PROGRESS";
        public const string StatusComplete = "COMPLETE";
        public const string StatusAvailable = "AVAILABLE";

        // Class Selector State Modifiers

        /// <summary> 
        ///     Class selector used to indicate that a window should be made visible, 
        ///     with a fade-in animation defined in relevant .uss file 
        /// </summary>
        public const string ClassSelectorVisible = "visible";

        /// <summary> 
        ///     Class selector used to indicate that a window should be hidden, 
        ///     with a fade-out animation defined in relevant .uss file 
        /// </summary>
        public const string ClassSelectorHidden = "hidden";

        /// <summary> Class selector used to identify the tab navigation buttons </summary>
        public const string ClassTabButton = "tab-button";

        /// <summary> Class selector used to indicate that a tab button is currently selected </summary>
        public const string ClassTabButtonSelected = ClassTabButton + ClassSelectedSuffix;

        /// <summary> Class selector used to identify the tab container contents, related to a specific Tab Button</summary>
        public const string ClassTabContainer = "tab-container";

        /// <summary> Class selector used to identify individual task elements within the tab view </summary>
        public const string ClassTaskOption = "task-button";

        /// <summary> Class selector used to indicate that a task option is currently mounted </summary>
        public const string ClassTaskOptionSelected = ClassTaskOption + ClassSelectedSuffix;

        /// <summary> Class selector used to indicate that a task is available to be started </summary>
        public const string ClassTaskStatusAvailable = ClassTaskStatusPrefix + ClassAvailableSuffix;

        /// <summary> Class selector used to indicate that a task is not available yet </summary>
        public const string ClassTaskStatusUnavailable = ClassTaskStatusPrefix + ClassUnavailableSuffix;

        /// <summary> Class selector used to indicate that a task is currently in progress</summary>
        public const string ClassTaskStatusInProgress = ClassTaskStatusPrefix + ClassInProgressSuffix;

        /// <summary> Class selector used to indicate that a task has been completed</summary>
        public const string ClassTaskStatusComplete = ClassTaskStatusPrefix + ClassCompleteSuffix;

        /// <summary> Class selector used to describe a status label overlay in the top-right corner of a task button </summary>
        public const string ClassStatusLabel = "status-label";

        /// <summary> Class selector used to indicate that a wrong answer is currently selected in a knowledge check quiz </summary>
        public const string ClassTaskOptionWrong = ClassTaskOption + ClassWrongAnswerSuffix;

        /// <summary>
        ///     Used as a prefix to concatenate with another suffix to indicate
        ///     the current status of a task
        /// </summary>
        public const string ClassTaskStatusPrefix = "task-status";

        /// <summary>
        ///     Used as a class selector suffix to indicate elements in that
        ///     class should be treated as "Selected"
        /// </summary>
        public const string ClassSelectedSuffix = "__selected";

        /// <summary>
        ///     Used as a class selector suffix to indicate elements in that
        ///     class should be treated as "Wrong Answer"
        /// </summary>
        public const string ClassWrongAnswerSuffix = "__wrong";

        /// <summary>
        ///     Used as class selectors to indicate elements in that class
        ///     have a status of "Available"
        /// </summary>
        private const string ClassAvailableSuffix = "__available";

        /// <summary>
        ///     Used as class selectors to indicate elements in that class
        ///     have a status of "Unavailable"
        /// </summary>
        private const string ClassUnavailableSuffix = "__unavailable";

        /// <summary>
        ///     Used as class selectors to indicate elements in that class
        ///     have a status of "In Progress"
        /// </summary>
        private const string ClassInProgressSuffix = "__in-progress";

        /// <summary>
        ///     Used as class selectors to indicate elements in that class
        ///     have a status of "Complete"
        /// </summary>
        private const string ClassCompleteSuffix = "__complete";


        /// <summary>
        ///     Returns the Description attribute of an enum value as string
        /// </summary>
        /// <typeparam name="T">Any enum type</typeparam>
        /// <param name="value">Enum value to convert</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Throws an exception if T is not an enum</exception>
        public static string ToDescription<T>(this T value)
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type!");
            }

            FieldInfo field = value.GetType().GetField(value.ToString());
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute != null ? attribute.Description : value.ToString();
        }

        /// <summary>
        ///     Iterates over a list of <paramref name="elements" /> and returns the first element
        ///     with a <see cref="VisualElement.viewDataKey" /> field matching <paramref name="data" />
        /// </summary>
        /// <param name="elements">List of elements to search</param>
        /// <param name="data">Comparison string</param>
        /// <returns>The visual element with corresponding view data, or null</returns>
        public static VisualElement FindElementByViewData(this List<VisualElement> elements, string data)
        {
            return elements.Find(element => element.viewDataKey.Equals(data));
        }

        /// <summary>
        ///     Swaps the functionality of the specified button by replacing the old functionality with the new one using
        ///     delegates.
        /// </summary>
        /// <param name="root">The root element of the container to fetch the button</param>
        /// <param name="buttonName">The name of the button</param>
        /// <param name="previous">The old functionality to be removed from the button's click event.</param>
        /// <param name="current">The new functionality to be added to the button's click event.</param>
        public static void SwapButtonFunctionality(VisualElement root, string buttonName, Action previous, Action current)
        {
            Button button = root.Q<Button>(buttonName);

            // Given that the "clicked" Action works with delegates, we need to remove the reference to the old 
            // functionality and add a reference to the new one
            button.clicked -= current; // Ensure that there is no duplicate functionality
            button.clicked -= previous;
            button.clicked += current;
        }
    }
}