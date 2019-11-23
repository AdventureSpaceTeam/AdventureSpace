﻿using System.IO;
using Content.Shared.Chemistry;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Tests.Shared.Chemistry
{
    [TestFixture, TestOf(typeof(ReagentPrototype))]
    public class ReagentPrototype_Tests : ContentUnitTest
    {
        [Test]
        public void DeserializeReagentPrototype()
        {
            using (TextReader stream = new StringReader(YamlReagentPrototype))
            {
                var yamlStream = new YamlStream();
                yamlStream.Load(stream);
                var document = yamlStream.Documents[0];
                var rootNode = (YamlSequenceNode)document.RootNode;
                var proto = (YamlMappingNode)rootNode[0];

                var defType = proto.GetNode("type").AsString();
                var newReagent = new ReagentPrototype();
                newReagent.LoadFrom(proto);

                Assert.That(defType, Is.EqualTo("reagent"));
                Assert.That(newReagent.ID, Is.EqualTo("chem.H2"));
                Assert.That(newReagent.Name, Is.EqualTo("Hydrogen"));
                Assert.That(newReagent.Description, Is.EqualTo("A light, flammable gas."));
                Assert.That(newReagent.SubstanceColor, Is.EqualTo(Color.Teal));
            }
        }

        private const string YamlReagentPrototype = @"- type: reagent
  id: chem.H2
  name: Hydrogen
  desc: A light, flammable gas.
  color: " + "\"#008080\"";
    }
}
