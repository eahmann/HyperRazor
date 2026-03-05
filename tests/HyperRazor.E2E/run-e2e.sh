#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT_PATH="$SCRIPT_DIR/HyperRazor.E2E.csproj"
BUILD_OUTPUT_DIR="$SCRIPT_DIR/bin/Debug/net10.0"
NODE_BIN="$BUILD_OUTPUT_DIR/.playwright/node/linux-x64/node"
CLI_JS="$BUILD_OUTPUT_DIR/.playwright/package/cli.js"
BROWSERS_PATH="${PLAYWRIGHT_BROWSERS_PATH:-/tmp/ms-playwright}"

echo "Building E2E project..."
dotnet build "$PROJECT_PATH"

if [[ ! -x "$NODE_BIN" || ! -f "$CLI_JS" ]]; then
  echo "Playwright toolchain files were not found after build." >&2
  echo "Expected:" >&2
  echo "  $NODE_BIN" >&2
  echo "  $CLI_JS" >&2
  exit 1
fi

echo "Installing Linux browser dependencies and Chromium..."
if command -v sudo >/dev/null 2>&1; then
  sudo env PLAYWRIGHT_BROWSERS_PATH="$BROWSERS_PATH" "$NODE_BIN" "$CLI_JS" install --with-deps chromium
else
  echo "sudo is not available, running browser install without --with-deps." >&2
  echo "If browser launch fails, install Playwright host dependencies manually." >&2
  PLAYWRIGHT_BROWSERS_PATH="$BROWSERS_PATH" "$NODE_BIN" "$CLI_JS" install chromium
fi

echo "Running E2E tests..."
PLAYWRIGHT_BROWSERS_PATH="$BROWSERS_PATH" dotnet test "$PROJECT_PATH" -v minimal
