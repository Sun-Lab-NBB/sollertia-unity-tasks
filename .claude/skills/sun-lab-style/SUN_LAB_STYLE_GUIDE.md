# Sun Lab C# Style Guide

This guide defines the documentation and coding conventions used across Sun Lab C# and Unity projects.
Reference this during development to maintain consistency across all codebases.

---

## XML Documentation

Use **XML documentation comments** with standard tags for all public and private members:

```csharp
/// <summary>Processes the input data and returns the transformed result.</summary>
/// <param name="inputData">The raw data to process.</param>
/// <param name="threshold">The minimum threshold for filtering values.</param>
/// <returns>The processed data array.</returns>
private float[] ProcessData(float[] inputData, float threshold)
{
    ...
}
```

### General Rules

**Punctuation**: Always use proper punctuation in all documentation: XML comments, inline comments,
and parameter descriptions.

### Summary Guidelines

**Summary line**: Use imperative mood for ALL documentation (methods, classes, properties, fields).
Use verbs like "Computes...", "Initializes...", "Handles...", "Manages..." rather than noun
phrases like "A class that..." or "Configuration for...".

```csharp
// Good
/// <summary>Initializes the MQTT channel and registers event handlers.</summary>

// Avoid
/// <summary>This method initializes the MQTT channel.</summary>
/// <summary>A method for initializing the MQTT channel.</summary>
```

### File-Level Documentation

Place file-level documentation at the top of the file, before using statements:

```csharp
/// <summary>
/// Provides the ConfigLoader class for loading and validating experiment configurations from YAML files.
/// </summary>
using System.Collections.Generic;
using System.IO;
```

### Class Documentation

Document classes with summary and optional remarks for terminology or context:

```csharp
/// <summary>
/// Controls the infinite corridor VR task, managing segment generation, animal positioning, and MQTT communication.
/// </summary>
/// <remarks>
/// Terminology:
/// - Cue: A visual pattern displayed on the corridor walls.
/// - Segment: A portion of the maze composed of a sequence of cues.
/// - Corridor: A grouping of adjacent segments forming a visual unit.
/// </remarks>
public class Task : MonoBehaviour
```

### Parameter and Return Documentation

**Parameters**: Start descriptions with uppercase. Don't repeat type info (types are in signature).

**Returns**: Describe what is returned, not the type.

```csharp
/// <summary>Samples an index from a probability distribution.</summary>
/// <param name="probabilities">The array of probabilities that must sum to 1.0.</param>
/// <param name="random">The random number generator instance.</param>
/// <returns>The sampled index.</returns>
private int SampleFromDistribution(float[] probabilities, System.Random random)
```

### Boolean Parameters

Use "Determines whether..." for boolean parameter descriptions:

```csharp
/// <summary>Determines whether the animal must lick to receive a reward.</summary>
public bool requireLick = false;
```

### Field Documentation

Document all fields with single-line summaries:

```csharp
/// <summary>The MQTT channel for sending stimulus trigger messages.</summary>
private MQTTChannel _stimulusTrigger;

/// <summary>The mapping of cue names to their byte codes.</summary>
private Dictionary<string, byte> _cueIds;
```

### Property Documentation

Use imperative mood for properties:

```csharp
/// <summary>Returns the current corridor segment combination as a string key.</summary>
private string CorridorKey => string.Join("-", _curSegment);
```

---

## Naming Conventions

### Variables

Use **full words**, not abbreviations:

| Avoid           | Prefer                                    |
|-----------------|-------------------------------------------|
| `pos`, `idx`    | `position`, `index`                       |
| `msg`, `val`    | `message`, `value`                        |
| `cfg`, `config` | `configuration` (or `config` if standard) |
| `num`, `cnt`    | `number`, `count`                         |
| `cb`, `fn`      | `callback`, `function`                    |

**Exception**: Well-established abbreviations like `MQTT`, `VR`, `UI`, `ID` are acceptable.

### Private Fields

Use underscore prefix with camelCase:

```csharp
private int _currentSegmentIndex;
private MQTTChannel _cueSequenceTrigger;
private Dictionary<string, byte> _cueIds;
```

### Public Members

