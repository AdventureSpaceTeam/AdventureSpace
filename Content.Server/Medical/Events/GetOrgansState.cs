namespace Content.Server.Medical.Events;

[ByRefEvent]
public record struct GetOrgansState(Dictionary<string, string> OrgansState);

