using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;

public class TaskHandlerTests
{
    private TaskHandler taskHandler;
    private EquipmentFactory equipmentFactory;
    private CableMounter cableMounter;
    private BoxMounter boxMounter;
    private WireMounter wireMounter;
    private EditMountedObject editMountedObject;
    private ConfirmSelectionMenuController confirmDialog;
    private ObjectInteractionWidget interactionWidget;
    
    // TODO Remove the uncessary finding of these tasks throughout tests, it only will increase the time it takes to run these tests.
    // Since these values are always reset on each test, it doesn't really matter if we modify them. Though Ask for sure.
    private BoxMountingTask lightBoxMountingTask;
    private BoxMountingTask fanBoxMountingTask;
    private BoxMountingTask outletBoxMountingTask;
    private BoxMountingTask gangBoxMountingTask; 

    private bool showEquipmentSelectionMenuInvoked;
    private bool showSupplyCableSelectionMenuInvoked;
    private bool onMountableMoveInvoked;
    private bool onMountableReplaceInvoked;
    private bool actionDiscardedInvoked;

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator BeforeTest()
    {
        const string testScenePath = "Assets/Tests/PlayMode/TestScenes/TaskHandlerTest.unity";
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            testScenePath, new LoadSceneParameters(LoadSceneMode.Single));

        GameObject[] allObjects = TestUtils.GetAllObjects();
        (_, taskHandler) = TestUtils.FindGameObject<TaskHandler>(allObjects, "Task Handler");
        (_, equipmentFactory) = TestUtils.FindGameObject<EquipmentFactory>(allObjects, "Equipment Factory");
        (_, boxMounter) = TestUtils.FindGameObject<BoxMounter>(allObjects, "Box Mounter");
        (_, cableMounter) = TestUtils.FindGameObject<CableMounter>(allObjects, "Cable Mounter");
        (_, wireMounter) = TestUtils.FindGameObject<WireMounter>(allObjects, "Wire Mounter");
        (_, editMountedObject) = TestUtils.FindGameObject<EditMountedObject>(allObjects, "Edit Mounted Object");
        (_, confirmDialog) = TestUtils.FindGameObject<ConfirmSelectionMenuController>(allObjects, "Selection Confirmation Dialog");
        (_, interactionWidget) = TestUtils.FindGameObject<ObjectInteractionWidget>(allObjects, "Object Interaction Widget");
        
        lightBoxMountingTask = GameObject.Find("Mount Light Box Task").GetComponent<BoxMountingTask>();
        fanBoxMountingTask = GameObject.Find("Mount Fan Box Task").GetComponent<BoxMountingTask>();
        outletBoxMountingTask = GameObject.Find("Mount Outlet Box Task").GetComponent<BoxMountingTask>();
        gangBoxMountingTask = GameObject.Find("Mount Gang Box Task").GetComponent<BoxMountingTask>();
        
        SetupEvents();

