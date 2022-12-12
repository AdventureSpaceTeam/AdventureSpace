﻿using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Paper;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Tools;
using Content.Server.UserInterface;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Systems;
using Content.Shared.Fax;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.Fax;

public sealed class FaxSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ToolSystem _toolSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public const string PaperSlotId = "Paper";

    public override void Initialize()
    {
        base.Initialize();
        
        // Hooks
        SubscribeLocalEvent<FaxMachineComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<FaxMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FaxMachineComponent, ComponentRemove>(OnComponentRemove);
        
        SubscribeLocalEvent<FaxMachineComponent, EntInsertedIntoContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<FaxMachineComponent, EntRemovedFromContainerMessage>(OnItemSlotChanged);
        SubscribeLocalEvent<FaxMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<FaxMachineComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        
        // Interaction
        SubscribeLocalEvent<FaxMachineComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FaxMachineComponent, GotEmaggedEvent>(OnEmagged);
        
        // UI
        SubscribeLocalEvent<FaxMachineComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<FaxMachineComponent, FaxSendMessage>(OnSendButtonPressed);
        SubscribeLocalEvent<FaxMachineComponent, FaxRefreshMessage>(OnRefreshButtonPressed);
        SubscribeLocalEvent<FaxMachineComponent, FaxDestinationMessage>(OnDestinationSelected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (comp, receiver) in EntityQuery<FaxMachineComponent, ApcPowerReceiverComponent>())
        {
            if (!receiver.Powered)
                continue;

            ProcessPrintingAnimation(frameTime, comp);
            ProcessInsertingAnimation(frameTime, comp);
            ProcessSendingTimeout(frameTime, comp);
        }
    }

    private void ProcessPrintingAnimation(float frameTime, FaxMachineComponent comp)
    {
        if (comp.PrintingTimeRemaining > 0)
        {
            comp.PrintingTimeRemaining -= frameTime;
            UpdateAppearance(comp.Owner, comp);

            var isAnimationEnd = comp.PrintingTimeRemaining <= 0;
            if (isAnimationEnd)
            {
                SpawnPaperFromQueue(comp.Owner, comp);
                UpdateUserInterface(comp.Owner, comp);
            }
            
            return;
        }

        if (comp.PrintingQueue.Count > 0)
        {
            comp.PrintingTimeRemaining = comp.PrintingTime;
            _audioSystem.PlayPvs(comp.PrintSound, comp.Owner);
        }
    }

    private void ProcessInsertingAnimation(float frameTime, FaxMachineComponent comp)
    {
        if (comp.InsertingTimeRemaining <= 0)
            return;

        comp.InsertingTimeRemaining -= frameTime;
        UpdateAppearance(comp.Owner, comp);

        var isAnimationEnd = comp.InsertingTimeRemaining <= 0;
        if (isAnimationEnd)
        {
            _itemSlotsSystem.SetLock(comp.Owner, comp.PaperSlot, false);
            UpdateUserInterface(comp.Owner, comp);
        }
    }

    private void ProcessSendingTimeout(float frameTime, FaxMachineComponent comp)
    {
        if (comp.SendTimeoutRemaining > 0)
        {
            comp.SendTimeoutRemaining -= frameTime;

            if (comp.SendTimeoutRemaining <= 0)
                UpdateUserInterface(comp.Owner, comp);
        }
    }

    private void OnComponentInit(EntityUid uid, FaxMachineComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, FaxSystem.PaperSlotId, component.PaperSlot);
        UpdateAppearance(uid, component);
    }

    private void OnComponentRemove(EntityUid uid, FaxMachineComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.PaperSlot);
    }

    private void OnMapInit(EntityUid uid, FaxMachineComponent component, MapInitEvent args)
    {
        // Load all faxes on map in cache each other to prevent taking same name by user created fax
        Refresh(uid, component);
    }

    private void OnItemSlotChanged(EntityUid uid, FaxMachineComponent component, ContainerModifiedMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.PaperSlot.ID)
            return;

        var isPaperInserted = component.PaperSlot.Item.HasValue;
        if (isPaperInserted)
        {
            component.InsertingTimeRemaining = component.InsertionTime;
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, true);
        }

        UpdateUserInterface(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, FaxMachineComponent component, ref PowerChangedEvent args)
    {
        var isInsertInterrupted = !args.Powered && component.InsertingTimeRemaining > 0;
        if (isInsertInterrupted)
        {
            component.InsertingTimeRemaining = 0f; // Reset animation

            // Drop from slot because animation did not play completely
            _itemSlotsSystem.SetLock(uid, component.PaperSlot, false);
            _itemSlotsSystem.TryEject(uid, component.PaperSlot, null, out var _, true);
        }

        var isPrintInterrupted = !args.Powered && component.PrintingTimeRemaining > 0;
        if (isPrintInterrupted)
        {
            component.PrintingTimeRemaining = 0f; // Reset animation
        }
        
        if (isInsertInterrupted || isPrintInterrupted)
            UpdateAppearance(component.Owner, component);

        _itemSlotsSystem.SetLock(uid, component.PaperSlot, !args.Powered); // Lock slot when power is off
    }
    
    private void OnInteractUsing(EntityUid uid, FaxMachineComponent component, InteractUsingEvent args)
    {
        if (args.Handled ||
            !TryComp<ActorComponent>(args.User, out var actor) ||
            !_toolSystem.HasQuality(args.Used, "Screwing")) // Screwing because Pulsing already used by device linking
            return;

        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("fax-machine-dialog-rename"),
            Loc.GetString("fax-machine-dialog-field-name"),
            (string newName) =>
        {
            if (component.FaxName == newName)
                return;

            if (newName.Length > 20)
            {
                _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-long"), uid, Filter.Pvs(uid));
                return;
            }

            if (component.KnownFaxes.ContainsValue(newName) && !component.Emagged) // Allow exist names if emagged for fun
            {
                _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-exist"), uid, Filter.Pvs(uid));
                return;
            }

            component.FaxName = newName;
            _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-name-set"), uid, Filter.Pvs(uid));
            UpdateUserInterface(uid, component);
        });
        
        args.Handled = true;
    }
    
    private void OnEmagged(EntityUid uid, FaxMachineComponent component, GotEmaggedEvent args)
    {
        if (component.Emagged)
            return;

        _audioSystem.PlayPvs(component.EmagSound, uid);
        component.Emagged = true;
        args.Handled = true;
    }

    private void OnPacketReceived(EntityUid uid, FaxMachineComponent component, DeviceNetworkPacketEvent args)
    {
        if (!HasComp<DeviceNetworkComponent>(uid) || string.IsNullOrEmpty(args.SenderAddress))
            return;

        if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
        {
            switch (command)
            {
                case FaxConstants.FaxPingCommand:
                    var isForSyndie = component.Emagged &&
                                      args.Data.ContainsKey(FaxConstants.FaxSyndicateData);
                    if (!isForSyndie && !component.ResponsePings)
                        return;

                    var payload = new NetworkPayload()
                    {
                        { DeviceNetworkConstants.Command, FaxConstants.FaxPongCommand },
                        { FaxConstants.FaxNameData, component.FaxName }
                    };
                    _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, payload);

                    break;
                case FaxConstants.FaxPongCommand:
                    if (!args.Data.TryGetValue(FaxConstants.FaxNameData, out string? faxName))
                        return;

                    component.KnownFaxes[args.SenderAddress] = faxName;

                    UpdateUserInterface(uid, component);

                    break;
                case FaxConstants.FaxPrintCommand:
                    if (!args.Data.TryGetValue(FaxConstants.FaxPaperNameData, out string? name) ||
                        !args.Data.TryGetValue(FaxConstants.FaxPaperContentData, out string? content))
                        return;

                    var printout = new FaxPrintout(content, name);
                    Receive(uid, printout, args.SenderAddress);

                    break;
            }
        }
    }
    
    private void OnToggleInterface(EntityUid uid, FaxMachineComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }
    
    private void OnSendButtonPressed(EntityUid uid, FaxMachineComponent component, FaxSendMessage args)
    {
        Send(uid, component);
    }
    
    private void OnRefreshButtonPressed(EntityUid uid, FaxMachineComponent component, FaxRefreshMessage args)
    {
        Refresh(uid, component);
    }
    
    private void OnDestinationSelected(EntityUid uid, FaxMachineComponent component, FaxDestinationMessage args)
    {
        SetDestination(uid, args.Address, component);
    }

    private void UpdateAppearance(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.InsertingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Inserting);
        else if (component.PrintingTimeRemaining > 0)
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);
        else
            _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Normal);
    }

    private void UpdateUserInterface(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var isPaperInserted = component.PaperSlot.Item != null;
        var canSend = isPaperInserted &&
                      component.DestinationFaxAddress != null &&
                      component.SendTimeoutRemaining <= 0 &&
                      component.InsertingTimeRemaining <= 0;
        var state = new FaxUiState(component.FaxName, component.KnownFaxes, canSend, isPaperInserted, component.DestinationFaxAddress);
        _userInterface.TrySetUiState(uid, FaxUiKey.Key, state);
    }

    /// <summary>
    ///     Set fax destination address not checking if he knows it exists
    /// </summary>
    public void SetDestination(EntityUid uid, string destAddress, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxAddress = destAddress;

        UpdateUserInterface(uid, component);
    }

    /// <summary>
    ///     Clears current known fax info and make network scan ping
    ///     Adds special data to  payload if it was emagged to identify itself as a Syndicate
    /// </summary>
    public void Refresh(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.DestinationFaxAddress = null;
        component.KnownFaxes.Clear();
        
        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxConstants.FaxPingCommand }
        };

        if (component.Emagged)
            payload.Add(FaxConstants.FaxSyndicateData, true);

        _deviceNetworkSystem.QueuePacket(uid, null, payload);
    }

    /// <summary>
    ///     Sends message to addressee if paper is set and a known fax is selected
    ///     A timeout is set after sending
    /// </summary>
    public void Send(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var sendEntity = component.PaperSlot.Item;
        if (sendEntity == null)
            return;

        if (component.DestinationFaxAddress == null)
            return;
        
        if (!component.KnownFaxes.TryGetValue(component.DestinationFaxAddress, out var faxName))
            return;

        if (!TryComp<MetaDataComponent>(sendEntity, out var metadata) ||
            !TryComp<PaperComponent>(sendEntity, out var paper))
            return;

        var payload = new NetworkPayload()
        {
            { DeviceNetworkConstants.Command, FaxConstants.FaxPrintCommand },
            { FaxConstants.FaxPaperNameData, metadata.EntityName },
            { FaxConstants.FaxPaperContentData, paper.Content },
        };
        _deviceNetworkSystem.QueuePacket(uid, component.DestinationFaxAddress, payload);

        component.SendTimeoutRemaining += component.SendTimeout;
        
        _audioSystem.PlayPvs(component.SendSound, uid);
        
        UpdateUserInterface(uid, component);
    }

    /// <summary>
    ///     Accepts a new message and adds it to the queue to print
    ///     If has parameter "notifyAdmins" also output a special message to admin chat.
    /// </summary>
    public void Receive(EntityUid uid, FaxPrintout printout, string? fromAddress, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var faxName = Loc.GetString("fax-machine-popup-source-unknown");
        if (fromAddress != null && component.KnownFaxes.ContainsKey(fromAddress)) // If message received from unknown for fax address
            faxName = component.KnownFaxes[fromAddress];

        _popupSystem.PopupEntity(Loc.GetString("fax-machine-popup-received", ("from", faxName)), uid, Filter.Pvs(uid));
        _appearanceSystem.SetData(uid, FaxMachineVisuals.VisualState, FaxMachineVisualState.Printing);

        if (component.NotifyAdmins)
            NotifyAdmins(faxName);

        component.PrintingQueue.Enqueue(printout);
    }

    private void SpawnPaperFromQueue(EntityUid uid, FaxMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component) || component.PrintingQueue.Count == 0)
            return;

        var printout = component.PrintingQueue.Dequeue();
        var printed = EntityManager.SpawnEntity("Paper", Transform(uid).Coordinates);

        if (TryComp<PaperComponent>(printed, out var paper))
            _paperSystem.SetContent(printed, printout.Content);

        if (TryComp<MetaDataComponent>(printed, out var metadata))
            metadata.EntityName = printout.Name;
    }

    private void NotifyAdmins(string faxName)
    {
        _chat.SendAdminAnnouncement(Loc.GetString("fax-machine-chat-notify", ("fax", faxName)));
        _audioSystem.PlayGlobal("/Audio/Machines/high_tech_confirm.ogg", Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false);
    }
}
