using System;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    [Serializable,
     CreateAssetMenu(fileName = "SupplyTaskFeedback",
         menuName = "ScriptableObjects/FeedbackScriptableObject")]
    public class FeedbackScriptableObject : ScriptableObject
    {
        public string CodeViolation;
        public string FeedbackDescription;
    }
}
