using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class ToastMessageUITests
{
    private GameObject objMenu;
    private ToastMessageUI uiController;
    private VisualElement rootContainer;
    private ToastMessageType msgtype;
    private VisualElement infoIcon;
    private VisualElement successIcon;
    private VisualElement errorIcon;
    [UnitySetUp]
    public IEnumerator Setup()
    {
        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
           "Assets/UI Toolkit/Panel Settings.asset");
        var uxmlTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/UI Toolkit/Toast Messages/ToastMessageTopAnchor.uxml");

        if (panelSettings == null) { throw new System.ArgumentNullException(nameof(panelSettings)); }

        if (uxmlTemplate == null) { throw new System.ArgumentNullException(nameof(uxmlTemplate)); }


        objMenu = new("Toast Message Test");
        UIDocument uiDoc = objMenu.AddComponent<UIDocument>();
        SerializedObject soUiDoc = new(uiDoc);
        soUiDoc.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        soUiDoc.FindProperty("sourceAsset").objectReferenceValue = uxmlTemplate;
        soUiDoc.ApplyModifiedProperties();
        rootContainer = uiDoc.rootVisualElement;
        uiController = objMenu.AddComponent<ToastMessageUI>();
        uiController.gameObject.SetActive(true);
        yield return null;
        infoIcon = rootContainer.Q<VisualElement>("InfoIcon");
        errorIcon = rootContainer.Q<VisualElement>("ErrorIcon");
        successIcon = rootContainer.Q<VisualElement>("SuccessIcon");
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetTextCorrectly()
    {
        msgtype = ToastMessageType.Success;
        string text = "hello world";
        uiController.Show(msgtype, text);
        yield return null;
        Assert.AreEqual(rootContainer.Q<Label>("Message").text,text);

    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetCorrectIcon()
    {
        msgtype = ToastMessageType.Success;
        string text = "hello world";
        uiController.Show(msgtype, text);
        yield return null;
        Assert.IsTrue(successIcon.style.display == DisplayStyle.Flex);

    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetInfoIcon()
    {
        msgtype = ToastMessageType.Info;
        string text = "hello world";
        uiController.Show(msgtype, text);
        yield return null;
        Assert.IsTrue(infoIcon.style.display == DisplayStyle.Flex);

    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SetErrorIcon()
    {
        msgtype = ToastMessageType.Error;
        string text = "hello world";
        uiController.Show(msgtype, text);
        yield return null;
        Assert.IsTrue(errorIcon.style.display == DisplayStyle.Flex);

    }
}
