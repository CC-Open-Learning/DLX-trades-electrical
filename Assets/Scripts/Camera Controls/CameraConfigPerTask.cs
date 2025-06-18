using System;

namespace VARLab.TradesElectrical
{
    [Serializable]
    public struct CameraConfigPerTask
    {
        public Task task;
        public CameraSettings cameraSettings;

        public CameraConfigPerTask(Task task)
        {
            this.task = task;
            cameraSettings = new CameraSettings(null, 1f);
        }
    }
}
