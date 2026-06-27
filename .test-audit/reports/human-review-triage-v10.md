# Human Review Triage V10

## Scope

Analyzed remaining delete candidates skipped because their files overlap with human-review items.

Inputs:

- `.test-audit/queues/delete-by-file.md`
- `.test-audit/queues/delete-obvious.md`
- `.test-audit/queues/human-review.md`
- `.test-audit/audit-normalized/current-audit.jsonl`
- `.test-audit/reports/test-cleanup-run-state.md`

No production code, tests, audit JSONL, or existing queues were modified during triage.

## Methodology

Compared remaining delete candidates from `delete-by-file.md` and `delete-obvious.md` with human-review entries from `human-review.md`. Cross-checked normalized decisions in `current-audit.jsonl`. Classified files conservatively: rewrite/quarantine/high-value overlaps block automatic deletion unless delete items are clearly separable and low-value.

## Summary

- `safe-delete-next`: 29 files
- `requires-rewrite-first`: 7 files
- `manual-review-required`: 2 files
- `keep-for-now`: 2 files

## Safe Delete Next

### `ClubDoorman.Test/ErrorHandlingTests.cs`

- Delete candidates: 9.
- Human-review overlap: `ModerationService_CheckMessageAsync_WithNullMessage_ThrowsArgumentNullException`, `ModerationService_CheckUserNameAsync_WithEmptyFirstName_ThrowsModerationException`.
- Safe split: yes.
- Classification: `safe-delete-next`.
- Why: delete candidates are mock-choreography or no-production-code assertions. Keep/rewrite tests are separate and must remain.

### `ClubDoorman.Test/Integration/Effects/EffectsConfigurationIntegrationTest.cs`

- Delete candidates: `EffectBus_ShouldBeRealEffectBus`, `EffectsConfiguration_ShouldBeProperlyConfigured`, `EffectsConfiguration_ShouldEnableDeleteAndReportActions`, `ModerationEffectsBuilder_ShouldBeRealBuilder`.
- Human-review overlap: same 4 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: audited test methods are delete-only DI/config smoke tests.

### `ClubDoorman.Test/ModerationServiceSimpleTests.cs`

- Delete candidates: 5.
- Human-review overlap: `CheckUserName_WithNormalName_ReturnsAllow`.
- Safe split: yes.
- Classification: `safe-delete-next`.
- Why: delete candidates are fake-result tautologies or low-value null guard. One keep test remains separate.

### `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs`

- Delete candidates: 11.
- Human-review overlap: 7 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/CaptchaServiceTestFactoryTests.cs`

- Delete candidates: 5.
- Human-review overlap: `LoggerMock_IsProperlyConfigured`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactoryTests.cs`

- Delete candidates: 6.
- Human-review overlap: `BotMock_IsProperlyConfigured`, `LoggerMock_IsProperlyConfigured`, `UserManagerMock_IsProperlyConfigured`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactoryTests.cs`

- Delete candidates: 13.
- Human-review overlap: `CreateMessageHandler_ConfiguresAllDependencies`, `LoggerMock_IsProperlyConfigured`, `ServiceProviderMock_IsProperlyConfigured`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs`

- Delete candidates: 15.
- Human-review overlap: 6 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only test-helper/mock self-tests.

### `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs`

- Delete candidates: 11.
- Human-review overlap: 7 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/SpamHamClassifierTestFactoryTests.cs`

- Delete candidates: 4.
- Human-review overlap: `CreateSpamHamClassifier_ConfiguresAllDependencies`, `CreateSpamHamClassifier_ReturnsWorkingInstance`, `LoggerMock_IsProperlyConfigured`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactoryTests.cs`

- Delete candidates: 6.
- Human-review overlap: 5 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/SuspiciousUsersStorageTestFactoryTests.cs`

- Delete candidates: 4.
- Human-review overlap: `CreateSuspiciousUsersStorage_ConfiguresAllDependencies`, `CreateSuspiciousUsersStorage_ReturnsWorkingInstance`, `LoggerMock_IsProperlyConfigured`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/TelegramApiExceptionTestFactoryTests.cs`

