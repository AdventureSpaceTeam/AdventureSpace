﻿using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Nutrition.AnimalHusbandry;

/// <summary>
/// This is used for simple animal husbandry. Entities with this component,
/// given they are next to a particular entity that fulfills a whitelist,
/// can create several "child" entities.
/// </summary>
[RegisterComponent]
public sealed class ReproductiveComponent : Component
{
    /// <summary>
    /// The next time when breeding will be attempted.
    /// </summary>
    [DataField("nextBreedAttempt", customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextBreedAttempt;

    /// <summary>
    /// Minimum length between each attempt to breed.
    /// </summary>
    [DataField("minBreedAttemptInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinBreedAttemptInterval = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Maximum length between each attempt to breed.
    /// </summary>
    [DataField("maxBreedAttemptInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxBreedAttemptInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// How close to a partner an entity must be in order to breed.
    /// Unrealistically long.
    /// </summary>
    [DataField("breedRange"), ViewVariables(VVAccess.ReadWrite)]
    public float BreedRange = 3f;

    /// <summary>
    /// The chance that, on a given attempt,
    /// for each valid partner, the entity will breed.
    /// </summary>
    [DataField("breedChance"), ViewVariables(VVAccess.ReadWrite)]
    public float BreedChance = 0.15f;

    /// <summary>
    /// Entity prototypes for what type of
    /// offspring can be produced by this entity.
    /// </summary>
    [DataField("offspring", required: true)]
    public List<EntitySpawnEntry> Offspring = default!;

    /// <summary>
    /// Whether or not this entity has bred successfully
    /// and will produce offspring imminently
    /// </summary>
    [DataField("gestating")]
    public bool Gestating;

    /// <summary>
    /// When gestation will end.
    /// Null if <see cref="Gestating"/> is false
    /// </summary>
    [DataField("gestationEndTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? GestationEndTime;

    /// <summary>
    /// How long it takes the entity after breeding
    /// to produce offspring
    /// </summary>
    [DataField("gestationDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan GestationDuration = TimeSpan.FromMinutes(1.5);

    /// <summary>
    /// How much hunger is consumed when an entity
    /// gives birth. A balancing tool to require feeding.
    /// </summary>
    [DataField("hungerPerBirth"), ViewVariables(VVAccess.ReadWrite)]
    public float HungerPerBirth = 75f;

    /// <summary>
    /// Popup shown when an entity gives birth.
    /// Configurable for things like laying eggs.
    /// </summary>
    [DataField("birthPopup"), ViewVariables(VVAccess.ReadWrite)]
    public string BirthPopup = "reproductive-birth-popup";

    /// <summary>
    /// Whether or not the offspring should be made into "infants".
    /// </summary>
    [DataField("makeOffspringInfant"), ViewVariables(VVAccess.ReadWrite)]
    public bool MakeOffspringInfant = true;

    /// <summary>
    /// An entity whitelist for what entities
    /// can be this one's partner.
    /// </summary>
    [DataField("partnerWhitelist", required: true)]
    public EntityWhitelist PartnerWhitelist = default!;
}
