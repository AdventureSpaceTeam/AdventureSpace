using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Chat;
using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Inventory;
using Content.Server.Mind.Commands;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Roles;
using Content.Server.Speech.Components;
using Content.Server.Temperature.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Tools.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Robust.Shared.Audio;
using System.Linq;

namespace Content.Server.Zombies
{
    /// <summary>
    ///     Handles zombie propagation and inherent zombie traits
    /// </summary>
    /// <remarks>
    ///     Don't Shitcode Open Inside
    /// </remarks>
    public sealed partial class ZombieSystem
    {
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly ServerInventorySystem _inventory = default!;
        [Dependency] private readonly NpcFactionSystem _faction = default!;
        [Dependency] private readonly NPCSystem _npc = default!;
        [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
        [Dependency] private readonly IdentitySystem _identity = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
        [Dependency] private readonly SharedCombatModeSystem _combat = default!;
        [Dependency] private readonly IChatManager _chatMan = default!;
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        /// <summary>
        /// Handles an entity turning into a zombie when they die or go into crit
        /// </summary>
        private void OnDamageChanged(EntityUid uid, ZombifyOnDeathComponent component, MobStateChangedEvent args)
        {
            if (args.NewMobState == MobState.Dead)
            {
                ZombifyEntity(uid, args.Component);
            }
        }

        /// <summary>
        ///     This is the general purpose function to call if you want to zombify an entity.
        ///     It handles both humanoid and nonhumanoid transformation and everything should be called through it.
        /// </summary>
        /// <param name="target">the entity being zombified</param>
        /// <param name="mobState"></param>
        /// <remarks>
        ///     ALRIGHT BIG BOY. YOU'VE COME TO THE LAYER OF THE BEAST. THIS IS YOUR WARNING.
        ///     This function is the god function for zombie stuff, and it is cursed. I have
        ///     attempted to label everything thouroughly for your sanity. I have attempted to
        ///     rewrite this, but this is how it shall lie eternal. Turn back now.
        ///     -emo
        /// </remarks>
        public void ZombifyEntity(EntityUid target, MobStateComponent? mobState = null)
        {
            //Don't zombfiy zombies
            if (HasComp<ZombieComponent>(target) || HasComp<ZombieImmuneComponent>(target))
                return;

            if (!Resolve(target, ref mobState, logMissing: false))
                return;

            //you're a real zombie now, son.
            var zombiecomp = AddComp<ZombieComponent>(target);

            //we need to basically remove all of these because zombies shouldn't
            //get diseases, breath, be thirst, be hungry, or die in space
            RemComp<RespiratorComponent>(target);
            RemComp<BarotraumaComponent>(target);
            RemComp<HungerComponent>(target);
            RemComp<ThirstComponent>(target);

            //funny voice
            EnsureComp<ReplacementAccentComponent>(target).Accent = "zombie";

            //This is needed for stupid entities that fuck up combat mode component
            //in an attempt to make an entity not attack. This is the easiest way to do it.
            RemComp<CombatModeComponent>(target);
            var combat = AddComp<CombatModeComponent>(target);
            _combat.SetInCombatMode(target, true, combat);

            //This is the actual damage of the zombie. We assign the visual appearance
            //and range here because of stuff we'll find out later
            var melee = EnsureComp<MeleeWeaponComponent>(target);
            melee.ClickAnimation = zombiecomp.AttackAnimation;
            melee.WideAnimation = zombiecomp.AttackAnimation;
            melee.Range = 1.2f;

            if (mobState.CurrentState == MobState.Alive)
            {
                // Groaning when damaged
                EnsureComp<EmoteOnDamageComponent>(target);
                _emoteOnDamage.AddEmote(target, "Scream");

                // Random groaning
                EnsureComp<AutoEmoteComponent>(target);
                _autoEmote.AddEmote(target, "ZombieGroan");
            }

            //We have specific stuff for humanoid zombies because they matter more
            if (TryComp<HumanoidAppearanceComponent>(target, out var huApComp)) //huapcomp
            {
                //store some values before changing them in case the humanoid get cloned later
                zombiecomp.BeforeZombifiedSkinColor = huApComp.SkinColor;
                zombiecomp.BeforeZombifiedCustomBaseLayers = new(huApComp.CustomBaseLayers);
                if (TryComp<BloodstreamComponent>(target, out var stream))
                    zombiecomp.BeforeZombifiedBloodReagent = stream.BloodReagent;

                _humanoidAppearance.SetSkinColor(target, zombiecomp.SkinColor, verify: false, humanoid: huApComp);
                _humanoidAppearance.SetBaseLayerColor(target, HumanoidVisualLayers.Eyes, zombiecomp.EyeColor, humanoid: huApComp);

                // this might not resync on clone?
                _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.Tail, zombiecomp.BaseLayerExternal, humanoid: huApComp);
                _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.HeadSide, zombiecomp.BaseLayerExternal, humanoid: huApComp);
                _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.HeadTop, zombiecomp.BaseLayerExternal, humanoid: huApComp);
                _humanoidAppearance.SetBaseLayerId(target, HumanoidVisualLayers.Snout, zombiecomp.BaseLayerExternal, humanoid: huApComp);

