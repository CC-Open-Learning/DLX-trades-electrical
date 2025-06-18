using System;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    [Serializable]
    public struct CameraSettings
    {
        public Transform lookAtTarget;
        [Min(0f), Tooltip("Low scale = high zoom")] public float zoomScale;

        public CameraSettings(Transform lookAtTarget, float zoomScale)
        {
            this.lookAtTarget = lookAtTarget;
            this.zoomScale = zoomScale;
        }
    }
}
