using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class TerminateGangBoxSubMenuController : TerminateCablesMenuController
    {
        private const string WrongWireFeedback = "Try again. Conductors must be terminated in correct order.";

        public static readonly Task[] TaskSequence = new[]
        {
            Task.TerminateGangBoxBonds,
            Task.TerminateGangBoxNeutrals,
            Task.TerminateGangBoxHots,
        };

        public UnityEvent<ToastMessageType, string, float> ShowFeedback = new();
        public UnityEvent<Task> LastSubTaskCompleted = new();

        private Task currentSubTask;


        protected override void ConfigureTaskOptions()
        {
            // Categorized option buttons events
            Root.Query<Button>(className: "item-option").ForEach((button) =>
            {
                PopulateCurrentLabelsMapping(button);
                OnlyEnableOptionButtonLabel(button, AvailableLabelName);

                button.clicked += () =>
                {
                    currentSubTask = Enum.Parse<Task>(button.viewDataKey);

                    int taskIndex = 0;
                    foreach (var task in TaskSequence)
                    {
                        if (task == currentSubTask)
                        {
                            break;
                        }

                        taskIndex++;
                    }

                    if (currentSubTask != TaskSequence[0])
                    {
                        var gangBoxWireTasks =
                            FindObjectsByType<GangBoxWireMountingTask>(FindObjectsInactive.Exclude,
                                FindObjectsSortMode.None);
                        foreach (var gangBoxWireTask in gangBoxWireTasks)
                        {
                            if (gangBoxWireTask.TaskName != TaskSequence[taskIndex - 1])
                            {
                                continue;
                            }

                            if (gangBoxWireTask.GangBoxSelectedOptionsMap.Values.All(
                                    conductor => conductor.TerminationOption == TerminationOptionType.None))
                            {
                                ShowFeedback?.Invoke(ToastMessageType.Error, WrongWireFeedback, 3f);
                                return;
                            }
                        }
                    }

                    TerminateTaskSelected?.Invoke(currentSubTask);
                    Display(false);
                };
            });
        }


        /// <summary>
        ///     Handles the event when all wires in a box are terminated
        /// </summary>
        /// <param name="task">The mounting task that is being processed</param>
        public void HandleAllWiresTerminated(Task task)
        {
            if (task == Task.None) return;
            currentSubTask = task;

            // Check if the task is the last one in the sequence
            if (currentSubTask == Task.TerminateGangBoxHots)
            {
                LastSubTaskCompleted?.Invoke(currentSubTask);
            }

            // Fetch the current button mapping the task name to its associated button
            var buttonName = Array.Find(ItemOptionToTaskMap.Keys.ToArray(),
                button => ItemOptionToTaskMap[button] == currentSubTask);
            var button = Root.Q<Button>(className: "item-option", name: buttonName);

            // Mark mounting task as completed
            OnlyEnableOptionButtonLabel(button, CompleteLabelName);
        }
        
        public void SetTerminateTaskBtnEnabled(GangBoxWireMountingTask mountingTask, bool status)
        {
            if (mountingTask.TaskName == Task.None) return;
            SetTerminateTaskBtnEnabled(mountingTask.TaskName, status);
        }
        
        /// <summary>
        ///     Populates the ItemOptionToTaskMap with button name to terminate task
        ///     mappings
        /// </summary>
        protected override void PopulateItemOptionToTaskMap()
        {
            foreach (Task task in Enum.GetValues(typeof(Task)))
            {
                var taskName = task.ToString();

                if (task == Task.TerminateGangBox) continue;

                // Translate a terminate task to a button name and add it to the itemOptionMap
                var btnName = taskName.Replace("TerminateGangBox", "Btn");
                ItemOptionToTaskMap.Add(btnName, task);
            }
        }
    }
}