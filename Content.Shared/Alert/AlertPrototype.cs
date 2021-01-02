﻿using System;
using System.Globalization;
using Content.Shared.Interfaces;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Alert
{
    /// <summary>
    /// An alert popup with associated icon, tooltip, and other data.
    /// </summary>
    [Prototype("alert")]
    public class AlertPrototype : IPrototype
    {
        /// <summary>
        /// Type of alert, no 2 alert prototypes should have the same one.
        /// </summary>
        public AlertType AlertType { get; private set; }

        /// <summary>
        /// Path to the icon (png) to show in alert bar. If severity levels are supported,
        /// this should be the path to the icon without the severity number
        /// (i.e. hot.png if there is hot1.png and hot2.png). Use <see cref="GetIconPath"/>
        /// to get the correct icon path for a particular severity level.
        /// </summary>
        [ViewVariables]
        public SpriteSpecifier Icon { get; private set; }

        /// <summary>
        /// Name to show in tooltip window. Accepts formatting.
        /// </summary>
        public FormattedMessage Name { get; private set; }

        /// <summary>
        /// Description to show in tooltip window. Accepts formatting.
        /// </summary>
        public FormattedMessage Description { get; private set; }

        /// <summary>
        /// Category the alert belongs to. Only one alert of a given category
        /// can be shown at a time. If one is shown while another is already being shown,
        /// it will be replaced. This can be useful for categories of alerts which should naturally
        /// replace each other and are mutually exclusive, for example lowpressure / highpressure,
        /// hot / cold. If left unspecified, the alert will not replace or be replaced by any other alerts.
        /// </summary>
        public AlertCategory? Category { get; private set; }

        /// <summary>
        /// Key which is unique w.r.t category semantics (alerts with same category have equal keys,
        /// alerts with no category have different keys).
        /// </summary>
        public AlertKey AlertKey { get; private set; }

        /// <summary>
        /// -1 (no effect) unless MaxSeverity is specified. Defaults to 1. Minimum severity level supported by this state.
        /// </summary>
        public short MinSeverity => MaxSeverity == -1 ? (short) -1 : _minSeverity;

        private short _minSeverity;

        /// <summary>
        /// Maximum severity level supported by this state. -1 (default) indicates
        /// no severity levels are supported by the state.
        /// </summary>
        public short MaxSeverity { get; private set; }

        /// <summary>
        /// Indicates whether this state support severity levels
        /// </summary>
        public bool SupportsSeverity => MaxSeverity != -1;

        /// <summary>
        /// Whether this alert is clickable. This is valid clientside.
        /// </summary>
        public bool HasOnClick { get; private set; }

        /// <summary>
        /// Defines what to do when the alert is clicked.
        /// This will always be null on clientside.
        /// </summary>
        public IAlertClick OnClick { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(this, x => x.Icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(this, x => x.MaxSeverity, "maxSeverity", (short) -1);
            serializer.DataField(ref _minSeverity, "minSeverity", (short) 1);

            serializer.DataReadFunction("name", string.Empty,
                s => Name = FormattedMessage.FromMarkup(s));
            serializer.DataReadFunction("description", string.Empty,
                s => Description = FormattedMessage.FromMarkup(s));

            serializer.DataField(this, x => x.AlertType, "alertType", AlertType.Error);
            if (AlertType == AlertType.Error)
            {
                Logger.ErrorS("alert", "missing or invalid alertType for alert with name {0}", Name);
            }

            if (serializer.TryReadDataField("category", out AlertCategory alertCategory))
            {
                Category = alertCategory;
            }

            AlertKey = new AlertKey(AlertType, Category);

            HasOnClick = serializer.TryReadDataField("onClick", out string _);

            if (IoCManager.Resolve<IModuleManager>().IsClientModule) return;
            serializer.DataField(this, x => x.OnClick, "onClick", null);
        }

        /// <param name="severity">severity level, if supported by this alert</param>
        /// <returns>the icon path to the texture for the provided severity level</returns>
        public SpriteSpecifier GetIcon(short? severity = null)
        {
            if (!SupportsSeverity && severity != null)
            {
                throw new InvalidOperationException($"This alert ({AlertKey}) does not support severity");
            }

            if (!SupportsSeverity)
                return Icon;

            if (severity == null)
            {
                throw new ArgumentException($"No severity specified but this alert ({AlertKey}) has severity.", nameof(severity));
            }

            if (severity < MinSeverity)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), $"Severity below minimum severity in {AlertKey}.");
            }

            if (severity > MaxSeverity)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), $"Severity above maximum severity in {AlertKey}.");
            }

            var severityText = severity.Value.ToString(CultureInfo.InvariantCulture);
            switch (Icon)
            {
                case SpriteSpecifier.EntityPrototype entityPrototype:
                    throw new InvalidOperationException($"Severity not supported for EntityPrototype icon in {AlertKey}");
                case SpriteSpecifier.Rsi rsi:
                    return new SpriteSpecifier.Rsi(rsi.RsiPath, rsi.RsiState + severityText);
                case SpriteSpecifier.Texture texture:
                    var newName = texture.TexturePath.FilenameWithoutExtension + severityText;
                    return new SpriteSpecifier.Texture(
                        texture.TexturePath.WithName(newName + "." + texture.TexturePath.Extension));
                default:
                    throw new ArgumentOutOfRangeException(nameof(Icon));
            }
        }
    }

    /// <summary>
    /// Key for an alert which is unique (for equality and hashcode purposes) w.r.t category semantics.
    /// I.e., entirely defined by the category, if a category was specified, otherwise
    /// falls back to the id.
    /// </summary>
    [Serializable, NetSerializable]
    public struct AlertKey
    {
        public readonly AlertType? AlertType;
        public readonly AlertCategory? AlertCategory;

        /// NOTE: if the alert has a category you must pass the category for this to work
        /// properly as a key. I.e. if the alert has a category and you pass only the alert type, and you
        /// compare this to another AlertKey that has both the category and the same alert type, it will not consider them equal.
        public AlertKey(AlertType? alertType, AlertCategory? alertCategory)
        {
            AlertCategory = alertCategory;
            AlertType = alertType;
        }

        public bool Equals(AlertKey other)
        {
            // compare only on alert category if we have one
            if (AlertCategory.HasValue)
            {
                return other.AlertCategory == AlertCategory;
            }

            return AlertType == other.AlertType && AlertCategory == other.AlertCategory;
        }

        public override bool Equals(object obj)
        {
            return obj is AlertKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            // use only alert category if we have one
            if (AlertCategory.HasValue) return AlertCategory.GetHashCode();
            return AlertType.GetHashCode();
        }

        /// <param name="category">alert category, must not be null</param>
        /// <returns>An alert key for the provided alert category. This must only be used for
        /// queries and never storage, as it is lacking an alert type.</returns>
        public static AlertKey ForCategory(AlertCategory category)
        {
            return new(null, category);
        }
    }
}
