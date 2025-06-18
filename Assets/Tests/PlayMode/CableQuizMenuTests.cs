using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class CableQuizMenuTests
{
    // Setup objects
    private GameObject objMenu;
    private CableQuizMenuController menuController;
    private VisualElement rootContainer;

    // Event listeners
    private bool isOpenedEventInvoked;
    private bool isClosedEventInvoked;
    private bool isCloseButtonPressedEventInvoked;
    private bool isBackButtonPressedEventInvoked;
    private bool isQuizFinishedEventInvoked;
    private bool isHideToastEventInvoked;
    private ToastMessageType toastMessageType;
    private string toastMessage;
    private float toastTimeout;

    private VisualElement firstCategory;
    private VisualElement secondCategory;
    private VisualElement backButtonContainer;

    private Button optionAButton;
    private Button optionBButton;
    private Button optionCButton;

    // Identifiers
    private const string FirstCategoryId = "Category0";
    private const string SecondCategoryId = "Category1";
    private const string HeaderTextId = "HeaderText";
    private const string InstructionsId = "Instructions";
    private const string OptionAButtonId = "BtnOptionA";
    private const string OptionBButtonId = "BtnOptionB";
    private const string OptionCButtonId = "BtnOptionC";

    // Instructions
    private const string SupplyCableInstructionsText = "Select the appropriate supply cable for this project";
    private const string LengthInstructionsText = "Approximately how far past the box should you cut the cable?";

    // Initial menu information
    private const string HeaderText = "Supply Cable to Boxes";
    private const string BackButtonText = "Run Supply Cable";
    private const string Category1Text = "Select Supply Cable";
    private const string Category2Text = "Cut Cable";

    // Selection options content
    private const string OptionASupplyCableText = "14/2 NMD90 Electrical Cable";
    private const string OptionBSupplyCableText = "14/3 NMD90 Electrical Cable";
    private const string OptionCSupplyCableText = "12/2 NMD90 Electrical Cable";
    private const string OptionALengthText = "12\" or 300mm past box";
    private const string OptionBLengthText = "16\" or 400mm past box";
    private const string OptionCLengthText = "20\" or 500mm past box";

    // Toast feedback messages
    private const string OptionBCableTypeMsg =
        "14/3 NMD90 is not appropriate because it has too many conductors and is too expensive.";

    private const string OptionCCableTypeMsg = "12/2 NMD90 is not appropriate because it is used for 20A service.";

    private const string OptionACableLengthMsg =
        "12\" or 300mm is too short, it will not allow compliance with 12-510(4) and 12-3000(6)";

    private const string OptionCCableLengthMsg = "20\" or 500mm is too long, the excess cable will be wasted.";

    private const float ExpectedToastTimeout = 7f;

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        // Setup main panel and visual tree asset
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        var uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/QuizWindow.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        // Setup UI Doc
        objMenu = new("Cable Quiz Menu Tests");
        var uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        // Controller setup
        menuController = objMenu.AddComponent<CableQuizMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        menuController.gameObject.SetActive(true);
        yield return null;

        // Fetch UI elements
        optionAButton = rootContainer.Q<Button>(OptionAButtonId);
        optionBButton = rootContainer.Q<Button>(OptionBButtonId);
        optionCButton = rootContainer.Q<Button>(OptionCButtonId);
        backButtonContainer = rootContainer.Q<VisualElement>(UIHelper.NavigationContainerId);
        firstCategory = rootContainer.Q<VisualElement>(FirstCategoryId);
        secondCategory = rootContainer.Q<VisualElement>(SecondCategoryId);

        // Event listeners
        isOpenedEventInvoked = false;
        menuController.Opened.AddListener(() => isOpenedEventInvoked = true);

        isClosedEventInvoked = false;
        menuController.Closed.AddListener(() => isClosedEventInvoked = true);

        isCloseButtonPressedEventInvoked = false;
        menuController.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);

        isBackButtonPressedEventInvoked = false;
        menuController.BackButtonPressed.AddListener(() => isBackButtonPressedEventInvoked = true);

        isQuizFinishedEventInvoked = false;
        menuController.QuizFinished.AddListener(() => isQuizFinishedEventInvoked = true);

        toastMessage = string.Empty;
        toastTimeout = 0f;
        menuController.OpenToast.AddListener((type, msg, timeout) =>
        {
            toastMessageType = type;
            toastMessage = msg;
            toastTimeout = timeout;
        });

        isHideToastEventInvoked = false;
        menuController.HideToast.AddListener(() => isHideToastEventInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        isOpenedEventInvoked = false;
        isClosedEventInvoked = false;
        isBackButtonPressedEventInvoked = false;
        isCloseButtonPressedEventInvoked = false;
        isHideToastEventInvoked = false;
        isQuizFinishedEventInvoked = false;
        toastMessage = string.Empty;
        toastTimeout = 0f;

        Object.Destroy(objMenu);
        objMenu = null;
        menuController = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_InitialValuesSet()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        var optionALabel = optionAButton.Q<Label>();
        var optionBLabel = optionBButton.Q<Label>();
        var optionCLabel = optionCButton.Q<Label>();
        var titleLabel = rootContainer.Q<Label>(HeaderTextId);
        var backButtonLabel = backButtonContainer.Q<Label>();
        var instructionsLabel = rootContainer.Q<Label>(InstructionsId);
        var firstCategoryLabel = firstCategory.Q<Label>();
        var secondCategoryLabel = secondCategory.Q<Label>();

        // Act
        menuController.Display(true);
        yield return null;

        // Assert
        Assert.IsTrue(firstCategory.enabledInHierarchy);
        Assert.IsFalse(secondCategory.enabledInHierarchy);
        Assert.AreEqual(Category1Text, firstCategoryLabel.text);
        Assert.AreEqual(Category2Text, secondCategoryLabel.text);
        Assert.AreEqual(HeaderText, titleLabel.text);
        Assert.AreEqual(BackButtonText, backButtonLabel.text);
        Assert.AreEqual(SupplyCableInstructionsText, instructionsLabel.text);
        Assert.AreEqual(OptionASupplyCableText, optionALabel.text);
        Assert.AreEqual(OptionBSupplyCableText, optionBLabel.text);
        Assert.AreEqual(OptionCSupplyCableText, optionCLabel.text);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_ShouldEnable_MenuOpened()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Act
        menuController.Display(true);
        yield return null;

        // Assert
        Assert.That(isOpenedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_DontEnable_MenuClosed()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Act
        menuController.Display(false);
        yield return null;

        // Assert
        Assert.That(isClosedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOptions_WrongAnswers_ToastInformationSet()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        menuController.Display(true);
        yield return null;

        var optionBClickEvent = new NavigationSubmitEvent()
        {
            target = optionBButton
        };

        var optionCClickEvent = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        var optionAClickEvent = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        // Act Option B Category 1
        optionBButton.SendEvent(optionBClickEvent);
        yield return null;

        // Assert Option B Category 1
        Assert.AreEqual(ToastMessageType.Error, toastMessageType);
        Assert.AreEqual(OptionBCableTypeMsg, toastMessage);
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);

        // Act Option C Category 1
        optionCButton.SendEvent(optionCClickEvent);
        yield return null;

        // Assert Option C Category 1
        Assert.AreEqual(ToastMessageType.Error, toastMessageType);
        Assert.AreEqual(OptionCCableTypeMsg, toastMessage);
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);

        // Act Option A Category 1 (Move to the next quiz)
        optionAButton.SendEvent(optionAClickEvent);
        yield return null;

        // Assert Option A Category 1 (Simple check to ensure next quiz is visible)
        Assert.IsFalse(firstCategory.enabledInHierarchy);
        Assert.IsTrue(secondCategory.enabledInHierarchy);

        // Arrange Category 2 (NavigationSubmitEvents can only be dispatched once)
        var optionCClickEvent2 = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        var optionAClickEvent2 = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        // Act Option A Category 2
        optionAButton.SendEvent(optionAClickEvent2);
        yield return null;

        // Assert Option A Category 2
        Assert.AreEqual(ToastMessageType.Error, toastMessageType);
        Assert.AreEqual(OptionACableLengthMsg, toastMessage);
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);

        // Act Option C Category 2
        optionCButton.SendEvent(optionCClickEvent2);
        yield return null;

        // Assert Option C Category 2
        Assert.AreEqual(ToastMessageType.Error, toastMessageType);
        Assert.AreEqual(OptionCCableLengthMsg, toastMessage);
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOption_CorrectSupplyCable_NewQuizShown()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        var optionALabel = optionAButton.Q<Label>();
        var optionBLabel = optionBButton.Q<Label>();
        var optionCLabel = optionCButton.Q<Label>();
        var instructionsLabel = rootContainer.Q<Label>(InstructionsId);

        menuController.Display(true);
        yield return null;

        var correctButtonClick = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        // Act
        optionAButton.SendEvent(correctButtonClick);
        yield return null;

        // Assert
        Assert.IsFalse(firstCategory.enabledInHierarchy);
        Assert.IsTrue(secondCategory.enabledInHierarchy);
        Assert.AreEqual(LengthInstructionsText, instructionsLabel.text);
        Assert.AreEqual(OptionALengthText, optionALabel.text);
        Assert.AreEqual(OptionBLengthText, optionBLabel.text);
        Assert.AreEqual(OptionCLengthText, optionCLabel.text);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Navigation_BackButton_EventsInvoked()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        var backButton = backButtonContainer.Q<Button>();
        var backButtonClick = new NavigationSubmitEvent()
        {
            target = backButton
        };

        // Act
        backButton.SendEvent(backButtonClick);
        yield return null;

        // Assert
        VerifyStateReset();
        Assert.IsTrue(isBackButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Navigation_CloseButton_EventsInvoked()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        var closeButton = rootContainer.Q<Button>(UIHelper.CloseButtonId);
        var closeButtonClick = new NavigationSubmitEvent()
        {
            target = closeButton
        };

        // Act
        closeButton.SendEvent(closeButtonClick);
        yield return null;

        // Assert
        VerifyStateReset();
        Assert.IsTrue(isCloseButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOptions_CorrectCableLength_QuizFinished()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        menuController.Display(true);
        yield return null;

        var correctCableButtonClick = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        optionAButton.SendEvent(correctCableButtonClick);
        yield return null;

        var correctLengthButtonClick = new NavigationSubmitEvent()
        {
            target = optionBButton
        };

        // Act
        optionBButton.SendEvent(correctLengthButtonClick);
        yield return null;

        // Assert
        Assert.IsTrue(isQuizFinishedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ButtonClick__UIStateIsExpected()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        menuController.Display(true);
        yield return null;

        var optionBClickEvent = new NavigationSubmitEvent()
        {
            target = optionBButton
        };

        var optionCClickEvent = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        var optionAClickEvent = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        // Act Button B (Incorrect Answer)
        optionBButton.SendEvent(optionBClickEvent);

        // Assert Button B
        Assert.That(isHideToastEventInvoked);
        Assert.That(optionBButton.ClassListContains(UIHelper.ClassTaskOptionWrong));

        // Act Button C (Incorrect Answer)
        optionCButton.SendEvent(optionCClickEvent);

        // Assert Button C
        Assert.That(isHideToastEventInvoked);
        Assert.That(optionCButton.ClassListContains(UIHelper.ClassTaskOptionWrong));
        Assert.IsFalse(optionBButton.ClassListContains(UIHelper.ClassTaskOptionWrong));

        // Act Button A (Correct Answer)
        optionAButton.SendEvent(optionAClickEvent);

        // Assert Button A
        VerifyStateReset();
    }

    private void VerifyStateReset()
    {
        Assert.That(isHideToastEventInvoked);

        List<Button> selectionButtons = menuController.GetSelectionButtons();
        foreach (var button in selectionButtons)
        {
            Assert.IsFalse(button.ClassListContains(UIHelper.ClassTaskOptionWrong));
        }
    }

    private void VerifyInitialValues()
    {
        Assert.IsFalse(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);
        Assert.IsFalse(isBackButtonPressedEventInvoked);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);
        Assert.IsFalse(isHideToastEventInvoked);
        Assert.That(string.IsNullOrEmpty(toastMessage));
        Assert.That(toastTimeout == 0f);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }
}