using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class SupplyCableSelectionMenuTests
{
    // Setup objects
    private GameObject objMenu;
    private TabbedTaskMenuController controller;    // Added as an instance of the SupplyCableSelectionMenuController subclass
    private VisualElement rootContainer;

    // Event listeners
    private bool isMenuOpenedEventInvoked = false;
    private bool isMenuClosedEventInvoked = false;
    private bool isCloseButtonPressedEventInvoked = false;
    private bool isBackButtonPressedEventInvoked = false;
    private Task selectedTask = Task.None;
    private MountableName selectedRoute = MountableName.None;
    private ConfirmSelectionInfo? currentConfirmSelectionInfo = null;

    // Button names
    private const string BackButtonName = UIHelper.BackButtonId;
    private const string CloseButtonName = UIHelper.CloseButtonId;
    private const string BtnFanToGangAName = "BtnFanToGangA";
    private const string BtnFanBoxToGangBoxTab = "FanBoxToGangBox";

    // Utility variables
    private const string BodyCableRoutesFanBoxToGangBox = "FanBoxToGangBoxRoutes";

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        // Setup main panel and visual tree asset
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/SupplyCableSelectionMenu.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        // Setup UI Doc
        objMenu = new("Supply Cable Selection Menu Tests");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        // Setup and enable controller
        // Added as an instance of the SupplyCableSelectionMenuController subclass
        controller = objMenu.AddComponent<SupplyCableSelectionMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        controller.gameObject.SetActive(true);
        yield return null;

        // Setup event listeners
        isMenuOpenedEventInvoked = false;
        controller.Opened.AddListener(() => isMenuOpenedEventInvoked = true);

        isMenuClosedEventInvoked = false;
        controller.Closed.AddListener(() => isMenuClosedEventInvoked = true);

        isCloseButtonPressedEventInvoked = false;
        controller.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);

        isBackButtonPressedEventInvoked = false;
        controller.BackButtonPressed.AddListener(() => isBackButtonPressedEventInvoked = true);

        selectedTask = Task.None;
        controller.TaskSelected.AddListener((newTask) => selectedTask = newTask);

        selectedRoute = MountableName.None;
        controller.ItemSelected.AddListener((route) => selectedRoute = route);

        currentConfirmSelectionInfo = null;
        controller.ConfirmInfoChanged.AddListener((cfi) => currentConfirmSelectionInfo = cfi);
    }

    [TearDown]
    public void TearDown()
    {
        // Reset event listeners
        isMenuOpenedEventInvoked = false;
        isMenuClosedEventInvoked = false;
        isCloseButtonPressedEventInvoked = false;
        isBackButtonPressedEventInvoked = false;
        selectedRoute = MountableName.None;
        selectedTask = Task.None;
        currentConfirmSelectionInfo = null;

        // Destroy all setup objects
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
    public IEnumerator ShowMenuFromPreviousTaskTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        Task previousTask = Task.ConnectFanBoxToGangBox;
        TemplateContainer body = rootContainer.Q<TemplateContainer>(BodyCableRoutesFanBoxToGangBox);

        // Act
        controller.Display(previousTask);
        yield return null;

        // Assert
        Assert.AreEqual(previousTask, selectedTask);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, body.style.display);
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
    public IEnumerator OpenDefaultTabTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange

        // Fetch the tab button
        Button tabButton = rootContainer.Q<Button>(BtnFanBoxToGangBoxTab);
        var tabButtonClick = new NavigationSubmitEvent() { target = tabButton };

        // Fetch the template container (body)
        TemplateContainer body = rootContainer.Q<TemplateContainer>(BodyCableRoutesFanBoxToGangBox);

        // Act
        tabButton.SendEvent(tabButtonClick);
        yield return null;

        // Assert
        Assert.AreEqual(Task.ConnectFanBoxToGangBox, selectedTask);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, body.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectRouteFromSelectSupplyCableMenuTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange

        ConfirmSelectionInfo expectedConfirmSelectionInfo = new()
        {
            MainLabelText = $"Confirm selection for {MountableName.FanToGangA.ToDescription()}",
            SecondaryLabelText = Task.ConnectFanBoxToGangBox.ToDescription(),
            IsPreviewing = false
        };

        // Open main menu
        controller.Display(true);
        yield return null;

        // Ensure root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        // Fetch the specific route button and assign a click event
        Button routeButton = rootContainer.Q<Button>(BtnFanToGangAName);
        var routeButtonClick = new NavigationSubmitEvent() { target = routeButton };

        // Act
        routeButton.SendEvent(routeButtonClick);
        yield return null;

        // Assert
        Assert.AreEqual(MountableName.FanToGangA, selectedRoute);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.AreEqual(expectedConfirmSelectionInfo.MainLabelText, currentConfirmSelectionInfo.Value.MainLabelText);
        Assert.AreEqual(expectedConfirmSelectionInfo.SecondaryLabelText, currentConfirmSelectionInfo.Value.SecondaryLabelText);
        Assert.AreEqual(expectedConfirmSelectionInfo.IsPreviewing, currentConfirmSelectionInfo.Value.IsPreviewing);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator BackButtonClickTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange

        // Open main menu
        controller.Display(true);
        yield return null;

        // Ensure root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        // Fetch the back button
        Button backButton = rootContainer.Q<Button>(BackButtonName);
        var backButtonClick = new NavigationSubmitEvent() { target = backButton };

        // Act
        backButton.SendEvent(backButtonClick);
        yield return null;

        // Assert
        Assert.IsTrue(isBackButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator CloseButtonClickTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange

        // Open main menu
        controller.Display(true);
        yield return null;

        // Ensure root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        // Fetch the close button
        Button closeButton = rootContainer.Q<Button>(CloseButtonName);
        var closeButtonClick = new NavigationSubmitEvent() { target = closeButton };

        // Act
        closeButton.SendEvent(closeButtonClick);
        yield return null;

        // Assert
        Assert.IsTrue(isMenuClosedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    private void VerifyInitialValues()
    {
        // Verify initial event listeners
        Assert.IsFalse(isMenuOpenedEventInvoked);
        Assert.IsFalse(isMenuClosedEventInvoked);
        Assert.IsFalse(isBackButtonPressedEventInvoked);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);
        Assert.AreEqual(Task.None, selectedTask);
        Assert.AreEqual(MountableName.None, selectedRoute);
        Assert.AreEqual(null, currentConfirmSelectionInfo);

        // Verify initial container display states
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }
}