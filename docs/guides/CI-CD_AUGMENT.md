# CI/CD — Minimal workflow (GitHub Actions)

## Principer
- Grinden är grön: build + test måste vara 100% grönt (inga varningar) innan deploy.
- Scriptbar validering av testlayout.

## Workflow (mall)
```yaml
name: CI
on:
  push: { branches: [main, develop] }
  pull_request: { branches: [main] }

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    env:
      DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
      DOTNET_CLI_TELEMETRY_OPTOUT: true
      CI: true
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - name: Validate test layout
        run: bash scripts/validate-test-layout.sh
      - name: Restore
        run: dotnet restore
      - name: Build (warnings as errors)
        run: dotnet build --no-restore --configuration Release /warnaserror
      - name: Test with coverage
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"




### E) `/scripts/validate-test-layout.sh`
```bash
#!/usr/bin/env bash
set -euo pipefail
fail=false
while IFS= read -r csproj; do
  name="$(basename "$(dirname "$csproj")")"
  test_csproj="tests/${name}.Tests/${name}.Tests.csproj"
  if [[ ! -f "$test_csproj" ]]; then
    echo "❌ Saknar testprojekt: $test_csproj (för $csproj)"
    fail=true
  fi
done < <(find src -maxdepth 2 -name '*.csproj' -print)
if [[ "$fail" == true ]]; then
  echo "❌ Testlayout-validering misslyckades."; exit 1
fi
echo "✅ Testlayout OK."
