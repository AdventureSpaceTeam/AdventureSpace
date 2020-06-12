using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class RandomPottedPlantComponent : Component, IMapInit
    {
        public override string Name => "RandomPottedPlant";

        private static readonly string[] RegularPlantStates;
        private static readonly string[] PlasticPlantStates;

        private string _selectedState;
        private bool _plastic;

        // for shared string dict, since we don't define these anywhere in content
        [UsedImplicitly]
        public static readonly string[] plantIdStrings =
        {
            "plant-01", "plant-02", "plant-03", "plant-04", "plant-05",
            "plant-06", "plant-07", "plant-08", "plant-09", "plant-10",
            "plant-11", "plant-12", "plant-13", "plant-14", "plant-15",
            "plant-16", "plant-17", "plant-18", "plant-19", "plant-20",
            "plant-21", "plant-22", "plant-23", "plant-24", "plant-25",
            "plant-26", "plant-27", "plant-28", "plant-29", "plant-30",
        };

        static RandomPottedPlantComponent()
        {
            // ReSharper disable once StringLiteralTypo
            var states = new List<string> {"applebush"};

            for (var i = 1; i < 25; i++)
            {
                states.Add($"plant-{i:D2}");
            }

            RegularPlantStates = states.ToArray();

            states.Clear();

            for (var i = 26; i < 30; i++)
            {
                states.Add($"plant-{i:D2}");
            }

            PlasticPlantStates = states.ToArray();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _selectedState, "selected", null);
            serializer.DataField(ref _plastic, "plastic", false);
        }

        protected override void Startup()
        {
            base.Startup();

            if (_selectedState != null)
            {
                Owner.GetComponent<SpriteComponent>().LayerSetState(0, _selectedState);
            }
        }

        public void MapInit()
        {
            var random = IoCManager.Resolve<IRobustRandom>();

            var list = _plastic ? PlasticPlantStates : RegularPlantStates;
            _selectedState = random.Pick(list);

            Owner.GetComponent<SpriteComponent>().LayerSetState(0, _selectedState);
        }
    }
}
