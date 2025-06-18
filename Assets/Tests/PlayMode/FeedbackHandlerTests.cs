using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VARLab.TradesElectrical;
using Task = VARLab.TradesElectrical.Task;
using Object = UnityEngine.Object;

public class FeedbackHandlerTests
{
    // Handlers
    private FeedbackHandler feedbackHandler;
    private TaskHandler taskHandler;

    // Mounters
    private BoxMounter boxMounter;
    private CableMounter cableMounter;

    // Menu Controllers
    private BoxMountingSelectionMenuController boxMountingSelectionMenuController;
    private SupplyCableSelectionMenuController supplyCableSelectionMenuController;
    private TerminateCablesMenuController terminateCablesMenuController;
    private TerminateGangBoxSubMenuController terminateGangBoxSubMenuController;
    private DeviceSelectionMenuController deviceMenuSelectionController;

    // Event Listeners
    private SortedDictionary<Task, List<RoughInFeedbackInformation>> currentTaskFeedbackMap = new();

    // Mounting Tasks

    private BoxMountingTask outletBoxMountingTask;
    private BoxMountingTask lightBoxMountingTask;
    private BoxMountingTask fanBoxMountingTask;
    private BoxMountingTask gangBoxMountingTask;

    private CableMountingTask connectFanFromGangTask;
    private CableMountingTask connectLightFromGangTask;
    private CableMountingTask connectOutletFromGangTask;
    private CableMountingTask connectPanelFromGangTask;

    private WireMountingTask terminateLightBoxTask;
    private WireMountingTask terminateOutletBoxTask;
    private WireMountingTask terminateFanBoxTask;
    private GangBoxWireMountingTask terminateGangBoxBondsTask;
    private GangBoxWireMountingTask terminateGangBoxNeutralsTask;
    private GangBoxWireMountingTask terminateGangBoxHotsTask;

    private List<CableMountingTask> cableMountingTasks;
    private Dictionary<Task, List<InteractableWire>> taskWireMap;

    private const string ScenePath = "Assets/Tests/PlayMode/TestScenes/FeedbackHandlerTest.unity";
    private const string FeedbackDescription = "Too much material";

    [UnitySetUp]
    [Category("BuildServer")]
    public IEnumerator Setup()
    {
        // Load the scene with all the test objects
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
            ScenePath, new LoadSceneParameters(LoadSceneMode.Single));

