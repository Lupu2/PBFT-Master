# Practical Byzantine Fault Tolerance Implementation

## Contents

### PBFT Implementation Info

### Cleipnir Info

## Practical Byzantine Fault Tolerance Implementation
#### Introduction
In this Github directory, you can find Jørgen Melstveit's implementation of the consensus algorithm Practical Byzantine Fault Tolerance(PBFT) using the .Net framework Cleipnir. The Cleipnir framework is made by Thomas Sylvest Stidsborg. Information about Cleipnir is presented in the Cleipnir section.
Additionally, the official Github repository for Cleipnir can be found here: [https://github.com/stidsborg/Cleipnir].

The PBFT implementation consists of the code found in the folders PBFT and PBFTClient. The source code for the Cleipnir framework is found in the Cleipnir folders. The PBFT.Tests folders consist of the unit tests performed for the PBFT implementation. It is important that the references listed in PBFT.csproj and PBFTClient.csproj are all working before attempting to run the PBFT implementation. 

##### General info about Server
The PBFT directory contains the code needed to perform the PBFT protocol over several servers. Running the executable created from the PBFT directory will result in a single instance of a server participating in the PBFT protocol. This directory is where the majority of the code for the PBFT implementation will be found. This includes messages types, protocol workflow, networking, and handling incoming messages. 

This project requires 3 arguments provided by the user in order to run properly. 
The first argument sets the server id, which is assigned using the parameter id. Example: id=0, will create a server using the identifier 0, and will use information stored for server 0 during its execution. 
The second argument tells the application whether the application is run on a local machine or over multiple hosts/docker containers. This argument uses the parameter test, where test=true tells the application to use localhost IP addresses for networking. 
The third argument tells the application whether or not to use Cleipnirs persistency functionality to recover the application's state. This argument uses the parameter per, where per=true tells the application to recover the stored data for the server and attempt to rejoin the rest of the system. We assume that per=true is only used if the server crashed/stopped during runtime and now attempts to connect back to the system again like nothing ever happened. Unfortunately, persistency is not fully supported in this PBFT implementation, and running the application with the per argument will not cause the desired results.
The test and per parameters have default values in the case where the user does not list these parameters. Both of these parameters have defaulted to false value. 

##### General info about Clients
The PBFTClient directory contains the code needed to create a client application for the PBFT implementation. Running the executable created from the PBFTClient directory will create a single instance of a PBFT client. Similar to the server, the client has 2 arguments that have to be set before the application can run properly. These two arguments are the client id and the test parameter, which essentially works just the same as they do for the server implementation. The client is interactable, meaning the user will be asked to write the message, which will be sent as a request to the servers.

The PBFT implementation is recommended to be run using 4 servers and 1 client. By design, it is possible to run the implementation with more servers and clients than the recommended value. However, the application has not been fully tested for this; therefore, unexpected results may occur.

#### Run on local machine
Running the project locally requires Microsoft Dotnet SDK version 5, this can be downloaded from this page: [https://dotnet.microsoft.com/download/dotnet/5.0].
After installing the correct SDK version for your system:

1. Restore dependencies listed in PBFT.csproj and PBFTClient.csproj: ```dotnet restore```.
2. Build the dotnet project: ```dotnet build```.

To start a server:

3a. Move to the PBFT directory ```cd PBFT```

3b. Call dotnet run using the test=true argument. Example running server 0 requires the command: ```dotent run id=0 test=true per=false```

To start a client:

4a. Move to the PBFTClient directory: ```cd PBFTClient```

4b. Call dotnet run using the test=true argument. Example running client 0 requires the command: ```dotnet run id=0 test=true```

#### Run through docker
Steps for running the PBFT implementation using docker containers is listed under:

1. Go to root directory PBFT-Master
2. Build the server image: ```docker build -t pbftserver -f PBFT/Dockerfile .```
3. Build the client image: ```docker build -t pbftclient -f PBFTClient/Dockerfile .```
4. Set up docker network: ```docker network create --subnet=192.168.2.0/16 pbftnetwork```
Make sure the new network does not intersect any other docker network! Alternatively change ip addresses for servers in PBFT/JSONFiles/serverInfo and PBFTClient/JSONFiles/serverInfo
6. Run Containers: Need atleast 5 terminals: 4 servers and 1 client! 
Example: 
``` 
Serv0: docker run -it --name serv0 --net pbftnetwork --ip 192.168.2.0 --rm pbftserver id=0 test=false per=false
Serv1: docker run -it --name serv1 --net pbftnetwork --ip 192.168.2.1 --rm pbftserver id=1 test=false per=false
Serv2: docker run -it --name serv2 --net pbftnetwork --ip 192.168.2.2 --rm pbftserver id=2 test=false per=false
Serv3: docker run -it --name serv3 --net pbftnetwork --ip 192.168.2.3 --rm pbftserver id=3 test=false per=false
Client0: docker run -it --name client0 --net pbftnetwork --ip 192.168.2.5 --rm pbftclient id=0 test=false
Client1: docker run -it --name client1 --net pbftnetwork --ip 192.168.2.6 --rm pbftclient id=1 test=false
```

A docker-compose file exists within the PBFT folder. However, running the PBFT implementation using docker-compose is currently not supported due to a networking problem between the containers!