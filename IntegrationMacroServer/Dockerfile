#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["IntegrationMacroServer/IntegrationMacroServer.csproj", "IntegrationMacroServer/"]
RUN dotnet restore "IntegrationMacroServer/IntegrationMacroServer.csproj"
COPY . .
WORKDIR "/src/IntegrationMacroServer"
RUN dotnet build "IntegrationMacroServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IntegrationMacroServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV DOTNET_ENVIRONMENT="Docker"
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IntegrationMacroServer.dll"]