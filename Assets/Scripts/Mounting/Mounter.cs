using UnityEngine;

namespace VARLab.TradesElectrical
{
    public abstract class Mounter : MonoBehaviour
    {
        /// <summary> Indicates whether the mounter is currently in editing mode via the scene. </summary>
        public bool IsEditingViaScene { get; set; }

        public abstract void OnMountPreview(MountingTask mountingTask);
        public abstract void OnMountConfirm();
        public abstract void OnMountRedo();
        public abstract void OnLoadComplete(MountingTask mountingTask);
    }
}