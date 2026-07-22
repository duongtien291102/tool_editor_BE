FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/AiVideoStudio.Api/AiVideoStudio.Api.csproj", "src/AiVideoStudio.Api/"]
COPY ["src/AiVideoStudio.Application/AiVideoStudio.Application.csproj", "src/AiVideoStudio.Application/"]
COPY ["src/AiVideoStudio.Domain/AiVideoStudio.Domain.csproj", "src/AiVideoStudio.Domain/"]
COPY ["src/AiVideoStudio.Infrastructure/AiVideoStudio.Infrastructure.csproj", "src/AiVideoStudio.Infrastructure/"]
COPY ["src/AiVideoStudio.Shared/AiVideoStudio.Shared.csproj", "src/AiVideoStudio.Shared/"]
RUN dotnet restore "src/AiVideoStudio.Api/AiVideoStudio.Api.csproj"
COPY . .
WORKDIR /src/src/AiVideoStudio.Api
RUN dotnet build "AiVideoStudio.Api.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AiVideoStudio.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiVideoStudio.Api.dll"]
