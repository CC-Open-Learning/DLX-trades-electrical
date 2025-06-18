using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

/// <summary>
///     Basic test suite that ensures that the overriden functionality and fields in the Device Selection Menu
///     work as expected
/// </summary>
public class DeviceSelectionMenuControllerTests
{
    // Setup objects
    private GameObject objMenu;
    private DeviceSelectionMenuController uiController;
    private VisualElement rootContainer;

    private const Task DefaultTab = Task.InstallFan; // Default/Fallback Tab
    
    private const string InstallTaskHeaderPrefix = "Install ";
    private const string InstallTaskConfirmationPrefix = "Confirm selection for ";
    
    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/DeviceSelectionMenu.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        objMenu = new("Device Selection Menu Test");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        uiController = objMenu.AddComponent<DeviceSelectionMenuController>();

        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;
    }
    
    [TearDown]
    public void TearDown()
    {
        Object.Destroy(objMenu);
        objMenu = null;
        uiController = null;
        rootContainer = null;
    }
    
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_ShowAndHide()
    {
        // Arrange
        VisualElement exhaustFanTab = uiController.GetTab(DefaultTab);
        VisualElement exhaustFanContainer = uiController.GetContainer(DefaultTab);
        
        // Show
        uiController.Display(true);
        yield return null;
        
        // Assert
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, exhaustFanContainer.style.display);
        Assert.That(exhaustFanTab.enabledSelf);
        
        // Hide
        uiController.Display(false);
        yield return null;

        // Assert
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Display_FallbackTabOpened()
    {
        // Arrange
        VisualElement exhaustFanTab = uiController.GetTab(DefaultTab);
        VisualElement exhaustFanContainer = uiController.GetContainer(DefaultTab);
        
        // Show
        uiController.Display(Task.None);
        yield return null;
        
        // Assert
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, exhaustFanContainer.style.display);
        Assert.That(exhaustFanTab.enabledSelf);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator GenerateSelectionInfo_ReturnsProperDataObject()
    {
        // Arrange
        string mainLabelText = InstallTaskConfirmationPrefix + MountableName.ExhaustFan14In.ToDescription();
        string secondaryLabelText = InstallTaskHeaderPrefix + DefaultTab.ToDescription();
        
        // Act
        var selectionInfoDataObj = uiController.GenerateSelectionInfo(MountableName.ExhaustFan14In, DefaultTab);
        yield return null;
        
        // Assert
        Assert.AreEqual(mainLabelText, selectionInfoDataObj.MainLabelText);
        Assert.AreEqual(secondaryLabelText, selectionInfoDataObj.SecondaryLabelText);
    }
}
