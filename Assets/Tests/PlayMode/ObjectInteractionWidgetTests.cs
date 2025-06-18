using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;
using static VARLab.TradesElectrical.ObjectInteractionWidget;

/// <summary>
///     Provides a test suite for the EditMountedObjectUI class, ensuring its UI and functionality work as
///     expected.
/// </summary>
public class ObjectInteractionWidgetTests
{
    // Setup objects
    private GameObject uiGameObj;
    private ObjectInteractionWidget interactionWidgetController;
    private VisualElement rootElement;

    // Event listeners
    private bool isDialogOpenedEventInvoked = false;
    private bool isDialogClosedEventInvoked = false;
    private bool isMoveButtonPressedEventInvoked = false;
    private bool isReplaceButtonPressedEventInvoked = false;
    private bool isGoBackButtonPressedEventInvoked = false;

    // Utility variables for tests
    private const string ExpectedGoBackText = "Go Back";
    private const string ReplaceText = "Replace ...";
    private const string MoveText = "Move ...";

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator BeforeTest()
    {
        // Setup panel settings and visual tree asset
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        var uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Dialogs/ObjectInteractionWidget.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }

        // Setup UI document
        uiGameObj = new GameObject("Edit Mounted Object Dialog");
        var uiDoc = uiGameObj.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();
        rootElement = uiDoc.rootVisualElement;

        // Setup UI controller
        interactionWidgetController = uiGameObj.AddComponent<ObjectInteractionWidget>();
        interactionWidgetController.gameObject.SetActive(true);
        yield return null;

        // Event listeners
        isDialogOpenedEventInvoked = false;
        interactionWidgetController.WidgetOpened.AddListener(() => isDialogOpenedEventInvoked = true);

