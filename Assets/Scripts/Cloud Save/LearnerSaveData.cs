using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     Data object which will keep track of learner data during the DLX
    /// </summary>
    /// <remarks>
    ///     It has both cloud serialized and deserialized interfaces, whose methods
    ///     are executed on save/load, respectively
    /// </remarks>
    [CloudSaved]
    public class LearnerSaveData : MonoBehaviour, ICloudSerialized, ICloudDeserialized
    {
        public TaskHandler TaskHandler;

        public List<int> CustomData;

        /// <summary>
        ///     Handles any behaviour that needs to run before the data is saved
        /// </summary>
        public void OnSerialize() 
        {
            // TODO convert set of completed Mounting Task objects into a serializable data type
        }

        /// <summary>
        ///     Handles any behaviour that needs to run after the data is loaded
        /// </summary>
        public void OnDeserialize() 
        { 
            // TODO convert data object back into a set of completed Mounting Tasks,
            // pass to TaskHandler, and generate objects in the scene
        }

    }
}
