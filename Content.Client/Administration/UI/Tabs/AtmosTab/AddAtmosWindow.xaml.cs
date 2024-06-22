using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map.Components;

namespace Content.Client.Administration.UI.Tabs.AtmosTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public sealed partial class AddAtmosWindow : DefaultWindow
    {
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private readonly List<Entity<MapGridComponent>> _data = new();

        public AddAtmosWindow()
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
        }

        protected override void EnteredTree()
        {
            _data.Clear();

            var player = _players.LocalEntity;
            var playerGrid = _entities.GetComponentOrNull<TransformComponent>(player)?.GridUid;
            var query = IoCManager.Resolve<IEntityManager>().AllEntityQueryEnumerator<MapGridComponent>();

            while (query.MoveNext(out var uid, out var grid))
            {
                _data.Add((uid, grid));
                GridOptions.AddItem($"{uid} {(playerGrid == uid ? Loc.GetString($"admin-ui-atmos-grid-current") : "")}");
            }

            GridOptions.OnItemSelected += eventArgs => GridOptions.SelectId(eventArgs.Id);
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            var selectedGrid = _data[GridOptions.SelectedId].Owner;
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand($"addatmos {_entities.GetNetEntity(selectedGrid)}");
        }
    }
}
