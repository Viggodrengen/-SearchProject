FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SearchProject.sln ./
COPY Shared/Shared.csproj Shared/
COPY SearchWebApp/SearchWebApp.csproj SearchWebApp/

RUN dotnet restore SearchWebApp/SearchWebApp.csproj

COPY . .
RUN dotnet publish SearchWebApp/SearchWebApp.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SearchWebApp.dll"]
