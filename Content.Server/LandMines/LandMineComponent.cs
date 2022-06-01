﻿namespace Content.Server.LandMines;

[RegisterComponent]
public sealed class LandMineComponent : Component
{
    [DataField("deleteOnActivate")]
    public bool DeleteOnActivate = true;
}
