---
name: explore-codebase
description: >-
  Perform in-depth codebase exploration at the start of a coding session. Builds comprehensive
  understanding of Unity project structure, architecture, key components, and patterns.
---

# Codebase Exploration

When this skill is invoked, perform a thorough exploration of the codebase to build deep understanding before any
coding work begins.

---

## Exploration Requirements

You MUST use the Task tool with `subagent_type: Explore` to investigate the following areas:

### 1. Project Overview
- Read README.md, package.json files, and documentation
- Understand the project's purpose, goals, and primary use cases
- Identify the target users/audience (neuroscience researchers, VR experiments)

### 2. Unity Project Structure
- Map the complete directory structure under Assets/
- Identify scene files, prefabs, scripts, materials, and configurations
- Understand the Packages/ directory and custom packages (gimbl)
- Review ProjectSettings/ for Unity configuration

### 3. Architecture
- Understand the overall system architecture
- Identify main components and how they interact
- Document communication patterns (MQTT topics, Unity events, collider callbacks)
- Note external system integrations (MQTT broker, sl-experiment)

### 4. Core Scripts
- **MonoBehaviour classes**: Task, Zone components, UI elements
- **Data models**: Configuration classes, message types
- **Utilities**: ConfigLoader, helper functions
- **Editor tools**: CreateTask menu items, custom inspectors

### 5. Configuration System
- Review YAML configuration files in Configurations/
- Understand cue, segment, and trial structure definitions
- Note VR environment parameters and their effects
- Document how configurations drive prefab generation

### 6. MQTT Integration
- List all MQTT topics and their purposes
- Understand message flow between Unity and sl-experiment
- Note channel patterns (trigger-only vs typed payload)
- Document subscription and publication patterns

### 7. Prefab System
- Understand segment prefab structure
- Review zone prefab hierarchy (StimulusTriggerZone, GuidanceZone, OccupancyZone)
- Note how prefabs are combined in CreateTask workflow
- Document padding and corridor spacing

### 8. Dependencies
- Review gimbl package for display, controller, and MQTT functionality
- Understand third-party libraries (YamlDotNet, M2Mqtt)
- Note Unity package dependencies from manifest.json

### 9. Design Patterns & Conventions
- Document coding patterns used (MQTT event system, zone hierarchy, teleportation)
- Note naming conventions from .editorconfig
- Identify formatting standards from .csharpierrc.yaml

### 10. Key Files
- List the most important files with brief descriptions
- Include file paths and line counts where relevant

---

## Output Format

After exploration, provide a structured summary with:
- Project purpose (1-2 sentences)
- Architecture diagram (ASCII if helpful)
- Key components table
- Important files list with paths
- MQTT topic reference
- Notable patterns or conventions
- Any areas of complexity or concern

---

## Usage

This skill should be invoked at the start of coding sessions to ensure full context before making
changes. It prevents blind modifications and ensures understanding of existing patterns.
