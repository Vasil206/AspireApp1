# syntax=docker/dockerfile:1.7-labs
FROM mcr.microsoft.com/dotnet/sdk:8.0.203 AS final

WORKDIR /app

COPY --parents ./AspireApp1.AppHost ./AspireApp1.ServiceDefaults ./WorkerService1 .

RUN dotnet workload install aspire

RUN dotnet dev-certs https --trust

EXPOSE 1234 

ENTRYPOINT ["dotnet", "run", "--project", "./AspireApp1.AppHost"]
