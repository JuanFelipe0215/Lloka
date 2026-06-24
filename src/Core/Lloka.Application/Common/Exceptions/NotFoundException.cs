namespace Lloka.Application.Common.Exceptions;

public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} con id '{key}' no encontrado.");
