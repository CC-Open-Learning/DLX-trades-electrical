using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VARLab.StandardUILibrary;

namespace VARLab.TradesElectrical
{
    public class MeasurementSystem : MonoBehaviour
    {
        private const float TooltipOffset = 0.15f;

        [SerializeField] private LineRenderer lineRendererRef;
        [SerializeField] private TooltipSimple tooltipRef;
        private readonly List<LineRenderer> activeLines = new();
        private readonly List<TooltipSimple> activeTooltips = new();
        private readonly Dictionary<VisualElement, Vector3> tooltipPositionMap = new();
        private Camera mainCam;
        private bool isImperial = false;
        private bool shouldTooltipsVisible = false;
        private bool isWallMounted = false;
        private Renderer objectRenderer = null;
        private Dictionary<MeasurementMarker, Vector3> currentMarkerMap = null;

        public bool IsImperial => isImperial;

        private void Start()
        {
            lineRendererRef.startWidth = 0.005f;
            lineRendererRef.endWidth = 0.005f;
            mainCam = Camera.main;
        }

        private void Update()
        {
            // This is to hide the mirrored tooltip behind the camera. To learn more about this, read
            // https://forum.unity.com/threads/help-to-understand-worldtoscreenpoint.1252557/#post-7962132
            if (objectRenderer && !objectRenderer.isVisible)
            {
                foreach (var tooltip in activeTooltips)
                {
                    tooltip.Hide();
                }
                return;
            }

            if (shouldTooltipsVisible)
            {
                UpdateTooltipPositions();
            }
        }

        /// <summary>
        /// Invoked by:
        /// <see cref="BoxMounter.showMeasurements"/>
        /// <see cref="EditMountedObject.showMeasurements"/>
        /// </summary>
        /// <param name="markers">Measurement markers to display</param>
        /// <param name="objectCollider">Collider of the mountable object</param>
        public void OnDisplayMeasurements(MeasurementMarkers markers, Collider objectCollider)
        {
            shouldTooltipsVisible = true;
            isWallMounted = ParallelVectors(Vector3.up, markers.transform.up);
            objectRenderer = objectCollider.GetComponentInChildren<Renderer>();

            Vector3 objectCenter = objectCollider.bounds.center;
            // Half-sized bounding box of the object collider
            Vector3 objectHalfSize = objectCollider.bounds.size / 2;

            currentMarkerMap = markers.GetMarkers();
            foreach (var kvp in currentMarkerMap)
            {
                LineRenderer line = Instantiate(lineRendererRef, transform);
                line.name = "Measurement Marker Line";
                line.gameObject.SetActive(true);

                Vector3 direction = kvp.Value;
                MeasurementMarker marker = kvp.Key;
                float distance = marker.length;

                Vector3 startingPoint = objectCenter + Vector3.Scale(objectHalfSize, direction);
                line.SetPosition(0, startingPoint);
                line.SetPosition(1, startingPoint + direction * distance);
                activeLines.Add(line);

                TooltipSimple newTooltip = Instantiate(tooltipRef, transform);
                newTooltip.name = "Measurement Tooltip";
                VisualElement tooltipRoot = newTooltip.GetComponent<UIDocument>().rootVisualElement;
                Vector3 tooltipPos = startingPoint + direction * TooltipOffset;
                tooltipPositionMap.Add(tooltipRoot, tooltipPos);
                activeTooltips.Add(newTooltip);
                UpdateTooltipText(newTooltip, marker, isImperial);
            }
        }

        /// <summary>
        /// Invoked:
        /// <see cref="ConfirmSelectionMenuController.InstallConfirmButtonPressed"/>
        /// <see cref="ConfirmSelectionMenuController.InstallRedoButtonPressed"/>
        /// <see cref="EditMountedObject.hideEditDialog"/>
        /// </summary>
        public void OnHideMarkers()
        {
            foreach(LineRenderer line in activeLines)
            {
                Destroy(line.gameObject);
            }
            activeLines.Clear();

            foreach (var tooltip in activeTooltips)
            {
                Destroy(tooltip.gameObject);
            }
            activeTooltips.Clear();

            shouldTooltipsVisible = false;
            tooltipPositionMap.Clear();
            isWallMounted = false;
            objectRenderer = null;
            currentMarkerMap = null;
        }

