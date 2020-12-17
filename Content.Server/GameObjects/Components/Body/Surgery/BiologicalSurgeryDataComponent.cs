﻿#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using static Content.Shared.GameObjects.Components.Body.Surgery.ISurgeryData;

namespace Content.Server.GameObjects.Components.Body.Surgery
{
    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(ISurgeryData))]
    public class BiologicalSurgeryDataComponent : Component, ISurgeryData
    {
        public override string Name => "BiologicalSurgeryData";

        private readonly HashSet<IMechanism> _disconnectedOrgans = new();

        private bool SkinOpened { get; set; }

        private bool SkinRetracted { get; set; }

        private bool VesselsClamped { get; set; }

        public IBodyPart? Parent => Owner.GetComponentOrNull<IBodyPart>();

        public BodyPartType? ParentType => Parent?.PartType;

        private void AddDisconnectedOrgan(IMechanism mechanism)
        {
            if (_disconnectedOrgans.Add(mechanism))
            {
                Dirty();
            }
        }

        private void RemoveDisconnectedOrgan(IMechanism mechanism)
        {
            if (_disconnectedOrgans.Remove(mechanism))
            {
                Dirty();
            }
        }

        private async Task<bool> SurgeryDoAfter(IEntity performer)
        {
            if (!performer.HasComponent<DoAfterComponent>())
            {
                return true;
            }

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            var target = Parent?.Body?.Owner ?? Owner;
            var args = new DoAfterEventArgs(performer, 3, target: target)
            {
                BreakOnUserMove = true,
                BreakOnTargetMove = true
            };

            return await doAfterSystem.DoAfter(args) == DoAfterStatus.Finished;
        }

        private bool HasIncisionNotClamped()
        {
            return SkinOpened && !VesselsClamped;
        }

        private bool HasClampedIncisionNotRetracted()
        {
            return SkinOpened && VesselsClamped && !SkinRetracted;
        }

        private bool HasFullyOpenIncision()
        {
            return SkinOpened && VesselsClamped && SkinRetracted;
        }

        public string GetDescription()
        {
            if (Parent == null)
            {
                return string.Empty;
            }

            var toReturn = new StringBuilder();

            if (HasIncisionNotClamped())
            {
                toReturn.Append(Loc.GetString("The skin on {0:their} {1} has an incision, but it is prone to bleeding.",
                    Owner, Parent.Name));
            }
            else if (HasClampedIncisionNotRetracted())
            {
                toReturn.AppendLine(Loc.GetString("The skin on {0:their} {1} has an incision, but it is not retracted.",
                    Owner, Parent.Name));
            }
            else if (HasFullyOpenIncision())
            {
                toReturn.AppendLine(Loc.GetString("There is an incision on {0:their} {1}.\n", Owner, Parent.Name));
                foreach (var mechanism in _disconnectedOrgans)
                {
                    toReturn.AppendLine(Loc.GetString("{0:their} {1} is loose.", Owner, mechanism.Name));
                }
            }

            return toReturn.ToString();
        }

        public bool CanAddMechanism(IMechanism mechanism)
        {
            return Parent != null &&
                   SkinOpened &&
                   VesselsClamped &&
                   SkinRetracted;
        }

        public bool CanAttachBodyPart(IBodyPart part)
        {
            return Parent != null;
            // TODO BODY if a part is disconnected, you should have to do some surgery to allow another body part to be attached.
        }

        public SurgeryAction? GetSurgeryStep(SurgeryType toolType)
        {
            if (Parent == null)
            {
                return null;
            }

            if (toolType == SurgeryType.Amputation)
            {
                return RemoveBodyPartSurgery;
            }

            if (!SkinOpened)
            {
                // Case: skin is normal.
                if (toolType == SurgeryType.Incision)
                {
                    return OpenSkinSurgery;
                }
            }
            else if (!VesselsClamped)
            {
                // Case: skin is opened, but not clamped.
                switch (toolType)
                {
                    case SurgeryType.VesselCompression:
                        return ClampVesselsSurgery;
                    case SurgeryType.Cauterization:
                        return CauterizeIncisionSurgery;
                }
            }
            else if (!SkinRetracted)
            {
                // Case: skin is opened and clamped, but not retracted.
                switch (toolType)
                {
                    case SurgeryType.Retraction:
                        return RetractSkinSurgery;
                    case SurgeryType.Cauterization:
                        return CauterizeIncisionSurgery;
                }
            }
            else
            {
                // Case: skin is fully open.
                if (Parent.Mechanisms.Count > 0 &&
                    toolType == SurgeryType.VesselCompression)
                {
                    if (_disconnectedOrgans.Except(Parent.Mechanisms).Count() != 0 ||
                        Parent.Mechanisms.Except(_disconnectedOrgans).Count() != 0)
                    {
                        return LoosenOrganSurgery;
                    }
                }

                if (_disconnectedOrgans.Count > 0 && toolType == SurgeryType.Incision)
                {
                    return RemoveOrganSurgery;
                }

                if (toolType == SurgeryType.Cauterization)
                {
                    return CauterizeIncisionSurgery;
                }
            }

            return null;
        }

        public bool CheckSurgery(SurgeryType toolType)
        {
            return GetSurgeryStep(toolType) != null;
        }

        public bool PerformSurgery(SurgeryType surgeryType, IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            var step = GetSurgeryStep(surgeryType);

            if (step == null)
            {
                return false;
            }

            step(container, surgeon, performer);
            return true;
        }

        private async void OpenSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null)
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("Cut open the skin..."));

            if (await SurgeryDoAfter(performer))
            {
                SkinOpened = true;
            }
        }

        private async void ClampVesselsSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("Clamp the vessels..."));

            if (await SurgeryDoAfter(performer))
            {
                VesselsClamped = true;
            }
        }

        private async void RetractSkinSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("Retracting the skin..."));

            if (await SurgeryDoAfter(performer))
            {
                SkinRetracted = true;
            }
        }

        private async void CauterizeIncisionSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            performer.PopupMessage(Loc.GetString("Cauterizing the incision..."));

            if (await SurgeryDoAfter(performer))
            {
                SkinOpened = false;
                VesselsClamped = false;
                SkinRetracted = false;
            }
        }

        private void LoosenOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;
            if (Parent.Mechanisms.Count <= 0) return;

            var toSend = new List<IMechanism>();
            foreach (var mechanism in Parent.Mechanisms)
            {
                if (!_disconnectedOrgans.Contains(mechanism))
                {
                    toSend.Add(mechanism);
                }
            }

            if (toSend.Count > 0)
            {
                surgeon.RequestMechanism(toSend, LoosenOrganSurgeryCallback);
            }
        }

        private async void LoosenOrganSurgeryCallback(IMechanism? target, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer)
        {
            if (Parent == null || target == null || !Parent.Mechanisms.Contains(target))
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("Loosening the organ..."));

            if (!performer.HasComponent<DoAfterComponent>())
            {
                AddDisconnectedOrgan(target);
                return;
            }

            if (await SurgeryDoAfter(performer))
            {
                AddDisconnectedOrgan(target);
            }
        }

        private void RemoveOrganSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;

            if (_disconnectedOrgans.Count <= 0)
            {
                return;
            }

            if (_disconnectedOrgans.Count == 1)
            {
                RemoveOrganSurgeryCallback(_disconnectedOrgans.First(), container, surgeon, performer);
            }
            else
            {
                surgeon.RequestMechanism(_disconnectedOrgans, RemoveOrganSurgeryCallback);
            }
        }

        private async void RemoveOrganSurgeryCallback(IMechanism? target, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer)
        {
            if (Parent == null || target == null || !Parent.Mechanisms.Contains(target))
            {
                return;
            }

            performer.PopupMessage(Loc.GetString("Removing the organ..."));

            if (!performer.HasComponent<DoAfterComponent>())
            {
                Parent.RemoveMechanism(target, performer.Transform.Coordinates);
                RemoveDisconnectedOrgan(target);
                return;
            }

            if (await SurgeryDoAfter(performer))
            {
                Parent.RemoveMechanism(target, performer.Transform.Coordinates);
                RemoveDisconnectedOrgan(target);
            }
        }

        private async void RemoveBodyPartSurgery(IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            if (Parent == null) return;
            if (container is not IBody body) return;

            performer.PopupMessage(Loc.GetString("Sawing off the limb!"));

            if (await SurgeryDoAfter(performer))
            {
                body.RemovePart(Parent);
            }
        }
    }
}
