namespace VARLab.TradesElectrical
{
    public class BoxMountingSelectionMenuController : TabbedTaskMenuController
    {
        public const string MountingTaskHeaderPrefix = "Install ";

        public const string MountingTaskConfirmationPrefix = "Confirm location for ";

        protected Task defaultTab = Task.MountFanBox;

        protected Task fallbackTab = Task.MountFanBox;

        public override Task DefaultTab { get => defaultTab; set => defaultTab = value; }

        public override Task FallbackTab { get => fallbackTab; set => fallbackTab = value; }

        public override ConfirmSelectionInfo GenerateSelectionInfo(MountableName mountable, Task task)
        {
            return new()
            {
                MainLabelText = MountingTaskConfirmationPrefix + mountable.ToDescription(),
                SecondaryLabelText = MountingTaskHeaderPrefix + task.ToDescription(),
                IsPreviewing = false
            };
        }
    }
}
