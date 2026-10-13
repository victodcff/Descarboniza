# Descarboniza (ASP.NET Core 8) para o Render.
# Este arquivo fica na RAIZ do repositório, na mesma pasta do Descarboniza.sln.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura os pacotes primeiro (aproveita o cache quando só o código muda)
COPY Descarboniza/Descarboniza.csproj Descarboniza/
RUN dotnet restore Descarboniza/Descarboniza.csproj

COPY . .
RUN dotnet publish Descarboniza/Descarboniza.csproj -c Release -o /app/publish --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# TZ: faz o ToLocalTime() mostrar o horário de Brasília no servidor
ENV TZ=America/Sao_Paulo \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# A porta vem da variável PORT (o Program.cs lê). No Render o padrão é 10000.
EXPOSE 10000

ENTRYPOINT ["dotnet", "Descarboniza.dll"]
