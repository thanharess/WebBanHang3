# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file project và restore các dependencies
COPY ["WebBanHang.csproj", "./"]
RUN dotnet restore "WebBanHang.csproj"

# Copy toàn bộ mã nguồn còn lại
COPY . .
RUN dotnet publish "WebBanHang.csproj" -c Release -o /app/publish

# Stage 2: Run
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebBanHang.dll"]
