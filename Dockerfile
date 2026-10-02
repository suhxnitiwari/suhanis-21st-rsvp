FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/PartyRsvp/PartyRsvp.csproj src/PartyRsvp/
RUN dotnet restore src/PartyRsvp/PartyRsvp.csproj
COPY src/PartyRsvp/ src/PartyRsvp/
RUN dotnet publish src/PartyRsvp/PartyRsvp.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data && chown $APP_UID /data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=10000 \
    ConnectionStrings__Party="Data Source=/data/party.db"
EXPOSE 10000
ENTRYPOINT ["dotnet", "PartyRsvp.dll"]
