FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SearchProject.sln ./
COPY Shared/Shared.csproj Shared/
COPY indexer/indexer.csproj indexer/

RUN dotnet restore indexer/indexer.csproj

COPY . .
RUN dotnet publish indexer/indexer.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "indexer.dll"]
