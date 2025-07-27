# Build Vue.js frontend
FROM node:18 AS client-build
WORKDIR /client
COPY game.client/package*.json ./
RUN npm ci
COPY game.client/ ./
RUN npm run build

# Build .NET API
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY game.api/*.csproj ./
RUN dotnet restore
COPY game.api/ ./
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory
WORKDIR /app

# Copy the published API from the build stage
COPY --from=api-build /app/publish ./

# Copy the built Vue.js app to wwwroot for static file serving
COPY --from=client-build /client/dist ./wwwroot

# Expose the port the app runs on
EXPOSE 80
EXPOSE 443

# Set the entry point
ENTRYPOINT ["dotnet", "game.api.dll"]