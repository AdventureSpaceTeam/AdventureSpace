using Robust.Shared.IoC;

namespace Content.Client.Adventure.PrivateIoC;

public sealed partial class AdventurePrivateClientIoC
{
    public Action<IDependencyCollection>? PrivateRegister = null;
    public void PublicCall(IDependencyCollection collection)
    {
        if (PrivateRegister != null)
            PrivateRegister(collection);
    }
}
