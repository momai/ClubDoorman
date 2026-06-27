# Duplicate Audit IDs

Total duplicate IDs: **6**


## `E2E_FakeTelegramClient_ShouldSupportUserBanning`

- **Full ID**: `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs::InfrastructureE2ETests::E2E_FakeTelegramClient_ShouldSupportUserBanning`
- **Occurrences**: 2
- **Decisions**: `delete`, `delete`
- **Decisions agree**: Yes

  - **File**: `batch-0027.audit.jsonl` — decision=`delete`
  - **File**: `batch-0037.audit.jsonl` — decision=`delete`

- **Canonical record**: from `batch-0037.audit.jsonl`
- **Reason**: decisions agree, picked later file as canonical

## `CheckUserName_WithNormalName_ReturnsAllow`

- **Full ID**: `ClubDoorman.Test/ModerationServiceSimpleTests.cs::ModerationServiceSimpleTests::CheckUserName_WithNormalName_ReturnsAllow`
- **Occurrences**: 2
- **Decisions**: `rewrite`, `delete`
- **Decisions agree**: No

  - **File**: `batch-0037.audit.jsonl` — decision=`rewrite`
  - **File**: `batch-0038.audit.jsonl` — decision=`delete`

- **Canonical record**: NONE (sent to human review)
- **Reason**: decisions DISAGREE — cannot auto-resolve

## `LeftMemberCleanup_EmitsSemanticRule`

- **Full ID**: `ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs::MessageHandlerSemanticsTests::LeftMemberCleanup_EmitsSemanticRule`
- **Occurrences**: 2
- **Decisions**: `quarantine`, `rewrite`
- **Decisions agree**: No

  - **File**: `batch-0042.audit.jsonl` — decision=`quarantine`
  - **File**: `batch-0083.audit.jsonl` — decision=`rewrite`

- **Canonical record**: NONE (sent to human review)
- **Reason**: decisions DISAGREE — cannot auto-resolve

## `ShouldAllowChannelMessageAsync_WhenChannelDiscussion_ShouldReturnTrue`

- **Full ID**: `ClubDoorman.Test/Unit/Services/ChannelModerationServiceTests.cs::ChannelModerationServiceTests::ShouldAllowChannelMessageAsync_WhenChannelDiscussion_ShouldReturnTrue`
- **Occurrences**: 2
- **Decisions**: `keep`, `keep`
- **Decisions agree**: Yes

  - **File**: `batch-0098.audit.jsonl` — decision=`keep`
  - **File**: `batch-0099.audit.jsonl` — decision=`keep`

- **Canonical record**: from `batch-0099.audit.jsonl`
- **Reason**: decisions agree, picked later file as canonical

## `ShouldAllowChannelMessageAsync_WhenUnknownChannel_ShouldReturnFalse`

- **Full ID**: `ClubDoorman.Test/Unit/Services/ChannelModerationServiceTests.cs::ChannelModerationServiceTests::ShouldAllowChannelMessageAsync_WhenUnknownChannel_ShouldReturnFalse`
- **Occurrences**: 2
- **Decisions**: `keep`, `keep`
- **Decisions agree**: Yes

  - **File**: `batch-0098.audit.jsonl` — decision=`keep`
  - **File**: `batch-0099.audit.jsonl` — decision=`keep`

- **Canonical record**: from `batch-0099.audit.jsonl`
- **Reason**: decisions agree, picked later file as canonical

## `CheckMessageAsync_MimicryDetected_ReturnsBanAction`

- **Full ID**: `ClubDoorman.Test/Unit/Services/ModerationServiceBusinessLogicTests.cs::ModerationServiceBusinessLogicTests::CheckMessageAsync_MimicryDetected_ReturnsBanAction`
- **Occurrences**: 2
- **Decisions**: `rewrite`, `delete`
- **Decisions agree**: No

  - **File**: `batch-0033.audit.jsonl` — decision=`rewrite`
  - **File**: `batch-0034.audit.jsonl` — decision=`delete`

- **Canonical record**: NONE (sent to human review)
- **Reason**: decisions DISAGREE — cannot auto-resolve
