using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class TaskSelectionMenuController : WorkflowSelectionMenuController
    {
        // Identifiers
        private const string SelectAndMountBoxesButtonId = "BtnSelectAndMountBoxes";
        private const string RunSupplyCableButtonId = "BtnRunSupplyCable";
        private const string InstallDevicesButtonId = "BtnInstallDevices";
        private const string CircuitTestingButtonId = "BtnCircuitTesting";

        // UI Elements
        private Button selectAndMountBoxesButton;
        private Button runSupplyCableButton;
        private Button installDevicesButton;
        private Button circuitTestingButton;

        // Do these need to be exposed as serialized fields? Can they be injected from another controller?
        [Description("NOTE: This is information only used during updating not initialization")]
        [Header("Total Tasks Numbers")]
        public int TotalMountingTasks;
        public int TotalSupplyCableTasks;
        public int TotalDeviceInstallationTasks;
        public int TotalCircuitTestingQuizQuestions = 3;


        // Context-specific Events

        [Header("Task Selection Menu Events")]
        public UnityEvent SelectAndMountBoxesButtonPressed = new();
        public UnityEvent RunSupplyCableButtonPressed = new();
        public UnityEvent InstallDevicesButtonPressed = new();
        public UnityEvent CircuitTestingButtonPressed = new();

        // Start is called in the parent class

        protected override void ConfigureWorkflowButtons()
        {
            selectAndMountBoxesButton = Root.Q<Button>(SelectAndMountBoxesButtonId);
            selectAndMountBoxesButton.clicked += () =>
            {
                HandleWorkflowButtonClick(selectAndMountBoxesButton, SelectAndMountBoxesButtonPressed);
            };
            
            SetSelectAndMountBtnState(true);
            SetMountingCompleteTasksNum();

            runSupplyCableButton = Root.Q<Button>(RunSupplyCableButtonId);
            runSupplyCableButton.clicked += () =>
            {
                HandleWorkflowButtonClick(runSupplyCableButton, RunSupplyCableButtonPressed);
            };
            
            SetRunSupplyCableBtnState(false);
            SetSupplyCableCompletedTasksNum();

            installDevicesButton = Root.Q<Button>(InstallDevicesButtonId);
            installDevicesButton.clicked += () =>
            {
                HandleWorkflowButtonClick(installDevicesButton, InstallDevicesButtonPressed);
            };
            
            // This value is set to true only when testing the connection between the
            // Task Selection Menu and the Device Selection Menu.
            SetInstallDevicesBtnState(false);
            SetDevicesCompletedTasksNum();

            circuitTestingButton = Root.Q<Button>(CircuitTestingButtonId);
            circuitTestingButton.clicked += () =>
            {
                HandleWorkflowButtonClick(circuitTestingButton, CircuitTestingButtonPressed);
            };
            
            SetCircuitTestingBtnState(false);
        }


        // Set Button States

        public void SetSelectAndMountBtnState(bool state)
        {
            selectAndMountBoxesButton.SetEnabled(state);
        }

        public void SetRunSupplyCableBtnState(bool state)
        {
            runSupplyCableButton.SetEnabled(state);
        }

        public void SetInstallDevicesBtnState(bool state)
        {
            installDevicesButton.SetEnabled(state);
        }

        public void SetCircuitTestingBtnState(bool state)
        {
            circuitTestingButton.SetEnabled(state);
        }

        // Button Status Checks

        public bool IsSelectAndMountBoxesBtnEnabled()
        {
            return selectAndMountBoxesButton.enabledSelf;
        }

        public bool IsSupplyCableBtnEnabled()
        {
            return runSupplyCableButton.enabledSelf;
        }

        public bool IsInstallDevicesBtnEnabled()
        {
            return installDevicesButton.enabledSelf;
        }

        public bool IsCircuitTestingBtnEnabled()
        {
            return circuitTestingButton.enabledSelf;
        }

        // Update Task Count Labels
        public void SetMountingCompleteTasksNum(int tasksCompleted = 0)
        {
            SetButtonCompletedTaskCount(selectAndMountBoxesButton, tasksCompleted, TotalMountingTasks);
        }

        public void SetSupplyCableCompletedTasksNum(int tasksCompleted = 0)
        {
            SetButtonCompletedTaskCount(runSupplyCableButton, tasksCompleted, TotalSupplyCableTasks);
        }

        public void SetDevicesCompletedTasksNum(int tasksCompleted = 0)
        {
            SetButtonCompletedTaskCount(installDevicesButton, tasksCompleted, TotalDeviceInstallationTasks);
        }

        // Update Button Status

        public void SetMountingTaskBtnStatus(WorkflowStatus status)
        {
            SetButtonStatus(selectAndMountBoxesButton, status);
        }

        public void SetSupplyTaskBtnStatus(WorkflowStatus status)
        {
            SetButtonStatus(runSupplyCableButton, status);
        }

        public void SetDeviceInstallBtnStatus(WorkflowStatus status)
        {
            SetButtonStatus(installDevicesButton, status);
        }

        public void SetCircuitTestingBtnStatus(WorkflowStatus status)
        {
            SetButtonStatus(circuitTestingButton, status);
        }

        public void ActivateCircuitTestingQuiz()
        {
            SetCircuitTestingBtnState(true);
            SetCircuitTestingBtnStatus(WorkflowStatus.Available);
        }

        public void CompleteCircuitTestingQuiz()
        {
            SetCircuitTestingBtnState(false);
            SetCircuitTestingBtnStatus(WorkflowStatus.Complete);
            SetButtonCompletedTaskCount(circuitTestingButton, 1, 1);
        }
    }
}