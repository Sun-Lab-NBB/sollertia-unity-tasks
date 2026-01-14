# Task Template Verification Skill

Verifies YAML task templates against Unity prefab state using pre-baked expected values.

---

## Verification Workflow

Follow this procedure exactly. Most verifications require NO bash commands.

### Step 1: Compare Against Pre-Baked Values

For each template being verified, compare the YAML content against the **Expected Values** tables below:

1. **Read the template YAML** to extract: `cm_per_unity_unit`, `cue_offset_cm`, segment names, trial zone ranges
2. **Look up each segment** in the Current Segment Prefabs table
3. **Verify zone ranges** using the formula: `range = (zone_z ± size/2) × cm_per_unity_unit`
4. **Report results**: List each template as PASS or FAIL with specific mismatches

**If all values match the pre-baked tables: Verification complete. Report PASS.**

### Step 2: Handle Inconsistencies

If a template or prefab value does NOT match the pre-baked tables:

1. **Run the Single-Pass Extraction Script** (see below) to get actual values from files
2. **Determine the source of mismatch**:
   - If **template changed**: Verify the new template values are correct, update Expected Values tables
   - If **prefab changed**: Verify the prefab is correct, update Expected Values tables
   - If **pre-baked data was wrong**: Fix the tables
3. **Update this skill file** with corrected Expected Values
4. **Notify the user** of what changed and the final verification result

### Step 3: Report Results

Provide a summary table:

```
| Template | Status | Notes |
|----------|--------|-------|
| Name     | PASS   |       |
| Name     | FAIL   | Reason|
```

---

## Expected Values (Pre-Baked)

Use these tables for Step 1 verification. These values were extracted from actual files and should match.

### Templates

| Template               | cm_per_unit | cue_offset | Segments Used                                                              |
|------------------------|-------------|------------|----------------------------------------------------------------------------|
| MF_Aversion_Reward     | 10.0        | 10.0       | Segment_airPuff1, Segment_airPuff2                                         |
| MF_Reward              | 10.0        | 10.0       | Segment_abcdefgh                                                           |
| SSO_Connection         | 10.0        | 10.0       | Segment_abc_40cm, Segment_abca_40cm, Segment_defg_40cm, Segment_defgd_40cm |
| SSO_Connection_Base    | 10.0        | 10.0       | Segment_defg_40cm                                                          |
| SSO_Extension_Shortcut | 10.0        | 10.0       | Segment_abc_40cm, Segment_abdc_40cm                                        |
| SSO_Merging            | 10.0        | 10.0       | Segment_abc_40cm, Segment_agfe_40cm                                        |
| SSO_Merging_Base       | 10.0        | 10.0       | Segment_agfe_40cm                                                          |
| SSO_Shared_Base        | 10.0        | 10.0       | Segment_abc_40cm                                                           |
| SSO_Shortcut_Base      | 10.0        | 10.0       | Segment_abdc_40cm                                                          |

### Segment Prefabs

| Segment            | Wall Scale | Cue Count | Zone Type | Zone Z | Zone Size | Reset Z |
|--------------------|------------|-----------|-----------|--------|-----------|---------|
| Segment_abc_40cm   | 24         | 6         | Stimulus  | 18     | 2.4       | 1       |
| Segment_abca_40cm  | 32         | 8         | Stimulus  | 18     | 2.4       | 1       |
| Segment_abdc_40cm  | 32         | 8         | Stimulus  | 26     | 2.4       | 1       |
| Segment_agfe_40cm  | 32         | 8         | Stimulus  | 18     | 2.4       | 1       |
| Segment_defg_40cm  | 32         | 8         | Stimulus  | 26     | 2.4       | 1       |
| Segment_defgd_40cm | 40         | 10        | Stimulus  | 26     | 2.4       | 1       |
| Segment_abcdefgh   | 40         | 8         | Stimulus  | 37.5   | 3.5       | 1       |
| Segment_airPuff1   | 20         | 4         | Occupancy | 17.5   | 3.5       | 1       |
| Segment_airPuff2   | 20         | 4         | Stimulus  | 17.5   | 3.5       | 1       |

### Zone Range Formulas

**StimulusTriggerZone** (center=0):
```
range_start_cm = (zone_z - size/2) × cm_per_unity_unit
range_end_cm = (zone_z + size/2) × cm_per_unity_unit
```

**OccupancyTriggerZone** (occupancy region center=-5):
```
occupancy_start_cm = (zone_z - 5 - size/2) × cm_per_unity_unit
occupancy_end_cm = (zone_z - 5 + size/2) × cm_per_unity_unit
```

### Pre-Computed Zone Ranges

