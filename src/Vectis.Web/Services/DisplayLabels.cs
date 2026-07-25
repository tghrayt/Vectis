using Vectis.Domain;

namespace Vectis.Web.Services;

public static class DisplayLabels
{
    public static string Location(StorageLocation location)
    {
        return location switch
        {
            StorageLocation.RoomTemperature => "Temperature ambiante",
            StorageLocation.Refrigerator => "Refrigerateur",
            StorageLocation.FridgeFreezerCompartment => "Compartiment congelateur",
            StorageLocation.SeparateFreezer => "Congelateur separe",
            StorageLocation.CoolerBag => "Sac isotherme",
            StorageLocation.Other => "Autre",
            _ => location.ToString()
        };
    }

    public static string MilkStatus(MilkStatus status)
    {
        return status switch
        {
            Domain.MilkStatus.FreshlyPumped => "Fraichement tire",
            Domain.MilkStatus.Refrigerated => "Refrigere",
            Domain.MilkStatus.Frozen => "Congele",
            Domain.MilkStatus.Thawing => "En decongelation",
            Domain.MilkStatus.Thawed => "Decongele",
            Domain.MilkStatus.ReadyToFeed => "Pret a donner",
            Domain.MilkStatus.PartiallyConsumed => "Partiellement consomme",
            Domain.MilkStatus.Consumed => "Consomme",
            Domain.MilkStatus.Discarded => "Jete",
            Domain.MilkStatus.Expired => "Expire",
            _ => status.ToString()
        };
    }
}
