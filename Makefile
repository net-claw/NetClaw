DOTNET := $(HOME)/.dotnet/dotnet
export DOTNET_ROOT := $(HOME)/.dotnet
export PATH := $(DOTNET_ROOT):$(PATH)

FRONTEND_DIR := frontend
BACKEND_DIR  := backend/Src/NetClaw.Api
INFRA_DIR    := backend/Src/NetClaw.Infra
WWWROOT_DIR  := $(BACKEND_DIR)/wwwroot
OUT_DIR      := out
PLUGINS_DIR  := backend/Src/NetClaw.Api/plugins
CONFIG       ?= Debug
TFM          ?= net10.0

MIGRATION_TARGETS := migrations-add migrations-remove migrations-update migrations-list
MIGRATION_ARG := $(word 2,$(MAKECMDGOALS))

# Target OS/arch for self-contained binary.
# Override via: make build RUNTIME=linux-x64
# Common values: osx-arm64 | osx-x64 | linux-x64 | win-x64
RUNTIME ?= osx-arm64

IMAGE_NAME ?= netclaw
IMAGE_TAG  ?= latest

# ── Main targets ────────────────────────────────────────────────────────────

.PHONY: build build-fe copy-fe build-be run clean dev dev-local dev-down \
	docker docker-run migrations-add migrations-remove migrations-update \
	migrations-list plugin build-all-plugins help

## Build everything → single binary in ./out/
build: build-fe copy-fe build-be

## 1. Install deps and build React frontend
build-fe:
	@echo ">>> Building frontend..."
	cd $(FRONTEND_DIR) && pnpm install --frozen-lockfile && pnpm run build

## 2. Copy frontend/dist → backend/wwwroot
copy-fe:
	@echo ">>> Copying dist to wwwroot..."
	rm -rf $(WWWROOT_DIR)
	cp -r $(FRONTEND_DIR)/dist $(WWWROOT_DIR)

## 3. Publish ASP.NET as a self-contained single-file binary
build-be:
	@echo ">>> Publishing backend ($(RUNTIME))..."
	$(DOTNET) publish $(BACKEND_DIR) \
		-c Release \
		-r $(RUNTIME) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:PublishTrimmed=false \
		-o $(OUT_DIR)
	@echo ">>> Binary ready: $(OUT_DIR)/NetClaw.Api"

## Run the built binary (serves both API + SPA on :5000)
run:
	cd $(OUT_DIR) && ./NetClaw.Api --urls "http://localhost:5000"

## Remove all build artifacts
clean:
	rm -rf $(FRONTEND_DIR)/dist $(WWWROOT_DIR) $(OUT_DIR)

## Start the full dev stack with Docker Compose (app + sandbox)
dev:
	@echo ">>> Starting app + sandbox with Docker Compose..."
	docker compose up --build app

## Run backend/frontend locally, assuming sandbox is already available
dev-local:
	@echo ""
	@echo "Run these in separate terminals:"
	@echo ""
	@echo "  Terminal 0 (sandbox, one-time):"
	@echo "    docker compose up -d sandbox"
	@echo ""
	@echo "  Terminal 1 (API):"
	@echo "    cd $(BACKEND_DIR) && dotnet run --urls http://localhost:5000"
	@echo ""
	@echo "  Terminal 2 (Vite):"
	@echo "    cd $(FRONTEND_DIR) && pnpm dev"
	@echo ""

## Stop the Docker Compose dev stack
dev-down:
	@echo ">>> Stopping Docker Compose stack..."
	docker compose down

## Add a new EF Core migration into NetClaw.Infra/Migrations
migrations-add:
	@if [ -z "$(MIGRATION_ARG)" ]; then \
		echo "Usage: make migrations-add <MigrationName>"; \
		exit 1; \
	fi
	@echo ">>> Adding migration '$(MIGRATION_ARG)' to $(INFRA_DIR)..."
	$(DOTNET) ef migrations add $(MIGRATION_ARG) \
		--project $(INFRA_DIR) \
		--startup-project $(BACKEND_DIR) \
		--context AppDbContext \
		--output-dir Migrations

## Remove the last EF Core migration from NetClaw.Infra
migrations-remove:
	@echo ">>> Removing last migration from $(INFRA_DIR)..."
	$(DOTNET) ef migrations remove \
		--project $(INFRA_DIR) \
		--startup-project $(BACKEND_DIR) \
		--context AppDbContext

