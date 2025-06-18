using UnityEngine;
using VARLab.DeveloperTools;

namespace VARLab.TradesElectrical
{
    public class SceneControlCommand : MonoBehaviour, ICommand
    {
        public const string KeywordReset = "reset";
        public const string KeywordStuds = "studs";
        public const string KeywordDrywall = "drywall";
        public const string KeywordFinished = "finished";
        public const string KeywordPanel = "panel";
        
        public string Name => "scene";

        public string Usage => $"{Name} [ {KeywordStuds} | {KeywordDrywall} | {KeywordPanel} | {KeywordFinished} | {KeywordReset} ]";

        public string Description => "Swap the construction site setting";

        public SceneController Controller;
        public CameraController CameraController;
        public DeviceMountingTask PanelBox;

        /// <summary>
        ///     Adds the command to the command interpreter, if it is available
        /// </summary>
        public void Start()
        {
            CommandInterpreter.Instance?.Add(this);
        }

        public bool Execute(CommandEventArgs e)
        {
            if (e.Args.Length == 2)
            {
                switch (e.Args[1])
                {
                    case KeywordStuds:
                        Controller.SetHouseRoughInActive();
                        return true;
                    case KeywordDrywall:
                        Controller.SetHouseFinalActive();
                        return true;
                    case KeywordPanel:
                        CameraController.PreviewedPanelBox(PanelBox);
                        return true;                   
                    case KeywordFinished:
                        Controller.SetStagedForShowHouseActive();
                        return true;
                    case KeywordReset:
                        CameraController.SwitchDefaultCameraWithFade();
                        return true;
                }
            }

            e.Response = this.ErrorResponse();
            return false;
        }
    }
}
