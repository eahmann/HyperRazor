# HyperRazor.E2E

Playwright browser E2E tests for Demo.Mvc core HTMX flows:
- OOB multi-region updates
- Validation (`200` invalid flow)
- Redirect headers (`HX-Location` + `HX-Redirect`)

## Run

1. One-command bootstrap + run (recommended on Linux):

```bash
tests/HyperRazor.E2E/run-e2e.sh
```

2. Manual install path:

```bash
dotnet build tests/HyperRazor.E2E/HyperRazor.E2E.csproj
pwsh tests/HyperRazor.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
```

If PowerShell is unavailable on Linux, use the bundled Node runtime:

```bash
PLAYWRIGHT_BROWSERS_PATH=/tmp/ms-playwright \
tests/HyperRazor.E2E/bin/Debug/net10.0/.playwright/node/linux-x64/node \
tests/HyperRazor.E2E/bin/Debug/net10.0/.playwright/package/cli.js install chromium
```

3. Run the E2E suite:

```bash
dotnet test tests/HyperRazor.E2E/HyperRazor.E2E.csproj -v minimal
```
