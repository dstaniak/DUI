---
name: general-csharp
description: >-
  Guidelines for writing, refactoring, and reviewing C# code in the ClipMaker
  project, with an emphasis on human readability, structure, naming, and
  explicit contracts.
---

# General C# Skill

## Purpose

Write code for a human who reads slowly and can hold only a limited part of the
program in mind at once. Structure, names, and the order of operations should
reveal the author's intent and the relationship between cause and effect
without requiring the reader to follow many files simultaneously. Evaluate a
solution partly by how much context it takes to understand it.

## C# and .NET conventions

Follow the relevant Microsoft guidelines for class and member organization,
code style, naming, API design, and events, including handler names, standard
signatures. Consult the documentation as needed for the particular change;
this skill does not reproduce the full set of guidelines.

The current member order in the project's classes is arbitrary and **is not a
convention to follow**. Use Microsoft guidelines as the basis for organizing
members. Where the documentation leaves room for choice, adopt a consistent,
readable arrangement and distinguish that choice from an official
recommendation. Account for the requirements of the framework and generated
code.

References: [C# documentation][csharp] for coding and naming conventions, and
[.NET documentation][standard]. Distinguish general API design guidelines from
conventions adopted for documentation samples.

## Structure and separation of responsibilities

- Break larger operations into cohesive, named steps. The coordinating method
  should show the flow, while helper methods explain the details of individual
  steps. Extracting `ApplyTimingChange` and `ApplyLayoutChange` from `Apply` is
  an example.
- Extract a method when it gives a block of logic a meaningful name, separates
  a responsibility, or centralizes a shared rule. Avoid fragmentation that
  forces the reader to keep jumping between trivial functions. Do not impose an
  arbitrary line count limit.
- Keep a similar level of detail within each step. Break up long lambdas,
  conditions, and LINQ expressions when they obscure the algorithm.
- Use named local variables for long LINQ queries when this improves
  readability.
- Give long or non-obvious Boolean conditions meaningful names using local
  variables, methods, local functions, or properties.
- Separate logical stages with blank lines. Format complex conditions and
  argument lists so that their parts are visible. Avoid cramming declarations,
  control flow, and side effects onto a single line.
- Keep a rule with the type responsible for it. Centralize shared decisions,
  such as the conditions for resuming playback, so that different paths do not
  drift toward different criteria.
- Use Allman brace style. Compact `try`/`catch` blocks with a single statement
  in each block are an allowed exception. Do not put multiple statements from
  the same block on one line. Always place a conditional `return` on its own
  line below the `if` condition.

## Names and data semantics

- Choose names that are short but sufficient to identify the role of the data
  or the effect of an operation. Avoid both vague names and names that read
  like entire sentences.
- Identify the subject when omitting it would make the name ambiguous:
  `_workerRunning` instead of `_running`, or `changedNles` instead of
  `changed`. Do not repeat information that is already obvious from the
  immediate context.
- State and flag names should describe what is actually true. Distinguish
  requested, processed, and presented state. Names such as `next`, `previous`,
  or `processed` are sufficient only when the reader can readily identify their
  precise roles.
- Express important domain concepts through named properties or methods, such
  as `InInteractivePhase`. The name should describe the entire condition it
  represents.
- Give each new class, record, or struct a clear responsibility and clear data
  semantics. When these are not apparent from the declaration, explain them in
  a brief comment, including relevant units, scope, or state ownership.

## Comments that explain intent

Add comments where correct interpretation requires knowledge that is not
apparent at first glance. Focus on the reason for a decision, the dependency
between steps, and any invariant that must be preserved.

Pay particular attention to the meaning of version counters, retaining previous
state, deferred operations, resource ownership, concurrency, and initialization
or cleanup order. For example, a comment about deferral should identify who
later supplies the final state and what causes the outstanding operation to
run.

Place comments close to the decisions they explain. Use XML documentation for a
type or method contract when it helps the API consumer. Avoid mechanically
restating obvious instructions. Update comments when the logic changes; do not
treat a historical comment as proof of current behavior.

## Explicit contracts and operation order

- Aim to make correct API usage follow from its design. Pass required data and
  dependencies explicitly; use constructors, factories, types representing
  state, or validated transitions where they help.
- If correctness depends on a sequence of calls, consider encapsulating it in a
  single operation or an object that manages the lifecycle. API consumers
  should not have to guess that an extra step between construction and startup
  is necessary to avoid losing events.
- When the caller must retain control of the sequence, name the stages,
  document their requirements, and guard against invalid transitions. For
  operations with separate start and completion steps, account for
  cancellation, failure, and interrupted interaction as well.
- Distinguish command acceptance from execution and from observing the result.
  If completion is asynchronous, the contract should specify how completion is
  confirmed and correlated with the correct request.
- Match the strength of the contract to the actual problem. Do not introduce an
  elaborate type hierarchy or state protocol when a simpler API provides the
  same clarity.

## Warnings

It's forbidden to leave warnings in a finished code that builds and runs. There
might be some exceptions from that rule like native code interop with UTF-8
(CA2101) or other impossible to avoid, like Avalonia accessing property through
abstract interface (CA1822) or legacy naming (CA1720, CA1711). But otherwise
the warnings should be treated like errors and fixed.

## Refactoring and scope

Preserve program behavior when reorganizing code. In particular, check the
order of side effects, lock and exception handling boundaries, state captured
by callbacks, and the timing of resource cleanup. Extracting a method must not
accidentally change these relationships. Match validation to the risk of the
change, using existing tests for the relevant scenarios.

Before finishing, read the changed flow as someone who does not remember the
rest of the implementation: is it clear where the data comes from, what is
compared, what changes, and why the next step can proceed? Improve places that
require guessing the intent. Keep cleanup within the scope of the task; this
skill alone does not authorize restructuring the entire project.

Never increase file version in project not asked explicitly by the user.

[csharp]: https://learn.microsoft.com/en-us/dotnet/csharp/
[standard]: https://learn.microsoft.com/en-us/dotnet/standard/
