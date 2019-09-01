﻿using System;
using Content.Client.Chat;
using Content.Client.GameObjects.Components.Actor;
using Content.Client.GameTicking;
using Content.Client.Input;
using Content.Client.Interfaces;
using Content.Client.Interfaces.Chat;
using Content.Client.Interfaces.Parallax;
using Content.Client.Parallax;
using Content.Client.UserInterface;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.GameObjects.Components.VendingMachines;
using Content.Shared.Interfaces;
using Robust.Client.Interfaces;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Shared.ContentPack;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client
{
    public class EntryPoint : GameClient
    {
#pragma warning disable 649
        [Dependency] private readonly IPlayerManager _playerManager;
        [Dependency] private readonly IEscapeMenuOwner _escapeMenuOwner;
#pragma warning restore 649

        public override void Init()
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            factory.DoAutoRegistrations();

            var registerIgnore = new[]
            {
                "Breakable",
                "Pickaxe",
                "Interactable",
                "Destructible",
                "Temperature",
                "PowerTransfer",
                "PowerNode",
                "PowerProvider",
                "PowerDevice",
                "PowerStorage",
                "PowerGenerator",
                "Explosive",
                "OnUseTimerTrigger",
                "ToolboxElectricalFill",
                "ToolLockerFill",
                "EmitSoundOnUse",
                "FootstepModifier",
                "HeatResistance",
                "CombatMode",
                "Teleportable",
                "ItemTeleporter",
                "Portal",
                "EntityStorage",
                "PlaceableSurface",
                "Wirecutter",
                "Screwdriver",
                "Multitool",
                "Welder",
                "Wrench",
                "Crowbar",
                "HitscanWeapon",
                "ProjectileWeapon",
                "Projectile",
                "MeleeWeapon",
                "Storeable",
                "Stack",
                "Dice",
                "Construction",
                "Apc",
                "Door",
                "PoweredLight",
                "Smes",
                "Powercell",
                "HandheldLight",
                "LightBulb",
                "Healing",
                "Catwalk",
                "BallisticMagazine",
                "BallisticMagazineWeapon",
                "BallisticBullet",
                "HitscanWeaponCapacitor",
                "PowerCell",
                "AiController",
                "PlayerInputMover",
                "Computer",
                "AsteroidRock",
                "IdCard",
                "Access",
                "AccessReader",
            };

            foreach (var ignoreName in registerIgnore)
            {
                factory.RegisterIgnore(ignoreName);
            }

            factory.Register<SharedLatheComponent>();
            factory.Register<SharedSpawnPointComponent>();
            factory.Register<SolutionComponent>();

            factory.Register<SharedVendingMachineComponent>();
            factory.Register<SharedWiresComponent>();

            prototypes.RegisterIgnore("material");

            IoCManager.Register<IGameHud, GameHud>();
            IoCManager.Register<IClientNotifyManager, ClientNotifyManager>();
            IoCManager.Register<ISharedNotifyManager, ClientNotifyManager>();
            IoCManager.Register<IClientGameTicker, ClientGameTicker>();
            IoCManager.Register<IParallaxManager, ParallaxManager>();
            IoCManager.Register<IChatManager, ChatManager>();
            IoCManager.Register<IEscapeMenuOwner, EscapeMenuOwner>();
            if (TestingCallbacks != null)
            {
                var cast = (ClientModuleTestingCallbacks) TestingCallbacks;
                cast.ClientBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();

            IoCManager.Resolve<IParallaxManager>().LoadParallax();
            IoCManager.Resolve<IBaseClient>().PlayerJoinedServer += SubscribePlayerAttachmentEvents;

            var stylesheet = new NanoStyle();

            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = stylesheet.Stylesheet;
            IoCManager.Resolve<IUserInterfaceManager>().Stylesheet = stylesheet.Stylesheet;

            IoCManager.InjectDependencies(this);

            _escapeMenuOwner.Initialize();
        }

        /// <summary>
        /// Subscribe events to the player manager after the player manager is set up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void SubscribePlayerAttachmentEvents(object sender, EventArgs args)
        {
            _playerManager.LocalPlayer.EntityAttached += AttachPlayerToEntity;
            _playerManager.LocalPlayer.EntityDetached += DetachPlayerFromEntity;
        }

        /// <summary>
        /// Add the character interface master which combines all character interfaces into one window
        /// </summary>
        public static void AttachPlayerToEntity(EntityAttachedEventArgs eventArgs)
        {
            eventArgs.NewEntity.AddComponent<CharacterInterface>();
        }

        /// <summary>
        /// Remove the character interface master from this entity now that we have detached ourselves from it
        /// </summary>
        public static void DetachPlayerFromEntity(EntityDetachedEventArgs eventArgs)
        {
            eventArgs.OldEntity.RemoveComponent<CharacterInterface>();
        }

        public override void PostInit()
        {
            base.PostInit();

            // Setup key contexts
            var inputMan = IoCManager.Resolve<IInputManager>();
            ContentContexts.SetupContexts(inputMan.Contexts);

            IoCManager.Resolve<IGameHud>().Initialize();
            IoCManager.Resolve<IClientNotifyManager>().Initialize();
            IoCManager.Resolve<IClientGameTicker>().Initialize();
            IoCManager.Resolve<IOverlayManager>().AddOverlay(new ParallaxOverlay());
            IoCManager.Resolve<IChatManager>().Initialize();
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.FramePreEngine:
                    IoCManager.Resolve<IClientNotifyManager>().FrameUpdate(frameEventArgs);
                    IoCManager.Resolve<IClientGameTicker>().FrameUpdate(frameEventArgs);
                    IoCManager.Resolve<IChatManager>().FrameUpdate(frameEventArgs);
                    break;
            }
        }
    }
}
