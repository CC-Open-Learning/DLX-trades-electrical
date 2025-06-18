using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     Represents information for a toast message, including its
    ///     display duration and feedback text.
    /// </summary>
    public struct ToastInformation
    {
        public float ToastTimeout;
        public string ToastFeedback;
    }

    /// <summary>
    ///     An abstract base class for managing quiz menu functionality,
    ///     providing common behavior for derived quiz menu controllers.
    /// </summary>
    public abstract class QuizMenuController : MenuController
    {
        /// <summary>
        ///     Maps viewDataKeys to lists of toast information for managing toast messages.
        /// </summary>
        protected Dictionary<string, List<ToastInformation>> ToastInfoMap = new();


        // UI Elements
        protected Button OptionAButton;
        protected Button OptionBButton;
        protected Button OptionCButton;

        private VisualElement firstCategory;
        private VisualElement secondCategory;
        private VisualElement thirdCategory;

        private Label backButtonLabel;
        private Label headerLabel;
        private Label instructionsLabel;


        // Identifiers
        private const string SelectionArrayId = "SelectionArray";
        private const string FirstCategoryId = "Category0";
        private const string SecondCategoryId = "Category1";
        private const string ThirdCategoryId = "Category2";
        private const string HeaderTextId = "HeaderText";
        private const string OptionButtonImageId = "ImageOption";
        private const string InstructionsId = "Instructions";
        private const string OptionAButtonId = "BtnOptionA";
        private const string OptionBButtonId = "BtnOptionB";
        private const string OptionCButtonId = "BtnOptionC";
        private const string ClassCategoryFirst = "category__first"; // Technically unused but including it for consistency
        private const string ClassCategorySecond = "category__second";
        private const string ClassCategoryThird = "category__third";


        // Events
        public UnityEvent BackButtonPressed = new();

        public UnityEvent<ToastMessageType, string, float> OpenToast = new();

        public UnityEvent HideToast = new();

        public UnityEvent QuizFinished = new();

        public UnityEvent DisplayAfterQuizFinished = new();


        // Fields

        /// <summary> Serialized sprites used to set the image options for each quiz </summary>
        public Sprite Category1OptionA;
        public Sprite Category1OptionB;
        public Sprite Category1OptionC;
        public Sprite Category2OptionA;
        public Sprite Category2OptionB;
        public Sprite Category2OptionC;
        public Sprite Category3OptionA;
        public Sprite Category3OptionB;
        public Sprite Category3OptionC;

        /// <summary> The duration in seconds for which toast messages remain visible. </summary>
        protected const float ToastTimeout = 7f;

        /// <summary> The viewDataKey for the correct button in each quiz category. </summary>
        protected string CorrectButtonKeyCategory1;
        protected string CorrectButtonKeyCategory2;
        protected string CorrectButtonKeyCategory3;

        protected string InstructionsTextCategory1;
        protected string InstructionsTextCategory2;
        protected string InstructionsTextCategory3;

        protected string HeaderLabelText;
        protected string BackButtonLabelText;
        protected string FirstStepText;
        protected string SecondStepText;
        protected string ThirdStepText;

        protected string OptionATextCategory1;
        protected string OptionBTextCategory1;
        protected string OptionCTextCategory1;

        protected string OptionATextCategory2;
        protected string OptionBTextCategory2;
        protected string OptionCTextCategory2;

        protected string OptionATextCategory3;
        protected string OptionBTextCategory3;
        protected string OptionCTextCategory3;

        

        /// <summary>
        ///     Virtual property that indicates the number of quiz questions. 
        ///     
        ///     Default value is 3 as that is the max number of questions that this 
        ///     class currently supports.
        ///     
        ///     Override this in derived classes to restrict category functionality.
        /// </summary>
        protected virtual int NumQuestions => 3;

        /// <summary>
        ///     Indicates which quiz is active.
        ///     Sets the active button key (correct answer) and quiz index
        ///     based on the quiz state.
        /// </summary>
        private int ActiveQuizCategory
        {
            get => activeQuizCategory;
            set
            {
                activeQuizCategory = value;
                switch (value)
                {
                    case 1:
                        activeButtonKey = CorrectButtonKeyCategory1;
                        activeQuizIndex = 0;
                        break;
                    case 2:
                        activeButtonKey = CorrectButtonKeyCategory2;
                        activeQuizIndex = 1;
                        break;
                    case 3:
                        activeButtonKey = CorrectButtonKeyCategory3;
                        activeQuizIndex = 2;
                        break;
                }
            }
        }

        private int activeQuizCategory = 1;
        private string activeButtonKey;
        private int activeQuizIndex;

        public override void Initialize()
        {
            base.Initialize();

            firstCategory = Root.Q<VisualElement>(FirstCategoryId);
            secondCategory = Root.Q<VisualElement>(SecondCategoryId);
            thirdCategory = Root.Q<VisualElement>(ThirdCategoryId);

            instructionsLabel = Root.Q<Label>(InstructionsId);
            headerLabel = Root.Q<Label>(HeaderTextId);
            backButtonLabel = Root.Q<VisualElement>(UIHelper.NavigationContainerId)
                .Q<Label>();

            OptionAButton = Root.Q<Button>(OptionAButtonId);
            OptionBButton = Root.Q<Button>(OptionBButtonId);
            OptionCButton = Root.Q<Button>(OptionCButtonId);

            ConfigureCategoryDisplay();

            // Fetch information fields from child class
            ConfigureInformationFields();

            // Setup initial information
            ActiveQuizCategory = 1;
            OnlyEnableCurrentCategory();
            ConfigureInitialMenuInformation();
            ConfigureQuizInformation();

            // Setup navigation buttons
            ConfigureBackNavigation();
            ConfigureCloseNavigation();

            // Setup selection buttons and toast information relationships
            ConfigureSelectionButtons();
            ConfigureToastInfoMap();
        }

        /// <summary>
        ///     Returns a list of selection buttons from the UI.
        /// </summary>
        public List<Button> GetSelectionButtons()
        {
            return Root.Q<VisualElement>(SelectionArrayId).Query<Button>().ToList();
        }

        /// <summary>
        ///     Configures the information fields for the derived class, setting up necessary UI elements.
        /// </summary>
        protected abstract void ConfigureInformationFields();

        /// <summary>
        ///     Configures the toast information map for the derived class, establishing how toast messages are managed.
        /// </summary>
        protected abstract void ConfigureToastInfoMap();

        private void ConfigureCategoryDisplay()
        {
            if (NumQuestions == 2)
            {
                secondCategory.RemoveFromClassList(ClassCategorySecond);
                secondCategory.AddToClassList(ClassCategoryThird);
            }

            // Set last category visibility based on number of quiz questions (either 2 or 3)
            thirdCategory.style.display = (NumQuestions == 3) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        ///     Handles the selection of a quiz button, processing it
        ///     based on whether it matches the correct answer for the current quiz.
        /// </summary>
        /// <param name="selection">The button representing the selected quiz option.</param>
        private void SelectQuizOption(Button selection)
        {
            // Ensure that the state of the UI is set to default
            ResetUIState();

            // Handle incorrect answer then exit
            if (selection.viewDataKey != activeButtonKey)
            {
                if (!TryGetToastInformation(selection, out ToastInformation toastInfo))
                {
                    Debug.LogError($"Unable to find toast information for UI element {selection.name}");
                    return;
                }

                OpenToast?.Invoke(ToastMessageType.Error, toastInfo.ToastFeedback, toastInfo.ToastTimeout);
                selection.AddToClassList(UIHelper.ClassTaskOptionWrong);

                return;
            }

            // Process correct answer based on quiz state
            if (ActiveQuizCategory == 1)
            {
                ActiveQuizCategory = 2;
                OnlyEnableCurrentCategory();
                ConfigureQuizInformation();
            }
            else if (ActiveQuizCategory == 2 && NumQuestions == 3)
            {
                ActiveQuizCategory = 3;
                OnlyEnableCurrentCategory();
                ConfigureQuizInformation();
            }
            else
            {
                QuizFinished?.Invoke();
                DisplayAfterQuizFinished?.Invoke();
                Display(false);
            }
        }

        /// <summary>
        ///     Configures the initial menu information (header, back button text, category labels).
        /// </summary>
        private void ConfigureInitialMenuInformation()
        {
            headerLabel.text = HeaderLabelText;
            backButtonLabel.text = BackButtonLabelText;
            firstCategory.Q<Label>().text = FirstStepText;
            secondCategory.Q<Label>().text = SecondStepText;
            thirdCategory.Q<Label>().text = ThirdStepText;
        }

        /// <summary>
        ///     Configures the quiz information by updating any UI elements for the option buttons
        ///     based on the active quiz flag.
        /// </summary>
        private void ConfigureQuizInformation()
        {
            // Query common UI elements
            var optionALabel = OptionAButton.Q<Label>();
            var optionBLabel = OptionBButton.Q<Label>();
            var optionCLabel = OptionCButton.Q<Label>();

            // Setup information based on the state of the knowledge checks
            switch (ActiveQuizCategory)
            {
                case 1:
                    instructionsLabel.text = InstructionsTextCategory1;

                    OptionAButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category1OptionA);
                    OptionBButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category1OptionB);
                    OptionCButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category1OptionC);

                    optionALabel.text = OptionATextCategory1;
                    optionBLabel.text = OptionBTextCategory1;
                    optionCLabel.text = OptionCTextCategory1;
                    break;

                case 2:
                    instructionsLabel.text = InstructionsTextCategory2;

                    OptionAButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category2OptionA);
                    OptionBButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category2OptionB);
                    OptionCButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category2OptionC);

                    optionALabel.text = OptionATextCategory2;
                    optionBLabel.text = OptionBTextCategory2;
                    optionCLabel.text = OptionCTextCategory2;
                    break;

                case 3:
                    instructionsLabel.text = InstructionsTextCategory3;

                    OptionAButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category3OptionA);
                    OptionBButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category3OptionB);
                    OptionCButton.Q<VisualElement>(OptionButtonImageId).style.backgroundImage =
                        new StyleBackground(Category3OptionC);

                    optionALabel.text = OptionATextCategory3;
                    optionBLabel.text = OptionBTextCategory3;
                    optionCLabel.text = OptionCTextCategory3;
                    break;
            }
        }

        /// <summary>
        ///     Configures the selection buttons by setting up their click events
        ///     and ensuring their view data keys are in the toast information map.
        /// </summary>
        private void ConfigureSelectionButtons()
        {
            List<Button> selectionButtons = GetSelectionButtons();

            foreach (var button in selectionButtons)
            {
                if (!ToastInfoMap.ContainsKey(button.viewDataKey))
                {
                    // Toast information to be populated by child classes
                    ToastInfoMap.Add(button.viewDataKey, new List<ToastInformation>());
                }

                button.clicked += () => { SelectQuizOption(button); };
            }
        }

        /// <summary>
        ///     Enables the currently active quiz category while disabling the other category.
        /// </summary>
        private void OnlyEnableCurrentCategory()
        {
            firstCategory.SetEnabled(ActiveQuizCategory == 1);
            secondCategory.SetEnabled(ActiveQuizCategory == 2);
            thirdCategory.SetEnabled(ActiveQuizCategory == 3);
        }

        /// <summary>
        ///     Attempts to retrieve the toast information object for the specified visual element.
        /// </summary>
        /// <param name="element">The visual element for which to get the toast information.</param>
        /// <param name="toastInfo">The retrieved toast information, if successful.</param>
        /// <returns>True if toast information was found; otherwise, false.</returns>
        private bool TryGetToastInformation(VisualElement element, out ToastInformation toastInfo)
        {
            toastInfo = new ToastInformation();

            // Ensure that the viewDataKey to ToastInformation List mapping exists with at least one element
            if (!ToastInfoMap.TryGetValue(element.viewDataKey, out List<ToastInformation> elementsList)
                || elementsList.Count <= activeQuizIndex)
            {
                return false;
            }

            toastInfo = elementsList[activeQuizIndex];
            return true;
        }

        /// <summary>
        ///     Configures navigation events for the "Close" button in the header bar
        /// </summary>
        private void ConfigureCloseNavigation()
        {
            CloseButtonPressed.AddListener(() => ResetUIState());
        }

        /// <summary>
        ///     Configures navigation events for the "Back" button, allowing users
        ///     to navigate back
        /// </summary>
        private void ConfigureBackNavigation()
        {
            // Back button events
            var backButton = Root.Q<Button>(UIHelper.BackButtonId);

            backButton.clicked += () =>
            {
                BackButtonPressed?.Invoke();
                CancelInteractions();
            };
        }

        /// <summary>
        ///     Cancels user interactions by hiding the menu and resetting the UI state.
        /// </summary>
        private void CancelInteractions()
        {
            Display(false);
            ResetUIState();
        }

        /// <summary>
        ///     Resets the state of the UI by hiding toast messages and clearing incorrect selections.
        /// </summary>
        private void ResetUIState()
        {
            HideToast?.Invoke();

            // Get all selection buttons and set them to the default style
            List<Button> selectionButtons = GetSelectionButtons();
            foreach (var button in selectionButtons)
            {
                button.RemoveFromClassList(UIHelper.ClassTaskOptionWrong);
            }
        }
    }
}