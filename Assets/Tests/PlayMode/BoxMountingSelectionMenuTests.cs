using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEditor;
using VARLab.TradesElectrical;
using System.Collections.Generic;
using System.Linq;

public class BoxMountingSelectionMenuTests
{
    private GameObject objMenu;
    private TabbedTaskMenuController uiController;
    private VisualElement rootContainer;
    private readonly List<TemplateContainer> subMenus = new();

    private bool isMenuOpenedEventInvoked = false;
    private bool isMenuClosedEventInvoked = false;
    private bool isMenuDiscardedEventInvoked = false;
    private bool isCloseButtonPressedEventInvoked = false;
    private bool isBackButtonPressedEventInvoked = false;
    private Task selectedTask = Task.None;
    private Task defaultTabOpenedOn;
    private MountableName selectedItem = MountableName.None;
    private Task disabledtask = Task.MountFanBox;

    private const string BackButtonName = UIHelper.BackButtonId;
    private const string CloseButtonName = UIHelper.CloseButtonId;
    private const string BackButtonContainerName = UIHelper.NavigationContainerId;
    private const string GangBoxesSubBodyName = "MountGangBoxOptions";
    private const string FanBoxesSubBodyName = "MountFanBoxOptions";
    private const string GangBoxesTabButtonName = "MountGangBox";
    private const string TwoGangBoxButtonName = "BtnTwoGangBox";

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Tasks/BoxMountingSelectionMenu.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        objMenu = new("Item Selection Menu Test");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        uiController = objMenu.AddComponent<BoxMountingSelectionMenuController>();

        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;

        defaultTabOpenedOn = Task.MountFanBox;
        
        rootContainer.Query<TemplateContainer>(className: "sub").ForEach(subMenu =>
        {
            subMenus.Add(subMenu);
        });

        isMenuOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isMenuOpenedEventInvoked = true);
        
        isMenuClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isMenuClosedEventInvoked = true);
        
        isCloseButtonPressedEventInvoked = false;
        uiController.CloseButtonPressed.AddListener(() => isCloseButtonPressedEventInvoked = true);

        isBackButtonPressedEventInvoked = false;
        uiController.BackButtonPressed.AddListener(() => isBackButtonPressedEventInvoked = true);

        isMenuDiscardedEventInvoked = false;
        uiController.Cancelled.AddListener(() => isMenuDiscardedEventInvoked = true);

        selectedTask = Task.None;
        uiController.TaskSelected.AddListener((newTask) => selectedTask = newTask);

        selectedItem = MountableName.None;
        uiController.ItemSelected.AddListener((item) => { selectedItem = item; });
    }

    [TearDown]
    public void TearDown()
    {
        isMenuOpenedEventInvoked = false;
        isMenuClosedEventInvoked = false;
        isMenuDiscardedEventInvoked = false;
        isCloseButtonPressedEventInvoked = false;
        isBackButtonPressedEventInvoked = false;
        selectedItem = MountableName.None;
        selectedTask = Task.None;

        Object.Destroy(objMenu);
        objMenu = null;
        uiController = null;
        rootContainer = null;
        subMenus.Clear();
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_Display_ShowAndHide()
    {
        Assert.IsFalse(isMenuOpenedEventInvoked);
        Assert.IsFalse(isMenuClosedEventInvoked);

        // Show
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual(defaultTabOpenedOn, selectedTask);

        Assert.IsTrue(isMenuOpenedEventInvoked);
        Assert.IsFalse(isMenuClosedEventInvoked);

        // Hide
        uiController.Display(false);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.IsTrue(isMenuClosedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_OpenSpecificTabs()
    {
        var fanBoxTab = uiController.GetTab(Task.MountFanBox);
        var gangBoxTab = uiController.GetTab(Task.MountGangBox);
        var fanBoxTabContainer = uiController.GetContainer(Task.MountFanBox);
        var gangBoxTabContainer = uiController.GetContainer(Task.MountGangBox);

        // Pre-check, menu is disabled
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);

        uiController.Display(Task.MountGangBox);
        yield return null;

        // Menu displayed, visual style should be flex
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);

        // Gang box contents visible, fan box contents hidden
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, gangBoxTabContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, fanBoxTabContainer.style.display);

        // Gang box tab enabled, fan box tab disabled
        Assert.AreEqual(true, gangBoxTab.enabledSelf);
        Assert.AreEqual(false, fanBoxTab.enabledSelf);

        // MountGangBox is the currently selected task
        Assert.AreEqual(Task.MountGangBox, selectedTask);


        // Switch to fan box tab
        uiController.Display(Task.MountFanBox);
        yield return null;

        // Fan box contents visible, gang box contents hidden
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, fanBoxTabContainer.style.display); 
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, gangBoxTabContainer.style.display);

        Assert.AreEqual(true, fanBoxTab.enabledSelf);
        Assert.AreEqual(false, gangBoxTab.enabledSelf);

        Assert.AreEqual(Task.MountFanBox, selectedTask);

    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_OpenTabContents()
    {
        var defaultBodySubMenu = rootContainer.Q<TemplateContainer>(FanBoxesSubBodyName);
        var selectedBodySubMenu = rootContainer.Q<TemplateContainer>(GangBoxesSubBodyName);
        
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, defaultBodySubMenu.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, selectedBodySubMenu.style.display);
        Assert.AreEqual(uiController.TaskTabs.First(), selectedTask);

        Button btnBoxes = rootContainer.Q<Button>(GangBoxesTabButtonName);
        var btnBoxesClick = new NavigationSubmitEvent() { target = btnBoxes };
        // Click on Select Gang Box on main menu
        btnBoxes.SendEvent(btnBoxesClick);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display );
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, selectedBodySubMenu.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, defaultBodySubMenu.style.display);
        Assert.AreEqual(Task.MountGangBox, selectedTask);
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Navigation_BackButtonClosesMenu()
    {
        // Opens menu to GangBox tab, all others are still enabled
        yield return OpenTabContents();

        var backBtnContainer = rootContainer.Q<VisualElement>(BackButtonContainerName);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, backBtnContainer.style.display);

        Assert.IsFalse(isMenuDiscardedEventInvoked);
        Assert.IsFalse(isBackButtonPressedEventInvoked);

        Button backBtn = rootContainer.Q<Button>(BackButtonName);
        var backBtnClick = new NavigationSubmitEvent() { target = backBtn };
        // Click on back button
        backBtn.SendEvent(backBtnClick);
        yield return null;

        Assert.IsTrue(isMenuDiscardedEventInvoked);
        Assert.IsTrue(isBackButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Navigation_OpenFocusTab_NavigationDisabled()
    {
        var backBtnContainer = rootContainer.Q<VisualElement>(BackButtonContainerName);

        yield return OpenTabContents();

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, backBtnContainer.style.display);


        // Opens menu to GangBox tab, all others are still enabled
        uiController.Display(Task.MountLightBox);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, backBtnContainer.style.display);

        yield return OpenTabContents();

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, backBtnContainer.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_CloseWithButton()
    {
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.IsFalse(isCloseButtonPressedEventInvoked);
        Assert.IsFalse(isMenuDiscardedEventInvoked);
        
        Button btnClose = rootContainer.Q<Button>(CloseButtonName);
        var btnCloseClick = new NavigationSubmitEvent() { target = btnClose };
        // Click on close button
        btnClose.SendEvent(btnCloseClick);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.IsTrue(isCloseButtonPressedEventInvoked);
        Assert.IsTrue(isMenuDiscardedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_SelectTaskOption()
    {
        yield return OpenTabContents();
        Assert.AreEqual(selectedItem, MountableName.None);

        Button btn2GangBox = rootContainer.Q<Button>(TwoGangBoxButtonName);
        var btn2GangBoxClick = new NavigationSubmitEvent() { target = btn2GangBox };
        // Click on "2-1/2" 2-Gang Box" button on "Select a Box" sub menu
        btn2GangBox.SendEvent(btn2GangBoxClick);
        yield return null;

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootContainer.style.display);
        Assert.AreEqual(selectedItem, MountableName.TwoGangBox);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_DisableAllTaskOptions()
    {
        var fanBoxContainer = rootContainer.Q<TemplateContainer>(FanBoxesSubBodyName);
        
        uiController.Display(true);
        uiController.SetTaskOptionsEnabled(disabledtask, false);
        yield return null;
        Button btn1 = fanBoxContainer.Q<Button>("BtnOctagonalBox");
        Button btn2 = fanBoxContainer.Q<Button>("BtnSquareBox");
        Button btn3 = fanBoxContainer.Q<Button>("BtnFanBox");
        Button btn4 = fanBoxContainer.Q<Button>("BtnOctagonalBracketBox");

        Assert.IsTrue(btn1.enabledSelf == false);
        Assert.IsTrue(btn2.enabledSelf == false);
        Assert.IsTrue(btn3.enabledSelf == false);
        Assert.IsTrue(btn4.enabledSelf == false);
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_DisableSingleOption()
    {
        Task targetTask = Task.MountFanBox;
        MountableName targetMountable = MountableName.OctagonalBox;

        var fanBoxContent = uiController.GetContainer(targetTask);
        uiController.Display(true);

        var options = uiController.GetTaskOptions(targetTask);

        var btn = options.First(element => element.viewDataKey == targetMountable.ToString());

        uiController.SetTaskOptionEnabled(targetTask, targetMountable, false);
        yield return null;

        Assert.IsFalse(btn.enabledSelf);
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_DisableSingleOption_AllOthersEnabled()
    {
        Task targetTask = Task.MountFanBox;
        MountableName targetMountable = MountableName.OctagonalBox;

        var fanBoxContent = uiController.GetContainer(targetTask);
        uiController.Display(true);

        var options = uiController.GetTaskOptions(targetTask);

        var btn = options.First(element => element.viewDataKey == targetMountable.ToString());

        uiController.SetTaskOptionEnabledInvert(targetTask, targetMountable, false);
        yield return null;

        Assert.IsFalse(btn.enabledSelf);

        foreach (var option in options.Except(new List<VisualElement>() { btn }))
        {
            Assert.IsTrue(option.enabledSelf);
        }
    }



    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_EnableOnlySelectedOption()
    {
        var bodySubMenu = rootContainer.Q<TemplateContainer>(FanBoxesSubBodyName);
        uiController.Display(true);
        yield return null;

        List<VisualElement> options = uiController.GetTaskOptions(Task.MountFanBox);

        var optionFanBoxOctagonalBracketBox = options.First();


        // The first button option should map to the octagonal bracket box, based on data key
        Assert.AreEqual(MountableName.OctagonalBracketBox.ToString(), optionFanBoxOctagonalBracketBox.viewDataKey);

        // Trigger click event on that option
        optionFanBoxOctagonalBracketBox.SendEvent(
            new NavigationSubmitEvent() { target = optionFanBoxOctagonalBracketBox });

        uiController.SetTaskOptionEnabledInvert(Task.MountFanBox, MountableName.OctagonalBracketBox, true);
        yield return null;

        Assert.IsTrue(optionFanBoxOctagonalBracketBox.enabledSelf);

        foreach (var opt in options.Except(new List<VisualElement>() { optionFanBoxOctagonalBracketBox }))
        {
            Assert.IsFalse(opt.enabledSelf);
        }
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Menu_EnableWithNoTask_SafeDefault()
    {
        Task expectedTask = Task.MountFanBox;
        Task emptyTask = Task.None;

        uiController.Display(emptyTask);
        yield return null;

        // Expecting to open to FanBoxes if no task is specified
        var fanBoxContainer = uiController.GetContainer(expectedTask);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, fanBoxContainer.style.display);
    }


    [Test]
    [Category("BuildServer")]
    public void Tabs_GetTab()
    {
        var tabGet = uiController.GetTab(Task.MountGangBox);
        var tabExpect = rootContainer.Q<VisualElement>(GangBoxesTabButtonName);

        Assert.AreEqual(tabExpect, tabGet);
    }

    [Test]
    [Category("BuildServer")]
    public void Tabs_GetTab_FromContainer()
    {
        // Have to find it as a template container
        var container = rootContainer.Q<TemplateContainer>(GangBoxesSubBodyName); 

        var tabGet = uiController.GetTab(container);
        var tabExpect = rootContainer.Q<VisualElement>(GangBoxesTabButtonName);

        Assert.AreEqual(tabExpect, tabGet);
    }

    [Test]
    [Category("BuildServer")]
    public void Tabs_GetContainer()
    {
        var containerGet = uiController.GetContainer(Task.MountGangBox);
        var containerExpect = rootContainer.Q<TemplateContainer>(GangBoxesSubBodyName);

        Assert.AreEqual(containerExpect, containerGet);
    }

    [Test]
    [Category("BuildServer")]
    public void Tabs_GetContainer_FromTab()
    {
        // Have to find it as a template container
        var tab = rootContainer.Q<VisualElement>(GangBoxesTabButtonName);

        var containerGet = uiController.GetContainer(tab);
        var containerExpect = rootContainer.Q<TemplateContainer>(GangBoxesSubBodyName);

        Assert.AreEqual(containerExpect, containerGet);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator Tabs_SetDefaultTab()
    {
        var bodySubMenu = rootContainer.Q<TemplateContainer>(GangBoxesSubBodyName);
        Task newTask = Task.MountGangBox;
        uiController.SetDefaultTab(newTask);
        uiController.Display(true);
        yield return null;

        Assert.IsTrue(bodySubMenu.style.display == DisplayStyle.Flex);
    }

    private IEnumerator OpenTabContents()
    {
        uiController.Display(true);

        Button btnBoxes = rootContainer.Q<Button>(GangBoxesTabButtonName);
        var btnBoxesClick = new NavigationSubmitEvent() { target = btnBoxes };
        // Click on Select Gang Box button on main menu
        btnBoxes.SendEvent(btnBoxesClick);
        yield return null;
    }
}