## Apply EF Core migrations using the API startup configuration
migrations-update:
	@echo ">>> Applying migrations using $(BACKEND_DIR)..."
	$(DOTNET) ef database update \
		--project $(INFRA_DIR) \
		--startup-project $(BACKEND_DIR) \
		--context AppDbContext

## List EF Core migrations from NetClaw.Infra
migrations-list:
	@echo ">>> Listing migrations from $(INFRA_DIR)..."
	$(DOTNET) ef migrations list \
		--project $(INFRA_DIR) \
		--startup-project $(BACKEND_DIR) \
		--context AppDbContext

## Build and install a single plugin. PLUGIN=path relative to project root.
## Example: make plugin PLUGIN=backend/Plugins/NetClaw.Plugin.Telegram
plugin:
ifndef PLUGIN
	$(error PLUGIN is required. Example: make plugin PLUGIN=backend/Plugins/NetClaw.Plugin.Telegram)
endif
	@plugin_dir=$$(echo "$(PLUGIN)" | sed 's:/*$$::'); \
	proj=$$(basename "$$plugin_dir"); \
	name=$$(echo "$$proj" | sed 's/^.*Plugin\.//' | tr 'A-Z' 'a-z'); \
	dest="$(PLUGINS_DIR)/$$name"; \
	echo ""; \
	echo ">>> [$$name] Publishing $$proj ($(CONFIG)/$(TFM))..."; \
	$(DOTNET) publish "$$plugin_dir" -c $(CONFIG) --nologo \
		--no-self-contained \
		-o "$$dest" || exit 1; \
	cp "$$plugin_dir/plugin.json" "$$dest/"; \
	echo ">>> [$$name] Done → $$dest/"

## Build and install all plugins under backend/Plugins/
build-all-plugins:
	@for dir in backend/Plugins/NetClaw.Plugin.*/; do \
		dir=$$(echo "$$dir" | sed 's:/*$$::'); \
		[ -f "$$dir/plugin.json" ] || continue; \
		$(MAKE) --no-print-directory plugin PLUGIN="$$dir" CONFIG=$(CONFIG) TFM=$(TFM) || exit 1; \
	done

## Build Docker image (multi-stage, linux/arm64 for Apple Silicon)
docker:
	docker build --platform linux/arm64 -t $(IMAGE_NAME):$(IMAGE_TAG) .

## Run Docker image locally on :5001
docker-run:
	docker run --rm --platform linux/arm64 -p 5001:8080 $(IMAGE_NAME):$(IMAGE_TAG)

## Show available targets
help:
	@echo ""
	@echo "Usage: make <target> [RUNTIME=<rid>]"
	@echo ""
	@echo "Targets:"
	@echo "  build      Full pipeline: fe → copy → publish binary"
	@echo "  build-fe   Build React frontend only"
	@echo "  copy-fe    Copy frontend/dist → backend/wwwroot"
	@echo "  build-be   Publish ASP.NET binary only"
	@echo "  run        Run the built binary on :5000"
	@echo "  clean      Remove dist/, wwwroot/, out/"
	@echo "  dev        Run app + sandbox via Docker Compose"
	@echo "  dev-local  Print local backend/frontend dev commands"
	@echo "  dev-down   Stop Docker Compose dev stack"
	@echo "  migrations-add    Add migration to NetClaw.Infra: make migrations-add <MigrationName>"
	@echo "  migrations-remove Remove last migration from NetClaw.Infra"
	@echo "  migrations-update Apply DB migrations"
	@echo "  migrations-list   List migrations"
	@echo "  docker     Build Docker image (IMAGE_NAME=netclaw IMAGE_TAG=latest)"
	@echo "  docker-run Run Docker image on :5000"
	@echo ""

ifneq ($(filter migrations-add,$(MAKECMDGOALS)),)
$(MIGRATION_ARG):
	@:
endif
	@echo "Plugin targets:"
	@echo "  plugin             Build + install one plugin (PLUGIN=backend/Plugins/...)"
	@echo "  build-all-plugins  Build + install all plugins"
	@echo ""
	@echo "Plugin options (default: CONFIG=Debug TFM=net10.0):"
	@echo "  make plugin PLUGIN=backend/Plugins/NetClaw.Plugin.Telegram"
	@echo "  make plugin PLUGIN=backend/Plugins/NetClaw.Plugin.Sample CONFIG=Release"
	@echo ""
	@echo "RUNTIME options (default: osx-arm64):"
	@echo "  osx-arm64   macOS Apple Silicon"
	@echo "  osx-x64     macOS Intel"
	@echo "  linux-x64   Linux x86-64"
	@echo "  win-x64     Windows x86-64"
	@echo ""
