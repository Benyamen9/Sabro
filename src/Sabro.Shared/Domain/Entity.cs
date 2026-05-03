namespace Sabro.Shared.Domain;

/// <summary>Base entity with creation/update timestamps per the schema convention.</summary>
public abstract class Entity<TId>
    where TId : struct
{
    public TId Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }
}
