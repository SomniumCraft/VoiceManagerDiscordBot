FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH
WORKDIR /source

COPY VoiceLinkChatBot/*.csproj .
RUN dotnet restore -a $TARGETARCH

COPY VoiceLinkChatBot/. .
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine
WORKDIR /app
COPY --from=publish /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "VoiceLinkChatBot.dll"]
