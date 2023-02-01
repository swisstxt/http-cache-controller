FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["HttpCacheController/HttpCacheController.csproj", "HttpCacheController/"]
RUN dotnet restore "HttpCacheController/HttpCacheController.csproj"
COPY . .
WORKDIR "/src/HttpCacheController"
RUN dotnet build "HttpCacheController.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HttpCacheController.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HttpCacheController.dll"]
