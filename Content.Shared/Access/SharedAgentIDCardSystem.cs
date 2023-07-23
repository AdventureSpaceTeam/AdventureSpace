using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    public abstract class SharedAgentIdCardSystem : EntitySystem
    {
        // Just for friending for now
    }

    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum AgentIDCardUiKey : byte
    {
        Key,
    }

    /// <summary>
    /// Represents an <see cref="AgentIDCardComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class AgentIDCardBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentName { get; }
        public string CurrentJob { get; }

        public AgentIDCardBoundUserInterfaceState(string currentName, string currentJob)
        {
            CurrentName = currentName;
            CurrentJob = currentJob;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardNameChangedMessage : BoundUserInterfaceMessage
    {
        public string Name { get; }

        public AgentIDCardNameChangedMessage(string name)
        {
            Name = name;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AgentIDCardJobChangedMessage : BoundUserInterfaceMessage
    {
        public string Job { get; }

        public AgentIDCardJobChangedMessage(string job)
        {
            Job = job;
        }
    }
}
