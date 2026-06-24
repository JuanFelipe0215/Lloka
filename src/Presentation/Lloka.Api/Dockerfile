FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar archivos de proyecto para restaurar dependencias en capas separadas
COPY src/Core/Lloka.Domain/Lloka.Domain.csproj                    Core/Lloka.Domain/
COPY src/Core/Lloka.Application/Lloka.Application.csproj          Core/Lloka.Application/
COPY src/Infrastructure/Lloka.Infrastructure/Lloka.Infrastructure.csproj  Infrastructure/Lloka.Infrastructure/
COPY src/Presentation/Lloka.Api/Lloka.Api.csproj                  Presentation/Lloka.Api/

RUN dotnet restore Presentation/Lloka.Api/Lloka.Api.csproj

# Copiar todo el código fuente
COPY src/ .

RUN dotnet publish Presentation/Lloka.Api/Lloka.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Lloka.Api.dll"]