- Delete candidates: 3.
- Human-review overlap: `CreateTelegramApiException_ConfiguresAllDependencies`, `CreateTelegramApiException_ReturnsWorkingInstance`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only exception factory smoke tests.

### `ClubDoorman.Test/TestInfrastructure/TelegramBotClientWrapperTestFactoryTests.cs`

- Delete candidates: 3.
- Human-review overlap: `CreateTelegramBotClientWrapper_ConfiguresAllDependencies`, `CreateTelegramBotClientWrapper_ReturnsWorkingInstance`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only wrapper factory smoke tests.

### `ClubDoorman.Test/TestInfrastructure/UpdateDispatcherTestFactoryTests.cs`

- Delete candidates: 5.
- Human-review overlap: 4 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactoryTests.cs`

- Delete candidates: 6.
- Human-review overlap: 5 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only factory/mock smoke tests.

### `ClubDoorman.Test/TestInfrastructure/UserManagementExceptionTestFactoryTests.cs`

- Delete candidates: 3.
- Human-review overlap: `CreateUserManagementException_ConfiguresAllDependencies`, `CreateUserManagementException_ReturnsWorkingInstance`.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only exception factory smoke tests.

### `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs`

- Delete candidates: 16.
- Human-review overlap: 13 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: file is delete-only TestKit/builder smoke tests.

### `ClubDoorman.Test/Unit/Infrastructure/TelegramBotClientWrapperTests.cs`

- Delete candidates: 3.
- Human-review overlap: `TelegramBotClientWrapper_GetChatFullInfo_CopiesPhotoProperty`.
- Safe split: yes.
- Classification: `safe-delete-next`.
- Why: delete candidates are constructor smoke/comment-inspection tests. Keep tests are separate wrapper contracts.

### `ClubDoorman.Test/Unit/Moderation/ModerationServiceTests.cs`

- Delete candidates: 3.
- Human-review overlap: `CreateModerationService_WithFactory_ReturnsWorkingService`, `ModerationTestFactory_ConfiguresAllDependencies`.
- Safe split: yes.
- Classification: `safe-delete-next`.
- Why: delete candidates are factory smoke tests, separate from high-value moderation behavior tests.

### `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs`

- Delete candidates: 18.
- Human-review overlap: 12 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: audited tests are delete-only mock-self-tests.

### `ClubDoorman.Test/Unit/Services/AIModuleTests.cs`

- Delete candidates: 4.
- Human-review overlap: same 4 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: delete-only DI registration smoke tests.

### `ClubDoorman.Test/Unit/Services/CaptchaModuleTests.cs`

- Delete candidates: 3.
- Human-review overlap: same 3 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: delete-only DI registration smoke tests.

### `ClubDoorman.Test/Unit/Services/CommandsModuleTests.cs`

- Delete candidates: `AddCommandsServices_RegistersExpectedDescriptors`.
- Human-review overlap: same item delete/anomaly.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: delete-only DI descriptor smoke test.

### `ClubDoorman.Test/Unit/Services/StatisticsModuleTests.cs`

- Delete candidates: 3.
- Human-review overlap: same 3 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: delete-only DI registration smoke tests.

### `ClubDoorman.Test/Unit/Services/TelegramModuleTests.cs`

- Delete candidates: 2.
- Human-review overlap: same 2 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: delete-only DI registration smoke tests.

### `ClubDoorman.Test/Unit/Services/UserCleanupServiceTests.cs`

- Delete candidates: `RemoveUserFromAllApprovals_WhenExceptionOccurs_ReturnsFalseAndLogsError`, `RemoveUserFromGroupApproval_WhenExceptionOccurs_ReturnsFalseAndLogsError`.
- Human-review overlap: `RemoveUserFromGroupApproval_WhenExceptionOccurs_ReturnsFalseAndLogsError`.
- Safe split: yes.
- Classification: `safe-delete-next`.
- Why: delete candidates are low-value exception/logging tests; keep tests cover actual approval removal behavior.

### `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs`

- Delete candidates: 5.
- Human-review overlap: same 5 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: delete-only DI registration smoke tests.

### `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs`

- Delete candidates: 36.
- Human-review overlap: 11 delete/anomaly items.
- Safe split: whole-file delete after approval.
- Classification: `safe-delete-next`.
- Why: audited tests are delete-only edge/default/user-manager smoke tests; no keep/rewrite/quarantine/high-value audited tests remain.

## Requires Rewrite First

### `ClubDoorman.Test/Integration/AiAnalysisTests.cs`

- Delete candidates: `E2E_AI_Analysis_SpecificUserDnekxpb_ShouldDetectSuspiciousProfile`, `E2E_AI_Analysis_VerySuspiciousUser_ShouldDetectHighSpamProbability`, `E2E_AI_Analysis_WithRealPhoto_ShouldDetectHighSpamProbability`.
- Human-review overlap: `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` quarantine, `E2E_AI_Analysis_WithRealApi_ShouldWork` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file has multiple rewrite/quarantine AI E2E behaviors; deletion should wait for AI seam replacement coverage.

### `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs`

- Delete candidates: `E2E_FakeTelegramClient_ShouldSupportUserBanning`, `E2E_FakeTelegramClient_ShouldTrackCallbackQueries`, `E2E_FakeTelegramClient_ShouldTrackSentMessages`, `E2E_Infrastructure_ShouldSupportAsyncOperations`, `E2E_ModerationResult_ShouldHaveCorrectProperties`, `E2E_TestDataFactory_ShouldGenerateValidData`.
- Human-review overlap: `E2E_FakeTelegramClient_ShouldTrackCallbackQueries`, `E2E_ModerationResult_ShouldHaveCorrectProperties`, `E2E_TestDataFactory_ShouldGenerateValidData`.
- Safe split: mechanically possible, but not recommended before replacement.
- Classification: `requires-rewrite-first`.
- Why: broad integration coverage with fake Telegram and moderation-flow rewrite candidates.

### `ClubDoorman.Test/Integration/MessageHandlerBanTests.cs`

- Delete candidates: `WhenModerationReturnsBan_ShouldLogUserBanned`.
- Human-review overlap: `WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file contains high-value ban behavior and rewrite candidates around auto-ban/channel-ban flows.

