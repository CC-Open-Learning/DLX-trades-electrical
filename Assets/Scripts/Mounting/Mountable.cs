using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;

namespace VARLab.TradesElectrical
{
    public class Mountable : MonoBehaviour
    {
        [JsonProperty]
        [field: SerializeField] public MountableName Name { get; private set; } = MountableName.None;
        
        [FormerlySerializedAs("feedbackScriptableObject")] 
        public FeedbackScriptableObject[] FeedbackScriptableObject;
        
        
    }
}
