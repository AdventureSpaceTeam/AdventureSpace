﻿using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Interfaces
{
    /// <summary>
    /// Chemical reaction effect on the world such as an explosion, EMP, or fire.
    /// </summary>
    public interface IReactionEffect : IExposeData
    {
        void React(IEntity solutionEntity, decimal intensity);
    }
}
