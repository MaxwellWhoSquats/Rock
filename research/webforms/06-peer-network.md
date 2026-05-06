# Sub-feature: Peer Network

## What it is

Peer Network is a feature that calculates relationship strength scores between people based on shared group memberships. Each group type defines a base relationship strength (None, Basic, Strong, Intense), an optional growth-over-time flag, and four role-pair multipliers (Leader-to-Leader, Leader-to-NonLeader, NonLeader-to-Leader, NonLeader-to-NonLeader). Each individual `Group` can override its parent group type's settings.

This sub-panel lives inside `wpGeneral` and is rendered only when `GroupType.IsPeerNetworkEnabled == true`.

## Trigger conditions

| Condition | Result |
|---|---|
| `GroupType.IsPeerNetworkEnabled == false` | `pnlPeerNetworkOverride` is hidden. `SetPeerNetworkControls` short-circuits at line [`2397-2400`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2397). |
| `GroupType.IsPeerNetworkEnabled == true` | Override panel shown. Default checkbox state set from `Group.IsOverridingGroupTypePeerNetworkConfiguration`. |
| `cbOverrideRelationshipStrength.Checked == true` | All inner controls in `pnlPeerNetwork` shown. `rblRelationshipStrength` defaults to group's override OR group type's value. |
| `cbOverrideRelationshipStrength.Checked == false` | Inner controls hidden but the radio still defaults to the inherited value (so toggling on does not reset the visible state). |
| `relationshipStrength == 0` (None) | `pnlRelationshipGrowth` and `pnlShowPeerNetworkAdvancedSettings` are hidden (per [`SetPeerNetworkSubControlVisibility`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2449)). |
| `swShowPeerNetworkAdvancedSettings.Checked == true` AND strength > 0 | Multiplier panel `pnlPeerNetworkAdvanced` shown. |
| Either `groupType.AreAnyRelationshipMultipliersCustomized` OR `group.AreAnyRelationshipMultipliersCustomized` | Switch defaults to ON and advanced panel is auto-expanded ([`2412-2414`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2412)). |

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines [`136-193`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L136).

```
pnlPeerNetworkOverride
├── cbOverrideRelationshipStrength (autopostback)
│     Label: "Override Relationship Strength"
│     Help:  "A relationship strength has been set for this group type, but you can override that configuration here."
└── pnlPeerNetwork (visible if checkbox checked)
    ├── rblRelationshipStrength radio (autopostback)
    │     Label: "Relationship Strength"
    │     Help:  "Sets the relationship strength for individuals in groups of this type. Advanced settings offer additional customization options based on the individual's role in the group."
    │     RepeatDirection: Horizontal, CssClass: js-relationship-strength
    ├── pnlRelationshipGrowth (col-md-6, visible if strength > 0)
    │   └── cbEnableRelationshipGrowth
    │         Label: "Enable Relationship Growth Over Time"
    │         Help:  "Enable this setting to allow relationship strength to grow over time as individuals spend more time together in the group."
    ├── pnlShowPeerNetworkAdvancedSettings (visible if strength > 0)
    │   └── swShowPeerNetworkAdvancedSettings (Switch, autopostback)
    │         Text: "Show Advanced Settings"
    └── pnlPeerNetworkAdvanced (visible if Switch on)
        ├── lRelationshipStrenghAdvanced (literal explainer)
        │     "You can adjust the relationship score below based on the relationship type of the
        │      group members. For instance, if the group members have a strong relationship with
        │      the leaders but do not know other non-leaders, you can reduce or eliminate the
        │      relationship score percentage between non-leaders."
        ├── 3-column grid label header (Leader, Non-Leader)
        ├── tbLeaderToLeaderRelationshipMultiplier (input-width-sm)
        ├── tbLeaderToNonLeaderRelationshipMultiplier
        ├── tbNonLeaderToLeaderRelationshipMultiplier
        └── tbNonLeaderToNonLeaderRelationshipMultiplier
```

