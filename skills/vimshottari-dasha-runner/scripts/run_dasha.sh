#!/bin/sh
set -eu

if [ "$#" -ne 1 ]; then
  echo "Usage: run_dasha.sh <source.chartFile>" >&2
  exit 1
fi

chart_file=$1
case "$chart_file" in
  /*) ;;
  *) chart_file=$(CDPATH= cd -- "$(dirname -- "$chart_file")" && pwd -P)/$(basename -- "$chart_file") ;;
esac

if [ ! -f "$chart_file" ]; then
  echo "source.chartFile does not exist: $chart_file" >&2
  exit 1
fi

case "$chart_file" in
  *.json|*.JSON) ;;
  *) echo "source.chartFile must be a JSON file: $chart_file" >&2; exit 1 ;;
esac

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
project_root=$(CDPATH= cd -- "$script_dir/../../.." && pwd -P)
chart_basename=$(basename -- "$chart_file")
chart_stem=${chart_basename%.*}
output_file=$(dirname -- "$chart_file")/${chart_stem}_dasha.json
request_dir=$(mktemp -d "${TMPDIR:-/tmp}/astrocli-dasha.XXXXXX")
trap 'rm -rf "$request_dir"' EXIT HUP INT TERM

escaped_chart_file=$(printf '%s' "$chart_file" | sed 's/\\/\\\\/g; s/"/\\"/g')
printf '{"source":{"chartFile":"%s"}}\n' "$escaped_chart_file" > "$request_dir/request.json"
rm -f "$output_file"

dotnet run --project "$project_root/src/AstroCli/AstroCli.csproj" -- dasha "$request_dir/request.json"
if [ ! -f "$request_dir/request_dasha.json" ]; then
  echo "Dasha command completed without creating an output file" >&2
  exit 1
fi

cp "$request_dir/request_dasha.json" "$output_file"
printf '%s\n' "$output_file"
