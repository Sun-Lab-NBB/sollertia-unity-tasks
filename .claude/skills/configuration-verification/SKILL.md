# Configuration Verification Skill

Verifies that Unity prefabs match YAML experiment configuration files. Use this skill when creating,
modifying, or reviewing configuration files to ensure prefab positions and settings are valid.

---

## When to Use

- Creating new YAML experiment configurations
- Modifying existing configuration zone positions
- Reviewing configuration files for correctness
- Debugging zone trigger issues
- Creating new segment prefabs

---

## Configuration Structure

### Zone Position Fields in YAML

```yaml
trial_structures:
  trial_name:
    segment_name: "Segment_name"
    stimulus_trigger_zone_start_cm: 107.5      # Occupancy/trigger zone start
    stimulus_trigger_zone_end_cm: 142.5         # Occupancy/trigger zone end
    stimulus_location_cm: 157.5                 # Stimulus boundary location
    show_stimulus_collision_boundary: false
    occupancy_duration_ms: 1000                 # For occupancy-based trials
    reward_size_ul: 5.0                         # For reward-based trials
```

### Unit Conversion

The `cm_per_unity_unit` field in `vr_environment` defines the conversion factor (typically 10.0):
- **Config values are in centimeters**
- **Prefab positions are in Unity units**
- Formula: `Unity units = cm / cm_per_unity_unit`

---

## Prefab Zone Architecture

### StimulusTriggerZone.prefab (Reward Trials)

Used for lick-based reward trials. Structure:
```
StimulusTriggerZone (root)
├── Transform: local position defines zone location
├── BoxCollider: trigger zone bounds (m_Size, m_Center)
├── StimulusTriggerZone script
└── GuidanceRegion (child)
    ├── BoxCollider: guidance sub-zone
    └── GuidanceZone script
```

### OccupancyTriggerZone.prefab (Aversive/Occupancy Trials)

Used for occupancy-based aversive trials. Structure:
```
OccupancyTriggerZone (root)
├── Transform: local position (e.g., z=17.5)
├── BoxCollider: stimulus boundary (size z=3.5, center z=0)
├── StimulusTriggerZone script
└── OccupancyRegion (child)
    ├── BoxCollider: occupancy zone (size z=3.5, center z=-5)
    ├── OccupancyZone script
    └── OccupancyGuidanceRegion (child)
        ├── BoxCollider: guidance sub-zone
        └── OccupancyGuidanceZone script
```

**Key Insight**: Child colliders use `m_Center` offsets to position zones relative to parent transform.

---

## Verification Procedure

### Step 1: Extract Config Values

From the YAML configuration file:
```yaml
vr_environment:
  cm_per_unity_unit: 10.0

trial_structures:
  ABCD:
    segment_name: "Segment_airPuff1"
    stimulus_trigger_zone_start_cm: 107.5
    stimulus_trigger_zone_end_cm: 142.5
    stimulus_location_cm: 157.5
```

### Step 2: Locate Segment Prefab

Find the prefab referenced by `segment_name`:
```
Assets/InfiniteCorridorTask/Prefabs/{segment_name}.prefab
```

### Step 3: Extract Prefab Zone Positions

Search for nested prefab instances and their transforms:
```bash
# Find zone prefab references
grep -n "m_SourcePrefab:" Segment_{name}.prefab

# Find zone transform positions
grep -B5 -A15 "PrefabInstance:" Segment_{name}.prefab | grep "m_LocalPosition"
```

### Step 4: Calculate Effective Zone Positions

For each zone, calculate the effective position:
```
Effective Position = Parent Local Position + Collider Center Offset
Zone Range = Effective Position ± (Collider Size / 2)
```

**Example** (OccupancyTriggerZone at z=17.5):
- OccupancyRegion collider: center z=-5, size z=3.5
- Effective center: 17.5 + (-5) = 12.5 Unity units
- Range: 12.5 ± 1.75 = 10.75 to 14.25 Unity units
- In cm: 107.5 to 142.5 cm

### Step 5: Compare Against Config

| Zone              | Prefab Position | Config Range  | Status |
|-------------------|-----------------|---------------|--------|
| OccupancyRegion   | 107.5-142.5cm   | 107.5-142.5cm | Valid  |
| Stimulus Boundary | 157.5-192.5cm   | 157.5cm       | Valid  |

---

## Common Zone Prefab GUIDs

Reference for identifying zone types in prefab files:

| Prefab                      | GUID                             | Purpose               |
|-----------------------------|----------------------------------|-----------------------|
| ResetZone.prefab            | 78e4c512d0af3c44cbfbb233f81d345f | Lap reset trigger     |
| StimulusTriggerZone.prefab  | e502aa673cd52774593125318db2aeb3 | Reward trial zones    |
| OccupancyTriggerZone.prefab | 3d9e6b3219444f94e85ebcb948ade18a | Occupancy trial zones |

---

## Validation Checklist

When verifying configuration files:

1. **Segment exists**: Prefab file exists at `Prefabs/{segment_name}.prefab`
2. **Zone present**: Segment contains appropriate zone prefab instance
3. **Position valid**: Zone position is within segment length bounds
4. **Range matches**: Calculated zone range matches config start/end values
5. **Stimulus location**: Boundary trigger aligns with `stimulus_location_cm`

---

## Known Implementation Gap

**Important**: The YAML position fields (`stimulus_trigger_zone_start_cm`, etc.) are:
- Defined in YAML configuration files
- **NOT** declared in the `BaseTrial` C# class
- **NOT** used by `CreateTask.cs` at runtime

Zone positions are determined entirely by prefab transforms. The YAML values serve as
documentation/specification but are not programmatically applied. Always verify prefab positions match
intended config values manually.

---

## Segment Length Validation

Ensure zones are within segment bounds:

```
Segment Length = Sum of cue lengths (in cm)
Zone Position < Segment Length
```

Example:
- Segment with cues A,B,C,D (50cm each) = 200cm total
- Zone at 175cm: Valid (175 < 200)
- Zone at 210cm: Invalid (exceeds segment)
