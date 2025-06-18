using UnityEditor;
using UnityEngine;

namespace VARLab.TradesElectrical
{
#if UNITY_EDITOR
    [CustomEditor(typeof(CustomMeshSplineExtrude))]
    public class CustomMeshSplineExtrudeEditor : Editor
    {
        private CustomMeshSplineExtrude component;

        private void Awake()
        {
            component = target as CustomMeshSplineExtrude;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Regenerate"))
            {
                component.RegenerateMesh();
            }

            if (GUILayout.Button("Clear"))
            {
                component.ClearMesh();
            }

            DrawDefaultInspector();
        }
    }
#endif
}
