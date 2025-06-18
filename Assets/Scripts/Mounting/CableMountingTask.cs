using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class CableMountingTask : MountingTask
    {
        [System.Serializable]
        public class CableMountableContainer
        {
            [FormerlySerializedAs("MountableOption")] public Mountable UnsecureCable;
            [FormerlySerializedAs("MountableUnsheathed")] public Mountable SecuredCables;
        }
        [field: SerializeField] public CableMountableContainer[] MountableOptions { get; private set; }
        
        [field: SerializeField, Header("Expected Selection")] public Mountable CorrectMountable { get; set; }

        public MountableAction Action { get; set; } = MountableAction.Replace;

        public override bool IsCorrect()
        {
            bool isCorrect = SelectedMountable == CorrectMountable;

            return isCorrect;
        }
    }
}