#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 5055

ARG TARGETPLATFORM

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Nuget.Config", "."]
COPY ["AppBrokerASP/AppBrokerASP.csproj", "AppBrokerASP/"]
COPY ["AppBroker.Elsa/AppBroker.Elsa.csproj", "AppBroker.Elsa/"]
COPY ["AppBroker.Core/AppBroker.Core.csproj", "AppBroker.Core/"]
COPY ["AppBroker.Generators/AppBroker.Generators.csproj", "AppBroker.Generators/"]
RUN dotnet restore "AppBrokerASP/AppBrokerASP.csproj"
COPY . .
WORKDIR "/src/AppBrokerASP"

FROM build AS publish
ARG TARGETPLATFORM
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
        RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
        RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
        RID=linux-arm ; \
    fi \
    && dotnet publish -c Release -r $RID -o /app/publish /p:UseAppHost=false --self-contained false 


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppBrokerASP.dll"]