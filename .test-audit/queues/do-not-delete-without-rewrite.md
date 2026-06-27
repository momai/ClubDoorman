# Do Not Delete Without Rewrite

Purpose: files where delete candidates are adjacent to rewrite/quarantine-heavy behavior clusters. Deletion should wait until replacement coverage is approved or landed.

## `ClubDoorman.Test/Integration/AiAnalysisTests.cs`

- Delete candidates: `E2E_AI_Analysis_SpecificUserDnekxpb_ShouldDetectSuspiciousProfile`, `E2E_AI_Analysis_VerySuspiciousUser_ShouldDetectHighSpamProbability`, `E2E_AI_Analysis_WithRealPhoto_ShouldDetectHighSpamProbability`.
- Human-review overlap: `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` quarantine, `E2E_AI_Analysis_WithRealApi_ShouldWork` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file has multiple rewrite/quarantine AI E2E behaviors; deleting isolated ignored real-API tests is plausible but should wait for an AI seam replacement plan.

## `ClubDoorman.Test/Integration/MessageHandlerBanTests.cs`

- Delete candidates: `WhenModerationReturnsBan_ShouldLogUserBanned`.
- Human-review overlap: `WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file contains high-value ban behavior and multiple rewrite candidates around auto-ban/channel-ban flows.
