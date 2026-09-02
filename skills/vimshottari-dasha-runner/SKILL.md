---
name: vimshottari-dasha-runner
description: Run AstroCLI's Vimshottari Dasha calculation from a local source.chartFile and return the generated dasha JSON file.
metadata:
  short-description: Calculate Vimshottari Dasha from source.chartFile
---

# Vimshottari Dasha Runner

Use this skill when the user supplies a local horoscope JSON path as `source.chartFile` and asks for Vimshottari Dasha calculation.

## Workflow

1. Resolve `source.chartFile` to an existing local JSON file. If the user did not provide a path, ask for it. Do not invent a chart file.
2. From the AstroCLI repository root, run `sh skills/vimshottari-dasha-runner/scripts/run_dasha.sh <resolved-chart-path>`. The script creates a temporary request JSON containing:

   ```json
   { "source": { "chartFile": "<absolute-chart-path>" } }
   ```

3. The script invokes `astrocli dasha` through the repository's .NET project and copies the result to the chart's directory as `<chart-name-without-extension>_dasha.json`.
4. If the command exits non-zero, report the stderr error and do not claim that an output was generated.
5. On success, verify that the output file exists. Return a clickable link to that output file. Opening it in the Codex panel is optional when that helps the user inspect it.

## Constraints

- Use the chart JSON supplied by the user as `source.chartFile`; do not use any stored Dasha result as calculation input.
- The default calculation depth is 2. For depth, period, or reference-date options, use the CLI's request JSON format explicitly rather than changing the chart file.
- The runner may replace an existing `<chart-name-without-extension>_dasha.json`, as required by the CLI's stale-output behavior.
- Keep temporary request files outside the project and remove them after the command completes.
- Do not modify the horoscope chart JSON.
