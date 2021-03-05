using System.IO;
using Content.Shared.Alert;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Tests.Shared.Alert
{
    [TestFixture, TestOf(typeof(AlertManager))]
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
        public void TestAlertManager()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            var alertManager = IoCManager.Resolve<AlertManager>();
            alertManager.Initialize();

            Assert.That(alertManager.TryGet(AlertType.LowPressure, out var lowPressure));
            Assert.That(lowPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/lowpressure.png"))));
            Assert.That(alertManager.TryGet(AlertType.HighPressure, out var highPressure));
            Assert.That(highPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/highpressure.png"))));

            Assert.That(alertManager.TryGet(AlertType.LowPressure, out lowPressure));
            Assert.That(lowPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/lowpressure.png"))));
            Assert.That(alertManager.TryGet(AlertType.HighPressure, out highPressure));
            Assert.That(highPressure.Icon, Is.EqualTo(new SpriteSpecifier.Texture(new ResourcePath("/Textures/Interface/Alerts/Pressure/highpressure.png"))));
        }
    }
}
