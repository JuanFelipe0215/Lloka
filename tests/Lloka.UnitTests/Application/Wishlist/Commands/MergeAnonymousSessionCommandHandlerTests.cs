using FluentAssertions;
using Lloka.Application.Common.Interfaces;
using Lloka.Application.Wishlist.Commands.MergeAnonymousSession;
using Lloka.Domain.Entities;
using Moq;

namespace Lloka.UnitTests.Application.Wishlist.Commands;

public class MergeAnonymousSessionCommandHandlerTests
{
    private readonly Mock<IWishlistRepository> _wishlistRepo = new();
    private readonly Mock<IUnitOfWork>         _unitOfWork   = new();

    private MergeAnonymousSessionCommandHandler CreateHandler() => new(
        _wishlistRepo.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task Handle_WithItems_AssignsToUserAndSaves()
    {
        var sessionId = Guid.NewGuid();
        var userId    = Guid.NewGuid();
        var command   = new MergeAnonymousSessionCommand(userId, sessionId);

        var items = new List<WishlistItem>
        {
            WishlistItem.Create(Guid.NewGuid(), userId: null, anonymousSessionId: sessionId),
            WishlistItem.Create(Guid.NewGuid(), userId: null, anonymousSessionId: sessionId)
        };

        _wishlistRepo.Setup(r => r.GetBySessionIdAsync(sessionId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(items);

        await CreateHandler().Handle(command, CancellationToken.None);

        // Todos los items deben quedar asignados al usuario y sin sessionId anónimo
        items.Should().AllSatisfy(item =>
        {
            item.UserId.Should().Be(userId);
            item.AnonymousSessionId.Should().BeNull();
        });

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoItems_ReturnsWithoutSaving()
    {
        var command = new MergeAnonymousSessionCommand(Guid.NewGuid(), Guid.NewGuid());

        _wishlistRepo.Setup(r => r.GetBySessionIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<WishlistItem>());

        await CreateHandler().Handle(command, CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
