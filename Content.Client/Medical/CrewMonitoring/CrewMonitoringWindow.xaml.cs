using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Shared.Medical.SuitSensor;
using Robust.Client.AutoGenerated;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Medical.CrewMonitoring
{
    [GenerateTypedNameReferences]
    public sealed partial class CrewMonitoringWindow : FancyWindow
    {
        private List<Control> _rowsContent = new();
        private List<(DirectionIcon Icon, Vector2 Position)> _directionIcons = new();
        private readonly IEntityManager _entManager;
        private readonly IEyeManager _eye;
        private EntityUid? _stationUid;

        public static int IconSize = 16; // XAML has a `VSeparationOverride` of 20 for each row.

        public CrewMonitoringWindow(EntityUid? mapUid)
        {
            RobustXamlLoader.Load(this);
            _eye = IoCManager.Resolve<IEyeManager>();
            _entManager = IoCManager.Resolve<IEntityManager>();
            _stationUid = mapUid;

            if (_entManager.TryGetComponent<TransformComponent>(mapUid, out var xform))
            {
                NavMap.MapUid = xform.GridUid;
            }
            else
            {
                NavMap.Visible = false;
                SetSize = new Vector2(775, 400);
                MinSize = SetSize;
            }
        }

        public void ShowSensors(List<SuitSensorStatus> stSensors, Vector2 localPosition, bool snap, float precision)
        {
            ClearAllSensors();

            // TODO scroll container
            // TODO filter by name & occupation
            // TODO make each row a xaml-control. Get rid of some of this c# control creation.

            // add a row for each sensor
            foreach (var sensor in stSensors.OrderBy(a => a.Name))
            {
                // add users name
                // format: UserName
                var nameLabel = new PanelContainer()
                {
                    PanelOverride = new StyleBoxFlat()
                    {
                        BackgroundColor = StyleNano.ButtonColorDisabled,
                    },
                    Children =
                    {
                        new Label()
                        {
                            Text = sensor.Name,
                            Margin = new Thickness(5f, 5f),
                        }
                    }
                };

                // add users job
                // format: JobName
                var jobLabel = new Label()
                {
                    Text = sensor.Job,
                    HorizontalExpand = true
                };

                SensorsTable.AddChild(nameLabel);
                _rowsContent.Add(nameLabel);
                SensorsTable.AddChild(jobLabel);
                _rowsContent.Add(jobLabel);

                // add users status and damage
                // format: IsAlive (TotalDamage)
                var statusText = Loc.GetString(sensor.IsAlive ?
                    "crew-monitoring-user-interface-alive" :
                    "crew-monitoring-user-interface-dead");
                if (sensor.TotalDamage != null)
                {
                    statusText += $" ({sensor.TotalDamage})";
                }
                var statusLabel = new Label()
                {
                    Text = statusText
                };
                SensorsTable.AddChild(statusLabel);
                _rowsContent.Add(statusLabel);

                // add users positions
                // format: (x, y)
                var box = GetPositionBox(sensor.Coordinates, localPosition, snap, precision);

                SensorsTable.AddChild(box);
                _rowsContent.Add(box);

                if (sensor.Coordinates != null && NavMap.Visible)
                {
                    NavMap.TrackedCoordinates.Add(sensor.Coordinates.Value, (true, Color.FromHex("#B02E26")));
                    nameLabel.MouseFilter = MouseFilterMode.Stop;

                    // Hide all others upon mouseover.
                    nameLabel.OnMouseEntered += args =>
                    {
                        foreach (var (coord, value) in NavMap.TrackedCoordinates)
                        {
                            if (coord == sensor.Coordinates)
                                continue;

                            NavMap.TrackedCoordinates[coord] = (false, value.Color);
                        }
                    };

                    nameLabel.OnMouseExited += args =>
                    {
                        foreach (var (coord, value) in NavMap.TrackedCoordinates)
                        {
                            NavMap.TrackedCoordinates[coord] = (true, value.Color);
                        }
                    };
                }
            }
        }

        private BoxContainer GetPositionBox(EntityCoordinates? coordinates, Vector2 sensorPosition, bool snap, float precision)
        {
            var box = new BoxContainer() { Orientation = LayoutOrientation.Horizontal };

            if (coordinates == null || !_entManager.TryGetComponent<TransformComponent>(_stationUid, out var xform))
            {
                var dirIcon = new DirectionIcon()
                {
                    SetSize = (IconSize, IconSize),
                    Margin = new(0, 0, 4, 0)
                };
                box.AddChild(dirIcon);
                box.AddChild(new Label() { Text = Loc.GetString("crew-monitoring-user-interface-no-info") });
            }
            else
            {
                var position = coordinates.Value.ToMapPos(_entManager);
                var local = xform.InvWorldMatrix.Transform(position);

                var displayPos = local.Floored();
                var dirIcon = new DirectionIcon(snap, precision)
                {
                    SetSize = (IconSize, IconSize),
                    Margin = new(0, 0, 4, 0)
                };
                box.AddChild(dirIcon);
                box.AddChild(new Label() { Text = displayPos.ToString() });
                _directionIcons.Add((dirIcon, local - sensorPosition));
            }

            return box;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            // the window is separate from any specific viewport, so there is no real way to get an eye-rotation without
            // using IEyeManager. Eventually this will have to be reworked for a station AI with multi-viewports.

            foreach (var (icon, pos) in _directionIcons)
            {
                icon.UpdateDirection(pos, -_eye.CurrentEye.Rotation);
            }
        }

        private void ClearAllSensors()
        {
            foreach (var child in _rowsContent)
            {
                SensorsTable.RemoveChild(child);
            }

            _rowsContent.Clear();
            NavMap.TrackedCoordinates.Clear();
        }
    }
}
