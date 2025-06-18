using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;
using Object = UnityEngine.Object;

/// <summary>
///     Provides a test suite for the RunSupplyCableMenuController class, ensuring its UI and functionality work as
///     expected.
/// </summary>
/// <remarks> Follows the AAA (Arrange, Act, Assert) Pattern </remarks>
public class RunSupplyCableMenuControllerTests
{
    // Setup objects
    private GameObject objMenu;
    private RunSupplyCableMenuController uiController;
    private VisualElement rootContainer;

    private Button terminateCablesBtn;
    private Button connectCablesBtn;

    // Event listeners
    private bool isOpenedEventInvoked = false;
    private bool isClosedEventInvoked = false;
    private bool isCloseButtonPressedEventInvoked = false;
    private bool isBackButtonPressedEventInvoked = false;
    private bool isConnectCablesButtonPressedEventInvoked = false;
    private bool isTerminateCablesButtonPressedEventInvoked = false;
    private bool isShowCableQuizEventInvoked = false;
    private bool isShowTerminateQuizEventInvoked = false;

    // Identifiers
    private const string CloseButtonId = UIHelper.CloseButtonId;
    private const string BackButtonId = UIHelper.BackButtonId;
    private const string ConnectCablesToBoxId = "BtnConnectCablesToBoxes";
    private const string TerminateCablesInBoxId = "BtnTerminateCables";
    private const string StatusLabelId = "StatusLabel";
    private const string TaskCountLabelId = "LabelTaskCount";

    // Utility fields
    private const int CompletedTerminateTasks = 2;
    private const int CompletedSupplyToBoxesTasks = 3;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        // Setup main panel and visual tree asset
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        var uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/RunSupplyCableMenu.uxml");

        if (panelSettings == null)
        {
            throw new ArgumentNullException(nameof(panelSettings));
        }

        if (uxmlTemplate == null)
        {
            throw new ArgumentNullException(nameof(uxmlTemplate));
        }

        // Setup UI Doc
        objMenu = new GameObject("Run Supply Cables Menu Test");
        var uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        // Controller setup
        uiController = objMenu.AddComponent<RunSupplyCableMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;

        connectCablesBtn = rootContainer.Q<Button>(ConnectCablesToBoxId);
        terminateCablesBtn = rootContainer.Q<Button>(TerminateCablesInBoxId);

