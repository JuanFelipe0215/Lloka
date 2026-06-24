using Ardalis.GuardClauses;
using Lloka.Domain.Common;
using Lloka.Domain.Enums;

namespace Lloka.Domain.Entities;

public class KycVerification : BaseEntity
{
    public Guid                  UserId             { get; private set; }
    public string?               ExtractedFirstName { get; private set; }
    public string?               ExtractedLastName  { get; private set; }
    public string?               DocumentNumber     { get; private set; }
    public DateOnly?             DateOfBirth        { get; private set; }
    public KycVerificationStatus Status             { get; private set; }
    public DateTime              SubmittedAt        { get; private set; }
    public DateTime?             ReviewedAt         { get; private set; }

    private KycVerification(Guid id, Guid userId) : base(id)
    {
        UserId      = userId;
        Status      = KycVerificationStatus.Pending;
        SubmittedAt = DateTime.UtcNow;
        // Los campos extraídos quedan null hasta que el modelo de IA retorne los datos.
    }

    private protected KycVerification() { }

    // Solo User puede crear verificaciones vía SubmitKycVerification().
    internal static KycVerification Create(Guid userId)
    {
        Guard.Against.Default(userId, nameof(userId));
        return new KycVerification(Guid.NewGuid(), userId);
    }

    public void Approve(string firstName, string lastName, string documentNumber, DateOnly dateOfBirth)
    {
        // Un veredicto ya emitido no se puede sobreescribir.
        if (Status != KycVerificationStatus.Pending)
            throw new DomainException(
                $"Solo se puede aprobar un KYC en estado Pending. Estado actual: {Status}.");

        Guard.Against.NullOrWhiteSpace(firstName,      nameof(firstName));
        Guard.Against.NullOrWhiteSpace(lastName,       nameof(lastName));
        Guard.Against.NullOrWhiteSpace(documentNumber, nameof(documentNumber));

        ExtractedFirstName = firstName;
        ExtractedLastName  = lastName;
        DocumentNumber     = documentNumber;
        DateOfBirth        = dateOfBirth;
        Status             = KycVerificationStatus.Approved;
        ReviewedAt         = DateTime.UtcNow;
    }

    public void Reject()
    {
        if (Status != KycVerificationStatus.Pending)
            throw new DomainException(
                $"Solo se puede rechazar un KYC en estado Pending. Estado actual: {Status}.");

        Status     = KycVerificationStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
    }
}
