# Claude Code Instructions

## Session Start Behavior

At the beginning of each coding session, before making any code changes, you should build a comprehensive
understanding of the codebase by invoking the `/explore-codebase` skill.

This ensures you:
- Understand the Unity project architecture before modifying code
- Follow existing patterns and conventions
- Don't introduce inconsistencies or break MQTT integrations

## Style Guide Compliance

Before writing, modifying, or reviewing any code or documentation, you MUST invoke the `/sun-lab-style` skill to load
the Sun Lab conventions. This applies to ALL file types including:
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

Sun Lab projects often depend on other `ataraxis-*` or `sl-*` libraries. These libraries may be stored
locally in the same parent directory as this project (`/home/cyberaxolotl/Desktop/GitHubRepos/`).

**Before writing code that interacts with a cross-referenced library, you MUST:**

1. **Check for local version**: Look for the library in the parent directory (e.g.,
   `../sl-shared-assets/`, `../gimbl/`).

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

- `/explore-codebase` - Perform in-depth codebase exploration for Unity/C# projects
- `/sun-lab-style` - Apply Sun Lab C# and Unity coding conventions (REQUIRED for all code and documentation changes)
- `/configuration-verification` - Verify prefab positions match YAML configuration constants (REQUIRED when working with configuration files)

## Related Libraries

This project integrates with other Sun Lab systems:

| Library              | Relationship          | Integration Points                                    |
|----------------------|-----------------------|-------------------------------------------------------|
| **sl-experiment**    | Data acquisition      | MQTT communication, cue sequence exchange, scene info |
| **sl-shared-assets** | Configuration schemas | Experiment configuration data classes                 |
| **gimbl**            | VR framework          | ActorObject, MQTTChannel, Display system              |

**When working on MQTT integration**, ensure topic names and message formats match sl-experiment expectations:
- `CueSequence/` - Sends pre-generated cue byte array
- `SceneName/` - Sends active Unity scene name
- `RequireLick/True/`, `RequireLick/False/` - Lick guidance mode control
- `RequireWait/True/`, `RequireWait/False/` - Occupancy guidance mode control
- `Gimbl/Stimulus/` - Stimulus trigger signal
- `LickPort/` - Lick detection from hardware

## Project Context

This is **sl-unity-tasks**, a Unity 6 project that provides VR behavioral experiment tasks for the Sun Lab's
mesoscope experiments. It creates infinite corridor environments where animals navigate through visual cue
sequences while receiving stimuli based on behavior.

### Key Areas

| Directory                                     | Purpose                                     |
|-----------------------------------------------|---------------------------------------------|
| `Assets/InfiniteCorridorTask/Scripts/`        | Core C# scripts for task logic              |
| `Assets/InfiniteCorridorTask/Configurations/` | YAML experiment configuration files         |
| `Assets/InfiniteCorridorTask/Prefabs/`        | Segment and zone prefabs                    |
| `Assets/InfiniteCorridorTask/Tasks/`          | Generated task prefabs                      |
| `Assets/UI-lick-reward/`                      | UI feedback system for lick/stimulus events |
| `Packages/gimbl/`                             | Custom VR framework package                 |

### Architecture

- **Task System**: MonoBehaviour-based controller managing corridor generation and animal tracking
- **Zone System**: Hierarchical zone components (StimulusTriggerZone, GuidanceZone, OccupancyZone)
- **MQTT Integration**: Type-safe channels for communication with sl-experiment
- **Configuration**: YAML-based experiment definitions loaded at runtime
- **Prefab Generation**: Editor tool creates task prefabs from configuration files

### Key Patterns

- **MQTT Event System**: MQTTChannel and MQTTChannel<T> for type-safe messaging
- **Corridor Teleportation**: Animals teleport between corridor combinations as they progress
- **Zone Hierarchy**: Parent-child zone relationships determine stimulus behavior modes
- **Configuration-Driven Tasks**: YAML files define all experiment parameters; no hardcoded values

### Code Standards

- CSharpier formatter with 120 character line limit
- EditorConfig enforcing Allman brace style and naming conventions
- XML documentation for all public and private members
- See `/sun-lab-style` for complete conventions

## Formatting

Run CSharpier before committing changes:

```bash
# Format all files
csharpier .

# Check without modifying (CI mode)
csharpier --check .
```

## Configuration File Workflow

Configuration files follow:
- **Naming convention**: `ProjectAbbreviation_TaskDescription.yaml` (e.g., `SSO_Merging.yaml`)
- **Header format**: Each file must include Project, Purpose, Layout, and Related fields as YAML comments

See the style guide for complete naming and header conventions.

When creating or modifying YAML configuration files, you MUST invoke the `/configuration-verification` skill to verify that:
- Referenced segment prefabs exist
- Zone positions in prefabs match configuration constants
- Zone ranges are within segment length bounds

This ensures experiment configurations are valid before task generation.

## Creating New Tasks

1. Create or modify a YAML configuration file in `Assets/InfiniteCorridorTask/Configurations/`
2. Run `/configuration-verification` to validate prefab/config alignment
3. Use the CreateTask editor tool: `CreateTask > New Task` menu
4. Select the YAML file and save the generated prefab
5. Create a new scene from ExperimentTemplate and add the task prefab

## Testing Workflow

1. Open a scene containing the task prefab
2. Assign an Actor in the Task Inspector
3. Configure displays via Window > GIMBL > Displays
4. Use SimulatedLinearTreadmill for manual testing without hardware
5. Monitor MQTT topics with an external client for debugging
