using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class ToolbarControllerTests
{
    private GameObject uiGameObj;
    private ToolbarController uiController;
    private VisualElement toolbar;

    private bool isToolbarInitializedEventInvoked;
    private bool isToolbarOpenedEventInvoked;
    private bool isToolbarHiddenEventInvoked;
    private bool isProjectSpecsBtnPressedEventInvoked;
    private bool isTaskListBtnPressedEventInvoked;
    private bool isHelpBtnPressedEventInvoked;
    private bool isSettingsBtnPressedEventInvoked;

    [UnitySetUp]
    public IEnumerator BeforeTest()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Toolbar/Toolbar.uxml");


        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        uiGameObj = new GameObject("Main Toolbar");
        UIDocument uiDoc = uiGameObj.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();
        toolbar = uiDoc.rootVisualElement;

        uiController = uiGameObj.AddComponent<ToolbarController>();
        uiController.gameObject.SetActive(true);
        yield return null;

        uiController.Opened.AddListener(() => isToolbarOpenedEventInvoked = true);
        uiController.Closed.AddListener(() => isToolbarHiddenEventInvoked = true);
        uiController.ProjectSpecsButtonPressed.AddListener(() => isProjectSpecsBtnPressedEventInvoked = true);
        uiController.TaskListButtonPressed.AddListener(() => isTaskListBtnPressedEventInvoked = true);
        uiController.HelpButtonPressed.AddListener(() => isHelpBtnPressedEventInvoked = true);
        uiController.SettingsButtonPressed.AddListener(() => isSettingsBtnPressedEventInvoked = true);
    }

    [TearDown]
    public void AfterTest()
    {
        isToolbarInitializedEventInvoked = false;
        isToolbarOpenedEventInvoked = false;
        isToolbarHiddenEventInvoked = false;
        isProjectSpecsBtnPressedEventInvoked = false;
        isTaskListBtnPressedEventInvoked = false;
        isHelpBtnPressedEventInvoked = false;
        isSettingsBtnPressedEventInvoked = false;

        Object.Destroy(uiGameObj);
        uiController = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowAndHideToolbarTest()
    {
        Assert.IsFalse(isToolbarInitializedEventInvoked);
        Assert.IsFalse(isToolbarOpenedEventInvoked);
        Assert.IsFalse(isToolbarHiddenEventInvoked);

        uiController.Close();

        yield return null;
        Assert.IsTrue(isToolbarHiddenEventInvoked);
        Assert.IsFalse(isToolbarOpenedEventInvoked);
        Assert.AreEqual(DisplayStyle.None, toolbar.style.display.value);

        uiController.Open();

        yield return null;
        Assert.IsTrue(isToolbarOpenedEventInvoked);
        Assert.AreEqual(DisplayStyle.Flex, toolbar.style.display.value);

        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ToolbarButtonClickTest()
    {
        Button btnProjectSpecs = toolbar.Q<VisualElement>(ToolbarController.ProjectsSpecsButtonId).Q<Button>();
        var btnProjectSpecsClick = new NavigationSubmitEvent() { target = btnProjectSpecs };

        Button btnTaskList = toolbar.Q<VisualElement>(ToolbarController.TaskListButtonId).Q<Button>();
        var btnTaskListClick = new NavigationSubmitEvent() { target = btnTaskList };

        Button btnHelp = toolbar.Q<Button>("BtnHelp");
        var btnHelpClick = new NavigationSubmitEvent() { target = btnHelp };

        Button btnSettings = toolbar.Q<Button>("BtnSettings");
        var btnSettingsClick = new NavigationSubmitEvent() { target = btnSettings };

        Assert.IsFalse(isProjectSpecsBtnPressedEventInvoked);
        Assert.IsFalse(isTaskListBtnPressedEventInvoked);
        Assert.IsFalse(isHelpBtnPressedEventInvoked);
        Assert.IsFalse(isSettingsBtnPressedEventInvoked);

        btnProjectSpecs.SendEvent(btnProjectSpecsClick);
        yield return null;
        Assert.IsTrue(isProjectSpecsBtnPressedEventInvoked);

        btnTaskList.SendEvent(btnTaskListClick);
        yield return null;
        Debug.Log(btnTaskList.enabledSelf);
        Assert.IsTrue(isTaskListBtnPressedEventInvoked);

        btnHelp.SendEvent(btnHelpClick);
        yield return null;
        Assert.IsTrue(isHelpBtnPressedEventInvoked); 

        btnSettings.SendEvent(btnSettingsClick);
        yield return null;
        Assert.IsTrue(isSettingsBtnPressedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator FocusOnTaskListBtnTest()
    {
        Button btnTaskList = toolbar.Q<VisualElement>(ToolbarController.TaskListButtonId).Q<Button>();
        Button btnProjectSpecs = toolbar.Q<VisualElement>(ToolbarController.ProjectsSpecsButtonId).Q<Button>();
        uiController.FocusOnTaskListBtn();

        yield return null;
        Assert.IsTrue(btnTaskList.enabledSelf);
        Assert.IsTrue(btnProjectSpecs.enabledSelf);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator FocusOnProjectSpecsTest()
    {
        Button btnTaskList = toolbar.Q<VisualElement>(ToolbarController.TaskListButtonId).Q<Button>();
        Button btnProjectSpecs = toolbar.Q<VisualElement>(ToolbarController.ProjectsSpecsButtonId).Q<Button>();
        uiController.FocusOnProjectSpecs();

        yield return null;
        Assert.IsFalse(btnTaskList.enabledSelf);
        Assert.IsTrue(btnProjectSpecs.enabledSelf);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ResetFocusTest()
    {
        Button btnTaskList = toolbar.Q<VisualElement>(ToolbarController.TaskListButtonId).Q<Button>();
        Button btnProjectSpecs = toolbar.Q<VisualElement>(ToolbarController.ProjectsSpecsButtonId).Q<Button>();
        uiController.ResetFocus();

        yield return null;
        Assert.IsTrue(btnTaskList.enabledSelf);
        Assert.IsTrue(btnProjectSpecs.enabledSelf);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetTaskListBtnOnTest()
    {
        Button btnTaskList = toolbar.Q<VisualElement>(ToolbarController.TaskListButtonId).Q<Button>();

        yield return null;
        uiController.SetTaskListBtnOn(true);
        Assert.IsTrue(btnTaskList.enabledSelf);

        yield return null;
        uiController.SetTaskListBtnOn(false);
        Assert.IsFalse(btnTaskList.enabledSelf);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetProjectSpecsBtnOnTest()
    {
        Button btnProjectSpecs = toolbar.Q<VisualElement>(ToolbarController.ProjectsSpecsButtonId).Q<Button>();

        yield return null;
        uiController.SetProjectSpecsBtnOn(true);
        Assert.IsTrue(btnProjectSpecs.enabledSelf);

        yield return null;
        uiController.SetProjectSpecsBtnOn(false);
        Assert.IsFalse(btnProjectSpecs.enabledSelf);
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ResetSelectedbuttonTest()
    {
        List<VisualElement> optionButtonArray = toolbar.Query<VisualElement>(className: ToolbarController.ClassTaskButton).ToList();

        uiController.ResetSelectedButton();

        yield return null;
        foreach (VisualElement btnContainer in optionButtonArray)
        {
            Assert.IsFalse(btnContainer.ClassListContains(ToolbarController.ClassTaskButtonSelected));
        }
    }
}
