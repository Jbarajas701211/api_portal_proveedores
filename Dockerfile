# Etapa base (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar proyecto
COPY . .

# Restaurar y publicar
RUN dotnet restore
RUN dotnet publish -c Release -o /out

# Etapa final

ENV TZ=America/Mexico_City

FROM base AS final
WORKDIR /app
COPY --from=build /out . 
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiProveedores.dll"]
