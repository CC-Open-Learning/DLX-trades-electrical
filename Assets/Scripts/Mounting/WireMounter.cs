using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static VARLab.TradesElectrical.ObjectInteractionWidget;

namespace VARLab.TradesElectrical
{
    public class WireMounter : Mounter
    {
        [field: SerializeField]
        public UnityEvent<bool> SetEnableOtherInteractables { get; private set; }= new();
        public UnityEvent<Task> TaskUpdated = new();
        public UnityEvent<MountableName> MountableSelected = new();
        public UnityEvent<WireMountingTask> Previewing = new();
        public UnityEvent<ConfirmSelectionInfo> ShowConfirmationDialog = new();
        public UnityEvent<WireMountingTask> MountingFinalized = new();
        public UnityEvent ResetToolbar = new();
        [NonSerialized] public readonly UnityEvent DeviceWireConnectionFinalized = new();
        public UnityEvent<ToastMessageType, string, float> ShowFeedback = new();
        public UnityEvent ShowInteractionWidget = new();
        public UnityEvent HideInteractionWidget = new();
        public UnityEvent<ButtonType, Action> AddButton = new();
        public UnityEvent<MountingTask> ResetCamera = new();
        public UnityEvent HideToast = new();
        public UnityEvent EnableTaskListButton = new();
        public UnityEvent<MountingTask> ReplacingWire = new();
        public UnityEvent UpdatingGangBoxConductors = new();
        public UnityEvent<WireMountingTask> UpdateTerminationMenu = new();
        public UnityEvent<Task> UpdateGangBoxTerminationMenu = new();


        private static readonly MountableName[] TerminationSequence = new[]
        {
            MountableName.BondWire,
            MountableName.NeutralWire,
            MountableName.HotWire
        };

        private const string WrongSequenceTermination = "Try again. Conductors must be terminated in correct order.";
        private const string WrongSequenceConnection = "Try again. Conductors must be connected in correct order.";
        private const string TerminationInstruction = "Select a conductor to terminate";
        private const string ConnectionInstruction = "Select a conductor to connect";

        private const float ToastTimeout = 3f; // Timeout in seconds

        private readonly Dictionary<Task, List<WireTerminationOption>> taskOptionMap = new();
        private readonly Dictionary<Task, List<InteractableWire>> taskWireMap = new();
        private readonly Dictionary<Task, List<WireNut>> taskWireNutMap = new();
        private readonly Dictionary<Task, WireMountingTask> allWireMountingTaskMap = new();

        // HostBoxName, WireTask list
        private readonly Dictionary<MountableName, List<WireMountingTask>> hostBoxNameMap = new();

        private WireMountingTask[] wireMountingTasks;
        private WireMountingTask previewingMountingTask;
        private int currentTerminationIndex;
        private bool wireReplacingMode;

        private const MountableName FirstSupplyCableRoute = MountableName.FanToGangA;
        private const Task LastCableTerminationTask = Task.TerminateGangBoxHots;

        /// <summary>
        ///     Indicates whether the mounter is reactive to host box click events.
        ///     This flag is set to true when a separate system initializes the Wire Mounter,
        ///     allowing the learner to click on boxes to terminate conductors. 
        /// </summary>
        private bool isReactiveToHostBoxClicks;
        
        private InteractableWire currentWire;
        private WireTerminationOption currentOption;
        
        private void Start()
        {
            wireMountingTasks =
                FindObjectsByType<WireMountingTask>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            foreach (WireMountingTask wireMountingTask in wireMountingTasks)
            {
                allWireMountingTaskMap.Add(wireMountingTask.TaskName, wireMountingTask);

                if (hostBoxNameMap.TryGetValue(wireMountingTask.HostBoxName, out var wireMountingTaskList))
                {
                    wireMountingTaskList.Add(wireMountingTask);
                }
                else
                {
                    hostBoxNameMap[wireMountingTask.HostBoxName] = new List<WireMountingTask> { wireMountingTask };
                }
            }
        }

        public void Init()
        {
            FindHostBoxes();

            FetchComponents(taskOptionMap);
            FetchComponents(taskWireMap);
            FetchComponents(taskWireNutMap);
            isReactiveToHostBoxClicks = true;
        }

