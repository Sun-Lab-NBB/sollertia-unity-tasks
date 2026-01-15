---
name: applying-sun-lab-style
description: >-
  Applies Sun Lab C# and Unity coding conventions when writing, reviewing, or refactoring code. Covers
  .cs files, XML documentation, naming conventions, MQTT patterns, README.md files, git commit messages,
  and YAML task templates. Use when writing or modifying any .cs, .md, or .yaml files in Sun Lab projects.
---

# Sun Lab Style Guide

Applies Sun Lab coding and documentation conventions.

**You MUST read the appropriate style guide and apply its conventions when writing or modifying any code,
documentation, commits, or skills. You MUST verify your changes against the style guide's checklist before submitting.**

---

## Style Guides

| Guide                              | Use When                                                                  |
|------------------------------------|---------------------------------------------------------------------------|
| [CSHARP_STYLE.md](CSHARP_STYLE.md) | Writing C# code (naming, formatting, XML docs, inline comments, patterns) |
| [README_STYLE.md](README_STYLE.md) | Creating or updating README files                                         |
| [COMMIT_STYLE.md](COMMIT_STYLE.md) | Writing git commit messages                                               |
| [SKILL_STYLE.md](SKILL_STYLE.md)   | Creating Claude skills or YAML task templates                             |

---

## Quick Reference

### C# Code (includes API docs and inline comments)

- **XML Documentation**: Triple-slash `///` with `<summary>`, `<param>`, `<returns>` tags
- **Prose Over Lists**: Use prose in all documentation; bullet lists are forbidden in code comments
- **Inline Comments**: Third person imperative, above the code, explain non-obvious logic
- **Naming**: Private fields `_camelCase`, public members `PascalCase`
- **Brace Style**: Allman (braces on new lines)
- **Line Length**: Maximum 120 characters
- **Formatting**: Run `csharpier .` before committing

### Commit Messages

- Start with past tense verb: Added, Fixed, Updated, Refactored, Removed
- Header line ≤ 72 characters
- End with a period

### README Files

- Third person voice throughout
- Present tense as default

### Skills & Templates

- SKILL.md frontmatter: `name` (gerund form), `description` (third person)
- YAML templates: Header with Project, Purpose, Layout, Related fields
- Line length ≤ 120 characters