| Segment            | Zone Range (cm)   | Calculation                        |
|--------------------|-------------------|------------------------------------|
| Segment_abc_40cm   | 168.0 - 192.0     | (18 ± 1.2) × 10                    |
| Segment_abca_40cm  | 168.0 - 192.0     | (18 ± 1.2) × 10                    |
| Segment_abdc_40cm  | 248.0 - 272.0     | (26 ± 1.2) × 10                    |
| Segment_agfe_40cm  | 168.0 - 192.0     | (18 ± 1.2) × 10                    |
| Segment_defg_40cm  | 248.0 - 272.0     | (26 ± 1.2) × 10                    |
| Segment_defgd_40cm | 248.0 - 272.0     | (26 ± 1.2) × 10                    |
| Segment_abcdefgh   | 357.5 - 392.5     | (37.5 ± 1.75) × 10                 |
| Segment_airPuff1   | 107.5 - 142.5     | ((17.5 - 5) ± 1.75) × 10 occupancy |
| Segment_airPuff2   | 157.5 - 192.5     | (17.5 ± 1.75) × 10                 |

### Cue Offset Verification

All templates use `cue_offset_cm: 10.0`. All prefabs have `reset_z: 1`.
Verification: `1 × 10.0 = 10.0` cm.

---

## Extraction Commands (Use Only When Inconsistencies Found)

Only run these commands if Step 1 comparison reveals mismatches or if updating pre-baked values.

### Single-Pass Extraction Script

```bash
TEMPLATE_DIR="Assets/InfiniteCorridorTask/Configurations"
PREFAB_DIR="Assets/InfiniteCorridorTask/Prefabs"

echo "## Templates"
for yaml in "$TEMPLATE_DIR"/*.yaml; do
  name=$(basename "$yaml" .yaml)
  cm_per_unit=$(grep "cm_per_unity_unit:" "$yaml" | grep -oP '[0-9.]+')
  cue_offset=$(grep "cue_offset_cm:" "$yaml" | grep -oP '[0-9.]+')
  segments=$(grep -oP 'name: "Segment_[^"]+' "$yaml" | cut -d'"' -f2 | sort -u | tr '\n' ',' | sed 's/,$//')
  echo "$name|cm_per_unit=$cm_per_unit|cue_offset=$cue_offset|segments=[$segments]"
done

echo ""
echo "## Segment Prefabs"
for prefab in "$PREFAB_DIR"/Segment_*.prefab; do
  seg=$(basename "$prefab" .prefab)
  wall=$(awk '/m_Name: (Left|Right)Wall/{f=1} f && /m_LocalScale:/{print; f=0; exit}' "$prefab" | grep -oP 'x: \K[0-9.]+')
  reset_z=$(grep -A15 "value: ResetZone" "$prefab" | grep "m_LocalPosition.z" -A1 | grep "value:" | head -1 | grep -oP 'value: \K[0-9.]+')
  stim_z=$(grep -A50 "guid: e502aa673cd52774593125318db2aeb3" "$prefab" | grep "m_LocalPosition.z" -A1 | grep "value:" | head -1 | grep -oP 'value: \K[0-9.]+')
  stim_size=$(grep -A100 "guid: e502aa673cd52774593125318db2aeb3" "$prefab" | grep "m_Size.z" -A1 | grep "value:" | head -1 | grep -oP 'value: \K[0-9.]+')
  occ_z=$(grep -A50 "guid: 3d9e6b3219444f94e85ebcb948ade18a" "$prefab" | grep "m_LocalPosition.z" -A1 | grep "value:" | head -1 | grep -oP 'value: \K[0-9.]+')
  echo "$seg|wall=$wall|reset_z=$reset_z|stim_z=$stim_z|stim_size=$stim_size|occ_z=$occ_z"
done
```

---

## Codebase Structure Reference

### Directory Layout

```
Assets/InfiniteCorridorTask/
├── Configurations/           # YAML task templates
│   └── {TemplateName}.yaml
├── Prefabs/                  # Unity prefabs
│   ├── Padding.prefab
│   ├── ResetZone.prefab
│   ├── StimulusTriggerZone.prefab
│   ├── OccupancyTriggerZone.prefab
│   └── Segment_*.prefab
└── Scripts/
```

### Zone Prefab GUIDs (Stable)

| Prefab                      | GUID                               |
|-----------------------------|------------------------------------|
| ResetZone.prefab            | `78e4c512d0af3c44cbfbb233f81d345f` |
| StimulusTriggerZone.prefab  | `e502aa673cd52774593125318db2aeb3` |
| OccupancyTriggerZone.prefab | `3d9e6b3219444f94e85ebcb948ade18a` |

### Verification Formulas

| Check              | Formula                                                                   |
|--------------------|---------------------------------------------------------------------------|
| **Segment length** | `wall_scale = sum(cue.length_cm for cue in cue_sequence) / cm_per_unit`   |
| **Cue offset**     | `reset_z × cm_per_unity_unit = cue_offset_cm`                             |
| **Zone range**     | `(zone_z ± size/2) × cm_per_unity_unit`                                   |

---

## Validation Checklist

### Schema Compliance
1. Cue codes unique (0-255)
2. Cue names unique
3. Cue lengths positive
4. Segment cue_sequence references valid cues
5. Transition probabilities sum to 1.0 (±0.001)
6. Trial segment_names reference valid segments

### Prefab Compliance
1. `Prefabs/{segment_name}.prefab` exists for each segment
2. `Prefabs/Padding.prefab` exists
3. Wall scale matches segment length
4. ResetZone z × cm_per_unity_unit = cue_offset_cm
5. Zone ranges match template values
