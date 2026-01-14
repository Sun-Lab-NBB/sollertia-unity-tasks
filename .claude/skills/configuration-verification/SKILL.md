# Configuration Verification Skill

Verifies that YAML experiment configuration files comply with the sl-shared-assets schema and that Unity prefabs
match the configuration values. Use this skill when creating, modifying, or reviewing configuration files.

---

## When to Use

- Creating new YAML experiment configurations
- Modifying existing configuration zone positions or trial structures
- Reviewing configuration files for correctness
- Debugging zone trigger issues
- Creating new segment prefabs
- Verifying configuration compliance with sl-shared-assets schema

---

## sl-shared-assets Version Verification

**CRITICAL**: Before validating any configuration, verify the local sl-shared-assets version matches the latest release.

### Verification Steps

1. **Check local version**:
   ```bash
   grep -E "^version" /home/cyberaxolotl/Desktop/GitHubRepos/sl-shared-assets/pyproject.toml
   ```

2. **Check latest GitHub release**:
   ```bash
   gh api repos/Sun-Lab-NBB/sl-shared-assets/releases/latest --jq '.tag_name'
   ```

3. **If versions differ**: Notify the user and ask whether to:
   - Use the online version (fetch from GitHub)
   - Update the local copy before proceeding

### Source File Location

The authoritative schema is defined in:
```
sl-shared-assets/src/sl_shared_assets/data_classes/configuration_data.py
```

---

## Complete Configuration Schema

### MesoscopeExperimentConfiguration (Root)

The root configuration class that defines an experiment session. All YAML configuration files must conform to this
structure.

```yaml
# Required fields
cues: []                           # list[Cue] - VR wall cues
segments: []                       # list[Segment] - VR corridor segments
trial_structures: {}               # dict[str, WaterRewardTrial | GasPuffTrial]
experiment_states: {}              # dict[str, MesoscopeExperimentState]
vr_environment: {}                 # VREnvironment - corridor configuration
unity_scene_name: ""               # str - Unity scene name (must match filename)

# Optional fields
cue_offset_cm: 0.0                 # float - animal starting position offset
```

---

## Data Class Definitions

### Cue

Defines a single visual cue used in the VR environment.

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `name` | str | Yes | Unique | Visual identifier (e.g., 'A', 'B', 'Gray') |
| `code` | int | Yes | 0-255, unique | uint8 code for MQTT communication |
| `length_cm` | float | Yes | > 0 | Length of the cue in centimeters |

**Validation Rules**:
- `code` must be a uint8 value (0-255)
- `code` must be unique across all cues
- `name` must be unique across all cues
- `length_cm` must be positive

```yaml
cues:
  - name: "A"
    code: 1
    length_cm: 50.0
  - name: "Gray"
    code: 0
    length_cm: 25.0
```

---

### Segment

Defines a visual segment (sequence of cues) in the VR environment.

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `name` | str | Yes | Must match prefab filename | Unity prefab identifier |
| `cue_sequence` | list[str] | Yes | Non-empty, valid cue names | Ordered sequence of cue names |
| `transition_probabilities` | list[float] | No | Sum to 1.0 (±0.001 tolerance) | Probabilities to other segments |

**Validation Rules**:
- `cue_sequence` must have at least one cue
- All cue names in `cue_sequence` must reference defined cues
- If `transition_probabilities` is provided, it must sum to 1.0 (within ±0.001 tolerance)
- `name` must match an existing prefab in `Assets/InfiniteCorridorTask/Prefabs/`

```yaml
segments:
  - name: "Segment_ABCD"
    cue_sequence: ["A", "B", "C", "D"]
    transition_probabilities: [0.5, 0.5]  # Optional
```

---

### VREnvironment

Defines the Unity VR corridor system configuration.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `corridor_spacing_cm` | float | 20.0 | Horizontal spacing between corridor instances |
| `segments_per_corridor` | int | 3 | Number of segments visible in each corridor |
| `padding_prefab_name` | str | "Padding" | Unity prefab for corridor padding |
| `cm_per_unity_unit` | float | 10.0 | Conversion factor: cm to Unity units |

**Unit Conversion Formula**:
```
Unity units = cm / cm_per_unity_unit
cm = Unity units × cm_per_unity_unit
```

```yaml
vr_environment:
  corridor_spacing_cm: 20.0
  segments_per_corridor: 3
  padding_prefab_name: "Padding"
  cm_per_unity_unit: 10.0
```

---

### WaterRewardTrial

