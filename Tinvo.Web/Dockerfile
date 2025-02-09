FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk --no-cache add tzdata \
    && cp /usr/share/zoneinfo/Asia/Shanghai /etc/localtime \
    && echo "Asia/Shanghai" > /etc/timezone

WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
WORKDIR "/src/Tinvo.Web"
RUN dotnet workload restore
RUN dotnet restore "Tinvo.Web.csproj"

RUN apt-get update
RUN apt-get install -y python3

RUN dotnet build "Tinvo.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tinvo.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV TZ=Asia/Shanghai
ENV ASPNETCORE_HTTP_PORTS=80

ENTRYPOINT ["dotnet", "Tinvo.Web.dll", "wasm"]