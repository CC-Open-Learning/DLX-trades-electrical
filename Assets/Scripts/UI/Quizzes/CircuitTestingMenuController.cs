using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using VARLab.CloudSave;

namespace VARLab.TradesElectrical
{
    /// <summary>
    /// </summary>
    [CloudSaved]
    [JsonObject(MemberSerialization.OptIn)]
    public class CircuitTestingMenuController : QuizMenuController
    {
        // Json saved field
        [JsonProperty] private bool isCircuitTestingQuizCompleted;

        [SerializeField] private CloudSaveAdapter cloudSaveAdapter;

        /// <summary>
        ///     Event invoked by the TaskHandler when this quiz should be made available to the learner
        /// </summary>
        public UnityEvent QuizEnabled = new();

        // Instructions
        private const string Cat1InstructionsText = "What is the <b>expected resistance reading</b> when the meter leads are connected \nbetween the Breaker screw (Hot conductor) and the Bond bar?";
        private const string Cat2InstructionsText = "What is the <b>expected resistance reading</b> when the meter leads are connected between the Hot conductor and the Neutral conductor, when the Light and Fan Switches are in the OFF position?";
        private const string Cat3InstructionsText = "What is the <b>expected resistance reading</b> when the meter leads are connected between the Hot conductor and the Neutral conductor, when the Light and Fan Switches are in the ON position?";

        // Initial menu information
        private const string HeaderText = "Circuit Resistance Testing";
        private const string BackButtonText = "My Tasks"; // Same label as other task menus that return to the top-level task selection menu
        private const string Category1Text = "Hot to Bond";
        private const string Category2Text = "Hot to Neutral";
        private const string Category3Text = "Load Check";

        // All 3 quiz questions use the same text fields for answers
        private const string ButtonAText = "0Ω";
        private const string ButtonBText = "O/L";
        private const string ButtonCText = "150Ω";

        // Toast feedback messages 
        private const string OptionAFeedback = "0Ω indicates a dead short in the circuit.";
        private const string OptionBFeedback = "O/L indicates incomplete circuit.";
        private const string OptionCFeedback = "The meter should not be able to read resistance on an open circuit.";

        // Overriden setup functionality
        protected override void ConfigureInformationFields()
        {
            InstructionsTextCategory1 = Cat1InstructionsText;
            InstructionsTextCategory2 = Cat2InstructionsText;
            InstructionsTextCategory3 = Cat3InstructionsText;

            HeaderLabelText = HeaderText;
            BackButtonLabelText = BackButtonText;
            FirstStepText = Category1Text;
            SecondStepText = Category2Text;
            ThirdStepText = Category3Text;

            // Set the correct button keys for each category
            CorrectButtonKeyCategory1 = OptionBButton.viewDataKey;
            CorrectButtonKeyCategory2 = OptionBButton.viewDataKey;
            CorrectButtonKeyCategory3 = OptionCButton.viewDataKey;

            OptionATextCategory1 = ButtonAText;
            OptionBTextCategory1 = ButtonBText;
            OptionCTextCategory1 = ButtonCText;

            OptionATextCategory2 = ButtonAText;
            OptionBTextCategory2 = ButtonBText;
            OptionCTextCategory2 = ButtonCText;

            OptionATextCategory3 = ButtonAText;
            OptionBTextCategory3 = ButtonBText;
            OptionCTextCategory3 = ButtonCText;
        }

        protected override void ConfigureToastInfoMap()
        {
            // Category 1 Feedback
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionAFeedback,
            });
            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation    
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty    // Correct answer
            });
            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionCFeedback
            });

            // Category 2 Feedback
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionAFeedback
            });
            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty    // Correct answer
            });

            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionCFeedback
            });

            // Category 3 Feedback
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionAFeedback
            });
            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionBFeedback
            });
            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty    // Correct answer
            });
        }

        /// <summary>
        ///     Invokes the <see cref="QuizEnabled"/> event only if the quiz
        ///     has not already been completed
        /// </summary>
        public void EnableQuiz()
        {
            if (isCircuitTestingQuizCompleted) { return; }

            QuizEnabled?.Invoke();
        }

        public void UpdateCloudSaveStatus(bool status)
        {
            isCircuitTestingQuizCompleted = status;
            cloudSaveAdapter.Save();
        }

        public void OnLoadComplete()
        {
            if (isCircuitTestingQuizCompleted)
            {
                QuizFinished?.Invoke();
                DisplayAfterQuizFinished?.Invoke();
            }
        }
    }
}