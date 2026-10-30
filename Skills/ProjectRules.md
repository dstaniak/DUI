# Specific project rules

## Temporary build and test output

Write temporary build, test, and diagnostic logs under `artifacts/logs/`,
never in the repository root. Use a task subdirectory when several logs
belong to the same investigation.

After checking successful results, remove temporary logs that are no longer
needed. Keep failed-run logs only while diagnosing the failure or when the
user requests them. Do not delete or move logs that are still being written.
Git ignore rules prevent commits; they do not replace this cleanup.

## Big changes

When the user requests a large change on a feature branch, split changes into 
separate commits for logical steps. Try the commits not to be overwhelming 
keeping them below 500 inserts. But divide them logically into logical steps.
Separate test introduction commits from the regular program changes.

## Branch name convention

Classify branches by their short local names. A special branch has no
`/` character and is operated manually by the user. A working branch uses
`<type>/<name>`, with `/` separating the type from the name.

Examples of special branches:

- `main`
- `devel`
- `stage`
- `stable`
- `release`

Examples of working branches:

- `feature/new-visuals`
- `bugfix/wrong-sampling-rate`
- `docs/fill-in-classmap`

The presence of `/` alone does not authorize autonomous commits. Only the
prefixes listed below permit them. An unrecognized prefix or detached HEAD
state does not permit autonomous commits.

## Allowed and forbidden autonomous Git actions

Git operations that only inspect local state, such as status, diff, log,
and show, are allowed on every branch and in detached HEAD state.

Autonomously stage and commit changes only when the current local branch
starts with one of these prefixes:

- `bugfix/`
- `hotfix/`
- `feature/`
- `chore/`
- `refactor/`
- `docs/`
- `test/`

On these branches, commit changes when the task is complete, following the
rules in this file. Do not wait for the user to inspect them. For large
tasks, introduce incremental commits for logical steps. The user may amend
or rebase these commits afterward.

Include only changes you introduced for the current task. Do not include
pre-existing user changes or changes from other tasks without explicit
user authorization. When a file contains both, stage only the relevant
changes.

On special branches, branches with unrecognized prefixes, and in detached
HEAD state, staging changes or creating commits requires explicit user
authorization.

Creating or switching branches requires explicit user authorization. Do
not do so autonomously to bypass the restrictions on commits.

Other Git operations that change repository state, including creating tags,
pushing, merging, rebasing, and amending commits, require explicit user
authorization. These restrictions do not prohibit editing working-tree
files as part of an authorized task.

## Issue number

The name after the first `/` may begin with `i<number>-`, where `<number>` is a
positive decimal issue number. The full format is
`<type>/i<number>-<description>`. Extract the number only from this position;
do not infer it from numbers elsewhere in the branch name.

Examples:

- `bugfix/i23-fix-broken-ui` - issue number is 23,
- `bugfix/wrong-display` - issue number is unknown,
- `feature/i1234-new-settings` - issue number is 1234.

Use the issue number explicitly supplied by the user first. An explicit
statement that the task has no issue also takes precedence over the branch
name. Otherwise, obtain the number from the branch name using the format
above. If neither source identifies an issue, ask the user before committing.
Ask only once for the current task; an unanswered question does not establish
that there is no issue. If the user confirms that no issue applies, omit the
issue reference and do not ask again for subsequent commits in that task.

## Commit message format

When performing a commit, follow these rules:

- The first line contains a short commit title. Include "Fix #nr" or
  "Issue #nr" when an issue number is known, following the rules above.
- The second line is empty.
- Following lines contain a concise commit description, optionally as a
  bullet list.
- Every line, including the title and body, must contain at most 60
  characters. Insert actual line breaks when wrapping text.
- Validate the complete commit message programmatically before committing.

## Github connection

When the issue nr is known, then you can reach github directly to inspect the
ticket and execute actions appropriate for the description. After reading the
ticket ask the user for action approval.

## Performance requirements

This project's critical value is performance. Snappy seek, play, pause and
scrubbing is crucial for the user experience and life of the program. Always
take into consideration this assumption. Don't be deceived though, code
maintanability, readability and clean architecture is also very high priority.

Other aspect of this requirement is the multiplatform target. We need to focus
on non-regressive and performant approcha on both targets: Windows and MacOS.

## Class map html doc

The project contains Docs/ClassMap.html which needs to be constantly held
up to date after refactoring, adding or removing new classes.

This also applies when behavior or user workflows change without adding
or removing classes. Check every description of the changed flow: node
summaries, operation templates, graph relations, source links, examples,
and descriptions of test coverage. Updating one card is not sufficient
when another section still describes the previous behavior.

Follow the current UI bindings and calls through to the model and engine.
Do not infer the user workflow from a command that still exists in code
but is no longer bound to the UI. Validate the map data and affected views
after editing, including relation types and source references.

## Versioning

For versioning of the product, the FileVersion csproj field is used.

The version format is A.B.C.D and respective parts are:
- A : major version, not necessarily compatible with previous,
- B : every feature branch increases this,
- C : every bugfix branch increases this - no new functionality,
- D : build number, yet unused.

If asked to increase version, you can automatically do it with following rules:
1. Increase the version in accordance to current branch; if on main or other
   special branch ask for version specifically.
2. Name a version increment commit as "vA.B.C.D - Branch ticket title". If
   there is no ticket connected to current branch, then ask user to provide
   the title.
3. Tag the commit immediately. Tag with annotated tag with version and same
   message as the commit message, such as:
   git -a vA.B.C.D COMMIT_HASH -m "vA.B.C.D - Branch ticket title".

## Changelog

In the project root directory there's a CHANGELOG.md file. It has to be
kept up to date when a tagged commit appears in the work tree. Do not
update the changelog beyond last tagged commit. This update needs to be
carried out only on user specific request.
