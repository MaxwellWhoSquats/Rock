# GroupDetail Conversion: Research and Specs

Working directory for the multi-phase Obsidian conversion of `RockWeb/Blocks/Groups/GroupDetail.ascx`. Holds both the research artifacts that informed the plan and the specs that gate each implementation phase.

## Subdirectories

- **[webforms/](webforms/)** — research on the existing WebForms block: configuration, behavior, code-behind walkthrough, sub-features, dependencies, portability gaps. Start at [webforms/INDEX.md](webforms/INDEX.md).
- **[design/](design/)** — research on the new design (Figma): layout, components, net-new features, redesign decisions. Start at [design/README.md](design/README.md).
- **[specs/](specs/)** — master architecture spec and per-phase implementation specs. Start at specs/INDEX.md. Phase 0 produces the architecture spec; each later phase reads it plus its own spec.

## Sequence

1. WebForms research pass (complete) — captures the parity baseline.
2. Design research pass (complete) — captures the new design and its delta from parity.
3. Phase partitioning informed by both research halves; recommendation lives in [specs/ROADMAP.md](specs/ROADMAP.md).
4. Per-phase specs authored in [specs/](specs/), one phase per Claude session.

## Conventions

- Use file:line references like `RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1556` for every claim about WebForms behavior.
- Use markdown links so the user can click them.
- Do not use em-dashes or `--`.
- Each research file ends with an "Open questions / flag for spec phase" section.

## Completion

When all phases land, the [specs/](specs/) directory will be moved out to `specs/completed/Group/GroupDetail-Conversion/` per the `/spec` skill convention. The research artifacts under [webforms/](webforms/) and [design/](design/) stay in the repo as historical reference (or are archived alongside the completed specs, depending on team preference).