        public void ToggleUnits()
        {
            isImperial = !isImperial;

            if (activeTooltips.Count == 0) { return; }
            int i = 0;
            foreach (MeasurementMarker marker in currentMarkerMap.Keys)
            {
                UpdateTooltipText(activeTooltips[i++], marker, isImperial);
            }
        }

        private void UpdateTooltipText(TooltipSimple tooltip, MeasurementMarker marker, bool isUnitImperial)
        {
            bool isDisplayValuesEmpty = string.IsNullOrEmpty(marker.displayValueMetric) &&
                                        string.IsNullOrEmpty(marker.displayValueImperial);
            string overridenValue = isUnitImperial ? marker.displayValueImperial : marker.displayValueMetric;
            string displayValue = isDisplayValuesEmpty ? marker.length.ToString() : overridenValue;
            tooltip.SetLabel($"{displayValue} {marker.displayText}");
        }

        /// <summary>
        /// Update the position and alignment (anchor point) of each tooltip
        /// </summary>
        private void UpdateTooltipPositions()
        {
            // TODO: Adjust margins according to the camera distance and zoom
            int i = 0;
            foreach (var kvp in tooltipPositionMap)
            {
                VisualElement tooltipRoot = kvp.Key;
                VisualElement tooltipBody = tooltipRoot.Q("Tooltip");
                Vector2 tooltipSize = tooltipRoot.panel.visualTree.layout.size;

                Vector3 tooltipPos = kvp.Value;
                Vector3 screenPos = mainCam.WorldToScreenPoint(tooltipPos);
                Vector2 tooltipScreenPos = TooltipHelper.ConvertToEditorCoordinates(
                    new Rect(screenPos.x, screenPos.y, 0.0f, 0.0f), tooltipSize, Screen.width, Screen.height);

                Vector3 lineStartPoint = activeLines[i].GetPosition(0);
                Vector3 startPoint = mainCam.WorldToScreenPoint(lineStartPoint);
                Vector2 posStart = TooltipHelper.ConvertToEditorCoordinates(
                        new Rect(startPoint.x, startPoint.y, 0.0f, 0.0f), tooltipSize, Screen.width, Screen.height);

                Vector3 lineEndPoint = activeLines[i].GetPosition(1);
                Vector3 endPoint = mainCam.WorldToScreenPoint(lineEndPoint);
                Vector2 posEnd = TooltipHelper.ConvertToEditorCoordinates(
                        new Rect(endPoint.x, endPoint.y, 0.0f, 0.0f), tooltipSize, Screen.width, Screen.height);

                // Set anchor points according to the horizontal position on screen
                tooltipBody.style.translate = posStart.x > posEnd.x ?
                    // end <--------- start (Anchor point at right)
                    new Translate(Length.Percent(-100), Length.Percent(-50)) :
                    // start ---------> end (Anchor point at left)
                    new Translate(0, Length.Percent(-50));

                if (isWallMounted)
                {
                    var lineDirection = (lineStartPoint - lineEndPoint).normalized;
                    tooltipBody.style.translate =
                        // Check if the tooltip is on Y (up/down) axis
                        Mathf.Approximately(Mathf.Abs(Vector3.Dot(lineDirection, Vector3.up)), 1f) ?
                        // Anchor point at center
                        new Translate(Length.Percent(-50), Length.Percent(-50)) :
                        // Anchor point at a bit below bottom
                        new Translate(tooltipBody.style.translate.value.x, Length.Percent(-110));
                }

                tooltipRoot.transform.position = tooltipScreenPos;
                activeTooltips[i].Show();

                i++;
            }
        }

        private static bool ParallelVectors(Vector3 vecA, Vector3 vecB)
        {
            return Mathf.Approximately(Vector3.Dot(vecA, vecB), 1.0f);
        }
    }
}
