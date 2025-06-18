using System.Collections.Generic;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    public class CameraConfigurations : MonoBehaviour
    {
        [SerializeField] private List<CameraConfigPerTask> cameraConfigList = new();

        public Dictionary<Task, CameraSettings> CameraSettingsMap { get; private set; } = new();

        private void Awake()
        {
            foreach(CameraConfigPerTask config in cameraConfigList)
            {
                CameraSettingsMap.Add(config.task, config.cameraSettings);
            }
        }
    }
}
