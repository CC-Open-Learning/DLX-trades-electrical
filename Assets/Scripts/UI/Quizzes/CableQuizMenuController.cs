namespace VARLab.TradesElectrical
{
    /// <summary>
    /// </summary>
    public class CableQuizMenuController : QuizMenuController
    {
        // Instructions
        private const string SupplyCableInstructionsText = "Select the appropriate supply cable for this project";
        private const string LengthInstructionsText = "Approximately how far past the box should you cut the cable?";

        // Initial menu information
        private const string HeaderText = "Supply Cable to Boxes";
        private const string BackButtonText = "Run Supply Cable";
        private const string Category1Text = "Select Supply Cable";
        private const string Category2Text = "Cut Cable";

        // Selection options content
        private const string OptionASupplyCableText = "14/2 NMD90 Electrical Cable";
        private const string OptionBSupplyCableText = "14/3 NMD90 Electrical Cable";
        private const string OptionCSupplyCableText = "12/2 NMD90 Electrical Cable";
        private const string OptionALengthText = "12\" or 300mm past box";
        private const string OptionBLengthText = "16\" or 400mm past box";
        private const string OptionCLengthText = "20\" or 500mm past box";

        // Toast feedback messages
        private const string OptionBCableTypeMsg =
            "14/3 NMD90 is not appropriate because it has too many conductors and is too expensive.";

        private const string OptionCCableTypeMsg = "12/2 NMD90 is not appropriate because it is used for 20A service.";

        private const string OptionACableLengthMsg =
            "12\" or 300mm is too short, it will not allow compliance with 12-510(4) and 12-3000(6)";

        private const string OptionCCableLengthMsg = "20\" or 500mm is too long, the excess cable will be wasted.";


        // This quiz only has 2 questions, so the 3rd category marker should be hidden by the parent class
        protected override int NumQuestions => 2;


        protected override void ConfigureInformationFields()
        {
            CorrectButtonKeyCategory1 = OptionAButton.viewDataKey;
            CorrectButtonKeyCategory2 = OptionBButton.viewDataKey;

            InstructionsTextCategory1 = SupplyCableInstructionsText;
            InstructionsTextCategory2 = LengthInstructionsText;

            HeaderLabelText = HeaderText;
            BackButtonLabelText = BackButtonText;
            FirstStepText = Category1Text;
            SecondStepText = Category2Text;

            OptionATextCategory1 = OptionASupplyCableText;
            OptionBTextCategory1 = OptionBSupplyCableText;
            OptionCTextCategory1 = OptionCSupplyCableText;

            OptionATextCategory2 = OptionALengthText;
            OptionBTextCategory2 = OptionBLengthText;
            OptionCTextCategory2 = OptionCLengthText;
        }

        protected override void ConfigureToastInfoMap()
        {
            // Category 1 (Supply Cable) - Add empty toast for correct answer (A)
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty
            });

            // Category 1 (Supply Cable) - Add incorrect answer messages for B and C
            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionBCableTypeMsg
            });

            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionCCableTypeMsg
            });

            // Category 2 (Cable Length) - Add incorrect answer message for A and C
            ToastInfoMap[OptionAButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionACableLengthMsg
            });

            ToastInfoMap[OptionCButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = OptionCCableLengthMsg
            });

            // Category 2 (Cable Length) - Add empty toast for correct answer (B)
            ToastInfoMap[OptionBButton.viewDataKey].Add(new ToastInformation
            {
                ToastTimeout = ToastTimeout,
                ToastFeedback = string.Empty
            });

        }
    }
}