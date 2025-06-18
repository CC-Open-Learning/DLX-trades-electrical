using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     Represents the current status of a Workflow
    /// </summary>
    public enum WorkflowStatus
    {
        Complete,
        InProgress,
        Unavailable,
        Available
    }

    /// <summary>
    ///     An abstract base class for managing common Workflow Selection functionality,
    ///     providing common behavior for derived selection menus.
    /// </summary>
    public abstract class WorkflowSelectionMenuController : MenuController
    {
        // Identifiers
        private const string StatusLabelId = "StatusLabel";
        private const string TaskCountLabelId = "LabelTaskCount";

        // Events
        [Tooltip("Does this menu have a back button available?")]
        public bool HasBackwardsNavigation = true;

        public UnityEvent BackButtonPressed = new();

        public override void Initialize()
        {
            base.Initialize();

            // Configure Workflow Buttons (handled by derived classes)
            ConfigureWorkflowButtons();

            // Configure Navigation
            if (HasBackwardsNavigation)
            {
                ConfigureBackNavigation();
            }
        }

        /// <summary>
        ///     Derived classes will implement this to configure any context-specific workflow buttons
        /// </summary>
        protected abstract void ConfigureWorkflowButtons();

        /// <summary>
        ///     Handles the click of a workflow button using a reference to an event
        ///     to invoke
        /// </summary>
        /// <param name="buttonClicked">The button representing the workflow that was clicked</param>
        /// <param name="action">The event to invoke to handle menu navigation</param>
        protected void HandleWorkflowButtonClick(Button buttonClicked, UnityEvent action)
        {
            SetButtonStatus(buttonClicked, WorkflowStatus.InProgress);
            action?.Invoke();
            Display(false);
        }

        /// <summary>
        ///     Sets the Task Count value of a Workflow
        /// </summary>
        /// <param name="button">The button representing the current workflow</param>
        /// <param name="tasksCompleted">The current number of tasks completed</param>
        /// <param name="totalWorkflowTasks">The total number of tasks for this workflow</param>
        protected static void SetButtonCompletedTaskCount(Button button, int tasksCompleted, int totalWorkflowTasks)
        {
            var label = button.Q<Label>(TaskCountLabelId);
            label.text = $"{tasksCompleted} of {totalWorkflowTasks} complete";
        }

        /// <summary>
        ///     Used by derived classes to determine the current status of the button
        ///     as defined by its embedded status label
        /// </summary>
        /// <param name="button"></param>
        /// <returns></returns>
        protected WorkflowStatus GetButtonStatus(Button button)
        {
            var label = button.Q<Label>(StatusLabelId);
            if (label == null || label.ClassListContains(UIHelper.ClassTaskStatusUnavailable))
            {
                return WorkflowStatus.Unavailable;
            }

            if (label.ClassListContains(UIHelper.ClassTaskStatusAvailable))
            {
                return WorkflowStatus.Available;
            }

            if (label.ClassListContains(UIHelper.ClassTaskStatusInProgress))
            {
                return WorkflowStatus.InProgress;
            }

            if (label.ClassListContains(UIHelper.ClassTaskStatusComplete))
            {
                return WorkflowStatus.Complete;
            }

            // Default case if button contains none of the status message states for some reason
            return WorkflowStatus.Unavailable;

        }

        /// <summary>
        ///     Abstract implementation of setting the status of a Workflow.
        ///     Used by derived classes for simplicity and to allow parent class
        ///     to handle further processing required.
        /// </summary>
        /// <param name="button">The button representing the current workflow</param>
        /// <param name="status">The new status to set</param>
        protected void SetButtonStatus(Button button, WorkflowStatus status)
        {
            var label = button.Q<Label>(StatusLabelId);
            SetWorkflowStatus(label, status);
        }

        /// <summary>
        ///     Sets the status of a workflow using the possible values of the WorkflowStatus
        ///     enum
        /// </summary>
        /// <param name="label">The label representing the status of the workflow</param>
        /// <param name="status">The new status to set</param>
        private void SetWorkflowStatus(Label label, WorkflowStatus status)
        {
            // Remove any existing status style selectors applied to the label
            ResetStatusLabel(label);

            switch (status)
            {
                case WorkflowStatus.Complete:
                    label.AddToClassList(UIHelper.ClassTaskStatusComplete);
                    label.text = UIHelper.StatusComplete;
                    break;
                case WorkflowStatus.InProgress:
                    label.AddToClassList(UIHelper.ClassTaskStatusInProgress);
                    label.text = UIHelper.StatusInProgress;
                    break;
                case WorkflowStatus.Available:
                    label.AddToClassList(UIHelper.ClassTaskStatusAvailable);
                    label.text = UIHelper.StatusAvailable;
                    break;
                case WorkflowStatus.Unavailable:
                default:
                    label.AddToClassList(UIHelper.ClassTaskStatusUnavailable);
                    label.text = UIHelper.StatusNotAvailable;
                    break;
            }
        }

        /// <summary>
        ///     Removes any existing style selectors from a label
        /// </summary>
        /// <param name="label">The label representing the status of the workflow</param>
        private void ResetStatusLabel(Label label)
        {
            label.RemoveFromClassList(UIHelper.ClassTaskStatusUnavailable);
            label.RemoveFromClassList(UIHelper.ClassTaskStatusAvailable);
            label.RemoveFromClassList(UIHelper.ClassTaskStatusComplete);
            label.RemoveFromClassList(UIHelper.ClassTaskStatusInProgress);
        }


        /// <summary>
        ///     Configures navigation events for the "Back" button, allowing users
        ///     to navigate back, if applicable <see cref="WorkflowSelectionMenuController.HasBackwardsNavigation" />
        /// </summary>
        private void ConfigureBackNavigation()
        {
            // Back button events
            var backButton = Root.Q<Button>(UIHelper.BackButtonId);

            backButton.clicked += () =>
            {
                BackButtonPressed?.Invoke();
                Display(false);
            };
        }
    }
}