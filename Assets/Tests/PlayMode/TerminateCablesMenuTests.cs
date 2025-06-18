using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

/// <summary>
///     Provides a test suite for the TerminateWiresMenuController class, ensuring its UI and functionality work as
///     expected.
/// </summary>
public class TerminateCablesMenuTests
{
    // Setup objects
    private GameObject objMenu;
    private TerminateCablesMenuController controller;
    private VisualElement rootContainer;
    private WireMountingTask terminateLightBoxTask;

    // Event listeners
    private bool isMenuOpenedEventInvoked;
    private bool isMenuClosedEventInvoked;
    private bool isCloseButtonPressedEventInvoked;
    private bool isBackButtonPressedEventInvoked;
    private Task currentTask;

    private const string BackButtonName = "BtnBack";
    private const string CloseButtonName = "BtnClose";
    private const string BtnLightBoxName = "BtnLightBox";
    private const string AvailableLabelName = "LabelStatusAvailable";
    private const string NotAvailableLabelName = "LabelStatusNotAvailable";
    private const string CompleteLabelName = "LabelStatusComplete";
    private const string ScenePath = "Assets/Tests/PlayMode/TestScenes/TerminateCablesControllerTest.unity";
    private const string TerminateLightBoxTaskName = "Terminate Light Box Task";

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        // Load test scene
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

        // Setup main panel and visual tree asset
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        var uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/TerminateCablesMenu.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        // Setup UI Doc
        objMenu = new("Terminate Wires Menu Tests");
        var uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        // Controller setup
        controller = objMenu.AddComponent<TerminateCablesMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        controller.gameObject.SetActive(true);
        yield return null;

        // Event listeners
        isMenuOpenedEventInvoked = false;
        controller.Opened.AddListener(() => isMenuOpenedEventInvoked = true);

        isMenuClosedEventInvoked = false;
        controller.Closed.AddListener(() => isMenuClosedEventInvoked = true);

        isCloseButtonPressedEventInvoked = false;
        controller.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);

        isBackButtonPressedEventInvoked = false;
        controller.BackButtonPressed.AddListener(() => isBackButtonPressedEventInvoked = true);

        currentTask = Task.None;
        controller.TerminateTaskSelected.AddListener((task) => currentTask = task);
    }

    [TearDown]
    public void TearDown()
    {
        isMenuOpenedEventInvoked = false;
        isMenuClosedEventInvoked = false;
        isBackButtonPressedEventInvoked = false;
        isCloseButtonPressedEventInvoked = false;
        currentTask = Task.None;

        Object.Destroy(objMenu);
        objMenu = null;
        controller = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowMenuTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Act
        controller.Display(true);
        yield return null;

        // Assert
        Assert.IsTrue(isMenuOpenedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator HideMenuTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Act
        controller.Display(false);
        yield return null;

        // Assert
        Assert.IsTrue(isMenuClosedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ClickItemOptionTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        controller.Display(true);
        yield return null;

        // Ensure the root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        var itemOptionButton = rootContainer.Q<Button>(BtnLightBoxName);
        var itemOptionClickEvent = new NavigationSubmitEvent
        {
            target = itemOptionButton
        };

        // Act
        itemOptionButton.SendEvent(itemOptionClickEvent);
        yield return null;

        // Assert
        Assert.IsTrue(isMenuClosedEventInvoked);
        Assert.AreEqual(Task.TerminateLightBox, currentTask);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator CompleteSubTasksTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        controller.Display(true);
        yield return null;

        // Ensure the root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        // Fetch the necessary fields
        var itemOptionButton = rootContainer.Q<Button>(BtnLightBoxName);
        var availableLabel = itemOptionButton.Q<Label>(AvailableLabelName);
        var notAvailableLabel = itemOptionButton.Q<Label>(NotAvailableLabelName);
        var completeLabel = itemOptionButton.Q<Label>(CompleteLabelName);

        // Find setup task object
        GameObject terminateLightBoxObj = GameObject.Find(TerminateLightBoxTaskName);
        terminateLightBoxTask = terminateLightBoxObj.GetComponent<WireMountingTask>();

        // First Assert Before Act
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, notAvailableLabel.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, completeLabel.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, availableLabel.style.display);

        // Act
        controller.HandleAllWiresTerminated(terminateLightBoxTask);
        yield return null;

        // Last Assert
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, availableLabel.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, notAvailableLabel.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, completeLabel.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator BackButtonTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        var backButton = rootContainer.Q<Button>(BackButtonName);
        var backButtonClick = new NavigationSubmitEvent
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
    public IEnumerator CloseButtonTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        var closeButton = rootContainer.Q<Button>(CloseButtonName);
        var closeButtonClick = new NavigationSubmitEvent
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

    private void VerifyInitialValues()
    {
        Assert.IsFalse(isMenuOpenedEventInvoked);
        Assert.IsFalse(isMenuClosedEventInvoked);
        Assert.IsFalse(isBackButtonPressedEventInvoked);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);
        Assert.AreEqual(Task.None, currentTask);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }
}