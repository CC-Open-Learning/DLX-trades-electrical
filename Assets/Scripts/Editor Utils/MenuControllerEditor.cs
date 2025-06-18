#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     A custom inspector for the <see cref="MenuController"/>
    ///     classes and their sub-classes.
    ///     
    ///     Allows developers to manually toggle any menu from the hierarchy 
    /// </summary>
    [CustomEditor(typeof(MenuController), true)]
    public class MenuControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                var menu = (MenuController)target;
                string label = menu.IsOpen ? "Close" : "Open";
                if (GUILayout.Button(label))
                {
                    menu.Toggle();
                }
            }

            base.OnInspectorGUI();
        }
    }
}
#endif
