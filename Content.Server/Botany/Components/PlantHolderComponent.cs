#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos;
using Content.Server.Chemistry.Components;
using Content.Server.Fluids.Components;
using Content.Server.Hands.Components;
using Content.Server.Notification;
using Content.Server.Plants;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Botany;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Notification.Managers;
using Content.Shared.Random.Helpers;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Botany.Components
{
    [RegisterComponent]
    public class PlantHolderComponent : Component, IInteractUsing, IInteractHand, IActivate, IExamine
    {
        public const float HydroponicsSpeedMultiplier = 1f;
        public const float HydroponicsConsumptionMultiplier = 4f;

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "PlantHolder";

        [ViewVariables] private int _lastProduce;
        [ViewVariables(VVAccess.ReadWrite)] private int _missingGas;
        private readonly TimeSpan _cycleDelay = TimeSpan.FromSeconds(15f);
        [ViewVariables] private TimeSpan _lastCycle = TimeSpan.Zero;
        [ViewVariables(VVAccess.ReadWrite)] private bool _updateSpriteAfterUpdate;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("drawWarnings")]
        public bool DrawWarnings { get; private set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public float WaterLevel { get; private set; } = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float NutritionLevel { get; private set; } = 100f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float PestLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Toxins { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Age { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int SkipAging { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Dead { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Harvest { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Sampled { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public int YieldMod { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationMod { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public float MutationLevel { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Health { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public float WeedCoefficient { get; set; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        public Seed? Seed { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperHeat { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperPressure { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ImproperLight { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ForceUpdate { get; set; }

        [ComponentDependency] private readonly SolutionContainerComponent? _solutionContainer = default!;
        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default!;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<SolutionContainerComponent>();
        }

        public void WeedInvasion()
        {
            // TODO
        }

        public void Update()
        {
            UpdateReagents();

            var curTime = _gameTiming.CurTime;

            if (ForceUpdate)
                ForceUpdate = false;
            else if (curTime < (_lastCycle + _cycleDelay))
            {
                if(_updateSpriteAfterUpdate)
                    UpdateSprite();
                return;
            }

            _lastCycle = curTime;


            // Weeds like water and nutrients! They may appear even if there's not a seed planted.
            if (WaterLevel > 10 && NutritionLevel > 2 && _random.Prob(Seed == null ? 0.05f : 0.01f))
            {
                WeedLevel += 1 * HydroponicsSpeedMultiplier * WeedCoefficient;

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // There's a chance for a weed explosion to happen if weeds take over.
            // Plants that are themselves weeds (WeedTolerance > 8) are unaffected.
            if (WeedLevel >= 10 && _random.Prob(0.1f))
            {
                if (Seed == null || WeedLevel >= Seed.WeedTolerance + 2)
                    WeedInvasion();
            }

            // If we have no seed planted, or the plant is dead, stop processing here.
            if (Seed == null || Dead)
            {
                if (_updateSpriteAfterUpdate)
                    UpdateSprite();

                return;
            }

            // There's a small chance the pest population increases.
            // Can only happen when there's a live seed planted.
            if (_random.Prob(0.01f))
            {
                PestLevel += 0.5f * HydroponicsSpeedMultiplier;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Advance plant age here.
            if (SkipAging > 0)
                SkipAging--;
            else
            {
                if(_random.Prob(0.8f))
                    Age += (int)(1 * HydroponicsSpeedMultiplier);

                _updateSpriteAfterUpdate = true;
            }

            // Nutrient consumption.
            if (Seed.NutrientConsumption > 0 && NutritionLevel > 0 && _random.Prob(0.75f))
            {
                NutritionLevel -= MathF.Max(0f, Seed.NutrientConsumption * HydroponicsSpeedMultiplier);
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Water consumption.
            if (Seed.WaterConsumption > 0 && WaterLevel > 0 && _random.Prob(0.75f))
            {
                WaterLevel -= MathF.Max(0f, Seed.NutrientConsumption * HydroponicsConsumptionMultiplier * HydroponicsSpeedMultiplier);
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            var healthMod = _random.Next(1, 3) * HydroponicsSpeedMultiplier;

            // Make sure the plant is not starving.
            if (_random.Prob(0.35f))
            {
                if (NutritionLevel > 2)
                {
                    Health += healthMod;
                }
                else
                {
                    AffectGrowth(-1);
                    Health -= healthMod;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Make sure the plant is not thirsty.
            if (_random.Prob(0.35f))
            {
                if (WaterLevel > 10)
                {
                    Health += healthMod;
                }
                else
                {
                    AffectGrowth(-1);
                    Health -= healthMod;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            var tileAtmos = Owner.Transform.Coordinates.GetTileAtmosphere();
            var environment = tileAtmos?.Air ?? GasMixture.SpaceGas;

            if (Seed.ConsumeGasses.Count > 0)
            {
                _missingGas = 0;

                foreach (var (gas, amount) in Seed.ConsumeGasses)
                {
                    if (environment.GetMoles(gas) < amount)
                    {
                        _missingGas++;
                        continue;
                    }

                    environment.AdjustMoles(gas, -amount);
                }

                if (_missingGas > 0)
                {
                    Health -= _missingGas * HydroponicsSpeedMultiplier;
                    if (DrawWarnings)
                        _updateSpriteAfterUpdate = true;
                }
            }

            // Seed pressure resistance.
            var pressure = environment.Pressure;
            if (pressure < Seed.LowPressureTolerance || pressure > Seed.HighPressureTolerance)
            {
                Health -= healthMod;
                ImproperPressure = true;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }
            else
            {
                ImproperPressure = false;
            }

            // Seed ideal temperature.
            if (MathF.Abs(environment.Temperature - Seed.IdealHeat) > Seed.HeatTolerance)
            {
                Health -= healthMod;
                ImproperHeat = true;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }
            else
            {
                ImproperHeat = false;
            }

            // Gas production.
            var exudeCount = Seed.ExudeGasses.Count;
            if (exudeCount > 0)
            {
                foreach (var (gas, amount) in Seed.ExudeGasses)
                {
                    environment.AdjustMoles(gas, MathF.Max(1f, MathF.Round((amount * MathF.Round(Seed.Potency)) / exudeCount)));
                }
            }

            // Toxin levels beyond the plant's tolerance cause damage.
            // They are, however, slowly reduced over time.
            if (Toxins > 0)
            {
                var toxinUptake = MathF.Max(1, MathF.Round(Toxins / 10f));
                if (Toxins > Seed.ToxinsTolerance)
                {
                    Health -= toxinUptake;
                }

                Toxins -= toxinUptake;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Weed levels.
            if (PestLevel > 0)
            {
                // TODO: Carnivorous plants?
                if (PestLevel > Seed.PestTolerance)
                {
                    Health -= HydroponicsSpeedMultiplier;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            // Weed levels.
            if (WeedLevel > 0)
            {
                // TODO: Parasitic plants.
                if (WeedLevel >= Seed.WeedTolerance)
                {
                    Health -= HydroponicsSpeedMultiplier;
                }

                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }

            if (Age > Seed.Lifespan)
            {
                Health -= _random.Next(3, 5) * HydroponicsSpeedMultiplier;
                if (DrawWarnings)
                    _updateSpriteAfterUpdate = true;
            }
            else if(Age < 0) // Revert back to seed packet!
            {
                Seed.SpawnSeedPacket(Owner.Transform.Coordinates);
                RemovePlant();
                ForceUpdate = true;
                Update();
            }

            CheckHealth();

            if (Harvest && Seed.HarvestRepeat == HarvestType.SelfHarvest)
                AutoHarvest();

            // If enough time has passed since the plant was harvested, we're ready to harvest again!
            if (!Dead && Seed.ProductPrototypes.Count > 0)
            {
                if (Age > Seed.Production)
                {
                    if ((Age - _lastProduce) > Seed.Production && !Harvest)
                    {
                        Harvest = true;
                        _lastProduce = Age;
                    }
                }
                else
                {
                    if (Harvest)
                    {
                        Harvest = false;
                        _lastProduce = Age;
                    }
                }
            }

            CheckLevelSanity();

            if(_updateSpriteAfterUpdate)
                UpdateSprite();
        }

        private void CheckLevelSanity()
        {
            if (Seed != null)
                Health = MathHelper.Clamp(Health, 0, Seed.Endurance);
            else
            {
                Health = 0f;
                Dead = false;
            }

            MutationLevel = MathHelper.Clamp(MutationLevel, 0f, 100f);
            NutritionLevel = MathHelper.Clamp(NutritionLevel, 0f, 100f);
            WaterLevel = MathHelper.Clamp(WaterLevel, 0f, 100f);
            PestLevel = MathHelper.Clamp(PestLevel, 0f, 10f);
            WeedLevel = MathHelper.Clamp(WeedLevel, 0f, 10f);
            Toxins = MathHelper.Clamp(Toxins, 0f, 100f);
            YieldMod = MathHelper.Clamp(YieldMod, 0, 2);
            MutationMod = MathHelper.Clamp(MutationMod, 0f, 3f);
        }

        public bool DoHarvest(IEntity user)
        {
            if (Seed == null || user.Deleted || !EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                return false;

            if (Harvest && !Dead)
            {
                if (user.TryGetComponent(out HandsComponent? hands))
                {
                    if (!Seed.CheckHarvest(user, hands.GetActiveHand?.Owner))
                        return false;

                } else if (!Seed.CheckHarvest(user))
                {
                    return false;
                }

                Seed.Harvest(user, YieldMod);
                AfterHarvest();
                return true;
            }

            if (!Dead) return false;

            RemovePlant();
            AfterHarvest();
            return true;
        }

        public void AutoHarvest()
        {
            if (Seed == null || !Harvest)
                return;

            Seed.AutoHarvest(Owner.Transform.Coordinates);
            AfterHarvest();
        }

        private void AfterHarvest()
        {
            Harvest = false;
            _lastProduce = Age;

            if(Seed?.HarvestRepeat == HarvestType.NoRepeat)
                RemovePlant();

            CheckLevelSanity();
            UpdateSprite();
        }

        public void CheckHealth()
        {
            if (Health <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            Dead = true;
            Harvest = false;
            MutationLevel = 0;
            YieldMod = 1;
            MutationMod = 1;
            ImproperLight = false;
            ImproperHeat = false;
            ImproperPressure = false;
            WeedLevel += 1 * HydroponicsSpeedMultiplier;
            PestLevel = 0;
            UpdateSprite();
        }

        public void RemovePlant()
        {
            YieldMod = 1;
            MutationMod = 1;
            PestLevel = 0;
            Seed = null;
            Dead = false;
            Age = 0;
            Sampled = false;
            Harvest = false;
            ImproperLight = false;
            ImproperPressure = false;
            ImproperHeat = false;

            UpdateSprite();
        }

        public void AffectGrowth(int amount)
        {
            if (Seed == null)
                return;

            if (amount > 0)
            {
                if (Age < Seed.Maturation)
                    Age += amount;
                else if (!Harvest && Seed.Yield <= 0f)
                    _lastProduce -= amount;
            }
            else
            {
                if (Age < Seed.Maturation)
                    SkipAging++;
                else if (!Harvest && Seed.Yield <= 0f)
                    _lastProduce += amount;
            }
        }

        public void AdjustNutrient(float amount)
        {
            NutritionLevel += amount;
        }

        public void AdjustWater(float amount)
        {
            WaterLevel += amount;

            // Water dilutes toxins.
            if (amount > 0)
            {
                Toxins -= amount * 4f;
            }
        }

        public void UpdateReagents()
        {
            if (_solutionContainer == null)
                return;

            if (_solutionContainer.Solution.TotalVolume <= 0 || MutationLevel >= 25)
            {
                if (MutationLevel >= 0)
                {
                    Mutate(Math.Min(MutationLevel, 25));
                    MutationLevel = 0;
                }
            }
            else
            {
                var one = ReagentUnit.New(1);

                foreach (var (reagent, amount) in _solutionContainer.ReagentList.ToArray())
                {
                    var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent);
                    reagentProto.ReactionPlant(Owner);
                    _solutionContainer.Solution.RemoveReagent(reagent, amount < one ? amount : one);
                }
            }

            CheckLevelSanity();
        }

        private void Mutate(float severity)
        {
            // TODO: Coming soon in "Botany 2: Plant boogaloo".
        }

        public void UpdateSprite()
        {
            _updateSpriteAfterUpdate = false;

            if (_appearanceComponent == null)
                return;

            if (Seed != null)
            {
                if(DrawWarnings)
                    _appearanceComponent.SetData(PlantHolderVisuals.HealthLight, Health <= (Seed.Endurance / 2f));

                if (Dead)
                {
                    _appearanceComponent.SetData(PlantHolderVisuals.Plant, new SpriteSpecifier.Rsi(Seed.PlantRsi, "dead"));
                }
                else if (Harvest)
                {
                    _appearanceComponent.SetData(PlantHolderVisuals.Plant, new SpriteSpecifier.Rsi(Seed.PlantRsi, "harvest"));
                }
                else if (Age < Seed.Maturation)
                {
                    var growthStage = Math.Max(1, (int)((Age * Seed.GrowthStages) / Seed.Maturation));
                    _appearanceComponent.SetData(PlantHolderVisuals.Plant, new SpriteSpecifier.Rsi(Seed.PlantRsi,$"stage-{growthStage}"));
                    _lastProduce = Age;
                }
                else
                {
                    _appearanceComponent.SetData(PlantHolderVisuals.Plant, new SpriteSpecifier.Rsi(Seed.PlantRsi,$"stage-{Seed.GrowthStages}"));
                }
            }
            else
            {
                _appearanceComponent.SetData(PlantHolderVisuals.Plant, SpriteSpecifier.Invalid);
                _appearanceComponent.SetData(PlantHolderVisuals.HealthLight, false);
            }

            if (!DrawWarnings) return;
            _appearanceComponent.SetData(PlantHolderVisuals.WaterLight, WaterLevel <= 10);
            _appearanceComponent.SetData(PlantHolderVisuals.NutritionLight, NutritionLevel <= 2);
            _appearanceComponent.SetData(PlantHolderVisuals.AlertLight, WeedLevel >= 5 || PestLevel >= 5 || Toxins >= 40 || ImproperHeat || ImproperLight || ImproperPressure || _missingGas > 0);
            _appearanceComponent.SetData(PlantHolderVisuals.HarvestLight, Harvest);
        }

        public void CheckForDivergence(bool modified)
        {
            // Make sure we're not modifying a "global" seed.
            // If this seed is not in the global seed list, then no products of this line have been harvested yet.
            // It is then safe to assume it's restricted to this tray.
            if (Seed == null) return;
            var plantSystem = EntitySystem.Get<PlantSystem>();
            if (plantSystem.Seeds.ContainsKey(Seed.Uid))
                Seed = Seed.Diverge(modified);
        }

        private void ForceUpdateByExternalCause()
        {
            SkipAging++; // We're forcing an update cycle, so one age hasn't passed.
            ForceUpdate = true;
            Update();
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            var user = eventArgs.User;
            var usingItem = eventArgs.Using;

            if (usingItem == null || usingItem.Deleted || !EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
                return false;

            if (usingItem.TryGetComponent(out SeedComponent? seeds))
            {
                if (Seed == null)
                {
                    if (seeds.Seed == null)
                    {
                        user.PopupMessageCursor(Loc.GetString("plant-holder-component-empty-seed-packet-message"));
                        usingItem.QueueDelete();
                        return false;
                    }

                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-plant-success-message",
                                                          ("seedName", seeds.Seed.SeedName),
                                                          ("seedNoun", seeds.Seed.SeedNoun)));

                    Seed = seeds.Seed;
                    Dead = false;
                    Age = 1;
                    Health = Seed.Endurance;
                    _lastCycle = _gameTiming.CurTime;

                    usingItem.QueueDelete();

                    CheckLevelSanity();
                    UpdateSprite();

                    return true;
                }

                user.PopupMessageCursor(Loc.GetString("plant-holder-component-already-seeded-message", ("name", Owner.Name)));
                return false;
            }

            if (usingItem.HasTag("Hoe"))
            {
                if (WeedLevel > 0)
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-remove-weeds-message",("name", Owner.Name)));
                    user.PopupMessageOtherClients(Loc.GetString("plant-holder-component-remove-weeds-others-message",("otherName", user.Name)));
                    WeedLevel = 0;
                    UpdateSprite();
                }
                else
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-no-weeds-message"));
                }

                return true;
            }

            if (usingItem.HasTag("Shovel"))
            {
                if (Seed != null)
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-remove-plant-message",("name", Owner.Name)));
                    user.PopupMessageOtherClients(Loc.GetString("plant-holder-component-remove-plant-others-message",("name", user.Name)));
                    RemovePlant();
                }
                else
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-no-plant-message"));
                }

                return true;
            }

            if (usingItem.TryGetComponent(out ISolutionInteractionsComponent? solution) && solution.CanDrain)
            {
                var amount = ReagentUnit.New(5);
                var sprayed = false;

                if (usingItem.TryGetComponent(out SprayComponent? spray))
                {
                    sprayed = true;
                    amount = ReagentUnit.New(1);

                    if (!string.IsNullOrEmpty(spray.SpraySound))
                    {
                        SoundSystem.Play(Filter.Pvs(usingItem), spray.SpraySound, usingItem, AudioHelpers.WithVariation(0.125f));
                    }
                }

                var split = solution.Drain(amount);
                if (split.TotalVolume == 0)
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-empty-message",("owner", usingItem)));
                    return true;
                }

                user.PopupMessageCursor(Loc.GetString(sprayed ? "plant-holder-component-spray-message" : "plant-holder-component-transfer-message",
                                                      ("owner",Owner),
                                                      ("amount",split.TotalVolume)));

                _solutionContainer?.TryAddSolution(split);

                ForceUpdateByExternalCause();

                return true;
            }

            if (usingItem.HasTag("PlantSampleTaker"))
            {
                if (Seed == null)
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-nothing-to-sample-message"));
                    return false;
                }

                if (Sampled)
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-already-sampled-message"));
                    return false;
                }

                if (Dead)
                {
                    user.PopupMessageCursor(Loc.GetString("plant-holder-component-dead-plant-message"));
                    return false;
                }

                var seed = Seed.SpawnSeedPacket(user.Transform.Coordinates);
                seed.RandomOffset(0.25f);
                user.PopupMessageCursor(Loc.GetString("plant-holder-component-take-sample-message", ("seedName", Seed.DisplayName)));
                Health -= (_random.Next(3, 5) * 10);

                if (_random.Prob(0.3f))
                    Sampled = true;

                // Just in case.
                CheckLevelSanity();
                ForceUpdateByExternalCause();

                return true;
            }

            if (usingItem.HasTag("BotanySharp"))
            {
                return DoHarvest(user);
            }

            if (usingItem.HasComponent<ProduceComponent>())
            {
                user.PopupMessageCursor(Loc.GetString("plant-holder-component-compost-message",
                                                      ("owner", Owner),
                                                      ("usingItem", usingItem)));
                user.PopupMessageOtherClients(Loc.GetString("plant-holder-component-compost-others-message",
                                                            ("user",user),
                                                            ("usingItem", usingItem),
                                                            ("owner", Owner)));

                if (usingItem.TryGetComponent(out SolutionContainerComponent? solution2))
                {
                    // This deliberately discards overfill.
                    _solutionContainer?.TryAddSolution(solution2.SplitSolution(solution2.Solution.TotalVolume));

                    ForceUpdateByExternalCause();
                }

                usingItem.QueueDelete();

                return true;
            }

            return false;
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            // DoHarvest does the sanity checks.
            return DoHarvest(eventArgs.User);
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            // DoHarvest does the sanity checks.
            DoHarvest(eventArgs.User);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!inDetailsRange)
                return;

            if (Seed == null)
            {
                message.AddMarkup(Loc.GetString("plant-holder-component-nothing-planted-message") + "\n");
            }
            else if (!Dead)
            {
                message.AddMarkup(Loc.GetString("plant-holder-component-something-already-growing-message",
                                                ("seedName", Seed.DisplayName),
                                                ("toBeForm", Seed.DisplayName.EndsWith('s') ? "are" : "is"))
                                  + "\n");

                if(Health <= Seed.Endurance / 2)
                    message.AddMarkup(Loc.GetString("plant-holder-component-something-already-growing-low-health-message",
                                                    ("healthState", Loc.GetString(Age > Seed.Lifespan ? "plant-holder-component-plant-old-adjective" :
                                                                                                        "plant-holder-component-plant-unhealthy-adjective")))
                                      + "\n");
            }
            else
            {
                message.AddMarkup(Loc.GetString("plant-holder-component-dead-plant-matter-message") + "\n");
            }

            if(WeedLevel >= 5)
                message.AddMarkup(Loc.GetString("plant-holder-component-weed-high-level-message") + "\n");

            if(PestLevel >= 5)
                message.AddMarkup(Loc.GetString("plant-holder-component-pest-high-level-message") + "\n");

            message.AddMarkup(Loc.GetString($"plant-holder-component-water-level-message",("waterLevel", (int)WaterLevel)) + "\n");
            message.AddMarkup(Loc.GetString($"plant-holder-component-nutrient-level-message", ("nutritionLevel", (int)NutritionLevel)) + "\n");

            if (DrawWarnings)
            {
                if(Toxins > 40f)
                    message.AddMarkup(Loc.GetString("plant-holder-component-toxins-high-warning") + "\n");

                if(ImproperLight)
                    message.AddMarkup(Loc.GetString("plant-holder-component-light-improper-warning") + "\n");

                if(ImproperHeat)
                    message.AddMarkup(Loc.GetString("plant-holder-component-heat-improper-warning") + "\n");

                if(ImproperPressure)
                    message.AddMarkup(Loc.GetString("plant-holder-component-pressure-improper-warning") + "\n");

                if(_missingGas > 0)
                    message.AddMarkup(Loc.GetString("plant-holder-component-gas-missing-warning") + "\n");
            }
        }
    }
}
