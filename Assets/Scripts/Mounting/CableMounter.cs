using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class CableMounter : Mounter, IReplaceable
    {
        [JsonProperty] private bool securedCableEnabled;

        public bool SecuredCablesEnabled
        {
            private get => securedCableEnabled;
            set
            {
                securedCableEnabled = value;
            }
        }

        [SerializeField] private UnityEvent<CableMountingTask> previewingCableSelection = new();
        [SerializeField] private UnityEvent<Task> openCableRouteSelection = new();
        [SerializeField] private UnityEvent<MountingTask> mountingFinalized = new();

        [SerializeField] private RunSupplyCableMenuController runSupplyCableMenuController;

        public UnityEvent<bool> EnableSupplyCableColliders = new();
        public UnityEvent<bool> EnableBoxMountableColliders = new();

        private Mountable previewingMountable;
        private readonly Dictionary<CableMountingTask, Mountable> mountedCablesMap = new();
        private CableMountingTask currentMountingTask;

        public override void OnMountPreview(MountingTask mountingTask)
        {
            currentMountingTask = mountingTask as CableMountingTask;
            if (!currentMountingTask)
            {
                Debug.LogError("Trying to preview an invalid cable");
                return;
            }

            Mountable mountable = GetMountableFromMountableName(mountingTask.RequestedMountableName);
            if (mountable)
            {
                SetMountedCablesState(isMounted: false);
                SwapPreviewingMountable(mountable);
                previewingCableSelection?.Invoke(currentMountingTask);
                EnableSupplyCableColliders?.Invoke(false);
            }
        }

        public override void OnMountConfirm()
        {
            FinalizeMountingCable();
        }

        public override void OnMountRedo()
        {
            currentMountingTask.SelectedMountable = null;
            currentMountingTask.PreviewingMountable.gameObject.SetActive(false);
            SetMountedCablesState(isMounted: true);
            openCableRouteSelection?.Invoke(currentMountingTask.TaskName);
            EnableSupplyCableColliders?.Invoke(true);
        }

        public void OnMountableReplace(MountingTask mountingTask)
        {
            currentMountingTask = (CableMountingTask)mountingTask;
            if (!currentMountingTask)
            {
                Debug.LogError("Trying to swap an invalid cable!");
                return;
            }
            
            MountableName requestedMountable = mountingTask.RequestedMountableName;
            Mountable mountable = GetMountableFromMountableName(requestedMountable);
            if (SecuredCablesEnabled)
            {
                currentMountingTask.PreviewingMountable = mountable;
            }
            SwapPreviewingMountable(mountable);
            FinalizeMountingCable();
        }

        private Mountable GetMountableFromMountableName(MountableName selectedMountable)
        {
            // Sticking to the iterative method to find the mountable instead of using a data container like
            // a HashSet because the max number of cable selection options is going to be 3.
            foreach (CableMountingTask.CableMountableContainer mountable in currentMountingTask.MountableOptions)
            {
                bool matchingCableInContainerFound = mountable.UnsecureCable.Name == selectedMountable ||
                                                      mountable.SecuredCables.Name == selectedMountable;
                if (matchingCableInContainerFound)
                {
                    return SecuredCablesEnabled ? mountable.SecuredCables : mountable.UnsecureCable;                
                }
            }

            return null;
        }

        public void SwapAllCablesToSecuredVersions()
        {
            List<CableMountingTask> cableMountingTasksList = mountedCablesMap.Keys.ToList();
            foreach (var task in cableMountingTasksList)
            {
                currentMountingTask = task;

                foreach (var cable in task.MountableOptions)
                {
                    if (currentMountingTask.CorrectMountable.Equals(cable.UnsecureCable))
                    {
                        currentMountingTask.CorrectMountable = cable.SecuredCables;
                    }
                    
                    if (!cable.UnsecureCable.isActiveAndEnabled) continue;
                    
                    SwapPreviewingMountable(cable.SecuredCables);
                    FinalizeMountingCable(modifyState: false);
                }
            }
        }
        
        private void SwapPreviewingMountable(Mountable newMountable)
        {
            // Hide the existing cable of the same mounting task if present
            if (mountedCablesMap.TryGetValue(currentMountingTask, out Mountable oldMountable))
            {
                oldMountable.gameObject.SetActive(false);
            }

            currentMountingTask.PreviewingMountable = newMountable;
            newMountable.gameObject.SetActive(true);
        }

        private void SetMountedCablesState(bool isMounted)
        {
            foreach (var kvp in mountedCablesMap)
            {
                Mountable mountedCable = kvp.Value;
                mountedCable.gameObject.SetActive(isMounted);
            }
        }

        private void FinalizeMountingCable(bool modifyState = true)
        {
            mountedCablesMap[currentMountingTask] = currentMountingTask.PreviewingMountable;
            currentMountingTask.SelectedMountable = currentMountingTask.PreviewingMountable;
            mountingFinalized?.Invoke(currentMountingTask);
            EnableSupplyCableColliders?.Invoke(true);
            if (runSupplyCableMenuController.IsTerminateQuizCompleted)
            {
                EnableBoxMountableColliders?.Invoke(true);
            }

            if (modifyState)
            {
                SetMountedCablesState(isMounted: true);
                currentMountingTask = null;
            }
        }
        
        public override void OnLoadComplete(MountingTask task)
        {
            if (task is not CableMountingTask cableTask) return;
            currentMountingTask = cableTask;

            Mountable mountable = GetMountableFromMountableName(task.SelectedMountableName);
            if (SecuredCablesEnabled)
            {
                currentMountingTask.PreviewingMountable = mountable;
            }

            currentMountingTask.PreviewingMountable = mountable;
            mountable.gameObject.SetActive(true);
            FinalizeMountingCable();
        }
        
    }
}
