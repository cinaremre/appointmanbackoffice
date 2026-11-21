# Backoffice API - cok kademeli derleme imaji.
# Baglam kopyasi onbesiginne katman onbellegini korur: kaynak degisince
# yalnizca publish asamasi yeniden calisir.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AppointMan.slnx .
COPY AppointMan.Domain/AppointMan.Domain.csproj AppointMan.Domain/
COPY AppointMan.Application/AppointMan.Application.csproj AppointMan.Application/
COPY AppointMan.Infrastructure/AppointMan.Infrastructure.csproj AppointMan.Infrastructure/
COPY AppointMan.Api/AppointMan.Api.csproj AppointMan.Api/
COPY AppointMan.BackOffice.Api/AppointMan.BackOffice.Api.csproj AppointMan.BackOffice.Api/
RUN dotnet restore AppointMan.BackOffice.Api/AppointMan.BackOffice.Api.csproj

COPY . .
RUN dotnet publish AppointMan.BackOffice.Api/AppointMan.BackOffice.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AppointMan.BackOffice.Api.dll"]
