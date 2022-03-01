using Robust.Shared.Utility;
using Robust.Client.Placement;
using Robust.Shared.Map;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Client.Actions;
using Content.Shared.Maps;

namespace Content.Client.Mapping;

public sealed partial class MappingSystem : EntitySystem
{
    [Dependency] private readonly IPlacementManager _placementMan = default!;
    [Dependency] private readonly ITileDefinitionManager _tileMan = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;

    /// <summary>
    ///     The icon to use for space tiles.
    /// </summary>
    private readonly SpriteSpecifier _spaceIcon = new SpriteSpecifier.Texture(new ResourcePath("Tiles/cropped_parallax.png"));

    /// <summary>
    ///     The icon to use for entity-eraser.
    /// </summary>
    private readonly SpriteSpecifier _deleteIcon = new SpriteSpecifier.Texture(new ResourcePath("Interface/VerbIcons/delete.svg.192dpi.png"));

    public string DefaultMappingActions = "/mapping_actions.yml";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FillActionSlotEvent>(OnFillActionSlot);
        SubscribeLocalEvent<StartPlacementActionEvent>(OnStartPlacementAction);
    }

    public void LoadMappingActions()
    {
        _actionsSystem.LoadActionAssignments(DefaultMappingActions, false);
    }

    /// <summary>
    ///     This checks if the placement manager is currently active, and attempts to copy the placement information for
    ///     some entity or tile into an action. This is somewhat janky, but it seem to work well enough. Though I'd
    ///     prefer if it were to function more like DecalPlacementSystem.
    /// </summary>
    private void OnFillActionSlot(FillActionSlotEvent ev)
    {
        if (!_placementMan.IsActive)
            return;

        if (ev.Action != null)
            return;

        var actionEvent = new StartPlacementActionEvent();

        if (_placementMan.CurrentPermission != null)
        {
            actionEvent.EntityType = _placementMan.CurrentPermission.EntityType;
            actionEvent.IsTile = _placementMan.CurrentPermission.IsTile;
            actionEvent.TileType = _placementMan.CurrentPermission.TileType;
            actionEvent.PlacementOption = _placementMan.CurrentPermission.PlacementOption;
        }
        else if (_placementMan.Eraser)
        {
            actionEvent.Eraser = true;
        }
        else
            return;

        if (actionEvent.IsTile)
        {
            var tileDef = _tileMan[actionEvent.TileType];

            if (tileDef is not ContentTileDefinition contentTileDef)
                return;

            var tileIcon = contentTileDef.IsSpace
                ? _spaceIcon
                : new SpriteSpecifier.Texture(new ResourcePath(tileDef.Path) / $"{tileDef.SpriteName}.png");

            ev.Action = new InstantAction()
            {
                CheckCanInteract = false,
                Event = actionEvent,
                Name = tileDef.Name,
                Icon = tileIcon
            };

            return;
        }

        if (actionEvent.Eraser)
        {
            ev.Action = new InstantAction()
            {
                CheckCanInteract = false,
                Event = actionEvent,
                Name = "action-name-mapping-erase",
                Icon = _deleteIcon,
            };

            return;
        }

        if (string.IsNullOrWhiteSpace(actionEvent.EntityType))
            return;

        ev.Action = new InstantAction()
        {
            CheckCanInteract = false,
            Event = actionEvent,
            Name = actionEvent.EntityType,
            Icon = new SpriteSpecifier.EntityPrototype(actionEvent.EntityType),
        };
    }

    private void OnStartPlacementAction(StartPlacementActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _placementMan.BeginPlacing(new()
        {
            EntityType = args.EntityType,
            IsTile = args.IsTile,
            TileType = args.TileType,
            PlacementOption = args.PlacementOption,
        });

        if (_placementMan.Eraser != args.Eraser)
            _placementMan.ToggleEraser();
    }
}

public sealed class StartPlacementActionEvent : PerformActionEvent
{
    [DataField("entityType")]
    public string? EntityType;

    [DataField("isTile")]
    public bool IsTile;

    [DataField("tileType")]
    public ushort TileType;

    [DataField("placementOption")]
    public string? PlacementOption;

    [DataField("eraser")]
    public bool Eraser;
}
