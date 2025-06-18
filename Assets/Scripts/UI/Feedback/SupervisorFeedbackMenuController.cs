using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     The UI controller which displays task feedback in a clipboard-style panel.
    ///     
    ///     There are currently two, and soon to be three, feedback states:
    ///     
    ///     * "Supervisor" (box-mounting) feedback
    ///         The learner has completed all of the box mounting tasks, feedback is a grid with checks and crosses
    ///     * Rough-In Feedback
    ///         The learner has completed all supply cable tasks (running and terminating), 
    ///         feedback is provided in a TreeView which breaks down task and error as description or CEC code
    ///     * Final Feedback
    ///     
    /// </summary>
    public class SupervisorFeedbackMenuController : MenuController
    {
        public const string CorrectFeedbackContainerId = "ReviewFeedbackCorrectContainer";
        public const string ErrorFeedbackContainerId = "ReviewFeedbackErrorContainer";

        public const string BoxMountingFeedbackContainerId = "BoxMountingFeedback";
        public const string CECFeedbackContainerId = "CECFeedback";

        // Box Mounting Feedback element IDs
        public const string FanBoxContainerId = "FanBoxContainer";
        public const string LightBoxContainerId = "LightBoxContainer";
        public const string OutletBoxContainerId = "OutletBoxContainer";
        public const string GangBoxContainerId = "GangBoxContainer";

        private const int PrimaryMouseButton = 0;
        private const int TreeViewIdStartIndex = 1000;

        public enum FeedbackState
        {
            None,
            BoxMounting,
            RoughIn,
            Final
        }

        [Header("Context Events")]

        public UnityEvent ShowFeedbackButton = new();

        public UnityEvent ClickedOutside = new();


        [Header("Fields")]
        public FeedbackState State = FeedbackState.None;

        [Range(250, 2000)]
        [Tooltip("Defines scroll speed when using the mouse wheel, though value seems relatively arbitrary. Changes during PlayMode are not reflected")]
        public int ScrollWheelSensitivity = 500;

        private int treeViewIdCounter;


        public List<BoxMountingFeedback> BoxMountingFeedbackCollection = new();

        private SortedDictionary<Task, List<RoughInFeedbackInformation>> roughInFeedbackCollection = new();


        // Visual Elements
        private VisualElement boxMountingFeedbackContainer;

        private VisualElement cecFeedbackContainer;

        /// <summary> Holds the feedback status message to display when there are incorrect answers </summary>
        private VisualElement feedbackErrorContainer;

        /// <summary> Holds the feedback status message to display when all answers are correct </summary>
        private VisualElement feedbackCorrectContainer;

        private TreeView treeView;

        // Properties

        public bool IsMouseOver { get; protected set; }

        public bool IsBoxMountingCorrect { get; set; }

        public bool IsRoughInCorrect { get; set; }
        
        public bool IsFinalCorrect { get; set; }

        
        /// <summary>
        ///     This property considers the current state when indicating whether all tasks 
        ///     that the learner has access to are correct
        /// </summary>
        /// <remarks>
        ///     The associated boolean properties are set by the <see cref="TaskHandler"/>
        ///     as tasks are updated
        /// </remarks>
        public bool IsCurrentTaskCorrect =>
            State == FeedbackState.None ||
            (State == FeedbackState.BoxMounting && IsBoxMountingCorrect) ||
            (State == FeedbackState.RoughIn && IsRoughInCorrect) ||
            (State == FeedbackState.Final && IsFinalCorrect);

        public override void Open()
        {
            base.Open();

            SetFeedbackStatus();
        }

        public override void Close()
        {
            base.Close();

            if (!IsCurrentTaskCorrect) 
            { 
                ShowFeedbackButton?.Invoke();
            }
        }

        public void Update()
        {
            CheckOutOfBoundsClick();
        }

        public override void Initialize()
        {
            Root = GetComponent<UIDocument>().rootVisualElement;

            // Sets up the click target for closing the window when clicking outside the screen
            Root.RegisterCallback<MouseOverEvent>(data => { IsMouseOver = true; }, TrickleDown.TrickleDown);
            Root.RegisterCallback<MouseOutEvent>(data => { IsMouseOver = false; }, TrickleDown.TrickleDown);


            feedbackErrorContainer = Root.Q<VisualElement>(ErrorFeedbackContainerId);
            feedbackCorrectContainer = Root.Q<VisualElement>(CorrectFeedbackContainerId);

            boxMountingFeedbackContainer = Root.Q<VisualElement>(BoxMountingFeedbackContainerId);
            cecFeedbackContainer = Root.Q<VisualElement>(CECFeedbackContainerId);
            treeView = cecFeedbackContainer.Q<TreeView>();

            // Box Mounting feedback setup
            BoxMountingFeedbackCollection.Add(new(Root.Q<VisualElement>(FanBoxContainerId), Task.MountFanBox));
            BoxMountingFeedbackCollection.Add(new(Root.Q<VisualElement>(LightBoxContainerId), Task.MountLightBox));
            BoxMountingFeedbackCollection.Add(new(Root.Q<VisualElement>(OutletBoxContainerId), Task.MountOutletBox));
            BoxMountingFeedbackCollection.Add(new(Root.Q<VisualElement>(GangBoxContainerId), Task.MountGangBox));

            // Set all box mounting feedback values to 'false' on start
            foreach (var feedback in BoxMountingFeedbackCollection)
            {
                feedback.Update(false, false);
            }

            // Rough-In Feedback setup
            ConfigureTreeView(treeView);

            // Sets the initial Feedback status based on the serialized State value
            SetFeedbackStatus();
        }

        /// <summary>
        ///     Uses the (legacy) Input system to check if a mouse click has occurred
        ///     outside the bounds of the feedback window while it is open.
        ///     If so, the window is closed
        /// </summary>
        private void CheckOutOfBoundsClick()
        {
            if (Input.GetMouseButtonDown(PrimaryMouseButton) && IsOpen && !IsMouseOver)
            {
                Display(false);
                ClickedOutside?.Invoke();
            }
        }

        private TreeViewItemData<string> GetTaskWithViolations(Task task, List<RoughInFeedbackInformation> violations)
        {
            string description = task.ToDescription();
            
            // Remove prefixes from task descriptions
            if (description.StartsWith("Supply Cable"))
            {
                description = description.Replace("Supply Cable to ", "");
            }
            else if (description.StartsWith("Terminate"))
            {
                description = description.Replace("Terminate ", "");
            }
            
            // add red color tags to the task description
            description = $"<color=red>{description}</color>";
            
            return new((int)task, description, GetViolations(violations));
        }

        private List<TreeViewItemData<string>> GetViolations(List<RoughInFeedbackInformation> violations)
        {
            var data = new List<TreeViewItemData<string>>();

            foreach (var violation in violations)
            {
                if (!string.IsNullOrEmpty(violation.CodeViolation))
                {
                    data.Add(new(treeViewIdCounter++, violation.CodeViolation));
                }

                if (!string.IsNullOrEmpty(violation.FeedbackDescription))
                {
                    data.Add(new(treeViewIdCounter++, violation.FeedbackDescription));
                }
            }

            return data;
        }

        private void ConfigureTreeView(TreeView treeView)
        {
            var items = new List<TreeViewItemData<string>>();
            treeViewIdCounter = TreeViewIdStartIndex;

            // Create lists for supply and terminate tasks
            var supplyTasks = new List<TreeViewItemData<string>>();
            var terminateTasks = new List<TreeViewItemData<string>>();
            var deviceTasks = new List<TreeViewItemData<string>>();

            // Sort existing tasks into appropriate lists
            foreach (var kvp in roughInFeedbackCollection)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    Debug.Log($"{kvp.Key}");
                    var taskData = GetTaskWithViolations(kvp.Key, kvp.Value);
                    if (kvp.Key.ToDescription().StartsWith("Supply"))
                    {
                        supplyTasks.Add(taskData);
                    }
                    else if (kvp.Key.ToString().Contains("Terminate"))
                    {
                        terminateTasks.Add(taskData);
                    }
                    else if (kvp.Key.ToDescription().StartsWith("Connect") || kvp.Key.ToDescription().StartsWith("Install"))
                    {
                        deviceTasks.Add(taskData);
                    }
                }
            }

            // Only add categories if they have tasks with violations
            if (supplyTasks.Count > 0)
            {
                items.Add(new TreeViewItemData<string>(treeViewIdCounter++, "<b>Supply Cables to Boxes</b>", supplyTasks));
            }
            if (terminateTasks.Count > 0)
            {
                items.Add(new TreeViewItemData<string>(treeViewIdCounter++, "<b>Terminate Cables in Boxes</b>", terminateTasks));
            }            
            if (deviceTasks.Count > 0)
            {
                items.Add(new TreeViewItemData<string>(treeViewIdCounter++, "<b>Select & Install Devices</b>", deviceTasks));
            }

            treeView.SetRootItems(items);

            // The "makeItem" function will be called as needed when the TreeView needs more items to render
            treeView.makeItem = () => new Label();

            // As the user scrolls through the list, the TreeView object will recycle elements created
            // by the "makeItem" and invoke the "bindItem" callback to associate the element with
            // the matching data item (specified as an index in the list)
            treeView.bindItem = (e, i) =>
            {
                var item = treeView.GetItemDataForIndex<string>(i);
                (e as Label).text = item;
            };

            treeView.selectionType = SelectionType.None;
            treeView.Rebuild();

            // Top-level items for Run Supply Cable and Terminate Cable in Boxes will start in the open position
            treeView.ExpandAll();

            // Custom Scroll View behaviour, since the ScrollView in the UXML is read-only
            ScrollView scrollView = treeView.Q<ScrollView>();
            scrollView.mouseWheelScrollSize = ScrollWheelSensitivity;
        }


        public void SetFeedback(Task task, bool box, bool location)
        {
            BoxMountingFeedbackCollection.Find((element) => element.Task == task)?.Update(box, location);
        }

        public void SetFeedback(SortedDictionary<Task, List<RoughInFeedbackInformation>> taskFeedbackMap)
        {
            roughInFeedbackCollection = taskFeedbackMap;
        }

        public void EnableBoxMountingFeedback()
        {
            State = FeedbackState.BoxMounting;
            SetFeedbackStatus();
        }

        public void EnableRoughInFeedback()
        {
            State = FeedbackState.RoughIn;
            SetFeedbackStatus();
        }

        public void EnableFinalFeedback()
        {
            State = FeedbackState.Final;
            SetFeedbackStatus();
        }

        private void SetFeedbackStatus()
        {
            if (feedbackCorrectContainer == null) { return; }
            if (feedbackErrorContainer == null) { return; }

            if (boxMountingFeedbackContainer == null) { return; }
            if (cecFeedbackContainer == null) { return; }

            boxMountingFeedbackContainer.style.display = DisplayStyle.None;
            cecFeedbackContainer.style.display = DisplayStyle.None;

            switch (State)
            {
                case FeedbackState.BoxMounting:
                    boxMountingFeedbackContainer.style.display = DisplayStyle.Flex;
                    break;
                case FeedbackState.RoughIn:
                case FeedbackState.Final:
                    cecFeedbackContainer.style.display = DisplayStyle.Flex;
                    ConfigureTreeView(treeView);
                    break;
                default:
                    Debug.LogWarning("Default case encountered - feedback not yet defined");
                    break;
            }

            // These same status containers can be used for all feedback states 
            if (IsCurrentTaskCorrect)
            {
                feedbackErrorContainer.style.display = DisplayStyle.None;
                feedbackCorrectContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                feedbackErrorContainer.style.display = DisplayStyle.Flex;
                feedbackCorrectContainer.style.display = DisplayStyle.None;
            }
        }
    }
}
