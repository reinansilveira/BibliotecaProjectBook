FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Library.Domain/Library.Domain.csproj src/Library.Domain/
COPY src/Library.Application/Library.Application.csproj src/Library.Application/
COPY src/Library.Infrastructure/Library.Infrastructure.csproj src/Library.Infrastructure/
COPY src/Library.Api/Library.Api.csproj src/Library.Api/
RUN dotnet restore src/Library.Api/Library.Api.csproj

COPY src/ src/
RUN dotnet publish src/Library.Api/Library.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Library.Api.dll"]
