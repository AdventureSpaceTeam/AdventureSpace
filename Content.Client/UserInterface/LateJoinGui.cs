using System;
using System.Collections.Generic;
using System.Linq;
using Content.Client.Interfaces;
using Content.Shared.Roles;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface
{
    public sealed class LateJoinGui : SS14Window
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IClientConsole _console = default!;
        [Dependency] private readonly IClientGameTicker _gameTicker = default!;

        protected override Vector2? CustomSize => (360, 560);

        public event Action<string> SelectedId;

        private readonly Dictionary<string, JobButton> _jobButtons = new();

        public LateJoinGui()
        {
            IoCManager.InjectDependencies(this);

            Title = Loc.GetString("Late Join");

            var jobList = new VBoxContainer();
            var vBox = new VBoxContainer
            {
                    Children =
                    {
                        new ScrollContainer
                        {
                            SizeFlagsVertical = SizeFlags.FillExpand,
                            Children =
                            {
                                jobList
                            }
                        }
                    }
                };
            Contents.AddChild(vBox);

            foreach (var job in _prototypeManager.EnumeratePrototypes<JobPrototype>().OrderBy(j => j.Name))
            {
                var jobButton = new JobButton
                {
                    JobId = job.ID
                };

                var jobSelector = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.FillExpand
                };

                var icon = new TextureRect
                {
                    TextureScale = (2, 2),
                    Stretch = TextureRect.StretchMode.KeepCentered
                };

                if (job.Icon != null)
                {
                    var specifier = new SpriteSpecifier.Rsi(new ResourcePath("/Textures/Interface/Misc/job_icons.rsi"), job.Icon);
                    icon.Texture = specifier.Frame0();
                }
                jobSelector.AddChild(icon);

                var jobLabel = new Label
                {
                    Text = job.Name
                };

                jobSelector.AddChild(jobLabel);

                jobButton.AddChild(jobSelector);
                jobList.AddChild(jobButton);
                jobButton.OnPressed += args =>
                {
                    SelectedId?.Invoke(jobButton.JobId);
                };

                if (!_gameTicker.JobsAvailable.Contains(job.ID))
                {
                    jobButton.Disabled = true;
                }

                _jobButtons[job.ID] = jobButton;
            }

            SelectedId += jobId =>
            {
                Logger.InfoS("latejoin", $"Late joining as ID: {jobId}");
                _console.ProcessCommand($"joingame {CommandParsing.Escape(jobId)}");
                Close();
            };

            _gameTicker.LobbyJobsAvailableUpdated += JobsAvailableUpdated;
        }

        public string ReturnId()
        {
            return SelectedId.ToString();
        }

        private void JobsAvailableUpdated(IReadOnlyList<string> jobs)
        {
            foreach (var (id, button) in _jobButtons)
            {
                button.Disabled = !jobs.Contains(id);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _gameTicker.LobbyJobsAvailableUpdated -= JobsAvailableUpdated;
                _jobButtons.Clear();
            }
        }
    }

    class JobButton : ContainerButton
    {
        public string JobId { get; set; }
        public JobButton()
        {
            AddStyleClass(StyleClassButton);
        }
    }
}
