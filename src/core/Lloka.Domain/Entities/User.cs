using Ardalis.GuardClauses;
using Lloka.Domain.Common;
using Lloka.Domain.Enums;

namespace Lloka.Domain.Entities;

public class User : BaseEntity
{
    public string    Email        { get; private set; } = null!;
    public string    PasswordHash { get; private set; } = null!;
    public string    FirstName    { get; private set; } = null!;
    public string    LastName     { get; private set; } = null!;
    public string    FullName     => $"{FirstName} {LastName}";
    public string?   PhoneNumber  { get; private set; }
    public KycStatus KycStatus    { get; private set; }
    public bool      IsOwner      { get; private set; }
    public DateTime  CreatedAt    { get; private set; }
    public DateTime  UpdatedAt    { get; private set; }

    private readonly List<KycVerification> _kycVerifications = [];
    public IReadOnlyCollection<KycVerification> KycVerifications => _kycVerifications.AsReadOnly();

    private User(
        Guid id,
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber) : base(id)
    {
        Email        = email;
        PasswordHash = passwordHash;
        FirstName    = firstName;
        LastName     = lastName;
        PhoneNumber  = phoneNumber;
        KycStatus    = KycStatus.NotSubmitted;
        IsOwner      = false;
        CreatedAt    = DateTime.UtcNow;
        UpdatedAt    = DateTime.UtcNow;
    }

    private protected User() { }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber = null)
    {
        Guard.Against.NullOrWhiteSpace(email,        nameof(email));
        Guard.Against.NullOrWhiteSpace(passwordHash, nameof(passwordHash));
        Guard.Against.NullOrWhiteSpace(firstName,    nameof(firstName));
        Guard.Against.NullOrWhiteSpace(lastName,     nameof(lastName));

        return new User(Guid.NewGuid(), email, passwordHash, firstName, lastName, phoneNumber);
    }

    // Crea un intento de verificación KYC y actualiza el estado agregado del usuario.
    // El caller (SubmitKycDocumentCommand) debe llamar Approve()/Reject() sobre el objeto retornado.
    public KycVerification SubmitKycVerification()
    {
        var verification = KycVerification.Create(Id);
        _kycVerifications.Add(verification);
        KycStatus = KycStatus.Pending;
        UpdatedAt = DateTime.UtcNow;
        return verification;
    }

    public void BecomeOwner()
    {
        IsOwner   = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateKycStatus(KycStatus newStatus)
    {
        KycStatus = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        UpdatedAt   = DateTime.UtcNow;
    }
}
