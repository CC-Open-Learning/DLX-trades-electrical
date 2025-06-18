using System.ComponentModel;

namespace VARLab.TradesElectrical
{
    /// <summary>
    ///     Enumerators describing all possible mountables in the sim
    /// </summary>
    /// <remarks>
    ///     The <see cref="DescriptionAttribute"/> is used to provide a 
    ///     "friendly" text description for each mountable
    /// </remarks>
    public enum MountableName
    {
        None = -1,

        [Description("3x2x3\" Device Box")]
        DeviceBox = 0,
        [Description("2-1/2\" 2-Gang Box")]
        TwoGangBox,
        [Description("2-1/2\" 3-Gang Box")]
        ThreeGangBox,
        [Description("2-1/2\" 4-Gang Box")]
        FourGangBox,
        [Description("4 x 1-1/2\" Octagonal Box")]
        OctagonalBox,
        [Description("4-11/16\" Square Box")]
        SquareBox,
        [Description("4 x 1/4\" Round Box")]
        RoundBox,
        [Description("7-1/4\" x 7-1/2\" Exhaust Fan Box")]
        FanBox,
        [Description("3x2x1-1/2\" Device Box")]
        DeviceBoxLowDepth,
        [Description("1110 Utility Box")]
        UtilityBox,
        [Description("4 x 1-1/2\" Octagonal Box")]
        OctagonalBracketBox,
        Panel,

        [Description("Fan Box from Gang Box")]
        FanToGangA = 100,
        [Description("Fan Box from Gang Box")]
        FanToGangB,
        [Description("Fan Box from Gang Box")]
        FanToGangC,
        [Description("Gang Box from Panel")]
        GangToPanelA,
        [Description("Gang Box from Panel")]
        GangToPanelB,
        [Description("Gang Box from Panel")]
        GangToPanelC,
        [Description("Outlet Box from Gang Box")]
        OutletToGangA,
        [Description("Outlet Box from Gang Box")]
        OutletToGangB,
        [Description("Outlet Box from Gang Box")]
        OutletToGangC,
        [Description("Light Box from Gang Box")]
        LightToGangA,
        [Description("Light Box from Gang Box")]
        LightToGangB,
        [Description("Light Box from Gang Box")]
        LightToGangC,

        // The "descriptions" below will likely need to be updated at a later date for use in UI
        [Description("FanToGangASecured")]
        FanToGangASecured,
        [Description("FanToGangBSecured")]
        FanToGangBSecured,
        [Description("FanToGangCSecured")]
        FanToGangCSecured,
        [Description("GangToPanelASecured")]
        GangToPanelASecured,
        [Description("GangToPanelBSecured")]
        GangToPanelBSecured,
        [Description("GangToPanelCSecured")]
        GangToPanelCSecured,
        [Description("OutletToGangASecured")]
        OutletToGangASecured,
        [Description("OutletToGangBSecured")]
        OutletToGangBSecured,
        [Description("OutletToGangCSecured")]
        OutletToGangCSecured,
        [Description("LightToGangASecured")]
        LightToGangASecured,
        [Description("LightToGangBSecured")]
        LightToGangBSecured,
        [Description("LightToGangCSecured")]
        LightToGangCSecured,

        [Description("Bond Conductor")]
        BondWire = 200,
        [Description("Neutral Conductor")]
        NeutralWire,
        [Description("Hot Conductor")]
        HotWire,
        
        [Description("9-1/4\" x 9-3/4\" Exhaust Fan")]
        ExhaustFan14In = 250,
        [Description("20\" Stainless Ventilation Fan")]
        VentilationFan20In,
        [Description("6\" Stainless Ventilation Fan")]
        VentilationFan6In,
        FanSupply,
        
        [Description("4\" Pot Light")]
        PotLight = 260,
        [Description("Exterior Floodlight")]
        FloodLight,
        [Description("Wall Sconce")]
        WallSconce,
        [Description("12\" Flush Mount LED")]
        FlushMountLED,
        PotLightSupply,
        FlushMountLEDSupply,
        WallSconceSupply,
        FloodlightSupply,

        [Description("15A 125V TR GFCI Outlet")]
        OutletGFCI = 270,
        [Description("20A 125V TR Outlet")]
        Outlet20A,
        [Description("15A 125V TR Outlet")]
        OutletTR,
        [Description("15A 125V TR AFCI Outlet")]
        OutletAFCI,
        OutletSupply,
        OutletSupplyXFCI,

        [Description("15A 120V Single Pole Switch")]
        LightSwitchSinglePole = 280,
        [Description("15A 120V 3-Way Switch")]
        LightSwitch3Way,
        [Description("15A 120V 4-Way Switch")]
        LightSwitch4Way,
        [Description("Single Pole Timer Switch")]
        LightSwitchTimer60Minute,
        LightSwitchSinglePoleSupply,
        LightSwitch3WaySupply,
        LightSwitchTimer60MinuteSupply,
        LightSwitch4WaySupply,
        
        [Description("15A 120V Single Pole Switch")]
        FanSwitchSinglePole = 290,
        [Description("15A 120V 3-Way Switch")]
        FanSwitch3Way,
        [Description("15A 120V 4-Way Switch")]
        FanSwitch4Way,
        [Description("Single Pole Timer Switch")]
        FanSwitchTimer60Minute,
        FanSwitchSinglePoleSupply,
        FanSwitch3WaySupply,
        FanSwitchTimer60MinuteSupply,
        FanSwitch4WaySupply,

        [Description("15A 120V Single Pole Circuit Breaker")]
        Breaker15A120V = 300,
        [Description("20A 120V Single Pole Circuit Breaker")]
        Breaker20A120V,
        [Description("15A 120V AFCI Single Pole Circuit Breaker")]
        BreakerAFCI15A120V,
        [Description("30A 240V Double Pole Circuit Breaker")]
        Breaker30A240V,
    }
}
