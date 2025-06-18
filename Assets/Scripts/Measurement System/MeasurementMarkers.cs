using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace VARLab.TradesElectrical
{
    [Serializable]
    public struct MeasurementMarker
    {
        [Min(0f)] public float length;
        public string displayValueMetric;
        public string displayValueImperial;
        public string displayText;
    }

    public class MeasurementMarkers : MonoBehaviour
    {
        [Header("Marker Lines in World Axes")]
        [SerializeField] private MeasurementMarker downMarker;
        [SerializeField] private MeasurementMarker upMarker;
        [SerializeField] private MeasurementMarker rightMarker;
        [SerializeField] private MeasurementMarker leftMarker;
        [SerializeField] private MeasurementMarker frontMarker;
        [SerializeField] private MeasurementMarker backMarker;

        [Conditional("UNITY_EDITOR")]
        private void OnDrawGizmos()
        {
            Collider itemCollider = GetComponentInChildren<Collider>();

            if (!Mathf.Approximately(downMarker.length, 0))
            {
                DrawLineInEditor(downMarker.length, -Vector3.up, itemCollider.bounds);
            }
            if (!Mathf.Approximately(upMarker.length, 0))
            {
                DrawLineInEditor(upMarker.length, Vector3.up, itemCollider.bounds);
            }
            if (!Mathf.Approximately(frontMarker.length, 0))
            {
                DrawLineInEditor(frontMarker.length, Vector3.forward, itemCollider.bounds);
            }
            if (!Mathf.Approximately(backMarker.length, 0))
            {
                DrawLineInEditor(backMarker.length, -Vector3.forward, itemCollider.bounds);
            }
            if (!Mathf.Approximately(rightMarker.length, 0))
            {
                DrawLineInEditor(rightMarker.length, Vector3.right, itemCollider.bounds);
            }
            if (!Mathf.Approximately(leftMarker.length, 0))
            {
                DrawLineInEditor(leftMarker.length, -Vector3.right, itemCollider.bounds);
            }
        }

        public Dictionary<MeasurementMarker, Vector3> GetMarkers()
        {
            Dictionary<MeasurementMarker, Vector3> markers = new();

            if (!Mathf.Approximately(downMarker.length, 0))
            {
                markers.Add(downMarker, -Vector3.up);
            }
            if (!Mathf.Approximately(upMarker.length, 0))
            {
                markers.Add(upMarker,Vector3.up);
            }
            if (!Mathf.Approximately(frontMarker.length, 0))
            {
                markers.Add(frontMarker, Vector3.forward);
            }
            if (!Mathf.Approximately(backMarker.length, 0))
            {
                markers.Add(backMarker, -Vector3.forward);
            }
            if (!Mathf.Approximately(rightMarker.length, 0))
            {
                markers.Add(rightMarker, Vector3.right);
            }
            if (!Mathf.Approximately(leftMarker.length, 0))
            {
                markers.Add(leftMarker, -Vector3.right);
            }

            return markers;
        }

        private void DrawLineInEditor(float length, Vector3 direction, Bounds bounds)
        {
            Vector3 itemCenter = bounds.center;
            Vector3 objectHalfSize = bounds.size / 2;
            Vector3 startingPoint = itemCenter + Vector3.Scale(objectHalfSize, direction);
            Gizmos.DrawLine(startingPoint, startingPoint + (direction * length));
        }
    }
}
