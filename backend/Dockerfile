# Dockerfile ל-API (.NET 8) - מתאים לפריסה ל-GCP Cloud Run.
# Build בשני שלבים: קומפילציה ב-SDK, ריצה ב-runtime רזה.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore DeepSearch.sln
RUN dotnet publish src/DeepSearch.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# Cloud Run מצפה לפורט 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeepSearch.Api.dll"]
