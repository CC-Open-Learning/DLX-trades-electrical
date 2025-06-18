using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{

    /// <summary>
    ///     UI controller for the "Welcome Screen" which displays information to the
    ///     learner in a paginated format.
    ///     
    ///     This controller supports any number of pages, as well as forward and 
    ///     back navigation functionality, though the current "Welcome Screen" 
    ///     has 3 pages and requires only forward navigation.
    /// </summary>
    public class PaginatedDialogController : TabbedMenuController
    {
        public const string NextButtonId = "NextButton";
        public const string PageLabelId = "PageLabel";

        public const string NextLabelId = "LabelNext";
        public const string FinishLabelId = "LabelGotIt";

        public int InitialTabIndex = 0;

        protected Label pageLabel;
        protected Button nextButton;
        
        protected Label nextLabel;
        protected Label finishLabel;

        protected VisualElement currentTab;

        public override void Initialize()
        {
            base.Initialize();

            pageLabel = Root.Q<Label>(PageLabelId);
            nextLabel = Root.Q<Label>(NextLabelId);
            finishLabel = Root.Q<Label>(FinishLabelId);

            // Setup button click action
            nextButton = Root.Q<Button>(NextButtonId);
            if (nextButton != null)
            {
                nextButton.clicked += () => MoveNextTab(close: true);
            }    
            
            SetActiveTab(InitialTabIndex);
        }

        public override void Open()
        {
            SetActiveTab(InitialTabIndex);

            base.Open();
        }

        /// <summary>
        ///     Advance to the previous tab in chronological order.
        ///     
        ///     If <paramref name="wrap"/> is enabled, the first tab will 
        ///     loop around to the end.
        ///     
        ///     If 'wrap' is not enabled, this function has no effect when the
        ///     first tab is already shown.
        /// </summary>
        /// <param name="wrap">
        ///     Indicates whether the first tab should wrap around to the end
        /// </param>
        protected virtual void MovePreviousTab(bool wrap = false)
        {
            if (IsFirstPage(currentTab, out int index))
            {
                SetActiveTab(TabButtons.Count - 1);
                return;
            }

            SetActiveTab(index - 1);
        }

        /// <summary>
        ///     Advance to the next tab in chronological order.
        ///     
        ///     If <paramref name="wrap"/> is enabled, the last tab will 
        ///     loop back around to the beginning.
        ///     
        ///     If <paramref name="close"/> is enabled, advancing past the last tab 
        ///     will close the dialog. Automatically disabled if 'wrap' is enabled.
        ///     
        ///     If neither 'wrap' nor 'close' are enabled, this function has no effect
        ///     when the last tab is already shown.
        /// </summary>
        /// <param name="wrap">
        ///     Indicates whether the last tab should wrap around to the beginning
        /// </param>
        /// <param name="close">
        ///     Indicates whether navigating past the last tab should close the dialog
        /// </param>
        protected virtual void MoveNextTab(bool wrap = false, bool close = false)
        {
            if (IsFinalPage(currentTab, out int index))
            {
                if (wrap)
                {
                    SetActiveTab(0);
                }
                else if (close) 
                { 
                    Close(); 
                }

                return;
            }

            SetActiveTab(index + 1);
        }

        protected virtual void SetActiveTab(int index)
        {
            if (index < 0 || index >= TabButtons.Count)
            {
                Debug.LogError($"Value {index} not a valid tab index");
                return;
            }

            SetActiveTab(TabButtons[index]);
        }

        protected override void SetActiveTab(VisualElement tab)
        {
            base.SetActiveTab(tab);

            if (!TabButtons.Contains(tab)) 
            {
                return;
            }

            currentTab = tab;

            bool final = IsFinalPage(tab, out int index);

            // Displays "X of Y" to indicate current vs total pages
            if (pageLabel != null)
            {
                pageLabel.text = $"{index + 1} of {TabButtons.Count}";
            }

            // If the user is on the final page, the "Got it!" text should be shown,
            // otherwise the "Next >" text is shown
            nextLabel.style.display = final ? DisplayStyle.None : DisplayStyle.Flex;
            finishLabel.style.display = final ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected bool IsFirstPage(VisualElement tab, out int index)
        {
            if (!TabButtons.Contains(tab))
            {
                index = -1;
                return false;
            }

            index = TabButtons.IndexOf(tab);
            return index == 0;
        }

        protected bool IsFinalPage(VisualElement tab, out int index)
        {
            if (!TabButtons.Contains(tab))
            {
                index = -1;
                return false;
            }

            index = TabButtons.IndexOf(tab);
            return (index + 1) == TabButtons.Count;
        }
    }
}
