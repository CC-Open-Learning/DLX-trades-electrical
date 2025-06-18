using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VARLab.TradesElectrical;

public class MeshMergerTests
{
    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator BeforeTest()
    {
        const string testScenePath = "Assets/Tests/PlayMode/TestScenes/MeshMergerTest.unity";
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            testScenePath, new LoadSceneParameters(LoadSceneMode.Single));

        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator MergingWhenParentHasValidMesh()
    {
        var testObject = GameObject.Find("Parent Has Valid Mesh");
        MeshFilter parentMeshFilter = testObject.GetComponent<MeshFilter>();
        MeshFilter childMeshFilter = testObject.transform.GetChild(0).GetComponent<MeshFilter>();

        MeshRenderer parentMeshRenderer = parentMeshFilter.GetComponent<MeshRenderer>();

        int parentInitialVertexCount = parentMeshFilter.sharedMesh.vertexCount;
        int childVertexCount = childMeshFilter.sharedMesh.vertexCount;

        Assert.IsTrue(childMeshFilter.gameObject.activeSelf);
        Assert.IsNotNull(parentMeshFilter);
        Assert.IsNotNull(parentMeshRenderer.sharedMaterial);
        Assert.AreEqual("Lit", parentMeshRenderer.sharedMaterial.name);
        Assert.IsTrue(parentMeshFilter.sharedMesh.vertexCount > 0);

        Assert.IsTrue(parentMeshFilter.sharedMesh.vertexCount == parentInitialVertexCount);
        
        testObject.GetComponent<MeshMerger>().MergeMeshes();
        yield return null;

        Assert.IsFalse(childMeshFilter.gameObject.activeSelf);
        Assert.IsTrue(parentMeshFilter.sharedMesh.vertexCount == parentInitialVertexCount + childVertexCount);
        Assert.AreEqual("Wire Copper", parentMeshRenderer.sharedMaterial.name); // Overridden material

        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator MergingWhenParentHasEmptyMesh()
    {
        var testObject = GameObject.Find("Parent Has Empty Mesh");
        MeshFilter parentMeshFilter = testObject.GetComponent<MeshFilter>();
        MeshFilter childMeshFilter0 = testObject.transform.GetChild(0).GetComponent<MeshFilter>();
        MeshFilter childMeshFilter1 = testObject.transform.GetChild(1).GetComponent<MeshFilter>();

        int allChildVertexCount = childMeshFilter0.sharedMesh.vertexCount + childMeshFilter1.sharedMesh.vertexCount;

        Assert.IsTrue(AllChildrenWithMeshesEnabled(testObject));
        Assert.IsNull(parentMeshFilter.sharedMesh);

        MeshRenderer parentMeshRenderer = parentMeshFilter.GetComponent<MeshRenderer>();
        MeshCollider parentMeshCollider = parentMeshFilter.GetComponent<MeshCollider>();
        Assert.IsNull(parentMeshRenderer.sharedMaterial);
        Assert.IsNull(parentMeshCollider.sharedMesh);

        testObject.GetComponent<MeshMerger>().MergeMeshes();
        yield return null;

        Assert.IsNotNull(parentMeshFilter.sharedMesh);
        Assert.IsNotNull(parentMeshRenderer.sharedMaterial);
        Assert.IsNotNull(parentMeshCollider.sharedMesh);
        Assert.IsTrue(AllChildrenWithMeshesDisabled(testObject));
        Assert.IsTrue(parentMeshFilter.sharedMesh.vertexCount == allChildVertexCount);

        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator MergingWhenChildHasEmptyMesh()
    {
        var testObject = GameObject.Find("Child Has Empty Mesh");
        MeshFilter parentMeshFilter = testObject.GetComponent<MeshFilter>();
        MeshFilter childMeshFilter0 = testObject.transform.GetChild(0).GetComponent<MeshFilter>();
        MeshFilter childMeshFilter1 = testObject.transform.GetChild(1).GetComponent<MeshFilter>(); // Empty mesh
        MeshFilter childMeshFilter2 = testObject.transform.GetChild(2).GetComponent<MeshFilter>();

        int allChildVertexCount = childMeshFilter0.sharedMesh.vertexCount + childMeshFilter2.sharedMesh.vertexCount;

        Assert.IsTrue(AllChildrenWithMeshesEnabled(testObject));
        Assert.IsNull(parentMeshFilter.sharedMesh);

        testObject.GetComponent<MeshMerger>().MergeMeshes();
        yield return null;

        Assert.IsNotNull(parentMeshFilter.sharedMesh);
        Assert.IsTrue(parentMeshFilter.sharedMesh.vertexCount == allChildVertexCount);

        Assert.IsFalse(childMeshFilter0.gameObject.activeSelf);
        Assert.IsTrue(childMeshFilter1.gameObject.activeSelf); // Not merged, therefore enabled
        Assert.IsFalse(childMeshFilter2.gameObject.activeSelf);

        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator MergingOverriddenMeshes()
    {
        var testObject = GameObject.Find("Overridden Meshes");
        MeshFilter parentMeshFilter = testObject.GetComponent<MeshFilter>();
        MeshFilter cubeMesh = testObject.transform.GetChild(0).GetComponent<MeshFilter>(); // Overridden
        MeshFilter emptyMesh = testObject.transform.GetChild(1).GetComponent<MeshFilter>();
        MeshFilter sphereMesh = testObject.transform.GetChild(2).GetComponent<MeshFilter>();
        MeshFilter cylinderMesh = testObject.transform.GetChild(3).GetComponent<MeshFilter>(); // Overridden

        int allChildVertexCount = cubeMesh.sharedMesh.vertexCount + cylinderMesh.sharedMesh.vertexCount;

        Assert.IsTrue(AllChildrenWithMeshesEnabled(testObject));
        Assert.IsNull(parentMeshFilter.sharedMesh);

        testObject.GetComponent<MeshMerger>().MergeMeshes();
        yield return null;

        Assert.IsNotNull(parentMeshFilter.sharedMesh);
        Assert.IsTrue(parentMeshFilter.sharedMesh.vertexCount == allChildVertexCount);

        Assert.IsFalse(cubeMesh.gameObject.activeSelf);
        Assert.IsTrue(emptyMesh.gameObject.activeSelf); // Not merged, therefore enabled
        Assert.IsTrue(sphereMesh.gameObject.activeSelf); // Not merged, therefore enabled
        Assert.IsFalse(cylinderMesh.gameObject.activeSelf);

        yield return null;
    }

    private bool AllChildrenWithMeshesEnabled(GameObject parent)
    {
        int childCount = parent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            if (!parent.transform.GetChild(i).TryGetComponent(out MeshFilter meshFilter))
            {
                continue;
            }

            if (!meshFilter.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }

    private bool AllChildrenWithMeshesDisabled(GameObject parent)
    {
        int childCount = parent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            if (!parent.transform.GetChild(i).TryGetComponent(out MeshFilter meshFilter))
            {
                continue;
            }

            if (meshFilter.gameObject.activeSelf)
            {
                return false;
            }
        }

        return true;
    }
}
