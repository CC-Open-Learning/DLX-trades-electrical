using UnityEngine;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     UI controller for the Settings menu, handling settings for gameplay and graphics
    /// </summary>
    public class SettingsMenuController : MenuController
    {
        public const int QualityLevelLowIndex = 0;
        public const int QualityLevelHighIndex = 1;

        // Element identifiers
        public const string ButtonMeasurementUnitsId = "BtnMeasurementUnits";
        public const string SliderSensitivityId = "SliderMouseSensitivity";
        public const string QualityHighButtonId = "BtnQualityHigh";
        public const string QualityLowButtonId = "BtnQualityLow";
        public const string SliderBrightnessId = "SliderBrightness";

        // Class identifiers
        public const string ButtonSelectedClass = "settings-button__selected";

        // Labels
        public const string LabelImperial = "Imperial (inches)";
        public const string LabelMetric = "Metric (mm)";


        [Header("GameObject References")]
        public MeasurementSystem MeasurementSystem;
        public CameraPovHandler CameraPovHandler;

        [Header("Value Configurations")]
        public float MinimumBrightness = 0.5f;
        public float MaximumBrightness = 2.0f;
        public float MinMouseSensitivity = 0.1f;
        public float MaxMouseSensitivity = 4.0f;


        // UI element fields
        private Button measurementUnitsButton;
        private Slider mouseSensitivitySlider;
        private Button lowQualityButton;
        private Button highQualityButton;
        private Slider brightnessSlider;

        public override void Initialize()
        {
            base.Initialize();

            // Measurement Units toggles the values displayed through the Measurements System
            measurementUnitsButton = Root.Q<Button>(ButtonMeasurementUnitsId);
            measurementUnitsButton.clicked += UpdateUnits;

            // Mouse sensitivity slider controls camera panning speed
            mouseSensitivitySlider = Root.Q<Slider>(SliderSensitivityId);
            mouseSensitivitySlider.lowValue = MinMouseSensitivity;
            mouseSensitivitySlider.highValue = MaxMouseSensitivity;
            mouseSensitivitySlider.RegisterValueChangedCallback((data) => UpdateMouseSensitivity(data.newValue));
            mouseSensitivitySlider.SetEnabled(true);

            // The Quality buttons allow users to toggle between Low and High quality
            lowQualityButton = Root.Q<Button>(QualityLowButtonId);
            lowQualityButton.clicked += () => UpdateQualityLevel(high: false);

            highQualityButton = Root.Q<Button>(QualityHighButtonId);
            highQualityButton.clicked += () => UpdateQualityLevel(high: true);

            
            // The Brightness slider manipulates the environment lighting 
            brightnessSlider = Root.Q<Slider>(SliderBrightnessId);
            brightnessSlider.lowValue = MinimumBrightness;
            brightnessSlider.highValue = MaximumBrightness;
            brightnessSlider.RegisterValueChangedCallback((data) => UpdateBrightness(data.newValue));
        }

        /// <summary>
        ///     When the window is opened, the various UI refresh methods are called
        ///     to ensure that the UI matches the state of any settings that may have
        ///     been manipulated through other methods
        /// </summary>
        public override void Open()
        {
            UpdateUnitsLabel();
            UpdateQualityLevelButtons();
            UpdateBrightnessSliderValue();
            UpdateMouseSensitivitySliderValue();

            base.Open();
        }

        /// <summary>
        ///     Toggles the units displayed through the <see cref="MeasurementSystem"/> 
        ///     to be imperial or metric
        /// </summary>
        private void UpdateUnits()
        {
            MeasurementSystem.ToggleUnits();
            UpdateUnitsLabel();
        }

        /// <summary>
        ///     Trades Electrical has been configured to use two quality settings - Low and High
        ///     This function exposes the ability to toggle between these two quality settings from
        ///     the Settings menu
        /// </summary>
        /// <param name="high">
        ///     Indicates whether High quality settings should be used, otherwise the Low setting is used
        /// </param>
        private void UpdateQualityLevel(bool high)
        {
            if (QualitySettings.count != 2)
            {
                Debug.LogWarning("Quality settings for Trades Electrical ");
                return;
            }

            QualitySettings.SetQualityLevel(high ? QualityLevelHighIndex : QualityLevelLowIndex);
            UpdateQualityLevelButtons();
        }

        private void UpdateBrightness(float brightness)
        {
            RenderSettings.ambientIntensity = brightness;
        }

        private void UpdateMouseSensitivity(float sensitivity)
        {
            if (CameraPovHandler)
            {
                CameraPovHandler.CameraPanSensitivity = sensitivity;

                // Set this controller value when sensitivity changes.
                // Though it will not be read from for the settings menu, 
                // only CameraPovHandler will be used
                if (CameraPovHandler.TaskCameraPanSettings)
                {
                    CameraPovHandler.TaskCameraPanSettings.MouseSensitivity = sensitivity;
                }

            }
        }

        // UI Refresh Methods

        /// <summary>
        ///     Updates the text in the "Measurement Units" button when changed
        /// </summary>
        private void UpdateUnitsLabel()
        {
            measurementUnitsButton.text =
                MeasurementSystem.IsImperial ? LabelImperial : LabelMetric;
        }

        /// <summary>
        ///     Updates the appearance of the Low and High Quality Settings buttons
        ///     to reflect the current quality level
        /// </summary>
        private void UpdateQualityLevelButtons()
        {
            highQualityButton.RemoveFromClassList(ButtonSelectedClass);
            lowQualityButton.RemoveFromClassList(ButtonSelectedClass);

            switch (QualitySettings.GetQualityLevel())
            {
                case QualityLevelLowIndex:
                    lowQualityButton.AddToClassList(ButtonSelectedClass);
                    break;
                case QualityLevelHighIndex:
                    highQualityButton.AddToClassList(ButtonSelectedClass);
                    break;
            }

        }

        /// <summary>
        ///     Updates the value of the brightness slider based on the value
        ///     provided by the RenderSettings library
        /// </summary>
        private void UpdateBrightnessSliderValue()
        {
            brightnessSlider.SetValueWithoutNotify(RenderSettings.ambientIntensity);
        }

        /// <summary>
        ///     Updates the value of the mouse sensitivity slider based on the value
        ///     provided by the CameraPovHandler class
        /// </summary>
        private void UpdateMouseSensitivitySliderValue()
        {
            if (CameraPovHandler)
            {
                mouseSensitivitySlider.SetValueWithoutNotify(CameraPovHandler.CameraPanSensitivity);
            }
        }
    }
}
