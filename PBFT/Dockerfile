#Run build command from root directory and not current directory
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS base

ADD . /App
ADD ./PBFT /App/Server 

WORKDIR /App/Server
RUN dotnet restore "PBFT.csproj"
RUN dotnet build "PBFT.csproj"
RUN dotnet publish "PBFT.csproj" -c Release -o /App/Server
ENTRYPOINT ["dotnet", "PBFT.dll"]