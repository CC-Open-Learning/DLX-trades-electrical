using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VARLab.TradesElectrical;
using Assert = UnityEngine.Assertions.Assert;


public class SceneInteractionsTest
{
    private bool isClickOnMountLocationEventInvoked;
    private bool isClickOnMountableEventInvoked;
    private bool isClickOnWireEventInvoked;
    private bool isClickOnTerminationOptionEventInvoked;
    private SceneInteractions sceneInteractions;
    private GameObject[] allObjects;
    
    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        // Setup the Scene Interactions Script
        string testScenePath = "Assets/Tests/PlayMode/TestScenes/SceneInteractionsTestScene.unity";
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            testScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        
        sceneInteractions = GameObject.Find("Scene Interactions").GetComponent<SceneInteractions>();
        allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        
        isClickOnMountLocationEventInvoked = false;
        sceneInteractions.ClickOnMountLocation.AddListener((mountLocation) => isClickOnMountLocationEventInvoked = true);

        isClickOnMountableEventInvoked = false;
        sceneInteractions.ClickOnMountable.AddListener((mountable) => isClickOnMountableEventInvoked = true);        
        
        isClickOnWireEventInvoked = false;
        sceneInteractions.ClickOnWire.AddListener((conductor) => isClickOnWireEventInvoked = true);
        
        isClickOnTerminationOptionEventInvoked = false;
        sceneInteractions.ClickOnTerminationOption.AddListener((terminationOption) => isClickOnTerminationOptionEventInvoked = true);
        
        yield return null;
    }
    
    [TearDown]
    [Category("BuildServer")]
    public void AfterTest()
    {
        SceneManager.UnloadSceneAsync("SceneInteractionsTestScene");
    }
    
    GameObject FindInactiveObject(string objectName)
    {
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == objectName)
            {
                return obj;
            }
        }
        return null;
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator InteractWithMountLocation()
    {
        // Set up game object
        GameObject mountLocationObject = FindInactiveObject("Above Entry Door"); 
        MountLocation location = mountLocationObject.GetComponent<MountLocation>();
        location.Enable(true);
        mountLocationObject.SetActive(true);
        mountLocationObject.transform.parent.gameObject.SetActive(true);
        yield return null;
        
        // Check if Components correct
        Assert.IsTrue(mountLocationObject.activeSelf);
        Assert.IsTrue(mountLocationObject.GetComponent<BoxCollider>().enabled);
        Assert.IsNotNull(mountLocationObject.GetComponent<MeasurementMarkers>());
        
        Vector3 screenPositionOfObject = Camera.main.WorldToScreenPoint(mountLocationObject.transform.position);
        
        sceneInteractions.ProcessMouseClickUp(screenPositionOfObject);
        yield return new WaitForSeconds(0.1f);

        Assert.IsTrue(isClickOnMountLocationEventInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator InteractWithMountable()
    {
        GameObject mountableGameObj = FindInactiveObject("Fan Box to Gang Box Correct"); 
        mountableGameObj.SetActive(true);
        
        Assert.IsTrue(mountableGameObj.activeSelf);
        Assert.IsTrue(mountableGameObj.GetComponent<MeshCollider>().enabled);
        Vector3 screenPositionOfObject = Camera.main.WorldToScreenPoint(mountableGameObj.transform.position);
        
        sceneInteractions.ProcessMouseClickUp(screenPositionOfObject);
        yield return new WaitForSeconds(0.1f);
    
        Assert.IsTrue(isClickOnMountableEventInvoked);
        
    }
    
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator InteractWithConductor()
    {
        GameObject mountableGameObj = FindInactiveObject("Wire_Device_Bond_Initial_fake");
        mountableGameObj.SetActive(true);
        
        Assert.IsTrue(mountableGameObj.activeSelf);
        Assert.IsTrue(mountableGameObj.GetComponent<BoxCollider>().enabled);
        Vector3 screenPositionOfObject = Camera.main.WorldToScreenPoint(mountableGameObj.transform.position);
        
        sceneInteractions.ProcessMouseClickUp(screenPositionOfObject);
        yield return new WaitForSeconds(0.1f);
    
        Assert.IsTrue(isClickOnWireEventInvoked);
    }
    
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator InteractWithTerminationOption()
    {
        GameObject mountableGameObj = FindInactiveObject("TerminationOption");
        mountableGameObj.SetActive(true);
        
        Assert.IsTrue(mountableGameObj.activeSelf);
        Assert.IsTrue(mountableGameObj.GetComponent<SphereCollider>().enabled);
        Vector3 screenPositionOfObject = Camera.main.WorldToScreenPoint(mountableGameObj.transform.position);
        
        sceneInteractions.ProcessMouseClickUp(screenPositionOfObject);
        yield return new WaitForSeconds(0.1f);
    
        Assert.IsTrue(isClickOnTerminationOptionEventInvoked);
    }
}
