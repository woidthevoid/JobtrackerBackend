FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["JobtrackerBackend/JobtrackerBackend.csproj", "JobtrackerBackend/"]
RUN dotnet restore "JobtrackerBackend/JobtrackerBackend.csproj"
COPY . .
WORKDIR "/src/JobtrackerBackend"
RUN dotnet build "./JobtrackerBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JobtrackerBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobtrackerBackend.dll"]
