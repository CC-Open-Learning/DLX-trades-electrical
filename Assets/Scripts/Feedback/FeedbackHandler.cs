using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace VARLab.TradesElectrical
{
    public class FeedbackHandler : MonoBehaviour
    {
        private struct BoxTaskStatus
        {
            public bool IsLocationCorrect;
            public bool IsCorrectMountable;
        }

        // Possibly replace these references with the TaskMenuRouter since it holds all relevant UI controller references
        [SerializeField] private TaskSelectionMenuController taskSelectionUI;
        [SerializeField] private TabbedTaskMenuController itemSelectionUI;
        [SerializeField] private TabbedTaskMenuController supplyCableSelectionUI;
        [SerializeField] private RunSupplyCableMenuController runSupplyCableUI;
        [SerializeField] private TerminateCablesMenuController terminateCablesMenuController;
        [SerializeField] private TerminateGangBoxSubMenuController terminateGangBoxSubMenuController;
        [SerializeField] private DeviceSelectionMenuController deviceSelectionMenuController;
        [SerializeField] private SceneInteractions sceneInteractions;
        
        public UnityEvent<SortedDictionary<Task, List<RoughInFeedbackInformation>>> TaskFeedbackProcessed = new();
        
        private readonly Dictionary<Task, MountingTask> AllMountingTasksMap = new();

        private readonly Dictionary<Task, HashSet<MountableName>> DisabledTaskOption = new();

        private readonly Dictionary<Task, List<InteractableWire>> TaskWireMap = new();

        private readonly SortedDictionary<Task, List<RoughInFeedbackInformation>> TaskFeedbackMap = new();

        private readonly HashSet<WireMountingTask> CorrectlyCompletedWireMountingTasks = new();

        private readonly HashSet<CableMountingTask> CorrectlyCompletedSupplyTasks = new();
        
        private readonly HashSet<DeviceMountingTask> CorrectlyCompletedDeviceTasks = new();

        private Dictionary<Task, BoxTaskStatus> givenBoxFeedback = new();
        
        public int TotalSubGangBoxTasks { get; private set; }
        public int TotalBoxMountingTasks { get; private set; }
        public int TotalTerminationTasks { get; private set; }
        public int TotalSupplyCableTasks { get; private set; }
        public int TotalDeviceTasks { get; private set; }
        public int TotalSupplyCablesTasksCorrectlyCompleted { get; private set; }
        public int TotalSubGangBoxTasksCorrectlyCompleted { get; private set; }
        public int TotalDeviceTasksCorrectlyCompleted { get; private set; }

        public bool AllSupplyCablesTasksCorrectlyCompleted { get; private set; }
        public bool AllTerminationTasksCorrectlyComplete { get; private set; }
        
        public bool AllDeviceTasksCorrectlyComplete { get; private set; }


        public void Awake()
        {
            MountingTask[] mountingTasks =
                FindObjectsByType<MountingTask>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (MountingTask mountingTask in mountingTasks)
            {
                switch (mountingTask)
                {
                    case BoxMountingTask:
                        TotalBoxMountingTasks++;
                        break;
                    case CableMountingTask:
                        TotalSupplyCableTasks++;
                        break;
                    case GangBoxWireMountingTask:
                        TotalSubGangBoxTasks++;
                        TotalTerminationTasks++;
                        break;
                    case WireMountingTask:
                        // As device connection tasks reuse WireMountingTasks, we filter termination tasks by
                        // their TaskName enum values
                        if (mountingTask.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
                        {
                            TotalTerminationTasks++;
                        }
                        else
                        {
                            continue;
                        }

                        break;
                    case DeviceMountingTask:
                        TotalDeviceTasks++;
                        break;
                }

                AllMountingTasksMap.Add(mountingTask.TaskName, mountingTask);
            }

            foreach (var task in AllMountingTasksMap.Keys)
            {
                DisabledTaskOption.Add(task, new HashSet<MountableName>());
            }
        }

        public void SupplyCableTaskFeedbackInit()
        {
            FetchInteractableWires();
        }


        /// <summary>
        ///     Method That handles GC Invokes and updates the status of the mounting task.
        ///     Invoked by <see cref="ReminderDialog.ConfirmButtonPressed" />
        /// </summary>
        public void ProcessMountBoxesTasks()
        {
            foreach (BoxMountingTask mountTask in AllMountingTasksMap.Values.OfType<BoxMountingTask>())
            {
                bool isMountLocationCorrect = mountTask.CorrectLocation == mountTask.SelectedLocation;
                bool isMountCorrect = mountTask.SelectedMountable.Name == mountTask.CorrectMountable;
                
                bool mountAndLocationCorrect =  isMountCorrect && isMountLocationCorrect;
                bool mountAndLocationIncorrect = !isMountCorrect && !isMountLocationCorrect;

                BoxTaskStatus boxTasksStatus = new BoxTaskStatus
                {
                    IsCorrectMountable = isMountCorrect,
                    IsLocationCorrect = isMountLocationCorrect
                };
                
                if (!givenBoxFeedback.TryAdd(mountTask.TaskName, boxTasksStatus))
                {
                    givenBoxFeedback[mountTask.TaskName] = boxTasksStatus;
                }
                
                bool mountIncorrectAndLocationCorrect = !isMountCorrect && isMountLocationCorrect;
                bool mountCorrectAndLocationIncorrect = isMountCorrect && !isMountLocationCorrect;

                if (mountAndLocationCorrect)
                {
                    itemSelectionUI.SetTaskOptionsEnabled(mountTask.TaskName, false);
                    mountTask.Action = MountableAction.None;
                }
                else if (mountCorrectAndLocationIncorrect)
                {
                    // Sets all options disabled except for the selected element
                    itemSelectionUI.SetTaskOptionEnabledInvert(mountTask.TaskName, mountTask.SelectedMountable.Name,
                        true);
                    mountTask.Action = MountableAction.Move;
                }
                else if (mountIncorrectAndLocationCorrect)
                {
                    DisableTaskOption(mountTask.TaskName, mountTask.SelectedMountable.Name, itemSelectionUI);
                    mountTask.Action = MountableAction.Replace;
                }
                else if (mountAndLocationIncorrect)
                {
                    mountTask.Action = MountableAction.Move | MountableAction.Replace;
                    DisableTaskOption(mountTask.TaskName, mountTask.SelectedMountable.Name, itemSelectionUI);
                }
            }
        }

        public bool GetGivenBoxMountStatus(Task task)
        {
            if (givenBoxFeedback.TryGetValue(task, out BoxTaskStatus status))
            {
                return status.IsCorrectMountable;
            }
            Debug.LogWarning("Box Mounting Feedback has not been asked");
            return false;
        }

        public bool GetGivenBoxLocationStatus(Task task)
        {
            if (givenBoxFeedback.TryGetValue(task, out BoxTaskStatus status))
            {
                return status.IsLocationCorrect;
            }
            Debug.LogWarning("Box Mounting Feedback has not been asked");
            return false;
        }
        
        public void DisableTaskOption(Task task, MountableName mountableName,
            TabbedTaskMenuController taskMenuController)
        {
            if (DisabledTaskOption[task].Count() != 0)
            {
                MountableName previousMountable = DisabledTaskOption[task].First();
                taskMenuController.SetTaskOptionEnabled(task, previousMountable, true);
                DisabledTaskOption[task].Remove(previousMountable);
            }

            taskMenuController.SetTaskOptionEnabled(task, mountableName, false);
            DisabledTaskOption[task].Add(mountableName);
        }

        public void ProcessRunSupplyCableTasks()
        {
            foreach (CableMountingTask mountTask in AllMountingTasksMap.Values.OfType<CableMountingTask>())
            {
                if (CorrectlyCompletedSupplyTasks.Contains(mountTask))
                {
                    continue;
                }

                CheckRunSupplyCableTaskForFeedback(mountTask);

                var cableMountCorrect = mountTask.SelectedMountable.Name == mountTask.CorrectMountable.Name;

                if (cableMountCorrect)
                {
                    supplyCableSelectionUI.SetTaskOptionsEnabled(mountTask.TaskName, false);
                    mountTask.SelectedMountable.gameObject.layer = LayerMask.NameToLayer(SceneInteractions.LayerIgnoreRaycast);
                    sceneInteractions.supplyCablesCollidersToIgnore.Add(mountTask.SelectedMountable.gameObject
                        .GetComponent<Collider>());
                    CorrectlyCompletedSupplyTasks.Add(mountTask);
                    TotalSupplyCablesTasksCorrectlyCompleted += 1;
                }
                else
                {
                    DisableTaskOption(mountTask.TaskName, mountTask.SelectedMountable.Name, supplyCableSelectionUI);
                }
            }

            if (TotalSupplyCablesTasksCorrectlyCompleted == TotalSupplyCableTasks)
            {
                runSupplyCableUI.SetConnectCablesBtnStatus(WorkflowStatus.Complete);
                runSupplyCableUI.SetConnectCablesBtnState(false);
                AllSupplyCablesTasksCorrectlyCompleted = true;
            }
        }

        public void ProcessTerminationTasks()
        {
            IEnumerable<WireMountingTask> wireMountingTasks = AllMountingTasksMap.Values.OfType<WireMountingTask>();
            foreach (WireMountingTask mountTask in wireMountingTasks)
            {
                if (CorrectlyCompletedWireMountingTasks.Contains(mountTask))
                {
                    // Skip processing
                    continue;
                }

                ProcessTerminationWireForFeedback(mountTask);
                if (!mountTask.IsCorrect()) continue;

                if (mountTask is GangBoxWireMountingTask gangBoxWireMountingTask)
                {
                    terminateGangBoxSubMenuController.SetTerminateTaskBtnEnabled
                        (gangBoxWireMountingTask, false);
                    TotalSubGangBoxTasksCorrectlyCompleted += 1;

                    CorrectlyCompletedWireMountingTasks.Add(mountTask);
                    if (TotalSubGangBoxTasksCorrectlyCompleted == TotalSubGangBoxTasks)
                    {
                        terminateCablesMenuController.SetTerminateTaskBtnEnabled(Task.TerminateGangBox, false);
                        // Pick a random wire from the task and make the host box non-interactable
                        MakeHostBoxNonInteractable(mountTask);
                    }
                }
                else
                {
                    CorrectlyCompletedWireMountingTasks.Add(mountTask);
                    terminateCablesMenuController.SetTerminateTaskBtnEnabled(mountTask.TaskName, false);
                    // Pick a random wire from the task and make the host box non-interactable
                    MakeHostBoxNonInteractable(mountTask);
                }
            }

            if (CorrectlyCompletedWireMountingTasks.Count ==
                AllMountingTasksMap.Values.OfType<WireMountingTask>().Count())
            {
                AllTerminationTasksCorrectlyComplete = true;
                runSupplyCableUI.SetTerminateCablesBtnStatus(WorkflowStatus.Complete);
                runSupplyCableUI.SetTerminateCablesBtnState(false);
            }
        }

        private void MakeHostBoxNonInteractable(WireMountingTask wireTask)
        {
            // Disabling the collider instead of switching the layer because when going back from
            // termination view, the layer will switched back to Default. Since we will not interact with
            // boxes after termination tasks, disabling the collider should not cause any issues.
            wireTask.HostBoxInstance.GetComponent<BoxCollider>().enabled = false;
        }

        private bool TryGetFeedbackData(FeedbackScriptableObject[] feedbacks, MountingTask mountingTask,
            out List<RoughInFeedbackInformation> feedbackList)
        {
            feedbackList = new List<RoughInFeedbackInformation>();

            if (feedbacks == null || feedbacks.Length == 0)
            {
                Debug.LogError($"No feedback attached for {mountingTask.TaskName}");
                return false;
            }

            foreach (var fb in feedbacks)
            {
                Task task = mountingTask.TaskName;
                string violationCode = fb.CodeViolation;
                string description = fb.FeedbackDescription;

                RoughInFeedbackInformation feedback = new()
                {
                    Task = task,
                    CodeViolation = violationCode,
                    FeedbackDescription = description
                };

                feedbackList.Add(feedback);
            }

            return true;
        }

        /// <summary>
        ///     Checks if correct or not and handles feedback according to that
        /// </summary>
        /// <param name="task"></param>
        public void CheckRunSupplyCableTaskForFeedback(CableMountingTask task)
        {
            if (!CorrectlyCompletedSupplyTasks.Contains(task))
            {
                // Get the supply cables 
                TaskFeedbackMap.TryAdd(task.TaskName, new List<RoughInFeedbackInformation>());
                TaskFeedbackMap[task.TaskName].Clear();

                if (task.SelectedMountable != task.CorrectMountable)
                {
                    if (TryGetFeedbackData(task.SelectedMountable.FeedbackScriptableObject, task,
                            out List<RoughInFeedbackInformation> copy))
                    {
                        TaskFeedbackMap[task.TaskName] = copy;
                    }
                }

                TaskFeedbackProcessed?.Invoke(TaskFeedbackMap);
            }
        }

        /// <summary>
        ///     Checks if termination wires correct or not and handles feedback according to that with special case for
        ///     Gang Box task
        /// </summary>
        /// <param name="wireMountingTask"></param>
        public void ProcessTerminationWireForFeedback(WireMountingTask wireMountingTask)
        {
            List<RoughInFeedbackInformation> feedbacks = new();

            if (wireMountingTask is GangBoxWireMountingTask gangBoxWireMountingTask)
            {
                List<InteractableWire> interactableWires = TaskWireMap[gangBoxWireMountingTask.TaskName];
                foreach (GangBoxConductor wire in interactableWires)
                {
                    var conductorSelection =
                        gangBoxWireMountingTask.GangBoxSelectedOptionsMap[wire.ConnectedBox];

                    bool matchingInteractableWireFound = conductorSelection.TerminationOption == wire.Option &&
                                                         conductorSelection.Conductor == wire.Name;
                    if (matchingInteractableWireFound)
                    {
                        HandleWireFeedbackTuple(VerifyAndGatherFeedback(wire, gangBoxWireMountingTask, wire.Name),
                            feedbacks);
                    }
                }

                HandleWireFeedbackList(wireMountingTask, feedbacks);

                return;
            }

            wireMountingTask.SelectedOptionsMap.TryGetValue((MountableName.BondWire),
                out InteractableWire bondWire);
            wireMountingTask.SelectedOptionsMap.TryGetValue((MountableName.NeutralWire),
                out InteractableWire neutralWire);
            wireMountingTask.SelectedOptionsMap.TryGetValue((MountableName.HotWire),
                out InteractableWire hotWire);

            HandleWireFeedbackTuple(VerifyAndGatherFeedback(bondWire, wireMountingTask, MountableName.BondWire),
                feedbacks);
            HandleWireFeedbackTuple(VerifyAndGatherFeedback(neutralWire, wireMountingTask, MountableName.NeutralWire),
                feedbacks);
            HandleWireFeedbackTuple(VerifyAndGatherFeedback(hotWire, wireMountingTask, MountableName.HotWire),
                feedbacks);
            HandleWireFeedbackList(wireMountingTask, feedbacks);
        }

        public void ProcessAllDeviceTasksForFeedback()
        {
            IEnumerable<DeviceMountingTask> deviceMountingTasks =
                AllMountingTasksMap.Values.OfType<DeviceMountingTask>();
            foreach (var task in deviceMountingTasks)
            {
                if(CorrectlyCompletedDeviceTasks.Contains(task)){continue;} 
                if (task.SelectedMountable == null)
                {
                    Debug.LogWarning($"No Device installed for task {task.TaskName} to process feedback");
                    continue;
                }
                ProcessDeviceForFeedback(task);
            }
            
            if (TotalDeviceTasksCorrectlyCompleted == TotalDeviceTasks)
            {
                taskSelectionUI.SetDeviceInstallBtnStatus(WorkflowStatus.Complete);
                taskSelectionUI.SetInstallDevicesBtnState(false);
                AllDeviceTasksCorrectlyComplete = true;
            }
        }

        /// <summary>
        /// Will get feedback from device and process it to the UI
        /// </summary>
        /// <param name="deviceMountingTask"></param>
        private void ProcessDeviceForFeedback(DeviceMountingTask deviceMountingTask)
        {
            if (!deviceMountingTask.IsCorrect())
            {
                TaskFeedbackMap.TryAdd(deviceMountingTask.TaskName, new List<RoughInFeedbackInformation>());
                TaskFeedbackMap[deviceMountingTask.TaskName].Clear();
                
                if (TryGetFeedbackData(deviceMountingTask.SelectedMountable.FeedbackScriptableObject,
                        deviceMountingTask,
                        out List<RoughInFeedbackInformation> copy))
                {
                    TaskFeedbackMap[deviceMountingTask.TaskName] = copy;
                }
                
                WireMountingTask wireMountingTask = deviceMountingTask.GetComponent<WireMountingTask>();
                TryCleanTaskFeedbackMapWithKey(wireMountingTask);
                TaskFeedbackProcessed?.Invoke(TaskFeedbackMap);
                deviceSelectionMenuController.SetTaskOptionEnabledInvert(deviceMountingTask.TaskName,
                    deviceMountingTask.SelectedMountable.Name, false); // Disable the current selected option if incorrect
            }
            else
            {
                TryCleanTaskFeedbackMapWithKey(deviceMountingTask);
                ProcessDeviceWireTaskForFeedback(deviceMountingTask);
                deviceSelectionMenuController.SetTaskOptionEnabledInvert(deviceMountingTask.TaskName, 
                    deviceMountingTask.SelectedMountable.Name, true);
            }
            
            if (deviceMountingTask.IsCorrect() && deviceMountingTask.IsWireTaskCorrect())
            {
                deviceSelectionMenuController.SetTaskOptionsEnabled(deviceMountingTask.TaskName, false); 
                deviceMountingTask.SelectedMountable.gameObject.layer = 
                    LayerMask.NameToLayer(SceneInteractions.LayerIgnoreRaycast);
                TotalDeviceTasksCorrectlyCompleted += 1;
                CorrectlyCompletedDeviceTasks.Add(deviceMountingTask);
                
            }
        }

        /// <summary>
        /// Get feedback from devices wire mounting task and process it to UI
        /// </summary>
        /// <param name="deviceMountingTask"></param>
        private void ProcessDeviceWireTaskForFeedback(DeviceMountingTask deviceMountingTask)
        {
            WireMountingTask wireMountingTask = deviceMountingTask.GetComponent<WireMountingTask>();
            TryCleanTaskFeedbackMapWithKey(wireMountingTask);
            ProcessTerminationWireForFeedback(wireMountingTask);
            
        }

        private void TryCleanTaskFeedbackMapWithKey(MountingTask mountingTask)
        {
            if (TaskFeedbackMap.ContainsKey(mountingTask.TaskName))
            {
                TaskFeedbackMap[mountingTask.TaskName].Clear();
            }
        }

        private Tuple<bool, RoughInFeedbackInformation> VerifyAndGatherFeedback(InteractableWire wire,
            WireMountingTask task, MountableName name)
        {
            bool interactableWireCorrect = false;
            RoughInFeedbackInformation wireFeedback = default;

            if (!VerifyInteractableWireCorrect(wire, task, name))
            {
                TryGetFeedbackData(wire.FeedbackScriptableObject, task, out List<RoughInFeedbackInformation> copy);

                wireFeedback = copy.First();
                MountableName overriddenName = task.GetOverridenName(name);
                if (overriddenName == MountableName.None)
                {
                    overriddenName = name;
                }

                TranslateFeedback(overriddenName, ref wireFeedback);
            }
            else
            {
                if (task.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
                {
                    task.ConductorCorrectlyCompleted.Add(wire);
                }
                wire.gameObject.layer = LayerMask.NameToLayer(SceneInteractions.LayerIgnoreRaycast);
                interactableWireCorrect = true;
            }

            return new Tuple<bool, RoughInFeedbackInformation>(interactableWireCorrect, wireFeedback);
        }

        private Tuple<bool, RoughInFeedbackInformation> VerifyAndGatherFeedback(GangBoxConductor wire,
            GangBoxWireMountingTask task, MountableName name)
        {
            bool interactableWireCorrect = false;
            RoughInFeedbackInformation wireFeedback = default;

            if (!VerifyConductorWireCorrect(wire, task))
            {
                TryGetFeedbackData(wire.FeedbackScriptableObject, task, out List<RoughInFeedbackInformation> copy);

                wireFeedback = copy.First();
                TranslateFeedback(wire, name, ref wireFeedback);
            }
            else
            {
                if (task.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
                {
                    task.ConductorCorrectlyCompleted.Add(wire);
                }
                wire.gameObject.layer = LayerMask.NameToLayer(SceneInteractions.LayerIgnoreRaycast);
                interactableWireCorrect = true;
            }

            return new Tuple<bool, RoughInFeedbackInformation>(interactableWireCorrect, wireFeedback);
        }

        private void TranslateFeedback(MountableName wireType, ref RoughInFeedbackInformation feedback)
        {
            
            // IF override, use that
            string wireTypeFriendlyName = wireType.ToDescription();

            if (!string.IsNullOrEmpty(feedback.CodeViolation))
            {
                feedback.CodeViolation = $"<b>{wireTypeFriendlyName}:</b> {feedback.CodeViolation}";
            }

            if (!string.IsNullOrEmpty(feedback.FeedbackDescription))
            {
                feedback.FeedbackDescription = $"<b>{wireTypeFriendlyName}:</b> {feedback.FeedbackDescription}";
            }
        }

        private void TranslateFeedback(GangBoxConductor wire, MountableName wireType,
            ref RoughInFeedbackInformation feedback)
        {
            string wireTypeFriendlyName = wireType.ToDescription();
            string connectedBox = GetConnectedBoxFromConductor(wire);

            if (!string.IsNullOrEmpty(feedback.CodeViolation))
            {
                feedback.CodeViolation = $"<b>{wireTypeFriendlyName} from {connectedBox}:</b> {feedback.CodeViolation}";
            }

            if (!string.IsNullOrEmpty(feedback.FeedbackDescription))
            {
                feedback.FeedbackDescription =
                    $"<b>{wireTypeFriendlyName} from {connectedBox}:</b> {feedback.FeedbackDescription}";
            }
        }

        private string GetConnectedBoxFromConductor(GangBoxConductor wire)
        {
            string connectedBox = "";

            switch (wire.ConnectedBox)
            {
                // Outlet
                case MountableName.DeviceBox:
                    connectedBox = "Outlet";
                    break;
                // Light
                case MountableName.OctagonalBracketBox:
                    connectedBox = "Light";
                    break;
                // Fan
                case MountableName.FanBox:
                    connectedBox = "Fan";
                    break;
                // Gang
                case MountableName.Panel:
                    connectedBox = "Panel";
                    break;
            }

            return connectedBox;
        }

        private void HandleWireFeedbackTuple(Tuple<bool, RoughInFeedbackInformation> feedbackTuple,
            List<RoughInFeedbackInformation> feedbacks)
        {
            if (!feedbackTuple.Item1) // Wire selection is not correct
            {
                feedbacks.Add(feedbackTuple.Item2);
            }
        }

        private void HandleWireFeedbackList(WireMountingTask task, List<RoughInFeedbackInformation> feedbackList)
        {
            if (feedbackList == null)
            {
                return;
            }

            TaskFeedbackMap.TryAdd(task.TaskName, new List<RoughInFeedbackInformation>());
            TaskFeedbackMap[task.TaskName].Clear();

            if (feedbackList.Count != 0)
            {
                TaskFeedbackMap[task.TaskName] = feedbackList;
            }

            TaskFeedbackProcessed?.Invoke(TaskFeedbackMap);
        }

        private bool VerifyInteractableWireCorrect(InteractableWire interactableWire, WireMountingTask task,
            MountableName wireTypeToCheck)
        {
            if (interactableWire.Option == task.CorrectOptionsMap[wireTypeToCheck])
            {
                return true;
            }

            return false;
        }

        private bool VerifyConductorWireCorrect(GangBoxConductor interactableWire, GangBoxWireMountingTask task)
        {
            bool isConductorCorrect = interactableWire.Option ==
                                      task.GangBoxCorrectOptionsMap[interactableWire.ConnectedBox].TerminationOption &&
                                      interactableWire.Name ==
                                      task.GangBoxCorrectOptionsMap[interactableWire.ConnectedBox].Conductor;
            if (isConductorCorrect)
            {
                return true;
            }

            return false;
        }

        private void FetchInteractableWires()
        {
            InteractableWire[] interactableWires =
                FindObjectsByType<InteractableWire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (InteractableWire wire in interactableWires)
            {
                if (!TaskWireMap.TryGetValue(wire.TerminationTask, out List<InteractableWire> wireList))
                {
                    TaskWireMap[wire.TerminationTask] = new List<InteractableWire> { wire };
                }
                else
                {
                    wireList.Add(wire);
                }
            }
        }
    }
}