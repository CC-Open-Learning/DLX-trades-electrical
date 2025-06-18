using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using static VARLab.TradesElectrical.ObjectInteractionWidget;

namespace VARLab.TradesElectrical
{
    public class EditMountedObject : MonoBehaviour
    {
        [SerializeField] private TaskHandler taskHandler;
        [SerializeField] private FeedbackHandler feedbackHandler;
        [SerializeField] private RunSupplyCableMenuController runSupplyCableMenuController;

        [Header("Backend Events")]
        public UnityEvent<BoxMountingTask> PreviewBox = new();
        [FormerlySerializedAs("ResetCamera")] public UnityEvent ActionDiscarded = new();
        public UnityEvent HideMeasurements = new();
        public UnityEvent<bool> EnableBoxColliders = new();
        public UnityEvent<BoxMountingTask> MovingMountable = new();
        public UnityEvent<MountingTask> ReplacingMountable = new();

        [Header("UI Events")]
        public UnityEvent ShowInteractionWidget = new();
        public UnityEvent HideInteractionWidget = new();
        public UnityEvent<ButtonType, Action> AddButton = new();
        public UnityEvent<ButtonType, string> UpdateButtonText = new();
        public UnityEvent<ButtonType, bool> SetEnabledButton = new();
        
        public bool IsGCFeedbackInvoked { private get; set; } = false;

        // This flag is used to avoid unintentional mouse clicks on mounted objects when panning
        private Mountable currentMountable;
        private MountingTask currentlyEditingTask;
        public bool isAllBoxMountingTasksCorrect { get; private set; }

        public void OnMountableClick(Mountable clickedMountable)
        {
            HideInteractionWidget?.Invoke(); // Hide existing widget if any

            if (!taskHandler.MountedObjectMap.TryGetValue(clickedMountable, out MountingTask mountingTask))
            {
                Debug.Log($"Make sure the mountable: {clickedMountable.transform.parent.name} is mounted " +
                          $"before editing");
                return;
            }

            currentlyEditingTask = mountingTask;

            if (mountingTask is BoxMountingTask boxMountingTask)
            {
                if (isAllBoxMountingTasksCorrect)
                {
                    return;
                }

                PreviewBox?.Invoke(boxMountingTask);
                EnableBoxColliders?.Invoke(false);
                SetupWidgetForEditingBoxes(boxMountingTask);
            }
            else if (mountingTask is CableMountingTask cableMountingTask)
            {
                // TODO: Update camera when editing cables
                SetupWidgetForEditingCables();
            }
            else if (mountingTask is DeviceMountingTask)
            {
                SetupWidgetForEditingDevices();
            }

            ShowInteractionWidget?.Invoke();
        }

        public void OnAllBoxMountingTasksCorrect()
        {
            isAllBoxMountingTasksCorrect = true;
            if (currentlyEditingTask != null)
            {
                OnCancelButtonClick();
            }
        }

        private void HideUIWidget()
        {
            HideInteractionWidget?.Invoke();
            HideMeasurements?.Invoke();

            // For cables, only enable box colliders if terminate quiz is completed
            if (currentlyEditingTask is CableMountingTask)
            {
                if (runSupplyCableMenuController.IsTerminateQuizCompleted)
                {
                    EnableBoxColliders?.Invoke(true);
                }
            }
            // For other objects, enable box colliders with no conditions
            else
            {
                EnableBoxColliders?.Invoke(true);
            }

            currentlyEditingTask = null;
        }

        private void OnMoveButtonClick()
        {
            if (currentlyEditingTask == null) { return; }

            MovingMountable?.Invoke((BoxMountingTask)currentlyEditingTask);
            HideUIWidget();
        }

        private void OnReplaceButtonClick()
        {
            if (currentlyEditingTask == null) { return; }

            ReplacingMountable?.Invoke(currentlyEditingTask);
            HideUIWidget();
        }

        private void OnCancelButtonClick()
        {
            HideUIWidget();
            ActionDiscarded?.Invoke();
        }

        private void SetupWidgetForEditingBoxes(BoxMountingTask task)
        {
            AddButton?.Invoke(ButtonType.Move, OnMoveButtonClick);
            AddButton?.Invoke(ButtonType.Replace, OnReplaceButtonClick);
            AddButton?.Invoke(ButtonType.GoBack, OnCancelButtonClick);

            string taskName = UIHelper.ToDescription(task.TaskName);
            UpdateButtonText?.Invoke(ButtonType.Move, $"Move {taskName}");
            UpdateButtonText?.Invoke(ButtonType.Replace, $"Replace {taskName}");

            SetWidgetForEditingBoxesBtnStates();
        }
        
        private void SetWidgetForEditingBoxesBtnStates()
        {
            if (IsGCFeedbackInvoked)
            {
                bool isCorrectLocation = feedbackHandler.GetGivenBoxLocationStatus(currentlyEditingTask.TaskName);
                bool isCorrectMountable = feedbackHandler.GetGivenBoxMountStatus(currentlyEditingTask.TaskName);

                SetEnabledButton?.Invoke(ButtonType.Move, !isCorrectLocation);
                SetEnabledButton?.Invoke(ButtonType.Replace, !isCorrectMountable);
            }
            else
            {
                SetEnabledButton?.Invoke(ButtonType.Move, true);
                SetEnabledButton?.Invoke(ButtonType.Replace, true);
            }
        }

        public void RefreshWidgetForEditingBoxes()
        {
            if (currentlyEditingTask != null)
            {
                SetWidgetForEditingBoxesBtnStates();
            }
        }

        private void SetupWidgetForEditingCables()
        {
            AddButton?.Invoke(ButtonType.Replace, OnReplaceButtonClick);
            AddButton?.Invoke(ButtonType.GoBack, OnCancelButtonClick);

            UpdateButtonText?.Invoke(ButtonType.Replace, $"Replace Cable");
        }

        private void SetupWidgetForEditingDevices()
        {
            AddButton?.Invoke(ButtonType.Replace, OnReplaceButtonClick);
            AddButton?.Invoke(ButtonType.GoBack, OnCancelButtonClick);

            UpdateButtonText?.Invoke(ButtonType.Replace, $"Replace Device");
        }
    }
}