Defines a trial that delivers water rewards when the animal licks in the trigger zone.

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `segment_name` | str | Yes | Must reference defined segment | Unity segment for this trial |
| `stimulus_trigger_zone_start_cm` | float | Yes | 0 ≤ value ≤ trial_length | Zone start position |
| `stimulus_trigger_zone_end_cm` | float | Yes | ≥ start, ≤ trial_length | Zone end position |
| `stimulus_location_cm` | float | Yes | ≥ zone_start, ≤ trial_length | Stimulus boundary location |
| `show_stimulus_collision_boundary` | bool | No | - | Show boundary marker (default: false) |
| `reward_size_ul` | float | No | > 0 | Water volume in microliters (default: 5.0) |
| `reward_tone_duration_ms` | int | No | > 0 | Auditory tone duration (default: 300) |

**Trigger Mode**: Animal must lick while inside the stimulus trigger zone to receive the water reward.

**Guidance Mode**: Animal receives the reward upon colliding with the stimulus boundary (no lick required).

```yaml
trial_structures:
  ABCD_reward:
    segment_name: "Segment_ABCD"
    stimulus_trigger_zone_start_cm: 107.5
    stimulus_trigger_zone_end_cm: 142.5
    stimulus_location_cm: 157.5
    show_stimulus_collision_boundary: false
    reward_size_ul: 5.0
    reward_tone_duration_ms: 300
```

---

### GasPuffTrial

Defines a trial that delivers N2 gas puffs when the animal fails to meet occupancy duration.

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `segment_name` | str | Yes | Must reference defined segment | Unity segment for this trial |
| `stimulus_trigger_zone_start_cm` | float | Yes | 0 ≤ value ≤ trial_length | Zone start position |
| `stimulus_trigger_zone_end_cm` | float | Yes | ≥ start, ≤ trial_length | Zone end position |
| `stimulus_location_cm` | float | Yes | ≥ zone_start, ≤ trial_length | Stimulus boundary location |
| `show_stimulus_collision_boundary` | bool | No | - | Show boundary marker (default: false) |
| `puff_duration_ms` | int | No | > 0 | Gas puff duration (default: 100) |
| `occupancy_duration_ms` | int | No | > 0 | Required occupancy time (default: 1000) |

**Trigger Mode**: Animal must occupy the trigger zone for the specified duration to disarm the stimulus boundary
and avoid the gas puff.

**Guidance Mode**: When the animal exits early, OccupancyFailed is emitted for movement blocking.

```yaml
trial_structures:
  ABCD_aversive:
    segment_name: "Segment_airPuff1"
    stimulus_trigger_zone_start_cm: 107.5
    stimulus_trigger_zone_end_cm: 142.5
    stimulus_location_cm: 157.5
    show_stimulus_collision_boundary: false
    puff_duration_ms: 100
    occupancy_duration_ms: 1000
```

---

### MesoscopeExperimentState

Defines the structure and runtime parameters of an experiment state (phase).

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `experiment_state_code` | int | Required | Unique identifier for the experiment state |
| `system_state_code` | int | Required | Data acquisition system state code |
| `state_duration_s` | float | Required | Duration to maintain this state |
| `supports_trials` | bool | true | Whether trials execute during this state |
| `reinforcing_initial_guided_trials` | int | 0 | Initial guided reinforcing trials |
| `reinforcing_recovery_failed_threshold` | int | 0 | Failed trials before recovery mode |
| `reinforcing_recovery_guided_trials` | int | 0 | Guided trials in recovery mode |
| `aversive_initial_guided_trials` | int | 0 | Initial guided aversive trials |
| `aversive_recovery_failed_threshold` | int | 0 | Failed trials before recovery mode |
| `aversive_recovery_guided_trials` | int | 0 | Guided trials in recovery mode |

```yaml
experiment_states:
  habituation:
    experiment_state_code: 1
    system_state_code: 1
    state_duration_s: 300.0
    supports_trials: false
  training:
    experiment_state_code: 2
    system_state_code: 2
    state_duration_s: 1800.0
    supports_trials: true
    reinforcing_initial_guided_trials: 5
    reinforcing_recovery_failed_threshold: 3
    reinforcing_recovery_guided_trials: 2
```

---

## Zone Position Validation Rules

The `_MesoscopeBaseTrial.validate_zones()` method enforces these rules:

1. **Zone ordering**: `stimulus_trigger_zone_end_cm` ≥ `stimulus_trigger_zone_start_cm`
2. **Zone start bounds**: 0 ≤ `stimulus_trigger_zone_start_cm` ≤ `trial_length_cm`
3. **Zone end bounds**: 0 ≤ `stimulus_trigger_zone_end_cm` ≤ `trial_length_cm`
4. **Stimulus bounds**: 0 ≤ `stimulus_location_cm` ≤ `trial_length_cm`
5. **Stimulus after zone start**: `stimulus_location_cm` ≥ `stimulus_trigger_zone_start_cm`