                //This is done here because non-humanoids shouldn't get baller damage
                //lord forgive me for the hardcoded damage
                DamageSpecifier dspec = new()
                {
                    DamageDict = new()
                    {
                        { "Slash", 13 },
                        { "Piercing", 7 },
                        { "Structural", 10 }
                    }
                };
                melee.Damage = dspec;

                // humanoid zombies get to pry open doors and shit
                var tool = EnsureComp<ToolComponent>(target);
                tool.SpeedModifier = 0.75f;
                tool.Qualities = new ("Prying");
                tool.UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
                Dirty(tool);
            }

            Dirty(melee);

            //The zombie gets the assigned damage weaknesses and strengths
            _damageable.SetDamageModifierSetId(target, "Zombie");

            //This makes it so the zombie doesn't take bloodloss damage.
            //NOTE: they are supposed to bleed, just not take damage
            _bloodstream.SetBloodLossThreshold(target, 0f);
            //Give them zombie blood
            _bloodstream.ChangeBloodReagent(target, zombiecomp.NewBloodReagent);

            //This is specifically here to combat insuls, because frying zombies on grilles is funny as shit.
            _inventory.TryUnequip(target, "gloves", true, true);
            //Should prevent instances of zombies using comms for information they shouldnt be able to have.
            _inventory.TryUnequip(target, "ears", true, true);

            //popup
            _popup.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, PopupType.LargeCaution);

            //Make it sentient if it's an animal or something
            MakeSentientCommand.MakeSentient(target, EntityManager);

            //Make the zombie not die in the cold. Good for space zombies
            if (TryComp<TemperatureComponent>(target, out var tempComp))
                tempComp.ColdDamage.ClampMax(0);

            _mobThreshold.SetAllowRevives(target, true);
            //Heals the zombie from all the damage it took while human
            if (TryComp<DamageableComponent>(target, out var damageablecomp))
                _damageable.SetAllDamage(target, damageablecomp, 0);
            _mobState.ChangeMobState(target, MobState.Alive);
            _mobThreshold.SetAllowRevives(target, false);

            var factionComp = EnsureComp<NpcFactionMemberComponent>(target);
            foreach (var id in new List<string>(factionComp.Factions))
            {
                _faction.RemoveFaction(target, id);
            }
            _faction.AddFaction(target, "Zombie");

            //gives it the funny "Zombie ___" name.
            var meta = MetaData(target);
            zombiecomp.BeforeZombifiedEntityName = meta.EntityName;
            _metaData.SetEntityName(target, Loc.GetString("zombie-name-prefix", ("target", meta.EntityName)), meta);

            _identity.QueueIdentityUpdate(target);

            //He's gotta have a mind
            var mindComp = EnsureComp<MindContainerComponent>(target);
            if (_mind.TryGetMind(target, out var mind, mindComp) && _mind.TryGetSession(mind, out var session))
            {
                //Zombie role for player manifest
                _mind.AddRole(mind, new ZombieRole(mind, _protoManager.Index<AntagPrototype>(zombiecomp.ZombieRoleId)));

                //Greeting message for new bebe zombers
                _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

                // Notificate player about new role assignment
                _audio.PlayGlobal(zombiecomp.GreetSoundNotification, session);
            }
            else
            {
                var htn = EnsureComp<HTNComponent>(target);
                htn.RootTask = new HTNCompoundTask() {Task = "SimpleHostileCompound"};
                htn.Blackboard.SetValue(NPCBlackboard.Owner, target);
                _npc.WakeNPC(target, htn);
            }

            if (!HasComp<GhostRoleMobSpawnerComponent>(target) && !mindComp.HasMind) //this specific component gives build test trouble so pop off, ig
            {
                //yet more hardcoding. Visit zombie.ftl for more information.
                var ghostRole = EnsureComp<GhostRoleComponent>(target);
                EnsureComp<GhostTakeoverAvailableComponent>(target);
                ghostRole.RoleName = Loc.GetString("zombie-generic");
                ghostRole.RoleDescription = Loc.GetString("zombie-role-desc");
                ghostRole.RoleRules = Loc.GetString("zombie-role-rules");
            }

            if (TryComp<HandsComponent>(target, out var handsComp))
            {
                _hands.RemoveHands(target);
                RemComp(target, handsComp);
            }

            // No longer waiting to become a zombie:
            // Requires deferral because this is (probably) the event which called ZombifyEntity in the first place.
            RemCompDeferred<PendingZombieComponent>(target);

            //zombie gamemode stuff
            var ev = new EntityZombifiedEvent(target);
            RaiseLocalEvent(target, ref ev, true);
            //zombies get slowdown once they convert
            _movementSpeedModifier.RefreshMovementSpeedModifiers(target);
        }
    }
}
