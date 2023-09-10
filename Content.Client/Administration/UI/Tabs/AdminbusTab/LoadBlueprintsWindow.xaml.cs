using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;

namespace Content.Client.Administration.UI.Tabs.AdminbusTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public sealed partial class LoadBlueprintsWindow : DefaultWindow
    {
        public LoadBlueprintsWindow()
        {
            RobustXamlLoader.Load(this);
        }

        protected override void EnteredTree()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            foreach (var mapId in mapManager.GetAllMapIds())
            {
                MapOptions.AddItem(mapId.ToString(), (int) mapId);
            }

            Reset();

            MapOptions.OnItemSelected += OnOptionSelect;
            RotationSpin.ValueChanged += OnRotate;
            SubmitButton.OnPressed += OnSubmitButtonPressed;
            TeleportButton.OnPressed += OnTeleportButtonPressed;
            ResetButton.OnPressed += OnResetButtonPressed;
        }

        private void Reset()
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            var xformSystem = entManager.System<SharedTransformSystem>();
            var playerManager = IoCManager.Resolve<IPlayerManager>();
            var player = playerManager.LocalPlayer?.ControlledEntity;

            var currentMap = MapId.Nullspace;
            var position = Vector2.Zero;
            var rotation = Angle.Zero;

            if (entManager.TryGetComponent<TransformComponent>(player, out var xform))
            {
                currentMap = xform.MapID;
                position = xformSystem.GetWorldPosition(xform);

                if (entManager.TryGetComponent<TransformComponent>(xform.GridUid, out var gridXform))
                {
                    rotation = xformSystem.GetWorldRotation(gridXform);
                }
                else
                {
                    // MapId moment
                    rotation = xformSystem.GetWorldRotation(xform) - xform.LocalRotation;
                }
            }

            if (currentMap != MapId.Nullspace)
                MapOptions.Select((int) currentMap);

            XCoordinate.Value = (int) position.X;
            YCoordinate.Value = (int) position.Y;

            RotationSpin.OverrideValue(Wraparound((int) rotation.Degrees));
        }

        private void OnResetButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            Reset();
        }

        private void OnRotate(ValueChangedEventArgs e)
        {
            var newValue = Wraparound(e.Value);

            if (e.Value == newValue) return;

            RotationSpin.OverrideValue(newValue);
        }

        private int Wraparound(int value)
        {
            var newValue = (value % 360);
            if (newValue < 0)
                newValue += 360;

            return newValue;
        }

        private void OnOptionSelect(OptionButton.ItemSelectedEventArgs obj)
        {
            MapOptions.SelectId(obj.Id);
        }

        private void OnTeleportButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"tp {XCoordinate.Value} {YCoordinate.Value} {new NetEntity(MapOptions.SelectedId)}");
        }

        private void OnSubmitButtonPressed(BaseButton.ButtonEventArgs obj)
        {
            if (MapPath.Text.Length == 0) return;

            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"loadbp {new NetEntity(MapOptions.SelectedId)} \"{MapPath.Text}\" {XCoordinate.Value} {YCoordinate.Value} {RotationSpin.Value}");
        }
    }
}
