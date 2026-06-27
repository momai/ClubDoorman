# Do Not Delete Without Rewrite

Purpose: files where delete candidates are adjacent to rewrite/quarantine-heavy behavior clusters. Deletion should wait until replacement coverage is approved or landed.

## `ClubDoorman.Test/Integration/AiAnalysisTests.cs`

- Delete candidates: `E2E_AI_Analysis_SpecificUserDnekxpb_ShouldDetectSuspiciousProfile`, `E2E_AI_Analysis_VerySuspiciousUser_ShouldDetectHighSpamProbability`, `E2E_AI_Analysis_WithRealPhoto_ShouldDetectHighSpamProbability`.
- Human-review overlap: `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` quarantine, `E2E_AI_Analysis_WithRealApi_ShouldWork` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file has multiple rewrite/quarantine AI E2E behaviors; deleting isolated ignored real-API tests is plausible but should wait for an AI seam replacement plan.

## `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs`

- Delete candidates: `E2E_FakeTelegramClient_ShouldSupportUserBanning`, `E2E_FakeTelegramClient_ShouldTrackCallbackQueries`, `E2E_FakeTelegramClient_ShouldTrackSentMessages`, `E2E_Infrastructure_ShouldSupportAsyncOperations`, `E2E_ModerationResult_ShouldHaveCorrectProperties`, `E2E_TestDataFactory_ShouldGenerateValidData`.
- Human-review overlap: `E2E_FakeTelegramClient_ShouldTrackCallbackQueries`, `E2E_ModerationResult_ShouldHaveCorrectProperties`, `E2E_TestDataFactory_ShouldGenerateValidData`.
- Safe split: mechanically possible, but not recommended before replacement.
- Classification: `requires-rewrite-first`.
- Why: same file still contains rewrite candidates for fake Telegram deletion and moderation flow. This is broad integration coverage, so cleanup should wait for replacement coverage.

## `ClubDoorman.Test/Integration/MessageHandlerBanTests.cs`

- Delete candidates: `WhenModerationReturnsBan_ShouldLogUserBanned`.
- Human-review overlap: `WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file contains high-value ban behavior and multiple rewrite candidates around auto-ban/channel-ban flows.

## `ClubDoorman.Test/Unit/Services/AiChecksTests.cs`

- Delete candidates: `Constructor_WithValidDependencies_CreatesInstance`, `MarkUserOkay_DoesNotThrowException`, `MarkUserOkay_MultipleCalls_DoNotInterfere`.
- Human-review overlap: `Constructor_WithValidDependencies_CreatesInstance`, `GetSpamProbability_WithValidMessage_ReturnsSpamProbability` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file is dominated by rewrite/quarantine AI verdict-provider tests. Avoid partial cleanup until replacement seam is clear.

## `ClubDoorman.Test/Unit/Services/CaptchaServiceExtendedTests.cs`

- Delete candidates: `CreateCaptchaAsync_NullChat_ThrowsArgumentNullException`, `CreateCaptchaAsync_NullUser_ThrowsArgumentNullException`, `GetCaptchaInfo_AfterValidation_ReturnsNull`, `GetCaptchaInfo_NonExistentCaptcha_ReturnsNull`, `RemoveCaptcha_NonExistentCaptcha_ReturnsFalse`, `ValidateCaptchaAsync_EmptyKey_ReturnsFalse`, `ValidateCaptchaAsync_NullKey_ReturnsFalse`.
- Human-review overlap: None remaining.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: low-value no-op/load/key-format slice removed in `8763310`; remaining candidates touch null/invalid-key lifecycle behavior and should be checked against seam coverage before further deletion.

## `ClubDoorman.Test/Unit/Services/ModerationServiceBusinessLogicTests.cs`

- Delete candidates: `CheckMessageAsync_MimicryDetected_ReturnsBanAction`, `CheckUserNameAsync_ValidUsername_ReturnsAllowAction`, `GetSuspiciousUsersStats_EmptyStorage_ReturnsZeroCounts`, `IsUserApproved_UserNotInLists_ReturnsFalse`, `SetAiDetectForSuspiciousUser_ValidUser_ReturnsTrue`.
- Human-review overlap: `CheckMessageAsync_MimicryDetected_ReturnsBanAction`.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file contains high-value moderation decisions and rewrite candidates; do not thin this file before replacement coverage is agreed.