        // Event listeners setup
        isOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isOpenedEventInvoked = true);

        isClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isClosedEventInvoked = true);

        isCloseButtonPressedEventInvoked = false;
        uiController.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);

        isBackButtonPressedEventInvoked = false;
        uiController.BackButtonPressed.AddListener(() => isBackButtonPressedEventInvoked = true);

        isConnectCablesButtonPressedEventInvoked = false;
        uiController.ConnectCablesButtonPressed.AddListener(() => isConnectCablesButtonPressedEventInvoked = true);

        isTerminateCablesButtonPressedEventInvoked = false;
        uiController.TerminateCablesButtonPressed.AddListener(() => isTerminateCablesButtonPressedEventInvoked = true);

        isShowCableQuizEventInvoked = false;
        uiController.ShowCableQuiz.AddListener(() => isShowCableQuizEventInvoked = true);

        isShowTerminateQuizEventInvoked = false;
        uiController.ShowTerminateQuiz.AddListener(() => isShowTerminateQuizEventInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        isOpenedEventInvoked = false;
        isClosedEventInvoked = false;
        isCloseButtonPressedEventInvoked = false;
        isBackButtonPressedEventInvoked = false;
        isConnectCablesButtonPressedEventInvoked = false;
        isTerminateCablesButtonPressedEventInvoked = false;
        isShowCableQuizEventInvoked = false;
        isShowTerminateQuizEventInvoked = false;

        Object.Destroy(objMenu);
        objMenu = null;
        uiController = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_OpenAndClose()
    {
        AssertListeners();

        // Display_Enabled
        uiController.Display(true);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.IsTrue(isOpenedEventInvoked);
        Assert.IsFalse(terminateCablesBtn.enabledSelf);
        Assert.That(connectCablesBtn.enabledSelf);

        // Display_NotEnabled
        uiController.Display(false);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.IsTrue(isClosedEventInvoked);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator WorkflowButtonClick_WorkflowsVisible()
    {
        AssertListeners();

        // Click on available workflows

        // Cable Quiz Incomplete
        yield return TestUtils.ClickOnButton(connectCablesBtn);
        Assert.IsTrue(isShowCableQuizEventInvoked);

        // Cable Quiz Complete
        uiController.IsCableQuizCompleted = true;
        yield return TestUtils.ClickOnButton(connectCablesBtn);
        Assert.IsTrue(isConnectCablesButtonPressedEventInvoked);

        // Terminate Cables Quiz Incomplete
        yield return TestUtils.ClickOnButton(terminateCablesBtn);
        Assert.IsTrue(isShowTerminateQuizEventInvoked);

        // Terminate Cables Quiz Complete
        uiController.IsTerminateQuizCompleted = true;
        yield return TestUtils.ClickOnButton(terminateCablesBtn);
        Assert.IsTrue(isTerminateCablesButtonPressedEventInvoked);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator SetButtonStatus_LabelsUpdated()
    {
        AssertListeners();

        uiController.SetConnectCablesBtnStatus(WorkflowStatus.Complete);
        uiController.SetTerminateCablesBtnStatus(WorkflowStatus.Available);
        yield return null;

        Label connectCablesStatusLabel = connectCablesBtn.Q<Label>(StatusLabelId);
        Label terminateCablesStatusLabel = terminateCablesBtn.Q<Label>(StatusLabelId);

        Assert.AreEqual(UIHelper.StatusComplete, connectCablesStatusLabel.text);
        Assert.AreEqual(UIHelper.StatusAvailable, terminateCablesStatusLabel.text);
        Assert.That(connectCablesStatusLabel.ClassListContains(UIHelper.ClassTaskStatusComplete));
        Assert.That(terminateCablesStatusLabel.ClassListContains(UIHelper.ClassTaskStatusAvailable));
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator SetCompletedTasksNum_LabelsUpdated()
    {
        AssertListeners();

        string oldConnectCablesText = connectCablesBtn.Q<Label>(TaskCountLabelId).text;
        string oldTerminateCablesText = terminateCablesBtn.Q<Label>(TaskCountLabelId).text;

        uiController.UpdateSupplyToBoxesCompleteTasksNum(4);
        uiController.UpdateTerminateCompleteTasksNum(3);
        yield return null;

        string connectCablesText = connectCablesBtn.Q<Label>(TaskCountLabelId).text;
        string terminateCablesText = terminateCablesBtn.Q<Label>(TaskCountLabelId).text;

        Assert.AreNotEqual(oldConnectCablesText, connectCablesText);
        Assert.AreNotEqual(oldTerminateCablesText, terminateCablesText);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator Navigation_CloseButton_MenuClosed()
    {
        // Setup
        AssertListeners();
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        yield return TestUtils.ClickOnButton(rootContainer.Q<Button>(CloseButtonId));
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.IsTrue(isCloseButtonPressedEventInvoked);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator Navigation_BackButton_MenuClosed()
    {
        // Setup
        AssertListeners();
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        yield return TestUtils.ClickOnButton(rootContainer.Q<Button>(BackButtonId));
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.IsTrue(isBackButtonPressedEventInvoked);
    }

    /// <summary>
    ///     Used to assert the initial values for every listener in a test case
    /// </summary>
    private void AssertListeners()
    {
        Assert.IsFalse(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);
        Assert.IsFalse(isShowTerminateQuizEventInvoked);
        Assert.IsFalse(isShowCableQuizEventInvoked);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);
        Assert.IsFalse(isBackButtonPressedEventInvoked);
        Assert.IsFalse(isConnectCablesButtonPressedEventInvoked);
        Assert.IsFalse(isTerminateCablesButtonPressedEventInvoked);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }
}