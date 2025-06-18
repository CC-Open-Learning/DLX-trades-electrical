using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class TerminateCablesQuizTests
{
    // Setup objects
    private GameObject objMenu;
    private TerminateCablesQuizMenuController controller;
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
    private const string QuestionCat0Text = "What is the correct amount of outer sheathing to remove?";

    private const string QuestionCat1Text =
        "When securing the cable in the box, how much sheathing should be visible past the clamp?";

    // Initial menu information
    private const string WindowTitleText = "Terminate Cables In Boxes";
    private const string Category0TabText = "Unsheath Cables";
    private const string Category1TabText = "Secure Cables";
    private const string BackButtonText = "Run Supply Cables";

    // Selection options content
    private const string Cat0ButtonAText = "Remove 2-3\" or 50-75mm of cable sheathing";
    private const string Cat0ButtonBText = "Remove 4-6\" or 100-150mm of cable sheathing";
    private const string Cat0ButtonCText = "Remove 8-10\" or 200-250mm of cable sheathing ";
    private const string Cat1ButtonAText = "2\" or 50mm of sheathing visible";
    private const string Cat1ButtonBText = "1/4\" or 6mm of sheathing visible ";
    private const string Cat1ButtonCText = "No sheathing visible ";

    // Toast feedback messages
    private const string Cat0Feedback =
        "Try again! CEC rule 12-3000(6) requires 150mm free conductor, measured from the face of the box.";

    private const string Cat1ButtonAFeedback =
        "Try again! 2\" of sheathing past the clamp will make it difficult to work with these conductors in such a small space.";

    private const string Cat1ButtonCFeedback =
        "Try again! CEC rules 2-026 and 2-112 require that conductors be protected by sheathing in this situation.";


    private const ToastMessageType ExpectedToastType = ToastMessageType.Error;
    private const float ExpectedToastTimeout = 7f;

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        var uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/QuizWindow.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        //UI Doc compnent  
        objMenu = new("Terminate cables Menu Tests");
        var uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        //controller component
        controller = objMenu.AddComponent<TerminateCablesQuizMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        controller.gameObject.SetActive(true);
        yield return null;

        optionAButton = rootContainer.Q<Button>(OptionAButtonId);
        optionBButton = rootContainer.Q<Button>(OptionBButtonId);
        optionCButton = rootContainer.Q<Button>(OptionCButtonId);
        backButtonContainer = rootContainer.Q<VisualElement>(UIHelper.NavigationContainerId);
        firstCategory = rootContainer.Q<VisualElement>(FirstCategoryId);
        secondCategory = rootContainer.Q<VisualElement>(SecondCategoryId);

        // Event listeners
        isOpenedEventInvoked = false;
        controller.Opened.AddListener(() => isOpenedEventInvoked = true);

        isClosedEventInvoked = false;
        controller.Closed.AddListener(() => isClosedEventInvoked = true);

        isCloseButtonPressedEventInvoked = false;
        controller.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);

        isBackButtonPressedEventInvoked = false;
        controller.BackButtonPressed.AddListener(() => isBackButtonPressedEventInvoked = true);

        isQuizFinishedEventInvoked = false;
        controller.QuizFinished.AddListener(() => isQuizFinishedEventInvoked = true);

        toastMessage = string.Empty;
        toastTimeout = 0f;
        controller.OpenToast.AddListener((type, msg, timeout) =>
        {
            toastMessageType = type;
            toastMessage = msg;
            toastTimeout = timeout;
        });

        isHideToastEventInvoked = false;
        controller.HideToast.AddListener(() => isHideToastEventInvoked = true);
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
        controller = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_Enable_MenuOpened()
    {
        // Pre-arrange check
        AssertListeners();

        // Act
        controller.Display(true);
        yield return null;

        // Assert
        Assert.IsTrue(isOpenedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_DontEnable_MenuClosed()
    {
        // Pre-arrange check
        AssertListeners();

        // Act
        controller.Display(false);
        yield return null;

        // Assert
        Assert.IsTrue(isClosedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_InitialValuesSet()
    {
        // Pre-arrange check
        AssertListeners();

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
        controller.Display(true);
        yield return null;

        // Assert
        Assert.IsTrue(firstCategory.enabledInHierarchy);
        Assert.IsFalse(secondCategory.enabledInHierarchy);
        Assert.AreEqual(Category0TabText, firstCategoryLabel.text);
        Assert.AreEqual(Category1TabText, secondCategoryLabel.text);
        Assert.AreEqual(WindowTitleText, titleLabel.text);
        Assert.AreEqual(BackButtonText, backButtonLabel.text);
        Assert.AreEqual(QuestionCat0Text, instructionsLabel.text);
        Assert.AreEqual(Cat0ButtonAText, optionALabel.text);
        Assert.AreEqual(Cat0ButtonBText, optionBLabel.text);
        Assert.AreEqual(Cat0ButtonCText, optionCLabel.text);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOption_CorrectAnswerCat1_NewQuizVisible()
    {
        // Pre-arrange check
        AssertListeners();

        // Arrange
        var instructionsLabel = rootContainer.Q<Label>(InstructionsId);

        controller.Display(true);
        yield return null;

        var correctCat0Click = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        // Act
        optionCButton.SendEvent(correctCat0Click);
        yield return null;

        // Assert
        Assert.IsFalse(firstCategory.enabledInHierarchy);
        Assert.IsTrue(secondCategory.enabledInHierarchy);
        Assert.AreEqual(optionAButton.Q<Label>().text, Cat1ButtonAText);
        Assert.AreEqual(optionBButton.Q<Label>().text, Cat1ButtonBText);
        Assert.AreEqual(optionCButton.Q<Label>().text, Cat1ButtonCText);
        Assert.AreEqual(instructionsLabel.text, QuestionCat1Text);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOption_IncorrectAnswerCat1_ToastFieldsSet()
    {
        // Pre-arrange check
        AssertListeners();

        // Arrange Option B
        controller.Display(true);

        var optionBIncorrectClick = new NavigationSubmitEvent()
        {
            target = optionBButton
        };

        // Act Option B
        optionBButton.SendEvent(optionBIncorrectClick);
        yield return null;

        // Assert Option B
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);
        Assert.AreEqual(ExpectedToastType, toastMessageType);
        Assert.AreEqual(Cat0Feedback, toastMessage);

        // Arrange Option A
        var optionAIncorrectClick = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        // Act Option A
        optionAButton.SendEvent(optionAIncorrectClick);
        yield return null;

        // Assert Option A
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);
        Assert.AreEqual(ExpectedToastType, toastMessageType);
        Assert.AreEqual(Cat0Feedback, toastMessage);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOption_CorrectAnswerCat2_QuizFinishedInvoked()
    {
        // Pre-arrange check
        AssertListeners();

        // Arrange
        controller.Display(true);

        // Simulate moving onto the next quiz
        var correctCat0Click = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        optionCButton.SendEvent(correctCat0Click);
        yield return null;

        // Act
        var correctCat1Click = new NavigationSubmitEvent()
        {
            target = optionBButton
        };

        optionBButton.SendEvent(correctCat1Click);
        yield return null;

        // Assert
        Assert.IsTrue(isQuizFinishedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectQuizOption_IncorrectAnswerCat2_ToastFieldsSet()
    {
        // Pre-arrange check
        AssertListeners();

        // Arrange Cat 1
        controller.Display(true);

        // Simulate completing the first half of the quiz
        var correctCat0Click = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        optionCButton.SendEvent(correctCat0Click);
        yield return null;

        // Arrange Option A Cat 2
        var optionAIncorrectClick = new NavigationSubmitEvent()
        {
            target = optionAButton
        };

        // Act Option A Cat 2
        optionAButton.SendEvent(optionAIncorrectClick);
        yield return null;

        // Assert Option A Cat 2
        Assert.AreEqual(ExpectedToastType, toastMessageType);
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);
        Assert.AreEqual(Cat1ButtonAFeedback, toastMessage);

        // Arrange Option C Cat 2
        var optionCIncorrectClick = new NavigationSubmitEvent()
        {
            target = optionCButton
        };

        // Act Option C Cat 2
        optionCButton.SendEvent(optionCIncorrectClick);
        yield return null;

        // Assert Option C Cat 2
        Assert.AreEqual(ExpectedToastType, toastMessageType);
        Assert.AreEqual(ExpectedToastTimeout, toastTimeout);
        Assert.AreEqual(Cat1ButtonCFeedback, toastMessage);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Navigation_BackButton_EventInvoked()
    {
        // Pre-arrange check
        AssertListeners();

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
        Assert.IsTrue(isBackButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Navigation_CloseButton_EventInvoked()
    {
        // Pre-arrange check
        AssertListeners();

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
        Assert.IsTrue(isCloseButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    private void AssertListeners()
    {
        Assert.IsFalse(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);
        Assert.IsFalse(isBackButtonPressedEventInvoked);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);
        Assert.IsFalse(isHideToastEventInvoked);
        Assert.That(toastMessage == string.Empty);
        Assert.That(toastTimeout == 0f);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }
}