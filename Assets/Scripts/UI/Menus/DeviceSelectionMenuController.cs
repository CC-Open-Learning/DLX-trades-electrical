namespace VARLab.TradesElectrical
{
    public class DeviceSelectionMenuController : TabbedTaskMenuController
    {
        public override Task DefaultTab { get => TrackedDefaultTab; set => TrackedDefaultTab = value; }

        public override Task FallbackTab { get => TrackedFallbackTab; set => TrackedFallbackTab = value; }

        protected Task TrackedDefaultTab = Task.InstallFan;

        protected Task TrackedFallbackTab = Task.InstallFan;

        private const string InstallTaskHeaderPrefix = "Install ";
        private const string InstallTaskConfirmationPrefix = "Confirm selection for ";

        public override ConfirmSelectionInfo GenerateSelectionInfo(MountableName mountable, Task task)
        {
            // The current implementation of the ConfirmSelectionMenuController will NOT work
            // with the addition of a third workflow (Installing Devices).
            // This is a PLACEHOLDER until that refactor is implemented.
            return new ConfirmSelectionInfo
            {
                MainLabelText = InstallTaskConfirmationPrefix + mountable.ToDescription(),
                SecondaryLabelText = InstallTaskHeaderPrefix + task.ToDescription(),
                IsPreviewing = false
            };
        }
    }
}