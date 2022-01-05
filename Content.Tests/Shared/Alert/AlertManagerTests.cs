using System.IO;
using Content.Shared.Alert;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertsSystem))]
    public class AlertManagerTests : ContentUnitTest
    {
        const string PROTOTYPES = @"
- type: alert
  name: AlertLowPressure
  alertType: LowPressure
  icon: /Textures/Interface/Alerts/Pressure/lowpressure.png

- type: alert
  name: AlertHighPressure
  alertType: HighPressure
  icon: /Textures/Interface/Alerts/Pressure/highpressure.png
";

        [Test]
        [Ignore("There is no way to load extra Systems in a unit test, fixing RobustUnitTest is out of scope.")]
        public void TestAlertManager()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();

            var reflection = IoCManager.Resolve<IReflectionManager>();
            reflection.LoadAssemblies();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.Initialize();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));

            Assert.That(EntitySystem.Get<AlertsSystem>().TryGet(AlertType.LowPressure, out var lowPressure));
            Assert.That(lowPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/lowpressure.png"))));
            Assert.That(EntitySystem.Get<AlertsSystem>().TryGet(AlertType.HighPressure, out var highPressure));
            Assert.That(highPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/highpressure.png"))));

            Assert.That(EntitySystem.Get<AlertsSystem>().TryGet(AlertType.LowPressure, out lowPressure));
            Assert.That(lowPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/lowpressure.png"))));
            Assert.That(EntitySystem.Get<AlertsSystem>().TryGet(AlertType.HighPressure, out highPressure));
            Assert.That(highPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/highpressure.png"))));
        }
    }
}
