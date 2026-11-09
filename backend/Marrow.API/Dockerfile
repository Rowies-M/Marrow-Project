# Contexto de build: backend/Marrow.API

# 1. Restauración: solo se repite si cambia el archivo de proyecto.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore
WORKDIR /src
COPY Marrow.API.csproj .
RUN dotnet restore

# 2. Compilación y publicación.
FROM restore AS publish
COPY . .
RUN dotnet publish --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# 3. Ejecución: solo el runtime y la aplicación publicada, sin SDK ni código fuente.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Los secretos llegan por variables de entorno al ejecutar el contenedor; la imagen no contiene ninguno.
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Usuario sin privilegios que ya incluyen las imágenes oficiales de .NET.
USER $APP_UID
ENTRYPOINT ["dotnet", "Marrow.API.dll"]
