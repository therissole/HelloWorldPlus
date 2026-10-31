FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY ["global.json", "."]
COPY ["Directory.Build.props", "."]
COPY ["BannedSymbols.txt", "."]
COPY [".editorconfig", "."]
COPY ["src/HelloWorldPlus.Gateway/HelloWorldPlus.Gateway.csproj", "src/HelloWorldPlus.Gateway/"]
COPY ["src/HelloWorldPlus.Gateway/packages.lock.json", "src/HelloWorldPlus.Gateway/"]
COPY ["src/HelloWorldPlus.ServiceDefaults/HelloWorldPlus.ServiceDefaults.csproj", "src/HelloWorldPlus.ServiceDefaults/"]
COPY ["src/HelloWorldPlus.ServiceDefaults/packages.lock.json", "src/HelloWorldPlus.ServiceDefaults/"]
COPY ["src/HelloWorldPlus.ClientServiceDefaults/", "src/HelloWorldPlus.ClientServiceDefaults/"]
# Static Web Assets influence the SDK's implicit restore graph, so the locked
# restore must see the Web project's source and wwwroot rather than only its project file.
COPY ["src/HelloWorldPlus.Web/", "src/HelloWorldPlus.Web/"]
RUN dotnet restore "src/HelloWorldPlus.Gateway/HelloWorldPlus.Gateway.csproj" --locked-mode

COPY . .
WORKDIR "/src/src/HelloWorldPlus.Gateway"
RUN dotnet build "HelloWorldPlus.Gateway.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "HelloWorldPlus.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS final
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HelloWorldPlus.Gateway.dll"]