## Code-behind region

| Method | Lines | Notes |
|---|---|---|
| [`SetPeerNetworkControls`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2391) | 2391-2442 | Visibility, value population, placeholders. |
| [`SetPeerNetworkSubControlVisibility`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2449) | 2449-2456 | Hides Growth and Advanced when strength == 0. |
| [`cbOverrideRelationshipStrength_CheckedChanged`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L5007) | 5007-5010 | Toggles `pnlPeerNetwork.Visible`. |
| [`rblRelationshipStrength_SelectedIndexChanged`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L5017) | 5017-5023 | Recomputes Growth/Advanced visibility based on new strength. |
| [`swShowPeerNetworkAdvancedSettings_CheckedChanged`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L5030) | 5030-5033 | Toggles advanced multiplier panel. |
| Save block ([`1089-1117`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1089)) | | Persists overrides only when `IsPeerNetworkEnabled`. |
| Group type re-bind on change ([`1566`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1566)) | | `ddlGroupType_SelectedIndexChanged` calls `SetPeerNetworkControls` with a fresh `Group`. |

## Save logic specifics

```csharp
if ( group.GroupType.IsPeerNetworkEnabled )
{
    if ( cbOverrideRelationshipStrength.Checked )
    {
        group.RelationshipStrengthOverride = rblRelationshipStrength.SelectedValueAsInt() ?? (int)RelationshipStrength.None;
        group.RelationshipGrowthEnabledOverride = cbEnableRelationshipGrowth.Checked;
        group.LeaderToLeaderRelationshipMultiplierOverride = tbLeaderToLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( 0, 100 );
        group.LeaderToNonLeaderRelationshipMultiplierOverride = tbLeaderToNonLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( 0, 100 );
        group.NonLeaderToLeaderRelationshipMultiplierOverride = tbNonLeaderToLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( 0, 100 );
        group.NonLeaderToNonLeaderRelationshipMultiplierOverride = tbNonLeaderToNonLeaderRelationshipMultiplier.Text.AsDecimalPercentageOrNull( 0, 100 );
    }
    else
    {
        // Clear all overrides
        group.RelationshipStrengthOverride = null;
        group.RelationshipGrowthEnabledOverride = null;
        group.LeaderToLeaderRelationshipMultiplierOverride = null;
        group.LeaderToNonLeaderRelationshipMultiplierOverride = null;
        group.NonLeaderToLeaderRelationshipMultiplierOverride = null;
        group.NonLeaderToNonLeaderRelationshipMultiplierOverride = null;
    }
}
// else: leave existing override values in place. The calculation engine ignores
//       them when the parent group type has the feature disabled.
```

The four multipliers are parsed via `AsDecimalPercentageOrNull(0, 100)` so they accept inputs like `75` or `75%` and store as a decimal in the range 0.0-1.0.

## View-mode label (highlight label)

`hlPeerNetwork` ([`2609-2662`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2609)):
- Hidden if not `IsPeerNetworkEnabled`.
- Text: `"{Strength.Titleized} Relationships [icons]"`.
- Icons:
  - `ti-chart-line` if `isRelationshipGrowthEnabled && finalStrength > 0` ("strengthens over time").
  - `ti-asterisk` if `IsOverridingGroupTypePeerNetworkConfiguration` (in any way).
- Tooltip describes the relationship in prose:
  - "Individuals in this group share a strong relationship, overriding the group type's default setting of a basic relationship. The relationship is also set to strengthen over time."

The `GetRelationshipStrengthLabel` helper ([`2866-2895`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2866)) converts the integer score to `{ Article, Relationship }`. Out-of-range values fall back to `"an unknown"`.

## Inline JavaScript

`addRelationshipStrengthTooltips` (in `.ascx` [`794-820`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L794)) adds context-sensitive tooltips to the radio buttons:

