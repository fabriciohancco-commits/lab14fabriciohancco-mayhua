FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["lab14fabriciohancco/lab14fabriciohancco.csproj", "lab14fabriciohancco/"]
RUN dotnet restore "lab14fabriciohancco/lab14fabriciohancco.csproj"
COPY . .
WORKDIR /src/lab14fabriciohancco
RUN dotnet publish "lab14fabriciohancco.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "lab14fabriciohancco.dll"]
