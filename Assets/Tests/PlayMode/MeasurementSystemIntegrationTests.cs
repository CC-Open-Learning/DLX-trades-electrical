using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.StandardUILibrary;
using VARLab.TradesElectrical;

public class MeasurementSystemIntegrationTests
{
    private MeasurementSystem measurementSystem;
    private GameObject mountableObj;
    private MeasurementMarkers markers;

    private const string MeasurementLineObjName = "Measurement Marker Line";
    private const string MeasurementTooltipObjName = "Measurement Tooltip";
    private const float ActualMarkerLength = 1.39f;

    [UnitySetUp]
    public IEnumerator BeforeTest()
    {
        // Create the mountable GameObject
        mountableObj = new("Mountable Object");
        mountableObj.transform.position = new(0f, 1f, 0f);
        markers = mountableObj.AddComponent<MeasurementMarkers>();

        SerializedObject markersSo = new(markers);
        markersSo.FindProperty("downMarker.length").floatValue = ActualMarkerLength;
        markersSo.ApplyModifiedProperties();

        // Add Meshes GameObject with a Box Collider
        GameObject meshesObj = new("Meshes");
        meshesObj.transform.parent = mountableObj.transform;
        meshesObj.AddComponent<BoxCollider>();
        meshesObj.SetActive(true);

        mountableObj.SetActive(true);
        yield return null;

        // Create a Main Camera
        GameObject mainCamObj = new("Main Camera");
        mainCamObj.transform.position = new(0f, 0f, 5f);
        mainCamObj.transform.LookAt(mountableObj.transform, Vector3.up);
        Camera cam = mainCamObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        mainCamObj.SetActive(true);

        // Create Measurement System GameObject
        GameObject measurementSysObj = new("Measurement System");
        measurementSystem = measurementSysObj.AddComponent<MeasurementSystem>();
        SerializedObject measurementSysSo = new(measurementSystem);
        LineRenderer lineReference = AssetDatabase.LoadAssetAtPath<LineRenderer>(
            "Assets/Prefab/Measurement/Line Reference.prefab");
        TooltipSimple tooltipReference = AssetDatabase.LoadAssetAtPath<TooltipSimple>(
            "Assets/Prefab/Measurement/Tooltip Reference.prefab");
        measurementSysSo.FindProperty("lineRendererRef").objectReferenceValue = lineReference;
        measurementSysSo.FindProperty("tooltipRef").objectReferenceValue = tooltipReference;
        measurementSysSo.ApplyModifiedProperties();
        measurementSysObj.SetActive(true);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ShowAndHideSingleMeasurementTest()
    {
        Assert.IsNull(GameObject.Find(MeasurementLineObjName));
        Assert.IsNull(GameObject.Find(MeasurementTooltipObjName));

        // Show measurements
        measurementSystem.OnDisplayMeasurements(markers, mountableObj.GetComponentInChildren<Collider>());
        yield return new WaitForSeconds(0.2f);

        Assert.IsNotNull(GameObject.Find(MeasurementLineObjName));
        GameObject tooltipObj = GameObject.Find(MeasurementTooltipObjName);
        Assert.IsNotNull(tooltipObj);

        VisualElement tooltipRoot = tooltipObj.GetComponent<UIDocument>().rootVisualElement;
        VisualElement tooltipBody = tooltipRoot.Q<VisualElement>("Tooltip");
        Label tooltipLabel = tooltipRoot.Q<Label>();
        Assert.AreEqual(DisplayStyle.Flex, tooltipBody.style.display.value);
        StringAssert.Contains(ActualMarkerLength.ToString(), tooltipLabel.text);

        // Hide measurements
        measurementSystem.OnHideMarkers();
        yield return null;

        Assert.IsNull(GameObject.Find(MeasurementLineObjName));
        Assert.IsNull(GameObject.Find(MeasurementTooltipObjName));
    }
}
