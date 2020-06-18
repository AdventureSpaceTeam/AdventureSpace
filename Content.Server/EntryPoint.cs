﻿using Content.Server.Interfaces;
﻿using Content.Server.AI.WorldState;
using Content.Server.Interfaces.Chat;
using Content.Server.Interfaces.GameTicking;
using Content.Server.Interfaces.PDA;
using Content.Server.Sandbox;
using Content.Shared.Kitchen;
using Robust.Server.Interfaces.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server
{
    public class EntryPoint : GameServer
    {
        private IGameTicker _gameTicker;
        private StatusShell _statusShell;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            var factory = IoCManager.Resolve<IComponentFactory>();

            factory.DoAutoRegistrations();

            var registerIgnore = new[]
            {
                "ConstructionGhost",
                "IconSmooth",
                "SubFloorHide",
                "LowWall",
                "ReinforcedWall",
                "Window",
                "CharacterInfo",
                "InteractionOutline",
                "MeleeWeaponArcAnimation",
                "AnimationsTest",
                "ItemStatus",
                "Marker",
                "EmergencyLight",
                "Clickable",
            };

            foreach (var ignoreName in registerIgnore)
            {
                factory.RegisterIgnore(ignoreName);
            }

            ServerContentIoC.Register();

            if (TestingCallbacks != null)
            {
                var cast = (ServerModuleTestingCallbacks) TestingCallbacks;
                cast.ServerBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();

            _gameTicker = IoCManager.Resolve<IGameTicker>();

            IoCManager.Resolve<IServerNotifyManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();

            var playerManager = IoCManager.Resolve<IPlayerManager>();

            _statusShell = new StatusShell();

            var logManager = IoCManager.Resolve<ILogManager>();
            logManager.GetSawmill("Storage").Level = LogLevel.Info;

            IoCManager.Resolve<IServerPreferencesManager>().StartInit();

        }

        public override void PostInit()
        {
            base.PostInit();

            _gameTicker.Initialize();
            IoCManager.Resolve<ISandboxManager>().Initialize();
            IoCManager.Resolve<IServerPreferencesManager>().FinishInit();
            IoCManager.Resolve<RecipeManager>().Initialize();
            IoCManager.Resolve<BlackboardManager>().Initialize();
            IoCManager.Resolve<IPDAUplinkManager>().Initialize();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.PreEngine:
                {
                    _gameTicker.Update(frameEventArgs);
                    break;
                }
            }
        }
    }
}
