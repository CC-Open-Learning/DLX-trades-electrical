using UnityEngine.UIElements;

namespace VARLab.TradesElectrical
{
    public class TransitionDialogController : DialogController
    {
        private VisualElement window;
        private VisualElement dialogWindow;

        public override void Open()
        { 
            window.AddToClassList(UIHelper.ClassSelectorVisible);
            window.RemoveFromClassList(UIHelper.ClassSelectorHidden);
            dialogWindow.style.display = DisplayStyle.Flex; 
            Opened?.Invoke();
        }

        public override void Close()
        {
            window.RemoveFromClassList(UIHelper.ClassSelectorVisible);
            window.AddToClassList(UIHelper.ClassSelectorHidden);
            dialogWindow.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }
        

        public override void Initialize()
        {
            base.Initialize();

            window = Root.Q<VisualElement>(UIHelper.WindowRootId);
            dialogWindow = Root.Q<VisualElement>(UIHelper.ContentRootId);
            window.AddToClassList(UIHelper.ClassSelectorHidden);

            if (confirmButton != null)
            {
                // Disable button after confirmation click to avoid repeat events firing
                confirmButton.clicked += () => confirmButton.SetEnabled(false);
            }
        }
    }
}
