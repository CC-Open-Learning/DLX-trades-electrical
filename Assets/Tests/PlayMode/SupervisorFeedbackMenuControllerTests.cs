using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class SupervisorFeedbackMenuControllerTests
{
    private GameObject objMenu;
    private SupervisorFeedbackMenuController uiController;
    private VisualElement rootContainer;

    private bool isMenuOpenedEventInvoked = false;
    private bool isMenuClosedEventInvoked = false;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
        "Assets/UI Toolkit/Feedback/SupervisorFeedbackClipboard.uxml");
        //Debug.Log("TODO replace the UXML template with SupervisorFeedbackClipboard.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        objMenu = new("General Contractor Feedback Dialog Test");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        uiController = objMenu.AddComponent<SupervisorFeedbackMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;

        isMenuOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isMenuOpenedEventInvoked = true);

        isMenuClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isMenuClosedEventInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        isMenuOpenedEventInvoked = false;
        isMenuClosedEventInvoked = false;

        Object.Destroy(objMenu);
        objMenu = null;
        uiController = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowAndHideMenuTest()
    {
        Assert.IsFalse(isMenuOpenedEventInvoked);
        Assert.IsFalse(isMenuClosedEventInvoked);

        //Show
        uiController.Display(true);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);
        Assert.IsTrue(isMenuOpenedEventInvoked);
        Assert.IsFalse(isMenuClosedEventInvoked);

        //Hide
        uiController.Display(false);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.None);
        Assert.IsTrue(isMenuClosedEventInvoked);
    }
}
