using Lloka.Application.Common.Interfaces;

namespace Lloka.Infrastructure.Services;

// MOCK TEMPORAL — simula aprobación automática para desarrollo/demo.
// Ver README para estado de implementación del cliente real de Groq.
// Para producción: reemplazar por GroqKycService que llame a la API de Llama Vision.
public class MockGroqKycService : IGroqKycService
{
    public Task<KycExtractionResult> ExtractAsync(string documentBase64, CancellationToken ct = default)
    {
        var result = new KycExtractionResult(
            FirstName:      "Juan",
            LastName:       "Simulado",
            DocumentNumber: "1000000000",
            DateOfBirth:    new DateOnly(1990, 1, 1),
            Approved:       true);

        return Task.FromResult(result);
    }
}
