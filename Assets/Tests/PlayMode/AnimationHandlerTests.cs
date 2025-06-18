using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VARLab.TradesElectrical;

public class AnimationHandlerTests
{
    private GameObject mountable;
    private GameObject mountingAnimator;
    private MountLocation location;
    private MountingAnimationHandler mountingAnimatorHandler;
    private BoxMounter boxMounter;
    private GameObject fanBox;
    private GameObject mountLocation;
    private const string mountingAnimatorObjectName = "MountingAnimator";
    private const string testScenePath = "Assets/Tests/PlayMode/TestScenes/AnimationTest.unity";
    private const string fanBoxPrefabPath = "Assets/Prefab/Mounting/Boxes/Fan Box.prefab";
    private const string mountLocationObjectName = "MountLocation";
    
    
    [UnitySetUp]
    [Category("BuildServer")] 
    public IEnumerator BeforeTest()
    {
        // Animator Object
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(testScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
        mountingAnimator = GameObject.Find(mountingAnimatorObjectName);
        mountingAnimatorHandler = mountingAnimator.GetComponent<MountingAnimationHandler>();
        // Add Mountable Object
        mountable = AssetDatabase.LoadAssetAtPath<GameObject>(fanBoxPrefabPath);
        fanBox = Object.Instantiate(mountable);

        location = GameObject.Find(mountLocationObjectName).GetComponent<MountLocation>();
        location.PlaceMountable(fanBox.transform, MountableName.FanBox);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return SceneManager.UnloadSceneAsync(testScenePath);
    }

    [Category("BuildServer")] 
    [UnityTest]
    public IEnumerator MountableDrillTriggerAnimationTest()
    {
        mountingAnimatorHandler.TriggerDrillAnimation(location);
        yield return null;
        Assert.IsNotNull(location.MountedItem.gameObject.GetComponent<Animator>());
    }
}
