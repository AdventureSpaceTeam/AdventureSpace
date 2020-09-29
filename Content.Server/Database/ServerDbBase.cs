﻿#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Maths;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public abstract class ServerDbBase
    {
        public async Task<PlayerPreferences?> GetPlayerPreferencesAsync(NetUserId userId)
        {
            await using var db = await GetDb();

            var prefs = await db.DbContext
                .Preference
                .Include(p => p.Profiles).ThenInclude(h => h.Jobs)
                .Include(p => p.Profiles).ThenInclude(h => h.Antags)
                .SingleOrDefaultAsync(p => p.UserId == userId.UserId);

            if (prefs is null) return null;

            var maxSlot = prefs.Profiles.Max(p => p.Slot)+1;
            var profiles = new ICharacterProfile[maxSlot];
            foreach (var profile in prefs.Profiles)
            {
                profiles[profile.Slot] = ConvertProfiles(profile);
            }

            return new PlayerPreferences
            (
                profiles,
                prefs.SelectedCharacterSlot
            );
        }

        public async Task SaveSelectedCharacterIndexAsync(NetUserId userId, int index)
        {
            await using var db = await GetDb();

            var prefs = await db.DbContext.Preference.SingleAsync(p => p.UserId == userId.UserId);
            prefs.SelectedCharacterSlot = index;

            await db.DbContext.SaveChangesAsync();
        }

        public async Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
        {
            if (profile is null)
            {
                await DeleteCharacterSlotAsync(userId, slot);
                return;
            }

            await using var db = await GetDb();
            if (!(profile is HumanoidCharacterProfile humanoid))
            {
                // TODO: Handle other ICharacterProfile implementations properly
                throw new NotImplementedException();
            }

            var entity = ConvertProfiles(humanoid, slot);

            var prefs = await db.DbContext
                .Preference
                .Include(p => p.Profiles)
                .SingleAsync(p => p.UserId == userId.UserId);

            var oldProfile = prefs
                .Profiles
                .SingleOrDefault(h => h.Slot == entity.Slot);

            if (!(oldProfile is null))
            {
                prefs.Profiles.Remove(oldProfile);
            }

            prefs.Profiles.Add(entity);
            await db.DbContext.SaveChangesAsync();
        }

        private async Task DeleteCharacterSlotAsync(NetUserId userId, int slot)
        {
            await using var db = await GetDb();

            db.DbContext
                .Preference
                .Single(p => p.UserId == userId.UserId)
                .Profiles
                .RemoveAll(h => h.Slot == slot);

            await db.DbContext.SaveChangesAsync();
        }

        public async Task<PlayerPreferences> InitPrefsAsync(NetUserId userId, ICharacterProfile defaultProfile)
        {
            await using var db = await GetDb();

            var profile = ConvertProfiles((HumanoidCharacterProfile) defaultProfile, 0);
            var prefs = new Preference
            {
                UserId = userId.UserId,
                SelectedCharacterSlot = 0
            };

            prefs.Profiles.Add(profile);

            db.DbContext.Preference.Add(prefs);

            await db.DbContext.SaveChangesAsync();

            return new PlayerPreferences(new []{defaultProfile}, 0);
        }

        private static HumanoidCharacterProfile ConvertProfiles(Profile profile)
        {
            var jobs = profile.Jobs.ToDictionary(j => j.JobName, j => (JobPriority) j.Priority);
            var antags = profile.Antags.Select(a => a.AntagName);
            return new HumanoidCharacterProfile(
                profile.CharacterName,
                profile.Age,
                profile.Sex == "Male" ? Sex.Male : Sex.Female,
                new HumanoidCharacterAppearance
                (
                    profile.HairName,
                    Color.FromHex(profile.HairColor),
                    profile.FacialHairName,
                    Color.FromHex(profile.FacialHairColor),
                    Color.FromHex(profile.EyeColor),
                    Color.FromHex(profile.SkinColor)
                ),
                jobs,
                (PreferenceUnavailableMode) profile.PreferenceUnavailable,
                antags.ToList()
            );
        }

        private static Profile ConvertProfiles(HumanoidCharacterProfile humanoid, int slot)
        {
            var appearance = (HumanoidCharacterAppearance) humanoid.CharacterAppearance;

            var entity = new Profile
            {
                CharacterName = humanoid.Name,
                Age = humanoid.Age,
                Sex = humanoid.Sex.ToString(),
                HairName = appearance.HairStyleName,
                HairColor = appearance.HairColor.ToHex(),
                FacialHairName = appearance.FacialHairStyleName,
                FacialHairColor = appearance.FacialHairColor.ToHex(),
                EyeColor = appearance.EyeColor.ToHex(),
                SkinColor = appearance.SkinColor.ToHex(),
                Slot = slot,
                PreferenceUnavailable = (DbPreferenceUnavailableMode) humanoid.PreferenceUnavailable
            };
            entity.Jobs.AddRange(
                humanoid.JobPriorities
                    .Where(j => j.Value != JobPriority.Never)
                    .Select(j => new Job {JobName = j.Key, Priority = (DbJobPriority) j.Value})
            );
            entity.Antags.AddRange(
                humanoid.AntagPreferences
                    .Select(a => new Antag {AntagName = a})
            );

            return entity;
        }

        public async Task<NetUserId?> GetAssignedUserIdAsync(string name)
        {
            await using var db = await GetDb();

            var assigned = await db.DbContext.AssignedUserId.SingleOrDefaultAsync(p => p.UserName == name);
            return assigned?.UserId is { } g ? new NetUserId(g) : default(NetUserId?);
        }

        public async Task AssignUserIdAsync(string name, NetUserId netUserId)
        {
            await using var db = await GetDb();

            db.DbContext.AssignedUserId.Add(new AssignedUserId
            {
                UserId = netUserId.UserId,
                UserName = name
            });

            await db.DbContext.SaveChangesAsync();
        }

        /*
         * BAN STUFF
         */
        public abstract Task<ServerBanDef?> GetServerBanAsync(IPAddress? address, NetUserId? userId);
        public abstract Task AddServerBanAsync(ServerBanDef serverBan);

        /*
         * PLAYER RECORDS
         */
        public abstract Task UpdatePlayerRecord(NetUserId userId, string userName, IPAddress address);

        /*
         * CONNECTION LOG
         */
        public abstract Task AddConnectionLogAsync(NetUserId userId, string userName, IPAddress address);


        protected abstract Task<DbGuard> GetDb();

        protected abstract class DbGuard : IAsyncDisposable
        {
            public abstract ServerDbContext DbContext { get; }

            public abstract ValueTask DisposeAsync();
        }

    }
}
