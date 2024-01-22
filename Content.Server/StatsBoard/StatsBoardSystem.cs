using System.Linq;
using Content.Server.Cargo.Components;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Construction;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Electrocution;
using Content.Shared.Fluids;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.StatsBoard;

public sealed class StatsBoardSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private (EntityUid? killer, EntityUid? victim, TimeSpan time) _firstMurder = (null, null, TimeSpan.Zero);
    private EntityUid? _hamsterKiller;
    private int _jointCreated;
    private (EntityUid? clown, TimeSpan? time) _clownCuffed = (null, null);
    private readonly Dictionary<EntityUid, StatisticEntry> _statisticEntries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatsBoardComponent, ComponentInit>(OnStartup);
        SubscribeLocalEvent<StatsBoardComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
        SubscribeLocalEvent<StatsBoardComponent, DamageChangedEvent>(OnDamageModify);
        SubscribeLocalEvent<StatsBoardComponent, SlippedEvent>(OnSlippedEvent);
        SubscribeLocalEvent<StatsBoardComponent, CreamedEvent>(OnCreamedEvent);
        SubscribeLocalEvent<StatsBoardComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<StatsBoardComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<StatsBoardComponent, DoorEmaggedEvent>(OnDoorEmagged);
        SubscribeLocalEvent<StatsBoardComponent, ElectrocutedEvent>(OnElectrocuted);
        SubscribeLocalEvent<StatsBoardComponent, SubtractCashEvent>(OnItemPurchasedEvent);
        SubscribeLocalEvent<StatsBoardComponent, CuffedEvent>(OnCuffedEvent);
        SubscribeLocalEvent<StatsBoardComponent, ItemConstructionCreated>(OnCraftedEvent);
        SubscribeLocalEvent<StatsBoardComponent, AbsorberPudleEvent>(OnAbsorbedPuddleEvent);
        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
    }

    private void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;
        _firstMurder = (null, null, TimeSpan.Zero);
        _hamsterKiller = null;
        _jointCreated = 0;
        _clownCuffed = (null, TimeSpan.Zero);
        _statisticEntries.Clear();
    }

    private void OnShutdown(EntityUid uid, StatsBoardComponent component, ComponentShutdown args)
    {
        if (!_statisticEntries.ContainsKey(uid))
        {
            _statisticEntries.Add(uid, new StatisticEntry(MetaData(uid).EntityName));
        }
        else
        {
            _statisticEntries[uid].Name = MetaData(uid).EntityName;
        }
    }

    private void OnStartup(EntityUid uid, StatsBoardComponent component, ComponentInit args)
    {
        if (!_statisticEntries.ContainsKey(uid))
        {
            _statisticEntries.Add(uid, new StatisticEntry(MetaData(uid).EntityName));
        }
    }

    private void OnAbsorbedPuddleEvent(EntityUid uid, StatsBoardComponent comp, ref AbsorberPudleEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        _statisticEntries[uid].AbsorbedPuddleCount += 1;
    }

    private void OnCraftedEvent(EntityUid uid, StatsBoardComponent comp, ref ItemConstructionCreated ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        if (!TryComp<MetaDataComponent>(ev.Item, out var metaDataComponent))
            return;
        if (metaDataComponent.EntityPrototype == null)
            return;
        switch (metaDataComponent.EntityPrototype.ID)
        {
            case "Blunt":
            case "Joint":
                _jointCreated += 1;
                break;
        }
    }

    private void OnCuffedEvent(EntityUid uid, StatsBoardComponent comp, ref CuffedEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        _statisticEntries[uid].CuffedCount += 1;
        if (_clownCuffed.clown != null)
            return;
        if (!HasComp<ClumsyComponent>(uid))
            return;
        _clownCuffed.clown = uid;
        _clownCuffed.time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
    }

    private void OnItemPurchasedEvent(EntityUid uid, StatsBoardComponent comp, ref SubtractCashEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        if (ev.Currency != "Telecrystal")
            return;
        if (_statisticEntries[uid].SpentTk == null)
        {
            _statisticEntries[uid].SpentTk = ev.Cost.Int();
        }
        else
        {
            _statisticEntries[uid].SpentTk += ev.Cost.Int();
        }
    }

    private void OnElectrocuted(EntityUid uid, StatsBoardComponent comp, ElectrocutedEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        _statisticEntries[uid].ElectrocutedCount += 1;
    }

    private void OnDoorEmagged(EntityUid uid, StatsBoardComponent comp, ref DoorEmaggedEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        _statisticEntries[uid].DoorEmagedCount += 1;
    }

    private void OnInteractionAttempt(EntityUid uid, StatsBoardComponent component, InteractionAttemptEvent args)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        if (!HasComp<ItemComponent>(args.Target))
            return;
        if (MetaData(args.Target.Value).EntityPrototype == null)
            return;
        var entityPrototype = MetaData(args.Target.Value).EntityPrototype;
        if (entityPrototype is not { ID: "CaptainIDCard" })
            return;
        if (_statisticEntries[uid].IsInteractedCaptainCard)
            return;
        _statisticEntries[uid].IsInteractedCaptainCard = true;
    }

    private void OnCreamedEvent(EntityUid uid, StatsBoardComponent comp, ref CreamedEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;

        _statisticEntries[uid].CreamedCount += 1;
    }

    private void OnMobStateChanged(EntityUid uid, StatsBoardComponent component, MobStateChangedEvent args)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;

        switch (args.NewMobState)
        {
            case MobState.Dead:
            {
                _statisticEntries[uid].DeadCount += 1;

                EntityUid? origin = null;
                if (args.Origin != null)
                {
                    origin = args.Origin.Value;
                }

                if (_firstMurder.victim == null && HasComp<HumanoidAppearanceComponent>(uid))
                {
                    _firstMurder.victim = uid;
                    _firstMurder.killer = origin;
                    _firstMurder.time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                    Logger.Info($"First Murder. CurTime: {_gameTiming.CurTime}, RoundStartTimeSpan: {_gameTicker.RoundStartTimeSpan}, Substract: {_gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)}");
                }

                if (origin != null)
                {
                    if (_tagSystem.HasTag(uid, "Hamster"))
                    {
                        _hamsterKiller = origin.Value;
                    }

                    if (TryComp<StatsBoardComponent>(args.Origin, out var statsBoard))
                    {
                        if (_tagSystem.HasTag(uid, "Mouse"))
                        {
                            _statisticEntries[origin.Value].KilledMouseCount += 1;
                        }
                        if (HasComp<HumanoidAppearanceComponent>(uid))
                            _statisticEntries[origin.Value].HumanoidKillCount += 1;
                    }
                }

                break;
            }
        }
    }

    private void OnDamageModify(EntityUid uid, StatsBoardComponent comp, DamageChangedEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        if (ev.DamageDelta == null)
            return;
        if (ev.DamageIncreased)
        {
            _statisticEntries[uid].TotalTakeDamage += ev.DamageDelta.GetTotal().Int();
        }
        else
        {
            _statisticEntries[uid].TotalTakeHeal += Math.Abs(ev.DamageDelta.GetTotal().Int());
        }

        if (ev.Origin != null)
        {
            if (!_statisticEntries.ContainsKey(ev.Origin.Value))
            {
                _statisticEntries.Add(ev.Origin.Value, new StatisticEntry(MetaData(ev.Origin.Value).EntityName));
            }
            if (ev.DamageIncreased)
            {
                _statisticEntries[ev.Origin.Value].TotalInflictedDamage += ev.DamageDelta.GetTotal().Int();
            }
            else
            {
                _statisticEntries[ev.Origin.Value].TotalInflictedHeal += Math.Abs(ev.DamageDelta.GetTotal().Int());
            }
        }
    }

    private void OnSlippedEvent(EntityUid uid, StatsBoardComponent comp, ref SlippedEvent ev)
    {
        if (!_statisticEntries.ContainsKey(uid))
            return;
        _statisticEntries[uid].SlippedCount += 1;
    }

    private StationBankAccountComponent? GetBankAccount(EntityUid? uid)
    {
        if (uid != null && TryComp<StationBankAccountComponent>(uid, out var bankAccount))
        {
            return bankAccount;
        }
        return null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var statsQuery = EntityQueryEnumerator<StatsBoardComponent>();
        while (statsQuery.MoveNext(out var ent, out var comp))
        {
            if (!_statisticEntries.ContainsKey(ent))
                return;

            if (TryComp<TransformComponent>(ent, out var transformComponent) &&
                transformComponent.GridUid == null && HasComp<HumanoidAppearanceComponent>(ent))
                _statisticEntries[ent].SpaceTime += TimeSpan.FromSeconds(frameTime);

            if (TryComp<CuffableComponent>(ent, out var cuffableComponent) &&
                !cuffableComponent.CanStillInteract)
                _statisticEntries[ent].CuffedTime += TimeSpan.FromSeconds(frameTime);

            if (HasComp<SleepingComponent>(ent))
                _statisticEntries[ent].SleepTime += TimeSpan.FromSeconds(frameTime);
        }
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        var result = "";
        var totalSlipped = 0;
        var totalCreampied = 0;
        var totalDamage = 0;
        var totalHeal = 0;
        var totalDoorEmaged = 0;
        var maxSlippedCount = 0;
        var maxDeadCount = 0;
        var maxSpeciesCount = 0;
        var maxDoorEmagedCount = 0;
        var totalKilledMice = 0;
        var totalAbsorbedPuddle = 0;
        var maxKillsMice = 0;
        var totalCaptainCardInteracted = 0;
        var totalElectrocutedCount = 0;
        var totalSleepTime = TimeSpan.Zero;
        var minSpentTk = int.MaxValue;
        var maxHumKillCount = 0;
        var totalCuffedCount = 0;
        var maxTakeDamage = 0;
        var maxInflictedHeal = 0;
        var maxInflictedDamage = 0;
        var maxPuddleAbsorb = 0;
        decimal maxBalance = 0;
        var maxCuffedTime = TimeSpan.Zero;
        var maxSpaceTime = TimeSpan.Zero;
        var maxSleepTime = TimeSpan.Zero;
        string? mostPopularSpecies = null;
        Dictionary<string, int> roundSpecies = new();
        EntityUid? mostSlippedCharacter = null;
        EntityUid? mostDeadCharacter = null;
        EntityUid? mostDoorEmagedCharacter = null;
        EntityUid? mostKillsMiceCharacter = null;
        EntityUid? playerWithMinSpentTk = null;
        EntityUid? playerWithMaxHumKills = null;
        EntityUid? playerWithMaxDamage = null;
        EntityUid? playerWithLongestCuffedTime = null;
        EntityUid? playerWithLongestSpaceTime = null;
        EntityUid? playerWithLongestSleepTime = null;
        EntityUid? playerWithMostInflictedHeal = null;
        EntityUid? playerWithMostInflictedDamage = null;
        EntityUid? playerWithMostPuddleAbsorb = null;
        EntityUid? playerWithMostBigBalance = null;

        foreach (var (uid, data) in _statisticEntries)
        {
            if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoidAppearanceComponent))
            {
                var speciesProto = _prototypeManager.Index<SpeciesPrototype>(humanoidAppearanceComponent.Species);

                if (roundSpecies.TryGetValue(speciesProto.Name, out var count))
                {
                    roundSpecies[speciesProto.Name] = count + 1;
                }
                else
                {
                    roundSpecies.Add(speciesProto.Name, 1);
                }
            }

            totalDoorEmaged += data.DoorEmagedCount;
            totalSlipped += data.SlippedCount;
            totalCreampied += data.CreamedCount;
            totalDamage += data.TotalTakeDamage;
            totalHeal += data.TotalTakeHeal;
            totalCuffedCount += data.CuffedCount;
            totalKilledMice += data.KilledMouseCount;
            totalSleepTime += data.SleepTime;
            totalAbsorbedPuddle += data.AbsorbedPuddleCount;
            totalElectrocutedCount += data.ElectrocutedCount;

            if (data.SlippedCount > maxSlippedCount)
            {
                maxSlippedCount = data.SlippedCount;
                mostSlippedCharacter = uid;
            }

            if (data.DoorEmagedCount > maxDoorEmagedCount)
            {
                maxDoorEmagedCount = data.DoorEmagedCount;
                mostDoorEmagedCharacter = uid;
            }

            if (data.DeadCount > maxDeadCount)
            {
                maxDeadCount = data.DeadCount;
                mostDeadCharacter = uid;
            }

            if (data.KilledMouseCount > maxKillsMice)
            {
                maxKillsMice = data.KilledMouseCount;
                mostKillsMiceCharacter = uid;
            }

            if (data.IsInteractedCaptainCard)
            {
                totalCaptainCardInteracted += 1;
            }

            if (data.SpentTk != null && data.SpentTk < minSpentTk)
            {
                minSpentTk = data.SpentTk.Value;
                playerWithMinSpentTk = uid;
            }

            if (data.HumanoidKillCount > maxHumKillCount)
            {
                maxHumKillCount = data.HumanoidKillCount;
                playerWithMaxHumKills = uid;
            }

            if (data.TotalTakeDamage > maxTakeDamage)
            {
                maxTakeDamage = data.TotalTakeDamage;
                playerWithMaxDamage = uid;
            }

            if (data.CuffedTime > maxCuffedTime)
            {
                maxCuffedTime = data.CuffedTime;
                playerWithLongestCuffedTime = uid;
            }

            if (data.SleepTime > maxSleepTime)
            {
                maxSleepTime = data.SleepTime;
                playerWithLongestSleepTime = uid;
            }

            if (data.SpaceTime > maxSpaceTime)
            {
                maxSpaceTime = data.SpaceTime;
                playerWithLongestSpaceTime = uid;
            }

            if (data.TotalInflictedHeal > maxInflictedHeal)
            {
                maxInflictedHeal = data.TotalInflictedHeal;
                playerWithMostInflictedHeal = uid;
            }

            if (data.TotalInflictedDamage > maxInflictedDamage)
            {
                maxInflictedDamage = data.TotalInflictedDamage;
                playerWithMostInflictedDamage = uid;
            }

            if (data.AbsorbedPuddleCount > maxPuddleAbsorb)
            {
                maxPuddleAbsorb = data.AbsorbedPuddleCount;
                playerWithMostPuddleAbsorb = uid;
            }
        }

        result += "На станции были представители таких рас:";
        foreach (var speciesEntry in roundSpecies)
        {
            var species = speciesEntry.Key;
            var count = speciesEntry.Value;

            if (count > maxSpeciesCount)
            {
                maxSpeciesCount = count;
                mostPopularSpecies = species;
            }

            result += $"\n[bold][color=white]{Loc.GetString(species)}[/color][/bold] в количестве [color=white]{count}[/color].";
        }

        if (mostPopularSpecies != null)
        {
            result += $"\nСамой распространённой расой стал [color=white]{Loc.GetString(mostPopularSpecies)}[/color].";
        }

        var station = _station.GetStations().FirstOrDefault();
        var bank = GetBankAccount(station);

        if (bank != null)
            result += $"\nПод конец смены баланс карго составил [color=white]{bank.Balance}[/color] кредитов.";

        if (_firstMurder.victim != null)
        {
            var victimUsername = TryGetUsername(_firstMurder.victim.Value);
            var victimName = TryGetName(_firstMurder.victim.Value,
                _statisticEntries[_firstMurder.victim.Value].Name);
            var victimUsernameColor = victimUsername != null ? $" ([color=gray]{victimUsername}[/color])" : "";
            result += $"\nПервая жертва станции - [color=white]{victimName}[/color]{victimUsernameColor}.";
            result += $"\nВремя смерти - [color=yellow]{_firstMurder.time.ToString("hh\\:mm\\:ss")}[/color].";
            if (_firstMurder.killer != null)
            {
                var killerUsername = TryGetUsername(_firstMurder.killer.Value);
                var killerName = TryGetName(_firstMurder.killer.Value,
                    _statisticEntries[_firstMurder.killer.Value].Name);
                var killerUsernameColor = killerUsername != null ? $" ([color=gray]{killerUsername}[/color])" : "";
                result +=
                    $"\nУбийца - [color=white]{killerName}[/color]{killerUsernameColor}.";
            }
            else
            {
                result += "\nСмерть наступила при неизвестных обстоятельствах.";
            }
        }

        if (totalSlipped >= 1)
        {
            result += $"\nВ этой смене поскользнулись [color=white]{totalSlipped}[/color] раз.";
        }

        if (mostSlippedCharacter != null && maxSlippedCount > 1)
        {
            var username = TryGetUsername(mostSlippedCharacter.Value);
            var name = TryGetName(mostSlippedCharacter.Value,
                _statisticEntries[mostSlippedCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всех раз поскользнулся [color=white]{name}[/color]{usernameColor} - [color=white]{maxSlippedCount}[/color].";
        }

        if (totalCreampied >= 1)
        {
            result += $"\nВсего кремировано игроков: {totalCreampied}.";
        }

        if (mostDeadCharacter != null && maxDeadCount > 1)
        {
            var username = TryGetUsername(mostDeadCharacter.Value);
            var name = TryGetName(mostDeadCharacter.Value,
                _statisticEntries[mostDeadCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего раз умирал [color=white]{name}[/color]{usernameColor}, а именно [color=white]{maxDeadCount}[/color] раз.";
        }

        if (totalDoorEmaged >= 1)
        {
            result += $"\nШлюзы были емагнуты [color=white]{totalDoorEmaged}[/color] раз.";
        }

        if (mostDoorEmagedCharacter != null)
        {
            var username = TryGetUsername(mostDoorEmagedCharacter.Value);
            var name = TryGetName(mostDoorEmagedCharacter.Value,
                _statisticEntries[mostDoorEmagedCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего шлюзов емагнул - [color=white]{name}[/color]{usernameColor} - [color=white]{maxDoorEmagedCount}[/color] раз.";
        }

        if (_jointCreated >= 1)
        {
            result += $"\nБыло скручено [color=white]{_jointCreated}[/color] косяков.";
        }

        if (totalKilledMice >= 1)
        {
            result += $"\nБыло убито [color=white]{totalKilledMice}[/color] мышей.";
        }

        if (mostKillsMiceCharacter != null && maxKillsMice > 1)
        {
            var username = TryGetUsername(mostKillsMiceCharacter.Value);
            var name = TryGetName(mostKillsMiceCharacter.Value,
                _statisticEntries[mostKillsMiceCharacter.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += $"\n{name}[/color]{usernameColor} устроил геноцид, убив [color=white]{maxKillsMice}[/color] мышей.";
        }

        if (_hamsterKiller != null)
        {
            var username = TryGetUsername(_hamsterKiller.Value);
            var name = TryGetName(_hamsterKiller.Value,
                _statisticEntries[_hamsterKiller.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nУбийцей гамлета был [color=white]{name}[/color]{usernameColor}.";
        }

        if (totalCuffedCount >= 1)
        {
            result += $"\nИгроки были закованы [color=white]{totalCuffedCount}[/color] раз.";
        }

        if (playerWithLongestCuffedTime != null)
        {
            var username = TryGetUsername(playerWithLongestCuffedTime.Value);
            var name = TryGetName(playerWithLongestCuffedTime.Value,
                _statisticEntries[playerWithLongestCuffedTime.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего времени в наручниках провёл [color=white]{name}[/color]{usernameColor} - [color=yellow]{maxCuffedTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (totalSleepTime > TimeSpan.Zero)
        {
            result += $"\nОбщее время сна составило [color=yellow]{totalSleepTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (playerWithLongestSleepTime != null)
        {
            var username = TryGetUsername(playerWithLongestSleepTime.Value);
            var name = TryGetName(playerWithLongestSleepTime.Value,
                _statisticEntries[playerWithLongestSleepTime.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += $"\nГлавной соней станции оказался [color=white]{name}[/color]{usernameColor}.";
            result += $"\nОн спал на протяжении [color=yellow]{maxSleepTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (playerWithLongestSpaceTime != null)
        {
            var username = TryGetUsername(playerWithLongestSpaceTime.Value);
            var name = TryGetName(playerWithLongestSpaceTime.Value,
                _statisticEntries[playerWithLongestSpaceTime.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего времени в космосе провел [color=white]{name}[/color]{usernameColor} - [color=yellow]{maxSpaceTime.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (_clownCuffed.clown != null && _clownCuffed.time != null)
        {
            var username = TryGetUsername(_clownCuffed.clown.Value);
            var name = TryGetName(_clownCuffed.clown.Value,
                _statisticEntries[_clownCuffed.clown.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nКлоун [color=white]{name}[/color]{usernameColor} был закован всего спустя [color=yellow]{_clownCuffed.time.Value.ToString("hh\\:mm\\:ss")}[/color].";
        }

        if (totalHeal >= 1)
        {
            result += $"\nВсего было излечено [color=white]{totalHeal}[/color] урона.";
        }

        if (playerWithMostInflictedHeal != null)
        {
            var username = TryGetUsername(playerWithMostInflictedHeal.Value);
            var name = TryGetName(playerWithMostInflictedHeal.Value,
                _statisticEntries[playerWithMostInflictedHeal.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего урона вылечил [color=white]{name}[/color]{usernameColor} - [color=white]{maxInflictedHeal}[/color].";
        }

        if (totalDamage >= 1)
        {
            result += $"\nВсего было получено [color=white]{totalDamage}[/color] урона.";
        }

        if (playerWithMostInflictedDamage != null)
        {
            var username = TryGetUsername(playerWithMostInflictedDamage.Value);
            var name = TryGetName(playerWithMostInflictedDamage.Value,
                _statisticEntries[playerWithMostInflictedDamage.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего урона нанес [color=white]{name}[/color]{usernameColor} - [color=white]{maxInflictedDamage}[/color].";
        }

        if (playerWithMinSpentTk != null)
        {
            var username = TryGetUsername(playerWithMinSpentTk.Value);
            var name = TryGetName(playerWithMinSpentTk.Value,
                _statisticEntries[playerWithMinSpentTk.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nМеньше всего телекристалов потратил [color=white]{name}[/color]{usernameColor} - [color=white]{minSpentTk}[/color]ТК.";
        }

        if (playerWithMaxHumKills != null && maxHumKillCount > 1)
        {
            var username = TryGetUsername(playerWithMaxHumKills.Value);
            var name = TryGetName(playerWithMaxHumKills.Value,
                _statisticEntries[playerWithMaxHumKills.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result += $"\nНастоящим маньяком в этой смене был [color=white]{name}[/color]{usernameColor}.";
            result += $"\nОн убил [color=white]{maxHumKillCount}[/color] гуманоидов.";
        }

        if (playerWithMaxDamage != null)
        {
            var username = TryGetUsername(playerWithMaxDamage.Value);
            var name = TryGetName(playerWithMaxDamage.Value,
                _statisticEntries[playerWithMaxDamage.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего урона получил [color=white]{name}[/color]{usernameColor} - [color=white]{maxTakeDamage}[/color]. Вот бедняга.";
        }

        if (totalAbsorbedPuddle >= 1)
        {
            result += $"\nБыло убрано [color=white]{totalAbsorbedPuddle}[/color] луж.";
        }

        if (playerWithMostPuddleAbsorb != null && maxPuddleAbsorb > 1)
        {
            var username = TryGetUsername(playerWithMostPuddleAbsorb.Value);
            var name = TryGetName(playerWithMostPuddleAbsorb.Value,
                _statisticEntries[playerWithMostPuddleAbsorb.Value].Name);
            var usernameColor = username != null ? $" ([color=gray]{username}[/color])" : "";
            result +=
                $"\nБольше всего луж было убрано благодаря [color=white]{name}[/color]{usernameColor} - [color=white]{maxPuddleAbsorb}[/color].";
        }

        if (totalCaptainCardInteracted >= 1)
        {
            result += $"\nКарта капитана побывала у [color=white]{totalCaptainCardInteracted}[/color] игроков.";
        }

        if (totalElectrocutedCount >= 1)
        {
            result += $"\nИгроки были шокированы [color=white]{totalElectrocutedCount}[/color] раз.";
        }

        result += "\n";

        ev.AddLine(result);
    }

    private string? TryGetUsername(EntityUid uid)
    {
        string? username = null;

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
        {
            username = mind.Session?.Name;
        }

        return username;
    }

    private string TryGetName(EntityUid uid, string savedName)
    {
        return TryComp<MetaDataComponent>(uid, out var metaDataComponent) ? metaDataComponent.EntityName : savedName;
    }
}
