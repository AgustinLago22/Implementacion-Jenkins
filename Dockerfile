# Imagen de la API. Multi-stage: se compila con el SDK (grande) y se entrega solo con el
# runtime de ASP.NET (chico). El compilador no viaja a staging ni a producción.

# --- Etapa 1: compilar ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo el .csproj y el restore: esa capa queda cacheada y no se repite
# mientras no cambien las dependencias, aunque cambie el código.
COPY src/HolaMundo.Api/HolaMundo.Api.csproj src/HolaMundo.Api/
RUN dotnet restore src/HolaMundo.Api/HolaMundo.Api.csproj

COPY src/HolaMundo.Api/ src/HolaMundo.Api/
RUN dotnet publish src/HolaMundo.Api/HolaMundo.Api.csproj -c Release -o /app --no-restore

# --- Etapa 2: la imagen que se entrega ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Las imágenes de .NET traen un usuario sin privilegios ($APP_UID): la app no corre como root.
USER $APP_UID
# Las imágenes de .NET escuchan en 8080 por defecto. En el paso 8 se publica como 8081 en la PC.
EXPOSE 8080
# APP_VERSION no está acá a propósito: la pone cada entorno al levantar el contenedor.
ENTRYPOINT ["dotnet", "HolaMundo.Api.dll"]
