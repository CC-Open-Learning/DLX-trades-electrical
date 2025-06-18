using System;
using System.Collections.Generic;
using UnityEngine;
using VARLab.CloudSave;
using VARLab.DeveloperTools;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     Debug command for accessing the CloudSave system at runtime.
    /// </summary>
    /// <remarks>
    ///     The <c>cloud</c> command supports the following arguments:
    ///     <list type="bullet">
    ///         <item>list - print save file contents to the command output</item>
    ///         <item>log - print CloudSave log messages to the command output</item>
    ///         <item>save - CloudSave save action</item>
    ///         <item>load - CloudSave load action</item>
    ///         <item>delete - CloudSave delete action</item>
    ///     </list>
    /// </remarks>
    public class CloudSaveCommand : MonoBehaviour, ICommand
    {
        public const string ListKey = "list";
        public const string LogKey = "log";
        public const string SaveKey = "save";
        public const string LoadKey = "load";
        public const string DeleteKey = "delete";

        [SerializeField] protected ExperienceSaveHandler saveHandler;

        protected List<string> logs = new();

        public string Name => "cloud";

        public string Usage => $"{Name} [ {ListKey} | {LogKey} | {SaveKey} | {LoadKey} | {DeleteKey} ]";

        public string Description => $"Provides debug access to the Cloud Save system. " +
            $"Arguments perform the corresponding action. " +
            $"\n\n\tNo arguments or `{ListKey}` serializes and prints the save data in memory " +
            $"(NOTE: this causes the save data to be serialized, so any `OnSerialize` actions will be performed). ";

        public void Start()
        {
            if (!saveHandler)
            {
                Debug.Log("No SaveHandler provided for CloudSave dev command.");
                return;
            }

            if (CommandInterpreter.Instance == null) { return; }

            // Adds a CloudSave dev command to the interpreter, which is usable only if
            // the app is built in a development environment (or in Unity Editor)
            CommandInterpreter.Instance?.Add(this);

            // Add listeners to log Cloud Save action attempts and their results
            saveHandler.OnSaveComplete.AddListener(result => LogResult(result, SaveKey));
            saveHandler.OnLoadComplete.AddListener(result => LogResult(result, LoadKey));
            saveHandler.OnDeleteComplete.AddListener(result => LogResult(result, DeleteKey));

        }

        public bool Execute(CommandEventArgs args)
        {
            if (!saveHandler) { return false; }


            if (args.Args.Length > 3)
            {
                args.Response = this.ErrorResponse();
                return false;
            }

            if (args.Args.Length == 2)
            {
                switch (args.Args[1])
                {
                    case ListKey:
                        // Fall out of 'switch' statement and list contents below
                        break;
                    case LogKey:
                        args.Response = string.Join("\n\n", logs);
                        return true;
                    case SaveKey:
                        saveHandler.Save();
                        args.Response = GetAttemptMessage(SaveKey);
                        return true;
                    case LoadKey:
                        saveHandler.Load();
                        args.Response = GetAttemptMessage(LoadKey);
                        return true;
                    case DeleteKey:
                        saveHandler.Delete();
                        args.Response = GetAttemptMessage(DeleteKey);
                        return true;
                    default:
                        args.Response = this.ErrorResponse();
                        return false;
                }
            }

            if (args.Args.Length == 3)
            {
                switch (args.Args[1])
                {
                    case SaveKey:
                        SaveBlobWithName(args.Args[2]);
                        args.Response = GetAttemptMessage(SaveKey);
                        return true;
                    case LoadKey:
                        LoadBlobWithName(args.Args[2]);
                        args.Response = GetAttemptMessage(LoadKey);
                        return true;
                    case DeleteKey:
                        DeleteBlobWithName(args.Args[2]);
                        args.Response = GetAttemptMessage(DeleteKey);
                        return true;
                    default:
                        args.Response = this.ErrorResponse();
                        return false;
                }
            }

            // No arguments provided, simply list content in memory
            string data = saveHandler.CloudSerializer.Serialize();

            // Format output for prettier viewing because the return from CloudSerializer
            // is serialized JSON of a list of serialized JSON objects, so there is a lot of cruft
            data = data.Replace(@"\n", "\n");         // Replace escaped newlines with real ones
            data = data.Replace(@"\r", string.Empty); // Clear out CR characters
            data = data.Replace(@"\""", "\"");        // Remove unecessary escape before quotes

            // Prepend file name before printing data
            args.Response = $"file: {saveHandler.Blob}\ncontainer: {saveHandler.Container}\n" + data;
            return true;
        }

        protected string GetAttemptMessage(string action) => $"'{action}' action attempted. Check logs for response.";

        protected void LogResult(bool result, string action)
        {
            logs.Add($"'{action}' action returned '{result}' at {DateTime.Now}");
        }

        protected void SaveBlobWithName(string name)
        {
            string old = saveHandler.Blob;
            saveHandler.Blob = name;
            saveHandler.Save();
            // Even though Save() calls a coroutine internally, it
            // should be safe to reset the blob name now
            saveHandler.Blob = old;
        }

        protected void LoadBlobWithName(string name)
        {
            string old = saveHandler.Blob;
            saveHandler.Blob = name;
            saveHandler.Load();
            // Even though Load() calls a coroutine internally, it
            // should be safe to reset the blob name now
            saveHandler.Blob = old;
        }

        protected void DeleteBlobWithName(string name)
        {
            string old = saveHandler.Blob;
            saveHandler.Blob = name;
            saveHandler.Delete();
            // Even though Delete() calls a coroutine internally, it
            // should be safe to reset the blob name now
            saveHandler.Blob = old;
        }
    }
}
