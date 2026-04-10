# Claude Code Instructions

## Session Start Behavior

At the beginning of each coding session, before making any code changes, you should build a comprehensive
understanding of the codebase by invoking the `/exploring-codebase` skill.

This ensures you:
- Understand the Unity project architecture before modifying code
- Follow existing patterns and conventions
- Don't introduce inconsistencies or break MQTT integrations

## Style Guide Compliance

Before writing, modifying, or reviewing any code or documentation, you MUST invoke the `/applying-sun-lab-style` skill
to load the Sun Lab conventions. This applies to ALL file types including:
- C# source files (`.cs`)
- Documentation files (`README.md`)
- YAML configuration files when adding comments or descriptions
- Git commit messages

All contributions must strictly follow these conventions and all reviews must check for compliance. Key conventions
include:
- XML documentation comments with `<summary>`, `<param>`, `<returns>` tags
- Private fields with `_camelCase`, public members with `PascalCase`
- Allman brace style (braces on new lines)
- Third person imperative mood for comments and documentation
- 120 character line limit with CSharpier formatting
- Commit messages use past tense verbs (Added, Fixed, Updated) and end with periods

## Cross-Referenced Library Verification

Sollertia platform projects often depend on other `ataraxis-*` or `sollertia-*` libraries. These libraries may be stored
locally in the same parent directory as this project (`/home/cyberaxolotl/Desktop/GitHubRepos/`).

**Before writing code that interacts with a cross-referenced library, you MUST:**

1. **Check for local version**: Look for the library in the parent directory (e.g.,
   `../sollertia-shared-assets/`, `../gimbl/`).

2. **Compare versions**: If a local copy exists, compare its version against the latest release or
   main branch on GitHub:
   - Read the local package.json or version file to get the current version
   - Use `gh api repos/Sun-Lab-NBB/{repo-name}/releases/latest` to check the latest release

3. **Handle version mismatches**: If the local version differs from the latest release, notify the
   user with the following options:
   - **Use online version**: Fetch documentation and API details from the GitHub repository
   - **Update local copy**: The user will pull the latest changes locally before proceeding

4. **Proceed with correct source**: Use whichever version the user selects as the authoritative
   reference for API usage, patterns, and documentation.

**Why this matters**: Skills and documentation may reference outdated APIs. Always verify against the
actual library state to prevent integration errors.

## Available Skills

- `/exploring-codebase` - Perform in-depth codebase exploration for Unity/C# projects
- `/applying-sun-lab-style` - Apply Sun Lab C# and Unity coding conventions (REQUIRED for all code and documentation changes)
- `/verifying-task-templates` - Verify prefab positions match YAML task template constants (REQUIRED when working with template files)

## Skill Workflow Guide

Use skills in combination for different tasks:

| Task Type                    | Skills to Invoke (in order)                                            |
|------------------------------|------------------------------------------------------------------------|
| Session start                | `/exploring-codebase`                                                  |
| Writing C# code              | `/exploring-codebase` (if new session), `/applying-sun-lab-style`      |
| Creating task templates      | `/applying-sun-lab-style`, `/verifying-task-templates`                 |
| Modifying task templates     | `/verifying-task-templates`                                            |
| Modifying segment prefabs    | `/verifying-task-templates` (after changes)                            |
| Writing/updating README      | `/exploring-codebase`, `/applying-sun-lab-style`                       |
| Writing commit messages      | `/applying-sun-lab-style`                                              |
| Code review                  | `/applying-sun-lab-style`                                              |
| Creating new skills          | `/applying-sun-lab-style` (see SKILL_STYLE.md)                         |

**Workflow examples:**

1. **New coding session**: Invoke `/exploring-codebase` first to understand the project, then `/applying-sun-lab-style`
   before writing any code.

2. **Adding a new task template**: Invoke `/applying-sun-lab-style` to review naming and header conventions, create the
   template, then invoke `/verifying-task-templates` to validate against prefabs.

3. **Fixing a bug in Task.cs**: If unfamiliar with the codebase, invoke `/exploring-codebase`. Then invoke
   `/applying-sun-lab-style` before making changes. After fixing, use the style guide's commit conventions.

4. **Updating README documentation**: Invoke `/exploring-codebase` to understand the current implementation. Then
   invoke `/applying-sun-lab-style` and cross-reference all technical claims against actual source files before
   writing. Verify file paths, class names, and API examples match the codebase.

## Related Libraries

This project integrates with other Sollertia platform libraries:

| Library              | Relationship          | Integration Points                                    |
|----------------------|-----------------------|-------------------------------------------------------|
| **sollertia-experiment**    | Data acquisition      | MQTT communication, cue sequence exchange, scene info |
| **sollertia-shared-assets** | Configuration schemas | Task template and experiment configuration classes    |
| **gimbl**            | VR framework          | ActorObject, MQTTChannel, Display system              |

