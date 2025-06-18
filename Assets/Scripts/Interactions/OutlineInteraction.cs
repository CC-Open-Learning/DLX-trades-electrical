using EPOOutline;
using UnityEngine;


namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     Provides an interface between the EPOOutline package and CORE Interactions
    ///     by providing methods which can listen to mouse events and change the state of
    ///     an object with an <see cref="Outlinable"/> component
    /// </summary>
    public class OutlineInteraction : MonoBehaviour
    {
        [Header("Outliner Settings")]

        [Tooltip("A layer that is included in the Outliner layer mask")]
        public int VisibleLayer = 1;

        [Tooltip("A layer not included in the Outliner layer mask. " +
            "The Outline will be hidden when set to this layer.")]
        public int HiddenLayer = 0;


        [Header("Outlinable Settings")]
        
        // Indicates whether the outline should be rendered for the front (visible) or back (occluded)
        // portions of the mesh
        public bool OutlineFront = true;
        public bool OutlineBack = true;

        // Defines the colours used for the front (primary) and back (secondary) of the outline 
        public Color OutlinePrimary;
        public Color OutlineSecondary;


        /// <summary>
        ///     If the received <paramref name="obj"/> has an <see cref="Outlinable"/>
        ///     component, the layer of the Outline is changed so that the layer is visible
        /// </summary>
        /// <param name="obj">A relevant GameObject</param>
        public void ShowOutline(GameObject obj)
        {
            if (!obj) { return; }

            Outlinable outline = obj.GetComponentInChildren<Outlinable>();

            if (!outline) { return; }

            outline.ComplexMaskingMode = ComplexMaskingMode.MaskingMode;
            outline.RenderStyle = RenderStyle.FrontBack;
            outline.FrontParameters.Color = OutlinePrimary;
            outline.BackParameters.Color = OutlineSecondary;
            
            if (outline.OutlineTargets.Count <= 0)
            {
#if UNITY_EDITOR
                Debug.Log($"The object {obj.name} does not have any outline targets. Attempting to add one");
#endif

                if (!outline.TryAddTarget(new OutlineTarget(obj.GetComponentInChildren<Renderer>())))
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"Unable to add outline target for object {obj.name}");
#endif
                }
            }
            
            outline.FrontParameters.Enabled = OutlineFront;
            outline.BackParameters.Enabled = OutlineBack;

            outline.OutlineLayer = VisibleLayer;
        }

        /// <summary>
        ///     If the received <paramref name="obj"/> has an <see cref="Outlinable"/>
        ///     component, the layer of the Outline is changed so that the layer is hidden.
        /// </summary>
        /// <param name="obj">A relevant GameObject</param>
        public void HideOutline(GameObject obj)
        {
            if (!obj) { return; }

            Outlinable outline = obj.GetComponentInChildren<Outlinable>();

            if (!outline) { return; }

            outline.OutlineLayer = HiddenLayer;
        }
    }
}