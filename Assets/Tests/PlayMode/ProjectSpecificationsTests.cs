using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class ProjectSpecificationsTests
{
    private GameObject objMenu;
    private MenuController uiController;
    private VisualElement rootContainer;

    private bool isMenuOpenedEventInvoked = false;
    private bool isMenuClosedEventInvoked = false;
    private bool isCloseButtonPressedEventInvoked = false;

    private const string CloseButtonName = "BtnClose";

    [UnitySetUp]
    public IEnumerator Setup()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Windows/ProjectSpecificationWindow.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        objMenu = new("Project Specifications Menu Test");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        uiController = objMenu.AddComponent<MenuController>();
        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;

        isMenuOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isMenuOpenedEventInvoked = true);

        isMenuClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isMenuClosedEventInvoked = true);

        isCloseButtonPressedEventInvoked = false;
        uiController.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);
    }

    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        isMenuOpenedEventInvoked = false;
        isMenuClosedEventInvoked = false;
        isCloseButtonPressedEventInvoked = false;
        Object.Destroy(objMenu);
        objMenu = null;
        uiController = null;
        rootContainer = null;
        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowMenuTest()
    {
        Assert.IsFalse(isMenuOpenedEventInvoked);
        uiController.Display(true);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);
        Assert.IsTrue(isMenuOpenedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator CloseMenuTest()
    {
        Assert.IsFalse(isMenuClosedEventInvoked);
        uiController.Display(false);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.None);
        Assert.IsTrue(isMenuClosedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator CloseMenuUsingCloseButtonTest()
    {
        uiController.Display(true);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);

        Button btnClose = rootContainer.Q<Button>(CloseButtonName);
        var btnCloseClick = new NavigationSubmitEvent() { target = btnClose };
        // Click on close button on main menu
        btnClose.SendEvent(btnCloseClick);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.None);
        Assert.IsTrue(isCloseButtonPressedEventInvoked);
    }
}
