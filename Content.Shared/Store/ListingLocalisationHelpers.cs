﻿using Robust.Shared.Prototypes;

namespace Content.Shared.Store;

public static class ListingLocalisationHelpers
{
    /// <summary>
    /// ListingData's Name field can be either a localisation string or the actual entity's name.
    /// This function gets a localised name from the localisation string if it exists, and if not, it gets the entity's name.
    /// If neither a localised string exists, or an associated entity name, it will return the value of the "Name" field.
    /// </summary>
    public static string GetLocalisedNameOrEntityName(ListingData listingData, IPrototypeManager prototypeManager)
    {
        bool wasLocalised = Loc.TryGetString(listingData.Name, out string? listingName);

        if (!wasLocalised && listingData.ProductEntity != null)
        {
            var proto = prototypeManager.Index<EntityPrototype>(listingData.ProductEntity);
            listingName = proto.Name;
        }

        return listingName ?? listingData.Name;
    }

    /// <summary>
    /// ListingData's Description field can be either a localisation string or the actual entity's description.
    /// This function gets a localised description from the localisation string if it exists, and if not, it gets the entity's description.
    /// If neither a localised string exists, or an associated entity description, it will return the value of the "Description" field.
    /// </summary>
    public static string GetLocalisedDescriptionOrEntityDescription(ListingData listingData, IPrototypeManager prototypeManager)
    {
        bool wasLocalised = Loc.TryGetString(listingData.Description, out string? listingDesc);

        if (!wasLocalised && listingData.ProductEntity != null)
        {
            var proto = prototypeManager.Index<EntityPrototype>(listingData.ProductEntity);
            listingDesc = proto.Description;
        }

        return listingDesc ?? listingData.Description;
    }
}
