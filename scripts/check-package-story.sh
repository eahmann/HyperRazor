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

check_absent_text() {
  local file="$1"
  local disallowed="$2"

  if grep -Fq -- "$disallowed" "$file"; then
    echo "Unexpected current-story text in $file: $disallowed" >&2
    failures=1
  fi
}

check_exact_file_set() {
  local root="$1"
  shift
  local -a expected=("$@")
  mapfile -t actual < <(find "$root" -maxdepth 2 -type f -name '*.csproj' | sed "s#^$repo_root/##" | sort)

  local expected_joined actual_joined
  expected_joined="$(printf '%s\n' "${expected[@]}")"
  actual_joined="$(printf '%s\n' "${actual[@]}")"

  if [[ "$expected_joined" != "$actual_joined" ]]; then
    echo "Unexpected project layout under $root" >&2
    echo "Expected:" >&2
    printf '  %s\n' "${expected[@]}" >&2
    echo "Actual:" >&2
    printf '  %s\n' "${actual[@]}" >&2
    failures=1
  fi
}

canonical_install_files=(
  "README.md"
  "docs/quickstart.md"
  "docs/adopting-hyperrazor.md"
  "docs/package-surface.md"
  "docs/release-policy.md"
)

shared_install_lines=(
  '## Which package do I install?'
  '- Full HyperRazor app: install `HyperRazor`.'
  '- Typed HTMX only: install `HyperRazor.Htmx`.'
  '- Advanced component composition: install `HyperRazor.Components` only when you are intentionally composing on that layer.'
)

for file in "${canonical_install_files[@]}"; do
  for line in "${shared_install_lines[@]}"; do
    check_line "$file" "$line"
  done
done

classification_files=(
  "README.md"
  "docs/package-surface.md"
  "docs/release-policy.md"
)

shared_classification_text=(
  'Primary entry-point packages:'
  'Advanced but supported composition package:'
  'Internal-only projects:'
  '`HyperRazor`: the default onboarding package for a full HyperRazor app'
  '`HyperRazor.Htmx`: the default onboarding package for typed HTMX support without the full HyperRazor rendering stack'
  '- `HyperRazor.Components`'
  '- `samples/HyperRazor.Demo.Mvc`'
)

for file in "${classification_files[@]}"; do
  for text in "${shared_classification_text[@]}"; do
    check_text "$file" "$text"
  done
done

check_text "docs/README.md" '# Docs Index'
check_text "docs/README.md" '## Current docs'
check_text "docs/README.md" '## Historical docs'
check_text "docs/README.md" 'Package migration guidance'
check_text "docs/README.md" 'archive/'

for file in docs/archive/*.md; do
  check_text "$file" '> Historical document'
done

check_exact_file_set "$repo_root/src" \
  "src/HyperRazor.Components/HyperRazor.Components.csproj" \
  "src/HyperRazor.Htmx/HyperRazor.Htmx.csproj" \
  "src/HyperRazor/HyperRazor.csproj"

check_exact_file_set "$repo_root/samples" \
  "samples/HyperRazor.Demo.Api/HyperRazor.Demo.Api.csproj" \
  "samples/HyperRazor.Demo.Mvc/HyperRazor.Demo.Mvc.csproj"

published_package_files=(
  "src/HyperRazor/HyperRazor.csproj"
  "src/HyperRazor.Components/HyperRazor.Components.csproj"
  "src/HyperRazor.Htmx/HyperRazor.Htmx.csproj"
)

published_package_ids=(
  '<PackageId>HyperRazor</PackageId>'
  '<PackageId>HyperRazor.Components</PackageId>'
  '<PackageId>HyperRazor.Htmx</PackageId>'
)

for package_id in "${published_package_ids[@]}"; do
  found=0
  for file in "${published_package_files[@]}"; do
    if grep -Fq -- "$package_id" "$file"; then
      found=1
      break
    fi
  done

  if [[ "$found" -eq 0 ]]; then
    echo "Missing published package id: $package_id" >&2
    failures=1
  fi
done

current_story_files=(
  "README.md"
  "docs/README.md"
  "docs/quickstart.md"
  "docs/adopting-hyperrazor.md"
  "docs/package-surface.md"
  "docs/release-policy.md"
  "docs/validation-architecture.md"
)

retired_install_patterns=(
  'install `HyperRazor.Client`'
  'install `HyperRazor.Mvc`'
  'install `HyperRazor.Rendering`'
  'install `HyperRazor.Htmx.Core`'
  'install `HyperRazor.Htmx.Components`'
  'dotnet add package HyperRazor.Client'
  'dotnet add package HyperRazor.Mvc'
  'dotnet add package HyperRazor.Rendering'
  'dotnet add package HyperRazor.Htmx.Core'
  'dotnet add package HyperRazor.Htmx.Components'
)

for file in "${current_story_files[@]}"; do
  for pattern in "${retired_install_patterns[@]}"; do
    check_absent_text "$file" "$pattern"
  done
done

current_path_files=(
  "README.md"
  "docs/README.md"
  "docs/adopting-hyperrazor.md"
  "docs/nuget-readme.md"
  "docs/package-surface.md"
  "docs/quickstart.md"
  "docs/release-policy.md"
  "docs/validation-architecture.md"
)

retired_path_patterns=(
  'src/HyperRazor.Mvc/'
  'src/HyperRazor.Rendering/'
  'src/HyperRazor.Demo.Mvc/'
  'src/HyperRazor.Demo.Api/'
)

for file in "${current_path_files[@]}"; do
  for pattern in "${retired_path_patterns[@]}"; do
    check_absent_text "$file" "$pattern"
  done
done

migration_files=(
  "docs/package-surface.md"
  "docs/adopting-hyperrazor.md"
)

migration_lines=(
  '`HyperRazor.Client` -> `HyperRazor.Components`'
  '`HyperRazor.Mvc` -> `HyperRazor`'
  '`HyperRazor.Rendering` -> `HyperRazor`'
  '`HyperRazor.Htmx.Core` -> `HyperRazor.Htmx`'
  '`HyperRazor.Htmx.Components` -> `HyperRazor.Htmx`'
)

for file in "${migration_files[@]}"; do
  for line in "${migration_lines[@]}"; do
    check_text "$file" "$line"
  done
done

if [[ "$failures" -ne 0 ]]; then
  exit 1
fi

echo "Package-story shape is aligned."
