using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     Extends the <see cref="ExperienceSaveHandler"/> to provide 
    ///     DLX-specific save & load functionality to Trades Electrical.
    /// </summary>
    /// <remarks>
    ///     Currently just connects the CloudSave actions to listeners which
    ///     log to the console, however further CloudSave code will be added 
    ///     here to react to learner interaction and trigger automatic saving
    ///     & on-demand reloading of save files.
    /// </remarks>
    public class CloudSaveAdapter : ExperienceSaveHandler
    {
        /// <summary>
        ///     Defines the maximum number of network connection retries 
        ///     before the <see cref="CloudSaveNetworkError" /> event is invoked
        /// </summary>
        private const int MaxAttempts = 3;

        public const string TradesElectricalContainer = "trades-electrical";

        [Header("Initial Session Events")]
        public UnityEvent SaveFileNotFound = new();
        public UnityEvent SaveFileFound = new();

        [Header("Error Handling")]
        public UnityEvent CloudSaveNetworkError = new();

              [Header("Dialog Management")]
        public PaginatedDialogController PaginatedDialogControllerHandler;

        public float FailedAttemptDelaySeconds = 1f;

        public bool InternalsSyncing { private get; set; }

        private int pingAttempts = 0;

        public override void Save()
        {
            if (!InternalsSyncing)
            {
                base.Save();
            }
        }

        public void Reset()
        {
            Container = TradesElectricalContainer;
        }        public void Start()
        {
            OnListComplete.AddListener((success) => StartCoroutine(CheckForSaveFileCallback(success)));
        }

        public void UpdateFileName(string name)
        {
            Blob = name;
        }

        /// <summary>
        ///     Callback from List() function in order to retry the connection upon failure.
        ///     The connection is attempted <see cref="MaxAttempts"/> times before invoking
        ///     the <see cref="CloudSaveNetworkError"/> event
        /// </summary>
        /// <param name="success">Indicates whether the last action was successful</param>
        /// <returns></returns>
        private IEnumerator CheckForSaveFileCallback(bool success)
        {
            if (success) { yield break; }

            // If we've exceeded the max number of attempts, this function will invoke the network
            // error event and exit
            if (++pingAttempts >= MaxAttempts)
            {
                Debug.LogError($"Unable to reach Cloud Save service after {MaxAttempts} attempts");
                //CloudSaveNetworkError?.Invoke();
                pingAttempts = 0;

                // If we're now using the local save system, try to load directly
                if (SaveSystem is CookieSaveSystem)
                {
                    Debug.Log("Switched to local save system.");
                    Load();
                }

                yield break;
            }

            yield return new WaitForSeconds(FailedAttemptDelaySeconds);

            // Check which save system we're currently using
            if (SaveSystem is AzureSaveSystem)
            {
                // For cloud storage, continue with the List operation
                base.List();
            }
        }

        public void clearSaveData()
        {
            Delete();
        }

        /// <summary>
        /// Called when delete operation completes. Only resets scene for local saves.
        /// For cloud saves (Azure), no reset is needed as they work fine without it.
        /// </summary>
        /// <param name="success">Whether the delete operation was successful</param>
        public void ResetSceneForLocalSave(bool success)
        {
            // Only reset scene if delete was successful AND we're using local save
            if (success && SaveSystem is CookieSaveSystem)
            {
                Debug.Log("Local delete completed successfully. Resetting scene...");
                ResetScene();
            }
            else if (!success && SaveSystem is CookieSaveSystem)
            {
                Debug.LogWarning("Local delete operation failed.");
            }
            else if (success && SaveSystem is AzureSaveSystem)
            {
                // For cloud saves: open the paginated dialog
                PaginatedDialogController dialogController = FindObjectOfType<PaginatedDialogController>();
                if (dialogController != null)
                {
                    dialogController.Open();
                }
            }
            else if (!success && SaveSystem is AzureSaveSystem)
            {
                Debug.LogWarning("Cloud delete operation failed.");
            }
        }

        /// <summary>
        /// Resets the current scene by reloading it
        /// </summary>
        public void ResetScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        ///     Custom request handler for Trades Electrical, in order to use the List() action
        ///     to check whether the current user has a save file *BEFORE* attempting to load the file data
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public override void HandleRequestCompleted(object sender, RequestCompletedEventArgs args)
        {
            base.HandleRequestCompleted(sender, args);

            if (args == null)
            {
                return;
            }

            // Handle Load actions for local save system
            if (args.Action == RequestAction.Load && SaveSystem is CookieSaveSystem)
            {
                Debug.Log($"Local load completed. Success: {args.Success}, Data present: {args.Data != null}");

                // For local storage, we consider a successful load with data as "file found"
                // and a failed load or no data as "file not found"
                if (args.Success && args.Data != null)
                {
                    Debug.Log("Local save file found");
                    SaveFileFound?.Invoke();
                }
                else
                {
                    Debug.Log("Local save file not found");
                    SaveFileNotFound?.Invoke();
                }
                return;
            }

            // For cloud storage, we use the List operation
            if (args.Action != RequestAction.List)
            {
                return;
            }

            // Error state check
            if (!args.Success)
            {
                return;
            }

            // For cloud storage, check if our blob name is in the list
            Debug.Log($"List operation completed. Checking for blob: {Blob}");
            var tokens = args.Data.Split("\"");
            bool fileFound = tokens.Contains(Blob);

            Debug.Log(fileFound ? "Cloud save file found" : "Cloud save file not found");
            (fileFound ? SaveFileFound : SaveFileNotFound)?.Invoke();
        }
    }
}
