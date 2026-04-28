namespace NexTraceOne.IdentityAccess.Domain.Entities;

/// <summary>Estado de um registo de alerta.</summary>
public enum AlertFiringStatus
{
    Firing = 0,
    Resolved = 1,
    Silenced = 2,
}
