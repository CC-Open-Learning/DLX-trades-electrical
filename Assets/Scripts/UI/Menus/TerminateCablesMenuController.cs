using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     This class is a MonoBehaviour that manages the UI and behaviour of the TerminateCablesMenu in our
    ///     simulation.
    /// </summary>
    public class TerminateCablesMenuController : MenuController
    {
        public const string GangBoxButtonClassName = "BtnGangBox";

        public const string AvailableLabelName = "LabelStatusAvailable";
        public const string NotAvailableLabelName = "LabelStatusNotAvailable";
        public const string CompleteLabelName = "LabelStatusComplete";

        private const Task FallbackGangBoxTask = Task.TerminateGangBox;

        [FormerlySerializedAs("<BackButtonPressed>k__BackingField")]
        public UnityEvent BackButtonPressed = new();

        [FormerlySerializedAs("<TerminateTaskSelected>k__BackingField")]
        public UnityEvent<Task> TerminateTaskSelected = new();

        [FormerlySerializedAs("<TerminateGangBoxSubTaskSelected>k__BackingField")]
        public UnityEvent<Task> TerminateGangBoxSubTaskSelected = new();


        protected readonly Dictionary<string, Task> ItemOptionToTaskMap = new();
        protected readonly Dictionary<Button, Dictionary<string, Label>> OptionButtonToLabelsMap = new();
        private readonly HashSet<Task> gangBoxSubTasks = new();

        private Task currentTask;


        /// <summary>
        ///     Initializes the user interface elements of the menu.
        /// </summary>
        public override void Initialize()
        {
            // Close button events
            base.Initialize();

            // Back button events
            var backButton = Root.Q<Button>(UIHelper.BackButtonId);

            backButton.clicked += () =>
            {
                BackButtonPressed?.Invoke();
                Display(false);
            };

            // One-time setup
            PopulateItemOptionToTaskMap();

            ConfigureTaskOptions();
        }

        protected virtual void ConfigureTaskOptions()
        {
            // Categorized option buttons events
            Root.Query<Button>(className: UIHelper.ClassTaskOption).ForEach((button) =>
            {
                PopulateCurrentLabelsMapping(button);
                OnlyEnableOptionButtonLabel(button, AvailableLabelName);

                button.clicked += () =>
                {
                    currentTask = ItemOptionToTaskMap[button.name];

                    if (button.name == GangBoxButtonClassName)
                    {
                        TerminateGangBoxSubTaskSelected?.Invoke(currentTask);
                    }
                    else
                    {
                        TerminateTaskSelected?.Invoke(currentTask);
                    }

                    Display(false);
                };
            });
        }

        /// <summary>
        ///     Handles the event when all wires in a box are terminated (excluding Gang Box)
        /// </summary>
        /// <param name="mountingTask">The mounting task that is being processed</param>
        public virtual void HandleAllWiresTerminated(WireMountingTask mountingTask)
        {
            if (mountingTask.TaskName == Task.None) return;
            if (gangBoxSubTasks.Contains(mountingTask.TaskName)) return;

            currentTask = mountingTask.TaskName;
            Button button = GetButtonFromValidTask(currentTask);

            // Mark mounting task as completed
            OnlyEnableOptionButtonLabel(button, CompleteLabelName);
        }

        public void SetTerminateTaskBtnEnabled(Task mountingTask, bool status)
        {
            if (mountingTask == Task.None) return;
            currentTask = mountingTask;
            Button button = GetButtonFromValidTask(currentTask);

            // Mark mounting task as completed
            button.SetEnabled(false);
        }

        /// <summary>
        ///     Handles the event when all wires in the gang box have been terminated
        /// </summary>
        /// <param name="subTask">The subtask that invoked the functionality</param>
        public void HandleAllGangBoxWiresTerminated(Task subTask)
        {
            if (subTask == Task.None) return;
            if (!gangBoxSubTasks.Contains(subTask)) return;

            currentTask = FallbackGangBoxTask;
            Button button = GetButtonFromValidTask(currentTask);

            // Mark gang box task as completed
            OnlyEnableOptionButtonLabel(button, CompleteLabelName);
        }

        /// <summary>
        ///     Retrieves the current button mapping the task name to its associated button
        /// </summary>
        /// <param name="task">The task associated with the button to retrieve</param>
        /// <returns>A button object representing the option button</returns>
        protected Button GetButtonFromValidTask(Task task)
        {
            var buttonName = Array.Find(ItemOptionToTaskMap.Keys.ToArray(),
                button => ItemOptionToTaskMap[button] == task);

            return Root.Q<Button>(name: buttonName);
        }

        /// <summary>
        ///     Displays specified label on the given button by hiding all other labels and displaying the target.
        /// </summary>
        /// <param name="button">The button whose label needs to be displayed.</param>
        /// <param name="labelName">The name of the label to be displayed.</param>
        protected virtual void OnlyEnableOptionButtonLabel(Button button, string labelName)
        {
            HideAllCurrentOptionLabels(button);
            OptionButtonToLabelsMap[button][labelName].style.display = DisplayStyle.Flex;
        }

        /// <summary>
        ///     Hides all the option labels associated with the specified button.
        ///     This method is used to ensure that only the relevant label is displayed for the button.
        /// </summary>
        /// <param name="button">The button whose option labels need to be hidden.</param>
        protected virtual void HideAllCurrentOptionLabels(Button button)
        {
            foreach (var label in OptionButtonToLabelsMap[button].Select(kvp => kvp.Value))
            {
                label.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        ///     Populates the mapping between the specified button and its associated labels.
        ///     This method is used to store references to the task count, not available, and complete labels
        ///     for the given button, so that they can be easily accessed and updated later.
        /// </summary>
        /// <param name="button">The button whose labels need to be mapped.</param>
        protected virtual void PopulateCurrentLabelsMapping(Button button)
        {
            var availableLabel = button.Q<Label>(AvailableLabelName);
            var notAvailableLabel = button.Q<Label>(NotAvailableLabelName);
            var completeLabel = button.Q<Label>(CompleteLabelName);

            var currentLabelsMap = new Dictionary<string, Label>
            {
                { AvailableLabelName, availableLabel },
                { NotAvailableLabelName, notAvailableLabel },
                { CompleteLabelName, completeLabel }
            };

            OptionButtonToLabelsMap.Add(button, currentLabelsMap);
        }

        /// <summary>
        ///     Populates the ItemOptionToTaskMap with button name to terminate task
        ///     mappings
        /// </summary>
        protected virtual void PopulateItemOptionToTaskMap()
        {
            foreach (Task task in Enum.GetValues(typeof(Task)))
            {
                var taskName = task.ToString();

                // Ignore any tasks that are not terminate-related
                if (!taskName.StartsWith("Terminate")) continue;
                if (!taskName.EndsWith("Box"))
                {
                    gangBoxSubTasks.Add(task);
                    continue;
                }

                // Translate a terminate task to a button name and add it to the itemOptionMap
                var btnName = taskName.Replace("Terminate", "Btn");
                ItemOptionToTaskMap.Add(btnName, task);
            }
        }
    }
}