**When working on MQTT integration**, ensure topic names and message formats match sollertia-experiment expectations:
- `CueSequence/` - Sends pre-generated cue byte array
- `SceneName/` - Sends active Unity scene name
- `RequireLick/True/`, `RequireLick/False/` - Lick guidance mode control
- `RequireWait/True/`, `RequireWait/False/` - Occupancy guidance mode control
- `Gimbl/Stimulus/` - Stimulus trigger signal
- `LickPort/` - Lick detection from hardware

## Project Context

This is **sollertia-unity-tasks**, a Unity 6 project that provides VR behavioral experiment tasks for the Sollertia
platform's mesoscope data acquisition systems. It creates infinite corridor environments where animals navigate through visual cue
sequences while receiving stimuli based on behavior.

### Key Areas

| Directory                                     | Purpose                                     |
|-----------------------------------------------|---------------------------------------------|
| `Assets/InfiniteCorridorTask/Scripts/`        | Core C# scripts for task logic              |
| `Assets/InfiniteCorridorTask/Configurations/` | YAML task template files                    |
| `Assets/InfiniteCorridorTask/Prefabs/`        | Segment and zone prefabs                    |
| `Assets/InfiniteCorridorTask/Tasks/`          | Generated task prefabs                      |
| `Assets/UI-lick-reward/`                      | UI feedback system for lick/stimulus events |
| `Packages/gimbl/`                             | Custom VR framework package                 |

### Architecture

- **Task System**: MonoBehaviour-based controller managing corridor generation and animal tracking
- **Zone System**: Hierarchical zone components (StimulusTriggerZone, GuidanceZone, OccupancyZone)
- **MQTT Integration**: Type-safe channels for communication with sollertia-experiment
- **Configuration**: YAML-based task templates loaded at runtime
- **Prefab Generation**: Editor tool creates task prefabs from template files

### Key Patterns

- **MQTT Event System**: MQTTChannel and MQTTChannel<T> for type-safe messaging
- **Corridor Teleportation**: Animals teleport between corridor combinations as they progress
- **Zone Hierarchy**: Parent-child zone relationships determine stimulus behavior modes
- **Template-Driven Tasks**: YAML files define all task parameters; no hardcoded values

### Code Standards

- CSharpier formatter with 120 character line limit
- EditorConfig enforcing Allman brace style and naming conventions
- XML documentation for all public and private members
- See `/applying-sun-lab-style` for complete conventions

## Formatting

Run CSharpier before committing changes:

```bash
# Format all files
csharpier .

# Check without modifying (CI mode)
csharpier --check .
```

## Task Template Workflow

Task template files follow:
- **Naming convention**: `ProjectAbbreviation_TaskDescription.yaml` (e.g., `SSO_Merging.yaml`)
- **Header format**: Each file must include Project, Purpose, Layout, and Related fields as YAML comments
- **Template name derivation**: The template name and Unity scene name are derived from the filename

See the style guide for complete naming and header conventions.

When creating or modifying YAML task template files, you MUST invoke the `/verifying-task-templates` skill to verify that:
- Referenced segment prefabs exist
- Zone positions in prefabs match template constants (using correct `cm_per_unity_unit` conversion)
- Zone ranges are within segment length bounds

This ensures task templates are valid before task generation.

### Maintaining Pre-Baked Verification Data

The `/verifying-task-templates` skill contains pre-baked expected values that enable efficient verification. This data
MUST be updated when:

1. **Template changes**: Adding, removing, or modifying any YAML file in `Configurations/`
2. **Prefab changes**: Adding, removing, or modifying segment prefabs (`Segment_*.prefab`) or zone prefabs
3. **Zone prefab GUID changes**: If zone prefabs are recreated (new GUIDs assigned)

**When any of these changes occur, update the `EXPECTED_VALUES.md` companion file alongside the `configuration-verification` skill in the central [sollertia](https://github.com/Sun-Lab-NBB/sollertia) marketplace plugin (`plugins/experiment/skills/configuration-verification/EXPECTED_VALUES.md`). The file no longer lives in this repository — the per-repo `.claude/skills/` copy was removed when the skill was lifted into the central marketplace.**

| Section                    | Update When                                    |
|----------------------------|------------------------------------------------|
| Templates                  | Any template added, removed, or modified       |
| Template Trial Structures  | Trial trigger_type values change               |
| Segment Prefabs            | Any segment prefab added, removed, or modified |
| Pre-Computed Zone Ranges   | Zone positions or sizes change                 |
| Zone Prefab GUIDs          | Zone prefabs recreated with new GUIDs          |

## Creating New Tasks

1. Create or modify a YAML task template file in `Assets/InfiniteCorridorTask/Configurations/`
2. Run `/verifying-task-templates` to validate prefab/template alignment
3. Use the CreateTask editor tool: `CreateTask > New Task` menu
4. Select the YAML file and save the generated prefab
5. Create a new scene from ExperimentTemplate and add the task prefab

## Testing Workflow

1. Open a scene containing the task prefab
2. Assign an Actor in the Task Inspector
3. Configure displays via Window > GIMBL > Displays
4. Use SimulatedLinearTreadmill for manual testing without hardware
5. Monitor MQTT topics with an external client for debugging
