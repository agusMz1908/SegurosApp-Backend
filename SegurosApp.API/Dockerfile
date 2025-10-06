# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["SegurosApp.API.csproj", "./"]
RUN dotnet restore "SegurosApp.API.csproj"

# Copiar el resto de los archivos y compilar
COPY . .
RUN dotnet build "SegurosApp.API.csproj" -c Release -o /app/build

# Publicar la aplicaci�n
FROM build AS publish
RUN dotnet publish "SegurosApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final - runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SegurosApp.API.dll"]