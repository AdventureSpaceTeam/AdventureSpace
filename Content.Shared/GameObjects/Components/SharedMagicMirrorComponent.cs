#nullable enable
using System;
using Content.Shared.Preferences.Appearance;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public class SharedMagicMirrorComponent : Component
    {
        public override string Name => "MagicMirror";

        [Serializable, NetSerializable]
        public enum MagicMirrorUiKey
        {
            Key
        }

        [Serializable, NetSerializable]
        public class HairSelectedMessage : BoundUserInterfaceMessage
        {
            public readonly string HairId;
            public readonly bool IsFacialHair;

            public HairSelectedMessage(string id, bool isFacialHair)
            {
                HairId = id;
                IsFacialHair = isFacialHair;
            }
        }

        [Serializable, NetSerializable]
        public class HairColorSelectedMessage : BoundUserInterfaceMessage
        {
            public (byte r, byte g, byte b) HairColor;
            public readonly bool IsFacialHair;

            public HairColorSelectedMessage((byte r, byte g, byte b) color, bool isFacialHair)
            {
                HairColor = color;
                IsFacialHair = isFacialHair;
            }
        }

        [Serializable, NetSerializable]
        public class EyeColorSelectedMessage : BoundUserInterfaceMessage
        {
            public (byte r, byte g, byte b) EyeColor;

            public EyeColorSelectedMessage((byte r, byte g, byte b) color)
            {
                EyeColor = color;
            }
        }

        [Serializable, NetSerializable]
        public class MagicMirrorInitialDataMessage : BoundUserInterfaceMessage
        {
            public readonly Color HairColor;
            public readonly Color FacialHairColor;
            public readonly string HairId;
            public readonly string FacialHairId;
            public readonly Color EyeColor;
            public readonly SpriteAccessoryCategories CategoriesHair;
            public readonly SpriteAccessoryCategories CategoriesFacialHair;
            public readonly bool CanColorHair;
            public readonly bool CanColorFacialHair;

            public MagicMirrorInitialDataMessage(Color hairColor, Color facialHairColor, string hairId, string facialHairId, Color eyeColor, SpriteAccessoryCategories categoriesHair, SpriteAccessoryCategories categoriesFacialHair, bool canColorHair, bool canColorFacialHair)
            {
                HairColor = hairColor;
                FacialHairColor = facialHairColor;
                HairId = hairId;
                FacialHairId = facialHairId;
                EyeColor = eyeColor;
                CategoriesHair = categoriesHair;
                CategoriesFacialHair = categoriesFacialHair;
                CanColorHair = canColorHair;
                CanColorFacialHair = canColorFacialHair;
            }
        }
    }
}
