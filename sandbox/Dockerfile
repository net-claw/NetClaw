FROM debian:bookworm-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
    bash \
    build-essential \
    ca-certificates \
    chromium \
    curl \
    git \
    jq \
    nodejs \
    npm \
    python3 \
    python3-pip \
    python3-venv \
    && rm -rf /var/lib/apt/lists/*

# Point Playwright and Puppeteer-based tools (agent-browser) to system Chromium.
# This works on all architectures including ARM64 where bundled Chrome binaries are unavailable.
# Wrapper ensures --no-sandbox and --disable-dev-shm-usage are always passed,
# which is required when running Chromium as root inside Docker.
RUN printf '#!/bin/sh\nexec /usr/bin/chromium --no-sandbox --disable-dev-shm-usage --disable-gpu "$@"\n' \
    > /usr/local/bin/chromium-wrapper && chmod +x /usr/local/bin/chromium-wrapper

ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
ENV PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH=/usr/local/bin/chromium-wrapper
ENV PUPPETEER_SKIP_DOWNLOAD=1
ENV PUPPETEER_EXECUTABLE_PATH=/usr/local/bin/chromium-wrapper

WORKDIR /workspace

CMD ["sleep", "infinity"]
