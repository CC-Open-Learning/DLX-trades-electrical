using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     This class is a MonoBehaviour that manages the UI and behaviour of the Run Supply Cable Menu in our
    ///     simulation.
    /// </summary>
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class RunSupplyCableMenuController : WorkflowSelectionMenuController
    {
        public UnityEvent QuizCompleted;

        [JsonProperty] private bool isCableQuizCompleted;
        [JsonProperty] private bool isTerminateQuizCompleted;

        public TerminateCablesQuizMenuController TerminateCablesQuizMenuController;

        // Serialized Fields
        [Header("Total Task Numbers")] public int SupplyCableToBoxesTotalTasks;
        public int TerminateCableTotalTasks;

        [Header("Run Supply Cable Events")] public UnityEvent ConnectCablesButtonPressed = new();

        public UnityEvent ShowCableQuiz = new();

        public UnityEvent ShowTerminateQuiz = new();

        public UnityEvent TerminateCablesButtonPressed = new();

        public UnityEvent<bool> EnableBoxColliders = new();

        // Quiz Flags
        /// <summary>
        ///     Tracks whether the cable quiz has been completed.
        ///     When set to true, swaps the functionality of the "Connect Cables to Box" button
        /// </summary>
        public bool IsCableQuizCompleted
        {
            set
            {
                if (value)
                {
                    UIHelper.SwapButtonFunctionality(root: Root, buttonName: ConnectCablesToBoxName,
                        previous: HandleShowCableQuizEvent,
                        current: HandleConnectCableButtonPressedEvent);
                }
                isCableQuizCompleted = value;
                QuizCompleted?.Invoke();
            }
            get => isCableQuizCompleted;
        }

        /// <summary>
        ///     Tracks whether the terminate quiz has been completed.
        ///     When set to true, swaps the functionality of the "Terminate Cables in Box" button
        /// </summary>
        public bool IsTerminateQuizCompleted
        {
            set
            {
                if (value)
                {
                    UIHelper.SwapButtonFunctionality(root: Root, buttonName: TerminateCablesInBoxName,
                        previous: HandleShowTerminateQuizEvent,
                        current: HandleTerminateCablesButtonPressedEvent);
                }
                isTerminateQuizCompleted = value;
                QuizCompleted?.Invoke();
            }
            get => isTerminateQuizCompleted;
        }

        // UI Elements
        private Button terminateCablesBtn;
        private Button connectCablesBtn;

        // Identifiers
        private const string ConnectCablesToBoxName = "BtnConnectCablesToBoxes";
        private const string TerminateCablesInBoxName = "BtnTerminateCables";

        protected override void ConfigureWorkflowButtons()
        {
            connectCablesBtn = Root.Q<Button>(ConnectCablesToBoxName);
            connectCablesBtn.clicked += HandleShowCableQuizEvent;
            SetConnectCablesBtnState(true);
            UpdateSupplyToBoxesCompleteTasksNum();

            terminateCablesBtn = Root.Q<Button>(TerminateCablesInBoxName);
            terminateCablesBtn.clicked += HandleShowTerminateQuizEvent;
            SetTerminateCablesBtnState(false);
            UpdateTerminateCompleteTasksNum();
        }

        /// <summary>
        ///     Updates the task completion status label for the Supply Cables to Boxes button.
        ///     This method is invoked by <see cref="TaskHandler.OnCableMounted" />
        /// </summary>
        /// <param name="tasksCompleted">The number of completed tasks.</param>
        public void UpdateSupplyToBoxesCompleteTasksNum(int tasksCompleted = 0)
        {
            SetButtonCompletedTaskCount(connectCablesBtn, tasksCompleted, SupplyCableToBoxesTotalTasks);
        }

        /// <summary>
        ///     Updates the task completion status label for the Terminate Cables in Box button.
        /// </summary>
        /// <param name="tasksCompleted">The number of completed tasks.</param>
        public void UpdateTerminateCompleteTasksNum(int tasksCompleted = 0)
        {
            SetButtonCompletedTaskCount(terminateCablesBtn, tasksCompleted, TerminateCableTotalTasks);
        }

        // Workflow Setup

        public void EnableConnectCablesWorkflow()
        {
            // We will only update the button state if it is currently in the 'NotAvailable' state
            if (GetButtonStatus(connectCablesBtn) != WorkflowStatus.Unavailable)
            {
                return;
            }

            SetConnectCablesBtnState(true);
            SetConnectCablesBtnStatus(WorkflowStatus.Available);
        }


        public void EnableTerminateCablesWorkflow()
        {
            // We will only update the button state if it is currently in the 'NotAvailable' state
            if (GetButtonStatus(terminateCablesBtn) != WorkflowStatus.Unavailable)
            {
                return;
            }

            SetTerminateCablesBtnState(true);
            SetTerminateCablesBtnStatus(WorkflowStatus.Available);
        }


        // Set Button States

        public void SetConnectCablesBtnState(bool state)
        {
            connectCablesBtn.SetEnabled(state);
        }

        public void SetTerminateCablesBtnState(bool state)
        {
            terminateCablesBtn.SetEnabled(state);
        }

        // Set Button Status

        public void SetConnectCablesBtnStatus(WorkflowStatus status)
        {
            SetButtonStatus(connectCablesBtn, status);
        }

        public void SetTerminateCablesBtnStatus(WorkflowStatus status)
        {
            SetButtonStatus(terminateCablesBtn, status);
        }

        /// <summary>
        /// Enables the box mountable colliders AFTER the termination quiz is completed
        /// </summary>
        public void EnableBoxCollidersPostTermination()
        {
            if (IsTerminateQuizCompleted)
            {
                EnableBoxColliders?.Invoke(true);
            }
        }

        /// <summary>
        ///     Handles the event when the Connect Cables to Box button is pressed and the Cable Quiz is complete
        /// </summary>
        private void HandleConnectCableButtonPressedEvent()
        {
            HandleWorkflowButtonClick(connectCablesBtn, ConnectCablesButtonPressed);
        }

        /// <summary>
        ///     Handles the event when the Connect Cables to Box button is pressed and the Cable Quiz is incomplete
        /// </summary>
        private void HandleShowCableQuizEvent()
        {
            HandleWorkflowButtonClick(connectCablesBtn, ShowCableQuiz);
        }

        /// <summary>
        ///     Handles the event when the Terminate Cables in Box button is pressed and the Terminate Quiz is incomplete
        /// </summary>
        private void HandleShowTerminateQuizEvent()
        {
            HandleWorkflowButtonClick(terminateCablesBtn, ShowTerminateQuiz);
        }

        /// <summary>
        ///     Handles the event when the Terminate Cables in Box button is pressed and the Terminate Quiz is complete
        /// </summary>
        private void HandleTerminateCablesButtonPressedEvent()
        {
            HandleWorkflowButtonClick(terminateCablesBtn, TerminateCablesButtonPressed);
        }

        public void OnLoadComplete()
        {
            if (isCableQuizCompleted)
            {
                IsCableQuizCompleted = isCableQuizCompleted;
            }
            if (isTerminateQuizCompleted)
            {
                IsTerminateQuizCompleted = isTerminateQuizCompleted;
                TerminateCablesQuizMenuController.QuizFinished.Invoke();
            }
        }
    }
}