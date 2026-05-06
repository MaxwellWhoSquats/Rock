# GroupDetail Conversion Research

Research for the conversion of `RockWeb/Blocks/Groups/GroupDetail.ascx` to the Obsidian framework. This is a multi-session, spec-driven conversion. Research is organized by domain to keep concerns separated.

## Subdirectories

- **[webforms/](webforms/)** — research on the existing WebForms block: configuration, behavior, code-behind walkthrough, sub-features, dependencies, portability gaps. Start at [webforms/INDEX.md](webforms/INDEX.md).
- **[design/](design/)** — research on the new design (Figma): layout, components, net-new features, redesign decisions. Start at [design/README.md](design/README.md). Pending Figma link.

## Sequence

1. WebForms research pass (in progress / mostly complete) — captures the parity baseline.
2. Design research pass (pending Figma) — captures the new design and its delta from parity.
3. Phase partitioning is then revised to incorporate the design delta.
4. Per-phase specs are authored under `specs/`.

## Conventions

- Use file:line references like `RockWeb/Blocks/Groups/GroupDetail.ascx.cs:1556` for every claim about WebForms behavior.
- Use markdown links so the user can click them.
- Do not use em-dashes or `--`.
- Each research file ends with an "Open questions / flag for spec phase" section.
