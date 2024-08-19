using Content.Shared.FixedPoint;

namespace Content.Server._c4llv07e.Bridges;

//TODO BY UR
public interface IDiseasesBridge
{
    void TransferDiseasesContact(EntityUid recipient, EntityUid donor);

    public bool CanHealDisease(EntityUid entityUid, EntityUid target);

    public bool TryHealDisease(EntityUid entityUid, EntityUid target);

    public void IncreaseImmunityByVitamins(EntityUid uid, FixedPoint2 quantity);
}

public sealed class StubDiseasesBridge : IDiseasesBridge
{
    public void TransferDiseasesContact(EntityUid recipient, EntityUid donor)
    {

    }

    public bool CanHealDisease(EntityUid entityUid, EntityUid target)
    {
        return false;
    }

    public bool TryHealDisease(EntityUid entityUid, EntityUid target)
    {
        return false;
    }

    public void IncreaseImmunityByVitamins(EntityUid uid, FixedPoint2 quantity)
    {

    }
}
