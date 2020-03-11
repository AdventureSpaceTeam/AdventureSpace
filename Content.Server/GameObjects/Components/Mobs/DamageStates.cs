﻿using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Defines the blocking effect of each damage state, and what effects to apply upon entering or exiting the state
    /// </summary>
    public interface IDamageState : IActionBlocker
    {
        void EnterState(IEntity entity);

        void ExitState(IEntity entity);

        bool IsConscious { get; }
    }

    /// <summary>
    /// Standard state that a species is at with no damage or negative effect
    /// </summary>
    public struct NormalState : IDamageState
    {
        public void EnterState(IEntity entity)
        {
        }

        public void ExitState(IEntity entity)
        {
        }

        public bool IsConscious => true;

        bool IActionBlocker.CanInteract()
        {
            return true;
        }

        bool IActionBlocker.CanMove()
        {
            return true;
        }

        bool IActionBlocker.CanUse()
        {
            return true;
        }

        bool IActionBlocker.CanThrow()
        {
            return true;
        }

        bool IActionBlocker.CanSpeak()
        {
            return true;
        }

        bool IActionBlocker.CanDrop()
        {
            return true;
        }

        bool IActionBlocker.CanPickup()
        {
            return true;
        }

        bool IActionBlocker.CanEmote()
        {
            return true;
        }

        bool IActionBlocker.CanAttack()
        {
            return true;
        }
    }

    /// <summary>
    /// A state in which you are disabled from acting due to damage
    /// </summary>
    public struct CriticalState : IDamageState
    {
        public void EnterState(IEntity entity)
        {
        }

        public void ExitState(IEntity entity)
        {
        }

        public bool IsConscious => false;

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }

        bool IActionBlocker.CanThrow()
        {
            return false;
        }

        bool IActionBlocker.CanSpeak()
        {
            return false;
        }

        bool IActionBlocker.CanDrop()
        {
            return false;
        }

        bool IActionBlocker.CanPickup()
        {
            return false;
        }

        bool IActionBlocker.CanEmote()
        {
            return false;
        }

        bool IActionBlocker.CanAttack()
        {
            return false;
        }
    }

    /// <summary>
    /// A damage state which will allow ghosting out of mobs
    /// </summary>
    public struct DeadState : IDamageState
    {
        public void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                var newState = SharedSpeciesComponent.MobState.Down;
                appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, newState);
            }

            if (entity.TryGetComponent(out CollidableComponent collidable))
            {
                collidable.CollisionEnabled = false;
            }
        }

        public void ExitState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                var newState = SharedSpeciesComponent.MobState.Stand;
                appearance.SetData(SharedSpeciesComponent.MobVisuals.RotationState, newState);
            }

            if (entity.TryGetComponent(out CollidableComponent collidable))
            {
                collidable.CollisionEnabled = true;
            }
        }

        public bool IsConscious => false;

        bool IActionBlocker.CanInteract()
        {
            return false;
        }

        bool IActionBlocker.CanMove()
        {
            return false;
        }

        bool IActionBlocker.CanUse()
        {
            return false;
        }

        bool IActionBlocker.CanThrow()
        {
            return false;
        }

        bool IActionBlocker.CanSpeak()
        {
            return false;
        }

        bool IActionBlocker.CanDrop()
        {
            return false;
        }

        bool IActionBlocker.CanPickup()
        {
            return false;
        }

        bool IActionBlocker.CanEmote()
        {
            return false;
        }

        bool IActionBlocker.CanAttack()
        {
            return false;
        }
    }
}
