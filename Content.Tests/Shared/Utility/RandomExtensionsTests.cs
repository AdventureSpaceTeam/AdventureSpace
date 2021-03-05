﻿using System.IO;
using Content.Shared.Prototypes;
using Content.Shared.Utility;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Content.Tests.Shared.Utility
{
    [TestFixture]
    [TestOf(typeof(SharedRandomExtensions))]
    public class RandomExtensionsTests : ContentUnitTest
    {
        private const string TestDatasetId = "TestDataset";

        private static readonly string Prototypes = $@"
- type: dataset
  id: {TestDatasetId}
  values:
  - A";

        [Test]
        public void RandomDataSetValueTest()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            prototypeManager.LoadFromStream(new StringReader(Prototypes));

            var dataSet = prototypeManager.Index<DatasetPrototype>(TestDatasetId);
            var random = IoCManager.Resolve<IRobustRandom>();
            var id = random.Pick(dataSet);

            Assert.NotNull(id);
        }
    }
}