### `ClubDoorman.Test/Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs`

- Delete candidates: `DeleteMessageLater_WithDefaultTimeout_UsesFiveMinutes`, `DeleteMessageLater_WithNegativeTimeout_NoThrow`, `DeleteMessageLater_WithNullMessage_NoThrow`, `DeleteMessageLater_WithZeroTimeout_NoThrow`.
- Human-review overlap: `DeleteMessageLater_WithNullMessage_NoThrow`.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: delete candidates are in the same local behavior cluster as high-value scheduling/cancellation/delete-failure tests.

### `ClubDoorman.Test/Unit/Services/AiChecksTests.cs`

- Delete candidates: `Constructor_WithValidDependencies_CreatesInstance`, `MarkUserOkay_DoesNotThrowException`, `MarkUserOkay_MultipleCalls_DoNotInterfere`.
- Human-review overlap: `Constructor_WithValidDependencies_CreatesInstance`, `GetSpamProbability_WithValidMessage_ReturnsSpamProbability` quarantine.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file is dominated by rewrite/quarantine AI verdict-provider tests.

### `ClubDoorman.Test/Unit/Services/CaptchaServiceExtendedTests.cs`

- Delete candidates: `BanExpiredCaptchaUsersAsync_NoExpiredCaptchas_CompletesSuccessfully`, `CreateCaptchaAsync_CancellationToken_RespectsCancellation`, `CreateCaptchaAsync_LargeBatch_HandlesCorrectly`, `CreateCaptchaAsync_NullChat_ThrowsArgumentNullException`, `CreateCaptchaAsync_NullUser_ThrowsArgumentNullException`, `GenerateKey_ValidParameters_ReturnsExpectedKey`, `GetCaptchaInfo_AfterValidation_ReturnsNull`, `GetCaptchaInfo_NonExistentCaptcha_ReturnsNull`, `RemoveCaptcha_NonExistentCaptcha_ReturnsFalse`, `ValidateCaptchaAsync_EmptyKey_ReturnsFalse`, `ValidateCaptchaAsync_LargeBatch_HandlesCorrectly`, `ValidateCaptchaAsync_NullKey_ReturnsFalse`.
- Human-review overlap: `CreateCaptchaAsync_CancellationToken_RespectsCancellation`, `GenerateKey_ValidParameters_ReturnsExpectedKey`.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: many keep/rewrite captcha-flow tests remain; deletion touches the same captcha lifecycle/key/validation behavior area.

