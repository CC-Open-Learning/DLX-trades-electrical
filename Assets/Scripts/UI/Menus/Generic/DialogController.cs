using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     The DialogController is a specialized MenuController which assumes
    ///     the menu will likely contain two buttons, namely 'Confirm' and 'Cancel',
    ///     and maps these to Confirmed and Cancelled events.
    ///     
    ///     This generic behaviour provides a basis for code reusability.
    /// </summary>
    public class DialogController : MenuController
    {
        [Header("Dialog Events")]

        [FormerlySerializedAs("ConfirmButtonPressed")]
        public UnityEvent Confirmed = new();

        [FormerlySerializedAs("CloseButtonPressed")]
        public UnityEvent Cancelled = new();


        /// <summary>
        ///     The affirmative button in the dialog
        /// </summary>
        protected Button confirmButton;

        /// <summary>
        ///     The negative button in the dialog
        /// </summary>
        protected Button cancelButton;


        /// <summary>
        ///     Assign click events to the expected dialog buttons
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // Dialog should be cancelled if the "Close" button is pressed.
            // Parent already handles close functionality
            CloseButtonPressed.AddListener(() => Cancelled?.Invoke());

            confirmButton = Root.Q<Button>(UIHelper.ConfirmButtonId);
            if (confirmButton != null)
            {
                confirmButton.clicked += () => Confirm();
            }

            cancelButton = Root.Q<Button>(UIHelper.CancelButtonId);
            if (cancelButton != null)
            {
                cancelButton.clicked += () => Cancel();
            }
        }

        protected void Confirm()
        {
            Confirmed?.Invoke();
            Close();
        }

        protected void Cancel()
        {
            Cancelled?.Invoke();
            Close();
        }
    }
}
