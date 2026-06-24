using Ardalis.GuardClauses;
using Lloka.Domain.Common;
using Lloka.Domain.Enums;

namespace Lloka.Domain.Entities;

public class OutboxMessage : BaseEntity
{
    public string             EventType   { get; private set; } = null!;
    public string             Payload     { get; private set; } = null!; // JSON serializado
    public OutboxMessageStatus Status     { get; private set; }
    public DateTime?          PublishedAt { get; private set; }
    public int                RetryCount  { get; private set; }
    public DateTime           CreatedAt   { get; private set; }

    private OutboxMessage(Guid id, string eventType, string payload) : base(id)
    {
        EventType  = eventType;
        Payload    = payload;
        Status     = OutboxMessageStatus.Pending;
        RetryCount = 0;
        CreatedAt  = DateTime.UtcNow;
    }

    private protected OutboxMessage() { }

    public static OutboxMessage Create(string eventType, string payload)
    {
        Guard.Against.NullOrWhiteSpace(eventType, nameof(eventType));
        Guard.Against.NullOrWhiteSpace(payload,   nameof(payload));

        return new OutboxMessage(Guid.NewGuid(), eventType, payload);
    }

    public void MarkPublished()
    {
        Status      = OutboxMessageStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = OutboxMessageStatus.Failed;
        RetryCount++;
    }

    public void ResetForRetry()
    {
        Status = OutboxMessageStatus.Pending;
    }
}
