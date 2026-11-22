# Backoffice API - cok kademeli derleme imaji.
# Baglam kopyasi onbesiginne katman onbellegini korur: kaynak degisince
# yalnizca publish asamasi yeniden calisir.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Rezervise.slnx .
COPY Rezervise.Domain/Rezervise.Domain.csproj Rezervise.Domain/
COPY Rezervise.Application/Rezervise.Application.csproj Rezervise.Application/
COPY Rezervise.Infrastructure/Rezervise.Infrastructure.csproj Rezervise.Infrastructure/
COPY Rezervise.Api.Mobil/Rezervise.Api.Mobil.csproj Rezervise.Api.Mobil/
COPY Rezervise.Api.BackOffice/Rezervise.Api.BackOffice.csproj Rezervise.Api.BackOffice/
RUN dotnet restore Rezervise.Api.BackOffice/Rezervise.Api.BackOffice.csproj

COPY . .
RUN dotnet publish Rezervise.Api.BackOffice/Rezervise.Api.BackOffice.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Rezervise.Api.BackOffice.dll"]
