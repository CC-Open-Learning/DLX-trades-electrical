namespace VARLab.TradesElectrical
{
    public class SupplyCableSelectionMenuController : TabbedTaskMenuController
    {
        public const string SupplyCableTaskConfirmationPrefix = "Confirm selection for ";

        protected Task defaultTab = Task.ConnectFanBoxToGangBox;

        protected Task fallbackTab = Task.ConnectFanBoxToGangBox;

        public override Task DefaultTab { get => defaultTab; set => defaultTab = value; }

        public override Task FallbackTab { get => fallbackTab; set => fallbackTab = value; }

        public override ConfirmSelectionInfo GenerateSelectionInfo(MountableName mountable, Task task)
        {
            return new()
            {
                MainLabelText = SupplyCableTaskConfirmationPrefix + mountable.ToDescription(),
                SecondaryLabelText = task.ToDescription(),
                IsPreviewing = false
            };
        }
    }
}
