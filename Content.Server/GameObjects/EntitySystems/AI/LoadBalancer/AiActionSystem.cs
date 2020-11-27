using System.Threading;
using Content.Server.GameObjects.EntitySystems.JobQueues.Queues;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems.AI.LoadBalancer
{
    /// <summary>
    /// This will queue up an AI's request for an action and give it one when possible
    /// </summary>
    public class AiActionSystem : EntitySystem
    {
        private readonly AiActionJobQueue _aiRequestQueue = new();

        public AiActionRequestJob RequestAction(AiActionRequest request, CancellationTokenSource cancellationToken)
        {
            var job = new AiActionRequestJob(0.002, request, cancellationToken.Token);
            // AI should already know if it shouldn't request again
            _aiRequestQueue.EnqueueJob(job);
            return job;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _aiRequestQueue.Process();
        }
    }
}
