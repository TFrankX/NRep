namespace WebServer.Models.Device
{

    public enum PowerBankChargeLevel : uint
    {
        ChargeLev0_20 = 1,
        ChargeLev20_40 = 2,
        ChargeLev40_60 = 3,
        ChargeLev60_80 = 4,
        ChargeLev80_100 = 5,
        ChargeLev100 = 6,
    }

    public enum TypeOfUse : uint
    {
        FreeTake = 1,
        FreeMultiTake = 2,
        SMSTake=3,
        OwnerAuth = 4,
        PayByCard = 5,        
    }


}