FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet workload install aspire

COPY . /source

WORKDIR /source

RUN dotnet publish -c Release --self-contained false -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app .

EXPOSE 1234

ENTRYPOINT ["dotnet", "AspireApp1.dll"]
