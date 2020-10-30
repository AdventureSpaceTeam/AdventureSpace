﻿using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Prototypes.Cargo
{
    [NetSerializable, Serializable, Prototype("cargoProduct")]
    public class CargoProductPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private string _description;
        private SpriteSpecifier _icon;
        private string _product;
        private int _pointCost;
        private string _category;
        private string _group;

        [ViewVariables]
        public string ID => _id;

        /// <summary>
        ///     Product name.
        /// </summary>
        [ViewVariables]
        public string Name
        {
            get
            {
                if (_name.Trim().Length != 0)
                    return _name;
                EntityPrototype prototype = null;
                IoCManager.Resolve<IPrototypeManager>()?.TryIndex(_product, out prototype);
                if (prototype?.Name != null)
                    _name = prototype.Name;
                return _name;
            }
        }

        /// <summary>
        ///     Short description of the product.
        /// </summary>
        [ViewVariables]
        public string Description
        {
            get
            {
                if (_description.Trim().Length != 0)
                    return _description;
                EntityPrototype prototype = null;
                IoCManager.Resolve<IPrototypeManager>()?.TryIndex(_product, out prototype);
                if (prototype?.Description != null)
                    _description = prototype.Description;
                return _description;
            }
        }

        /// <summary>
        ///     Texture path used in the CargoConsole GUI.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     The prototype name of the product.
        /// </summary>
        [ViewVariables]
        public string Product => _product;

        /// <summary>
        ///     The point cost of the product.
        /// </summary>
        [ViewVariables]
        public int PointCost => _pointCost;

        /// <summary>
        ///     The prototype category of the product. (e.g. Engineering, Medical)
        /// </summary>
        [ViewVariables]
        public string Category => _category;

        /// <summary>
        ///     The prototype group of the product. (e.g. Contraband)
        /// </summary>
        [ViewVariables]
        public string Group => _group;

        public CargoProductPrototype()
        {
            IoCManager.InjectDependencies(this);
        }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(ref _product, "product", null);
            serializer.DataField(ref _pointCost, "cost", 0);
            serializer.DataField(ref _category, "category", string.Empty);
            serializer.DataField(ref _group, "group", string.Empty);
        }
    }
}
