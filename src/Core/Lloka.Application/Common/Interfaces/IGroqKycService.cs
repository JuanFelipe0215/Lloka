namespace Lloka.Application.Common.Interfaces;

public record KycExtractionResult(
    string   FirstName,
    string   LastName,
    string   DocumentNumber,
    DateOnly DateOfBirth,
    bool     Approved);

public interface IGroqKycService
{
    // Recibe el contenido del documento como base64 y retorna los datos extraídos.
    // Si el modelo no puede verificar el documento, Approved=false.
    Task<KycExtractionResult> ExtractAsync(string documentBase64, CancellationToken ct = default);
}
