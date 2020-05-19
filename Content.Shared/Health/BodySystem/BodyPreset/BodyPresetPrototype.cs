﻿using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     Prototype for the BodyPreset class.
    /// </summary>			
    [Prototype("bodyPreset")]
    [NetSerializable, Serializable]
    public class BodyPresetPrototype : IPrototype, IIndexedPrototype {
        private string _id;
        private string _name;
		private Dictionary<string,string> _partIDs;

        [ViewVariables]
        public string ID => _id;

        [ViewVariables]
        public string Name => _name;
	
        [ViewVariables]
		public Dictionary<string, string> PartIDs => _partIDs;

        public virtual void LoadFrom(YamlMappingNode mapping){
            var serializer = YamlObjectSerializer.NewReader(mapping);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _name, "name", string.Empty);
			serializer.DataField(ref _partIDs, "partIDs", new Dictionary<string, string>());
        }
    }
}
