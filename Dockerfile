FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/OpenStatusPage.Server/OpenStatusPage.Server.csproj", "src/OpenStatusPage.Server/"]
COPY ["src/OpenStatusPage.Shared/OpenStatusPage.Shared.csproj", "src/OpenStatusPage.Shared/"]
COPY ["src/OpenStatusPage.Client/OpenStatusPage.Client.csproj", "src/OpenStatusPage.Client/"]
COPY ["src/OpenStatusPage.Client.Application/OpenStatusPage.Client.Application.csproj", "src/OpenStatusPage.Client.Application/"]
COPY ["src/OpenStatusPage.Server.Application/OpenStatusPage.Server.Application.csproj", "src/OpenStatusPage.Server.Application/"]
COPY ["src/OpenStatusPage.Server.Persistence/OpenStatusPage.Server.Persistence.csproj", "src/OpenStatusPage.Server.Persistence/"]
COPY ["src/OpenStatusPage.Server.Domain/OpenStatusPage.Server.Domain.csproj", "src/OpenStatusPage.Server.Domain/"]
RUN dotnet restore "src/OpenStatusPage.Server/OpenStatusPage.Server.csproj"
COPY . .

FROM build AS publish
WORKDIR "/src/src/OpenStatusPage.Server"
RUN dotnet publish "OpenStatusPage.Server.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OpenStatusPage.Server.dll"]