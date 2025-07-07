using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class TaskHandler : MonoBehaviour
    {
        // Even though MountingTask also has a reference to the mounted Mountable, this
        // dictionary is defined for faster access to mounting tasks via Mountable references
        public Dictionary<Mountable, MountingTask> MountedObjectMap { get; } = new();

        public bool IsGCFeedbackInvoked { get; set; }
        [JsonProperty] private bool boxMountingTasksDone;
        [JsonProperty] private bool roughInTasksDone;
        [JsonProperty] private bool finalTasksDone;

        [Header("Systems")]
        [SerializeField] private FeedbackHandler feedbackHandler;
        [SerializeField] private CloudSaveAdapter cloudSaveAdapter;

        [Header("Menu Controllers")]
        public TaskSelectionMenuController TaskSelectionMenu;
        public BoxMountingSelectionMenuController BoxMountingSelectionMenu;
        public RunSupplyCableMenuController RunSupplyCableMenu;
        public SupplyCableSelectionMenuController SupplyCableSelectionMenu;
        public DeviceSelectionMenuController DeviceSelectionMenu;
        public CircuitTestingMenuController CircuitTestingMenu;

        [Header("Mounters")]
        [SerializeField] private BoxMounter boxMounter;
        [SerializeField] private CableMounter cableMounter;
        [SerializeField] private WireMounter wireMounter;
        [SerializeField] private DeviceMounter deviceMounter;


        [Header("Feedback Events")]
        public UnityEvent<Task, bool, bool> DisplaySelectionFeedback = new();

        public UnityEvent<ConfirmSelectionInfo> ConfirmInfoChanged = new();

        public UnityEvent<bool> DisableContractorFeedback = new();

        public UnityEvent BlinkTaskListIcon = new();

        [Header("Box Mounting Task Events")]

        public UnityEvent<Task> ShowBoxSelectionMenu = new();

        public UnityEvent BoxMountingTasksCompleted = new();

        public UnityEvent BoxMountingTasksCorrect = new();
        public UnityEvent LoadBoxMountingFeedbackInvoked = new();


        [Header("Supply Cable Task Events")]

        public UnityEvent<Task> ShowSupplyCableSelectionMenu = new();

        public UnityEvent<int> MountedCableCountUpdated = new();

        [Tooltip("Event invoked when the learner has provided answers for all 'Supply Cables to Boxes' tasks")]
        public UnityEvent SupplyCableMountingTasksCompleted = new();

        [Tooltip("Event invoked when the learner has provided answers for all 'Terminate Cables in Boxes' tasks")]
        public UnityEvent TerminateCableTasksCompleted = new();

        [Tooltip("Event invoked when the learner has provided correct answers for 'Supply Cables to Boxes' and 'Terminate Cables in Boxes' tasks")]
        public UnityEvent SupplyCablesTaskCorrect = new();


        [Header("Device Installation Task Events")]

        public UnityEvent<Task> ShowDeviceSelectionMenu = new();

        // Unused events until "Final" steps are implemented
        public UnityEvent DeviceInstallTasksCompleted = new();

        public UnityEvent DeviceInstallTasksCorrect = new();

        [Header("Scene Transition Events for Loading")]
        public UnityEvent LoadedPastRoughInTasks = new();


        // Private fields
        private readonly Dictionary<Task, MountingTask> AllMountingTasksMap = new();
        private readonly HashSet<MountingTask> CompletedMountingTasks = new();

        private MountingTask currentMountingTask;
        private Mounter currentMounter;
        private bool boxMountingTaskCorrectlyCompleted = false;
        private bool deviceTaskStart = false;
        // This flag will set when moving/replacing a mountable by clicking on it in the scene
        private bool isEditingViaScene;

        // This flag will be set when a mountable is being mounted for the first time
        private bool isMountingFirstTime;
        [JsonProperty] private bool isRunSupplyCableStarted = false;
        [JsonProperty] private bool isTerminationStarted = false;
        [JsonProperty] private bool isDeviceInstallStarted = false;
        [JsonProperty] private bool isCircuitQuizStarted = false;
        [JsonProperty] private bool isFinalTransitionStarted = false;
        private bool isSetupCableTaskComplete = false;

        public int GetCompletedTasksNum()
        {
            return CompletedMountingTasks.Count;
        }

        public void LoadBoxMountingTasks()
        {
            foreach (BoxMountingTask boxMountingTask in AllMountingTasksMap.Values.OfType<BoxMountingTask>())
            {
                boxMounter.OnLoadComplete(boxMountingTask);
            }

            TaskSelectionMenu.SetMountingTaskBtnStatus(WorkflowStatus.InProgress);
        }

        private void Awake()
        {
            MountingTask[] mountingTasks =
                FindObjectsByType<MountingTask>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (MountingTask mountingTask in mountingTasks)
            {
                AllMountingTasksMap.Add(mountingTask.TaskName, mountingTask);
            }

            // Sets up event listeners to animate the task list icon when the previous task is fully correct
            BoxMountingTasksCorrect?.AddListener(() => BroadcastTaskStartState(ref isRunSupplyCableStarted));
            SupplyCablesTaskCorrect?.AddListener(() => BroadcastTaskStartState(ref isDeviceInstallStarted));
            DeviceInstallTasksCorrect?.AddListener(() => BroadcastTaskStartState(ref isFinalTransitionStarted));
        }

        /// <summary>
        /// Invoked when the learner selects a tab of a Task List menu
        /// <see cref="BoxMountingSelectionMenuController.TaskSelected"/>
        /// <see cref="SupplyCableSelectionMenuController.TaskSelected"/>
        /// </summary>
        /// <param name="task">Selected task</param>
        public void OnMountingTaskClick(Task task)
        {
            if (!AllMountingTasksMap.TryGetValue(task, out MountingTask mountingTask))
            {
                Debug.LogError("Selected an invalid task!");
                return;
            }

            if (mountingTask is BoxMountingTask boxMountingTask)
            {
                currentMountingTask = boxMountingTask;
                currentMounter = boxMounter;
            }
            else if (mountingTask is CableMountingTask cableMountingTask)
            {
                currentMountingTask = cableMountingTask;
                currentMounter = cableMounter;
            }
            else if (mountingTask is WireMountingTask wireMountingTask)
            {
                currentMountingTask = wireMountingTask;
                currentMounter = wireMounter;
                wireMounter.PrepareToPreview(wireMountingTask);
            }
            else if (mountingTask is DeviceMountingTask deviceMountingTask)
            {
                currentMountingTask = deviceMountingTask;
                currentMounter = deviceMounter;
            }
        }

        /// <summary>
        /// Invoked when a learner selects a mountable from a Task List menu
        /// Invoked by <see cref="BoxMountingSelectionMenuController.ItemSelected"/>
        /// Invoked by <see cref="SupplyCableSelectionMenuController.RouteSelected"/>
        /// </summary>
        /// <param name="selectedMountable">Selected mountable type</param>
        public void OnMountableSelected(MountableName selectedMountable)
        {
            currentMountingTask.RequestedMountableName = selectedMountable;

            currentMounter.IsEditingViaScene = isEditingViaScene;
            isMountingFirstTime = !CompletedMountingTasks.Contains(currentMountingTask);
            if (currentMountingTask is BoxMountingTask boxMountingTask)
            {
                if (isMountingFirstTime)
                {
                    boxMounter.PreviewMountLocations(boxMountingTask);
                }
                else
                {
                    if (isEditingViaScene || boxMountingTask.Action == MountableAction.Replace)
                    {
                        ReplaceMountable(boxMounter, boxMountingTask);
                        // Immediately replace the box without showing location options if it is replaced
                        // via the edit dialog (by clicking on the box in the scene)
                        MountingTaskCompleted(boxMountingTask);
                    }
                    else if (boxMountingTask.Action == MountableAction.Move)
                    {
                        MoveMountable(boxMounter, boxMountingTask);
                    }
                    else if (boxMountingTask.Action == (MountableAction.Replace | MountableAction.Move))
                    {
                        MoveMountable(boxMounter, boxMountingTask);
                    }
                }
            }
            else if (currentMountingTask is CableMountingTask cableMountingTask)
            {
                cableMountingTask.RequestedMountableName = selectedMountable;
                MountPreview(cableMounter, cableMountingTask);
            }
            else if (currentMountingTask is WireMountingTask wireMountingTask)
            {
                wireMounter.ShowTerminationOptions(wireMountingTask);
            }
            else if (currentMountingTask is DeviceMountingTask deviceMountingTask)
            {
                deviceMountingTask.RequestedDevice = selectedMountable;
                MountPreview(deviceMounter, deviceMountingTask);
            }
        }

        /// <summary>
        /// Invoked when the learner close a Task List menu without selecting a mountable type
        /// <see cref="BoxMountingSelectionMenuController.Cancelled"/>
        /// <see cref="SupplyCableSelectionMenuController.CloseButtonPressed"/>
        /// </summary>
        public void OnIncompleteEquipmentSelection()
        {
            isEditingViaScene = false;
        }

        /// <summary>
        /// Invoked when the learner clicks on the "Confirm" button of the confirmation dialog
        /// <see cref="ConfirmSelectionMenuController.InstallConfirmButtonPressed"/>
        /// <see cref="ConfirmSelectionMenuController.PreviewConfirmButtonPressed"/>
        /// </summary>
        public void OnMountConfirm()
        {
            currentMounter.OnMountConfirm();
        }

        /// <summary>
        /// Invoked when the learner clicks on the "Redo" button of the confirmation dialog
        /// <see cref="ConfirmSelectionMenuController.InstallRedoButtonPressed"/>
        /// <see cref="ConfirmSelectionMenuController.PreviewRedoButtonPressed"/>
        /// </summary>
        public void OnMountRedo()
        {
            currentMounter.OnMountRedo();
        }

        /// <summary>
        /// Invoked when the mounting task is completed. Some tasks can have continuous
        /// subtasks, therefore this method gets invoked when all the subtasks are done.
        /// </summary>
        /// <param name="mountingTask">Relevant MountingTask</param>
        public void OnMountingTaskComplete(MountingTask mountingTask)
        {
            MountingTaskCompleted(mountingTask);
            cloudSaveAdapter.Save(); // Save to cloud
        }

        /// <summary>
        /// Move a box by clicking on it via <see cref="EditMountedObject"/>
        /// </summary>
        /// <param name="task">Relevant MountingTask</param>
        public void OnMountableMove(BoxMountingTask task)
        {
            isEditingViaScene = true;
            currentMountingTask = task;
            currentMounter = boxMounter;

            // Given that the learner is able to move a Mounted Box through the Scene,
            // we need to keep the information in the ConfirmSelectionMenu synced
            ConfirmSelectionInfo cfi = BoxMountingSelectionMenu.GenerateSelectionInfo(task.SelectedMountable.Name, task.TaskName);
            ConfirmInfoChanged?.Invoke(cfi);
            MoveMountable(boxMounter, task);
        }

        /// <summary>
        /// Replace a mountable by clicking on it via <see cref="EditMountedObject"/>
        /// </summary>
        /// <param name="mountingTask">Relevant MountingTask</param>
        public void OnMountableReplace(MountingTask mountingTask)
        {
            isEditingViaScene = true;
            currentMountingTask = mountingTask;

            if (mountingTask is BoxMountingTask)
            {
                ShowBoxSelectionMenu?.Invoke(mountingTask.TaskName);
            }
            else if (mountingTask is CableMountingTask)
            {
                ShowSupplyCableSelectionMenu?.Invoke(mountingTask.TaskName);
            }
            else if (mountingTask is WireMountingTask wireMountingTask)
            {
                wireMounter.ShowTerminationOptions(wireMountingTask);
            }
            else if (mountingTask is DeviceMountingTask deviceMountingTask)
            {
                ShowDeviceSelectionMenu?.Invoke(deviceMountingTask.TaskName);
            }
        }

        /// <summary>
        /// Invoked after successfully loading cloud save data (after filling JsonProperties)
        /// </summary>
        public void OnCloudLoadComplete()
        {
            cloudSaveAdapter.InternalsSyncing = true; // Make sure to keep this at the beginning of this method
            SyncInternalSystemsWithCloud();
            cloudSaveAdapter.InternalsSyncing = false; // Make sure to keep this at the end of this method
        }

        /// <summary>
        /// Sync internal systems with successfully loaded cloud save data. Note that the sequence
        /// here is very important. Tasks may not load properly if the sequence is updated incorrectly.
        /// </summary>
        private void SyncInternalSystemsWithCloud()
        {
            LoadBoxMountingTasks();
            if (!boxMountingTasksDone) { return; }

            LoadBoxMountingFeedbackInvoked?.Invoke();
            DisableContractorFeedback?.Invoke(false);

            RunSupplyCableMenu.OnLoadComplete(); // Sync completion of the first two quizzes
            if (!RunSupplyCableMenu.IsCableQuizCompleted) { return; }
            TaskSelectionMenu.SetSupplyTaskBtnStatus(WorkflowStatus.InProgress);

            LoadAllCableMountingTasksStates();
            if (!RunSupplyCableMenu.IsTerminateQuizCompleted) { return; }

            LoadWireTerminationTasks();
            if (!roughInTasksDone) { return; }

            SupervisorFeedbackHandler(); // Handle feedback for rough-in stage

            LoadedPastRoughInTasks?.Invoke();
            DisableContractorFeedback?.Invoke(false);

            LoadDeviceInstallationTasks();
            CircuitTestingMenu.OnLoadComplete();
            if (!finalTasksDone) { return; }

            SupervisorFeedbackHandler(); // Handle feedback for final stage
            DisableContractorFeedback?.Invoke(false);
        }

        private void MountPreview(Mounter mounter, MountingTask mountingTask)
        {
            mounter.OnMountPreview(mountingTask);
        }

        private void ReplaceMountable(IReplaceable replaceable, MountingTask mountingTask)
        {
            replaceable.OnMountableReplace(mountingTask);
        }

        private void MoveMountable(IMovable movable, MountingTask mountingTask)
        {
            movable.OnMountableMove(mountingTask);
        }

        private void PlaceMountableAutomatic(MountingTask mountingTask)
        {
            currentMounter.OnLoadComplete(mountingTask);
            MountingTaskCompleted(mountingTask);
        }

        // Both conditional blocks in this function are performing almost identical options, can
        // likely genericize by providing a type and a list of callbacks
        private void MountingTaskCompleted(MountingTask task)
        {
            CompletedMountingTasks.Add(task);

            if (task is BoxMountingTask)
            {
                Task newDefaultUITaskTab = DetermineWhichTask(task,
                    CompletedMountingTasks,
                    BoxMountingSelectionMenu.TaskTabs);
                BoxMountingSelectionMenu.SetDefaultTab(newDefaultUITaskTab);

                int completedTaskCount = CompletedMountingTasks.OfType<BoxMountingTask>().Count();
                TaskSelectionMenu.SetMountingCompleteTasksNum(completedTaskCount);

                if (HasAllBoxMountingTasksCompleted())
                {
                    BoxMountingTasksCompleted?.Invoke();
                }
            }
            else if (task is CableMountingTask cableMountingTask)
            {
                Task newDefaultUITaskTab = DetermineWhichTask(task,
                    CompletedMountingTasks,
                    SupplyCableSelectionMenu.TaskTabs);
                SupplyCableSelectionMenu.SetDefaultTab(newDefaultUITaskTab);

                int completedWireMountingTasks = CompletedMountingTasks.OfType<WireMountingTask>().Count();
                int completedCableMountingTasks = CompletedMountingTasks.OfType<CableMountingTask>().Count();
                TaskSelectionMenu.SetSupplyCableCompletedTasksNum(completedCableMountingTasks +
                                                                  completedWireMountingTasks);
                RunSupplyCableMenu.UpdateSupplyToBoxesCompleteTasksNum(completedCableMountingTasks);

                // Given that we are able to edit supply cable tasks when terminating wires,
                // we only need to update the status of the next workflow when we are mounting the last
                // supply cable task for the first time
                // UPDATE:
                // This event will now invoke each time a supply cable mounting task is completed once 
                // they have all bee completed initially
                if (HasAllSupplyCableMountingTasksCompleted())// && isMountingFirstTime)
                {
                    // Indicates the status of the next workflow
                    SupplyCableMountingTasksCompleted?.Invoke();
                    if (!isTerminationStarted)
                    {
                        BlinkTaskListIcon.Invoke();
                        isTerminationStarted = true;
                    }
                }

            }
            else if (task is WireMountingTask wireMountingTask)
            {
                int completedWireMountingTasks = CompletedMountingTasks.OfType<WireMountingTask>().Count();
                int completedCableMountingTasks = CompletedMountingTasks.OfType<CableMountingTask>().Count();
                TaskSelectionMenu.SetSupplyCableCompletedTasksNum(completedCableMountingTasks +
                                                                  completedWireMountingTasks);
                RunSupplyCableMenu.UpdateTerminateCompleteTasksNum(completedWireMountingTasks);

                // Call event to invoke the feedback
                if (HasAllWireMountingTasksCompleted())
                {
                    TerminateCableTasksCompleted?.Invoke();
                }
            }
            else if (task is DeviceMountingTask deviceMountingTask)
            {
                Task defaultTabTask = DetermineWhichTask(deviceMountingTask,
                    CompletedMountingTasks,
                    DeviceSelectionMenu.TaskTabs);
                DeviceSelectionMenu.SetDefaultTab(defaultTabTask);

                TaskSelectionMenu.SetDevicesCompletedTasksNum(CompletedMountingTasks.OfType<DeviceMountingTask>().Count());

                if (HasAllDeviceMountingTasksComplete())
                {
                    DeviceInstallTasksCompleted?.Invoke();
                    if (!isCircuitQuizStarted)
                    {
                        BlinkTaskListIcon.Invoke();
                        isCircuitQuizStarted = true;
                    }
                }
            }

            // SelectedMountable is not useful for WireMountingTasks and will be null when loading
            if (task.SelectedMountable)
            {
                MountedObjectMap[task.SelectedMountable] = task;
            }
            isEditingViaScene = false;
            currentMountingTask = null;
            currentMounter = null;
        }

        /// <summary>
        /// Checks if all box mounting tasks have been completed
        /// </summary>
        /// <returns>True if the count of completed tasks equals the total number of tasks, false otherwise.</returns>
        private bool HasAllBoxMountingTasksCompleted()
        {
            int completionCount = CompletedMountingTasks.OfType<BoxMountingTask>().Count();
            return completionCount == feedbackHandler.TotalBoxMountingTasks;
        }

        /// <summary>
        /// Checks if all cable mounting tasks have been completed
        /// </summary>
        /// <returns>True if the count of completed tasks equals the total number of tasks, false otherwise.</returns>
        private bool HasAllSupplyCableMountingTasksCompleted()
        {
            int completionCount = CompletedMountingTasks.OfType<CableMountingTask>().Count();
            return completionCount == feedbackHandler.TotalSupplyCableTasks;
        }

        /// <summary>
        /// Checks if all wire mounting tasks have been completed
        /// </summary>
        /// <returns>True if the count of completed tasks equals the total number of tasks, false otherwise.</returns>
        private bool HasAllWireMountingTasksCompleted()
        {
            int completionCount = 0;
            foreach (MountingTask mountingTask in CompletedMountingTasks)
            {
                // As device connection tasks reuse WireMountingTasks, we filter termination tasks by
                // their TaskName enum values
                if (mountingTask is WireMountingTask wireMountingTask &&
                    wireMountingTask.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
                {
                    completionCount++;
                }
            }
            return completionCount == feedbackHandler.TotalTerminationTasks;
        }

        private bool HasAllDeviceMountingTasksComplete()
        {
            int completionCount = CompletedMountingTasks.OfType<DeviceMountingTask>().Count();
            return completionCount == feedbackHandler.TotalDeviceTasks;
        }

        // TODO: Move this up since this is a public method
        // This function seems to only refer to Box Mounting tasks.
        // Needs to either specifically reference BoxMounting or be made generic for all task feedback
        public void CompleteAndDisplayFeedback()
        {
            int tasksCorrect = 0;
            foreach (var kvp in AllMountingTasksMap)
            {
                if (kvp.Value is not BoxMountingTask mountingTask) { continue; }

                bool isCorrectLocation = mountingTask.SelectedLocation == mountingTask.CorrectLocation;
                bool isCorrectMountable = mountingTask.SelectedMountable.Name == mountingTask.CorrectMountable;
                DisplaySelectionFeedback?.Invoke(kvp.Key, isCorrectMountable, isCorrectLocation);
                if (isCorrectLocation && isCorrectMountable)
                {
                    tasksCorrect += 1;
                }
            }

            if (tasksCorrect == feedbackHandler.TotalBoxMountingTasks && !isSetupCableTaskComplete)
            {
                boxMountingTaskCorrectlyCompleted = true;
                boxMountingTasksDone = true;
                cloudSaveAdapter.Save();
                BoxMountingTasksCorrect?.Invoke();
                SetupCableTask();
                isSetupCableTaskComplete = true;
            }
            else
            {
                int completionCount = CompletedMountingTasks.OfType<BoxMountingTask>().Count();
                TaskSelectionMenu.SetMountingCompleteTasksNum(completionCount);
            }
        }

        /// <summary>
        ///     Invokes the <see cref="BlinkTaskListIcon"/> event if the provided
        ///     <paramref name="taskFlag"/> is false, then sets the value to true by reference
        /// </summary>
        /// <param name="taskFlag"></param>
        private void BroadcastTaskStartState(ref bool taskFlag)
        {
            if (!taskFlag)
            {
                BlinkTaskListIcon?.Invoke();
                taskFlag = true;
            }
        }

        private void SetupCableTask()
        {
            TaskSelectionMenu.SetSelectAndMountBtnState(false);
            TaskSelectionMenu.SetMountingCompleteTasksNum(feedbackHandler.TotalBoxMountingTasks);
            TaskSelectionMenu.SetMountingTaskBtnStatus(WorkflowStatus.Complete);
            TaskSelectionMenu.SetSupplyTaskBtnStatus(WorkflowStatus.Available);
            TaskSelectionMenu.SetRunSupplyCableBtnState(true);
        }

        private Task DetermineWhichTask(MountingTask taskToDefault, HashSet<MountingTask> completedTasks,
            List<Task> mountableUITabOrderLeftToRightList)
        {
            Task defaultScreen = Task.None;

            if (completedTasks.Contains(taskToDefault))
            {
                foreach (Task task in mountableUITabOrderLeftToRightList)
                {
                    if (!completedTasks.Contains(AllMountingTasksMap[task]) &&
                        task != Task.None &&
                        task != taskToDefault.TaskName)
                    {
                        defaultScreen = task;
                        break;
                    }
                }
            }

            return defaultScreen;
        }

        // TODO: Move this up since this is a public method
        /// <summary>
        /// Method That handles GC Invokes and updates the status of the mounting task.
        /// Invoked by <see cref="ReminderDialog.ConfirmButtonPressed"/>
        /// </summary>
        public void SupervisorFeedbackHandler()
        {
            if (!boxMountingTaskCorrectlyCompleted)
            {
                feedbackHandler.ProcessMountBoxesTasks();
            }

            if (HasAllSupplyCableMountingTasksCompleted() && !feedbackHandler.AllSupplyCablesTasksCorrectlyCompleted)
            {
                feedbackHandler.ProcessRunSupplyCableTasks();

            }
            if (HasAllWireMountingTasksCompleted() && !feedbackHandler.AllTerminationTasksCorrectlyComplete)
            {
                feedbackHandler.ProcessTerminationTasks();
                roughInTasksDone = feedbackHandler.AllTerminationTasksCorrectlyComplete &&
                                   feedbackHandler.AllSupplyCablesTasksCorrectlyCompleted;
                cloudSaveAdapter.Save();
            }

            if (HasAllDeviceMountingTasksComplete() && feedbackHandler.AllSupplyCablesTasksCorrectlyCompleted
                                                    && feedbackHandler.AllTerminationTasksCorrectlyComplete)
            {
                feedbackHandler.ProcessAllDeviceTasksForFeedback();
            }

            if (!deviceTaskStart && feedbackHandler.AllSupplyCablesTasksCorrectlyCompleted
                                 && feedbackHandler.AllTerminationTasksCorrectlyComplete)
            {
                SetUpInstallDeviceTask();
            }

            if (feedbackHandler.AllDeviceTasksCorrectlyComplete)
            {
                DeviceInstallTasksCorrect?.Invoke();
                CompleteDeviceInstall();

                finalTasksDone = true;
                cloudSaveAdapter.Save();
            }
        }

        private void SetUpInstallDeviceTask()
        {
            TaskSelectionMenu.SetSupplyTaskBtnStatus(WorkflowStatus.Complete);
            TaskSelectionMenu.SetRunSupplyCableBtnState(false);
            TaskSelectionMenu.SetInstallDevicesBtnState(true);
            TaskSelectionMenu.SetDeviceInstallBtnStatus(WorkflowStatus.Available);
            deviceTaskStart = true;
            SupplyCablesTaskCorrect?.Invoke();
        }

        private void CompleteDeviceInstall()
        {
            TaskSelectionMenu.SetInstallDevicesBtnState(false);
            TaskSelectionMenu.SetDeviceInstallBtnStatus(WorkflowStatus.Complete);
        }

        /// <summary>
        /// Method that skips box mounting task by simulating the workflow of mounting the correct boxes and correct
        /// locations for completing the task.
        /// NOTE: This method will only be used for testing purposes. Dev tool console uses <see cref="TaskSkipper"/>
        /// to skip tasks, including boxes.
        /// </summary>
        public void SkipBoxMountingTask()
        {
            if (boxMountingTaskCorrectlyCompleted)
            {
                Debug.Log("Already finished task");
                return;
            }
            // Loop through Total Mounting Tasks Number
            foreach (BoxMountingTask boxMountingTask in AllMountingTasksMap.Values.OfType<BoxMountingTask>())
            {
                // Setup the Mounting Task
                currentMounter = boxMounter;
                currentMountingTask = boxMountingTask;
                boxMountingTask.SelectedLocation = boxMountingTask.CorrectLocation;
                boxMountingTask.RequestedMountableName = boxMountingTask.CorrectMountable;

                ((BoxMounter)currentMounter).AutomaticMountablePlace(boxMountingTask);
                MountingTaskCompleted(boxMountingTask);
            }
            SupervisorFeedbackHandler();
            CompleteAndDisplayFeedback();
            DisableContractorFeedback?.Invoke(false);
        }

        /// <summary>
        /// Set up all cable mounting tasks from loaded state of cable mounting tasks which would be done via cloud save
        /// </summary>
        /// <param name="mountingTask"></param>
        private void LoadAllCableMountingTasksStates()
        {
            RunSupplyCableMenu.SetConnectCablesBtnStatus(WorkflowStatus.InProgress);

            foreach (var task in AllMountingTasksMap.Values.OfType<CableMountingTask>().ToList())
            {
                if (task.SelectedMountableName != MountableName.None)
                {
                    SkipCableMountingSubTask(task);
                }
            }
        }

        /// <summary>
        /// Skips cable mounting task using requested mountable set in mounting task
        /// </summary>
        /// <param name="mountingTask"></param>
        public void SkipCableMountingSubTask(MountingTask mountingTask)
        {
            // TODO Refactor this part when we are all combining our parts and making sure sequential
            // Validate if Box Mounting Task correctly Completed
            if (!boxMountingTaskCorrectlyCompleted)
            {
                Debug.Log("Box Mounting Task not complete, cannot skip supply cable");
                return;
            }

            if (feedbackHandler.AllSupplyCablesTasksCorrectlyCompleted)
            {
                Debug.Log("Cable supply tasks already completed");
                return;
            }

            currentMounter = cableMounter;
            PlaceMountableAutomatic(mountingTask);
        }

        private void LoadWireTerminationTasks()
        {
            RunSupplyCableMenu.SetTerminateCablesBtnStatus(WorkflowStatus.InProgress);

            foreach (var wireMountingTask in AllMountingTasksMap.Values.OfType<WireMountingTask>())
            {
                if (wireMountingTask.TaskName > Task.TerminateGangBoxHots) { continue; }
                wireMounter.OnLoadComplete(wireMountingTask);
            }
        }

        private void LoadDeviceInstallationTasks()
        {
            foreach (var task in AllMountingTasksMap.Values.OfType<DeviceMountingTask>())
            {
                if (task.SelectedMountableName == MountableName.None) { continue; }
                TaskSelectionMenu.SetDeviceInstallBtnStatus(WorkflowStatus.InProgress);
                currentMounter = deviceMounter;
                task.RequestedDevice = task.SelectedMountableName;
                deviceMounter.OnLoadComplete(task);

                task.GetComponent<WireMountingTask>();
                wireMounter.OnLoadComplete(task.GetComponent<WireMountingTask>());
                MountingTaskCompleted(task);
            }
        }

        public void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }    

      
    }
}