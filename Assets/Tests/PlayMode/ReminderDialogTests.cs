using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class ReminderDialogTests
{
    private GameObject objDialog;
    private DialogController uiController;
    private VisualElement rootContainer;

    private bool isOpenedEventInvoked = false;
    private bool isClosedEventInvoked = false;
    private bool isConfirmButtonPressInvoked = false;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        PanelSettings pannelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
            "Assets/UI Toolkit/Panel Settings.asset");
        VisualTreeAsset uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Dialogs/ReminderDialog.uxml");
        objDialog = new("Reminder Dialog Test");
        UIDocument uiDoc = objDialog.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = pannelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();

        uiController = objDialog.AddComponent<DialogController>();
        rootContainer = uiDoc.rootVisualElement;
        uiController.gameObject.SetActive(true);
        yield return null;

        isOpenedEventInvoked = false;
        uiController.Opened.AddListener(() => isOpenedEventInvoked = true);

        isClosedEventInvoked = false;
        uiController.Closed.AddListener(() => isClosedEventInvoked = true);

        isConfirmButtonPressInvoked = false;
        uiController.Confirmed.AddListener(() =>
            isConfirmButtonPressInvoked = true);
    }

    [TearDown]
    public void TearDown()
    {
        isOpenedEventInvoked = false;
        isClosedEventInvoked = false;
        isConfirmButtonPressInvoked = false;

        Object.Destroy(objDialog);
        objDialog = null;
        uiController = null;
        rootContainer = null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ShowAndHideDialogTest()
    {
        Assert.IsFalse(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);

        //Show
        uiController.Display(true);
        yield return null;
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootContainer.style.display);
        Assert.IsTrue(isOpenedEventInvoked);
        Assert.IsFalse(isClosedEventInvoked);

        //Hide
        uiController.Display(false);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.None);
        Assert.IsTrue(isClosedEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ButtonTest()
    {
        uiController.Display(true);
        yield return null;
        Assert.AreEqual(rootContainer.style.display, (StyleEnum<DisplayStyle>)DisplayStyle.Flex);
        Assert.IsFalse(isConfirmButtonPressInvoked);

        Button btnConfirm = rootContainer.Q<Button>(UIHelper.ConfirmButtonId);

        var boxMountingConfirmClick = new NavigationSubmitEvent() { target = btnConfirm };

        btnConfirm.SendEvent(boxMountingConfirmClick);
        yield return null;

        Assert.IsTrue(isConfirmButtonPressInvoked);
    }
}