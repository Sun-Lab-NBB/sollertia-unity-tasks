# C# Code Style Guide

Conventions for C# and Unity code in Sun Lab projects.

---

## Contents

- [XML Documentation](#xml-documentation)
- [Naming Conventions](#naming-conventions)
- [Code Organization](#code-organization)
- [Brace Style](#brace-style)
- [Line Length and Formatting](#line-length-and-formatting)
- [MQTT Patterns](#mqtt-patterns)
- [Error Handling](#error-handling)
- [Comments](#comments)
- [Unity-Specific Patterns](#unity-specific-patterns)
- [Configuration Classes](#configuration-classes)
- [Verification Checklist](#verification-checklist)

---

## XML Documentation

Use **XML documentation comments** for all public and private members:

```csharp
/// <summary>Processes the input data and returns the transformed result.</summary>
/// <param name="inputData">The raw data to process.</param>
/// <param name="threshold">The minimum threshold for filtering values.</param>
/// <returns>The processed data array.</returns>
private float[] ProcessData(float[] inputData, float threshold)
```

### Rules

- **Punctuation**: Always use proper punctuation in all documentation.
- **Imperative mood**: Use verbs like "Computes...", "Initializes...", "Handles..." for ALL members.
- **Boolean descriptions**: Use "Determines whether..." for boolean parameters.
- **Parameters**: Start descriptions with uppercase. Don't repeat type info.
- **Returns**: Describe what is returned, not the type.
- **Prose over lists**: Always use prose instead of bullet lists or dashes in XML documentation. Lists are permitted
  in README files and skill files, but not in API documentation or inline comments.

### File-Level Documentation

Place at the top of the file, before using statements:

```csharp
/// <summary>
/// Provides the ConfigLoader class for loading and validating task templates from YAML files.
/// </summary>
using System.Collections.Generic;
```

### Class Documentation

Document with summary and optional remarks for terminology:

```csharp
/// <summary>
/// Controls the infinite corridor VR task, managing segment generation and MQTT communication.
/// </summary>
/// <remarks>
/// A "cue" refers to a visual pattern displayed on the corridor walls. A "segment" is a portion of the
/// maze composed of a sequence of cues.
/// </remarks>
public class Task : MonoBehaviour
```

### Field and Property Documentation

```csharp
/// <summary>The MQTT channel for sending stimulus trigger messages.</summary>
private MQTTChannel _stimulusTrigger;

/// <summary>Returns the current corridor segment combination as a string key.</summary>
private string CorridorKey => string.Join("-", _curSegment);

/// <summary>Determines whether the animal must lick to trigger the stimulus.</summary>
public bool requireLick = false;
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

**Exception**: Well-established abbreviations like `MQTT`, `VR`, `UI`, `ID` are acceptable.

### Member Naming

| Member Type     | Convention                | Example                        |
|-----------------|---------------------------|--------------------------------|
| Private fields  | `_camelCase`              | `_currentSegmentIndex`         |
| Public fields   | `PascalCase`              | `RequireLick`                  |
| Methods         | `PascalCase` verb phrases | `UpdateLickMode()`             |
| Constants       | `PascalCase`              | `MaxRetryAttempts`             |
| Unity lifecycle | `PascalCase` (no docs)    | `Start()`, `Update()`          |

---

## Code Organization

### Using Statements Order

1. System namespaces
2. Unity namespaces
3. Third-party namespaces
4. Local/project namespaces

```csharp
using System;
using System.Collections.Generic;
using Gimbl;
using SL.Config;
using UnityEngine;
```

### Namespace Declarations

Use file-scoped namespaces when appropriate:

```csharp
namespace SL.Config;

public class ConfigLoader { ... }
```

### Class Member Order

1. Public fields (Inspector-visible in Unity)
2. Serialized private fields
3. Private fields
4. Nested types (classes, structs)
5. Properties
6. Unity lifecycle methods (Awake, OnValidate, Start, Update)
7. Public methods
8. Private methods

---

## Brace Style

Use **Allman style** (braces on new lines):

```csharp
if (condition)
{
    DoSomething();
}
else
{
    DoSomethingElse();
}
```

Single-line statements without braces are preferred when they fit within the line limit:

```csharp
if (condition) return;
if (actor == null) return;
```

---

## Line Length and Formatting

- Maximum line length: **120 characters**
- Use **CSharpier** for automatic formatting
- Break long method calls and LINQ chains across multiple lines:

```csharp
Debug.LogWarning(
    $"Task is positioned at {transform.position}. Automatically Setting Task position to "
        + "(0,0,0) for this runtime."
);

var result = items
    .Where(item => item.IsValid)
    .Select(item => item.Value)
    .ToList();
```

Run CSharpier before committing:

```bash
csharpier .                                    # Format all files
csharpier --check .                            # Check without modifying
```

### var Usage

- Use `var` when the type is obvious from the right side
- Use explicit types when the type is not immediately clear

```csharp
var deserializer = new DeserializerBuilder().Build();           // Type obvious
MesoscopeExperimentConfiguration config = deserializer.Deserialize<...>(yaml);  // Type explicit
```

---

## MQTT Patterns

### Channel Setup

```csharp
// Trigger-only channel (no payload)
private MQTTChannel _stimulusTrigger;

// Typed channel with payload
private MQTTChannel<SequenceMsg> _cueSequenceChannel;

void Start()
{
    _stimulusTrigger = new MQTTChannel("Gimbl/Stimulus/");
    _cueSequenceChannel = new MQTTChannel<SequenceMsg>("CueSequence/", false);

    // Subscribe to incoming messages
    _lickTrigger = new MQTTChannel("LickPort/", true);
    _lickTrigger.Event.AddListener(OnLickDetected);
}
```

### Message Classes and Topic Naming

```csharp
public class SequenceMsg { public byte[] cue_sequence; }
public class SceneNameMsg { public string name; }
```

Use hierarchical topic names with trailing slashes: `CueSequence/`, `Gimbl/Stimulus/`, `LickPort/`

---

## Error Handling

Use Unity's Debug logging with context and relevant values:

```csharp
if (!File.Exists(filePath))
{
    Debug.LogError($"Configuration file not found: {filePath}");
    return null;
}

Debug.LogError($"Cue '{cue.name}' has invalid code {cue.code}. Must be 0-255.");
Debug.LogError($"Segment '{segment.name}' transition probabilities sum to {sum}, must be 1.0.");
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
```

### What to Avoid

- Don't reiterate the obvious (e.g., `// Set x to 5` before `x = 5`)
- Don't add documentation to code you didn't write or modify
- Don't add excessive comments to self-explanatory code
- Don't use bullet lists or dashes in documentation; use prose instead

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
        if (allActors.Length > 0) actor = allActors[0];
    }
}
```

### TryGetComponent Pattern

```csharp
if (TryGetComponent<MeshRenderer>(out var meshRenderer))
{
    meshRenderer.enabled = false;
}
```

### Collider Callbacks

```csharp
void OnTriggerEnter(Collider collider) { _inZone = true; }
void OnTriggerExit(Collider collider) { _inZone = false; }
```

---

## Configuration Classes

### Data Classes for YAML

Use simple classes with public fields:

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
public Dictionary<string, byte> GetCueNameToCode()
{
    return cues.ToDictionary(c => c.name, c => (byte)c.code);
}
```

---

## Common Mistakes

### Documentation

```csharp
// Wrong: Noun phrase instead of imperative
/// <summary>A method that processes data.</summary>

// Correct: Imperative mood
/// <summary>Processes the input data and returns results.</summary>
```

```csharp
// Wrong: Missing punctuation
/// <summary>Handles the lick event</summary>

// Correct: Proper punctuation
/// <summary>Handles the lick event.</summary>
```

### Naming

```csharp
// Wrong: Abbreviations and missing underscore prefix
private int curIdx;
private MQTTChannel stimTrigger;

// Correct: Full words with underscore prefix
private int _currentIndex;
private MQTTChannel _stimulusTrigger;
```

### Brace Style

```csharp
// Wrong: K&R style (opening brace on same line)
if (condition) {
    DoSomething();
}

// Correct: Allman style (braces on new lines)
if (condition)
{
    DoSomething();
}
```

### Comments

```csharp
// Wrong: First person, states the obvious
// I'm setting the value to 5
x = 5;

// Correct: Third person imperative, explains why
// Resets counter after corridor transition
x = 5;
```

### List Notation

```csharp
// Wrong: Using bullet lists in documentation
/// <summary>
/// Handles trigger behavior:
/// - When enabled: triggers immediately
/// - When disabled: waits for input
/// </summary>

// Correct: Using prose
/// <summary>
/// Handles trigger behavior. When enabled, triggers immediately. When disabled, waits for input.
/// </summary>
```

---

## Verification Checklist

**You MUST verify your edits against this checklist before submitting any changes to C# files.**

```
C# Style Compliance:
- [ ] XML documentation on all public and private members
- [ ] Imperative mood in summaries ("Processes..." not "This method processes...")
- [ ] Prose used instead of bullet lists in documentation
- [ ] Private fields use `_camelCase` naming
- [ ] Public members use `PascalCase` naming
- [ ] Full words used (no abbreviations like `pos`, `idx`, `msg`)
- [ ] Allman brace style (braces on new lines)
- [ ] Lines under 120 characters
- [ ] Using statements ordered: System, Unity, Third-party, Local
- [ ] Class members ordered: public fields, private fields, properties, lifecycle, methods
- [ ] Inline comments use third person imperative
- [ ] CSharpier formatting applied
```
