using Lloka.Application.Common.Exceptions;
using Lloka.Application.Common.Interfaces;
using Lloka.Domain.Entities;
using Lloka.Domain.Enums;
using MediatR;

namespace Lloka.Application.Kyc.Commands.SubmitKycDocument;

public class SubmitKycDocumentCommandHandler(
    IUserRepository userRepo,
    IGroqKycService groqKycService,
    IUnitOfWork     unitOfWork
) : IRequestHandler<SubmitKycDocumentCommand, SubmitKycDocumentResponse>
{
    public async Task<SubmitKycDocumentResponse> Handle(
        SubmitKycDocumentCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.KycStatus == KycStatus.Approved)
            throw new ConflictException("Tu identidad ya fue verificada.");

        // Extraer datos del documento vía Groq Llama Vision.
        // En producción esto llama a la API real; en dev puede ser el MockGroqKycService.
        var extraction = await groqKycService.ExtractAsync(request.DocumentBase64, ct);

        // Registrar la verificación en el agregado User
        var verification = user.SubmitKycVerification();

        if (extraction.Approved)
        {
            verification.Approve(
                extraction.FirstName,
                extraction.LastName,
                extraction.DocumentNumber,
                extraction.DateOfBirth);

            user.UpdateKycStatus(KycStatus.Approved);
        }
        else
        {
            verification.Reject();
            user.UpdateKycStatus(KycStatus.Rejected);
        }

        userRepo.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        var message = extraction.Approved
            ? "Identidad verificada exitosamente."
            : "No fue posible verificar tu identidad. Verifica que la foto sea clara y vuelve a intentarlo.";

        return new SubmitKycDocumentResponse(verification.Id, user.KycStatus, message);
    }
}
