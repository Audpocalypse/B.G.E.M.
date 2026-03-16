# Advanced Variant Reference Material

This folder contains **reference material only** for the planned Advanced Variant backend feature.

## Files in this folder

- `FO4 Iterative Record Generators MS v200.pas`
- `cobjrecipes.csv`

These files come from a different system and a different runtime environment. They should **not** be ported directly.

They exist to preserve useful logic patterns and data-shape concepts that may inform a new C# implementation in this repository.

---

## Purpose of the Pascal file

The Pascal script was originally used in an FO4Edit/xEdit-related workflow and was constrained by that environment, including API and interpreter limitations.

Its value here is **not** as a UI template.

Its value is primarily as a reference for:

- multi-layer / multi-dimension index logic
- expansion of combinations across multiple dimensions
- default/wildcard behavior
- override specificity
- naming/token substitution concepts
- inferred index formatting behavior

The C# implementation should preserve useful behavior, but should remain idiomatic to this application and should not mimic Pascal structure unnecessarily.

---

## Purpose of the CSV file

The CSV demonstrates the **rule/data style** used by the original logic.

It is useful for understanding:

- how rows are structured
- how blank values can act as wildcard/default values
- how more specific rows override broader rows
- how generated data may need to resolve from general rules to specific rules

The CSV should be treated as a schema and behavior example, not as proof that CSV must be the primary user-facing authoring method in this application.

---

## Key concepts to extract

### 1. Multiple logical layers or dimensions

The original workflow uses multiple axes of variation. In the sample data, these are represented as fields such as:

- ObjType
- ObjName
- Slot
- Opt

In the new C# feature, these map conceptually to layered or multi-dimensional variant generation.

The exact names in the C# application may differ, but the pattern is the same:

- up to several layers
- each layer may be specified or omitted
- combinations are expanded or resolved across these dimensions

---

### 2. Wildcard/default behavior

A major behavior shown in the CSV is that blank values can represent broader applicability.

Examples of intended meaning:

- all fields blank = global/default rule
- some fields filled, others blank = partially specific rule
- all relevant fields filled = exact rule

This creates a specificity model:

- broader rows apply generally
- narrower rows override broader ones when more dimensions are specified

This behavior is important and should be considered when designing the backend rule engine.

---

### 3. Rule specificity and override resolution

The useful logic pattern is not merely "generate rows."

It is also:

- identify which rules apply to a given resolved combination
- prefer more specific matches over less specific ones
- preserve a predictable override order

This is likely more important than any specific Pascal syntax.

---

### 4. Derived index formatting (`IndexNN`)

`IndexNN` should be understood as something **derived from the resolved multi-layer index state**, not necessarily entered directly by the user.

The original logic suggests that zero-padded or otherwise formatted indices are produced from the active combination context.

For the C# implementation, the primary source of truth should generally be the resolved layer values.  
Formatted index strings such as `IndexNN` should be generated from those values.

---

### 5. Tokenized naming

The target feature is expected to support token replacement concepts such as:

- `(index)`
- `(indexNN)`
- `(indexTok)`

Intended interpretation:

- `(index)` = a normal numeric index or resolved index value
- `(indexNN)` = a formatted / padded derived index string
- `(indexTok)` = user-authored text token associated with a row or resolved combination

The exact implementation details can differ from the original script if the C# design benefits from a clearer or more maintainable structure.

---

## What should NOT be copied literally

The following should not be copied directly unless there is a very specific reason:

- FO4Edit-specific record handling
- xEdit-specific APIs
- JvInterpreter-related workarounds
- Pascal UI patterns
- Pascal naming conventions that do not fit the C# codebase
- script-environment limitations that do not apply in this application

This folder is meant to preserve **behavioral intent**, not legacy implementation details.

---

## Likely target shape in C#

The expected direction for the new feature is something like:

- shared backend models
- shared rule/expansion engine
- shared token substitution logic
- shared validation logic
- later, one or more UI entry points that consume the backend

The advanced UI may eventually support table-based editing, but that UI should be designed around the needs of this application rather than copied from the legacy logic.

---

## Guidance for implementation work

When using this folder as reference:

1. Identify only the Pascal sections relevant to:
   - layered index expansion
   - wildcard/default behavior
   - specificity resolution
   - derived index formatting
   - token/name generation

2. Use the CSV to understand:
   - row shape
   - wildcard semantics
   - override patterns
   - practical examples of resolved combinations

3. Translate those behaviors into clean, idiomatic C# backend code.

4. Keep the backend reusable for:
   - Bulk Generate advanced path
   - future Bulk Edit advanced path

5. Avoid overfitting the new design to legacy implementation details.

---

## Summary

This folder exists to answer:

- what behavior is intended
- how rule rows can behave
- how layered indices may resolve
- how naming tokens may be derived

It does **not** define the exact final architecture, UI, or file format requirements for the C# application.