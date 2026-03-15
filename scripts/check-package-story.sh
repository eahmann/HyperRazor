#!/usr/bin/env bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

failures=0

check_line() {
  local file="$1"
  local expected="$2"

  if ! grep -Fqx -- "$expected" "$file"; then
    echo "Missing line in $file: $expected" >&2
    failures=1
  fi
}

check_text() {
  local file="$1"
  local expected="$2"

  if ! grep -Fq -- "$expected" "$file"; then
    echo "Missing text in $file: $expected" >&2
    failures=1
  fi
}

install_files=(
  "README.md"
  "docs/package-surface.md"
  "docs/quickstart.md"
  "docs/adopting-hyperrazor.md"
  "docs/nuget-readme.md"
  "docs/release-policy.md"
)

shared_install_lines=(
  '## Which package do I install?'
  '- Full HyperRazor app: install `HyperRazor`.'
  '- Typed HTMX only: install `HyperRazor.Htmx`.'
  '- Advanced composition: install the lower-level packages directly only when you are intentionally composing on those layers.'
)

for file in "${install_files[@]}"; do
  for line in "${shared_install_lines[@]}"; do
    check_line "$file" "$line"
  done
done

classification_files=(
  "README.md"
  "docs/package-surface.md"
  "docs/nuget-readme.md"
  "docs/release-policy.md"
)

shared_classification_text=(
  'Primary entry-point packages:'
  'Advanced but supported composition packages'
  'Internal-only projects:'
  '`HyperRazor`: the default onboarding package for a full HyperRazor app'
  '`HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack'
)

for file in "${classification_files[@]}"; do
  for text in "${shared_classification_text[@]}"; do
    check_text "$file" "$text"
  done
done

metadata_files=(
  "src/HyperRazor/HyperRazor.csproj"
  "src/HyperRazor.Htmx/HyperRazor.Htmx.csproj"
)

for file in "${metadata_files[@]}"; do
  check_text "$file" '<Description>Default onboarding package'
done

advanced_metadata_files=(
  "src/HyperRazor.Client/HyperRazor.Client.csproj"
  "src/HyperRazor.Components/HyperRazor.Components.csproj"
  "src/HyperRazor.Htmx.Core/HyperRazor.Htmx.Core.csproj"
  "src/HyperRazor.Htmx.Components/HyperRazor.Htmx.Components.csproj"
  "src/HyperRazor.Mvc/HyperRazor.Mvc.csproj"
  "src/HyperRazor.Rendering/HyperRazor.Rendering.csproj"
)

for file in "${advanced_metadata_files[@]}"; do
  check_text "$file" '<Description>Supported advanced composition package'
done

if [[ "$failures" -ne 0 ]]; then
  exit 1
fi

echo "Package-story messaging is aligned."
