using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace VARLab.TradesElectrical
{
    public class SceneController : MonoBehaviour
    {
        [FormerlySerializedAs("HouseOriginal")]
        public GameObject HouseRoughIn;
        public GameObject HouseFinal;
        public GameObject StagedForShowHouse;

        private GameObject currentScene;

        private void Start()
        {
            currentScene = HouseRoughIn;
        }
        
        private void SetCurrentSceneActive(bool enable)
        {
            currentScene.SetActive(enable);
        }
        
        /// <summary>
        ///     Activates the Rough-In prefab to show the house with only studs
        /// </summary>
        /// <remarks>
        ///     The "Final" prefab is disabled
        /// </remarks>
        public void SetHouseRoughInActive()
        {
            SetCurrentSceneActive(false);
            currentScene = HouseRoughIn;
            HouseRoughIn.SetActive(true);
        }

        /// <summary>
        ///     Disables Rough-In and enables the "Final" prefab which shows the
        ///     studs covered in drywall
        /// </summary>
        public void SetHouseFinalActive()
        {
            SetCurrentSceneActive(false);
            currentScene = HouseFinal;
            HouseFinal.SetActive(true);
        }
        
        /// <summary>
        ///     Disables Final and enables the "Finished Staged Show" prefab which shows the
        ///     studs covered in drywall
        /// </summary>
        public void SetStagedForShowHouseActive()
        {
            SetCurrentSceneActive(false);
            currentScene = StagedForShowHouse;
            StagedForShowHouse.SetActive(true);
        }
    }
}
