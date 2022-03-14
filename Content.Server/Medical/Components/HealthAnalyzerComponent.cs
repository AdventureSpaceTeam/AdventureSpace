using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.MedicalScanner;
using Content.Shared.Disease;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Medical.Components
{
    /// <summary>
    ///    After scanning, retrieves the target Uid to use with its related UI.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(SharedHealthAnalyzerComponent))]
    public sealed class HealthAnalyzerComponent : SharedHealthAnalyzerComponent
    {
        /// <summary>
        /// How long it takes to scan someone.
        /// </summary>
        [DataField("scanDelay")]
        [ViewVariables]
        public float ScanDelay = 0.8f;
        /// <summary>
        ///     Token for interrupting scanning do after.
        /// </summary>
        public CancellationTokenSource? CancelToken;
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(HealthAnalyzerUiKey.Key);

        /// <summary>
        /// Is this actually going to give people the disease below
        /// </summary>
        [DataField("fake")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Fake = false;

        /// <summary>
        /// The disease this will give people if Fake == true
        /// </summary>
        [DataField("disease", customTypeSerializer: typeof(PrototypeIdSerializer<DiseasePrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string Disease = string.Empty;
    }
}
