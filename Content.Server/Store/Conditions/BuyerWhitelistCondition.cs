using Content.Shared.Store;
using Content.Shared.Whitelist;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Filters out an entry based on the components or tags on an entity.
/// </summary>
public sealed partial class BuyerWhitelistCondition : ListingCondition
{
    /// <summary>
    /// A whitelist of tags or components.
    /// </summary>
    [DataField("whitelist")]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist of tags or components.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;

        if (Whitelist != null)
        {
            if (!Whitelist.IsValid(args.Buyer, ent))
                return false;
        }

        if (Blacklist != null)
        {
            if (Blacklist.IsValid(args.Buyer, ent))
                return false;
        }

        return true;
    }
}
