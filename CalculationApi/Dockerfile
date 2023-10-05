FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "SharedModels/SharedModels.csproj"

COPY ["Monitoring/Monitoring.csproj", "Monitoring/"]
RUN dotnet restore "Monitoring/Monitoring.csproj"

COPY ["CalculationApi/CalculationApi.csproj", "CalculationApi/"]
RUN dotnet restore "CalculationApi/CalculationApi.csproj"
COPY . .
WORKDIR "/src/CalculationApi"
RUN dotnet build "CalculationApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CalculationApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CalculationApi.dll"]
