FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CalculationHistoryService/CalculationHistoryService.csproj", "CalculationHistoryService/"]
RUN dotnet restore "CalculationHistoryService/CalculationHistoryService.csproj"
COPY . .
WORKDIR "/src/CalculationHistoryService"
RUN dotnet build "CalculationHistoryService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CalculationHistoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CalculationHistoryService.dll"]
