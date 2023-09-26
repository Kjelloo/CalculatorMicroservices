FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["AdditionService/AdditionService.csproj", "AdditionService/"]
RUN dotnet restore "AdditionService/AdditionService.csproj"
COPY . .
WORKDIR "/src/AdditionService"
RUN dotnet build "AdditionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdditionService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdditionService.dll"]
