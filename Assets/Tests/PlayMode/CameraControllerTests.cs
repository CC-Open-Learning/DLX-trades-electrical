using System.Collections;
using Cinemachine;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VARLab.TradesElectrical;

public class CameraControllerTests
{
    private CameraController cameraController;
    private CinemachineBrain cmBrain;
    private CinemachineVirtualCamera defaultCamera;
    private GameObject fanBoxTaskObj;
    private BoxMountingTask mountingTask;
    private CinemachineVirtualCamera taskCamera;

    [UnitySetUp]
    public IEnumerator BeforeTest()
    {
        const string testScenePath = "Assets/Tests/PlayMode/TestScenes/CameraControllerTestScene.unity";
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            testScenePath, new LoadSceneParameters(LoadSceneMode.Single));

        cameraController = GameObject.Find("Camera Navigation Handler").GetComponent<CameraController>();
        defaultCamera = GameObject.Find("Default Camera").GetComponent<CinemachineVirtualCamera>();
        cmBrain = Camera.main.GetComponent<CinemachineBrain>();
        taskCamera = GameObject.Find("Virtual Camera Fan Box").GetComponent<CinemachineVirtualCamera>();

        fanBoxTaskObj = GameObject.Find("Mount Fan Box Task");
        mountingTask = fanBoxTaskObj.GetComponent<BoxMountingTask>();

        yield return null;
    }

    [TearDown]
    public void AfterTest()
    {
        SceneManager.UnloadSceneAsync("CameraControllerTestScene");
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator PreviewMountLocationsAndMountableTest()
    {
        cameraController.PreviewedMountingLocations(mountingTask);
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(mountingTask.TaskPreviewCamera, cmBrain.ActiveVirtualCamera);

        var mountLocation = GameObject.Find("Location 1").GetComponent<MountLocation>();
        mountingTask.SelectedLocation = mountLocation;

        cameraController.PreviewedMounting(mountingTask);
        yield return new WaitForSeconds(2);

        Assert.AreEqual(mountingTask.SelectedLocation.CameraPosition, cmBrain.ActiveVirtualCamera);

        var pov = defaultCamera.GetCinemachineComponent<CinemachinePOV>();
        Assert.IsTrue(pov.m_HorizontalRecentering.m_enabled);
        Assert.IsTrue(pov.m_VerticalRecentering.m_enabled);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SwitchTaskCameraTest()
    {
        Assert.AreEqual(defaultCamera, cmBrain.ActiveVirtualCamera);
        cameraController.SwitchCamera(taskCamera);

        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(taskCamera, cmBrain.ActiveVirtualCamera);

        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SwitchDefaultCameraTest()
    {
        Assert.AreEqual(defaultCamera, cmBrain.ActiveVirtualCamera);
        cameraController.SwitchCamera(taskCamera);

        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(taskCamera, cmBrain.ActiveVirtualCamera);

        cameraController.SwitchDefaultCamera();
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(defaultCamera, cmBrain.ActiveVirtualCamera);

        yield return null;
    }
}