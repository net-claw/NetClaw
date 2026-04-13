# syntax=docker/dockerfile:1.7

# ── Stage 1: Build React frontend ───────────────────────────────────────────
FROM --platform=$BUILDPLATFORM node:24-alpine AS frontend-builder

RUN corepack enable && corepack prepare pnpm@10.32.1 --activate

WORKDIR /app/frontend
COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN --mount=type=cache,target=/root/.local/share/pnpm/store \
    pnpm install --frozen-lockfile

COPY frontend/ ./
RUN pnpm run build


# ── Stage 2: Restore and publish ASP.NET app ────────────────────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-builder

WORKDIR /app/backend

COPY backend/backend.sln ./
COPY backend/Core/NetClaw.Docker.Extensions/NetClaw.Docker.Extensions.csproj ./Core/NetClaw.Docker.Extensions/
COPY backend/Core/NetClaw.EfCore.Extensions/NetClaw.EfCore.Extensions.csproj ./Core/NetClaw.EfCore.Extensions/
COPY backend/Core/NetClaw.Web.Extensions/NetClaw.Web.Extensions.csproj ./Core/NetClaw.Web.Extensions/
COPY backend/Plugins/NetClaw.Plugin.Abstractions/NetClaw.Plugin.Abstractions.csproj ./Plugins/NetClaw.Plugin.Abstractions/
COPY backend/Plugins/NetClaw.Plugin.Discord/NetClaw.Plugin.Discord.csproj ./Plugins/NetClaw.Plugin.Discord/
COPY backend/Plugins/NetClaw.Plugin.Sample/NetClaw.Plugin.Sample.csproj ./Plugins/NetClaw.Plugin.Sample/
COPY backend/Plugins/NetClaw.Plugin.Slack/NetClaw.Plugin.Slack.csproj ./Plugins/NetClaw.Plugin.Slack/
COPY backend/Plugins/NetClaw.Plugin.Telegram/NetClaw.Plugin.Telegram.csproj ./Plugins/NetClaw.Plugin.Telegram/
COPY backend/Src/NetClaw.Api/NetClaw.Api.csproj ./Src/NetClaw.Api/
COPY backend/Src/NetClaw.Application/NetClaw.Application.csproj ./Src/NetClaw.Application/
COPY backend/Src/NetClaw.Cli/NetClaw.Cli.csproj ./Src/NetClaw.Cli/
COPY backend/Src/NetClaw.Contracts/NetClaw.Contracts.csproj ./Src/NetClaw.Contracts/
COPY backend/Src/NetClaw.Domains/NetClaw.Domains.csproj ./Src/NetClaw.Domains/
COPY backend/Src/NetClaw.Infra/NetClaw.Infra.csproj ./Src/NetClaw.Infra/
COPY backend/Tests/NetClaw.AcceptanceTests/NetClaw.AcceptanceTests.csproj ./Tests/NetClaw.AcceptanceTests/
COPY backend/Tests/NetClaw.IntegrationTests/NetClaw.IntegrationTests.csproj ./Tests/NetClaw.IntegrationTests/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore backend.sln

COPY backend/ ./

# Copy frontend build output into wwwroot
COPY --from=frontend-builder /app/frontend/dist ./Src/NetClaw.Api/wwwroot

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Src/NetClaw.Api \
    -c Release \
    --self-contained false \
    -p:PublishSingleFile=false \
    --no-restore \
    -o /app/out


# ── Stage 3: ASP.NET runtime image ──────────────────────────────────────────
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

WORKDIR /app
COPY --from=backend-builder /app/out/ .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NetClaw.Api.dll"]
