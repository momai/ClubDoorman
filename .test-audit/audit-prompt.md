# Test Audit Prompt

You are auditing C# tests for the ClubDoorman Telegram bot project.

## Input

You will receive a JSON batch file containing test records. Each record has:
- `raw`: metadata (file, class, test name, line, infrastructure flags, assertion flags, smells)
- `source_excerpt`: the test source code (may be truncated)

## Output

Return JSONL only. One JSON object per input test. No markdown, no prose, no code blocks.

Each object must contain these fields:

| Field | Type | Values |
|-------|------|--------|
| schema_version | string | "test-audit-v1" |
| id | string | copy from raw.id exactly |
| declared_area | enum | MessageHandler, MessagePipeline, PipelineStep, UserBanService, ModerationFacade, CommandRouter, MessageService, NotificationService, FakeTelegramClient, DI, Pure, Integration, TestInfrastructure, Unknown |
| actual_seam | enum | same as declared_area + "Multiple" |
| test_intent | string | 1-2 sentence description of what the test tries to verify |
| assertion_type | enum | strong_behavior, state_assertion, fake_client_tracking, envelope_tracking, mock_choreography, does_not_throw, no_meaningful_assertion, compile_smoke, snapshot_or_golden, logs, unknown |
| behavior_value | enum | high, medium, low, unknown |
| maintenance_risk | enum | high, medium, low, unknown |
| bot_owner_value | enum | owner_would_thank_us, owner_would_swear, unclear |
| decision | enum | keep, rewrite, delete, quarantine, unknown |
| replacement_needed | boolean | true if decision=rewrite |
| replacement_seam | string or null | target seam if rewrite |
| reason | string | explanation |
| confidence | enum | high, medium, low |
| needs_human_review | boolean | must be true if confidence=low |

## Decision Guidance

Classify by usefulness as a safety net, not by test count.

- **keep**: high signal, low/acceptable maintenance cost
- **rewrite**: useful behavior, wrong seam or too broad
- **delete**: low behavior value, brittle, duplicate, mock choreography, no meaningful assertion
- **quarantine**: maybe useful but too broad/flaky/expensive; not safe to delete
- **unknown**: not enough information

### Important decision policy

Separate current test quality from production behavior value.

- assertion_type describes current test implementation quality.
- behavior_value describes production behavior / bot contract value.
- A test with weak/no assertions may still point to an important bot behavior.
- Weak assertions often mean rewrite, not delete.
- If the test name or setup points to an owner-visible bot contract, prefer rewrite unless the behavior is clearly low-value, duplicate, or nonsensical.

Before choosing delete, ask:

1. Would the bot owner care if this behavior regressed?
2. Is the current test bad but naming a real bot contract?
3. Is there an obvious seam where the behavior should be re-tested?

If yes to any of those, choose rewrite or quarantine, not delete.

Use delete only when:
- current test is weak/brittle/duplicative, AND
- production behavior appears low-value, duplicate, already covered, or nonsensical, AND
- no replacement is needed.

Use rewrite when:
- current test is weak/brittle/wrong seam, BUT
- the named behavior is a real bot contract worth preserving, OR
- losing this behavior would plausibly matter to the bot owner.

Use quarantine when:
- current test is broad/flaky/expensive, AND
- behavior value is unclear, OR
- replacement seam is not obvious.

If decision is delete:
- reason must explicitly explain why no replacement is needed.

If decision is rewrite:
- replacement_needed must be true.
- replacement_seam must be non-empty.
- reason must explain both:
  1. why the current test is bad;
  2. what behavior/contract should be preserved.

### Strong delete signals

- Assert.Pass after broad arrange/act
- DoesNotThrow without outcome assertion
- test mostly verifies mocks/factories instead of behavior
- MessageHandler test actually tests UserBanService/messaging/moderation/Telegram choreography
- MessageId-sensitive test relies on Message.MessageId instead of MessageEnvelope
- huge legacy factory setup for trivial behavior
- `.Should().NotBeNull()` on something you just created
- `.Should().NotBeEmpty()` on fake client sent messages without verifying content

### ID-sensitive tests

Tests flagged with `message_id.id_sensitive_suspected=true` rely on `Message.MessageId` which is unreliable in test construction. Prefer `rewrite` (to `MessageEnvelope`) or `quarantine`. Only `keep` if the test uses `MessageEnvelope` or the `MessageId` access is incidental (e.g., passing it through stable test helpers without asserting on its value).

### Replacement seam guidance

- MessageHandler: only orchestration/routing smoke
- MessagePipeline/PipelineStep: pipeline behavior
- UserBanService: ban/delete side effects
- CommandRouter/command handlers: commands
- ModerationFacade/policy: moderation decisions
- MessageService/NotificationService: messaging
- FakeTelegramClient/MessageEnvelope: Telegram side effects

## Validation Rules

- decision=rewrite REQUIRES replacement_needed=true and non-empty replacement_seam
- confidence=low REQUIRES needs_human_review=true
- behavior_value=high + decision=delete REQUIRES needs_human_review=true
