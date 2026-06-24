using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using MediatR;

namespace Lloka.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler(
    IUserRepository userRepo,
    IPasswordHasher passwordHasher,
    IUnitOfWork     unitOfWork
) : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    public async Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var existing = await userRepo.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            throw new ConflictException("El email ya está registrado.");

        var hash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, hash, request.FirstName, request.LastName);

        if (request.IsOwner)
            user.BecomeOwner();

        await userRepo.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new RegisterUserResponse(user.Id, user.Email, user.FullName, user.IsOwner);
    }
}
