using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;
using VARLab.TradesElectrical;

public class CustomMeshSplineExtrudeTests
{
    private const string TestScenePath = "Assets/Tests/EditMode/TestScenes/CustomSplineExtruderTest.unity";
    private const string TestGameObjectName = "Test Spline Mesh";

    private CustomMeshSplineExtrude customSplineExtruder;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private SplineInstantiate splineInstantiate;

    [SetUp]
    public void BeforeTest()
    {
        EditorSceneManager.OpenScene(TestScenePath);
        customSplineExtruder = GameObject.Find(TestGameObjectName).GetComponent<CustomMeshSplineExtrude>();
        meshCollider = customSplineExtruder.GetComponent<MeshCollider>();
        meshFilter = customSplineExtruder.GetComponent<MeshFilter>();
        splineInstantiate = customSplineExtruder.GetComponent<SplineInstantiate>();
    }

    [TearDown]
    public void AfterTest()
    {
        meshFilter.sharedMesh.Clear();
    }

    [Test]
    [Category("BuildServer")]
    public void MeshCreationTest()
    {
        Assert.IsNull(meshCollider.sharedMesh);
        Assert.IsNull(meshFilter.sharedMesh);

        splineInstantiate.UpdateInstances();
        customSplineExtruder.RegenerateMesh();

        Assert.IsNotNull(meshCollider.sharedMesh);
        Assert.IsNotNull(meshFilter.sharedMesh);
        Assert.AreEqual("poly_" + customSplineExtruder.name, meshFilter.sharedMesh.name);
    }

    [Test]
    [Category("BuildServer")]
    public void MeshClearTest()
    {
        splineInstantiate.UpdateInstances();
        customSplineExtruder.RegenerateMesh();
        Assert.IsTrue(meshFilter.sharedMesh.vertexCount > 0);
        Assert.IsTrue(meshFilter.sharedMesh.triangles.Length > 0);
        Assert.IsTrue(meshFilter.sharedMesh.normals.Length > 0);

        customSplineExtruder.ClearMesh();
        Assert.IsTrue(meshFilter.sharedMesh.vertexCount == 0);
        Assert.IsTrue(meshFilter.sharedMesh.triangles.Length == 0);
        Assert.IsTrue(meshFilter.sharedMesh.normals.Length == 0);
    }

    [Test]
    [Category("BuildServer")]
    public void VertexCountValidation()
    {
        splineInstantiate.UpdateInstances();
        customSplineExtruder.RegenerateMesh();

        GameObject shapeObj = splineInstantiate.itemsToInstantiate[0].Prefab;
        Mesh shapeMesh = shapeObj.GetComponent<MeshFilter>().sharedMesh;
        // Deduct 1 to ignore the current/parent GameObject
        int instanceCount = splineInstantiate.gameObject.GetComponentsInChildren<MeshFilter>().Length - 1;

        Assert.AreEqual(shapeMesh.vertexCount * instanceCount, meshFilter.sharedMesh.vertexCount);
    }
}
