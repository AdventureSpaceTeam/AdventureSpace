﻿using System.Linq;
using Content.Server.Administration;
using Content.Server.Station.Systems;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.Station.Commands;

[ToolshedCommand, AdminCommand(AdminFlags.VarEdit)]
public sealed class JobsCommand : ToolshedCommand
{
    private StationJobsSystem? _jobs;

    [CommandImplementation("jobs")]
    public IEnumerable<JobSlotRef> Jobs([PipedArgument] EntityUid station)
    {
        _jobs ??= GetSys<StationJobsSystem>();

        foreach (var (job, _) in _jobs.GetJobs(station))
        {
            yield return new JobSlotRef(job, station, _jobs, EntityManager);
        }
    }

    [CommandImplementation("jobs")]
    public IEnumerable<JobSlotRef> Jobs([PipedArgument] IEnumerable<EntityUid> stations)
        => stations.SelectMany(Jobs);

    [CommandImplementation("job")]
    public JobSlotRef Job([PipedArgument] EntityUid station, [CommandArgument] Prototype<JobPrototype> job)
    {
        _jobs ??= GetSys<StationJobsSystem>();

        return new JobSlotRef(job.Value.ID, station, _jobs, EntityManager);
    }

    [CommandImplementation("job")]
    public IEnumerable<JobSlotRef> Job([PipedArgument] IEnumerable<EntityUid> stations, [CommandArgument] Prototype<JobPrototype> job)
        => stations.Select(x => Job(x, job));

    [CommandImplementation("isinfinite")]
    public bool IsInfinite([PipedArgument] JobSlotRef job, [CommandInverted] bool inverted)
        => job.Infinite() ^ inverted;

    [CommandImplementation("isinfinite")]
    public IEnumerable<bool> IsInfinite([PipedArgument] IEnumerable<JobSlotRef> jobs, [CommandInverted] bool inverted)
        => jobs.Select(x => IsInfinite(x, inverted));

    [CommandImplementation("adjust")]
    public JobSlotRef Adjust(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] JobSlotRef @ref,
        [CommandArgument] ValueRef<int> by
        )
    {
        _jobs ??= GetSys<StationJobsSystem>();
        _jobs.TryAdjustJobSlot(@ref.Station, @ref.Job, by.Evaluate(ctx), true, true);
        return @ref;
    }

    [CommandImplementation("adjust")]
    public IEnumerable<JobSlotRef> Adjust(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<JobSlotRef> @ref,
        [CommandArgument] ValueRef<int> by
    )
        => @ref.Select(x => Adjust(ctx, x, by));


    [CommandImplementation("set")]
    public JobSlotRef Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] JobSlotRef @ref,
        [CommandArgument] ValueRef<int> by
    )
    {
        _jobs ??= GetSys<StationJobsSystem>();
        _jobs.TrySetJobSlot(@ref.Station, @ref.Job, by.Evaluate(ctx), true);
        return @ref;
    }

    [CommandImplementation("set")]
    public IEnumerable<JobSlotRef> Set(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<JobSlotRef> @ref,
        [CommandArgument] ValueRef<int> by
    )
        => @ref.Select(x => Set(ctx, x, by));

    [CommandImplementation("amount")]
    public int Amount(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] JobSlotRef @ref
    )
    {
        _jobs ??= GetSys<StationJobsSystem>();
        _jobs.TryGetJobSlot(@ref.Station, @ref.Job, out var slots);
        return (int)(slots ?? 0);
    }

    [CommandImplementation("amount")]
    public IEnumerable<int> Amount(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<JobSlotRef> @ref
    )
        => @ref.Select(x => Amount(ctx, x));
}

// Used for Toolshed queries.
public readonly record struct JobSlotRef(string Job, EntityUid Station, StationJobsSystem Jobs, IEntityManager EntityManager)
{
    public override string ToString()
    {
        if (!Jobs.TryGetJobSlot(Station, Job, out var slot))
        {
            return $"{EntityManager.ToPrettyString(Station)} job {Job} : (not a slot)";
        }

        return $"{EntityManager.ToPrettyString(Station)} job {Job} : {slot?.ToString() ?? "infinite"}";
    }

    public bool Infinite()
    {
        return Jobs.TryGetJobSlot(Station, Job, out var slot) && slot is null;
    }
}
