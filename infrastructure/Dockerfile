# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /root

# Copy only the files restore needs
COPY Directory.Packages.props ./
COPY src/CustomerApi/CustomerApi.csproj src/CustomerApi/
COPY src/CustomerApi.Application/CustomerApi.Application.csproj src/CustomerApi.Application/
COPY src/CustomerApi.Domain/CustomerApi.Domain.csproj src/CustomerApi.Domain/
COPY src/CustomerApi.Infrastructure/CustomerApi.Infrastructure.csproj src/CustomerApi.Infrastructure/
RUN dotnet restore src/CustomerApi/CustomerApi.csproj

# Now copy the source and publish
COPY src/ src/
RUN dotnet publish src/CustomerApi/CustomerApi.csproj -c Release -o /app --no-restore

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CustomerApi.dll"]