# README Style Guide

Conventions for README files in Sun Lab projects.

---

## Structure

README files follow a standardized structure:

1. **Title**: Project name as H1 heading
2. **One-line description**: Brief summary
3. **Badges**: License, status indicators
4. **Horizontal rule**: `___`
5. **Detailed Description**: Expanded explanation
6. **Table of Contents**: Links to sections
7. **Dependencies**: External requirements
8. **Installation**: Setup instructions
9. **Usage**: How to use the project
10. **API Documentation**: Link to docs (if applicable)
11. **Versioning**: Semantic versioning statement
12. **Authors**: List of contributors
13. **License**: License type with link
14. **Acknowledgments**: Credits

---

## Writing Style

**Voice**: Use third person throughout. Refer to the project as "this project," "the library," or by name. Avoid first
person ("I," "we") and second person ("you") where possible.

**Tense**: Use present tense as the default.

---

## Example

```markdown
# sl-unity-tasks

VR behavioral experiment tasks for the Sun Lab's mesoscope experiments.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

___

This project provides infinite corridor VR environments where animals navigate through visual cue sequences while
receiving stimuli based on behavior. The system integrates with sl-experiment for data acquisition via MQTT messaging.

## Table of Contents

- [Dependencies](#dependencies)
- [Installation](#installation)
- [Usage](#usage)
...
```

---

## Codebase Cross-Referencing

When writing or updating README content that describes how the library works, you MUST cross-reference against the
current state of the codebase to ensure accuracy.

**Sections requiring verification:**
- Architecture descriptions
- API usage examples
- Configuration options
- File paths and directory structures
- Class names, method signatures, and parameters
- MQTT topics and message formats
- Workflow descriptions

**Verification process:**
1. Identify all technical claims in the README section
2. Use `/exploring-codebase` skill if unfamiliar with the relevant code
3. Read source files to verify each claim
4. Update README content to match actual implementation
5. Remove references to deprecated or non-existent features

**Example verification:**

```
README claims: "The Task class uses MQTTChannel to publish cue sequences."

Verification steps:
1. Read Assets/InfiniteCorridorTask/Scripts/Task.cs
2. Confirm MQTTChannel<SequenceMsg> exists and publishes to "CueSequence/"
3. Verify message format matches README description
```

---

## Verification Checklist

**You MUST verify your edits against this checklist before submitting any changes to README files.**

```
README Style Compliance:
- [ ] Title as H1 heading with project name
- [ ] One-line description immediately after title
- [ ] Badges for license and status indicators
- [ ] Horizontal rule (`___`) after badges
- [ ] Detailed description section
- [ ] Table of Contents with links to sections
- [ ] Third person voice throughout (no "I", "we", "you")
- [ ] Present tense as default
- [ ] All required sections included (Dependencies, Installation, Usage, etc.)
- [ ] Technical descriptions cross-referenced against codebase
- [ ] File paths and class names verified to exist
- [ ] API examples tested against actual implementation
```
