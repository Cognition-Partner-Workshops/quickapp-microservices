# Build context: src/ (see docker-compose.yml)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore in its own layer so it is cached until a project file changes.
COPY ["global.json", "./"]
COPY ["Services/Order/Order.API/Order.API.csproj", "Services/Order/Order.API/"]
COPY ["Services/Order/Order.Domain/Order.Domain.csproj", "Services/Order/Order.Domain/"]
COPY ["Services/Order/Order.Infrastructure/Order.Infrastructure.csproj", "Services/Order/Order.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.Infrastructure/Shared.Infrastructure.csproj", "Shared/Shared.Infrastructure/"]
RUN dotnet restore "Services/Order/Order.API/Order.API.csproj"

COPY ["Services/Order/", "Services/Order/"]
COPY ["Shared/", "Shared/"]
RUN dotnet publish "Services/Order/Order.API/Order.API.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003 \
    DOTNET_RUNNING_IN_CONTAINER=true
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Order.API.dll"]
