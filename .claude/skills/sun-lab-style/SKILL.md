---
name: sun-lab-style
description: >-
  Apply Sun Lab C# and Unity coding conventions when writing, reviewing, or refactoring code. Covers
  XML documentation, naming conventions, code organization, MQTT patterns, and formatting standards.
---

# Sun Lab Style Guide

When writing, reviewing, or refactoring C# code, apply the conventions defined in the Sun Lab
style guide.

See @SUN_LAB_STYLE_GUIDE.md for complete guidelines.

## Key Conventions

- **XML Documentation**: Triple-slash `///` comments with `<summary>`, `<param>`, `<returns>` tags
- **Naming**: Private fields with `_camelCase`; public members with `PascalCase`; full words over abbreviations
- **Brace Style**: Allman style (braces on new lines)
- **Line Length**: Maximum 120 characters
- **Formatting**: Use CSharpier formatter (`csharpier .`) before committing
- **MQTT Patterns**: Type-safe channels with MQTTChannel and MQTTChannel<T> classes
- **Unity Patterns**: MonoBehaviour lifecycle methods, component references, coroutines
- **Comments**: Third-person imperative mood; explain non-obvious logic, not implementation details
