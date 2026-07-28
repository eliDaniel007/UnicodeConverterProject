# --- Étape build : compile l'app web ASP.NET (réutilise UnicodeConverter.cs) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY UnicodeConverter.cs ./
COPY web/ ./web/
RUN dotnet publish web/UnicodeWeb.csproj -c Release -o /app

# --- Étape runtime : image légère ASP.NET ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app ./
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UnicodeWeb.dll"]
