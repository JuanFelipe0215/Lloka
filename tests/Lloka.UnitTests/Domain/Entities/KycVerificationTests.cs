using FluentAssertions;
using Lloka.Domain.Common;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;

namespace Lloka.UnitTests.Domain.Entities;

public class KycVerificationTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly DateOnly ValidDob = new(1995, 3, 15);

    // --- Create ---

    [Fact]
    public void Create_ValidUserId_StatusIsPending()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();

        sut.Status.Should().Be(KycVerificationStatus.Pending);
    }

    [Fact]
    public void Create_ValidUserId_SubmittedAtIsPopulated()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user   = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut    = user.SubmitKycVerification();

        sut.SubmittedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Create_ValidUserId_ExtractedFieldsAreNull()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();

        sut.ExtractedFirstName.Should().BeNull();
        sut.ExtractedLastName .Should().BeNull();
        sut.DocumentNumber    .Should().BeNull();
        sut.DateOfBirth       .Should().BeNull();
        sut.ReviewedAt        .Should().BeNull();
    }

    // --- Approve (happy path) ---

    [Fact]
    public void Approve_FromPending_StatusIsApproved()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();

        sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        sut.Status.Should().Be(KycVerificationStatus.Approved);
    }

    [Fact]
    public void Approve_FromPending_ExtractedFieldsArePopulated()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();

        sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        sut.ExtractedFirstName.Should().Be("Juan");
        sut.ExtractedLastName .Should().Be("Pérez");
        sut.DocumentNumber    .Should().Be("1234567890");
        sut.DateOfBirth       .Should().Be(ValidDob);
    }

    [Fact]
    public void Approve_FromPending_ReviewedAtIsPopulated()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user   = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut    = user.SubmitKycVerification();

        sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        sut.ReviewedAt.Should().NotBeNull().And.BeAfter(before);
    }

    // --- Approve (transición inválida) ---

    [Fact]
    public void Approve_WhenAlreadyApproved_ThrowsDomainException()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();
        sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        var act = () => sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Approve_WhenRejected_ThrowsDomainException()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();
        sut.Reject();

        var act = () => sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    // --- Reject (happy path) ---

    [Fact]
    public void Reject_FromPending_StatusIsRejected()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();

        sut.Reject();

        sut.Status.Should().Be(KycVerificationStatus.Rejected);
    }

    [Fact]
    public void Reject_FromPending_ReviewedAtIsPopulated()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user   = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut    = user.SubmitKycVerification();

        sut.Reject();

        sut.ReviewedAt.Should().NotBeNull().And.BeAfter(before);
    }

    // --- Reject (transición inválida) ---

    [Fact]
    public void Reject_WhenApproved_ThrowsDomainException()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();
        sut.Approve("Juan", "Pérez", "1234567890", ValidDob);

        var act = () => sut.Reject();

        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ThrowsDomainException()
    {
        var user = User.Create("u@u.com", "hash", "Juan", "Pérez");
        var sut  = user.SubmitKycVerification();
        sut.Reject();

        var act = () => sut.Reject();

        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }
}
