using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared;
using Content.Shared.Preferences;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.UnitTesting;

namespace Content.Tests.Server.Preferences
{
    [TestFixture]
    public class ServerDbSqliteTests : RobustUnitTest
    {
        private const int MaxCharacterSlots = 10;

        private static HumanoidCharacterProfile CharlieCharlieson()
        {
            return new HumanoidCharacterProfile(
                "Charlie Charlieson",
                21,
                Sex.Male,
                new HumanoidCharacterAppearance(
                    "Afro",
                    Color.Aqua,
                    "Shaved",
                    Color.Aquamarine,
                    Color.Azure,
                    Color.Beige
                ),
                new Dictionary<string, JobPriority>
                {
                    {SharedGameTicker.OverflowJob, JobPriority.High}
                },
                PreferenceUnavailableMode.StayInLobby,
                new List<string> ()
            );
        }

        private static ServerDbSqlite GetDb()
        {
            var builder = new DbContextOptionsBuilder<ServerDbContext>();
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            builder.UseSqlite(conn);
            return new ServerDbSqlite(builder.Options);
        }

        [Test]
        public async Task TestUserDoesNotExist()
        {
            var db = GetDb();
            // Database should be empty so a new GUID should do it.
            Assert.Null(await db.GetPlayerPreferencesAsync(NewUserId()));
        }

        [Test]
        public async Task TestInitPrefs()
        {
            var db = GetDb();
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            const int slot = 0;
            var originalProfile = CharlieCharlieson();
            await db.InitPrefsAsync(username, originalProfile);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.ElementAt(slot).MemberwiseEquals(originalProfile));
        }

        [Test]
        public async Task TestDeleteCharacter()
        {
            var db = GetDb();
            var username = new NetUserId(new Guid("640bd619-fc8d-4fe2-bf3c-4a5fb17d6ddd"));
            await db.InitPrefsAsync(username, HumanoidCharacterProfile.Default());
            await db.SaveCharacterSlotAsync(username, CharlieCharlieson(), 1);
            await db.SaveSelectedCharacterIndexAsync(username, 1);
            await db.SaveCharacterSlotAsync(username, null, 1);
            var prefs = await db.GetPlayerPreferencesAsync(username);
            Assert.That(prefs.Characters.Skip(1).All(character => character is null));
        }

        private static NetUserId NewUserId()
        {
            return new NetUserId(Guid.NewGuid());
        }
    }
}
