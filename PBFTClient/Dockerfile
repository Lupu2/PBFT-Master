#Run build command from root directory and not current directory
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS base

ADD . /App
ADD ./PBFTClient /App/Client 

WORKDIR /App/Client
RUN dotnet restore "PBFTClient.csproj"
RUN dotnet build "PBFTClient.csproj"
RUN dotnet publish "PBFTClient.csproj" -c Release -o /App/Client
ENTRYPOINT ["dotnet", "PBFTClient.dll"]
