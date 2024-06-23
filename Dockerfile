FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base
ARG TARGETARCH
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["VoiceLinkChatBot/VoiceLinkChatBot.csproj", "VoiceLinkChatBot/"]
RUN dotnet restore "VoiceLinkChatBot/VoiceLinkChatBot.csproj" -a $TARGETARCH
COPY . .
WORKDIR "/src/VoiceLinkChatBot"
RUN dotnet build "VoiceLinkChatBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VoiceLinkChatBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VoiceLinkChatBot.dll"]
