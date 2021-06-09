using System;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class ProtolatheDatabaseComponent : SharedProtolatheDatabaseComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        /// <summary>
        ///     Invoked when the database gets updated.
        /// </summary>
        public event Action? OnDatabaseUpdated;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ProtolatheDatabaseState state) return;

            Clear();

            foreach (var ID in state.Recipes)
            {
                if(!_prototypeManager.TryIndex(ID, out LatheRecipePrototype? recipe)) continue;
                AddRecipe(recipe);
            }

            OnDatabaseUpdated?.Invoke();
        }
    }
}
