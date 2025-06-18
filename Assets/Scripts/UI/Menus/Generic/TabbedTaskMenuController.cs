using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class TabbedTaskMenuController : TabbedMenuController
    {
        public readonly List<Task> TaskTabs = new();


        // Events
        public UnityEvent Cancelled = new();

        [FormerlySerializedAs("<BackButtonPressed>k__BackingField")]
        public UnityEvent BackButtonPressed = new();

        [FormerlySerializedAs("<TaskSelected>k__BackingField")]
        public UnityEvent<Task> TaskSelected = new();

        [FormerlySerializedAs("<RouteSelected>k__BackingField")]
        public UnityEvent<MountableName> ItemSelected = new();

        [FormerlySerializedAs("<ConfirmInfoChanged>k__BackingField")]
        public UnityEvent<ConfirmSelectionInfo> ConfirmInfoChanged = new();



        // Fields

        /// <summary>
        ///     Indicates the default tab that should be open when the menu is first launched
        /// </summary>
        public virtual Task DefaultTab { get; set; } = Task.None;

        /// <summary>
        ///     Indicates the tab that will be used as fallback if the menu is launched with no Task or an invalid Task
        /// </summary>
        public virtual Task FallbackTab { get; set; } = Task.None;

        /// <summary> Indicates if the DefaultTab should be shown when opening the menu </summary>
        protected bool ShowDefaultTab = true;

        /// <summary> Indicates if the menu has been opened in "focus" mode, where only the initial tab is available </summary>
        protected bool FocusMode = false;

        protected VisualElement BackButtonContainer;


        /// <summary>
        ///     Invoked in the Start() callback, driven by the parent class
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            BackButtonContainer = Root.Q<VisualElement>(UIHelper.NavigationContainerId);
            
            // Navigation buttons setup
            ConfigureCloseNavigation();
            ConfigureBackNavigation();

            Display(false);
        }


        // Menu View Controls
        public override void Open()
        {
            base.Open();

            SetNavigationActive(true);

            if (ShowDefaultTab)
            {
                SetActiveTab(DefaultTab);
            }
        }

        /// <summary>
        /// Sets focus mode to false after calling base functionality to prevent issues where focus persists if they exist
        /// instead of continuing through selection workflow.
        /// </summary>
        public override void Close()
        {
            base.Close();
            FocusMode = false;
        }
        
        public virtual void Cancel()
        {
            Cancelled?.Invoke();
            Display(false);
        }

        public override void Display(bool enabled)
        {
            FocusMode = false;
            base.Display(enabled);
        }

        /// <summary>
        ///     Launches the TabbedTaskMenuController with only one tab enabled, 
        ///     as indicated by the specified <paramref name="task"/>
        /// </summary>
        /// <param name="task">Corresponds to the tab that should be shown</param>
        public virtual void Display(Task task)
        {
            if (task == Task.None)
            {
                Debug.LogWarning($"No valid task provided to {nameof(TabbedTaskMenuController)}");
                task = FallbackTab;
            }

            ShowDefaultTab = false;
            Display(true);
            SetNavigationActive(false);

            // Ensure only the tab associated with this task is enabled
            FocusMode = true;
            SetFocusedTab(task);

            // Reset ShowDefaultTab behaviour
            ShowDefaultTab = true;
        }

        // Task Option States

        /// <summary>
        ///     Set the "enabled" state for all Task Option buttons 
        ///     for the given <paramref name="task"/>
        /// </summary>
        public void SetTaskOptionsEnabled(Task task, bool value)
        {
            foreach (var option in GetTaskOptions(task))
            {
                option.SetEnabled(value);
            }
        }

        /// <summary>
        ///     Set the "enabled" state for a specific Task Option <paramref name="name"/> 
        ///     for the given <paramref name="task"/>
        /// </summary>
        public void SetTaskOptionEnabled(Task task, MountableName name, bool value)
        {
            List<VisualElement> taskBtnList = GetTaskOptions(task);
            taskBtnList.Find(button =>
                name.ToString().StartsWith(button.viewDataKey)).SetEnabled(value);
        }

        /// <summary>
        ///     Sets the "enabled" state for the Mountable <paramref name="name"/> 
        ///     of the specified <paramref name="task"/> option, while all other options
        ///     for that task are set to the inverse of the provided <paramref name="value"/>
        /// </summary>
        /// <example>
        ///     Used by the Task Handler to enable/disable sets of task options. 
        ///     When the mount option is correct but location is incorrect, all options 
        ///     will be disabled except for the element that is already selected (to continue 
        ///     moving through the UI workflow).
        ///     When the mount option is incorrect but the location is correct, all options 
        ///     will be enabled except for the mountable that was previously selected 
        ///     (since it is the incorrect option).
        /// </example>
        public void SetTaskOptionEnabledInvert(Task task, MountableName name, bool value)
        {
            SetTaskOptionsEnabled(task, !value);
            SetTaskOptionEnabled(task, name, value);
        }


        /// <summary>
        ///     Broadcasts events indicating that a MountableName element corresponding to 
        ///     the <paramref name="selection"/> element has been selected
        /// </summary>
        protected void SelectTaskOption(VisualElement selection, VisualElement container) // KNOW WHAT TASK IS HERE
        {
            if (!TryGetMountable(selection, out MountableName item)) { return; }

            if (!TryGetTask(container, out Task task))
            {
                Debug.LogError($"Unable to find task for container {container.name}");
            }

            // Change the Confirm Selection Menu information beforehand
            ConfirmSelectionInfo info = GenerateSelectionInfo(item, task);
            ConfirmInfoChanged?.Invoke(info);

            // Item/equipment option button clicked
            ItemSelected?.Invoke(item);
        }

        /// <summary>
        ///     Sets the "selected" state for the Mountable option of the
        ///     specified <paramref name="mountingTask"/>. This method updates the visual 
        ///     representation of task options by setting the selected option to a highlighted 
        ///     style while resetting all other options to the default style.
        /// </summary>
        public void SetMountableOptionSelected(MountingTask mountingTask)
        {
            VisualElement container = GetContainer(mountingTask.TaskName);
            
            // Get all sibling task options, set them to default style
            container.Query<VisualElement>(className: UIHelper.ClassTaskOption).ForEach(button =>
            {
                button.RemoveFromClassList(UIHelper.ClassTaskOptionSelected);
                button.Q<VisualElement>(className: UIHelper.ClassStatusLabel).style.display = DisplayStyle.None;
            });
            
            List<VisualElement> taskBtnList = GetTaskOptions(mountingTask.TaskName);
            VisualElement selection = taskBtnList.Find(button =>
                mountingTask.SelectedMountable.Name.ToString().StartsWith(button.viewDataKey));
            
            selection.AddToClassList(UIHelper.ClassTaskOptionSelected);
            selection.Q<VisualElement>(className: UIHelper.ClassStatusLabel).style.display = DisplayStyle.Flex;
        }

        /// <summary>
        ///     Creates a <see cref="ConfirmSelectionInfo"/> struct based on the <paramref name="task"/>
        ///     and <paramref name="mountable"/> provided
        /// </summary>
        /// <remarks>
        ///     Can be overridden in the child classes to provide custom dialog prompt text
        /// </remarks>
        public virtual ConfirmSelectionInfo GenerateSelectionInfo(MountableName mountable, Task task)
        {
            return new()
            {
                MainLabelText = mountable.ToDescription(),
                SecondaryLabelText = task.ToDescription(),
                IsPreviewing = false
            };
        }


        // UI Navigation

        protected override void ConfigureTabViewClasses()
        {
            // Ignores the base implementation in favour of this custom Task-based mapping


            // Configure tab buttons
            Root.Query<Button>(className: UIHelper.ClassTabButton).ForEach((button) =>
            {
                TabButtons.Add(button);

                // Adds the corresponding task to a list which defines the tab order based on tasks
                if (TryGetTask(button, out Task task)) { TaskTabs.Add(task); }

                button.clicked += () =>
                {
                    if (!FocusMode) { SetActiveTab(button, task); }
                };
            });

            // Configure tab containers
            Root.Query<TemplateContainer>(className: UIHelper.ClassTabContainer).ForEach(container =>
            {
                TabContainers.Add(container);
                Debug.Log($"{name} loading '{container.name}' container");

                // Each option within the container should be mapped to its parent
                container.Query<Button>(className: UIHelper.ClassTaskOption).ForEach((button) =>
                {
                    button.clicked += () =>
                    {
                        SelectTaskOption(button, container);
                        Display(false);
                    };
                });

            });

            // Disables all "Installed" status labels on start
            Root.Query<VisualElement>(className: UIHelper.ClassStatusLabel).ForEach(element => element.style.display = DisplayStyle.None);
        }

        /// <summary>
        ///     Configures navigation events for the "Close" button in the header bar,
        ///     allowing users to discard the current interaction with this UI
        /// </summary>
        protected void ConfigureCloseNavigation()
        {
            CloseButtonPressed.AddListener(() => Cancelled?.Invoke());
        }

        /// <summary>
        ///     Configures navigation events for the "Back" button, allowing users
        ///     to navigate back to the "Select Task" UI
        /// </summary>
        protected void ConfigureBackNavigation()
        {
            // Back button events
            Button backButton = Root.Q<Button>(UIHelper.BackButtonId);

            backButton.clicked += () =>
            {
                BackButtonPressed?.Invoke();
                Cancel();
            };
        }

        protected void SetNavigationActive(bool active)
        {
            if (BackButtonContainer == null) { return; }
            BackButtonContainer.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }


        // Tab Controls

        public void SetDefaultTab(Task task) => DefaultTab = task;

        /// <summary>
        ///     Set the active tab based on the provided <paramref name="tab"/> VisualElement,
        ///     if the element's <see cref="VisualElement.viewDataKey"/> metadata matches an enumerator in the <see cref="Task"/> enum
        /// </summary>
        /// <param name="tab">The button for the selected tab</param>
        protected void SetActiveTab(VisualElement tab, Task task)
        {
            // Ensures the selected tab is mapped to a Task
            if (!tab.viewDataKey.Equals(task.ToString()))
            {
                Debug.LogWarning($"{nameof(SetActiveTab)} invoked with invalid mapping: {tab.viewDataKey} - {task}");
                if (!TryGetTask(tab, out task)) { return; }
            }

            TaskSelected?.Invoke(task);

            SetActiveTab(tab);
        }


        /// <summary>
        ///     Sets the active tab content based on a specified <see cref="Task"/>.
        ///     The tab content must have its <see cref="VisualElement.viewDataKey"/> metadata set
        ///     according to the corresponding Task enum
        /// </summary>
        /// <param name="task"></param>
        protected void SetActiveTab(Task task)
        {
            var element = GetTab(task);

            if (element == null)
            {
                Debug.LogWarning($"Unable to find a view element that matches Task '{task}'");
                task = FallbackTab;
                element = GetTab(task);
            }

            SetActiveTab(element, task);
        }

        /// <summary>
        ///     A focused tab ensures that all other tabs are disabled
        /// </summary>
        /// <param name="task"></param>
        protected void SetFocusedTab(Task task)
        {
            if (task == Task.None)
            {
                task = FallbackTab;
            }

            SetActiveTab(task);

            TabButtons.ForEach(element =>
            {
                bool enabled = element.viewDataKey.Equals(task.ToString());
                element.SetEnabled(enabled);
            });
        }


        // Container Mapping

        public VisualElement GetContainer(Task task)
        {
            return TabContainers.FindElementByViewData(task.ToString());
        }

        // Tab Mapping


        public VisualElement GetTab(Task task)
        {
            return TabButtons.FindElementByViewData(task.ToString());
        }

        // Task Option Mapping

        /// <summary>
        ///     Gets a list of all Task Option elements for a given <paramref name="task"/>
        /// </summary>
        public List<VisualElement> GetTaskOptions(Task task)
        {
            return GetContainer(task)?.Query<VisualElement>(className: UIHelper.ClassTaskOption).ToList() ?? new();
        }

        protected static bool TryGetTask(VisualElement element, out Task task)
        {
            if (Enum.TryParse(element.viewDataKey, true, out task))
            {
#if UNITY_EDITOR
                Debug.Log($"The selection '{element.name}' with '{element.viewDataKey}' metadata can be converted into a Task");
#endif
                return true;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"No valid Task exists for '{element.name}' with metadata '{element.viewDataKey}'");
#endif
            return false;
        }

        /// <summary>
        ///     Parses the 'view data' field of a given <paramref name="element"/> 
        ///     to determine the corresponding <see cref="MountableName"/> which 
        ///     is provided as the <paramref name="result"/>.
        /// </summary>
        /// <remarks>
        ///     Using the <paramref name="verbose"/> flag indicates whether 
        ///     informational messages should be logged.
        /// </remarks>
        /// <returns>Boolean indicating whether the attempt to get MountableName data was successful</returns>
        protected static bool TryGetMountable(VisualElement element, out MountableName result)
        {
            if (Enum.TryParse(element.viewDataKey, true, out result))
            {
#if UNITY_EDITOR
                Debug.Log($"The selection '{element.name}' with '{element.viewDataKey}' metadata can be converted into a Mountable");
#endif
                return true;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"No valid Mountable exists for '{element.name}' with metadata '{element.viewDataKey}'");
#endif
            return false;
        }
    }
}
