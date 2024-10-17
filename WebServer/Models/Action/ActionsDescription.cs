using System;
using System.ComponentModel;

namespace WebServer.Models.Action
{
    public enum ActionsDescription
    {   // Service
        [Description("Service start")]
        ServiceStart = 0x1010,
        [Description("Service shutdown")]
        ServiceShutdown = 0x1020,


        [Description("Service initialise")]
        ServiceInitServer = 0x1510,

        [Description("Device initialise")]
        ServiceInitDevice = 0x1520,

        [Description("Powerbank initialise")]
        ServiceInitPowerBank = 0x1530,

        // User
        [Description("Register new user")]
        UserRegister = 0x2010,

        [Description("Login with password")]
        UserLogin = 0x2020,

        [Description("User login with SMS")]
        UserLoginSMS = 0x2030,

        [Description("Logout user")]
        UserLogout = 0x2040,

        [Description("User change the password")]
        UserChangePass = 0x2050,

        // Server
        [Description("connect")]
        ServerConnect = 0x110,
        [Description("disconnect")]
        ServerDisconnect = 0x120,

        // Stations

        [Description("register")]
        StationRegister = 0x210,
        [Description("connect")]
        StationConnect = 0x220,
        [Description("disconnect")]
        StationDisconnect = 0x230,
        [Description("remove from system")]
        StationRemove = 0x240,
        [Description("found the new")]
        StationFindNew = 0x250,


        [Description("take powerbank")]
        PowerBankTake = 0x310,

        [Description("insert powerbank")]
        PowerBankInsert = 0x320,

        [Description("found the new powerbank")]
        PowerBankFindNew = 0x330,



    }

}

