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
///     Test Suite for the Task Selection Menu Controller
/// </summary>
/// <remarks> Follows the AAA (Arrange, Act, Assert) Pattern </remarks>
public class TaskSelectionMenuControllerTests
{
    // Setup objects
    private GameObject uiGameObj;
    private TaskSelectionMenuController uiController;
    private VisualElement rootElement;

    private Button selectAndMountBoxesBtn;
    private Button runSupplyCableBtn;
    private Button installDevicesBtn;

    // Event listeners
    private bool isCloseButtonPressedInvoked = false;
    private bool isSelectAndMountBoxesButtonInvoked = false;
    private bool isRunSupplyCableButtonInvoked = false;
    private bool isInstallDevicesButtonPressedInvoked = false;
    private bool isOpenedEventInvoked = false;
    private bool isClosedEventInvoked = false;

    // Identifiers
    private const string SelectAndMountBoxesButtonId = "BtnSelectAndMountBoxes";
    private const string RunSupplyCableButtonId = "BtnRunSupplyCable";
    private const string InstallDevicesButtonId = "BtnInstallDevices";
    private const string StatusLabelId = "StatusLabel";
    private const string TaskCountLabelId = "LabelTaskCount";
    private const string CloseButtonId = UIHelper.CloseButtonId;

    [Category("BuildServer")]
    [UnitySetUp]
    public IEnumerator TestSetUp()
    {
        PanelSettings panelSettings =
            AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/TaskSelectionMenu.uxml");

        if (panelSettings == null)
        {
            throw new ArgumentNullException(nameof(panelSettings));
        }

        if (uxmlTemplate == null)
        {
            throw new ArgumentNullException(nameof(uxmlTemplate));
        }

        uiGameObj = new("Task Selection Menu");
        UIDocument uiDoc = uiGameObj.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();
        rootElement = uiDoc.rootVisualElement;

        uiController = uiGameObj.AddComponent<TaskSelectionMenuController>();
        uiController.HasBackwardsNavigation = false;
        uiController.gameObject.SetActive(true);
        yield return null;

        selectAndMountBoxesBtn = rootElement.Q<Button>(SelectAndMountBoxesButtonId);
        runSupplyCableBtn = rootElement.Q<Button>(RunSupplyCableButtonId);
        installDevicesBtn = rootElement.Q<Button>(InstallDevicesButtonId);

        isCloseButtonPressedInvoked = false;
        uiController.Closed.AddListener(() => isCloseButtonPressedInvoked = true);

        isSelectAndMountBoxesButtonInvoked = false;
        uiController.SelectAndMountBoxesButtonPressed.AddListener(() => isSelectAndMountBoxesButtonInvoked = true);

        isRunSupplyCableButtonInvoked = false;
        uiController.RunSupplyCableButtonPressed.AddListener(() => isRunSupplyCableButtonInvoked = true);

        isInstallDevicesButtonPressedInvoked = false;
        uiController.InstallDevicesButtonPressed.AddListener(() => isInstallDevicesButtonPressedInvoked = true);

        isOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isOpenedEventInvoked = true);

        isClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isClosedEventInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        isCloseButtonPressedInvoked = false;
        isSelectAndMountBoxesButtonInvoked = false;
        isRunSupplyCableButtonInvoked = false;
        isInstallDevicesButtonPressedInvoked = false;
        isOpenedEventInvoked = false;
        isClosedEventInvoked = false;

        Object.Destroy(uiGameObj);
        uiController = null;
        rootElement = null;
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator Display_OpenAndClose()
    {
        AssertListeners();

        // Display_Enabled
        uiController.Display(true);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);
        Assert.IsTrue(isOpenedEventInvoked);
        // TODO: Uncomment this line when done testing the connection between the
        // Task Selection Menu and the Device Selection Menu. The button has been enabled
        // for now as a placeholder.
        //Assert.IsFalse(uiController.IsInstallDevicesBtnEnabled());
        Assert.IsFalse(uiController.IsSupplyCableBtnEnabled());
        Assert.That(uiController.IsSelectAndMountBoxesBtnEnabled());

