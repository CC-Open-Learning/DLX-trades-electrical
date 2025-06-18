namespace VARLab.TradesElectrical
{
    public class TerminateCablesQuizMenuController : QuizMenuController
    {
        // Instructions
        private const string QuestionCat0Text = "What is the correct amount of outer sheathing to remove?";

        private const string QuestionCat1Text =
            "When securing the cable in the box, how much sheathing should be visible past the clamp?";

        // Initial menu information
        private const string WindowTitleText = "Terminate Cables In Boxes";
        private const string Category0TabText = "Unsheath Cables";
        private const string Category1TabText = "Secure Cables";
        private const string BackButtonText = "Run Supply Cables";

        // Selection options content
        private const string Cat0ButtonAText = "Remove 2-3\" or 50-75mm of cable sheathing";
        private const string Cat0ButtonBText = "Remove 4-6\" or 100-150mm of cable sheathing";
        private const string Cat0ButtonCText = "Remove 8-10\" or 200-250mm of cable sheathing ";
        private const string Cat1ButtonAText = "2\" or 50mm of sheathing visible";
        private const string Cat1ButtonBText = "1/4\" or 6mm of sheathing visible ";
        private const string Cat1ButtonCText = "No sheathing visible ";

        // Toast feedback messages
        private const string Cat0Feedback =
            "Try again! CEC rule 12-3000(6) requires 150mm free conductor, measured from the face of the box.";

        private const string Cat1ButtonAFeedback =
            "Try again! 2\" of sheathing past the clamp will make it difficult to work with these conductors in such a small space.";

        private const string Cat1ButtonCFeedback =
            "Try again! CEC rules 2-026 and 2-112 require that conductors be protected by sheathing in this situation.";

        // This quiz only has 2 questions, so the 3rd category marker should be hidden by the parent class
        protected override int NumQuestions => 2;

        protected override void ConfigureInformationFields()
        {
            CorrectButtonKeyCategory1 = OptionCButton.viewDataKey;
            CorrectButtonKeyCategory2 = OptionBButton.viewDataKey;

            InstructionsTextCategory1 = QuestionCat0Text;
            InstructionsTextCategory2 = QuestionCat1Text;

            HeaderLabelText = WindowTitleText;
            BackButtonLabelText = BackButtonText;
            FirstStepText = Category0TabText;
            SecondStepText = Category1TabText;

            OptionATextCategory1 = Cat0ButtonAText;
            OptionBTextCategory1 = Cat0ButtonBText;
            OptionCTextCategory1 = Cat0ButtonCText;

            OptionATextCategory2 = Cat1ButtonAText;
            OptionBTextCategory2 = Cat1ButtonBText;
            OptionCTextCategory2 = Cat1ButtonCText;
        }

        protected override void ConfigureToastInfoMap()
        {
            // Category 1 (Unsheath) - Add incorrect answer messages for A and B
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = Cat0Feedback
            });

            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = Cat0Feedback
            });

            // Category 1 (Unsheath) - Add empty toast for correct answer (C)
            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty
            });
            
            // Category 2 (Secure) - Add incorrect answer message for A
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = Cat1ButtonAFeedback
            });

            // Category 2 (Secure) - Add empty toast for correct answer (B)
            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty
            });

            // Category 2 (Secure) - Add incorrect answer message for C
            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = Cat1ButtonCFeedback
            });
        }
    }
}