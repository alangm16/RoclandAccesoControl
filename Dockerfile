# 1. Etapa de compilación usando el SDK de .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0-nanoserver-ltsc2022 AS build
WORKDIR /src

# Copiar el archivo del proyecto web y restaurar dependencias
# (Ajusta la ruta si tu .csproj está en otra carpeta)
COPY ["RoclandAccesoControl.Web/RoclandAccesoControl.Web.csproj", "RoclandAccesoControl.Web/"]
RUN dotnet restore "RoclandAccesoControl.Web/RoclandAccesoControl.Web.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/RoclandAccesoControl.Web"
RUN dotnet publish "RoclandAccesoControl.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Etapa final: Imagen ligera solo con el runtime para correr la app
FROM mcr.microsoft.com/dotnet/aspnet:10.0-nanoserver-ltsc2022 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configuramos la app para escuchar en el puerto 8080 dentro del contenedor
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RoclandAccesoControl.Web.dll"]