Use PascalCase for all public members:

```csharp
public ActorObject actor = null;
public bool requireLick = false;
public float trackLength = 15000;
```

### Methods

Use PascalCase with descriptive verb phrases:

```csharp
// Good
private void UpdateLickMode()
private void TriggerStimulus()
public void ResetState()

// Avoid
private void DoUpdate()
private void HandleMode()
```

### Constants

Use PascalCase (C# convention, differs from C++ kPrefix):

```csharp
public const int MaxRetryAttempts = 3;
private const float DefaultThreshold = 0.5f;
```

### Unity Lifecycle Methods

Standard Unity methods use PascalCase without documentation (well-known pattern):

```csharp
void Start()
void Update()
void OnValidate()
void OnTriggerEnter(Collider collider)
void OnTriggerExit(Collider collider)
```

---

## Code Organization

### Using Statements

Organize using statements in this order:

1. System namespaces
2. Unity namespaces
3. Third-party namespaces
4. Local/project namespaces

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gimbl;
using SL.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
```

### Namespace Declarations

Use file-scoped namespaces when appropriate:

```csharp
namespace SL.Config;

public class ConfigLoader
{
    ...
}
```

Or block-scoped for files with multiple types:

```csharp
namespace SL.Config
{
    public class Cue { ... }
    public class Segment { ... }
}
```

### Class Organization

Organize class members in this order:

1. Public fields (Inspector-visible in Unity)
2. Serialized private fields
3. Private fields
4. Nested types (classes, structs)
5. Properties
6. Unity lifecycle methods (Awake, OnValidate, Start, Update, etc.)
7. Public methods
8. Private methods

```csharp
public class Task : MonoBehaviour
{
    // Public fields (Inspector)
    public ActorObject actor = null;
    public bool requireLick = false;

    // Private fields
    private int _currentSegmentIndex;
    private MQTTChannel _stimulusTrigger;

    // Nested types
    public class SequenceMsg
    {
        public byte[] cue_sequence;
    }

    // Unity lifecycle
    void OnValidate() { ... }
    void Start() { ... }
    void Update() { ... }

    // Private methods
    private void TriggerStimulus() { ... }
}
```

---

## Brace Style

Use **Allman style** (braces on new lines) for all blocks:

```csharp
if (condition)
{
    DoSomething();
}
else
{
    DoSomethingElse();
}

public void ProcessData()
{
    for (int i = 0; i < count; i++)
    {
        items[i].Process();
    }
}
```

Single-line statements without braces are preferred for clarity when they fit within the line limit:

```csharp
// Good - concise and clear
if (condition) return;
if (actor == null) return;
if (!isActive) return;

// Also acceptable for multi-line blocks
if (condition)
{
    DoSomething();
    DoSomethingElse();
}
```

---

## Line Length and Formatting

- Maximum line length: 120 characters
- Use CSharpier for automatic formatting
- Break long method calls across multiple lines:

```csharp
Debug.LogWarning(
    $"Task is positioned at {transform.position}. Automatically Setting Task position to "
        + "(0,0,0) for this runtime but it is recommended to permanently set the task position to "
        + "(0,0,0) in Editor Mode."
);
```

- Break long LINQ chains across lines:

```csharp
var result = items
    .Where(item => item.IsValid)
    .Select(item => item.Value)
    .OrderBy(value => value)
    .ToList();
```

---

## var Usage

- Use `var` when the type is obvious from the right side
- Use explicit types when the type is not immediately clear

```csharp
// Good - type is obvious
var deserializer = new DeserializerBuilder().Build();
var corridorData = _corridorMap[corridorKey];

// Good - explicit type for clarity
MesoscopeExperimentConfiguration config = deserializer.Deserialize<MesoscopeExperimentConfiguration>(yaml);
HashSet<int> seenCodes = new HashSet<int>();
```

---

## MQTT Patterns

### Channel Setup

Use type-safe MQTT channels with proper naming:

```csharp
// Trigger-only channel (no payload)
private MQTTChannel _stimulusTrigger;

// Typed channel with payload
private MQTTChannel<SequenceMsg> _cueSequenceChannel;

// Initialize in Start()
void Start()
{
    _stimulusTrigger = new MQTTChannel("Gimbl/Stimulus/");
    _cueSequenceChannel = new MQTTChannel<SequenceMsg>("CueSequence/", false);

    // Subscribe to incoming messages
    _lickTrigger = new MQTTChannel("LickPort/", true);
    _lickTrigger.Event.AddListener(OnLickDetected);
}
```

### Message Classes

Define simple message classes for typed channels:

```csharp
public class SequenceMsg
{
    public byte[] cue_sequence;
}

public class SceneNameMsg
{
    public string name;
}
```

### Topic Naming

Use hierarchical topic names with trailing slashes:

- `CueSequence/`
- `Gimbl/Stimulus/`
- `LickPort/`
- `RequireLick/True/`
- `RequireLick/False/`

---

## Error Handling

Use Unity's Debug logging for errors and warnings:

```csharp
if (!File.Exists(filePath))
{
    Debug.LogError($"Configuration file not found: {filePath}");
    return null;
}

if (transform.position != Vector3.zero)
{
    Debug.LogWarning(
        $"Task is positioned at {transform.position}. It is recommended to set position to (0,0,0)."
    );
}
```

### Error Message Format

- Start with context: "ComponentName: Description of problem"
- Include relevant values for debugging
- Be specific about what went wrong

```csharp
Debug.LogError($"Cue '{cue.name}' has invalid code {cue.code}. Must be 0-255.");
Debug.LogError($"Segment '{segment.name}' transition probabilities sum to {sum}, must be 1.0.");
Debug.LogError($"Task: Corridor key '{corridorKey}' not found in corridor map");
```

---

## Comments

### Inline Comments

- Use third person imperative ("Configures..." not "This section configures...")
- Place above the code, not at end of line
- Explain non-obvious logic or provide context

```csharp
// Loads and validates configuration
_config = ConfigLoader.Load(globalConfigPath);

// Generates segment combination for current corridor index
for (int j = 0; j < _depth; j++)
{
    corridorSegments[j] = i / (int)Mathf.Pow(_nSegments, _depth - j - 1) % _nSegments;
}

// Uses transition probabilities if defined, otherwise uniform random
if (segment.HasTransitionProbabilities)
{
    choice = SampleFromDistribution(segment.transition_probabilities.ToArray(), random);
}
```

### What to Avoid

- Don't reiterate the obvious (e.g., `// Set x to 5` before `x = 5`)
- Don't add documentation to code you didn't write or modify
- Don't add excessive comments to self-explanatory code

---

## Unity-Specific Patterns

### Component References

Use `FindObjectsByType` or `FindAnyObjectByType` (Unity 2022+):

```csharp
void OnValidate()
{
    if (actor == null)
    {
        ActorObject[] allActors = FindObjectsByType<ActorObject>(FindObjectsSortMode.None);
        if (allActors.Length > 0)
        {
            actor = allActors[0];
        }
    }
}
```

### TryGetComponent Pattern

Use TryGetComponent for optional components:

```csharp
if (TryGetComponent<MeshRenderer>(out var meshRenderer))
{
    meshRenderer.enabled = false;
}
```

### Collider Callbacks

Use standard Unity collider callback patterns:

```csharp
void OnTriggerEnter(Collider collider)
{
    _inZone = true;
}

void OnTriggerExit(Collider collider)
{
    _inZone = false;
}
```

---

## Configuration Classes

### Data Classes for YAML

Use simple classes with public fields for YAML deserialization:

```csharp
public class Cue
{
    public string name;
    public int code;
    public float length_cm;
}

public class Segment
{
    public string name;
    public List<string> cue_sequence;
    public List<float> transition_probabilities;

    public bool HasTransitionProbabilities =>
        transition_probabilities != null && transition_probabilities.Count > 0;
}
```

### Computed Properties

Add helper methods for derived values:

```csharp
public class MesoscopeExperimentConfiguration
{
    public Dictionary<string, byte> GetCueNameToCode()
    {
        return cues.ToDictionary(c => c.name, c => (byte)c.code);
    }

    public float[] GetSegmentLengthsUnity()
    {
        return segments.Select(s => ComputeSegmentLength(s)).ToArray();
    }
}
```

---

## Formatting Commands

Run CSharpier before committing:

```bash
# Format all files
csharpier .

# Check without modifying (CI mode)
csharpier --check .

# Format specific directory
csharpier Assets/InfiniteCorridorTask/Scripts/
```

---

## README Files

README files follow a standardized structure:

### Structure

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

### Writing Style

**Voice**: Use third person throughout. Refer to the project as "this project," "the library,"
or by name. Avoid first person ("I," "we") and second person ("you") where possible.

**Tense**: Use present tense as the default.

---

## Skill and Asset Files

Claude Code skill files (`.md` files in `.claude/skills/`) and related documentation assets follow specific
formatting conventions to ensure readability and consistency.

### Line Length

All skill and asset markdown files must adhere to the **120 character line limit**. This matches the C# code
formatting standard and ensures consistent readability across all project files.

- Wrap prose text at 120 characters
- Break long sentences at natural boundaries (after punctuation, between clauses)
- Code blocks may exceed 120 characters only when necessary for readability

### Table Formatting

Use **pretty table formatting** with proper column alignment and consistent column widths:

**Good - Properly formatted table:**

```markdown
| Field                  | Type        | Required | Description                              |
|------------------------|-------------|----------|------------------------------------------|
| `name`                 | str         | Yes      | Visual identifier (e.g., 'A', 'Gray')    |
| `code`                 | int         | Yes      | Unique uint8 code for MQTT communication |
| `length_cm`            | float       | Yes      | Length of the cue in centimeters         |
| `transition_probs`     | list[float] | No       | Probabilities to other segments          |
```

**Avoid - Inconsistent column widths:**

```markdown
| Field | Type | Required | Description |
|---|---|---|---|
| `name` | str | Yes | Visual identifier |
| `code` | int | Yes | Unique uint8 code |
```

### Table Formatting Rules

1. **Column separators**: Align all `|` characters vertically
2. **Header separator**: Use dashes (`-`) that span the full column width
3. **Cell padding**: Add spaces to pad cells to consistent widths within each column
4. **Minimum width**: Each column should be at least as wide as its longest cell content
5. **Code formatting**: Use backticks for field names, types, and values in tables

### Section Organization

Organize skill files with clear hierarchical sections:

```markdown
# Skill Name

Brief description of the skill's purpose.

---

## When to Use

- Bullet points describing use cases

---

## Main Content Section

### Subsection

Content with tables, code blocks, and explanations.

---

## Additional Sections

Continue with logical organization.
```

### Code Blocks in Skills

Use fenced code blocks with language identifiers:

````markdown
```yaml
cues:
  - name: "A"
    code: 1
```

```bash
grep -n "pattern" file.txt
```

```csharp
private void ProcessData() { }
```
````

### Skill File Checklist

When creating or modifying skill files:

1. **Line length**: All lines ≤ 120 characters
2. **Tables**: Use pretty formatting with aligned columns
3. **Sections**: Separate major sections with horizontal rules (`---`)
4. **Code blocks**: Include language identifiers
5. **Voice**: Use third person imperative (same as code documentation)
6. **Headers**: Use sentence case for section headers

---

## Configuration File Naming

YAML experiment configuration files follow the naming convention:

```
ProjectAbbreviation_TaskDescription.yaml
```

### Components

**ProjectAbbreviation**: A short identifier for the project or experiment cohort.

| Abbreviation | Project Name      |
|--------------|-------------------|
| MF           | MaalstroomicFlow  |
| SSO          | StateSpaceOdyssey |

**TaskDescription**: One or more words separated by underscores describing the task variant or trial structure.

### Examples

```
MF_Reward.yaml                    # MaalstroomicFlow reward-only task
MF_Aversion_Reward.yaml           # MaalstroomicFlow aversion + reward task
SSO_Shared_Base.yaml              # StateSpaceOdyssey shared base training task
SSO_Connection.yaml               # StateSpaceOdyssey connection trials
SSO_Connection_Base.yaml          # StateSpaceOdyssey connection base training
SSO_Extension_Shortcut.yaml       # StateSpaceOdyssey extension/shortcut trials
SSO_Merging.yaml                  # StateSpaceOdyssey merging trials
```

### Conventions

- Use `_Base` suffix for single-segment training configurations that precede multi-segment tasks
- Use descriptive task names that reflect the trial structure or behavioral paradigm
- Capitalize each word in the task description
- Separate multiple words with underscores

### Unity Scene Name

The `unity_scene_name` field must match the configuration file name (without the `.yaml` extension):

```yaml
# File: SSO_Shared_Base.yaml
unity_scene_name: "SSO_Shared_Base"

# File: MF_Aversion_Reward.yaml
unity_scene_name: "MF_Aversion_Reward"
```

This ensures consistency between configuration files and their corresponding Unity scenes.

### Configuration Header

Each configuration file must begin with a YAML comment header containing the following information:

```yaml
# Project: [Full project name]
# Purpose: [Single sentence describing the task structure]
# Layout:  [Segment names with cue letters and zone placements]
# Related: [Related config file (parenthetical explanation of relationship)]
```

**Multi-line Wrapping**: When content exceeds the line length, wrap to the next line and align continuation
text with the first character after the field name:

```yaml
# Project: MaalstroomicFlow
# Purpose: Extends the base MF_Reward trial structure to also include an aversive stimulus in the ABCD segment.
# Layout:  Segment ABCD with occupancy zone at cue C and the aversive stimulus (air puff) trigger zone in cue D.
#          Segment EFGH with the rewarding stimulus (water) trigger zone in cue H.
# Related: MF_Reward (the base version of this task that only includes the reward zone)
```

**Single-Segment Example:**

```yaml
# Project: MaalstroomicFlow
# Purpose: Defines a cyclic 8-cue corridor with a rewarding stimulus trigger zone at the end of the corridor.
# Layout:  Segment ABCDEFGH with the rewarding stimulus (water) trigger zone in cue H.
# Related: MF_Aversion_Reward (extends the task to allow studying both reward and aversion coding).
```

**Guidelines:**

- **Project**: Use the full project name, not the abbreviation
- **Purpose**: Single sentence describing the task structure; use verbs like "Defines", "Extends", "Teaches"
- **Layout**: Use two spaces after `#` for alignment; include segment names with cue letters (e.g., "Segment ABCD"),
  zone types (occupancy zone, trigger zone), and parenthetical stimulus clarifications (e.g., "(water)", "(air puff)")
- **Related**: Explain the relationship in parentheses (e.g., "extends the task to...", "the base version of...")

### Inline YAML Comments

Add inline comments to clarify non-obvious YAML values:

```yaml
cues:
  - name: "Gray" # This is a placeholder, the task does not use Gray cues.
    code: 0
    length_cm: 1.0
```

Use inline comments when:
- A value exists for technical reasons but isn't actively used
- A field name is ambiguous in context
- Special values need explanation

---

## Commit Messages

### Format

**Header line limit**: The first line (header) must be no longer than 72 characters. This ensures proper display in
Git logs, GitHub, and other tools.

**Single-line commits**: Use for focused, single-purpose changes.

```
Added random seed support to maze generation.
Fixed corridor transition logic that caused teleportation errors.
Updated MQTT topic names to match sl-experiment conventions.
```

**Multi-line commits**: Use for changes that bundle related modifications.

```
Added occupancy-based stimulus trigger mode.

-- Added OccupancyZone component for duration-based triggers.
-- Added OccupancyGuidanceZone for brake activation in guidance mode.
-- Updated StimulusTriggerZone to support both lick and occupancy modes.
```

### Writing Style

**Verb tense**: Start with a past tense verb:

| Verb       | Use Case                                    |
|------------|---------------------------------------------|
| Added      | New features, files, or functionality       |
| Fixed      | Bug fixes and error corrections             |
| Updated    | Modifications to existing functionality     |
| Refactored | Code restructuring without behavior changes |
| Optimized  | Performance improvements                    |
| Improved   | Enhancements to existing features           |
| Removed    | Deletions of code, files, or features       |

**Punctuation**: Always end commit messages with a period.

**Content**: Focus on *what* was changed and *why*, not *how*.
