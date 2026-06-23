namespace Lloka.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; private set; }

    private readonly List<object> _domainEvents = [];
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    protected BaseEntity(Guid id)
    {
        Id = id;
    }

    // EF Core needs a parameterless constructor to materialize entities from the DB.
    // private protected prevents accidental use from outside the Domain assembly.
    private protected BaseEntity() { }

    protected void AddDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(BaseEntity? left, BaseEntity? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(BaseEntity? left, BaseEntity? right) =>
        !(left == right);
}