| Value | Tooltip text |
|---|---|
| 0 | "No established relationship or interaction." |
| 5 | "Basic interactions with a familiar but limited bond." |
| 10 | "Frequent interactions characterized by a strong and supportive relationship." |
| 20 | "Intense and trusted relationship with a high level of personal engagement and understanding." |

Tooltips dismiss on click. In Obsidian this becomes a Vue tooltip directive on the radio options.

## Relevant cache / model surface

- `Rock.Enums.Group.RelationshipStrength` (enum int values None=0, Basic=5, Strong=10, Intense=20).
- `GroupTypeCache.IsPeerNetworkEnabled`, `RelationshipStrength`, `RelationshipGrowthEnabled`, `LeaderToLeaderRelationshipMultiplier`, etc.
- `GroupTypeCache.AreAnyRelationshipMultipliersCustomized` (computed property).
- `Group.IsOverridingGroupTypePeerNetworkConfiguration` (computed property).
- `Group.AreAnyRelationshipMultipliersCustomized` (computed property).

## Persisted state

| Column on `Group` | Type | Notes |
|---|---|---|
| `RelationshipStrengthOverride` | int? | null = inherit from group type |
| `RelationshipGrowthEnabledOverride` | bool? | null = inherit |
| `LeaderToLeaderRelationshipMultiplierOverride` | decimal? | null = inherit. Stored 0.0-1.0; UI shows 0-100%. |
| `LeaderToNonLeaderRelationshipMultiplierOverride` | decimal? | null = inherit. |
| `NonLeaderToLeaderRelationshipMultiplierOverride` | decimal? | null = inherit. |
| `NonLeaderToNonLeaderRelationshipMultiplierOverride` | decimal? | null = inherit. |

No FK relationships to other entities. No cascade behavior. All the persistence is scalar columns on `Group`.

## Permission inheritance

This panel has no separate permission gate. It shows whenever `wpGeneral` shows, which is whenever the user has `EDIT` on the group.

## Edge cases

- Toggling the override checkbox OFF clears all six override fields as a single bundle. There is no per-field "stop overriding" UX.
- Toggling group type from one IsPeerNetworkEnabled=true type to another preserves whatever overrides the user had typed in the form during this postback. They are not reset.
- Toggling group type to a IsPeerNetworkEnabled=false type hides the panel; the save no-ops on the override fields, so any pre-existing values stay in the database. The block comment at [`1116-1117`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1116) documents this is intentional ("the calculations will safely ignore this group's values").
- The "advanced" multipliers each act independently. Setting only one (e.g., LeaderToLeader) keeps the other three using the group type's defaults. The placeholder UX for each box is the group type's value, formatted as a percent.
- If the strength radio is set to None (0), Growth and Advanced are hidden. But on save, if override-checkbox is still checked, all four multipliers are still persisted (likely as null since nothing was entered). They simply have no effect because no edges in the network calculation evaluate them when strength = 0.

## Phase considerations

This sub-feature is well-isolated and self-contained. It touches only Group + GroupType and has no cross-block dependencies. Good candidate for an early phase.

## Open questions / flag for spec phase

- Is the Override checkbox needed at all in Obsidian, or can the bag use "all six fields nullable, no gating checkbox"? The current pattern bundles them, but technically each multiplier is individually nullable.
- The placeholder-as-default-value UX (showing group-type values dimmed) is awkward to replicate cleanly in Vue. Consider a small "Reset to inherited" link per field.
- The `AreAnyRelationshipMultipliersCustomized` auto-expansion is fragile (it expands the panel even when the user just opened the page to look at a group with custom values). Confirm this UX is desired or simplify.

## Notes

- The four multiplier overrides are individually nullable; the calculation engine inspects each one separately. Setting only one keeps the other three using the group type's defaults. This is what allows the placeholder UX to work cleanly.
- The "override the entire configuration" checkbox is purely a UI device. When unchecked, the save clears all four multipliers + strength + growth as a single bundle.
