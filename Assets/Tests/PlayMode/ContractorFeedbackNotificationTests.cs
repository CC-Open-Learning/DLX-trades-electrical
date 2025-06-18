using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class ContractorFeedbackNotificationTests
{
    // Setup objects
    private GameObject menuObject;
    private DialogController cfnController;
    private VisualElement rootContainer;

    // Event listeners
    private bool isConfirmButtonPressedEventInvoked = false;
    private bool isMenuInitializedEventInvoked = false;

    // Utility variables
    private const string ConfirmButtonName = "BtnConfirm";

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Feedback/FeedbackButton.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        menuObject = new("Contractor Feedback Notification Menu Test");
        UIDocument uiDoc = menuObject.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        cfnController = menuObject.AddComponent<DialogController>();
        rootContainer = uiDoc.rootVisualElement;
        cfnController.gameObject.SetActive(true);
        yield return null;

        isConfirmButtonPressedEventInvoked = false;
        cfnController.Confirmed.AddListener(() => isConfirmButtonPressedEventInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        isConfirmButtonPressedEventInvoked = false;
        isMenuInitializedEventInvoked = false;

        Object.Destroy(menuObject);
        menuObject = null;
        cfnController = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowAndHideMenuTest()
    {
        // Pre-act check
        AssertInitialFlags();

        // Act and Assert

        // Show Menu
        cfnController.Display(true);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);

        // Hide Menu
        cfnController.Display(false);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.None);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ConfirmButtonClicked_ExternalEventTriggered()
    {
        // Pre-arrange check
        AssertInitialFlags();

        // Arrange
        Button btnConfirm = rootContainer.Q<Button>(ConfirmButtonName);
        cfnController.Display(true);

        yield return null;

        // Ensure that the root container is properly instantiated
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);

        var btnConfirmClick = new NavigationSubmitEvent() { target = btnConfirm };

        // Act
        btnConfirm.SendEvent(btnConfirmClick);
        yield return null;

        // Assert
        Assert.IsTrue(isConfirmButtonPressedEventInvoked);
    }

    /// <summary>
    /// Helper method to assert that the listener flags have the correct initial values set before act
    /// </summary>
    private void AssertInitialFlags()
    {
        Assert.IsFalse(isConfirmButtonPressedEventInvoked);
        Assert.IsFalse(isMenuInitializedEventInvoked);
    }
}