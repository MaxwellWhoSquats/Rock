# Sub-feature: Chat Channel Configuration

## What it is

When the platform's chat system is enabled (`ChatHelper.IsChatEnabled`) AND the group type allows chat (`GroupType.IsChatAllowed`), this group can opt into being a chat channel. Five settings on the group can override the corresponding group type defaults, plus a channel avatar.

Each override is tri-state: null (inherit from group type), true ("y"), or false ("n").

## Trigger conditions

| Condition | Result |
|---|---|
| `ChatHelper.IsChatEnabled == false` (platform-level) | `wpChat.Visible = false`. Save block at [`995`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L995) is also gated, so chat fields untouched. |
| `groupType.IsChatAllowed == false` | `wpChat.Visible = false`. Save block also gated. |
| Both true AND `group.IsSystem == true` | Panel shown, but every control is `Enabled = false` (read-only). |
| Both true AND not system | Panel fully editable. |

## Markup region

[`RockWeb/Blocks/Groups/GroupDetail.ascx`](../../RockWeb/Blocks/Groups/GroupDetail.ascx) lines [`359-398`](../../RockWeb/Blocks/Groups/GroupDetail.ascx#L359).

```
wpChat (visible only if ChatHelper.IsChatEnabled && groupType.IsChatAllowed)
├── Row 1
│   ├── col-md-6
│   │   ├── ddlIsChatEnabled (input-width-xl)
│   │   │     Label: "Enable Chat"
│   │   │     Help:  "If enabled, this group will participate in the chat system as a chat channel."
│   │   │     Items: ""=Inherit from Group Type, "n"=No, "y"=Yes
│   │   └── ddlIsLeavingChatChannelAllowed
│   │         Label: "Allow Members to Leave Channel"
│   │         Help:  "If enabled, individuals are allowed to leave this chat channel."
│   │         Items: same tri-state
│   └── col-md-6
│       ├── ddlIsChatChannelPublic
│       │     Label: "Make Channel Public"
│       │     Help:  "If enabled, this chat channel is visible to everyone when performing a search.
│       │            This also implies that the channel may be joined by any person via the chat application."
│       └── ddlIsChatChannelAlwaysShown
│             Label: "Always Show Channel"
│             Help:  "If enabled, this chat channel is always shown in the channel list even if the person has not joined the channel."
├── Row 2
│   └── col-md-6
│       └── ddlChatPushNotificationMode (input-width-xl)
│             Label: "Push Notification Mode"
│             Help:  "Controls how push notifications are sent for this chat channel."
│             Items: ""=Inherit, "0"=All Messages, "1"=Mentions, "2"=Silent
└── (col-md-6 in same row)
    └── imgChatChannelAvatar (ImageUploader)
          Label: "Channel Avatar"
          Help:  "The image to use for this chat channel in the external chat system. Recommended image size is 120x120."
          BinaryFileTypeGuid: Rock.SystemGuid.BinaryFiletype.DEFAULT
```

## Code-behind

| Method | Lines | Notes |
|---|---|---|
| [`SetChatControls`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2512) | 2512-2561 | Visibility + value population. Disables every control if `IsSystem`. |
| `btnSave_Click` body | [`993-1009`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L993) | Reads "y"/"n"/empty back to bool? overrides. Tracks orphaned avatar binary file id. |
| `btnSave_Click` final block | [`1382-1405`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1382) | Toggles `IsTemporary` on binary files: orphaned -> true, current -> false. |
| `ShowReadonlyDetails` body | [`2664-2672`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L2664) | Shows `hlChat` highlight label if `group.GetIsChatEnabled()` resolves true. |
| `ddlGroupType_SelectedIndexChanged` | [`1573`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1573) | Calls `SetChatControls` with the newly-selected group type. |

## Persisted fields on Group

| Field | Type | Notes |
|---|---|---|
| `IsChatEnabledOverride` | bool? | null = inherit |
| `IsLeavingChatChannelAllowedOverride` | bool? | null = inherit |
| `IsChatChannelPublicOverride` | bool? | null = inherit |
| `IsChatChannelAlwaysShownOverride` | bool? | null = inherit |
| `ChatPushNotificationModeOverride` | enum? (`ChatNotificationMode`) | null = inherit |
| `ChatChannelAvatarBinaryFileId` | int? | references BinaryFile |

## Tri-state UI mapping

The dropdowns use string values "" (empty), "y", "n". The C# converts:
- "" -> null (inherit)
- "y" -> true
- "n" -> false

For the push notification mode dropdown, values are "" or 0/1/2 mapping to the enum.

Save block ([`997-1001`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L997)):

```csharp
group.IsChatEnabledOverride               = ddlIsChatEnabled.SelectedValue.AsBooleanOrNull();
group.IsLeavingChatChannelAllowedOverride = ddlIsLeavingChatChannelAllowed.SelectedValue.AsBooleanOrNull();
group.IsChatChannelPublicOverride         = ddlIsChatChannelPublic.SelectedValue.AsBooleanOrNull();
group.IsChatChannelAlwaysShownOverride    = ddlIsChatChannelAlwaysShown.SelectedValue.AsBooleanOrNull();
group.ChatPushNotificationModeOverride    = ddlChatPushNotificationMode.SelectedValueAsEnumOrNull<ChatNotificationMode>();
```

The `AsBooleanOrNull` extension parses "y"/"yes"/"true" -> true, "n"/"no"/"false" -> false, anything else -> null.

## Side effects: chat-channel-avatar binary file management

The orphan toggle is a two-step pattern:

1. During save ([`1003-1006`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1003)): if the avatar binary file id is changing, capture the OLD value as `orphanedChatChannelAvatarId`.

   ```csharp
   if ( group.ChatChannelAvatarBinaryFileId != imgChatChannelAvatar.BinaryFileId )
   {
       orphanedChatChannelAvatarId = group.ChatChannelAvatarBinaryFileId;
   }
   group.ChatChannelAvatarBinaryFileId = imgChatChannelAvatar.BinaryFileId;
   ```

2. After the main `SaveChanges` ([`1382-1405`](../../RockWeb/Blocks/Groups/GroupDetail.ascx.cs#L1382)):

   ```csharp
   if ( orphanedChatChannelAvatarId.HasValue || group.ChatChannelAvatarBinaryFileId.HasValue )
   {
       var binaryFileService = new BinaryFileService( rockContext );

       if ( orphanedChatChannelAvatarId.HasValue )
       {
           var binaryFile = binaryFileService.Get( orphanedChatChannelAvatarId.Value );
           if ( binaryFile != null )
           {
               binaryFile.IsTemporary = true; // mark old avatar for cleanup job
           }
       }

       if ( group.ChatChannelAvatarBinaryFileId.HasValue )
       {
           var binaryFile = binaryFileService.Get( group.ChatChannelAvatarBinaryFileId.Value );
           if ( binaryFile != null )
           {
               binaryFile.IsTemporary = false; // pin the new avatar
           }
       }

       rockContext.SaveChanges();
   }
   ```

Three cases:

| Old avatar | New avatar | Effect |
|---|---|---|
| None | None | No-op (the outer `if` short-circuits). |
| None | New | New avatar's `IsTemporary` -> false (pin it). |
| Old | None | Old avatar's `IsTemporary` -> true (so cleanup job deletes it later). |
| Old | New (different) | Old -> true, New -> false. |
| Old | New (same) | The `!=` check at line 1003 is false; no-op. |

This is wrapped inside the same `WrapTransaction` as the rest of the save.

## Visibility

```csharp
wpChat.Visible = ChatHelper.IsChatEnabled && groupType?.IsChatAllowed == true;
```

If `group.IsSystem`, every control inside is disabled (read-only) but the panel still renders.

## View-mode label

`hlChat` shows `"Chat-Enabled <i class='ti ti-messages'></i>"` when `group.GetIsChatEnabled()` is true. Hidden otherwise.

`Group.GetIsChatEnabled()` is an instance method that resolves the override-or-inherit logic to a definitive bool (group's override wins; otherwise the group type's value).

## Permission inheritance

No separate gate beyond EDIT-on-group. The IsSystem read-only behavior is the only special case.

## Edge cases

- Removing the avatar entirely: `imgChatChannelAvatar.BinaryFileId == null`. The save block sets `group.ChatChannelAvatarBinaryFileId = null`. The orphan handling marks the old avatar's `IsTemporary = true`. The "if new" block doesn't fire because `group.ChatChannelAvatarBinaryFileId.HasValue == false`.
- Replacing an avatar with the same file: `imgChatChannelAvatar.BinaryFileId == group.ChatChannelAvatarBinaryFileId`. The `!=` check short-circuits. No `IsTemporary` toggling occurs.
- The `SetChatControls` method runs both for `ddlGroupType_SelectedIndexChanged` (with a fresh new Group) and `ShowEditDetails` (with the loaded Group). When called with a fresh Group, all override fields are null, so the dropdowns default to "Inherit". This is the right behavior for "I just changed the group type, reset the inheritance UX".

## Phase considerations

- Self-contained sub-feature.
- Depends on the platform-level chat system being installed; the bag should signal whether to render the panel.
- The binary file `IsTemporary` toggle is non-trivial. It needs to be implemented carefully in the new save action.

## Open questions / flag for spec phase

- The `ChatNotificationMode` enum has values 0=All, 1=Mentions, 2=Silent. The dropdown items hard-code these in markup; the new block should `BindToEnum` instead so additional values stay in sync automatically.
- The image-uploader's `BinaryFileTypeGuid` is set in `SetChatControls`. The Obsidian uploader will need the same guid passed via the bag.
- Confirm that disabling all controls when `IsSystem` is the desired UX for system groups; an alternative is to hide the entire panel.

## Notes

- `ChatHelper.IsChatEnabled` is a static property of `Rock.Communication.Chat.ChatHelper` indicating whether chat is configured at the platform level.
- `Group.GetIsChatEnabled()` is an instance method that resolves the override-or-inherit logic.
- Default avatar binary file type guid: `Rock.SystemGuid.BinaryFiletype.DEFAULT`.
