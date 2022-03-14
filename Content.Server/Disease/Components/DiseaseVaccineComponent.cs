using System.Threading;
using Content.Shared.Disease;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// <summary>
    /// For disease vaccines
    /// </summary>
    public sealed class DiseaseVaccineComponent : Component
    {
        /// <summary>
        /// How long it takes to inject someone
        /// </summary>
        [DataField("injectDelay")]
        [ViewVariables]
        public float InjectDelay = 2f;
        /// <summary>
        /// If this vaccine has been used
        /// </summary>
        public bool Used = false;
        /// <summary>
        /// Token for interrupting injection do after.
        /// </summary>
        public CancellationTokenSource? CancelToken;

        /// <summary>
        /// The disease prototype currently on the vaccine
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public DiseasePrototype? Disease;
    }
}
