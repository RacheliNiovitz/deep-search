# Dockerfile משולב: בונה את ה-Angular, מטמיע אותו ב-API, ואורז לשירות אחד.
# מיועד לפריסה ל-GCP Cloud Run (לינק יחיד, ללא CORS).

# ---------- שלב 1: בניית ה-Angular ----------
FROM node:24 AS frontend
WORKDIR /fe
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---------- שלב 2: בניית ה-API + הטמעת ה-Angular ב-wwwroot ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY backend/ ./
# קבצי ה-Angular שנבנו -> wwwroot של ה-API
COPY --from=frontend /fe/dist/frontend/browser ./src/DeepSearch.Api/wwwroot
RUN dotnet publish src/DeepSearch.Api -c Release -o /app

# ---------- שלב 3: ריצה (image רזה) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend /app .
# Cloud Run מאזין על 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeepSearch.Api.dll"]
