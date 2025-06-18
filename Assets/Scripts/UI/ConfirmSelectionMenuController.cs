using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     Handles a dialog which is used to confirm or redo a user's selection for installation 
    ///     or replacement of mountables.
    ///     <para />
    ///     
    ///     "Confirm" and "Redo" (cancel) button events can be accessed in code or through serialized fields.
    /// </summary>
    /// <remarks>
    ///     The standard UI behaviours of showing and hiding a UXML document are handled by the 
    ///     parent <see cref="MenuController"/> abstract class.
    ///     
    ///     This could be standardized even further with a <see cref="DialogController"> class which 
    ///     could also support the <see cref="TransitionDialogController"/> classes
    /// </remarks>
    public class ConfirmSelectionMenuController : MenuController
    {
        public const string ClassInfoIcon = "info-icon";
        public const string ClassInfoIconQuestionMark = ClassInfoIcon + "__question";
        public const string ClassInfoIconEye = ClassInfoIcon + "__eye";

        public const string MainLabelId = "MainLabel";
        public const string SecondaryLabelId = "SecondaryLabel";

        public const string ConfirmButtonId = UIHelper.ConfirmButtonId;
        public const string RedoButtonId = "BtnRedo";


        public const string ConfirmPreviewLabel = "Confirm";
        public const string RedoPreviewLabel = "No, thanks";
        public const string ConfirmInstallLabel = "Confirm";
        public const string RedoInstallLabel = "Redo";


        [Header("Dialog Events")]
        [Tooltip("Event invoked when the confirmation button (right) in the dialog is pressed.")]
        [FormerlySerializedAs("installConfirmButtonPressed")]
        public UnityEvent ConfirmButtonPressed = new();

        [Tooltip("Event invoked when the decline/redo button (left) in the dialog is pressed.")]
        [FormerlySerializedAs("installRedoButtonPressed")]
        public UnityEvent RedoButtonPressed = new();

        [Header("Extras")]
        [Tooltip("Event invoked when the info button on the far left of the dialog bar is pressed. Currently unused")]
        public UnityEvent InfoButtonPressed = new();


        // Private fields
        private Button confirmButton;
        private Button redoButton;

        private VisualElement infoIcon;

        private Label mainLabel;
        private Label secondaryLabel;

        private Label confirmButtonLabel;
        private Label redoButtonLabel;

        /// <summary>
        ///     Initialize element fields and register events
        /// </summary>
        public override void Initialize()
        {
            // Does not need base.Initialize() as there is no "Close" button functionality

            // Find the info icon to the left of the dialog text and subscribe to MouseDownEvent
            // as it is not a button in the UXML
            infoIcon = Root.Q<VisualElement>(className: ClassInfoIcon);
            infoIcon.RegisterCallback<MouseDownEvent>((data) => InfoButtonPressed?.Invoke());

            // Find the dialog content labels
            mainLabel = Root.Q<Label>(MainLabelId);
            secondaryLabel = Root.Q<Label>(SecondaryLabelId);

            // Find the dialog buttons and assign click events
            confirmButton = Root.Q<Button>(ConfirmButtonId);
            confirmButton.clicked += () => { ConfirmButtonPressed?.Invoke(); };

            redoButton = Root.Q<Button>(RedoButtonId);
            redoButton.clicked += () => { RedoButtonPressed?.Invoke(); };

            // Store references to the text labels inside the buttons
            confirmButtonLabel = confirmButton.Q<Label>();
            redoButtonLabel = redoButton.Q<Label>();
        }

        /// <summary>
        ///     Overloads the standard Display() function to accept <paramref name="info"/> 
        ///     about the current selection context
        /// </summary>
        /// <param name="info">
        ///     Struct containing text strings and data regarding the current selection context
        /// </param>
        public void Display(ConfirmSelectionInfo info)
        {
            HandleConfirmSelectionInfoChange(info);
            Open();
        }

        /// <summary>
        /// A method to change the information of the menu and its state (installing/previewing)
        /// 
        /// Invoked by:
        /// <see cref="TaskHandler.ConfirmInfoChanged"/>
        /// <see cref="SupplyCableSelectionMenuController.ConfirmInfoChanged"/>
        /// <see cref="BoxMountingSelectionMenuController.ConfirmInfoChanged"/>
        /// </summary>
        /// <param name="info">a struct containing all the necessary information to change the state of the menu</param>
        public void HandleConfirmSelectionInfoChange(ConfirmSelectionInfo info)
        {
            mainLabel.text = info.MainLabelText;
            secondaryLabel.text = info.SecondaryLabelText;
            ChangeButtonsContainer(info.IsPreviewing);
            ChangeMenuIcon(info.IsPreviewing);
        }


        /// <summary>
        ///     Changes the menu icon based on the state of the application, using style selectors
        /// </summary>
        /// <remarks>
        ///     An 'eye' icon is shown when previewing a mountable.
        ///     A 'question mark' icon is shown when installing a mountable.
        /// </remarks>
        /// <param name="isPreviewing">
        ///     Flag indicating if there is a mountable being previewed
        /// </param>
        private void ChangeMenuIcon(bool isPreviewing)
        {
            if (infoIcon == null) { return; }

            if (isPreviewing)
            {
                // Ensure they 'eye' icon is shown when previewing
                infoIcon.RemoveFromClassList(ClassInfoIconQuestionMark);
                infoIcon.AddToClassList(ClassInfoIconEye);
            }
            else
            {
                // Otherwise, the 'question mark' icon should be shown
                infoIcon.RemoveFromClassList(ClassInfoIconEye);
                infoIcon.AddToClassList(ClassInfoIconQuestionMark);
            }
        }

        /// <summary>
        ///     Changes the labels of the buttons in the Buttons Container based 
        ///     on the state of the application (previewing/installing).
        /// </summary>
        /// <param name="isPreviewing">a flag that indicates if there is a mountable being previewed</param>
        private void ChangeButtonsContainer(bool isPreviewing)
        {
            confirmButtonLabel.text = isPreviewing ? ConfirmPreviewLabel : ConfirmInstallLabel;
            redoButtonLabel.text = isPreviewing ? RedoPreviewLabel : RedoInstallLabel;
        }
    }
}
