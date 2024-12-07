FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["src/VibriaApiGateway.csproj", "./src/"]
WORKDIR /app/src
RUN dotnet restore "VibriaApiGateway.csproj"

COPY ./src ./src
RUN dotnet build "VibriaApiGateway.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/build .

EXPOSE 80

ENTRYPOINT ["dotnet", "VibriaApiGateway.dll"]
