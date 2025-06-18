using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class ConfirmSelectionMenuTests
{
    private const string ConfirmButtonName = ConfirmSelectionMenuController.ConfirmButtonId;
    private const string RedoButtonName = ConfirmSelectionMenuController.RedoButtonId;
    private const string MainLabelName = ConfirmSelectionMenuController.MainLabelId;
    private const string SecondaryLabelName = ConfirmSelectionMenuController.SecondaryLabelId;

    private const string InstallMainLabelText = "Is this...";
    private const string PreviewMainLabelText = "OPTION A";
    private const string PreviewSecondaryLabelText = "Preview...";
    private const string InstallSecondaryLabelText = "Install Gang Box";


    private GameObject objMenu;
    private ConfirmSelectionMenuController uiController;
    private VisualElement rootContainer;

    private bool isOpenedEventInvoked = false;
    private bool isClosedEventInvoked = false;
    private bool isConfirmEventInvoked = false;
    private bool isRedoEventInvoked = false;


    private ConfirmSelectionInfo installInfo = new()
    {
        MainLabelText = InstallMainLabelText,
        SecondaryLabelText = InstallSecondaryLabelText,
        IsPreviewing = false
    };

    private ConfirmSelectionInfo previewInfo = new()
    {
        MainLabelText = PreviewMainLabelText,
        SecondaryLabelText = PreviewSecondaryLabelText,
        IsPreviewing = true
    };



    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator UnitySetUp()
    {
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Dialogs/ConfirmDialog.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        objMenu = new("Confirm Selection Menu Test");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        uiController = objMenu.AddComponent<ConfirmSelectionMenuController>();
        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;

        isOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isOpenedEventInvoked = true);

        isClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isClosedEventInvoked = true);

        isRedoEventInvoked = false;
        uiController.RedoButtonPressed.AddListener(() => isRedoEventInvoked = true);

        isConfirmEventInvoked = false;
        uiController.ConfirmButtonPressed.AddListener(() => isConfirmEventInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(objMenu);
        objMenu = null;
        uiController = null;
        rootContainer = null;
    }

    /// <summary>
    ///     Validates base MenuController open/close functionality
    /// </summary>
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowAndHideMenuTest()
    {
        // Pre-check
        AssertInitialValues();

        //Show
        uiController.Display(true);
        yield return null;

        Assert.IsTrue(rootContainer.style.display == DisplayStyle.Flex);
        Assert.IsTrue(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);

        //Hide
        uiController.Display(false);
        yield return null;
        Assert.IsTrue(rootContainer.style.display == DisplayStyle.None);
        Assert.IsTrue(isClosedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator BaseButtonTest()
    {
        // Pre-check
        AssertInitialValues();

        // Arrange
        uiController.Display(true);
        yield return null;
        Assert.IsTrue(rootContainer.style.display == DisplayStyle.Flex);

        Button btnRedo = rootContainer.Q<Button>(RedoButtonName);
        Button btnConfirm = rootContainer.Q<Button>(ConfirmButtonName);
        var btnInstallRedoClick = new NavigationSubmitEvent() { target = btnRedo };
        var btnInstallConfirmClick = new NavigationSubmitEvent() { target = btnConfirm };

        // Act
        btnRedo.SendEvent(btnInstallRedoClick);
        btnConfirm.SendEvent(btnInstallConfirmClick);

        yield return null;

        // Assert
        Assert.IsTrue(isRedoEventInvoked);
        Assert.IsTrue(isConfirmEventInvoked);
    }

    /// <summary>
    ///     Validates the expected behaviour that when the dialog is shown in 
    ///     'install' mode (not a preview), the 'question mark' icon is shown
    ///     in the 'info icon' area (far left)
    /// </summary>
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator InstallButtonsTest()
    {
        // Pre-check
        AssertInitialValues();

        // Arrange
        uiController.Display(true);
        yield return null;
        Assert.IsTrue(rootContainer.style.display == DisplayStyle.Flex);
        
        VisualElement infoIcon = rootContainer.Q<VisualElement>(className: ConfirmSelectionMenuController.ClassInfoIcon);

        // Button setup
        Button btnRedo = rootContainer.Q<Button>(RedoButtonName);
        Button btnConfirm = rootContainer.Q<Button>(ConfirmButtonName);
        var btnInstallRedoClick = new NavigationSubmitEvent() { target = btnRedo };
        var btnInstallConfirmClick = new NavigationSubmitEvent() { target = btnConfirm };

        // Label setup
        Label mainLabel = rootContainer.Q<Label>(MainLabelName);
        Label secondaryLabel = rootContainer.Q<Label>(SecondaryLabelName);

        Assert.AreNotEqual(InstallMainLabelText, mainLabel.text);
        Assert.AreNotEqual(InstallSecondaryLabelText, secondaryLabel.text);


        // Act
        // Send a message to the UI Controller indicating a state change
        uiController.HandleConfirmSelectionInfoChange(installInfo);
        yield return null;

        btnRedo.SendEvent(btnInstallRedoClick);
        btnConfirm.SendEvent(btnInstallConfirmClick);


        // Assert
        Assert.AreEqual(InstallMainLabelText, mainLabel.text);
        Assert.AreEqual(InstallSecondaryLabelText, secondaryLabel.text);
        Assert.IsTrue(infoIcon.ClassListContains(ConfirmSelectionMenuController.ClassInfoIconQuestionMark));
        Assert.IsFalse(infoIcon.ClassListContains(ConfirmSelectionMenuController.ClassInfoIconEye));
        Assert.IsTrue(isRedoEventInvoked);
        Assert.IsTrue(isConfirmEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator PreviewButtonsTest()
    {
        // Pre-check
        AssertInitialValues();


        // Arrange
        uiController.Display(true);
        yield return null;
        Assert.IsTrue(rootContainer.style.display == DisplayStyle.Flex);

        VisualElement infoIcon = rootContainer.Q<VisualElement>(className: ConfirmSelectionMenuController.ClassInfoIcon);

        // Button setup
        Button btnRedo = rootContainer.Q<Button>(RedoButtonName);
        Button btnConfirm = rootContainer.Q<Button>(ConfirmButtonName);
        var btnInstallRedoClick = new NavigationSubmitEvent() { target = btnRedo };
        var btnInstallConfirmClick = new NavigationSubmitEvent() { target = btnConfirm };

        // Label setup
        Label mainLabel = rootContainer.Q<Label>(MainLabelName);
        Label secondaryLabel = rootContainer.Q<Label>(SecondaryLabelName);

        Assert.AreNotEqual(PreviewMainLabelText, mainLabel.text);
        Assert.AreNotEqual(PreviewSecondaryLabelText, secondaryLabel.text);


        // Act
        // Send a message to the UI Controller indicating a state change
        uiController.HandleConfirmSelectionInfoChange(installInfo);
        yield return null;

        // Simulate switching from Mounting/Installing Boxes to Previewing Cables:
        // This is to confirm that the controller is able to transition from past functionality/information
        // to newer.
        uiController.HandleConfirmSelectionInfoChange(previewInfo);
        yield return null;

        btnRedo.SendEvent(btnInstallRedoClick);
        btnConfirm.SendEvent(btnInstallConfirmClick);


        // Assert
        Assert.AreEqual(PreviewMainLabelText, mainLabel.text);
        Assert.AreEqual(PreviewSecondaryLabelText, secondaryLabel.text);
        Assert.IsTrue(infoIcon.ClassListContains(ConfirmSelectionMenuController.ClassInfoIconEye));
        Assert.IsFalse(infoIcon.ClassListContains(ConfirmSelectionMenuController.ClassInfoIconQuestionMark));
        Assert.IsTrue(isRedoEventInvoked);
        Assert.IsTrue(isConfirmEventInvoked);
    }

    /// <summary>
    ///     Asserts the expected initial state of the test
    /// </summary>
    private void AssertInitialValues()
    {
        Assert.IsFalse(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);
        Assert.IsFalse(isRedoEventInvoked);
        Assert.IsFalse(isConfirmEventInvoked);
        Assert.IsTrue(rootContainer.style.display == DisplayStyle.None);
    }
}