        yield return null;
    }

    [UnityTearDown]
    [Category("BuildServer")]
    public IEnumerator AfterTest()
    {
        yield return SceneManager.UnloadSceneAsync("TaskHandlerTest");

        showEquipmentSelectionMenuInvoked = false;
        showSupplyCableSelectionMenuInvoked = false;
        onMountableMoveInvoked = false;
        onMountableReplaceInvoked = false;
        actionDiscardedInvoked = false;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectAndMountBox()
    {
        const Task selectedTask = Task.MountLightBox;
        const MountableName selectedMountableName = MountableName.OctagonalBox;
        MountingTask expectedMountingTask = GameObject.Find("Mount Light Box Task").GetComponent<BoxMountingTask>();

        foreach (MountLocation location in ((BoxMountingTask)expectedMountingTask).LocationOptions)
        {
            // Mount locations are disabled
            Assert.IsFalse(location.isActiveAndEnabled);

            var markerCollider = location.GetComponentInChildren<SphereCollider>();
            // Location markers (orbs) are not present
            Assert.IsNull(markerCollider);
        }

        yield return SelectTaskAndMountableFromMenu(selectedTask, selectedMountableName);

        foreach (MountLocation location in ((BoxMountingTask)expectedMountingTask).LocationOptions)
        {
            // Mount locations are enabled
            Assert.IsTrue(location.isActiveAndEnabled);

            var markerCollider = location.GetComponentInChildren<SphereCollider>();
            // Location markers (orbs) are visible
            Assert.IsNotNull(markerCollider);
        }

        // Select a random location option
        MountLocation selectedLocation = ((BoxMountingTask)expectedMountingTask).LocationOptions[1];
        string expectedName = $"{selectedMountableName.ToString()} - {selectedLocation.name}";
        GameObject spawnedObject = GameObject.Find(expectedName);
        Assert.IsNull(spawnedObject);

        // Click on a mount location
        boxMounter.OnMountLocationClick(selectedLocation);
        yield return new WaitForSeconds(0.5f);

        spawnedObject = GameObject.Find(expectedName);
        Assert.IsNotNull(spawnedObject);

        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        Mountable savedMountable = expectedMountingTask.SelectedMountable;
        Assert.AreEqual(spawnedObject.GetComponentInChildren<Mountable>(), savedMountable);
    }
    
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectAndMountSupplyCable()
    {
        const Task selectedTask = Task.ConnectLightBoxToGangBox;
        const MountableName selectedMountableName = MountableName.LightToGangB;
        MountingTask expectedMountingTask =
            GameObject.Find("Connect Light Box to Gang Task").GetComponent<CableMountingTask>();

        const string firstCableName = "Light Box to Gang Box Incorrect 1";
        GameObject expectedObjectToEnable = GameObject.Find(firstCableName);
        Assert.IsNull(expectedObjectToEnable);

        yield return SelectTaskAndMountableFromMenu(selectedTask, selectedMountableName);

        expectedObjectToEnable = GameObject.Find(firstCableName);
        Assert.IsNotNull(expectedObjectToEnable);
        Mountable expectedMountable = expectedObjectToEnable.GetComponent<Mountable>();
        Assert.IsTrue(expectedMountable.isActiveAndEnabled);

        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        Mountable savedMountable = expectedMountingTask.SelectedMountable;
        Assert.AreEqual(expectedMountable, savedMountable);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator EditBoxThroughTaskMenu()
    {
        const Task selectedTask = Task.MountGangBox;
        const MountableName firstMountableSelection = MountableName.DeviceBoxLowDepth;
        BoxMountingTask expectedMountingTask = GameObject.Find("Mount Gang Box Task").GetComponent<BoxMountingTask>();

        yield return SelectTaskAndMountableFromMenu(selectedTask, firstMountableSelection);

        // Select a random location option
        MountLocation firstLocationSelection = expectedMountingTask.LocationOptions[1];
        // Click on a mount location
        boxMounter.OnMountLocationClick(firstLocationSelection);
        yield return new WaitForSeconds(0.5f);

        string expectedFirstObjName = $"{firstMountableSelection.ToString()} - {firstLocationSelection.name}";
        GameObject firstSpawnedObject = GameObject.Find(expectedFirstObjName);
        Assert.IsNotNull(firstSpawnedObject);

        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        const MountableName secondMountableSelection = MountableName.TwoGangBox;
        // Editing a mounted box through the Select & Mount Box menu
        taskHandler.OnMountingTaskClick(selectedTask);
        taskHandler.OnMountableSelected(secondMountableSelection);
        yield return null;

        MountLocation secondLocationSelection = expectedMountingTask.LocationOptions[2];
        boxMounter.OnMountLocationClick(secondLocationSelection);
        yield return new WaitForSeconds(0.5f);

        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        string expectedSecondObjName = $"{secondMountableSelection.ToString()} - {secondLocationSelection.name}";
        GameObject secondSpawnedObject = GameObject.Find(expectedSecondObjName);
        Assert.IsNotNull(secondSpawnedObject);

        firstSpawnedObject = GameObject.Find(expectedFirstObjName);
        Assert.IsNull(firstSpawnedObject);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ReplaceBoxByClickingOnIt()
    {
        const Task selectedTask = Task.MountGangBox;
        const MountableName firstMountableSelection = MountableName.DeviceBoxLowDepth;
        BoxMountingTask expectedMountingTask = GameObject.Find("Mount Gang Box Task").GetComponent<BoxMountingTask>();

        yield return SelectTaskAndMountableFromMenu(selectedTask, firstMountableSelection);

        // Select a random location option
        MountLocation firstLocationSelection = expectedMountingTask.LocationOptions[1];
        // Click on a mount location
        boxMounter.OnMountLocationClick(firstLocationSelection);
        yield return new WaitForSeconds(0.5f);

        string expectedFirstObjName = $"{firstMountableSelection.ToString()} - {firstLocationSelection.name}";
        GameObject firstSpawnedObject = GameObject.Find(expectedFirstObjName);
        Assert.IsNotNull(firstSpawnedObject);

        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        Assert.IsFalse(showEquipmentSelectionMenuInvoked);

        // Click on Replace option of edit mounted object dialog
        taskHandler.OnMountableReplace(expectedMountingTask);
        yield return null;

        Assert.IsTrue(showEquipmentSelectionMenuInvoked);

        const MountableName secondMountableSelection = MountableName.ThreeGangBox;
        string expectedSecondObjName = $"{secondMountableSelection.ToString()} - {firstLocationSelection.name}";

        GameObject secondSpawnedObject = GameObject.Find(expectedSecondObjName);
        Assert.IsNull(secondSpawnedObject);

        // Select the replacement from Task Menu
        yield return SelectTaskAndMountableFromMenu(selectedTask, secondMountableSelection);

        secondSpawnedObject = GameObject.Find(expectedSecondObjName);
        // New Mountable is spawned
        Assert.IsNotNull(secondSpawnedObject);

        firstSpawnedObject = GameObject.Find(expectedFirstObjName);
        // Old Mountable is destroyed
        Assert.IsNull(firstSpawnedObject);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator MoveBoxByClickingOnIt()
    {
        const Task selectedTask = Task.MountGangBox;
        const MountableName mountableSelection = MountableName.DeviceBoxLowDepth;
        BoxMountingTask expectedMountingTask = GameObject.Find("Mount Gang Box Task").GetComponent<BoxMountingTask>();

        yield return SelectTaskAndMountableFromMenu(selectedTask, mountableSelection);

        // Select a random location option
        MountLocation firstLocationSelection = expectedMountingTask.LocationOptions[0];
        // Click on a mount location
        boxMounter.OnMountLocationClick(firstLocationSelection);
        yield return new WaitForSeconds(0.5f);
        
        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        string expectedFirstObjName = $"{mountableSelection.ToString()} - {firstLocationSelection.name}";
        GameObject firstSpawnedObject = GameObject.Find(expectedFirstObjName);
        Assert.IsNotNull(firstSpawnedObject);
        
        foreach (MountLocation location in expectedMountingTask.LocationOptions)
        {
            // Mount locations are disabled
            Assert.IsFalse(location.isActiveAndEnabled);

            var markerCollider = location.GetComponentInChildren<SphereCollider>();
            // Location markers (orbs) are not present
            Assert.IsNull(markerCollider);
        }

        // Click on Move option of edit mounted object dialog
        taskHandler.OnMountableMove(expectedMountingTask);
        yield return null;

        foreach (MountLocation location in expectedMountingTask.LocationOptions)
        {
            // Mount locations are disabled
            if(location == firstLocationSelection)
            {
                Assert.IsTrue(location.isActiveAndEnabled);
            }
            else
            {
                Assert.IsTrue(location.isActiveAndEnabled);
            } 
            

            var markerCollider = location.GetComponentInChildren<SphereCollider>();
            // Location markers (orbs) are not present
            Assert.IsNotNull(markerCollider);
        }
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ReplaceSupplyCableByClickingOnIt()
    {
        const Task selectedTask = Task.ConnectLightBoxToGangBox;
        const MountableName selectedMountableName = MountableName.LightToGangB;
        CableMountingTask expectedMountingTask =
            GameObject.Find("Connect Light Box to Gang Task").GetComponent<CableMountingTask>();

        const string firstCableName = "Light Box to Gang Box Incorrect 1";
        GameObject firstCableObj = GameObject.Find(firstCableName);
        Assert.IsNull(firstCableObj);

        yield return SelectTaskAndMountableFromMenu(selectedTask, selectedMountableName);

        firstCableObj = GameObject.Find(firstCableName);
        Assert.IsNotNull(firstCableObj);
        Mountable firstMountable = firstCableObj.GetComponent<Mountable>();
        Assert.IsTrue(firstMountable.isActiveAndEnabled);

        // Confirm selection
        taskHandler.OnMountConfirm();
        yield return null;

        Mountable savedMountable = expectedMountingTask.SelectedMountable;
        Assert.AreEqual(firstMountable, savedMountable);

        Assert.IsFalse(showSupplyCableSelectionMenuInvoked);

        taskHandler.OnMountableReplace(expectedMountingTask);
        yield return null;

        Assert.IsTrue(showSupplyCableSelectionMenuInvoked);

        const MountableName newMountableName = MountableName.LightToGangC;
        GameObject secondCableObj = GameObject.Find("Light Box to Gang Box Correct");
        Assert.IsNull(secondCableObj);

        yield return SelectTaskAndMountableFromMenu(selectedTask, newMountableName);

        secondCableObj = GameObject.Find("Light Box to Gang Box Correct");
        Assert.IsNotNull(secondCableObj);
        Mountable secondMountable = secondCableObj.GetComponent<Mountable>();
        Assert.IsTrue(secondMountable.isActiveAndEnabled);
        
        firstCableObj = GameObject.Find(firstCableName);
        Assert.IsNull(firstCableObj);
    }
    
    
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ReplaceCablesWithUnsheathed()
    {
        // Place the wires initially
        // Call event
        const Task selectedTask = Task.ConnectLightBoxToGangBox;
        const MountableName selectedMountableName = MountableName.LightToGangB;
        MountingTask expectedMountingTask =
            GameObject.Find("Connect Light Box to Gang Task").GetComponent<CableMountingTask>();

        const string firstCableName = "Light Box to Gang Box Incorrect 1";
        yield return SelectTaskAndMountableFromMenu(selectedTask, selectedMountableName);
        
        taskHandler.OnMountConfirm();
        yield return null;

        GameObject sheathedCableGameObject = GameObject.Find(firstCableName);
        Mountable savedMountable = expectedMountingTask.SelectedMountable;
        Mountable expectedMountable = sheathedCableGameObject.GetComponent<Mountable>();

        Assert.IsTrue(sheathedCableGameObject.activeSelf);
        Assert.AreEqual(expectedMountable, savedMountable);
                
        // Get the unsheathed version 
        const string firstUnsheathedCableName = "Light Box to Gang Box Incorrect 1 Secured";
        // Mountable expectedMountable = expectedObjectToEnable.GetComponent<Mountable>();
        
        // now time to replace
        cableMounter.SwapAllCablesToSecuredVersions();
        yield return new WaitForSeconds(1f);
        GameObject unsheathedCableGameObject = GameObject.Find(firstUnsheathedCableName);
        
        Assert.IsFalse(sheathedCableGameObject.activeSelf);
        Assert.IsTrue(unsheathedCableGameObject.activeSelf);
        savedMountable = expectedMountingTask.SelectedMountable;
        expectedMountable = unsheathedCableGameObject.GetComponent<Mountable>();

        Assert.AreEqual(expectedMountable, savedMountable);
        
        
        // make comparision if unsheathed version is active
        
        yield return null;
    }


    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SelectABoxToTerminate()
    {
        const Task selectedTask = Task.TerminateLightBox;

        // Assume all the box mounting tasks are completed. We will manually spawn the required box.
        yield return SkipBoxMountingTasks();
        GameObject hostBoxObj = lightBoxMountingTask.SelectedMountable.gameObject.transform.parent.gameObject;
        
        List<InteractableWire> initialWires = FetchInitialWires(hostBoxObj);
        Assert.IsFalse(AllObjectsActive(initialWires));
       
        // Init the WireMounter to fetch newly spawned boxes
        wireMounter.Init();
        yield return null;

        // Simulate selecting the task tab in the Select & Mount Box menu
        taskHandler.OnMountingTaskClick(selectedTask);
        yield return new WaitForSeconds(1f);

        Assert.IsTrue(AllObjectsActive(initialWires));
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator TerminateWire()
    {
        const MountableName selectedMountableName = MountableName.BondWire; 
        const Task selectedTask = Task.TerminateLightBox;
        WireMountingTask wireMountingTask =
            GameObject.Find("Terminate Light Box Task").GetComponent<WireMountingTask>();

        // Assume all the box mounting tasks are completed. We will manually spawn the required box.
        yield return SkipBoxMountingTasks();
        // Init the WireMounter to fetch newly spawned boxes
        wireMounter.Init();
        yield return null;

        // Simulate selecting the task tab in the Select & Mount Box menu
        taskHandler.OnMountingTaskClick(selectedTask);
        yield return new WaitForSeconds(0.1f);

        // Select a wire option
        InteractableWire interactableWire =
            GameObject.Find("Wire_Light_Bond_Initial").GetComponent<InteractableWire>();
        wireMounter.OnInteractableWireClick(interactableWire);
        yield return null;

        WireTerminationOption selectedTerminationOption =
            GameObject.Find("Marker SmallVariant - Tucked Neat").GetComponent<WireTerminationOption>();

        // Click on a yellow orb in the box
        wireMounter.OnTerminationOptionClick(selectedTerminationOption);

        Assert.IsTrue(AreSelectedOptionsFresh(wireMountingTask));

        // Click on the confirm button
        taskHandler.OnMountConfirm();

        Assert.IsFalse(AreSelectedOptionsFresh(wireMountingTask));
        Assert.AreEqual(selectedTerminationOption.Option, wireMountingTask.SelectedOptionsMap[selectedMountableName].Option);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator TerminateWireFromScene()
    {
        //variables needed for test
        const MountableName selectedMountableName = MountableName.BondWire;
        WireMountingTask wireMountingTask =
            GameObject.Find("Terminate Light Box Task").GetComponent<WireMountingTask>();
   
        // Assume all the box mounting tasks are completed. We will manually spawn the required box.
        yield return SkipBoxMountingTasks();
        // Init the WireMounter to fetch newly spawned boxes
        wireMounter.Init();
        yield return null;

        // click  on box
        wireMounter.OnHostBoxClick(wireMountingTask.HostBoxInstance);
        yield return new WaitForSeconds(0.1f);


        //select wire
        InteractableWire interactableWire =
            GameObject.Find("Wire_Light_Bond_Initial").GetComponent<InteractableWire>();
        wireMounter.OnInteractableWireClick(interactableWire);
        yield return null;

        WireTerminationOption selectedTerminationOption =
                 GameObject.Find("Marker SmallVariant - Tucked Neat").GetComponent<WireTerminationOption>();

        // Click on a yellow orb in the box
        wireMounter.OnTerminationOptionClick(selectedTerminationOption);

        // Click on the confirm button
        taskHandler.OnMountConfirm();

        Assert.IsFalse(AreSelectedOptionsFresh(wireMountingTask));
        Assert.AreEqual(selectedTerminationOption.Option, wireMountingTask.SelectedOptionsMap[selectedMountableName].Option);
    }

    //***********************************************************************************************************************************************************
    //NOTE: due to time constraints the tests were not able to be complete but code coverage can be improved in the wire mounter class by testing the following:
    // terminate gang box from menu
    // terminte gang box from scene
    //************************************************************************************************************************************************************

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator TerminateGangBoxFromMenu()
    {
        // Constants for test
        const Task selectedTask = Task.TerminateGangBoxBonds;
        const MountableName selectedMountableName = MountableName.BondWire;
        const MountableName connectedBox = MountableName.DeviceBox;

        // Get the GangBoxWireMountingTask component
        GangBoxWireMountingTask gangBoxWireMountingTask =
            GameObject.Find("Terminate Gang Box Bonds Task").GetComponent<GangBoxWireMountingTask>();

        // Assume all the box mounting tasks are completed. We will manually spawn the required box.
        yield return SkipBoxMountingTasks();
        // Init the WireMounter to fetch newly spawned boxes
        wireMounter.Init();
        yield return null;

        // Simulate selecting the task tab in the menu
        taskHandler.OnMountingTaskClick(selectedTask);
        yield return new WaitForSeconds(0.1f);

        // Find and select a gang box conductor (bond wire)
        GangBoxConductor gangBoxConductor =
            GameObject.Find("Wire_Outlet_Bond_Initial").GetComponent<GangBoxConductor>();
        wireMounter.OnInteractableWireClick(gangBoxConductor);
        yield return null;

        // Find and select a termination option (yellow orb)
        GangBoxTerminationOption terminationOption =
            GameObject.Find("Outlet - Tied To Bond").GetComponent<GangBoxTerminationOption>();
        wireMounter.OnTerminationOptionClick(terminationOption);
        yield return null;

        // Confirm the selection
        taskHandler.OnMountConfirm();
        yield return null;

        // Verify the wire was correctly terminated
        Assert.IsTrue(gangBoxWireMountingTask.GangBoxSelectedOptionsMap.ContainsKey(connectedBox));
        Assert.AreEqual(selectedMountableName, gangBoxWireMountingTask.GangBoxSelectedOptionsMap[connectedBox].Conductor);
        Assert.AreEqual(terminationOption.Option, gangBoxWireMountingTask.GangBoxSelectedOptionsMap[connectedBox].TerminationOption);
    }

    /// <summary>
    ///     Workflow for installing a device:
    ///     * Device is selected from UI
    ///     * System loads both supply (active) and installed (inactive) prefabs for device
    ///     * Learner connects bond, neutral, hot conductors in supply version of device
    ///     * System disables "supply" and enables "installed" version of device
    /// </summary>
    /// <returns></returns>
    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator DeviceMounter_SelectDeviceForInstallation_SupplyAndInstalledObjects()
    {
        const Task selectedTask = Task.InstallFan;
        const MountableName selectedMountableName = MountableName.VentilationFan6In;
        const MountableName mountableSupplyEnum = MountableName.FanSupply;
        DeviceMountingTask deviceMountingTask = GameObject.Find("Install Fan Task").GetComponent<DeviceMountingTask>();

        yield return SkipBoxMountingTasks();

        // Simulate selecting the task tab in the Select & Install Devices menu
        taskHandler.OnMountingTaskClick(selectedTask);

        // Select a device
        taskHandler.OnMountableSelected(selectedMountableName);
        yield return null;

        GameObject spawnedObjectSupply = Object.FindObjectsOfType<Mountable>(includeInactive: true).First(mountable => mountable.Name == mountableSupplyEnum).gameObject;

        GameObject spawnedObjectInstalled = Object.FindObjectsOfType<Device>(includeInactive:true).First(device => device.Name == selectedMountableName).gameObject;
        
        Assert.IsNotNull(spawnedObjectSupply);
        Assert.IsNotNull(spawnedObjectInstalled);
        Assert.IsTrue(spawnedObjectSupply.activeInHierarchy);
        Assert.IsFalse(spawnedObjectInstalled.activeInHierarchy);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator DeviceMounter_InstallDeviceWorkflow_6InchVentFan()
    {
        const Task selectedTask = Task.InstallFan;
        const MountableName selectedMountableName = MountableName.VentilationFan6In;
        const MountableName additionalMountableName = MountableName.FanSupply;
        DeviceMountingTask deviceMountingTask = GameObject.Find("Install Fan Task").GetComponent<DeviceMountingTask>();
        bool deviceWireConnectionEventInvoked = false;

        yield return SkipBoxMountingTasks();

        // Simulate selecting the task tab in the Select & Install Devices menu
        taskHandler.OnMountingTaskClick(selectedTask);

        // Select a device
        taskHandler.OnMountableSelected(selectedMountableName);
        yield return null;

        Mountable fanSupply = Object.FindObjectsOfType<Mountable>(includeInactive: true).First(mountable => mountable.Name == additionalMountableName);

        InteractableWire[] conductors = fanSupply.GetComponentsInChildren<InteractableWire>(includeInactive: true);
        WireTerminationOption[] terminationOptions = fanSupply.transform.parent.GetComponentsInChildren<WireTerminationOption>(includeInactive: true);

        // These are the conductors awaiting action
        InteractableWire bondWire = conductors.First(wire => wire.Name == MountableName.BondWire && wire.Option == TerminationOptionType.None);
        InteractableWire neutralWire = conductors.First(wire => wire.Name == MountableName.NeutralWire && wire.Option == TerminationOptionType.None);
        InteractableWire hotWire = conductors.First(wire => wire.Name == MountableName.HotWire && wire.Option == TerminationOptionType.None);

        wireMounter.DeviceWireConnectionFinalized.AddListener(() => deviceWireConnectionEventInvoked = true);

        // Terminate bond conductor by selecting bond wire, choosing "tied to bond" option, then confirming
        wireMounter.OnInteractableWireClick(bondWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToBond));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;


        // Terminate neutral conductor, similar workflow
        wireMounter.OnInteractableWireClick(neutralWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToNeutral));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;

        // Terminate hot conductor, similar workflow
        wireMounter.OnInteractableWireClick(hotWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToHot));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;


        // Device install is complete, camera will move back to default position.
        // Now the actual vent fan prefab should be enabled instead of the supply
        GameObject spawnedObjectInstalled = Object.FindObjectsOfType<Device>(includeInactive: true).First(device => device.Name == selectedMountableName).gameObject;

        Assert.IsTrue(deviceWireConnectionEventInvoked);
        Assert.IsFalse(fanSupply.gameObject.activeInHierarchy);
        Assert.IsTrue(spawnedObjectInstalled.activeInHierarchy);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator DeviceMounter_InstallDeviceWorkflow_15A120VBreaker()
    {
        const Task selectedTask = Task.InstallBreaker;
        const MountableName selectedMountableName = MountableName.Breaker15A120V;
        DeviceMountingTask deviceMountingTask = GameObject.Find("Install Breaker Task").GetComponent<DeviceMountingTask>();
        bool deviceWireConnectionEventInvoked = false;

        yield return SkipBoxMountingTasks();

        // Simulate selecting the task tab in the Select & Install Devices menu
        taskHandler.OnMountingTaskClick(selectedTask);

        // Select a device
        taskHandler.OnMountableSelected(selectedMountableName);
        yield return null;

        Device breakerMountable = Object.FindObjectsOfType<Device>(includeInactive: true).First(mountable => mountable.Name == selectedMountableName);

        // Need to move to parent transform since breaker device is on the meshes and the wires are not children.
        // This has no actual effect on the workflow
        InteractableWire[] conductors = breakerMountable.GetComponentsInChildren<InteractableWire>(includeInactive: true);
        WireTerminationOption[] terminationOptions = breakerMountable.GetComponentsInChildren<WireTerminationOption>(includeInactive: true);

        // These are the conductors awaiting action
        InteractableWire bondWire = conductors.First(wire => wire.Name == MountableName.BondWire && wire.Option == TerminationOptionType.None);
        InteractableWire neutralWire = conductors.First(wire => wire.Name == MountableName.NeutralWire && wire.Option == TerminationOptionType.None);
        InteractableWire hotWire = conductors.First(wire => wire.Name == MountableName.HotWire && wire.Option == TerminationOptionType.None);

        wireMounter.DeviceWireConnectionFinalized.AddListener(() => deviceWireConnectionEventInvoked = true);

        // Terminate bond conductor by selecting bond wire, choosing "tied to bond" option, then confirming
        wireMounter.OnInteractableWireClick(bondWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToBondLugSameSide));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;


        // Terminate neutral conductor, similar workflow
        wireMounter.OnInteractableWireClick(neutralWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToNeutral));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;

        // Terminate hot conductor, similar workflow
        wireMounter.OnInteractableWireClick(hotWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToHot));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;


        // Device install is complete, camera will move back to default position.
        // Now the actual vent fan prefab should be enabled instead of the supply
        Assert.IsTrue(deviceWireConnectionEventInvoked);
        Assert.IsTrue(breakerMountable.gameObject.activeInHierarchy);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator DeviceMounter_ReplaceDevice_CleanUpPrevious()
    {
        const Task selectedTask = Task.InstallLight;
        const MountableName initialSelectedMountableName = MountableName.PotLight;
        const MountableName secondarySelectedMountableName = MountableName.FlushMountLED;
        const MountableName initialMountableSupplyEnum = MountableName.PotLightSupply;
        const MountableName secondaryMountableSupplyEnum = MountableName.FlushMountLEDSupply;

        DeviceMountingTask deviceMountingTask = GameObject.Find("Install Light Task").GetComponent<DeviceMountingTask>();


        /////////////
        // This block is identical to the DeviceMounter_InstallDeviceWorkflow_6InchVentFan above
        // but for installing the Pot Light instead. Need to set up a device first in order to load a second one
        yield return SkipBoxMountingTasks();

        // Simulate selecting the task tab in the Select & Install Devices menu
        taskHandler.OnMountingTaskClick(selectedTask);

        // Select a device
        taskHandler.OnMountableSelected(initialSelectedMountableName);
        yield return null;

        Mountable firstLightSupplyPrefab = Object.FindObjectsOfType<Mountable>(includeInactive: true).First(mountable => mountable.Name == initialMountableSupplyEnum);

        InteractableWire[] conductors = firstLightSupplyPrefab.GetComponentsInChildren<InteractableWire>(includeInactive: true);
        WireTerminationOption[] terminationOptions = firstLightSupplyPrefab.GetComponentsInChildren<WireTerminationOption>(includeInactive: true);

        // These are the conductors awaiting action
        InteractableWire bondWire = conductors.First(wire => wire.Name == MountableName.BondWire && wire.Option == TerminationOptionType.None);
        InteractableWire neutralWire = conductors.First(wire => wire.Name == MountableName.NeutralWire && wire.Option == TerminationOptionType.None);
        InteractableWire hotWire = conductors.First(wire => wire.Name == MountableName.HotWire && wire.Option == TerminationOptionType.None);

        // Terminate bond conductor by selecting bond wire, choosing "tied to bond" option, then confirming
        wireMounter.OnInteractableWireClick(bondWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToBond));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;


        // Terminate neutral conductor, similar workflow
        wireMounter.OnInteractableWireClick(neutralWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToNeutral));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;

        // Terminate hot conductor, similar workflow
        wireMounter.OnInteractableWireClick(hotWire);
        yield return null;
        wireMounter.OnTerminationOptionClick(terminationOptions.First(option => option.Option == TerminationOptionType.TiedToHot));
        yield return null;
        taskHandler.OnMountConfirm();
        yield return null;


        // Device install is complete, camera will move back to default position.
        // Now the actual vent fan prefab should be enabled instead of the supply
        GameObject firstSpawnedObjectInstalled = Object.FindObjectsOfType<Device>(includeInactive: true).First(device => device.Name == initialSelectedMountableName).gameObject;

        Assert.IsFalse(firstLightSupplyPrefab.gameObject.activeInHierarchy);
        Assert.IsTrue(firstSpawnedObjectInstalled.activeInHierarchy);

        //
        /////////////

        // Now begin test part 2 where a different light device is selected
        taskHandler.OnMountingTaskClick(selectedTask);

        // Select a new device
        taskHandler.OnMountableSelected(secondarySelectedMountableName);
        yield return null;

        Mountable secondLightSupplyPrefab = Object.FindObjectsOfType<Mountable>(includeInactive: true).First(mountable => mountable.Name == secondaryMountableSupplyEnum);

        GameObject secondSpawnedObjectInstalled = Object.FindObjectsOfType<Device>(includeInactive: true).First(device => device.Name == secondarySelectedMountableName).gameObject;


        Assert.IsFalse(firstLightSupplyPrefab);
        Assert.IsTrue(secondLightSupplyPrefab);
        Assert.IsTrue(secondLightSupplyPrefab.gameObject.activeInHierarchy);
        Assert.IsTrue(secondSpawnedObjectInstalled);
        Assert.IsFalse(secondSpawnedObjectInstalled.activeInHierarchy);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ValidateBoxInteractionWidget()
    {
        const Task selectedTask = Task.MountLightBox;
        const MountableName selectedMountableName = MountableName.OctagonalBox;
        MountingTask expectedMountingTask = GameObject.Find("Mount Light Box Task").GetComponent<BoxMountingTask>();

        yield return SelectTaskAndMountableFromMenu(selectedTask, selectedMountableName);

        // Select a random location option
        MountLocation selectedLocation = ((BoxMountingTask)expectedMountingTask).LocationOptions[1];
        string expectedName = $"{selectedMountableName.ToString()} - {selectedLocation.name}";

        // Click on a mount location
        boxMounter.OnMountLocationClick(selectedLocation);
        yield return new WaitForSeconds(0.5f);

        GameObject spawnedObject = GameObject.Find(expectedName);
        Assert.IsNotNull(spawnedObject);

        // Confirm selection
        confirmDialog.ConfirmButtonPressed?.Invoke();
        yield return null;

        Mountable savedMountable = expectedMountingTask.SelectedMountable;
        Assert.AreEqual(spawnedObject.GetComponentInChildren<Mountable>(), savedMountable);

        UIDocument uiDoc = interactionWidget.GetComponent<UIDocument>();
        // Object interaction widget's root
        VisualElement rootElement = uiDoc.rootVisualElement;

        Button moveButton = rootElement.Q<Button>(ObjectInteractionWidget.MoveButtonName);
        Button replaceButton = rootElement.Q<Button>(ObjectInteractionWidget.ReplaceButtonName);
        Button backButton = rootElement.Q<Button>(ObjectInteractionWidget.GoBackButtonName);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, rootElement.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, moveButton.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, replaceButton.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, backButton.style.display);

        editMountedObject.OnMountableClick(savedMountable);

        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, rootElement.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, moveButton.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, replaceButton.style.display);
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.Flex, backButton.style.display);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ValidateBoxInteractionMoveButtonClick()
    {
        UIDocument uiDoc = interactionWidget.GetComponent<UIDocument>();
        VisualElement rootElement = uiDoc.rootVisualElement;

        Button moveButton = rootElement.Q<Button>(ObjectInteractionWidget.MoveButtonName);
        var moveButtonClick = new NavigationSubmitEvent() { target = moveButton };

        yield return ValidateBoxInteractionWidget();
        Assert.IsFalse(onMountableMoveInvoked);
        moveButton.SendEvent(moveButtonClick);
        Assert.IsTrue(onMountableMoveInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ValidateBoxInteractionReplaceButtonClick()
    {
        UIDocument uiDoc = interactionWidget.GetComponent<UIDocument>();
        VisualElement rootElement = uiDoc.rootVisualElement;

        Button replaceButton = rootElement.Q<Button>(ObjectInteractionWidget.ReplaceButtonName);
        var replaceButtonClick = new NavigationSubmitEvent() { target = replaceButton };

        yield return ValidateBoxInteractionWidget();
        Assert.IsFalse(onMountableReplaceInvoked);
        replaceButton.SendEvent(replaceButtonClick);
        Assert.IsTrue(onMountableReplaceInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ValidateBoxInteractionBackButtonClick()
    {
        UIDocument uiDoc = interactionWidget.GetComponent<UIDocument>();
        VisualElement rootElement = uiDoc.rootVisualElement;

        Button backButton = rootElement.Q<Button>(ObjectInteractionWidget.GoBackButtonName);
        var backButtonClick = new NavigationSubmitEvent() { target = backButton };

        yield return ValidateBoxInteractionWidget();
        Assert.IsFalse(actionDiscardedInvoked);
        backButton.SendEvent(backButtonClick);
        Assert.IsTrue(actionDiscardedInvoked);
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator SkipBoxMountingTasks()
    {
        yield return new WaitForSeconds(1f);
        
        taskHandler.SkipBoxMountingTask();

        yield return new WaitForSeconds(1f);;
        
        Task[] boxMountedTasks = {Task.MountFanBox, Task.MountLightBox, Task.MountGangBox, Task.MountOutletBox};
        
        const MountableName firstMountableSelection = MountableName.DeviceBox;

        // Select a random location option
        MountLocation fanBoxCorrectLocation = fanBoxMountingTask.CorrectLocation;
        MountLocation lightBoxCorrectLocation = lightBoxMountingTask.CorrectLocation;
        MountLocation outletBoxCorrectLocation = outletBoxMountingTask.CorrectLocation;
        MountLocation gangBoxCorrectLocation = gangBoxMountingTask.CorrectLocation;
        
        string expectedFanBoxObjectName = $"{fanBoxMountingTask.CorrectMountable.ToString()} - {fanBoxCorrectLocation.name}";
        string expectedLightBoxObjectName = $"{lightBoxMountingTask.CorrectMountable.ToString()} - {lightBoxCorrectLocation.name}";
        string expectedOutletBoxObjectName = $"{outletBoxMountingTask.CorrectMountable.ToString()} - {outletBoxCorrectLocation.name}";
        string expectedGangBoxObjectName = $"{gangBoxMountingTask.CorrectMountable.ToString()} - {gangBoxCorrectLocation.name}";
        
        GameObject fanBoxObject = GameObject.Find(expectedFanBoxObjectName);
        GameObject lightBoxObject = GameObject.Find(expectedLightBoxObjectName);
        GameObject outletBoxObject = GameObject.Find(expectedOutletBoxObjectName);
        GameObject gangBoxObject = GameObject.Find(expectedGangBoxObjectName);
        
        // Check Objects Spawned
        Assert.IsNotNull(fanBoxObject);
        Assert.IsNotNull(lightBoxObject);
        Assert.IsNotNull(outletBoxObject);
        Assert.IsNotNull(gangBoxObject);

        // Check Locations Are Expected
        Assert.AreEqual(fanBoxCorrectLocation, fanBoxMountingTask.SelectedLocation);
        Assert.AreEqual(lightBoxCorrectLocation, lightBoxMountingTask.SelectedLocation);
        Assert.AreEqual(outletBoxCorrectLocation, outletBoxMountingTask.SelectedLocation);
        Assert.AreEqual(gangBoxCorrectLocation, gangBoxMountingTask.SelectedLocation);
        
        
        // Check if spawned mountables same as selected
        Assert.AreEqual(fanBoxObject.GetComponentInChildren<Mountable>(), fanBoxMountingTask.SelectedMountable);
        Assert.AreEqual(lightBoxObject.GetComponentInChildren<Mountable>(), lightBoxMountingTask.SelectedMountable);
        Assert.AreEqual(outletBoxObject.GetComponentInChildren<Mountable>(), outletBoxMountingTask.SelectedMountable);
        Assert.AreEqual(gangBoxObject.GetComponentInChildren<Mountable>(), gangBoxMountingTask.SelectedMountable);

        // Check the Tasks Completed
        int expectedCompletedTasksNumber = 4;
        Assert.AreEqual(expectedCompletedTasksNumber, taskHandler.GetCompletedTasksNum());
        Assert.AreEqual(expectedCompletedTasksNumber, taskHandler.MountedObjectMap.Count);
        
        // Check UI        
        var generalContractorFeedbackNotificationController =
            GameObject.FindObjectOfType<DialogController>();
        UIDocument uiDocumentGeneralContractorFeedback = generalContractorFeedbackNotificationController.GetComponent<UIDocument>();
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, 
            uiDocumentGeneralContractorFeedback.rootVisualElement.style.display);        
        
        var taskSelectionMenuController =
            GameObject.FindObjectOfType<TaskSelectionMenuController>();
        Assert.AreEqual(expectedCompletedTasksNumber,taskSelectionMenuController.TotalMountingTasks);
        Assert.IsFalse(taskSelectionMenuController.IsSelectAndMountBoxesBtnEnabled());
        Assert.IsTrue(taskSelectionMenuController.IsSupplyCableBtnEnabled());
        
        var notificationController = GameObject.FindObjectOfType<SupervisorFeedbackMenuController>();
        UIDocument uiDocumentNotificationController = notificationController.GetComponent<UIDocument>();
        Assert.AreEqual((StyleEnum<DisplayStyle>)DisplayStyle.None, 
            uiDocumentNotificationController.rootVisualElement.style.display);        
        
        // Check the Scene Interaction
        var sceneInteraction = GameObject.FindObjectOfType<SceneInteractions>();
        
        Assert.IsTrue(sceneInteraction.boxColliders.Contains(fanBoxMountingTask.SelectedMountable.GetComponent<BoxCollider>()));
        Assert.IsTrue(sceneInteraction.boxColliders.Contains(lightBoxMountingTask.SelectedMountable.GetComponent<BoxCollider>()));
        Assert.IsTrue(sceneInteraction.boxColliders.Contains(outletBoxMountingTask.SelectedMountable.GetComponent<BoxCollider>()));
        Assert.IsTrue(sceneInteraction.boxColliders.Contains(gangBoxMountingTask.SelectedMountable.GetComponent<BoxCollider>()));
        
        // Check the Edit Script for the bool to turn off editing for mountables
        var editMount = GameObject.FindObjectOfType<EditMountedObject>();
        Assert.IsTrue(editMount.isAllBoxMountingTasksCorrect);

    }

    // TODO Fix test for conductor part
    /*[UnityTest]
    [Category("BuildServer")]
    public IEnumerator SkipDeviceTask()
    {
        // Set up device task
        TaskSelectionMenuController taskSelectionMenuController = 
            GameObject.FindObjectOfType<TaskSelectionMenuController>();
        
        // UI Component of Task List Device Btn
        UIDocument uiDocTaskSelection = taskSelectionMenuController.gameObject.GetComponent<UIDocument>();
        VisualElement rootElementTaskSelection = uiDocTaskSelection.rootVisualElement;
        Button taskListDeviceBtn = rootElementTaskSelection.Q<Button>("BtnInstallDevices");
        string taskListOldDeviceLabelText = taskListDeviceBtn.Q<Label>("LabelTaskCount").text;
        
        // Call box mount skip
        taskHandler.SkipBoxMountingTask();
        yield return null;
        
        
        // set the  task correctly completed within task handler
        const MountableName selectedMountableName = MountableName.VentilationFan6In;
        DeviceMountingTask deviceMountingTask = GameObject.Find("Install Fan Task").GetComponent<DeviceMountingTask>();
        deviceMountingTask.RequestedDevice = selectedMountableName;
        
        
        // Mock up Selected Mountable
        GameObject mockedUpSelectedMountable = new GameObject("Mocked up");
        Mountable selectedMountable = mockedUpSelectedMountable.AddComponent<Mountable>();
        typeof(Mountable)
            .GetProperty(nameof(Mountable.Name), BindingFlags.Instance 
                                                 | BindingFlags.NonPublic | BindingFlags.Public)
            ?.SetValue(selectedMountable, MountableName.VentilationFan6In);
        deviceMountingTask.SelectedMountable = selectedMountable;
        yield return null;

        // SETUP conductor 
        var wireMountingTask = deviceMountingTask.gameObject.GetComponentInChildren<WireMountingTask>();
        wireMountingTask.SavedWireTerminationOptions = new Dictionary<MountableName, TerminationOptionType>()
        {
            { MountableName.BondWire, TerminationOptionType.TiedToBond },
            { MountableName.NeutralWire, TerminationOptionType.TiedToNeutral },
            { MountableName.HotWire, TerminationOptionType.TiedToHot }
        };
        
        // Skip device task
        taskHandler.DeviceInstallLoadState();
        yield return new WaitForSeconds(1f);
        
        // TODO
        // Check if Pre Connection Disabled but spawned
        GameObject expectedDeviceInstalled = GameObject.Find($"{selectedMountableName} - Post Connection");
        Assert.IsTrue(expectedDeviceInstalled.activeSelf);
        
        Mountable savedMountable = deviceMountingTask.SelectedMountable;
        Device expectedDevice = expectedDeviceInstalled.GetComponentInChildren<Device>();
        Assert.AreEqual(expectedDevice, savedMountable);
        Assert.IsFalse(expectedDevice.PreConnectionMountableInstance.isActiveAndEnabled);
        
        // Check if Task UI count updated
        // Check if Task UI Label is in Progress
        Label taskListDeviceStatusLabel = taskListDeviceBtn.Q<Label>("StatusLabel");
        Assert.AreEqual(UIHelper.StatusInProgress, taskListDeviceStatusLabel.text);

        string taskListDeviceLabelText = taskListDeviceBtn.Q<Label>("LabelTaskCount").text;
        Assert.AreNotEqual(taskListOldDeviceLabelText, taskListDeviceLabelText);
        
        yield return null;
    }*/

    public IEnumerator SkipSupplyCableTask()
    {
        // UI Objects
        TaskSelectionMenuController taskSelectionMenuController =
            GameObject.FindObjectOfType<TaskSelectionMenuController>();
        var RunSupplyCableMenu = GameObject.FindObjectOfType<RunSupplyCableMenuController>();
        var supplyCableToBoxesMenu = GameObject.FindObjectOfType<SupplyCableSelectionMenuController>();

        // Installed Label Check
        UIDocument uiDocSupplyCableToBoxesMenu = supplyCableToBoxesMenu.gameObject.GetComponent<UIDocument>();
        VisualElement rootElementSupplyToBoxesMenu = uiDocSupplyCableToBoxesMenu.rootVisualElement;
        Button fanToGangBBtn = rootElementSupplyToBoxesMenu.Q<Button>("BtnFanToGangB");
        Assert.AreEqual(DisplayStyle.None,fanToGangBBtn.Q<Label>("Status").style.display.value);
        // Task List UI
        UIDocument uiDocTaskSelection = taskSelectionMenuController.gameObject.GetComponent<UIDocument>();
        VisualElement rootElementTaskSelection = uiDocTaskSelection.rootVisualElement;
        Button taskListRunSupplyCableBtn = rootElementTaskSelection.Q<Button>("BtnRunSupplyCable");
        string taskListOldSupplyLabelText = taskListRunSupplyCableBtn.Q<Label>("LabelTaskCount").text;
        
        // Run Supply Cable Menu UI
        UIDocument uiDocRunSupply = RunSupplyCableMenu.gameObject.GetComponent<UIDocument>();
        VisualElement rootElementRunSupply = uiDocRunSupply.rootVisualElement;
        Button runSupplyCableBtn = rootElementRunSupply.Q<Button>("BtnConnectCablesToBoxes");
        string oldRunSupplyLabelText = runSupplyCableBtn.Q<Label>("LabelTaskCount").text;
        
        // Skip Task
        taskHandler.SkipBoxMountingTask();
        
        // Setup task to skip
        
        Task task = Task.ConnectFanBoxToGangBox;
        CableMountingTask expectedMountingTask =
            GameObject.Find("Connect Fan to Gang Task").GetComponent<CableMountingTask>();

        const string cableName = "Fan Box to Gang Box Incorrect 1";
        GameObject cableObj = GameObject.Find(cableName);
        Assert.IsNull(cableObj);
        
        // Mock up Selected Mountable
        GameObject mockedUpSelectedMountable = new GameObject("Mocked up");
        Mountable selectedMountable = mockedUpSelectedMountable.AddComponent<Mountable>();
        typeof(Mountable)
            .GetProperty(nameof(Mountable.Name), BindingFlags.Instance 
                                                 | BindingFlags.NonPublic | BindingFlags.Public)
            ?.SetValue(selectedMountable, MountableName.FanToGangB);
        expectedMountingTask.SelectedMountable = selectedMountable;
        yield return null;

        // Skip Single Cable
        taskHandler.SkipCableMountingSubTask(expectedMountingTask);
        yield return new WaitForSeconds(1f);
        
        cableObj = GameObject.Find(cableName);
        Assert.IsNotNull(cableObj.activeSelf);
        
        Mountable expectedMountable = cableObj.GetComponent<Mountable>();
        Assert.IsTrue(expectedMountable.isActiveAndEnabled);
        
        Mountable savedMountable = expectedMountingTask.SelectedMountable;
        Assert.AreEqual(expectedMountable, savedMountable);
        
        int expectedCompletedTasksNumber = 5;
        Assert.AreEqual(expectedCompletedTasksNumber, taskHandler.GetCompletedTasksNum());
     
        // Task List UI 
        Label taskListSupplyStatusLabel = taskListRunSupplyCableBtn.Q<Label>("StatusLabel");
        Assert.AreEqual(UIHelper.StatusInProgress, taskListSupplyStatusLabel.text);

        string taskListSupplyLabelText = taskListRunSupplyCableBtn.Q<Label>("LabelTaskCount").text;
        Assert.AreNotEqual(taskListOldSupplyLabelText, taskListSupplyLabelText);
        
        // RUN SUPPLY 
        Label runSupplyStatusLabel = runSupplyCableBtn.Q<Label>("StatusLabel");
        Assert.AreEqual(UIHelper.StatusInProgress, runSupplyStatusLabel.text);

        string supplyLabelText = runSupplyCableBtn.Q<Label>("LabelTaskCount").text;
        Assert.AreNotEqual(oldRunSupplyLabelText, supplyLabelText);
        
        Assert.AreEqual(DisplayStyle.Flex,fanToGangBBtn.Q<Label>("Status").style.display.value);
        
        yield return null; 
    }
    private bool AreSelectedOptionsFresh(WireMountingTask task)
    {
        foreach (InteractableWire wire in task.SelectedOptionsMap.Values)
        {
            if (wire != null)
            {
                return false;
            }
        }

        return true;
    }

    private bool AllObjectsActive(List<InteractableWire> wires)
    {
        foreach (InteractableWire wire in wires)
        {
            if (!wire.gameObject.activeSelf) { return false; }
        }

        return true;
    }

    private List<InteractableWire> FetchInitialWires(GameObject baseObGameObject)
    {
        var interactableWires = baseObGameObject.GetComponentsInChildren<InteractableWire>(true);
        return interactableWires.Where(wire => wire.Option == TerminationOptionType.None).ToList();
    }

    private IEnumerator SelectTaskAndMountableFromMenu(Task selectedTask, MountableName selectedMountableName)
    {
        // Simulate selecting the task tab in the Select & Mount Box menu
        taskHandler.OnMountingTaskClick(selectedTask);
        // Simulate selecting a box in the Select & Mount Box menu
        taskHandler.OnMountableSelected(selectedMountableName);
        yield return null;
    }

    private void SetupEvents()
    {
        taskHandler.ShowBoxSelectionMenu.AddListener(task =>
        {
            showEquipmentSelectionMenuInvoked = true;
        });

        taskHandler.ShowSupplyCableSelectionMenu.AddListener(task =>
        {
            showSupplyCableSelectionMenuInvoked = true;
        });

        editMountedObject.MovingMountable.AddListener(boxMountingTask =>
        {
            onMountableMoveInvoked = true;
        });

        editMountedObject.ReplacingMountable.AddListener(mountingTask =>
        {
            onMountableReplaceInvoked = true;
        });
        
        editMountedObject.ActionDiscarded.AddListener(() =>
        {
            actionDiscardedInvoked = true;
        });
    }
}
