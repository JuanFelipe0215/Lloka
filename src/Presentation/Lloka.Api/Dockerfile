FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar todo src/ de una vez para que MSBuild resuelva las ProjectReferences
# correctamente sin depender del orden de copia de .csproj individuales.
COPY src/ src/

RUN dotnet publish src/Presentation/Lloka.Api/Lloka.Api.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Lloka.Api.dll"]
