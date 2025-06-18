using System.ComponentModel;

namespace VARLab.TradesElectrical
{
    public enum Task
    {
        None = -1,

        [Description("Outlet Box")]
        MountOutletBox,
        [Description("Gang Box")]
        MountGangBox,
        [Description("Light Box")]
        MountLightBox,
        [Description("Fan Box")]
        MountFanBox,

        [Description("Supply Cable to Fan Box from Gang Box")]
        ConnectFanBoxToGangBox,
        [Description("Supply Cable to Gang Box from Panel")]
        ConnectGangBoxToPanel,
        [Description("Supply Cable to Outlet Box from Gang Box")]
        ConnectOutletBoxToGangBox,
        [Description("Supply Cable to Light Box from Gang Box")]
        ConnectLightBoxToGangBox,

        [Description("Terminate Light Box")]
        TerminateLightBox,
        [Description("Terminate Outlet Box")]
        TerminateOutletBox,
        [Description("Terminate Fan Box")]
        TerminateFanBox,
        [Description("Terminate Gang Box")]
        TerminateGangBox,
        [Description("Terminate Bonds in Gang Box")]
        TerminateGangBoxBonds,
        [Description("Terminate Neutrals in Gang Box")]
        TerminateGangBoxNeutrals,
        [Description("Terminate Hots in Gang Box")]
        TerminateGangBoxHots,

        [Description("Install Fan")]
        InstallFan = 150,
        [Description("Install Light")]
        InstallLight,
        [Description("Install Outlet")]
        InstallOutlet,
        [Description("Install Light Switch")]
        InstallLightSwitch,
        [Description("Install Fan Switch")]
        InstallFanSwitch,
        [Description("Install Breaker")]
        InstallBreaker,

        [Description("Connect Fan")]
        ConnectFan = 200,
        [Description("Connect Light")]
        ConnectLight,
        [Description("Connect Outlet")]
        ConnectOutlet,
        [Description("Connect Light Switch")]
        ConnectLightSwitch,
        [Description("Connect Fan Switch")]
        ConnectFanSwitch,
        [Description("Connect Breaker")]
        ConnectBreaker,
    }
}
