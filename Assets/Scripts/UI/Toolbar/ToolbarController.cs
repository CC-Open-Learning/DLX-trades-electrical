using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class ToolbarController : MenuController
    {
        // Style selector contants
        public const string ClassButton = "toolbar-button";
        public const string ClassTaskButton = "toolbar-task-button";
        public const string ClassTaskButtonSelected = ClassTaskButton + UIHelper.ClassSelectedSuffix;


        // Class ID constants
        public const string ToolbarId = "Toolbar";
        public const string ProjectsSpecsButtonId = "BtnProjectSpecs";
        public const string TaskListButtonId = "BtnTaskList";
        public const string SettingsButtonId = "BtnSettings";
        public const string HelpButtonId = "BtnHelp";

        private bool _alertActive = false;

        [Header("Navigation Events")]
        public UnityEvent ProjectSpecsButtonPressed = new();

        public UnityEvent TaskListButtonPressed = new();

        public UnityEvent HelpButtonPressed = new();

        /// <summary> Event invoked when the "Settings" button is pressed </summary>
        public UnityEvent SettingsButtonPressed = new();


        // Private fields
        private Button taskListBtn;
        private Button projectSpecsBtn;

        private Button helpBtn;
        private Button settingsBtn;

        public override void Initialize()
        {
            // Does not need to use base.Initialize() as there is no "Close" button functionality

            projectSpecsBtn = GetTaskButton(Root, ProjectsSpecsButtonId, ProjectSpecsButtonPressed);

            taskListBtn = GetTaskButton(Root, TaskListButtonId, TaskListButtonPressed);

            settingsBtn = GetTaskButton(Root, SettingsButtonId, SettingsButtonPressed);

            helpBtn = GetTaskButton(Root, HelpButtonId, HelpButtonPressed);
        }
        
        /// <summary>
        /// 
        /// </summary>
        public void StartAlertCoroutine()
        {
            StartCoroutine(AlertFlash());
        }

        /// <summary>
        /// function to be used by external unity event to start notification. this is so it can be activated by the first inspection but not every inspection. <see cref="InspectableManager.OnInspectionChanged"/>
        /// </summary>
        public void PrimeAlert()
        {
            _alertActive = true;
        }
        
        /// <summary>
        /// function to be used by external unity event to start notification."/>
        /// </summary>
        public void PrimeAlertOFF()
        {
            _alertActive = false;
            var imgTaskIcon = taskListBtn.Q<VisualElement>("Icon");
            imgTaskIcon.RemoveFromClassList("icon-colour-transition");
        }

        /// <summary>
        /// coroutine to alternate colors on a 1 second delay. 1 second time is being used to avoid triggering epilepsy 
        /// </summary>
        /// <returns></returns>
        public IEnumerator AlertFlash()
        {
            yield return new WaitUntil(() => _alertActive == true);
            var imgTaskIcon = taskListBtn.Q<VisualElement>("Icon");
            while (_alertActive == true)
            {
                imgTaskIcon.AddToClassList("Yellow-Blink");
                imgTaskIcon.AddToClassList("icon-colour-transition");
                imgTaskIcon.RemoveFromClassList("icon");
                yield return new WaitForSeconds(1);
                imgTaskIcon.RemoveFromClassList("Yellow-Blink");
                imgTaskIcon.AddToClassList("icon");
                yield return new WaitForSeconds(1);
            }
        }

        /// <summary>
        ///     Enables all toolbar buttons and marks all as unselected (no highlights applied)
        /// </summary>
        public void Reset()
        {
            ResetFocus();
            ResetSelectedButton();
        }

        /// <summary>
        ///     Factory for the main buttons in the taskbar that open the key menus for the DLX
        /// </summary>
        /// <param name="root">Room element from which to query</param>
        /// <param name="id">Element id to query for</param>
        /// <param name="action">Callback to be executed when the button is clicked</param>
        /// <returns>
        ///     Button for the corresponding <paramref name="id"/> with the 
        ///     specified <paramref name="action"/> set on click
        /// </returns>
        private Button GetTaskButton(VisualElement root, string id, UnityEvent action)
        {
            Button button = root.Q<Button>(id);

            if (button == null)
            {
                Debug.LogError($"No Task button found for id '{id}'");
                return null;
            }

            // Ensures button is not selected on start
            SetOptionSelected(button, false);

            button.clicked += () =>
            {
                // Button is marked as selected when clicked
                SetOptionSelected(button, true);
                // A little hacky but the style option needs to be applied before the event is invoked, 
                if (_alertActive)
                {
                    PrimeAlertOFF();
                }
                action?.Invoke();
            };
            
            return button;
        }


        // This set of function calls can later be replaced with better stack-based UI management
        public void SetProjectSpecsButtonSelected(bool selected) => SetOptionSelected(projectSpecsBtn, selected);

        public void SetTaskListButtonSelected(bool selected) => SetOptionSelected(taskListBtn, selected);

        public void SetHelpButtonSelected(bool selected) => SetOptionSelected(helpBtn, selected);

        public void SetSettingsButtonSelected(bool selected) => SetOptionSelected(settingsBtn, selected);
        
        /// <summary>
        ///     Used to change the style of the task buttons in the toolbar when they are selected or deselected
        /// </summary>
        /// <param name="button"></param>
        /// <param name="selected"></param>
        private void SetOptionSelected(VisualElement button, bool selected)
        {
            if (button == null) { return; }

            if (selected)
            {
                button.AddToClassList(ClassTaskButtonSelected);
            }
            else
            {
                button.RemoveFromClassList(ClassTaskButtonSelected);
            }
        }
        
        public void ResetSelectedButton()
        {
            SetOptionSelected(taskListBtn, false);
            SetOptionSelected(projectSpecsBtn, false);
        }

        /// <summary>
        ///     Invoked by <see cref="TaskSelectionMenuController.MenuOpened"/>
        /// </summary>
        public void FocusOnTaskListBtn()
        {
            taskListBtn.SetEnabled(true);
            projectSpecsBtn.SetEnabled(true);
        }

        /// <summary>
        ///     Is this getting called by anything in the scene?
        ///     It seems unlikely, since we don't want to disable the Task List button
        ///     when Project Specs is open
        /// </summary>
        public void FocusOnProjectSpecs()
        {
            taskListBtn.SetEnabled(false);
            projectSpecsBtn.SetEnabled(true);
        }

        /// <summary>
        /// Invoked by <see cref="BoxMounter.OnMountConfirm"/>
        ///            <see cref="TaskSelectionMenuController.CloseButtonPressed"/>
        ///            <see cref="RunSupplyCableMenuController.CloseButtonPressed"/=>
        ///            <see cref="SupplyCableSelectionMenuController.RouteSelected"/> 
        ///            <see cref="SupplyCableSelectionMenuController.CloseButtonPressed"/>
        /// </summary>
        public void ResetFocus()
        {
            taskListBtn.SetEnabled(true);
            projectSpecsBtn.SetEnabled(true);
        }

        /// <summary>
        /// Invoked by<see cref="ConfirmSelectionMenuController.MenuOpened"/>
        ///           <see cref="ConfirmSelectionMenuController.MenuClosed"/>
        ///           <see cref="ConfirmSelectionMenuController.PreviewRedoButtonPressed"/>
        ///           <see cref="SupplyCableSelectionMenuController.MenuOpened"/>
        ///           <see cref="TaskHandler.PreviewingMountLocations"/>
        /// </summary>
        public void SetTaskListBtnOn(bool status)
        {
            taskListBtn.SetEnabled(status);
        }

        /// <see cref="SupplyCableSelectionMenuController.MenuOpened"/>
        public void SetProjectSpecsBtnOn(bool status)
        {
            projectSpecsBtn.SetEnabled(status);
        }
    }
}