        /// <summary>
        /// Invokes after the learner completes the Terminate Cables in Boxes quiz
        /// </summary>
        public void OnTerminationQuizCompleted()
        {
            foreach (WireMountingTask mountingTask in wireMountingTasks)
            {
                // Only include wires in Boxes (ignore Devices)
                if (mountingTask.TaskName > LastCableTerminationTask) { continue; }
                SetActiveInitialWires(mountingTask, true, false);
                SetWiresInteractable(mountingTask, false);
            }
        }

        /// <summary>
        /// Initial mounting interaction. This can be considered as the entry point.
        /// This will show interactable wires to terminate.
        /// </summary>
        /// <param name="mountingTask">Relevant wire mounting task</param>
        public void PrepareToPreview(WireMountingTask mountingTask)
        {
            DisableOldWiresWhenConnectingDevices(mountingTask);

            if (!previewingMountingTask && mountingTask.IsComplete())
            {
                SetupInteractionWidget();
                ShowInteractionWidget?.Invoke();
            }

            previewingMountingTask = mountingTask;
            Previewing?.Invoke(mountingTask);

            if (mountingTask.IsComplete())
            {
                wireReplacingMode = true;
                SetActiveWires(mountingTask, true);
            }
            else
            {
                // Show initial wires
                SetActiveInitialWires(mountingTask, true);
            }

            SetWiresInteractable(mountingTask, true);
            SetEnableOtherInteractables.Invoke(false);
            ShowInstructionToast(mountingTask);
        }

        /// <summary>
        /// Invokes when clicking on an interactable wire
        /// </summary>
        /// <param name="mountingTask">Relevant wire mounting task</param>
        /// <param name="replacingWire">Wires will be replaced when clicking on termination options</param>
        public void ShowTerminationOptions(WireMountingTask mountingTask)
        {
            HideToast?.Invoke();
            if (wireReplacingMode)
            {
                SetActiveTerminationOptions(mountingTask, true);
                SetActiveWires(mountingTask, false);
                return;
            }

            if (currentWire is not GangBoxConductor)
            {

                if (!ValidateTerminationSequence(mountingTask))
                {
                    return;
                }
            }

            currentTerminationIndex++;

            // Show termination options (small yellow orbs in the box)
            SetActiveTerminationOptions(mountingTask, true);
            // Hide interactable wires
            SetActiveWires(mountingTask, false);
        }

        /// <summary>
        /// Invoked when clicking on a termination option in a box (small yellow orb)
        /// </summary>
        /// <param name="clickedOption">Selected termination option</param>
        public void OnTerminationOptionClick(WireTerminationOption clickedOption)
        {
            currentOption = clickedOption;
            previewingMountingTask.RequestedOptionType = clickedOption.Option;
            OnMountPreview(previewingMountingTask);
        }

        public void OnHostBoxClick(Mountable mountable)
        {
            if (!isReactiveToHostBoxClicks)
            {
                return;
            }

            if (!(mountable.Name < FirstSupplyCableRoute))
            {
                return;
            }
            
            // Clicked on the Gang Box to terminate conductors
            if (mountable.Name == MountableName.TwoGangBox)
            {
                UpdatingGangBoxConductors?.Invoke();
                return;
            }

            InteractableWire fetchedWire = mountable.transform.parent.GetComponentInChildren<InteractableWire>(includeInactive: true);
            if (!fetchedWire)
            {
                return;
            }

            // Mountable has interactable wires
            TaskUpdated?.Invoke(fetchedWire.TerminationTask);
        }

        public void OnInteractableWireClick(InteractableWire interactableWire)
        {
            currentWire = interactableWire;

            if (interactableWire.Option == TerminationOptionType.None)
            {
                MountableSelected?.Invoke(interactableWire.Name);
                return;
            }

            WireMountingTask task = allWireMountingTaskMap[interactableWire.TerminationTask];
            if (!task.IsComplete() && currentWire is not GangBoxConductor)
            {
                ValidateTerminationSequence(task);
                return;
            }

            task.RequestedMountableName = interactableWire.Name;
            interactableWire.gameObject.SetActive(false);

            TaskUpdated?.Invoke(task.TaskName);
            ReplacingWire?.Invoke(task);
            HideInteractionWidget?.Invoke();
        }

