#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    [RequireComponent(typeof(UIDocument))]
    public class CloudSaveStatusIndicator : MonoBehaviour
    {
        private const string ClassIconSuccess = "CloudStatusSuccess";
        private const string ClassIconError = "CloudStatusError";

        private const string MessageCloudSaveSuccess = "Server connection successful! Your progress is saved";
        private const string MessageCloudSaveError = "Network Error: Unable to save progress at the moment";

        private UIDocument document;

        private Button iconSuccess;
        private Button iconError;

        private bool lastStatusReceived = true;

        [SerializeField] private ToastMessageUI toast;

        public enum Status
        {
            Error = -1,
            None = 0,
            Upload,
            Download,
            Success
        }

        protected void Start()
        {
            document = GetComponent<UIDocument>();

            iconSuccess = document.rootVisualElement.Q<Button>(ClassIconSuccess);
            iconError = document.rootVisualElement.Q<Button>(ClassIconError);

            iconSuccess.clicked += () => DisplayToast(true);
            iconError.clicked += () => DisplayToast(false);

            ClearStatus();
        }

        public void SetStatus(Status status)
        {
            iconSuccess.style.display = DisplayStyle.None;
            iconError.style.display = DisplayStyle.None;

            switch (status)
            {
                case Status.Error:
                    iconError.style.display = DisplayStyle.Flex;
                    break;
                case Status.Success:
                    iconSuccess.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        public void ClearStatus() => SetStatus(Status.None);

        public void CloudSaveActionStarted() => SetStatus(Status.Upload);

        public void CloudSaveActionFinished(bool success)
        {
            SetStatus(success ? Status.Success : Status.Error);

            if (lastStatusReceived != success)
            {
                DisplayToast(success);
            }

            lastStatusReceived = success;
        }

        protected void DisplayToast(bool success)
        {
            if (!toast) { return; }

            ToastMessageType type = success ? ToastMessageType.Success : ToastMessageType.Error;
            string message = success ? MessageCloudSaveSuccess : MessageCloudSaveError;

            toast.Show(type, message);
        }
    }


#if UNITY_EDITOR

    /// <summary>
    ///     A custom inspector used for debugging the <see cref="CloudSaveStatusIndicator"/>
    /// </summary>
    [CustomEditor(typeof(CloudSaveStatusIndicator))]
    public class CloudSaveStatusIndicatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var obj = (CloudSaveStatusIndicator)target;
            if (GUILayout.Button("None"))
            {
                obj.SetStatus(CloudSaveStatusIndicator.Status.None);
            }
            if (GUILayout.Button("Error"))
            {
                obj.CloudSaveActionFinished(false);
            }
            if (GUILayout.Button("Success"))
            {
                obj.CloudSaveActionFinished(true);
            }
        }
    }

#endif

}
