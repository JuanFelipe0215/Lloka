using FluentAssertions;
using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Application.Kyc.Commands.SubmitKycDocument;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using Moq;

namespace Lloka.UnitTests.Application.Kyc.Commands;

public class SubmitKycDocumentCommandHandlerTests
{
    private readonly Mock<IUserRepository>  _userRepo       = new();
    private readonly Mock<IGroqKycService>  _groqKycService = new();
    private readonly Mock<IUnitOfWork>      _unitOfWork     = new();

    private SubmitKycDocumentCommandHandler CreateHandler() => new(
        _userRepo.Object,
        _groqKycService.Object,
        _unitOfWork.Object);

    private static KycExtractionResult ApprovedResult() => new(
        FirstName:      "Ana",
        LastName:       "López",
        DocumentNumber: "1234567890",
        DateOfBirth:    new DateOnly(1990, 5, 15),
        Approved:       true);

    private static KycExtractionResult RejectedResult() => new(
        FirstName:      "",
        LastName:       "",
        DocumentNumber: "",
        DateOfBirth:    default,
        Approved:       false);

    private SubmitKycDocumentCommand ValidCommand(Guid userId) =>
        new(userId, "base64documentoaqui==");

    [Fact]
    public async Task Handle_ApprovedExtraction_SetsKycApprovedAndSaves()
    {
        var user    = User.Create("ana@test.com", "hash", "Ana", "López");
        var command = ValidCommand(user.Id);

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _groqKycService.Setup(s => s.ExtractAsync(command.DocumentBase64, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(ApprovedResult());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.KycStatus.Should().Be(KycStatus.Approved);
        user.KycStatus.Should().Be(KycStatus.Approved);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectedExtraction_SetsKycRejectedAndSaves()
    {
        var user    = User.Create("ana@test.com", "hash", "Ana", "López");
        var command = ValidCommand(user.Id);

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);
        _groqKycService.Setup(s => s.ExtractAsync(command.DocumentBase64, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(RejectedResult());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.KycStatus.Should().Be(KycStatus.Rejected);
        user.KycStatus.Should().Be(KycStatus.Rejected);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_AlreadyApproved_ThrowsConflictException()
    {
        var user = User.Create("ana@test.com", "hash", "Ana", "López");
        user.UpdateKycStatus(KycStatus.Approved);

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(user);

        var act = () => CreateHandler().Handle(ValidCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
                 .WithMessage("*verificada*");
    }
}