        // Display_NotEnabled
        uiController.Display(false);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);
        Assert.IsTrue(isClosedEventInvoked);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator WorkflowButtonClick_WorkflowsVisible()
    {
        AssertListeners();

        // Click on available workflows

        yield return TestUtils.ClickOnButton(selectAndMountBoxesBtn);
        Assert.IsTrue(isSelectAndMountBoxesButtonInvoked);

        yield return TestUtils.ClickOnButton(runSupplyCableBtn);
        Assert.IsTrue(isRunSupplyCableButtonInvoked);

        yield return TestUtils.ClickOnButton(installDevicesBtn);
        Assert.IsTrue(isInstallDevicesButtonPressedInvoked);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator SetButtonStatus_LabelsUpdated()
    {
        AssertListeners();

        uiController.SetDeviceInstallBtnStatus(WorkflowStatus.Complete);
        uiController.SetSupplyTaskBtnStatus(WorkflowStatus.InProgress);
        uiController.SetMountingTaskBtnStatus(WorkflowStatus.Unavailable);
        yield return null;

        Label mountStatusLabel = selectAndMountBoxesBtn.Q<Label>(StatusLabelId);
        Label supplyStatusLabel = runSupplyCableBtn.Q<Label>(StatusLabelId);
        Label devicesStatusLabel = installDevicesBtn.Q<Label>(StatusLabelId);

        Assert.AreEqual(UIHelper.StatusNotAvailable, mountStatusLabel.text);
        Assert.AreEqual(UIHelper.StatusInProgress, supplyStatusLabel.text);
        Assert.AreEqual(UIHelper.StatusComplete, devicesStatusLabel.text);
        Assert.That(mountStatusLabel.ClassListContains(UIHelper.ClassTaskStatusUnavailable));
        Assert.That(supplyStatusLabel.ClassListContains(UIHelper.ClassTaskStatusInProgress));
        Assert.That(devicesStatusLabel.ClassListContains(UIHelper.ClassTaskStatusComplete));
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator SetCompletedTasksNum_LabelsUpdated()
    {
        AssertListeners();

        string oldMountLabelText = selectAndMountBoxesBtn.Q<Label>(TaskCountLabelId).text;
        string oldSupplyLabelText = runSupplyCableBtn.Q<Label>(TaskCountLabelId).text;
        string oldDevicesLabelText = installDevicesBtn.Q<Label>(TaskCountLabelId).text;

        uiController.SetMountingCompleteTasksNum(2);
        uiController.SetSupplyCableCompletedTasksNum(4);
        uiController.SetDevicesCompletedTasksNum(3);
        yield return null;

        string mountLabelText = selectAndMountBoxesBtn.Q<Label>(TaskCountLabelId).text;
        string supplyLabelText = runSupplyCableBtn.Q<Label>(TaskCountLabelId).text;
        string devicesLabelText = installDevicesBtn.Q<Label>(TaskCountLabelId).text;

        Assert.AreNotEqual(oldMountLabelText, mountLabelText);
        Assert.AreNotEqual(oldSupplyLabelText, supplyLabelText);
        Assert.AreNotEqual(oldDevicesLabelText, devicesLabelText);
    }

    [Category("BuildServer")]
    [UnityTest]
    public IEnumerator Navigation_CloseButton_MenuClosed()
    {
        // Setup
        AssertListeners();
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);

        yield return TestUtils.ClickOnButton(rootElement.Q<Button>(CloseButtonId));
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);
        Assert.IsTrue(isCloseButtonPressedInvoked);
    }

    /// <summary>
    ///     Used to assert the initial values for every listener in a test case
    /// </summary>
    private void AssertListeners()
    {
        Assert.IsFalse(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);
        Assert.IsFalse(isCloseButtonPressedInvoked);
        Assert.IsFalse(isInstallDevicesButtonPressedInvoked);
        Assert.IsFalse(isRunSupplyCableButtonInvoked);
        Assert.IsFalse(isSelectAndMountBoxesButtonInvoked);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);
    }
}