**Trial length calculation**:
```
trial_length_cm = sum(cue.length_cm for cue in segment.cue_sequence)
```

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

## Prefab Verification Procedure

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

| Zone | Prefab Position | Config Range | Status |
|------|-----------------|--------------|--------|
| OccupancyRegion | 107.5-142.5cm | 107.5-142.5cm | Valid |
| Stimulus Boundary | 157.5-192.5cm | 157.5cm | Valid |

---

## Common Zone Prefab GUIDs

Reference for identifying zone types in prefab files:

| Prefab | GUID | Purpose |
|--------|------|---------|
| ResetZone.prefab | 78e4c512d0af3c44cbfbb233f81d345f | Lap reset trigger |
| StimulusTriggerZone.prefab | e502aa673cd52774593125318db2aeb3 | Reward trial zones |
| OccupancyTriggerZone.prefab | 3d9e6b3219444f94e85ebcb948ade18a | Occupancy trial zones |

---

## Complete Validation Checklist

When verifying configuration files:

### Schema Compliance
1. **Cue uniqueness**: All cue codes are unique (0-255)
2. **Cue name uniqueness**: All cue names are unique
3. **Cue lengths**: All cue lengths are positive
4. **Segment cue references**: All segment cue_sequence entries reference defined cues
5. **Transition probabilities**: If provided, sum to 1.0 (±0.001)
6. **Trial segment references**: All trial segment_names reference defined segments
7. **Zone position ordering**: end ≥ start for all trials
8. **Zone bounds**: All zone positions within trial length
9. **Stimulus after zone**: stimulus_location_cm ≥ stimulus_trigger_zone_start_cm

### Prefab Compliance
10. **Segment exists**: Prefab file exists at `Prefabs/{segment_name}.prefab`
11. **Zone present**: Segment contains appropriate zone prefab instance
12. **Position valid**: Zone position is within segment length bounds
13. **Range matches**: Calculated zone range matches config start/end values
14. **Stimulus location**: Boundary trigger aligns with `stimulus_location_cm`

### File Naming
15. **Scene name match**: `unity_scene_name` matches the YAML filename (without extension)

---

## Known Implementation Gap

**Important**: The YAML position fields (`stimulus_trigger_zone_start_cm`, etc.) are:
- Defined in YAML configuration files
- Validated by sl-shared-assets during loading
- **NOT** programmatically applied by Unity at runtime

Zone positions are determined entirely by prefab transforms. The YAML values serve as documentation/specification
and are validated by sl-experiment during runtime. Always verify prefab positions match intended config values
manually.

---

## Configuration File Header

Each configuration file must include the standard header (per style guide):

```yaml
# Project: [Full project name]
# Purpose: [Single sentence describing the task structure]
# Layout:  [Segment names with cue letters and zone placements]
# Related: [Related config file (parenthetical explanation of relationship)]
```

---

## Example Complete Configuration

```yaml
# Project: StateSpaceOdyssey
# Purpose: Defines a two-segment corridor with reward delivery in segment EFGH.
# Layout:  Segment ABCD with no zones. Segment EFGH with reward trigger zone in cue H.
# Related: SSO_Connection (extends this task with additional segments)

cues:
  - name: "Gray"
    code: 0
    length_cm: 25.0
  - name: "A"
    code: 1
    length_cm: 50.0
  - name: "B"
    code: 2
    length_cm: 50.0
  # ... additional cues

segments:
  - name: "Segment_ABCD"
    cue_sequence: ["A", "B", "C", "D"]
    transition_probabilities: [0.5, 0.5]
  - name: "Segment_EFGH"
    cue_sequence: ["E", "F", "G", "H"]
    transition_probabilities: [0.5, 0.5]

trial_structures:
  EFGH_reward:
    segment_name: "Segment_EFGH"
    stimulus_trigger_zone_start_cm: 150.0
    stimulus_trigger_zone_end_cm: 175.0
    stimulus_location_cm: 190.0
    show_stimulus_collision_boundary: false
    reward_size_ul: 5.0
    reward_tone_duration_ms: 300

experiment_states:
  habituation:
    experiment_state_code: 1
    system_state_code: 1
    state_duration_s: 300.0
    supports_trials: false
  training:
    experiment_state_code: 2
    system_state_code: 2
    state_duration_s: 1800.0
    supports_trials: true

vr_environment:
  corridor_spacing_cm: 20.0
  segments_per_corridor: 3
  padding_prefab_name: "Padding"
  cm_per_unity_unit: 10.0

unity_scene_name: "SSO_Shared_Base"
cue_offset_cm: 0.0
```
