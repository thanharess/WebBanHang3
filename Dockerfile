# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WebBanHang.csproj", "./"]
RUN dotnet restore "WebBanHang.csproj"

COPY . .
RUN dotnet publish "WebBanHang.csproj" -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebBanHang.dll"]
