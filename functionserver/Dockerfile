FROM mcr.microsoft.com/dotnet/aspnet:6.0
EXPOSE 80
EXPOSE 8989
WORKDIR /app

RUN mkdir -p /Functions

COPY /dist/* ./
ENTRYPOINT ["dotnet", "sharpfaas_functionserver.dll"]
