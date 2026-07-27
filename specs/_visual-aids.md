## Visual aids standard (all UI features)

Where a feature surfaces operational, forecast, or optimization outcomes in the
Blazor UI, prefer **maps and charts** over tables-only layouts. Tables remain
for detail drill-down; the first useful view SHOULD include at least one spatial
or temporal visual.

| Visual | Typical use |
|--------|-------------|
| **Network map** | Farms, facilities, customers (lat/lon); optional recommended flows as arcs |
| **Time-series chart** | Actuals vs forecast bands; price history; replay day stepping |
| **Bar / stacked bar** | Inventory by facility/product; margin vs cost breakdown |
| **Age / risk histogram** | Lot age or days-to-expiry distribution |
| **Compare chart** | Base vs scenario objectives; baseline vs optimizer regret |
| **Flow / sankey (optional)** | Origin→destination recommended pounds when volume warrants |

Rules:

- Status and risk MUST NOT rely on color alone (patterns, labels, or icons).
- Every chart/map MUST state data classification (Synthetic / Public / Forecast /
  Recommendation).
- Charts MUST expose accessible text alternatives (summary sentence or data table
  toggle).
- Map tiles may be schematic (no commercial map API required for demo); lat/lon
  scatter on a regional plot is acceptable.

Features **004**, **005–007**, **009–010**, **012** MUST list concrete visuals in
`spec.md`. Feature **000** SHOULD add a minimal network sketch when polished;
**001** SHOULD show entity locations on a simple network map when browsing.
