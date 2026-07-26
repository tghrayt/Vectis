FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Vectis.slnx ./
COPY src/Vectis.Domain/Vectis.Domain.csproj src/Vectis.Domain/
COPY src/Vectis.Web/Vectis.Web.csproj src/Vectis.Web/
COPY tests/Vectis.Tests/Vectis.Tests.csproj tests/Vectis.Tests/
RUN dotnet restore src/Vectis.Web/Vectis.Web.csproj

COPY . .
RUN dotnet publish src/Vectis.Web/Vectis.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vectis.Web.dll"]