### `ClubDoorman.Test/Unit/Services/ModerationServiceBusinessLogicTests.cs`

- Delete candidates: `CheckMessageAsync_MimicryDetected_ReturnsBanAction`, `CheckUserNameAsync_ValidUsername_ReturnsAllowAction`, `GetSuspiciousUsersStats_EmptyStorage_ReturnsZeroCounts`, `IsUserApproved_UserNotInLists_ReturnsFalse`, `SetAiDetectForSuspiciousUser_ValidUser_ReturnsTrue`.
- Human-review overlap: `CheckMessageAsync_MimicryDetected_ReturnsBanAction`.
- Safe split: no.
- Classification: `requires-rewrite-first`.
- Why: file contains high-value moderation decisions and rewrite candidates; do not thin this file before replacement coverage is agreed.

## Manual Review Required

### `ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs`

- Delete candidates: `BanUserForLongName_EdgeCases_MassiveDataGeneration`.
- Human-review overlap: `BanUserForLongName_Integration_WithRealDependencies` quarantine, `BanUserForLongName_ValidChat_BansUserAndSendsNotification` keep.
- Safe split: no.
- Classification: `manual-review-required`.
- Why: same file contains keep/quarantine/high-value ban behavior. Human must decide target coverage before cleanup.

### `ClubDoorman.Test/Unit/Services/UserBanServiceTests.cs`

- Delete candidates: `BanUserForLongName_ExceptionOccurs_LogsWarning`, `BanUserForLongName_PrivateChat_LogsWarningAndReturns_Obsolete`, `BanUserForLongName_ValidChat_BansUserAndSendsNotification`, `BanUserForLongName_ValidChat_BansUserAndSendsNotification_Obsolete`.
- Human-review overlap: `BanUserForLongName_ValidChat_BansUserAndSendsNotification` delete with `high_value_delete`.
- Safe split: no.
- Classification: `manual-review-required`.
- Why: high-value delete conflict on user-ban side effects. Requires explicit human decision.

## Keep For Now

### `ClubDoorman.Test/Unit/Services/ConfigurationModuleTests.cs`

- Delete candidates: `AddConfigurationServices_ShouldReturnServiceCollection`.
- Human-review overlap: same delete/anomaly item.
- Safe split: yes, but not worth doing now.
- Classification: `keep-for-now`.
- Why: one keep DI registration test remains. Low-value deletion is too small to justify splitting this file now.

### `ClubDoorman.Test/Unit/Services/MessagingModuleTests.cs`

- Delete candidates: `AddMessagingServices_ShouldRegisterIChatLinkFormatter`, `AddMessagingServices_ShouldRegisterILogChatService`, `AddMessagingServices_ShouldRegisterIMessageService`, `AddMessagingServices_ShouldRegisterINotificationService`, `AddMessagingServices_ShouldReturnServiceCollection`.
- Human-review overlap: same 5 delete/anomaly items.
- Safe split: yes, but policy-dependent.
- Classification: `keep-for-now`.
- Why: mixed DI registration file has keep tests for other registrations. Preserve for now unless module-test policy is decided.

## Run-State Accounting Fixes

Applied obvious accounting fixes to `.test-audit/reports/test-cleanup-run-state.md`:

- Commit count corrected from 4 to 5 because five SHA values are listed.
- Reworded deleted-test accounting so runner result counts are not presented as method deletion counts.

## Stop Point

Triage only. No test deletion or rewrite was performed.
