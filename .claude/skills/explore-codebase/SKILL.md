---
name: exploring-codebase
description: >-
  Performs in-depth codebase exploration at the start of a coding session. Builds comprehensive
  understanding of project structure, architecture, key components, and patterns. Use at session
  start or when the user asks to understand the codebase.
---

# Codebase Exploration

Performs thorough codebase exploration to build deep understanding before coding work begins.

---

## Exploration Approach

Use the Task tool with `subagent_type: Explore` to investigate the codebase. Focus on understanding:

1. **Project purpose and structure** - README, documentation, directory layout
2. **Architecture** - Main components, how they interact, communication patterns
3. **Core code** - Key classes, data models, utilities
4. **Configuration** - How the project is configured and customized
5. **Dependencies** - External libraries and integrations
6. **Patterns and conventions** - Coding style, naming conventions, design patterns

Adapt exploration depth based on project size and complexity. For small projects, a quick overview
suffices. For large projects, explore systematically.

---

## Guiding Questions

Answer these questions during exploration:

### Architecture
- What is the main entry point or controller?
- How do components communicate (events, callbacks, messaging)?
- What external systems does this integrate with?

### Patterns
- What naming conventions are used?
- What design patterns appear (singleton, observer, factory)?
- How is configuration managed?

### Structure
- Where is the core business logic?
- Where are tests located?
- What build/tooling configuration exists?

---

## Output Format

Provide a structured summary including:

- Project purpose (1-2 sentences)
- Key components table
- Important files list with paths
- Notable patterns or conventions
- Any areas of complexity or concern

### Example Output

```
## Project Purpose

Provides VR behavioral experiment tasks for mesoscope experiments. Animals navigate infinite
corridors while receiving stimuli based on behavior.

## Key Components

| Component       | Location                           | Purpose                              |
|-----------------|------------------------------------|--------------------------------------|
| Task Controller | Assets/.../Scripts/Task.cs         | Manages corridor generation and MQTT |
| Zone System     | Assets/.../Scripts/Zones/          | Handles trigger and occupancy zones  |
| Config Loader   | Assets/.../Scripts/ConfigLoader.cs | Loads YAML task templates            |

## Important Files

- `Assets/InfiniteCorridorTask/Scripts/Task.cs` - Main task controller (450 lines)
- `Assets/InfiniteCorridorTask/Configurations/*.yaml` - Task templates
- `Packages/gimbl/` - VR framework package

## Notable Patterns

- MQTT event system using MQTTChannel<T> for type-safe messaging
- Corridor teleportation for infinite hallway illusion
- YAML-driven task configuration

## Areas of Concern

- Zone position calculations require precise cm/unit conversion
- Prefab GUIDs must match expected values for verification
```

---

## Usage

Invoke at session start to ensure full context before making changes. Prevents blind modifications
and ensures understanding of existing patterns.
