FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "SharedModels/SharedModels.csproj"

COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
RUN dotnet restore "Monitoring/Monitoring.csproj"

COPY ["SubtractionService/SubtractionService.csproj", "SubtractionService/"]
RUN dotnet restore "SubtractionService/SubtractionService.csproj"

COPY . .
WORKDIR "/src/SubtractionService"
RUN dotnet build "SubtractionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SubtractionService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SubtractionService.dll"]
