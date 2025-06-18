using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using VARLab.CloudSave;
using VARLab.TradesElectrical;

namespace VARLab.DeveloperTools
{

    public class TaskSkippingCommand : MonoBehaviour, ICommand
    {
        protected const string KeywordSkip = "skip";
        protected const string KeywordSkipShort = "s";
        protected const string KeywordEnable = "enable";
        protected const string KeywordEnableShort = "e";

        public enum TaskIndex
        {
            None = -1,           // For invalid task
            Boxes = 1,
            Cables = 2,
            Devices = 3,
            CircuitTesting = 4 // For circuit testing debugging purposes only
        }

        public string Name => "task";

        public string Usage => $"{Name} [ {KeywordSkip} | {KeywordEnable } ] <int task_number> | <string task_name>";

        public string Description => "Skips tasks based on the provided name or integer identifier. Options: Boxes (1), Cables (2), Devices (3), CircuitTesting (4)";

        [Header("Resources")]
        public TaskSelectionMenuController TaskMenu;
        public ExperienceSaveHandler SaveHandler;

        private const string SkipBoxesBlobName = "SkipBoxMounting";
        private const string SkipTerminationBlobName = "SkipCableTermination";
        private const string SkipDevicesBlobName = "SkipDeviceInstallation";
        private const string SkipCircuitQuizBlobName = "SkipCircuitQuiz";

        public void Start()
        {
            if (CommandInterpreter.Instance == null) { return; }
            CommandInterpreter.Instance?.Add(this);
        }
        
        /// <summary>
        ///     Main entry point for the 'Tasks' command
        /// </summary>
        /// <param name="e">
        ///     EventArgs which stores the command-line arguments and handles the response string 
        /// </param>
        /// <returns>
        ///     Boolean indicating whether the command was executed successfully.
        /// </returns>
        public bool Execute(CommandEventArgs e)
        {
            if (e.Args.Length < 3) { return false; }

            switch (e.Args[1])
            {
                case KeywordSkip:
                case KeywordSkipShort:
                    TaskIndex task = GetTask(e.Args[2..]);
                    e.Response = $"Skipping task: {task.ToString()}";
                    return SkipTasks(task);
                case KeywordEnable:
                case KeywordEnableShort:
                    return EnableTaskOption(e.Args[2..], out e.Response);
                default:
                    e.Response = this.ErrorResponse();
                    return false;
            }
        }

        /// <summary>
        /// Skip tasks using pre-recorded save file blobs. These blobs must present
        /// in the cloud.
        /// </summary>
        /// <param name="task">Task index used in the skipping command</param>
        public bool SkipTasks(TaskIndex task)
        {
            string currentBlobName = SaveHandler.Blob;
            string blobToLoad;

            switch (task)
            {
                case TaskIndex.Boxes:
                    blobToLoad = SkipBoxesBlobName;
                    break;
                case TaskIndex.Cables:
                    blobToLoad = SkipTerminationBlobName;
                    break;
                case TaskIndex.Devices:
                    blobToLoad = SkipDevicesBlobName;
                    break;
                case TaskIndex.CircuitTesting:
                    blobToLoad = SkipCircuitQuizBlobName;
                    break;
                default:
                    Debug.LogError("Trying to skip an unknown task!");
                    return false;
            }

            SaveHandler.Blob = blobToLoad;
            SaveHandler.Load();
            SaveHandler.Blob = currentBlobName;

            return true;
        }

        public bool EnableTaskOption(string[] options, out string response)
        {
            response = $"Error attempting to enable task option: {string.Join(" ", options)}.\n {this.ErrorResponse()}";

            TaskIndex taskIndex = GetTask(options);

            string fieldName;

            switch (taskIndex)
            {
                case TaskIndex.Boxes:
                    fieldName = "selectAndMountBoxesButton";
                    break;
                case TaskIndex.Cables:
                    fieldName = "runSupplyCableButton";
                    break;
                case TaskIndex.Devices:
                    fieldName = "installDevicesButton";
                    break;
                case TaskIndex.CircuitTesting:
                    fieldName = "circuitTestingButton";
                    break;
                default:
                    return false;
            }

            // Uses System.Reflection to find the private Button field and enable it
            if (typeof(TaskSelectionMenuController)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(TaskMenu) is Button button)
            {
                button.SetEnabled(true);
                response = $"Enabled task menu option: {taskIndex}";
                return true;
            }
            response += "\n\nReflection was not successful.";
            return false;
        }

        protected static TaskIndex GetTask(string[] options)
        {
            string taskInput = string.Join(" ", options).Trim().ToLower();

            return int.TryParse(taskInput, out int taskNumber)
                ? (TaskIndex)taskNumber
                : TryParseTaskName(taskInput);
        }

        protected static TaskIndex TryParseTaskName(string task)
        {
            if (task.Equals(TaskIndex.Boxes.ToString().ToLower()))
            {
                return TaskIndex.Boxes;
            }
            else if (task.Equals(TaskIndex.Cables.ToString().ToLower()))
            {
                return TaskIndex.Cables;
            }
            else if (task.Equals(TaskIndex.Devices.ToString().ToLower()))
            {
                return TaskIndex.Devices;
            }
            else if (task.Equals(TaskIndex.CircuitTesting.ToString().ToLower()))
            {
                return TaskIndex.CircuitTesting;
            }

            return TaskIndex.None;
        }
    }
}
