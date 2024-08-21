namespace Content.Shared.AdventureSpace.FastUI;

public sealed class SecretListingEUISelectedEvent : CancellableEntityEventArgs
{
    public string Key;
    public ListingData Data;

    public SecretListingEUISelectedEvent(string key, ListingData data)
    {
        Key = key;
        Data = data;
    }
}
