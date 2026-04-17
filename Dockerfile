FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "src/AquaGas.API/AquaGas.API.csproj"
RUN dotnet build "src/AquaGas.API/AquaGas.API.csproj" -c Release --no-restore

FROM build AS publish
RUN dotnet publish "src/AquaGas.API/AquaGas.API.csproj" -c Release -o /app/publish --no-build

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AquaGas.API.dll"]