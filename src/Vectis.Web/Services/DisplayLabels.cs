using Vectis.Domain;

namespace Vectis.Web.Services;

public static class DisplayLabels
{
    public static string ContainerType(ContainerType type)
    {
        return type switch
        {
            Domain.ContainerType.StorageBag => "Sachet de conservation",
            Domain.ContainerType.Bottle => "Biberon",
            Domain.ContainerType.Jar => "Pot",
            Domain.ContainerType.Other => "Autre",
            _ => type.ToString()
        };
    }

    public static string UserRole(UserRole role)
    {
        return role switch
        {
            Domain.UserRole.Admin => "Administrateur",
            Domain.UserRole.Caregiver => "Accompagnant",
            _ => role.ToString()
        };
    }

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

    public static string FeedingReaction(FeedingReaction reaction)
    {
        return reaction switch
        {
            Domain.FeedingReaction.Finished => "Termine",
            Domain.FeedingReaction.Normal => "Normal",
            Domain.FeedingReaction.Slow => "Lent",
            Domain.FeedingReaction.Refused => "Refuse",
            Domain.FeedingReaction.Reflux => "Reflux",
            Domain.FeedingReaction.Vomiting => "Vomissement",
            Domain.FeedingReaction.Discomfort => "Inconfort",
            Domain.FeedingReaction.Other => "Autre",
            _ => reaction.ToString()
        };
    }

    public static string BottleStatus(string status)
    {
        return status switch
        {
            "prepared" => "Prepare",
            "consumed" => "Consomme",
            "partially_consumed" => "Partiellement consomme",
            "not_consumed" => "Non consomme",
            "expired" => "Expire",
            _ => status
        };
    }

    public static string NotificationKind(NotificationKind kind)
    {
        return kind switch
        {
            Domain.NotificationKind.StockLow => "Stock faible",
            Domain.NotificationKind.ExpiringSoon => "Lait bientot expire",
            Domain.NotificationKind.PreparedBottleAging => "Biberon en attente",
            _ => kind.ToString()
        };
    }
}