        public override void OnMountPreview(MountingTask mountingTask)
        {
            if (mountingTask is not WireMountingTask wireMountingTask)
            {
                Debug.LogError("Trying to preview an invalid wire");
                return;
            }

            // Hide termination options (yellow orbs)
            SetActiveTerminationOptions(wireMountingTask, false);
            
            InteractableWire wireToPreview = FindMatchingWire(wireMountingTask);
            wireMountingTask.SelectedMountable = wireToPreview;
            wireToPreview.gameObject.SetActive(true);

            if (wireToPreview is GangBoxConductor gangBoxConductor)
            {
                ((GangBoxWireMountingTask)wireMountingTask).PreviewingWireNut =
                    EnableCompatibleWireNut(gangBoxConductor);
            }

            SetWireInteractable(previewingMountingTask.SelectedMountable, false);

            OpenConfirmationDialog(wireMountingTask);
        }

        public override void OnMountConfirm()
        {
            MountableName selectedWire = previewingMountingTask.RequestedMountableName;
            TerminationOptionType selectedOption = previewingMountingTask.RequestedOptionType;

            if (previewingMountingTask is GangBoxWireMountingTask gangBoxWireTask)
            {
                gangBoxWireTask.GangBoxSelectedOptionsMap[((GangBoxConductor)currentWire).ConnectedBox] =
                    new GangBoxWireMountingTask.ConductorSelection(selectedWire, selectedOption, 
                        ((GangBoxTerminationOption)currentOption));
                
                gangBoxWireTask.PreviewingWireNut = null;
            }
            else
            {
                previewingMountingTask.SelectedOptionsMap[selectedWire] = (InteractableWire)previewingMountingTask.SelectedMountable;
                previewingMountingTask.SelectedOrbOptions[selectedWire] = currentOption;

            }

            SetActiveWires(previewingMountingTask, true);
            SetWireInteractable(previewingMountingTask.SelectedMountable, true);

            if (!previewingMountingTask.IsComplete())
            {
                // Some wires left to terminate, go back and preview them
                SetActiveInitialWires(previewingMountingTask, true);
                ShowInstructionToast(previewingMountingTask);
                return;
            }

            // Process the completed task from here on out

            // As device connection tasks reuse WireMountingTasks, we filter termination tasks by
            // their TaskName enum values
            // PreviewingMountingTask is a Conductor Termination task
            if (previewingMountingTask.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
            {
                // All wires in the box are terminated
                MountingFinalized?.Invoke(previewingMountingTask);

                if (wireReplacingMode)
                {
                    wireReplacingMode = false;
                    ShowInteractionWidget?.Invoke();
                    ShowInstructionToast(previewingMountingTask);
                    return;
                }

                if (previewingMountingTask is GangBoxWireMountingTask)
                {
                    ShowAllTerminatedGangBoxConductors();
                }

                UpdateTaskMenu(previewingMountingTask);
                SetWiresInteractable(previewingMountingTask, false);
            }
            else // PreviewingMountingTask is a Device Connection task
            {
                DeviceWireConnectionFinalized?.Invoke();
            }

            ResetToolbar?.Invoke();
            ResetCamera?.Invoke(previewingMountingTask);
            if (previewingMountingTask.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
            {
                SetEnableOtherInteractables.Invoke(true);
            }
            previewingMountingTask = null;
            currentTerminationIndex = 0;
            currentWire = null;
            currentOption = null;
            HideToast?.Invoke();
        }

        public override void OnMountRedo()
        {
            // Hide currently previewing wire
            previewingMountingTask.SelectedMountable.gameObject.SetActive(false);

            if (previewingMountingTask is GangBoxWireMountingTask gangBoxWireTask)
            {
                if (gangBoxWireTask.PreviewingWireNut)
                {
                    gangBoxWireTask.PreviewingWireNut.gameObject.SetActive(false);
                    gangBoxWireTask.PreviewingWireNut = null;
                }
            }

            SetActiveWires(previewingMountingTask, true);
            SetWireInteractable(previewingMountingTask.SelectedMountable, true);
            previewingMountingTask.SelectedMountable = null;

            if (wireReplacingMode)
            {
                ShowInteractionWidget?.Invoke();
                return;
            }

            currentTerminationIndex--;

            SetActiveInitialWires(previewingMountingTask, true);
        }

        public void FetchComponentsFromObject(GameObject obj)
        {
            FetchComponents(taskOptionMap, obj);
            FetchComponents(taskWireMap, obj);
            FetchComponents(taskWireNutMap, obj);
        }

        public void ReleaseFetchedComponents(GameObject obj)
        {
            FetchComponents(taskOptionMap, obj, true);
            FetchComponents(taskWireMap, obj, true);
            FetchComponents(taskWireNutMap, obj, true);
        }

        /// <summary>
        /// Loading conductor states. Assuming all the box mounting tasks and run supply cable tasks are done.
        /// </summary>
        public override void OnLoadComplete(MountingTask mountingTask)
        {
            if (mountingTask is WireMountingTask wireTask)
            {
                DisableOldWiresWhenConnectingDevices(wireTask);
                // Just activating wires is sufficient for gang box wire loading. Unfortunately, that's
                // not the case for other wires.
                if (mountingTask is not GangBoxWireMountingTask)
                {
                    LoadRegularWireMountingTasks(wireTask);
                }

                // Enable loaded wires so that they will be visible in their host boxes
                if (wireTask.IsComplete())
                {
                    SetActiveWires(wireTask, true, false);
                }
                else
                {
                    SetActiveInitialWires(wireTask, true, false);
                }
                SetWiresInteractable(wireTask, false);

                if (!wireTask.IsComplete()) { return; }
                if (wireTask.TaskName > Task.TerminateGangBoxHots) { return; }

                UpdateTaskMenu(wireTask);
                MountingFinalized?.Invoke(wireTask);
            }
            else
            {
                Debug.LogError("Trying to load an invalid task");
            }
        }

        private void LoadRegularWireMountingTasks(WireMountingTask wireTask)
        {
            foreach (InteractableWire wire in taskWireMap[wireTask.TaskName])
            {
                if (wireTask.SavedWireTerminationOptions[wire.Name] == TerminationOptionType.None)
                {
                    wireTask.SelectedOptionsMap[wire.Name] = null;
                    continue;
                }

                // Filtering out which wire GameObjects to put into SelectedOptionsMap
                if (wire.Option != wireTask.SavedWireTerminationOptions[wire.Name]) { continue; }
                wireTask.SelectedOptionsMap[wire.Name] = wire;
                
                // Filling wireMountingTask.SelectedOrbOptions
                foreach (WireTerminationOption optionOrb in taskOptionMap[wireTask.TaskName])
                {
                    if (wire.Option != optionOrb.Option) { continue; }
                    wireTask.SelectedOrbOptions[wire.Name] = optionOrb;
                }
            }
        }

        private void UpdateTaskMenu(WireMountingTask wireTask)
        {
            if (wireTask is GangBoxWireMountingTask gangBoxWireTask)
            {
                UpdateGangBoxTerminationMenu?.Invoke(gangBoxWireTask.TaskName);
            }
            else
            {
                UpdateTerminationMenu?.Invoke(wireTask);
            }
        }

        private void FindHostBoxes()
        {
            BoxMountingTask[] boxMountingTasks = FindObjectsByType<BoxMountingTask>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (BoxMountingTask task in boxMountingTasks)
            {
                if (hostBoxNameMap.TryGetValue(task.SelectedMountable.Name,
                        out List<WireMountingTask> wireMountingTasks))
                {
                    foreach (WireMountingTask wireMountingTask in wireMountingTasks)
                    {
                        wireMountingTask.HostBoxInstance = task.SelectedMountable;
                    }
                }
            }
        }

        private void SetWireInteractable(Mountable wire, bool interactable)
        {
            if (!wire) { return; }
            string layerMaskName = interactable ? SceneInteractions.LayerDefault : SceneInteractions.LayerIgnoreRaycast;
            wire.gameObject.layer = LayerMask.NameToLayer(layerMaskName);
        }

        private void SetWiresInteractable(WireMountingTask wireMountingTask, bool interactable)
        {
            foreach (InteractableWire wire in taskWireMap[wireMountingTask.TaskName])
            {
                // Only set intractability of the visible wires
                if (!wire.isActiveAndEnabled) { continue; }

                SetWireInteractable(wire, interactable);
            }

            if (wireMountingTask.ConductorCorrectlyCompleted.Count != 0 && interactable == true ) 
            { 
                foreach (InteractableWire wire in wireMountingTask.ConductorCorrectlyCompleted)
                {
                    SetWireInteractable(wire, false);
                }
            }
        }

        private bool ValidateTerminationSequence(WireMountingTask task)
        {
            if (task.OverriddenSequence.Length != 0 &&
                currentTerminationIndex >= task.OverriddenSequence.Length)
            {
                // Return after reaching the end of the OverriddenSequence
                return true;
            }

            MountableName[] currentSequence =
                task.OverriddenSequence.Length != 0 ? task.OverriddenSequence : TerminationSequence;

            bool isValid = task.RequestedMountableName == currentSequence[currentTerminationIndex];
            if (!isValid)
            {
                ShowFeedback?.Invoke(ToastMessageType.Error,
                    IsDeviceRelatedTask(task) ? WrongSequenceConnection : WrongSequenceTermination, ToastTimeout);
            }
            else 
            {
                HideToast?.Invoke();
            }
           
            return isValid;
        }

        private bool IsDeviceRelatedTask(WireMountingTask wireMountingTask)
        {
            return wireMountingTask.GetComponent<DeviceMountingTask>();
        }

        private void ShowInstructionToast(WireMountingTask currentTask)
        {
            ShowFeedback?.Invoke(ToastMessageType.Info,
                IsDeviceRelatedTask(currentTask) ? ConnectionInstruction : TerminationInstruction,
                ToastMessageUI.NoTimeout);
        }

        private void SetupInteractionWidget()
        {
            AddButton?.Invoke(ButtonType.GoBack, () =>
            {
                if (previewingMountingTask is GangBoxWireMountingTask)
                {
                    ShowAllTerminatedGangBoxConductors();
                }

                SetWiresInteractable(previewingMountingTask, false);
                ResetCamera?.Invoke(previewingMountingTask);
                EnableTaskListButton?.Invoke();
                SetEnableOtherInteractables.Invoke(true);
                previewingMountingTask = null;
                wireReplacingMode = false;
                currentWire = null;
                currentOption = null;
                HideToast?.Invoke();
            });
        }

        private void OpenConfirmationDialog(WireMountingTask task)
        {
            MountableName overriddenName = task.GetOverridenName(task.RequestedMountableName);

            ConfirmSelectionInfo confirmInfo;

            confirmInfo.MainLabelText = UIHelper.ToDescription(
                overriddenName == MountableName.None ? task.RequestedMountableName : overriddenName);

            confirmInfo.SecondaryLabelText = UIHelper.ToDescription(task.TaskName);
            confirmInfo.IsPreviewing = true;

            ShowConfirmationDialog?.Invoke(confirmInfo);
        }

        private InteractableWire FindMatchingWire(WireMountingTask task)
        {
            List<InteractableWire> interactableWires = taskWireMap[task.TaskName];
            foreach (InteractableWire wire in interactableWires)
            {
                if (currentWire is GangBoxConductor)
                {
                    if (wire is GangBoxConductor conductor)
                    {
                        if (conductor.ConnectedBox == ((GangBoxTerminationOption)currentOption).ConnectedBox &&
                            wire.Name == task.RequestedMountableName &&
                            wire.Option == task.RequestedOptionType)
                        {
                            return wire;
                        }
                    }
                }
                else if (wire.Option == task.RequestedOptionType &&
                         wire.Name == task.RequestedMountableName)
                {
                    return wire;
                }
            }

            return null;
        }

        private void SetActiveTerminationOptions(WireMountingTask task, bool activate)
        {
            List<WireTerminationOption> terminationOptions = taskOptionMap[task.TaskName];

            // Turn off all orbs if we are disabling
            if (!activate)
            {
                terminationOptions.ForEach(option => option.gameObject.SetActive(false));
                return;
            }

            // Special case for GangBoxWireMountingTask
            if (task is GangBoxWireMountingTask gangBoxTask)
            {
                foreach (var option in terminationOptions)
                {
                    if (option is GangBoxTerminationOption gangBoxTerminationOption)
                    {
                        if (((GangBoxConductor)currentWire).ConnectedBox != gangBoxTerminationOption.ConnectedBox)
                        {
                            continue;
                        }
                    }
                    option.gameObject.SetActive(true);
                }

                return;
            }

            if (currentWire == null) { return; }

            // Set of all interactable wires based on conductor type (ie. all bond wire poses)
            List<InteractableWire> wirePosesByType = taskWireMap[task.TaskName]
                .Where(wire => wire.Name == currentWire.Name).ToList();

            // Array for storing corresponding termination options for the given conductor type
            foreach (var wire in wirePosesByType)
            {
                var selection = terminationOptions
                    .DefaultIfEmpty(null)
                    .FirstOrDefault(option => option.Option == wire.Option);

                if (selection)
                {
                    selection.gameObject.SetActive(true);
                }

                // alternative without error check
                // optionsByPosition.Add(terminationOptions.First(option => option.Option == wire.Option));
            }
        }

        private void SetActiveInitialWires(WireMountingTask task, bool activate, bool excludeOtherGangBoxTasks = true)
        {
            if (task is GangBoxWireMountingTask gangBoxTask && excludeOtherGangBoxTasks)
            {
                HideWiresOfOtherGangBoxTasks(gangBoxTask);
            }

            List<InteractableWire> interactableWires = taskWireMap[task.TaskName];
            foreach (InteractableWire wire in interactableWires)
            {
                if (!activate)
                {
                    if (wire.Option == TerminationOptionType.None)
                    {
                        wire.gameObject.SetActive(false);
                    }
                    continue;
                }

                if (wire is GangBoxConductor gangBoxConductor)
                {
                    if (wire.Option == TerminationOptionType.None &&
                        task is GangBoxWireMountingTask gangBoxWireTask &&
                    gangBoxWireTask.GangBoxSelectedOptionsMap.TryGetValue(
                        gangBoxConductor.ConnectedBox, out GangBoxWireMountingTask.ConductorSelection selection) &&
                        selection.TerminationOption == TerminationOptionType.None)
                    {
                        wire.gameObject.SetActive(true);
                    }
                    continue;
                }

                // Only activate wires that have not been mounted/selected
                if (wire.Option == TerminationOptionType.None &&
                    task.SelectedOptionsMap.TryGetValue(wire.Name, out InteractableWire option) && option == null)
                {
                    wire.gameObject.SetActive(true);
                }
            }
        }

        private void SetActiveWires(WireMountingTask task, bool activate, bool excludeOtherGangBoxTasks = true)
        {
            if (!activate)
            {
                if (taskWireNutMap.TryGetValue(task.TaskName, out List<WireNut> wireNuts))
                {
                    foreach (WireNut wireNut in wireNuts)
                    {
                        wireNut.gameObject.SetActive(false);
                    }
                }
            }

            if (task is GangBoxWireMountingTask gangBoxTask && excludeOtherGangBoxTasks)
            {
                HideWiresOfOtherGangBoxTasks(gangBoxTask);
            }

            List<InteractableWire> interactableWires = taskWireMap[task.TaskName];
            foreach (InteractableWire wire in interactableWires)
            {
                if (!activate)
                {
                    wire.gameObject.SetActive(false);
                    continue;
                }

                if (task is GangBoxWireMountingTask gangBoxWireTask)
                {
                    if (wire is not GangBoxConductor gangBoxConductor) { continue; }
                    
                    foreach (var kvp in gangBoxWireTask.GangBoxSelectedOptionsMap)
                    {
                        MountableName connectedBox = kvp.Key;
                        GangBoxWireMountingTask.ConductorSelection conductor = kvp.Value;

                        // Only activate wires that have not been mounted/selected
                        if (gangBoxConductor.ConnectedBox == connectedBox &&
                            gangBoxConductor.Option == conductor.TerminationOption)
                        {
                            wire.gameObject.SetActive(true);
                            EnableCompatibleWireNut(gangBoxConductor);
                        }
                    }
                }
                // Only activate wires that have not been mounted/selected
                else if (task.SelectedOptionsMap.TryGetValue(wire.Name, out InteractableWire selected) && 
                         selected != null &&
                         selected.Option == wire.Option)
                {
                    wire.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Hide conductors and wire nuts of Gang Box termination tasks except the given task.
        /// </summary>
        /// <param name="task">Only leave conductors visible of this task</param>
        private void HideWiresOfOtherGangBoxTasks(GangBoxWireMountingTask task)
        {
            foreach (var kvp in taskWireMap)
            {
                Task taskToHide = kvp.Key;

                if (!TerminateGangBoxSubMenuController.TaskSequence.Contains(taskToHide))
                {
                    // Continue if the task is not a Gang Box termination task
                    continue;
                }

                if (taskToHide == task.TaskName)
                {
                    continue;
                }

                foreach (InteractableWire wire in kvp.Value)
                {
                    wire.gameObject.SetActive(false);
                }

                if (taskWireNutMap.TryGetValue(taskToHide, out List<WireNut> wireNuts))
                {
                    foreach (WireNut wireNut in wireNuts)
                    {
                        wireNut.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Set active terminated Gang Box conductors
        /// </summary>
        private void ShowAllTerminatedGangBoxConductors()
        {
            foreach (WireMountingTask wireMountingTask in allWireMountingTaskMap.Values)
            {
                if (wireMountingTask is not GangBoxWireMountingTask) { continue; }
                if (!wireMountingTask.IsComplete()) { continue; }

                // Show previously hidden conductors of other Gang Box termination tasks
                SetActiveWires(wireMountingTask, true, false);
            }
        }

        private void DisableOldWiresWhenConnectingDevices(WireMountingTask mountingTask)
        {
            if (hostBoxNameMap.TryGetValue(mountingTask.HostBoxName, out List<WireMountingTask> wireMountingTasks))
            {
                foreach (WireMountingTask taskFromMap in wireMountingTasks)
                {
                    if (mountingTask.TaskName >= DeviceMountingTask.FirstDeviceConnectionMember &&
                        taskFromMap.TaskName < DeviceMountingTask.FirstDeviceConnectionMember)
                    {
                        SetActiveWires(taskFromMap, false);
                    }
                }
            }
        }
        
        private WireNut EnableCompatibleWireNut(GangBoxConductor gangBoxConductor)
        {
            if (gangBoxConductor.Option == TerminationOptionType.TiedToWireNutWithOthers)
            {
                foreach (WireNut wireNut in taskWireNutMap[gangBoxConductor.TerminationTask])
                {
                    foreach (var compatibleConductor in wireNut.CompatibleConductors)
                    {
                        if (compatibleConductor == gangBoxConductor)
                        {
                            wireNut.gameObject.SetActive(true);
                            return wireNut;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// This method will fill dictionaries that has a type of key = Task, value = T by finding
        /// relevant type of components in the scene. If a GameObject is provided, this will find
        /// relevant components in that GameObject instead of the scene.
        /// </summary>
        /// <param name="taskComponentMap">Dictionary to fill</param>
        /// <param name="obj">Find components in the given GameObject instead</param>
        /// <param name="delete">Remove elements from the dictionary</param>
        /// <typeparam name="T">Component type to search in the scene</typeparam>
        private void FetchComponents<T>(Dictionary<Task, List<T>> taskComponentMap, GameObject obj = null, bool delete = false)
            where T : Component
        {
            T[] components;
            if (obj)
            {
                // Find in the given object
                components = obj.GetComponentsInChildren<T>(true);
            }
            else
            {
                // Find in the scene
                components = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            foreach (T component in components)
            {
                Task key = component switch
                {
                    WireTerminationOption terminationOption => terminationOption.TerminationTask,
                    InteractableWire interactableWire => interactableWire.TerminationTask,
                    WireNut wireNut => wireNut.TerminationTask,
                    _ => Task.None
                };

                if (delete)
                {
                    taskComponentMap.Remove(key);
                    continue;
                }

                if (!taskComponentMap.TryGetValue(key, out var componentList))
                {
                    taskComponentMap[key] = new List<T> { component };
                }
                else
                {
                    componentList.Add(component);
                }
            }
        }
    }
}
