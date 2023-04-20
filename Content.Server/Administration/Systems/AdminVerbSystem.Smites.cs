using System.Threading;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Damage.Systems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GhostKick;
using Content.Server.Interaction.Components;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Server.Tools.Systems;
using Content.Shared.Administration;
using Content.Shared.Administration.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Disease;
using Content.Shared.Electrocution;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Tabletop.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Audio;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Cluwne;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly CreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly GodmodeSystem _godmodeSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;

    // All smite verbs have names so invokeverb works.
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        Verb explode = new()
        {
            Text = "Explode",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Act = () =>
            {
                var coords = Transform(args.Target).MapPosition;
                Timer.Spawn(_gameTiming.TickPeriod,
                    () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                        4, 1, 2, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
                    CancellationToken.None);

                _bodySystem.GibBody(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-explode-description")
        };
        args.Verbs.Add(explode);

        Verb chess = new()
        {
            Text = "Chess Dimension",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Tabletop/chessboard.rsi"), "chessboard"),
            Act = () =>
            {
                _godmodeSystem.EnableGodmode(args.Target); // So they don't suffocate.
                EnsureComp<TabletopDraggableComponent>(args.Target);
                RemComp<PhysicsComponent>(args.Target); // So they can be dragged around.
                var xform = Transform(args.Target);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-chess-self"), args.Target,
                    args.Target, PopupType.LargeCaution);
                _popupSystem.PopupCoordinates(
                    Loc.GetString("admin-smite-chess-others", ("name", args.Target)), xform.Coordinates,
                    Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                var board = Spawn("ChessBoard", xform.Coordinates);
                var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
                xform.Coordinates = EntityCoordinates.FromMap(_mapManager, session.Position);
                xform.WorldRotation = Angle.Zero;
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-chess-dimension-description")
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            Verb flames = new()
            {
                Text = "Set Alight",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Alerts/Fire/fire.png")),
                Act = () =>
                {
                    // Fuck you. Burn Forever.
                    flammable.FireStacks = FlammableSystem.MaximumFireStacks;
                    _flammableSystem.Ignite(args.Target);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-set-alight-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-set-alight-others", ("name", args.Target)), xform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-set-alight-description")
            };
            args.Verbs.Add(flames);
        }

        Verb monkey = new()
        {
            Text = "Monkeyify",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Animals/monkey.rsi"), "dead"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminMonkeySmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-monkeyify-description")
        };
        args.Verbs.Add(monkey);

        Verb disposalBin = new()
        {
            Text = "Garbage Can",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Structures/Piping/disposal.rsi"), "disposal"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminDisposalsSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-garbage-can-description")
        };
        args.Verbs.Add(disposalBin);

        if (TryComp<DiseaseCarrierComponent>(args.Target, out var carrier))
        {
            Verb lungCancer = new()
            {
                Text = "Lung Cancer",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Species/Human/organs.rsi"), "lung-l"),
                Act = () =>
                {
                    _diseaseSystem.TryInfect(carrier, _prototypeManager.Index<DiseasePrototype>("StageIIIALungCancer"),
                        1.0f, true);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-lung-cancer-description")
            };
            args.Verbs.Add(lungCancer);
        }

        if (TryComp<DamageableComponent>(args.Target, out var damageable) &&
            HasComp<MobStateComponent>(args.Target))
        {
            Verb hardElectrocute = new()
            {
                Text = "Electrocute",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Hands/Gloves/Color/yellow.rsi"), "icon"),
                Act = () =>
                {
                    int damageToDeal;
                    if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Critical, out var criticalThreshold)) {
                        // We can't crit them so try killing them.
                        if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Dead,
                                out var deadThreshold))
                            return;// whelp.
                        damageToDeal = deadThreshold.Value.Int() - (int) damageable.TotalDamage;
                    }
                    else
                    {
                        damageToDeal = criticalThreshold.Value.Int() - (int) damageable.TotalDamage;
                    }

                    if (damageToDeal <= 0)
                        damageToDeal = 100; // murder time.

                    if (_inventorySystem.TryGetSlots(args.Target, out var slotDefinitions))
                    {
                        foreach (var slot in slotDefinitions)
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var slotEnt))
                                continue;

                            RemComp<InsulatedComponent>(slotEnt.Value); // Fry the gloves.
                        }
                    }

                    _electrocutionSystem.TryDoElectrocution(args.Target, null, damageToDeal,
                        TimeSpan.FromSeconds(30), refresh: true, ignoreInsulation: true);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-electrocute-description")
            };
            args.Verbs.Add(hardElectrocute);
        }

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            Verb creamPie = new()
            {
                Text = "Creampie",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Consumable/Food/Baked/pie.rsi"), "plain-slice"),
                Act = () =>
                {
                    _creamPieSystem.SetCreamPied(args.Target, creamPied, true);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-creampie-description")
            };
            args.Verbs.Add(creamPie);
        }

        if (TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
        {
            Verb bloodRemoval = new()
            {
                Text = "Remove blood",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Fluids/tomato_splat.rsi"), "puddle-1"),
                Act = () =>
                {
                    _bloodstreamSystem.SpillAllSolutions(args.Target, bloodstream);
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-blood-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-blood-others", ("name", args.Target)), xform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-remove-blood-description")
            };
            args.Verbs.Add(bloodRemoval);
        }

        // bobby...
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            Verb vomitOrgans = new()
            {
                Text = "Vomit organs",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Fluids/vomit_toxin.rsi"), "vomit_toxin-1"),
                Act = () =>
                {
                    _vomitSystem.Vomit(args.Target, -1000, -1000); // You feel hollow!
                    var organs = _bodySystem.GetBodyOrganComponents<TransformComponent>(args.Target, body);
                    var baseXform = Transform(args.Target);
                    foreach (var (xform, organ) in organs)
                    {
                        if (HasComp<BrainComponent>(xform.Owner) || HasComp<EyeComponent>(xform.Owner))
                            continue;

                        var coordinates = baseXform.Coordinates.Offset(_random.NextVector2(0.5f, 0.75f));
                        _bodySystem.DropOrganAt(organ.Owner, coordinates, organ);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-vomit-organs-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-vomit-organs-others", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-vomit-organs-description")
            };
            args.Verbs.Add(vomitOrgans);

            Verb handsRemoval = new()
            {
                Text = "Remove hands",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/remove-hands.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand))
                    {
                        _bodySystem.DropPartAt(part.Id, baseXform.Coordinates, part.Component);
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-others", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-remove-hands-description")
            };
            args.Verbs.Add(handsRemoval);

            Verb handRemoval = new()
            {
                Text = "Remove hands",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/remove-hand.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(body.Owner, BodyPartType.Hand, body))
                    {
                        _bodySystem.DropPartAt(part.Id, baseXform.Coordinates, part.Component);
                        break;
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-others", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-remove-hand-description")
            };
            args.Verbs.Add(handRemoval);

            Verb stomachRemoval = new()
            {
                Text = "Stomach Removal",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Species/Human/organs.rsi"), "stomach"),
                Act = () =>
                {
                    foreach (var (component, _) in _bodySystem.GetBodyOrganComponents<StomachComponent>(args.Target, body))
                    {
                        QueueDel(component.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-stomach-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-stomach-removal-description"),
            };
            args.Verbs.Add(stomachRemoval);

            Verb lungRemoval = new()
            {
                Text = "Lungs Removal",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Species/Human/organs.rsi"), "lung-r"),
                Act = () =>
                {
                    foreach (var (component, _) in _bodySystem.GetBodyOrganComponents<LungComponent>(args.Target, body))
                    {
                        QueueDel(component.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-lung-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-lung-removal-description"),
            };
            args.Verbs.Add(lungRemoval);
        }

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            Verb pinball = new()
            {
                Text = "Pinball",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/toys.rsi"), "basketball"),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    xform.Anchored = false; // Just in case.
                    _physics.SetBodyType(args.Target, BodyType.Dynamic, manager: fixtures, body: physics);
                    _physics.SetBodyStatus(physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        _physics.SetRestitution(args.Target, fixture, 1.1f, false, fixtures);
                    }

                    _fixtures.FixtureUpdate(args.Target, manager: fixtures, body: physics);

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(physics, 0f);
                    _physics.SetAngularDamping(physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-pinball-description")
            };
            args.Verbs.Add(pinball);

            Verb yeet = new()
            {
                Text = "Yeet",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    xform.Anchored = false; // Just in case.

                    _physics.SetBodyType(args.Target, BodyType.Dynamic, body: physics);
                    _physics.SetBodyStatus(physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        _physics.SetHard(args.Target, fixture, false, manager: fixtures);
                    }

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(8.0f, 8.0f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(physics, 0f);
                    _physics.SetAngularDamping(physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-yeet-description")
            };
            args.Verbs.Add(yeet);
        }

        Verb bread = new()
        {
            Text = "Become Bread",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Consumable/Food/Baked/bread.rsi"), "plain"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminBreadSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-become-bread-description")
        };
        args.Verbs.Add(bread);

        Verb mouse = new()
        {
            Text = "Become Mouse",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Mobs/Animals/mouse.rsi"), "icon-0"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminMouseSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-become-mouse-description")
        };
        args.Verbs.Add(mouse);

        if (TryComp<ActorComponent>(args.Target, out var actorComponent))
        {
            Verb ghostKick = new()
            {
                Text = "Ghostkick",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/gavel.svg.192dpi.png")),
                Act = () =>
                {
                    _ghostKickManager.DoDisconnect(actorComponent.PlayerSession.ConnectedClient, "Smitten.");
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-ghostkick-description")
            };
            args.Verbs.Add(ghostKick);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventory)) {
            Verb nyanify = new()
            {
                Text = "Nyanify",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Head/Hats/catears.rsi"), "icon"),
                Act = () =>
                {
                    var ears = Spawn("ClothingHeadHatCatEars", Transform(args.Target).Coordinates);
                    EnsureComp<UnremoveableComponent>(ears);
                    _inventorySystem.TryUnequip(args.Target, "head", true, true, false, inventory);
                    _inventorySystem.TryEquip(args.Target, ears, "head", true, true, false, inventory);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-nyanify-description")
            };
            args.Verbs.Add(nyanify);

            Verb killSign = new()
            {
                Text = "Kill sign",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Misc/killsign.rsi"), "icon"),
                Act = () =>
                {
                    EnsureComp<KillSignComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-kill-sign-description")
            };
            args.Verbs.Add(killSign);

            Verb cluwne = new()
            {
                Text = "Cluwne",
                Category = VerbCategory.Smite,

                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Mask/cluwne.rsi"), "icon"),

                Act = () =>
                {
                    EnsureComp<CluwneComponent>(args.Target);
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-cluwne-description")
            };
            args.Verbs.Add(cluwne);

            Verb maiden = new()
            {
                Text = "Maid",
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ("/Textures/Clothing/Uniforms/Jumpskirt/janimaid.rsi"), "icon"),
                Act = () =>
                {
                    SetOutfitCommand.SetOutfit(args.Target, "JanitorMaidGear", EntityManager, (_, clothing) =>
                    {
                        if (HasComp<ClothingComponent>(clothing))
                            EnsureComp<UnremoveableComponent>(clothing);
                        EnsureComp<ClumsyComponent>(args.Target);
                    });
                },
                Impact = LogImpact.Extreme,
                Message = Loc.GetString("admin-smite-maid-description")
            };
            args.Verbs.Add(maiden);
        }

        Verb angerPointingArrows = new()
        {
            Text = "Anger Pointing Arrows",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Interface/Misc/pointing.rsi"), "pointing"),
            Act = () =>
            {
                EnsureComp<PointingArrowAngeringComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-anger-pointing-arrows-description")
        };
        args.Verbs.Add(angerPointingArrows);

        Verb dust = new()
        {
            Text = "Dust",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Materials/materials.rsi"), "ash"),
            Act = () =>
            {
                EntityManager.QueueDeleteEntity(args.Target);
                Spawn("Ash", Transform(args.Target).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-dust-description"),
        };
        args.Verbs.Add(dust);

        Verb youtubeVideoSimulation = new()
        {
            Text = "Buffering",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Misc/buffering_smite_icon.png")),
            Act = () =>
            {
                EnsureComp<BufferingComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-buffering-description"),
        };
        args.Verbs.Add(youtubeVideoSimulation);

        Verb instrumentation = new()
        {
            Text = "Become Instrument",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/Instruments/h_synthesizer.rsi"), "icon"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminInstrumentSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-become-instrument-description"),
        };
        args.Verbs.Add(instrumentation);

        Verb noGravity = new()
        {
            Text = "Remove gravity",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Structures/Machines/gravity_generator.rsi"), "off"),
            Act = () =>
            {
                var grav = EnsureComp<MovementIgnoreGravityComponent>(args.Target);
                grav.Weightless = true;

                Dirty(grav);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-remove-gravity-description"),
        };
        args.Verbs.Add(noGravity);

        Verb reptilian = new()
        {
            Text = "Reptilian Species Swap",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Objects/Fun/toys.rsi"), "plushie_lizard"),
            Act = () =>
            {
                _polymorphSystem.PolymorphEntity(args.Target, "AdminLizardSmite");
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-reptilian-species-swap-description"),
        };
        args.Verbs.Add(reptilian);

        Verb locker = new()
        {
            Text = "Locker stuff",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ("/Textures/Structures/Storage/closet.rsi"), "generic"),
            Act = () =>
            {
                var xform = Transform(args.Target);
                var locker = Spawn("ClosetMaintenance", xform.Coordinates);
                if (TryComp<EntityStorageComponent>(locker, out var storage))
                {
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                    _entityStorageSystem.Insert(args.Target, locker, storage);
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                }
                _weldableSystem.ForceWeldedState(locker, true);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-locker-stuff-description"),
        };
        args.Verbs.Add(locker);

        Verb headstand = new()
        {
            Text = "Headstand",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Act = () =>
            {
                EnsureComp<HeadstandComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-headstand-description"),
        };
        args.Verbs.Add(headstand);

        Verb zoomIn = new()
        {
            Text = "Zoom in",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/zoom.png")),
            Act = () =>
            {
                var eye = EnsureComp<EyeComponent>(args.Target);

                eye.Zoom *= Vector2.One * 0.2f;

                Dirty(eye);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-zoom-in-description"),
        };
        args.Verbs.Add(zoomIn);

        Verb flipEye = new()
        {
            Text = "Flip eye",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/flip.png")),
            Act = () =>
            {
                var eye = EnsureComp<EyeComponent>(args.Target);

                eye.Zoom *= -1;

                Dirty(eye);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-flip-eye-description"),
        };
        args.Verbs.Add(flipEye);

        Verb runWalkSwap = new()
        {
            Text = "Run Walk Swap",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/run-walk-swap.png")),
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) = (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

                Dirty(movementSpeed);

                _popupSystem.PopupEntity(Loc.GetString("admin-smite-run-walk-swap-prompt"), args.Target,
                    args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-run-walk-swap-description"),
        };
        args.Verbs.Add(runWalkSwap);

        Verb backwardsAccent = new()
        {
            Text = "Speak Backwards",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/help-backwards.png")),
            Act = () =>
            {
                EnsureComp<BackwardsAccentComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-speak-backwards-description"),
        };
        args.Verbs.Add(backwardsAccent);

        Verb disarmProne = new()
        {
            Text = "Disarm Prone",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/Actions/disarm.png")),
            Act = () =>
            {
                EnsureComp<DisarmProneComponent>(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-disarm-prone-description"),
        };
        args.Verbs.Add(disarmProne);

        Verb superSpeed = new()
        {
            Text = "Super speed",
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/super_speed.png")),
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                _movementSpeedModifierSystem?.ChangeBaseSpeed(args.Target, 400, 8000, 40, movementSpeed);

                _popupSystem.PopupEntity(Loc.GetString("admin-smite-super-speed-prompt"), args.Target,
                    args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = Loc.GetString("admin-smite-super-speed-description"),
        };
        args.Verbs.Add(superSpeed);
    }
}