        // Find test game objects UNOPTIMIZED AND SLOW
        GameObject[] allObjects = TestUtils.GetAllObjects();
        (_, taskHandler) = TestUtils.FindGameObject<TaskHandler>(allObjects, "Task Handler");
        (_, boxMounter) = TestUtils.FindGameObject<BoxMounter>(allObjects, "Box Mounter");
        (_, cableMounter) = TestUtils.FindGameObject<CableMounter>(allObjects, "Cable Mounter");
        (_, feedbackHandler) = TestUtils.FindGameObject<FeedbackHandler>(allObjects, "Feedback Handler");
        (_, supplyCableSelectionMenuController) =
            TestUtils.FindGameObject<SupplyCableSelectionMenuController>(allObjects, "Supply Cable to Boxes Menu");
        (_, boxMountingSelectionMenuController) =
            TestUtils.FindGameObject<BoxMountingSelectionMenuController>(allObjects, "Box Mounting Selection Menu");
        (_, terminateCablesMenuController) =
            TestUtils.FindGameObject<TerminateCablesMenuController>(allObjects, "Terminate Cables Menu");
        (_, terminateGangBoxSubMenuController) =
            TestUtils.FindGameObject<TerminateGangBoxSubMenuController>(allObjects, "Terminate Gang Box Sub Menu");
        (_, deviceMenuSelectionController) =
            TestUtils.FindGameObject<DeviceSelectionMenuController>(allObjects, "Device Selection Menu");
        
    
        // Setup Event Listeners
        feedbackHandler.TaskFeedbackProcessed.AddListener((arg0 =>
        {
            Debug.Log("Received new map");
            currentTaskFeedbackMap = arg0;
        }));
    }

    [UnityTearDown]
    [Category("BuildServer")]
    public IEnumerator TearDown()
    {
        yield return SceneManager.UnloadSceneAsync(ScenePath);

        currentTaskFeedbackMap = default;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessMountBoxesTasks_SomeIncorrect()
    {
        // Arrange (Simulate placing boxes)

        FindBoxMountingTasks();

        // Light Box: Incorrect Location, Incorrect Box
        yield return SkipSingleBoxMountingTask(lightBoxMountingTask, false, false);

        // Fan Box : Incorrect Location, Correct Box
        yield return SkipSingleBoxMountingTask(fanBoxMountingTask, false, true);

        // Outlet Box : Correct Location, Incorrect Box
        yield return SkipSingleBoxMountingTask(outletBoxMountingTask, true, false);

        // Gang Box: Correct Location, Correct Location
        yield return SkipSingleBoxMountingTask(gangBoxMountingTask, true, true);

        taskHandler.SupervisorFeedbackHandler();
        taskHandler.CompleteAndDisplayFeedback();
        yield return null;

        // Act
        feedbackHandler.ProcessMountBoxesTasks();
        yield return null;

        // Assert
        Assert.That(lightBoxMountingTask.Action == (MountableAction.Move | MountableAction.Replace));
        Assert.That(HasSingleOptionDisabled(lightBoxMountingTask, lightBoxMountingTask.SelectedMountable.Name,
            boxMountingSelectionMenuController));

        Assert.That(fanBoxMountingTask.Action == MountableAction.Move);
        Assert.That(HasSingleOptionEnabled(fanBoxMountingTask, fanBoxMountingTask.SelectedMountable.Name,
            boxMountingSelectionMenuController));

        Assert.That(outletBoxMountingTask.Action == MountableAction.Replace);
        Assert.That(HasSingleOptionDisabled(outletBoxMountingTask, outletBoxMountingTask.SelectedMountable.Name,
            boxMountingSelectionMenuController));

        Assert.That(gangBoxMountingTask.Action == MountableAction.None);
        Assert.That(HasAllTaskOptionsDisabled(gangBoxMountingTask, boxMountingSelectionMenuController));
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessMountBoxesTasks_AllCorrect()
    {
        // Arrange (Simulate placing boxes correctly)
        FindBoxMountingTasks();
        taskHandler.SkipBoxMountingTask();
        yield return null;

        // Act
        feedbackHandler.ProcessMountBoxesTasks();
        yield return null;

        // Assert
        Assert.That(lightBoxMountingTask.Action == MountableAction.None);
        Assert.That(fanBoxMountingTask.Action == MountableAction.None);
        Assert.That(outletBoxMountingTask.Action == MountableAction.None);
        Assert.That(gangBoxMountingTask.Action == MountableAction.None);
        Assert.That(HasAllTaskOptionsDisabled(lightBoxMountingTask, boxMountingSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(fanBoxMountingTask, boxMountingSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(outletBoxMountingTask, boxMountingSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(gangBoxMountingTask, boxMountingSelectionMenuController));
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessRunSupplyCableTasks_SomeIncorrect()
    {
        // Arrange

        FindCableMountingTasks();

        // Light from Gang (Incorrect Selection)
        connectLightFromGangTask.RequestedMountableName = MountableName.LightToGangA;

        // Panel from Gang (Incorrect Selection
        connectPanelFromGangTask.RequestedMountableName = MountableName.GangToPanelB;

        // Fan from Gang (Correct Selection)
        connectFanFromGangTask.RequestedMountableName = MountableName.FanToGangA;

        // Outlet from Gang (Correct Selection)
        connectOutletFromGangTask.RequestedMountableName = MountableName.OutletToGangB;

        // Simulate running all supply cables
        yield return SkipAllRunSupplyCableTasks(cableMountingTasks);

        // Act
        feedbackHandler.ProcessRunSupplyCableTasks();
        yield return null;

        // Assert
        Assert.That(feedbackHandler.TotalSupplyCablesTasksCorrectlyCompleted.Equals(2));

        Assert.That(HasSingleOptionDisabled(connectLightFromGangTask, MountableName.LightToGangA,
            supplyCableSelectionMenuController));
        Assert.That(SupplyCableTaskHasSpecificFeedback(connectLightFromGangTask, FeedbackDescription));

        Assert.That(HasSingleOptionDisabled(connectPanelFromGangTask, MountableName.GangToPanelB,
            supplyCableSelectionMenuController));
        Assert.That(SupplyCableTaskHasSpecificFeedback(connectPanelFromGangTask, FeedbackDescription));

        Assert.That(HasAllTaskOptionsDisabled(connectFanFromGangTask, supplyCableSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(connectOutletFromGangTask, supplyCableSelectionMenuController));
    }

    [UnityTest]
    public IEnumerator ProcessRunSupplyCableTasks_AllCorrect()
    {
        // Arrange

        FindCableMountingTasks();

        // TODO:
        // Replace with SkipRunSupplyCableTasks functionality 

        connectLightFromGangTask.RequestedMountableName = MountableName.LightToGangC;
        connectPanelFromGangTask.RequestedMountableName = MountableName.GangToPanelC;
        connectFanFromGangTask.RequestedMountableName = MountableName.FanToGangA;
        connectOutletFromGangTask.RequestedMountableName = MountableName.OutletToGangB;

        // Simulate running all supply cables correctly
        yield return SkipAllRunSupplyCableTasks(cableMountingTasks);

        // Act
        feedbackHandler.ProcessRunSupplyCableTasks();
        yield return null;

        // Assert
        Assert.That(feedbackHandler.TotalSupplyCablesTasksCorrectlyCompleted.Equals(4));
        Assert.That(HasAllTaskOptionsDisabled(connectLightFromGangTask, supplyCableSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(connectPanelFromGangTask, supplyCableSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(connectFanFromGangTask, supplyCableSelectionMenuController));
        Assert.That(HasAllTaskOptionsDisabled(connectOutletFromGangTask, supplyCableSelectionMenuController));
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessWireTerminationTasks_SomeIncorrect()
    {
        // Arrange

        taskHandler.SkipBoxMountingTask();
        yield return null;

        feedbackHandler.SupplyCableTaskFeedbackInit();
        yield return null;

        FindTerminationTasks();
        yield return null;

        FetchInteractableWires();
        yield return null;

        List<InteractableWire> interactableWires;

        // Arrange and Act Per Termination Task

        // Terminate Fan Box : Bond Correct, Neutral Correct, Hot Incorrect
        interactableWires = taskWireMap[terminateFanBoxTask.TaskName];
        terminateFanBoxTask.SelectedOptionsMap[MountableName.BondWire] =
            GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TiedToBond);

        terminateFanBoxTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(interactableWires, MountableName.NeutralWire, TerminationOptionType.TuckedNeat);

        terminateFanBoxTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(interactableWires, MountableName.HotWire, TerminationOptionType.BentOut);

        feedbackHandler.ProcessTerminationWireForFeedback(terminateFanBoxTask);
        yield return null;

        // Terminate Light Box : Bond Incorrect, Neutral Correct, Hot Correct
        interactableWires = taskWireMap[terminateLightBoxTask.TaskName];
        terminateLightBoxTask.SelectedOptionsMap[MountableName.BondWire] =
            GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TuckedNeat);

        terminateLightBoxTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(interactableWires, MountableName.NeutralWire, TerminationOptionType.TuckedNeat);

        terminateLightBoxTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(interactableWires, MountableName.HotWire, TerminationOptionType.TuckedNeat);

        feedbackHandler.ProcessTerminationWireForFeedback(terminateLightBoxTask);
        yield return null;

        // Terminate Outlet Box : Bond Correct, Neutral Incorrect, Hot Correct
        interactableWires = taskWireMap[terminateOutletBoxTask.TaskName];
        terminateOutletBoxTask.SelectedOptionsMap[MountableName.BondWire] =
            GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TiedToBond);

        terminateOutletBoxTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(interactableWires, MountableName.NeutralWire, TerminationOptionType.BentOut);

        terminateOutletBoxTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(interactableWires, MountableName.HotWire, TerminationOptionType.TuckedNeat);

        feedbackHandler.ProcessTerminationWireForFeedback(terminateOutletBoxTask);
        yield return null;

        // Terminate Gang Box Bonds : One Bond Incorrect
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TuckedNeat; // Incorrect
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToBond;

        SetGangBoxSubTaskConductor(terminateGangBoxBondsTask, MountableName.BondWire);

        feedbackHandler.ProcessTerminationWireForFeedback(terminateGangBoxBondsTask);
        yield return null;

        // Terminate Gang Box Neutrals : One Neutral Incorrect
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TuckedNeat; // Incorrect
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;

        SetGangBoxSubTaskConductor(terminateGangBoxNeutralsTask, MountableName.NeutralWire);

        feedbackHandler.ProcessTerminationWireForFeedback(terminateGangBoxNeutralsTask);
        yield return null;

        // Terminate Gang Box Hots : One Hot Incorrect
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers; // Incorrect
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TuckedNeat;
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;

        SetGangBoxSubTaskConductor(terminateGangBoxHotsTask, MountableName.HotWire);

        feedbackHandler.ProcessTerminationWireForFeedback(terminateGangBoxHotsTask);
        yield return null;

        // Assert
        Assert.That(TerminationTaskHasSpecificFeedback(terminateFanBoxTask, "<b>Hot Conductor:</b>"));
        Assert.That(TerminationTaskHasSpecificFeedback(terminateLightBoxTask, "<b>Bond Conductor:</b>"));
        Assert.That(TerminationTaskHasSpecificFeedback(terminateOutletBoxTask, "<b>Neutral Conductor:</b>"));
        Assert.That(TerminationTaskHasSpecificFeedback(terminateGangBoxBondsTask, "<b>Bond Conductor from Fan:</b>"));
        Assert.That(TerminationTaskHasSpecificFeedback(terminateGangBoxNeutralsTask, "<b>Neutral Conductor from Light:</b>"));
        Assert.That(TerminationTaskHasSpecificFeedback(terminateGangBoxHotsTask, "<b>Hot Conductor from Fan:</b>"));
        Assert.That(feedbackHandler.TotalSubGangBoxTasksCorrectlyCompleted.Equals(0));
        Assert.That(HasAllTaskButtonsInState(terminateCablesMenuController, true));
        Assert.That(HasAllTaskButtonsInState(terminateGangBoxSubMenuController, true));

    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator VerifyGangBoxConductorDisabledAfterFeedback()
    {
        // Arrange
        taskHandler.SkipBoxMountingTask();
        yield return null;

        feedbackHandler.SupplyCableTaskFeedbackInit();
        yield return null;

        FindTerminationTasks();
        yield return null;

        FetchInteractableWires();
        yield return null;

        // Setup the gang box bond wires with correct termination options
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToBond;

        // Set the conductor type for all gang boxes
        SetGangBoxSubTaskConductor(terminateGangBoxBondsTask, MountableName.BondWire);

        // Process feedback to mark the wires as correctly completed
        feedbackHandler.ProcessTerminationWireForFeedback(terminateGangBoxBondsTask);
        yield return null;

        // Get the interactable wires for the gang box bonds task
        List<InteractableWire> interactableWires = taskWireMap[terminateGangBoxBondsTask.TaskName];

        // Find a bond wire with TiedToBond termination option
        InteractableWire gangBoxBondWire = GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TiedToBond);

        // Verify the wire was found
        Assert.IsNotNull(gangBoxBondWire, "Could not find a bond wire with TiedToBond termination option");

        // Check if the wire is now on the ignore raycast layer (disabled)
        int ignoreRaycastLayer = LayerMask.NameToLayer(SceneInteractions.LayerIgnoreRaycast);
        Assert.AreEqual(ignoreRaycastLayer, gangBoxBondWire.gameObject.layer,
            "Conductor should be on ignore raycast layer after feedback processing");

        // Then select the task again
        taskHandler.OnMountingTaskClick(Task.TerminateGangBoxBonds);
        yield return new WaitForSeconds(0.1f);

        // Verify the wire is still on the ignore raycast layer
        Assert.AreEqual(ignoreRaycastLayer, gangBoxBondWire.gameObject.layer,
            "Conductor should remain on ignore raycast layer after reselecting the task");
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessWireTerminationTasks_AllCorrect()
    {
        // Arrange

        taskHandler.SkipBoxMountingTask();
        yield return null;

        feedbackHandler.SupplyCableTaskFeedbackInit();
        yield return null;

        FindTerminationTasks();
        yield return null;

        FetchInteractableWires();
        yield return null;

        List<InteractableWire> interactableWires;

        // Terminate Fan Box
        interactableWires = taskWireMap[terminateFanBoxTask.TaskName];
        terminateFanBoxTask.SelectedOptionsMap[MountableName.BondWire] =
            GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TiedToBond);

        terminateFanBoxTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(interactableWires, MountableName.NeutralWire, TerminationOptionType.TuckedNeat);

        terminateFanBoxTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(interactableWires, MountableName.HotWire, TerminationOptionType.TuckedNeat);

        // Terminate Light Box
        interactableWires = taskWireMap[terminateLightBoxTask.TaskName];
        terminateLightBoxTask.SelectedOptionsMap[MountableName.BondWire] =
            GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TiedToBond);

        terminateLightBoxTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(interactableWires, MountableName.NeutralWire, TerminationOptionType.TuckedNeat);

        terminateLightBoxTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(interactableWires, MountableName.HotWire, TerminationOptionType.TuckedNeat);

        // Terminate Outlet Box
        interactableWires = taskWireMap[terminateOutletBoxTask.TaskName];
        terminateOutletBoxTask.SelectedOptionsMap[MountableName.BondWire] =
            GetInteractableWire(interactableWires, MountableName.BondWire, TerminationOptionType.TiedToBond);

        terminateOutletBoxTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(interactableWires, MountableName.NeutralWire, TerminationOptionType.TuckedNeat);

        terminateOutletBoxTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(interactableWires, MountableName.HotWire, TerminationOptionType.TuckedNeat);


        // Terminate Gang Box Bonds
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TiedToBond;
        terminateGangBoxBondsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToBond;

        SetGangBoxSubTaskConductor(terminateGangBoxBondsTask, MountableName.BondWire);

        // Terminate Gang Box Neutrals
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxNeutralsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;

        SetGangBoxSubTaskConductor(terminateGangBoxNeutralsTask, MountableName.NeutralWire);

        // Terminate Gang Box Hots
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.DeviceBox].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.FanBox].TerminationOption =
            TerminationOptionType.TuckedNeat;
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].TerminationOption =
            TerminationOptionType.TuckedNeat;
        terminateGangBoxHotsTask.GangBoxSelectedOptionsMap[MountableName.Panel].TerminationOption =
            TerminationOptionType.TiedToWireNutWithOthers;

        SetGangBoxSubTaskConductor(terminateGangBoxHotsTask, MountableName.HotWire);

        // Act
        feedbackHandler.ProcessTerminationTasks();
        yield return null;

        // Assert
        Assert.That(currentTaskFeedbackMap.Values.All(list => list.Count == 0)); // All tasks correct
        Assert.That(feedbackHandler.TotalSubGangBoxTasksCorrectlyCompleted == feedbackHandler.TotalSubGangBoxTasks);
        Assert.That(HasAllTaskButtonsInState(terminateCablesMenuController, false));
        Assert.That(HasAllTaskButtonsInState(terminateGangBoxSubMenuController, false));
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcesssDeviceTaskFeedback_CorrectDeviceAndIncorrectConnection()
    {
        // Skip box mounting task
        taskHandler.SkipBoxMountingTask();
        yield return null;
        
        // retrieve the task
        var fanDeviceTask = GameObject.Find("Install Fan Task").GetComponent<DeviceMountingTask>();
        var wireMountingTask = fanDeviceTask.GetComponentInChildren<WireMountingTask>();
        
        Device installedDevice = GameObject.Find("14 Inch Exhaust Fan - Installed").GetComponentInChildren<Device>();
        fanDeviceTask.SelectedMountable = installedDevice;

        GameObject fanDeviceSupply = GameObject.Find("Fan Wire Connector - Supply");
        List<InteractableWire> deviceConductors = fanDeviceSupply.GetComponentsInChildren<InteractableWire>(true).ToList();
        
        // Incorrect
        wireMountingTask.SelectedOptionsMap[MountableName.BondWire] = 
                GetInteractableWire(deviceConductors, MountableName.BondWire, TerminationOptionType.TiedToNeutral);
        // Correct
        wireMountingTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(deviceConductors, MountableName.NeutralWire, TerminationOptionType.TiedToNeutral);
        // Correct
        wireMountingTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(deviceConductors, MountableName.HotWire, TerminationOptionType.TiedToHot);
        
        feedbackHandler.ProcessAllDeviceTasksForFeedback();
        Assert.AreEqual(0, feedbackHandler.TotalDeviceTasksCorrectlyCompleted);
        Assert.That(TerminationTaskHasSpecificFeedback(wireMountingTask, "<b>Bond Conductor:</b>"));
        Assert.That(HasSingleOptionEnabled(fanDeviceTask, 
            MountableName.ExhaustFan14In, deviceMenuSelectionController));

    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessDeviceTaskFeedback_IncorrectDevice()
    {
        taskHandler.SkipBoxMountingTask();
        yield return null;
        
        // retrieve the task
        var fanDeviceTask = GameObject.Find("Install Fan Task").GetComponent<DeviceMountingTask>();
        
        Device installedDevice = GameObject.Find("6 Inch Ventilation Fan - Installed").GetComponentInChildren<Device>();
        fanDeviceTask.SelectedMountable = installedDevice;
        const int ONLY_ONE_FEEDBACK = 1; // TODO check format const variable supposed to be.
        
        feedbackHandler.ProcessAllDeviceTasksForFeedback();
        Assert.AreEqual(0, feedbackHandler.TotalDeviceTasksCorrectlyCompleted);
        Assert.That(DeviceTaskHasSpecificFeedback(fanDeviceTask, "Inappropriate device"));
        Assert.AreEqual(ONLY_ONE_FEEDBACK,currentTaskFeedbackMap.Count);
        
        yield return null;
    }

    [UnityTest]
    [Category("BuildServer")]
    public IEnumerator ProcessDeviceTaskFeedback_CorrectDeviceAndCorrectConnection()
    {
        // Skip box mounting task
        taskHandler.SkipBoxMountingTask();
        yield return null;
        
        // retrieve the task
        var fanDeviceTask = GameObject.Find("Install Fan Task").GetComponent<DeviceMountingTask>();
        var wireMountingTask = fanDeviceTask.GetComponentInChildren<WireMountingTask>();
        
        Device installedDevice = GameObject.Find("14 Inch Exhaust Fan - Installed").GetComponentInChildren<Device>();
        fanDeviceTask.SelectedMountable = installedDevice;
        
        GameObject fanDeviceSupply = GameObject.Find("Fan Wire Connector - Supply");
        List<InteractableWire> deviceConductors = fanDeviceSupply.GetComponentsInChildren<InteractableWire>(true).ToList();
        
        // Incorrect
        wireMountingTask.SelectedOptionsMap[MountableName.BondWire] = 
            GetInteractableWire(deviceConductors, MountableName.BondWire, TerminationOptionType.TiedToBond);
        // Correct
        wireMountingTask.SelectedOptionsMap[MountableName.NeutralWire] =
            GetInteractableWire(deviceConductors, MountableName.NeutralWire, TerminationOptionType.TiedToNeutral);
        // Correct
        wireMountingTask.SelectedOptionsMap[MountableName.HotWire] =
            GetInteractableWire(deviceConductors, MountableName.HotWire, TerminationOptionType.TiedToHot);

        const int NO_FEEDBACK = 0;
        feedbackHandler.ProcessAllDeviceTasksForFeedback();
        Assert.AreEqual(1, feedbackHandler.TotalDeviceTasksCorrectlyCompleted);
        Assert.That(HasAllTaskOptionsDisabled(fanDeviceTask, deviceMenuSelectionController));
        Assert.AreEqual(NO_FEEDBACK,currentTaskFeedbackMap[Task.ConnectFan].Count);

        yield return null;
    }
    
    /// <summary>
    ///     Coroutine used to skip a single <param name="task">box mounting task</param> that initially
    ///     sets the <param name="hasCorrectLocation">location of the box</param> and the
    ///     <param name="hasCorrectBox">box to mount</param>
    /// </summary>
    private IEnumerator SkipSingleBoxMountingTask(BoxMountingTask task, bool hasCorrectLocation, bool hasCorrectBox)
    {
        SetBoxLocation(task, hasCorrectLocation);
        SetBoxMountable(task, hasCorrectBox);

        boxMounter.AutomaticMountablePlace(task);
        taskHandler.OnMountingTaskComplete(task);
        yield return null;
    }

    /// <summary>
    ///     Coroutine used to simulate mounting all cable mounting tasks in a <param name="tasks">list</param>
    /// </summary>
    private IEnumerator SkipAllRunSupplyCableTasks(List<CableMountingTask> tasks)
    {
        // TODO:
        // Replace functionality with cableMounter.AutomaticMountablePlace when implemented
        foreach (CableMountingTask task in tasks)
        {
            cableMounter.SecuredCablesEnabled = true;
            cableMounter.OnMountableReplace(task);
            yield return null;
        }
    }

    /// <summary>
    ///     Utility method to set the conductor for all values in the SelectedOptionsMap
    ///     of a gang box sub-task.
    /// </summary>
    /// <remarks>
    ///     This is useful given that we are simulating input and not going through the usual Task Handler
    ///     workflow.
    /// </remarks>
    private void SetGangBoxSubTaskConductor(GangBoxWireMountingTask task, MountableName conductor)
    {
        task.GangBoxSelectedOptionsMap[MountableName.DeviceBox].Conductor = conductor;
        task.GangBoxSelectedOptionsMap[MountableName.OctagonalBracketBox].Conductor = conductor;
        task.GangBoxSelectedOptionsMap[MountableName.FanBox].Conductor = conductor;
        task.GangBoxSelectedOptionsMap[MountableName.Panel].Conductor = conductor;
    }

    /// <summary>
    ///     Finds an InteractableWire in the provided <param name="interactableWires">list</param>
    ///     that has the given <param name="type">mountable name</param> and <param name="terminationOption">termination
    ///     option</param>
    /// </summary>
    private InteractableWire GetInteractableWire(List<InteractableWire> interactableWires, MountableName type,
        TerminationOptionType terminationOption)
    {
        InteractableWire returnWire =
            interactableWires.Find(wire => wire.Name == type && wire.Option == terminationOption);

        return returnWire;
    }

    /// <summary>
    ///     Utility method to find all box mounting tasks in the scene
    /// </summary>
    private void FindBoxMountingTasks()
    {
        lightBoxMountingTask = GameObject.Find("Mount Light Box Task").GetComponent<BoxMountingTask>();
        fanBoxMountingTask = GameObject.Find("Mount Fan Box Task").GetComponent<BoxMountingTask>();
        outletBoxMountingTask = GameObject.Find("Mount Outlet Box Task").GetComponent<BoxMountingTask>();
        gangBoxMountingTask = GameObject.Find("Mount Gang Box Task").GetComponent<BoxMountingTask>();
    }

    /// <summary>
    ///     Utility method to find all cable mounting tasks in the scene
    /// 
    /// </summary>
    private void FindCableMountingTasks()
    {
        connectFanFromGangTask = GameObject.Find("Connect Fan to Gang Task").GetComponent<CableMountingTask>();
        connectLightFromGangTask = GameObject.Find("Connect Light Box to Gang Task").GetComponent<CableMountingTask>();
        connectPanelFromGangTask = GameObject.Find("Connect HomeRun To Gang Task").GetComponent<CableMountingTask>();
        connectOutletFromGangTask = GameObject.Find("Connect Outlet to Gang Task").GetComponent<CableMountingTask>();

        // Add all tasks to a known list
        cableMountingTasks = new List<CableMountingTask>
        {
            connectFanFromGangTask,
            connectLightFromGangTask,
            connectOutletFromGangTask,
            connectPanelFromGangTask
        };
    }

    /// <summary>
    ///     Utility method to find all termination tasks in the scene
    /// </summary>
    private void FindTerminationTasks()
    {
        terminateLightBoxTask = GameObject.Find("Terminate Light Box Task").GetComponent<WireMountingTask>();
        terminateOutletBoxTask = GameObject.Find("Terminate Outlet Box Task").GetComponent<WireMountingTask>();
        terminateFanBoxTask = GameObject.Find("Terminate Fan Box Task").GetComponent<WireMountingTask>();
        terminateGangBoxBondsTask =
            GameObject.Find("Terminate Gang Box Bonds Task").GetComponent<GangBoxWireMountingTask>();
        terminateGangBoxNeutralsTask =
            GameObject.Find("Terminate Gang Box Neutrals Task").GetComponent<GangBoxWireMountingTask>();
        terminateGangBoxHotsTask =
            GameObject.Find("Terminate Gang Box Hots Task").GetComponent<GangBoxWireMountingTask>();
    }

    /// <summary>
    ///     Verifies that the local Task Feedback Map contains feedback information for the
    ///     <param name="task">CableMountingTask</param>
    /// </summary>
    private bool SupplyCableTaskHasSpecificFeedback(CableMountingTask task, string feedback)
    {
        List<RoughInFeedbackInformation> feedbackList = currentTaskFeedbackMap[task.TaskName];

        return feedbackList.Any(f => f.FeedbackDescription.Equals(feedback));
    }
    
    /// <summary>
    ///     Verifies that the local Task Feedback Map contains feedback information for the
    ///     <param name="task">CableMountingTask</param>
    /// </summary>
    private bool DeviceTaskHasSpecificFeedback(DeviceMountingTask task, string feedback)
    {
        List<RoughInFeedbackInformation> feedbackList = currentTaskFeedbackMap[task.TaskName];

        return feedbackList.Any(f => f.FeedbackDescription.Equals(feedback));
    }
    
    /// <summary>
    ///     Verifies that the local Task Feedback Map contains feedback information for the
    ///     <param name="task">WireMountingTask</param>
    ///
    ///     This check uses a regex expression to check for the following format
    ///     <param name="prefix">prefix</param> _characters_, e.g., Hot Wire: Feedback where "Hot Wire" is
    ///     the prefix and "Feedback" is the characters
    /// </summary>
    private bool TerminationTaskHasSpecificFeedback(WireMountingTask task, string prefix)
    {
        List<RoughInFeedbackInformation> feedbackList = currentTaskFeedbackMap[task.TaskName];

        // Create the regex pattern using the provided prefix
        string pattern = $@"^{Regex.Escape(prefix)} (.+)$"; // Match prefix followed by any text

        foreach (var f in feedbackList)
        {
            Debug.Log(f.CodeViolation);
            Debug.Log(f.FeedbackDescription);

            // Match the feedback against the regex pattern
            if (!Regex.IsMatch(f.FeedbackDescription, pattern) && !Regex.IsMatch(f.CodeViolation, pattern))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Verifies that all task buttons for a <param name="controller">MenuController</param>
    ///     match the given <param name="state">state</param>
    /// </summary>
    private bool HasAllTaskButtonsInState(MenuController controller, bool state)
    {
        var root = controller.GetComponent<UIDocument>().rootVisualElement;

        return root.Query<Button>(className: UIHelper.ClassTaskOption)
            .ToList()
            .All(b => b.enabledInHierarchy.Equals(state));
    }

    /// <summary>
    ///     Verifies that all task options are disabled for the <param name="tabbedMenuController"></param>
    /// </summary>
    private bool HasAllTaskOptionsDisabled(MountingTask mountingTask, TabbedTaskMenuController tabbedMenuController)
    {
        List<VisualElement> taskOptions = tabbedMenuController.GetTaskOptions(mountingTask.TaskName);

        return !taskOptions.All(element => element.enabledInHierarchy);
    }

    /// <summary>
    ///     Verifies that only the specified <param name="option"></param>
    ///     task option is disabled for the <param name="tabbedMenuController"></param>
    /// </summary>
    private bool HasSingleOptionDisabled(MountingTask mountingTask, MountableName option,
        TabbedTaskMenuController tabbedMenuController)
    {
        List<VisualElement> taskOptions = tabbedMenuController.GetTaskOptions(mountingTask.TaskName);

        foreach (VisualElement taskOption in taskOptions)
        {
            // Only the option provided should be disabled
            if (taskOption.viewDataKey.StartsWith(option.ToString()))
            {
                if (taskOption.enabledInHierarchy)
                {
                    return false;
                }
            }
            else
            {
                if (!taskOption.enabledInHierarchy)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Verifies that only the specified <param name="option"></param>
    ///     task option is enabled for the <param name="tabbedMenuController"></param>
    /// </summary>
    private bool HasSingleOptionEnabled(MountingTask mountingTask, MountableName option,
        TabbedTaskMenuController tabbedMenuController)
    {
        List<VisualElement> taskOptions = tabbedMenuController.GetTaskOptions(mountingTask.TaskName);

        foreach (VisualElement taskOption in taskOptions)
        {
            if (taskOption.viewDataKey.StartsWith(option.ToString()))
            {
                // Only the option provided should be enabled
                if (!taskOption.enabledInHierarchy)
                {
                    return false;
                }
            }
            else
            {
                if (taskOption.enabledInHierarchy)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Sets the location of a BoxMountingTask based on the flag
    ///     <param name="isCorrect"></param>
    /// </summary>
    /// <param name="boxMountingTask">The current box mounting task</param>
    /// <param name="isCorrect">Whether to select the correct location for this task or a random incorrect one</param>
    private static void SetBoxLocation(BoxMountingTask boxMountingTask, bool isCorrect)
    {
        if (isCorrect)
        {
            boxMountingTask.SelectedLocation = boxMountingTask.CorrectLocation;
        }
        else
        {
            // If the location to set is incorrect, we don't care which one is chosen
            boxMountingTask.SelectedLocation =
                GetRandomOption(boxMountingTask.LocationOptions, boxMountingTask.CorrectLocation);
        }
    }

    /// <summary>
    ///     Sets the mountable (box) of a BoxMountingTask based on the flag
    ///     <param name="isCorrect"></param>
    /// </summary>
    /// <param name="boxMountingTask">The current box mounting task</param>
    /// <param name="isCorrect">Whether to select the correct mountable (box) for this task or a random incorrect one</param>
    private static void SetBoxMountable(BoxMountingTask boxMountingTask, bool isCorrect)
    {
        if (isCorrect)
        {
            boxMountingTask.RequestedMountableName = boxMountingTask.CorrectMountable;
        }
        else
        {
            // If the box to select is incorrect, we don't care which one is chosen
            boxMountingTask.RequestedMountableName =
                GetRandomOption(boxMountingTask.MountableOptions, boxMountingTask.CorrectMountable);
        }
    }

    /// <summary>
    ///     Returns a random option from an array of any type that does not match
    ///     the correct value from those options
    /// </summary>
    private static t GetRandomOption<t>(t[] options, t correctValue)
    {
        if (options.Length == 0)
        {
            throw new InvalidOperationException("Options array cannot be empty.");
        }

        t selectedOption;

        do
        {
            // Continue selecting a random option until it does not match with the correct value
            int randomIndex = UnityEngine.Random.Range(0, options.Length);
            selectedOption = options[randomIndex];
        } while (EqualityComparer<t>.Default.Equals(selectedOption, correctValue));

        return selectedOption;
    }

    /// <summary>
    ///     Utility method to fetch all interactable wires in the scene (includes inactive)
    /// </summary>
    private void FetchInteractableWires()
    {
        taskWireMap = new Dictionary<Task, List<InteractableWire>>();

        InteractableWire[] interactableWires =
            Object.FindObjectsByType<InteractableWire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (InteractableWire wire in interactableWires)
        {
            if (!taskWireMap.TryGetValue(wire.TerminationTask, out List<InteractableWire> wireList))
            {
                taskWireMap[wire.TerminationTask] = new List<InteractableWire> { wire };
            }
            else
            {
                wireList.Add(wire);
            }
        }
    }
}