        isDialogClosedEventInvoked = false;
        interactionWidgetController.WidgetClosed.AddListener(() => isDialogClosedEventInvoked = true);
    }

    [TearDown]
    [Category("BuildServer")]
    public void AfterTest()
    {
        isDialogOpenedEventInvoked = false;
        isDialogClosedEventInvoked = false;
        isMoveButtonPressedEventInvoked = false;
        isReplaceButtonPressedEventInvoked = false;
        isGoBackButtonPressedEventInvoked = false;

        Object.Destroy(uiGameObj);
        interactionWidgetController = null;
        rootElement = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowAndHideDialogTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Show Act
        interactionWidgetController.Open();
        yield return null;

        // Show Assert
        Assert.IsTrue(isDialogOpenedEventInvoked);
        Assert.IsFalse(isDialogClosedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);

        // Hide Act
        interactionWidgetController.Close();
        yield return null;

        // Hide Assert
        Assert.IsTrue(isDialogClosedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ClickButtonTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Ensure the root container is visible and expected flags haven't changed
        Assert.IsFalse(isMoveButtonPressedEventInvoked);
        Assert.IsFalse(isReplaceButtonPressedEventInvoked);
        Assert.IsFalse(isGoBackButtonPressedEventInvoked);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);

        var btnMove = rootElement.Q<Button>(MoveButtonName);
        var btnReplace = rootElement.Q<Button>(ReplaceButtonName);
        var btnGoBack = rootElement.Q<Button>(GoBackButtonName);

        var btnMoveClick = new NavigationSubmitEvent() { target = btnMove };
        var btnReplaceClick = new NavigationSubmitEvent() { target = btnReplace };
        var btnGoBackClick = new NavigationSubmitEvent() { target = btnGoBack };

        Dictionary<Button, NavigationSubmitEvent> buttonSubmitEvents = new()
        {
            { btnMove, btnMoveClick },
            { btnReplace, btnReplaceClick },
            { btnGoBack, btnGoBackClick },
        };

        // Click each button in buttonSubmitEvents
        foreach (var kvp in buttonSubmitEvents)
        {
            // Arrange: We have to setup each time before simulating clicks as the widget will close when clicking
            // on each button
            interactionWidgetController.AddButton(ButtonType.Move, () => isMoveButtonPressedEventInvoked = true);
            interactionWidgetController.AddButton(ButtonType.Replace, () => isReplaceButtonPressedEventInvoked = true);
            interactionWidgetController.AddButton(ButtonType.GoBack, () => isGoBackButtonPressedEventInvoked = true);

            interactionWidgetController.Open();
            yield return null;
            Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);

            // Act
            kvp.Key.SendEvent(kvp.Value);
            yield return null;
        }

        // Assert
        Assert.IsTrue(isMoveButtonPressedEventInvoked);
        Assert.IsTrue(isReplaceButtonPressedEventInvoked);
        Assert.IsTrue(isGoBackButtonPressedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetButtonTextTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        interactionWidgetController.Open();
        yield return null;

        // Ensure the root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);

        var btnMove = rootElement.Q<Button>(MoveButtonName);
        var btnReplace = rootElement.Q<Button>(ReplaceButtonName);
        var btnCancel = rootElement.Q<Button>(GoBackButtonName);

        var moveBtnLabel = btnMove.Q<Label>();
        var replaceBtnLabel = btnReplace.Q<Label>();
        var cancelBtnLabel = btnCancel.Q<Label>();

        interactionWidgetController.AddButton(ButtonType.Move, () => isMoveButtonPressedEventInvoked = true);
        interactionWidgetController.AddButton(ButtonType.Replace, () => isReplaceButtonPressedEventInvoked = true);

        // Act
        interactionWidgetController.SetButtonText(ButtonType.Move, MoveText);
        interactionWidgetController.SetButtonText(ButtonType.Replace, ReplaceText);
        yield return null;

        // Assert
        Assert.AreEqual(MoveText, moveBtnLabel.text);
        Assert.AreEqual(ReplaceText, replaceBtnLabel.text);
        Assert.AreEqual(ExpectedGoBackText, cancelBtnLabel.text);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetButtonEnabledStateTest()
    {
        // Pre-arrange check
        VerifyInitialValues();

        // Arrange
        interactionWidgetController.Open();
        yield return null;

        // Ensure the root container is visible
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);

        var btnMove = rootElement.Q<Button>(MoveButtonName);
        var btnReplace = rootElement.Q<Button>(ReplaceButtonName);
        var btnCancel = rootElement.Q<Button>(GoBackButtonName);

        interactionWidgetController.AddButton(ButtonType.Move, () => isMoveButtonPressedEventInvoked = true);
        interactionWidgetController.AddButton(ButtonType.Replace, () => isReplaceButtonPressedEventInvoked = true);
        interactionWidgetController.AddButton(ButtonType.GoBack, () => isGoBackButtonPressedEventInvoked = true);

        // Act (Deactivate)
        interactionWidgetController.SetButtonEnabled(ButtonType.Move, false);
        interactionWidgetController.SetButtonEnabled(ButtonType.Replace, false);
        interactionWidgetController.SetButtonEnabled(ButtonType.GoBack, false);
        yield return null;

        // Assert (Deactivate)
        Assert.IsFalse(btnMove.enabledInHierarchy);
        Assert.IsFalse(btnReplace.enabledInHierarchy);
        Assert.IsFalse(btnCancel.enabledInHierarchy);

        // Act (Activate)
        interactionWidgetController.SetButtonEnabled(ButtonType.Move, true);
        interactionWidgetController.SetButtonEnabled(ButtonType.Replace, true);
        interactionWidgetController.SetButtonEnabled(ButtonType.GoBack, true);
        yield return null;

        // Assert (Activate)
        Assert.IsTrue(btnMove.enabledInHierarchy);
        Assert.IsTrue(btnReplace.enabledInHierarchy);
        Assert.IsTrue(btnCancel.enabledInHierarchy);
    }

    private void VerifyInitialValues()
    {
        Assert.IsFalse(isDialogOpenedEventInvoked);
        Assert.IsFalse(isDialogClosedEventInvoked);
        Assert.IsFalse(isMoveButtonPressedEventInvoked);
        Assert.IsFalse(isReplaceButtonPressedEventInvoked);
        Assert.IsFalse(isGoBackButtonPressedEventInvoked);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);
    }
}