FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/PubSubPattern.Core/pub-sub-pattern-poc.csproj", "src/PubSubPattern.Core/"]
RUN dotnet restore "src/PubSubPattern.Core/pub-sub-pattern-poc.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet publish "src/PubSubPattern.Core/pub-sub-pattern-poc.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "pub-sub-pattern-poc.dll"] 