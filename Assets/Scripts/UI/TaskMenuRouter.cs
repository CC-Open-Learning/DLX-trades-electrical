using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     Tracks all <see cref="MenuController"/> objects related to the Task List workflow
    ///     so that it can treat the Task Selection Menu and all of its relevant sub-menus
    ///     as related.
    ///     
    ///     When opening or closing the Task List from the Toolbar, top-level Task List menu or
    ///     a relevant sub-menu is displayed, based on the current game context.
    ///     
    ///     Currently, only the main task menus (Box Mounting, Run Supply Cables, Device Selection)
    ///     are considered for context-based display. This could be expanded further to handle
    ///     the "Supply Cable Selection", "Terminate Cables in Boxes", and even "Terminate Gang Box Sub-Menu"
    ///     but for simplicity these sub-menus are not considered for auto-navigation yet.
    /// </summary>
    public class TaskMenuRouter : MonoBehaviour
    {
        [Header("Systems")]
        public TaskHandler TaskHandler;

        [Header("Menu Controllers")]
        public MenuController TaskSelectionMenu;            // Top-level task menu
        public MenuController BoxMountingSelectionMenu;     // Tabbed selection menu for boxes
        public MenuController RunSupplyCableMenu;           // Task selection menu for cables
        public MenuController SupplyCableSelectionMenu;     // Tabbed selection menu for running cables
        public MenuController TerminateCablesMenu;          // Selection menu for terminating cables
        public MenuController TerminateGangBoxSubMenu;      // Sub-menu specifically for gang box cables
        public MenuController DeviceSelectionMenu;          // Tabbed selection menu for devices

        [Header("Quiz Controllers")]
        public MenuController RunCablesQuizMenu;
        public MenuController TerminateCablesQuizMenu;
        public MenuController CircuitTestingQuizMenu;


        // These dialogs are important for events but are not included in the 
        // 'taskMenuElements' array as they should only ever be shown once per session
        [Header("Dialogs")]
        public MenuController SceneTransitionDialog;        
        public MenuController FinalSceneTransitionDialog;

        [Header("Events")]
        public UnityEvent Opened;
        public UnityEvent Closed;
        
        // Private fields
        private HashSet<MenuController> taskMenuElements = null;


        // Properties
        public MenuController MenuContext { get; private set; }

        /// <summary>
        ///     Uses LINQ to determine if any of the Task List menus are currently displayed
        /// </summary>
        public bool IsTaskMenuDisplayed => taskMenuElements.Any(menu => menu.IsOpen);


        /// <remarks>
        ///     During testing, it was uncovered that there is a potential race condition in
        ///     this code if this Start() callback is executed before the Start() callbacks
        ///     of the various MenuControllers referenced (particularly the SceneTransitionDialog).
        ///     
        ///     Due to the order of Start() methods in children before parents, this will likely 
        ///     not be an issue, though the potential for issue is noted here.
        /// </remarks>
        protected void Start()
        {
            InitializeMenuCollection();
            InitializeEvents();
        }

        // UI Controls
        public void InitializeMenuCollection()
        {
            // SceneTransitionDialog is specifically ignored from this list
            taskMenuElements = new()
            {
                // Adding all standard menu controllers
                TaskSelectionMenu,
                BoxMountingSelectionMenu,
                SupplyCableSelectionMenu,
                RunSupplyCableMenu,
                TerminateCablesMenu,
                TerminateGangBoxSubMenu,
                DeviceSelectionMenu,

                // Adding Quiz Menus 
                RunCablesQuizMenu,
                TerminateCablesQuizMenu,
                CircuitTestingQuizMenu
            };
        }

        /// <summary>
        ///     Defines the relevant menu context based on events triggered mainly through the
        ///     <see cref="TaskHandler"/> (as it drives the main sim workflow)
        /// </summary>
        public void InitializeEvents()
        {
            // Since Box Mounting is the first task, that menu is the default, but the
            // top-level menu should be shown until the task is "In Progress"
            MenuContext = TaskSelectionMenu;

            // Once a specific task menu is opened and a task is marked as "In Progress",
            // the "Task Menu" button will open directly to that specific menu.
            //
            // Previously, each menu had a specific event that indicated that once the
            // menu had been opened, it should be used as the current context. 
            //
            // This loop now handles that iteratively instead of explicitly.
            foreach (var menu in taskMenuElements)
            {
                menu.Opened?.AddListener(() => MenuContext = menu);
            }

            // This /could/ be extended to the Terminate Cables in Gang Box sub-menu
            // but they are not serialized. Additionally, this workflow feels better
            // and the Gang Boxes sub-menu can be directly accessed by clicking on
            // the gang box in the scene.



            // Task Completion Event Tracking

            // Once Box Mounting tasks are completed correctly, the menu context
            // is reset. The top-level menu should be shown until the next
            // task (Run Supply Cables) is "In Progress"
            TaskHandler.BoxMountingTasksCorrect?.AddListener(() => MenuContext = TaskSelectionMenu);

            // Once all "Supply Cables to Boxes" tasks have been completed,
            // the menu context can be moved up to the Run Supply Cables parent menu.
            // This will make it easier to correct mistakes in supervisor feedback.
            TaskHandler.SupplyCableMountingTasksCompleted?.AddListener(() => MenuContext = RunSupplyCableMenu);

            // Once all "Terminate Cables in Boxes" tasks have been completed,
            // the menu context can be moved up to the Run Supply Cables parent menu.
            // This will make it easier to correct mistakes in supervisor feedback.
            TaskHandler.TerminateCableTasksCompleted?.AddListener(() => MenuContext = RunSupplyCableMenu);

            // Once all Supply Cable tasks are completed, the Scene Transition Dialog should be shown
            // when attempting to open the Task List
            TaskHandler.SupplyCablesTaskCorrect?.AddListener(() => MenuContext = SceneTransitionDialog);

            // After the scene transitions, the Device Selection menu is the default task menu,
            // but the top-level menu should be shown until the task is "In Progress"
            SceneTransitionDialog.Closed?.AddListener(() => MenuContext = TaskSelectionMenu);

            // Once Device Installation tasks are completed correctly, the "Final" scene transition
            // dialog should be shown when opening the task list. The Circuit Resistance Testing quiz has
            // been completed by this point
            TaskHandler.DeviceInstallTasksCorrect?.AddListener(() => MenuContext = FinalSceneTransitionDialog);

            // After the scene transitions, the Task Selection menu is the default, even though there
            // are no more tasks to complete
            FinalSceneTransitionDialog.Closed?.AddListener(() => MenuContext = TaskSelectionMenu);
        }

        public void ToggleRelevantTaskMenu()
        {
            // Closes any/all menus if there is a menu shown currently
            if (IsTaskMenuDisplayed)
            {
                CloseAllTaskMenus();
                return;
            }

            OpenRelevantTaskMenu();
        }

        public void OpenRelevantTaskMenu()
        {
            // Ensures two task menus are not opened at the same time due to change of context.
            if (IsTaskMenuDisplayed) { return; }

            MenuContext.Open();

            Opened?.Invoke();
        }

        public void CloseAllTaskMenus()
        {
            if (taskMenuElements == null) { return; }

            // Only need to call the Close() method if the menu is actually open
            foreach (var menu in taskMenuElements.Where(menu => menu.IsOpen))
            {
                menu.Close();
            }

            Closed?.Invoke();
        }

        public void ResetMenuContext()
        {
            MenuContext = TaskSelectionMenu;
        }
    }
}