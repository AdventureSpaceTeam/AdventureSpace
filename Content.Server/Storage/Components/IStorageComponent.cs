using Robust.Shared.GameObjects;

namespace Content.Server.Storage.Components
{
    public interface IStorageComponent
    {
        bool Remove(EntityUid entity);
        bool Insert(EntityUid entity);
        bool CanInsert(EntityUid entity);
    }
}
