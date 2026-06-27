# Human Review Queue

Count: **143**


## `ModerationService_CheckMessageAsync_WithNullMessage_ThrowsArgumentNullException`

- **Full ID**: `ClubDoorman.Test/ErrorHandlingTests.cs::ErrorHandlingTests::ModerationService_CheckMessageAsync_WithNullMessage_ThrowsArgumentNullException`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: The test uses CreateModerationFacade() which returns a fully mocked IModerationFacade pre-configured with .Setup().ThrowsAsync(). The test asserts the mock throws exactly what the mock is configured t

## `ModerationService_CheckUserNameAsync_WithEmptyFirstName_ThrowsModerationException`

- **Full ID**: `ClubDoorman.Test/ErrorHandlingTests.cs::ErrorHandlingTests::ModerationService_CheckUserNameAsync_WithEmptyFirstName_ThrowsModerationException`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Same mock choreography pattern. CreateModerationFacade() returns a fully mocked facade pre-configured with .Setup(It.Is<User>(u => string.IsNullOrEmpty(u.FirstName))).ThrowsAsync(ModerationException).

## `E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis`

- **Full ID**: `ClubDoorman.Test/Integration/AiAnalysisTests.cs::AiAnalysisTests::E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis`
- **Decision**: `quarantine`
- **Confidence**: `medium`
- **Flags**: `quarantine`
- **Reason**: Current test is bad: name says should not trigger analysis but only asserts handler not null and fake bot sent messages. Does not verify AI analysis was skipped. No deduplication behavior is tested. T

## `E2E_AI_Analysis_WithRealApi_ShouldWork`

- **Full ID**: `ClubDoorman.Test/Integration/AiAnalysisTests.cs::AiAnalysisTests::E2E_AI_Analysis_WithRealApi_ShouldWork`
- **Decision**: `quarantine`
- **Confidence**: `medium`
- **Flags**: `quarantine`
- **Reason**: Currently ignored with [Ignore] attribute, so it never runs in CI. Calls real external AI API making it flaky and expensive. Assertions are weak (NotBeNull, BeGreaterThanOrEqualTo(0.0)) and provide no

## `GetAttentionBaitProbability_WithRealPhoto_ShouldAnalyzePhotoInAPI`

- **Full ID**: `ClubDoorman.Test/Integration/AiChecksPhotoLoggingTest.cs::AiChecksPhotoLoggingTest::GetAttentionBaitProbability_WithRealPhoto_ShouldAnalyzePhotoInAPI`
- **Decision**: `quarantine`
- **Confidence**: `medium`
- **Flags**: `quarantine`
- **Reason**: Test makes real external API calls and depends on .env file and local filesystem path (/home/kpblc/.../tmp/big.png), making it extremely fragile and non-portable. Assertions are weak (Not.Null, Length

## `EffectBus_ShouldBeRealEffectBus`

- **Full ID**: `ClubDoorman.Test/Integration/Effects/EffectsConfigurationIntegrationTest.cs::EffectsConfigurationIntegrationTest::EffectBus_ShouldBeRealEffectBus`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration resolves to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage already exists through t

## `EffectsConfiguration_ShouldBeProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/Integration/Effects/EffectsConfigurationIntegrationTest.cs::EffectsConfigurationIntegrationTest::EffectsConfiguration_ShouldBeProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Broad configuration smoke test that asserts multiple config properties from a real service provider. No distinct bot contract exists beyond the general happy path that effects are configured. If confi

## `EffectsConfiguration_ShouldEnableDeleteAndReportActions`

- **Full ID**: `ClubDoorman.Test/Integration/Effects/EffectsConfigurationIntegrationTest.cs::EffectsConfigurationIntegrationTest::EffectsConfiguration_ShouldEnableDeleteAndReportActions`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Duplicate of the broader configuration test in the same class. Only verifies that default config values are set correctly via DI resolution. No distinct production behavior is protected. If config def

## `ModerationEffectsBuilder_ShouldBeRealBuilder`

- **Full ID**: `ClubDoorman.Test/Integration/Effects/EffectsConfigurationIntegrationTest.cs::EffectsConfigurationIntegrationTest::ModerationEffectsBuilder_ShouldBeRealBuilder`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration resolves to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage already exists through t

## `E2E_FakeTelegramClient_ShouldTrackCallbackQueries`

- **Full ID**: `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs::InfrastructureE2ETests::E2E_FakeTelegramClient_ShouldTrackCallbackQueries`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests the fake's internal collection by manually adding an item then asserting it exists. No production behavior or bot contract is exercised. The fake infrastructure is an implementation detail of th

## `E2E_ModerationResult_ShouldHaveCorrectProperties`

- **Full ID**: `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs::InfrastructureE2ETests::E2E_ModerationResult_ShouldHaveCorrectProperties`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests test infrastructure factory methods (TestData.ModerationResults), not production behavior. The factories create test data objects; validating their output is a test helper concern, not a product

## `E2E_TestDataFactory_ShouldGenerateValidData`

- **Full ID**: `ClubDoorman.Test/Integration/InfrastructureE2ETests.cs::InfrastructureE2ETests::E2E_TestDataFactory_ShouldGenerateValidData`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: This test only validates test infrastructure (TestData factories), not production behavior. No bot contract is protected. The factories are test helpers, not production code. Owner would not care if t

## `WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync`

- **Full ID**: `ClubDoorman.Test/Integration/MessageHandlerBanTests.cs::MessageHandlerBanTests::WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync`
- **Decision**: `quarantine`
- **Confidence**: `low`
- **Flags**: `quarantine`, `needs_human_review=true`, `confidence=low`
- **Reason**: Test is currently skipped via [Ignore] attribute. Uses legacy assertion pattern verifying direct bot API calls (BanChatMember, DeleteMessage) instead of going through UserBanService. The AI-confirmed 

## `E2E_CompleteAIAnalysis_ShouldWorkEndToEnd`

- **Full ID**: `ClubDoorman.Test/Integration/SimpleE2ETests.cs::SimpleE2ETests::E2E_CompleteAIAnalysis_ShouldWorkEndToEnd`
- **Decision**: `quarantine`
- **Confidence**: `medium`
- **Flags**: `quarantine`
- **Reason**: Broad E2E test that exercises multiple seams (AI photo analysis via AiChecks, text spam classification via SpamHamClassifier, FakeTelegramClient setup). Uses real_env_or_api, making it potentially fla

## `Builder_ProvidesAccessToDependencies`

- **Full ID**: `ClubDoorman.Test/Integration/UserJoinFacadeIntegrationTests.cs::UserJoinFacadeIntegrationTests::Builder_ProvidesAccessToDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests test infrastructure (builder exposes mock properties), not production behavior. Asserts NotNull on two mock properties of a test builder. No bot contract is protected. If these properties become

## `CheckUserName_WithNormalName_ReturnsAllow`

- **Full ID**: `ClubDoorman.Test/ModerationServiceSimpleTests.cs::ModerationServiceSimpleTests::CheckUserName_WithNormalName_ReturnsAllow`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `duplicate_conflict`
- **Reason**: The test uses FakeModerationService where the result is pre-configured via _fakePolicy.SetResult(). The assertion only verifies the adapter returns what the fake was told to return, not any real polic

## `AiChecksMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::AiChecksMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `BadMessageManagerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::BadMessageManagerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `CaptchaServiceMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::CaptchaServiceMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test identical to BotMock test. Only asserts a mock field is not null. No production behavior is protected.

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `ModerationServiceMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::ModerationServiceMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `StatisticsServiceMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::StatisticsServiceMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `UserManagerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs::CallbackQueryHandlerTestFactoryTests::UserManagerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/CaptchaServiceTestFactoryTests.cs::CaptchaServiceTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `BotMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactoryTests.cs::ChatMemberHandlerTestFactoryTests::BotMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts that a mock field is not null. No production behavior is protected.

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactoryTests.cs::ChatMemberHandlerTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `UserManagerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ChatMemberHandlerTestFactoryTests.cs::ChatMemberHandlerTestFactoryTests::UserManagerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `CreateMessageHandler_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactoryTests.cs::MessageHandlerTestFactoryTests::CreateMessageHandler_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Despite the name claiming to verify all dependencies are configured, the only assertion is Assert.That(instance, Is.Not.Null). This is identical to the previous test and provides no additional coverag

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactoryTests.cs::MessageHandlerTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies object creation. No production behavior is protected. Duplicate pattern of other *_IsProperlyConfigured tests in the same class.

## `ServiceProviderMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MessageHandlerTestFactoryTests.cs::MessageHandlerTestFactoryTests::ServiceProviderMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies object creation. No production behavior is protected. The factory itself is legacy infrastructure.

## `CreateMockAiChecks_WithCustomParameters_ReturnsMockWithCustomValues`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs::MockAiChecksFactoryTests::CreateMockAiChecks_WithCustomParameters_ReturnsMockWithCustomValues`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test on a test helper. Only asserts the returned mock is not null and is the expected type. Does not verify that custom parameters are actually applied to the mock setup. No production b

## `CreateMockAiChecks_WithDefaultParameters_ReturnsMockWithDefaultValues`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs::MockAiChecksFactoryTests::CreateMockAiChecks_WithDefaultParameters_ReturnsMockWithDefaultValues`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test on a test helper. Only asserts the returned mock is not null and is the expected type. Does not verify that default values are actually configured on the mock. No production behavio

## `CreateNormalScenario_ReturnsLowProbabilities`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs::MockAiChecksFactoryTests::CreateNormalScenario_ReturnsLowProbabilities`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test on a test helper. Name suggests it should verify low probability values, but only asserts the returned mock is not null and is the expected type. Does not verify any probability val

## `CreateSpamScenario_ReturnsHighProbabilities`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs::MockAiChecksFactoryTests::CreateSpamScenario_ReturnsHighProbabilities`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test on a test helper. Name suggests it should verify high spam probabilities, but only asserts the returned mock is not null and is the expected type. Does not verify any probability va

## `CreateSuspiciousUserScenario_ReturnsHighSuspiciousProbability`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs::MockAiChecksFactoryTests::CreateSuspiciousUserScenario_ReturnsHighSuspiciousProbability`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test on a test helper. Name suggests it should verify high suspicious probability, but only asserts the returned mock is not null and is the expected type. Does not verify any probabilit

## `GetSuspiciousUserSpamProbabilityWithPhoto_ReturnsCombinedProbability`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs::MockAiChecksTests::GetSuspiciousUserSpamProbabilityWithPhoto_ReturnsCombinedProbability`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests mock factory implementation details. Test name claims combined probability but only returns the configured suspicious probability directly. No production behavior is protected; only verifies tes

## `AiChecksMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::AiChecksMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses ModerationSer

## `BadMessageManagerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::BadMessageManagerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `BotClientMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::BotClientMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses ModerationSer

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses ModerationSer

## `MimicryClassifierMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::MimicryClassifierMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Trivial factory property smoke test. Only asserts a mock field is not null. No production behavior is protected.

## `SuspiciousUsersStorageMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::SuspiciousUsersStorageMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses ModerationSer

## `UserManagerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/ModerationServiceTestFactoryTests.cs::ModerationServiceTestFactoryTests::UserManagerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses ModerationSer

## `CreateSpamHamClassifier_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/SpamHamClassifierTestFactoryTests.cs::SpamHamClassifierTestFactoryTests::CreateSpamHamClassifier_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Despite the name suggesting dependency verification, the test only asserts Is.Not.Null on the created instance. No dependencies are actually checked. Factory smoke test with no production behavior.

## `CreateSpamHamClassifier_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/SpamHamClassifierTestFactoryTests.cs::SpamHamClassifierTestFactoryTests::CreateSpamHamClassifier_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation and type. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses SpamHamClassifierTest

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/SpamHamClassifierTestFactoryTests.cs::SpamHamClassifierTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses SpamHamClassi

## `BotMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactoryTests.cs::StatisticsServiceTestFactoryTests::BotMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses StatisticsSer

## `ChatLinkFormatterMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactoryTests.cs::StatisticsServiceTestFactoryTests::ChatLinkFormatterMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses StatisticsSer

## `CreateStatisticsService_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactoryTests.cs::StatisticsServiceTestFactoryTests::CreateStatisticsService_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses Statisti

## `CreateStatisticsService_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactoryTests.cs::StatisticsServiceTestFactoryTests::CreateStatisticsService_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation and type. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses StatisticsServiceTest

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/StatisticsServiceTestFactoryTests.cs::StatisticsServiceTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses StatisticsSer

## `CreateSuspiciousUsersStorage_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/SuspiciousUsersStorageTestFactoryTests.cs::SuspiciousUsersStorageTestFactoryTests::CreateSuspiciousUsersStorage_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses Suspicio

## `CreateSuspiciousUsersStorage_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/SuspiciousUsersStorageTestFactoryTests.cs::SuspiciousUsersStorageTestFactoryTests::CreateSuspiciousUsersStorage_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by any test th

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/SuspiciousUsersStorageTestFactoryTests.cs::SuspiciousUsersStorageTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses SuspiciousUse

## `CreateTelegramApiException_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/TelegramApiExceptionTestFactoryTests.cs::TelegramApiExceptionTestFactoryTests::CreateTelegramApiException_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses Telegram

## `CreateTelegramApiException_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/TelegramApiExceptionTestFactoryTests.cs::TelegramApiExceptionTestFactoryTests::CreateTelegramApiException_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created exception instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by a

## `CreateTelegramBotClientWrapper_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/TelegramBotClientWrapperTestFactoryTests.cs::TelegramBotClientWrapperTestFactoryTests::CreateTelegramBotClientWrapper_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses Telegram

## `CreateTelegramBotClientWrapper_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/TelegramBotClientWrapperTestFactoryTests.cs::TelegramBotClientWrapperTestFactoryTests::CreateTelegramBotClientWrapper_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by any test th

## `CreateUpdateDispatcher_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UpdateDispatcherTestFactoryTests.cs::UpdateDispatcherTestFactoryTests::CreateUpdateDispatcher_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses UpdateDi

## `CreateUpdateDispatcher_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UpdateDispatcherTestFactoryTests.cs::UpdateDispatcherTestFactoryTests::CreateUpdateDispatcher_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by any test th

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UpdateDispatcherTestFactoryTests.cs::UpdateDispatcherTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses UpdateDispatc

## `UpdateHandlersMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UpdateDispatcherTestFactoryTests.cs::UpdateDispatcherTestFactoryTests::UpdateHandlersMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses UpdateDispatc

## `ApprovedUsersStorageLoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactoryTests.cs::UserCleanupServiceTestFactoryTests::ApprovedUsersStorageLoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses UserCleanupSe

## `CreateRealUserCleanupService_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactoryTests.cs::UserCleanupServiceTestFactoryTests::CreateRealUserCleanupService_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by any test th

## `CreateUserCleanupService_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactoryTests.cs::UserCleanupServiceTestFactoryTests::CreateUserCleanupService_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses UserClea

## `CreateUserCleanupService_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactoryTests.cs::UserCleanupServiceTestFactoryTests::CreateUserCleanupService_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by any test th

## `LoggerMock_IsProperlyConfigured`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserCleanupServiceTestFactoryTests.cs::UserCleanupServiceTestFactoryTests::LoggerMock_IsProperlyConfigured`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that a mock property is not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that actually uses UserCleanupSe

## `CreateUserManagementException_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserManagementExceptionTestFactoryTests.cs::UserManagementExceptionTestFactoryTests::CreateUserManagementException_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created instance is not null. The comment claims to check dependencies but the assertion provides no meaningful coverage. No production behavior is protected.

## `CreateUserManagementException_ReturnsWorkingInstance`

- **Full ID**: `ClubDoorman.Test/TestInfrastructure/UserManagementExceptionTestFactoryTests.cs::UserManagementExceptionTestFactoryTests::CreateUserManagementException_ReturnsWorkingInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies that the created exception instance is not null and is the expected type. No production behavior is protected. The factory's correctness is exercised transitively by a

## `AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: TestKit builder smoke test that only verifies the builder returns a non-null object of the expected interface. No production behavior is protected.

## `CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: TestKit builder smoke test that only verifies the builder returns a non-null object of the expected interface. No production behavior is protected.

## `CreateTestBotClient_ReturnsValidClient`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::CreateTestBotClient_ReturnsValidClient`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test infrastructure smoke test that only verifies a TK helper returns a non-null TelegramBotClient. The test is flagged as using real env or API, which adds fragility without protecting any production

## `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies the builder returns non-null objects. NotNull assertions on mocks you just configured provide no value. No production behavior is protected.

## `MessageHandlerBuilder_WithModerationServiceMock_CreatesHandlerWithCustomModeration`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::MessageHandlerBuilder_WithModerationServiceMock_CreatesHandlerWithCustomModeration`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies object creation. NotNull assertion on something you just created provides no value. The test name claims custom moderation but the body just calls WithStandardMoc

## `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies object creation. NotNull and InstanceOf assertions on something you just created provide no value. No production behavior is protected.

## `MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies object creation. NotNull assertion on something you just created provides no value. The UserManager configuration is part of the arrange, not the assertion. No pr

## `ModerationScenarios_CompleteSetup_WorksCorrectly`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::ModerationScenarios_CompleteSetup_WorksCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test infrastructure smoke test that only verifies a TK helper method returns non-null objects. No production behavior is protected. The TK helper's correctness is exercised transitively by any test th

## `ModerationScenarios_MinimalSetup_WorksCorrectly`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::ModerationScenarios_MinimalSetup_WorksCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test infrastructure smoke test that only verifies a TK helper method returns non-null objects. No production behavior is protected. The TK helper's correctness is exercised transitively by any test th

## `ModerationScenarios_MockedSetup_WorksCorrectly`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::ModerationScenarios_MockedSetup_WorksCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test infrastructure smoke test that verifies a TK helper returns expected mock dictionary keys and count. While slightly more specific than simple null checks, it only tests the test helper itself. No

## `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: TestKit builder smoke test that only verifies the builder returns a non-null object of the expected interface. No production behavior is protected. The builder is test infrastructure, not bot behavior

## `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: TestKit builder smoke test that only verifies the builder returns a non-null object of the expected interface. No production behavior is protected.

## `UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock`

- **Full ID**: `ClubDoorman.Test/TestKit/TestKit.BuilderTests.cs::TestKitBuilderTests::UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: TestKit builder smoke test that only verifies the builder returns a non-null object of the expected interface. No production behavior is protected.

## `DeleteMessageLater_WithNullMessage_NoThrow`

- **Full ID**: `ClubDoorman.Test/Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs::MessageHandlerDeleteMessageLaterTests::DeleteMessageLater_WithNullMessage_NoThrow`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: DoesNotThrow on null input is defensive programming, not a bot contract. Null messages should never reach this method in production. The general null-safety of the method is an implementation detail. 

## `LeftMemberCleanup_EmitsSemanticRule`

- **Full ID**: `ClubDoorman.Test/Unit/Handlers/MessageHandlerSemanticsTests.cs::MessageHandlerSemanticsTests::LeftMemberCleanup_EmitsSemanticRule`
- **Decision**: `rewrite`
- **Confidence**: `medium`
- **Flags**: `duplicate_conflict`
- **Reason**: Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, requires BotId setup on the Telegram wrapper mock, then asserts on temp filesystem output. The a

## `TelegramBotClientWrapper_GetChatFullInfo_CopiesPhotoProperty`

- **Full ID**: `ClubDoorman.Test/Unit/Infrastructure/TelegramBotClientWrapperTests.cs::TelegramBotClientWrapperTests::TelegramBotClientWrapper_GetChatFullInfo_CopiesPhotoProperty`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test cannot actually call the wrapper method (no mocked SDK client) and falls back to inspecting source code comments. Creates a real TelegramBotClient with a fake token. The assertion only checks tha

## `CreateModerationService_WithFactory_ReturnsWorkingService`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/ModerationServiceTests.cs::ModerationServiceTests::CreateModerationService_WithFactory_ReturnsWorkingService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies the factory creates a non-null service of the expected type. No production behavior is protected. The factory's correctness is exercised transitively by any test 

## `ModerationTestFactory_ConfiguresAllDependencies`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/ModerationServiceTests.cs::ModerationServiceTests::ModerationTestFactory_ConfiguresAllDependencies`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies eight mock properties on the test factory are not null. No production behavior is protected. The factory's correctness is exercised transitively by any test that 

## `AddHam_EmptyMessage_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddHam_EmptyMessage_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddHam on a mock object, not the real classifier. Empty message edge case for a feedback API is low-value. No production behavior is protected.

## `AddHam_LongMessage_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddHam_LongMessage_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddHam on a mock object, not the real classifier. Long message edge case for a feedback API is low-value. No production behavior is protected.

## `AddHam_SpecialCharacters_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddHam_SpecialCharacters_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddHam on a mock object, not the real classifier. Special characters in a feedback API are low-value. No production behavior is protected.

## `AddSpam_ConcurrentCalls_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddSpam_ConcurrentCalls_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddSpam 5 times concurrently on a mock object, so no real concurrency behavior is tested. Concurrency safety of a mock is meaningless. If thread safety of

## `AddSpam_EmptyMessage_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddSpam_EmptyMessage_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddSpam on a mock object, so no real empty-input handling is tested. Empty message edge case is low-value for a feedback/training API. No production behav

## `AddSpam_LongMessage_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddSpam_LongMessage_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddSpam on a mock object, so no real long-message handling is tested. Long message edge case for a feedback API is low-value. No production behavior is pr

## `AddSpam_SpecialCharacters_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::AddSpam_SpecialCharacters_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Assert.Pass provides zero verification. Calls AddSpam on a mock object, so no real special-character handling is tested. Special characters in a feedback/training API are low-value. No production beha

## `IsSpam_ConcurrentCalls_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::IsSpam_ConcurrentCalls_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test fires 10 concurrent calls against a mock that is set up to return (false, 0.2f). The mock handles concurrency trivially. No real classifier concurrency behavior is tested. The assertion only chec

## `IsSpam_EmptyMessage_ReturnsNotSpam`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::IsSpam_EmptyMessage_ReturnsNotSpam`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test exercises a mock of ISpamHamClassifier, not the real classifier. The mock is configured to return (false, 0.1f) and the test asserts those exact configured values back. No production behavior is 

## `IsSpam_NullText_ThrowsNullReferenceException`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::IsSpam_NullText_ThrowsNullReferenceException`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test configures a mock to throw NullReferenceException and then asserts it throws. No real classifier code is exercised. Additionally, NullReferenceException is not a meaningful production contract to

## `IsSpam_SpamMessage_ReturnsSpam`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::IsSpam_SpamMessage_ReturnsSpam`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests a mock object against itself: sets up _classifierMock to return (true, 0.8f), then calls _classifierMock.Object and asserts it returned (true, 0.8f). No production code is exercised. The actual 

## `IsSpam_ValidMessage_ReturnsNotSpam`

- **Full ID**: `ClubDoorman.Test/Unit/Moderation/SpamHamClassifierTests.cs::SpamHamClassifierTests::IsSpam_ValidMessage_ReturnsNotSpam`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests a mock object against itself: sets up _classifierMock to return (false, 0.2f), then calls _classifierMock.Object and asserts it returned (false, 0.2f). No production code is exercised. The actua

## `AddAIServices_ShouldRegisterIAiChecks`

- **Full ID**: `ClubDoorman.Test/Unit/Services/AIModuleTests.cs::AIModuleTests::AddAIServices_ShouldRegisterIAiChecks`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies DI registration. No production behavior is protected. Compile-time/service registration coverage already exists through the application startup path.

## `AddAIServices_ShouldRegisterIMimicryClassifier`

- **Full ID**: `ClubDoorman.Test/Unit/Services/AIModuleTests.cs::AIModuleTests::AddAIServices_ShouldRegisterIMimicryClassifier`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies DI registration. No production behavior is protected. Compile-time/service registration coverage already exists through the application startup path.

## `AddAIServices_ShouldRegisterISpamHamClassifier`

- **Full ID**: `ClubDoorman.Test/Unit/Services/AIModuleTests.cs::AIModuleTests::AddAIServices_ShouldRegisterISpamHamClassifier`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies DI registration. No production behavior is protected. Compile-time/service registration coverage already exists through the application startup path.

## `AddAIServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/AIModuleTests.cs::AIModuleTests::AddAIServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies the extension method returns the same ServiceCollection instance. This is trivial builder-pattern behavior. Service registration coverage already exists in the sa

## `Constructor_WithValidDependencies_CreatesInstance`

- **Full ID**: `ClubDoorman.Test/Unit/Services/AiChecksTests.cs::AiChecksTests::Constructor_WithValidDependencies_CreatesInstance`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Constructor smoke test only verifies object creation. No production behavior is protected. The Not.Null + InstanceOf assertions are trivial for a constructor that cannot fail with valid inputs. Flagge

## `GetSpamProbability_WithValidMessage_ReturnsSpamProbability`

- **Full ID**: `ClubDoorman.Test/Unit/Services/AiChecksTests.cs::AiChecksTests::GetSpamProbability_WithValidMessage_ReturnsSpamProbability`
- **Decision**: `quarantine`
- **Confidence**: `medium`
- **Flags**: `quarantine`
- **Reason**: The range assertion (0.0 to 1.0) is a meaningful contract check on the spam probability output. However, the test is flagged with real_env_or_api smell, meaning it may make real external calls through

## `AddCaptchaServices_ShouldRegisterCaptchaServiceAsICaptchaService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/CaptchaModuleTests.cs::CaptchaModuleTests::AddCaptchaServices_ShouldRegisterCaptchaServiceAsICaptchaService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies DI registration. No production behavior is protected. Compile-time and service registration coverage already exists elsewhere. The InstanceOf assertion is a thin wrapp

## `AddCaptchaServices_ShouldRegisterICaptchaService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/CaptchaModuleTests.cs::CaptchaModuleTests::AddCaptchaServices_ShouldRegisterICaptchaService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation and DI registration. No production behavior is protected. DI registration correctness is already covered by compile-time checks and broader integration

## `AddCaptchaServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/CaptchaModuleTests.cs::CaptchaModuleTests::AddCaptchaServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: No distinct bot contract exists beyond verifying the extension method returns IServiceCollection for fluent builder chaining. This is a trivial convention of Microsoft.Extensions.DependencyInjection e

## `CreateCaptchaAsync_CancellationToken_RespectsCancellation`

- **Full ID**: `ClubDoorman.Test/Unit/Services/CaptchaServiceExtendedTests.cs::CaptchaServiceExtendedTests::CreateCaptchaAsync_CancellationToken_RespectsCancellation`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Test creates a cancelled CancellationTokenSource but never passes it to the method under test. The test's own comment confirms CreateCaptchaAsync does not accept CancellationToken. DoesNotThrow assert

## `GenerateKey_ValidParameters_ReturnsExpectedKey`

- **Full ID**: `ClubDoorman.Test/Unit/Services/CaptchaServiceExtendedTests.cs::CaptchaServiceExtendedTests::GenerateKey_ValidParameters_ReturnsExpectedKey`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests internal implementation detail (string format of key). The key format is an internal concern; the relevant contract is that the same inputs produce the same key and different inputs produce diff

## `AddCommandsServices_RegistersExpectedDescriptors`

- **Full ID**: `ClubDoorman.Test/Unit/Services/CommandsModuleTests.cs::CommandsModuleTests::AddCommandsServices_RegistersExpectedDescriptors`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: DI registration smoke test. Verifies service descriptors exist after calling the module method. No distinct bot contract beyond compile-time registration coverage. If a handler is removed or renamed, 

## `AddConfigurationServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/ConfigurationModuleTests.cs::ConfigurationModuleTests::AddConfigurationServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests the .NET fluent extension method convention (returning the same ServiceCollection). This is a framework pattern, not a bot contract. The sibling test AddConfigurationServices_ShouldRegisterIAppC

## `AddMessagingServices_ShouldRegisterIChatLinkFormatter`

- **Full ID**: `ClubDoorman.Test/Unit/Services/MessagingModuleTests.cs::MessagingModuleTests::AddMessagingServices_ShouldRegisterIChatLinkFormatter`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation via Not.Null; no production behavior is protected. Compile-time/service registration coverage already exists elsewhere.

## `AddMessagingServices_ShouldRegisterILogChatService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/MessagingModuleTests.cs::MessagingModuleTests::AddMessagingServices_ShouldRegisterILogChatService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation via Not.Null; no production behavior is protected. Compile-time/service registration coverage already exists elsewhere.

## `AddMessagingServices_ShouldRegisterIMessageService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/MessagingModuleTests.cs::MessagingModuleTests::AddMessagingServices_ShouldRegisterIMessageService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation via Not.Null; no production behavior is protected. Compile-time/service registration coverage already exists elsewhere. If AddMessagingServices stops r

## `AddMessagingServices_ShouldRegisterINotificationService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/MessagingModuleTests.cs::MessagingModuleTests::AddMessagingServices_ShouldRegisterINotificationService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies object creation via Not.Null; no production behavior is protected. Compile-time/service registration coverage already exists elsewhere.

## `AddMessagingServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/MessagingModuleTests.cs::MessagingModuleTests::AddMessagingServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Fluent API return value check has no production behavior value. The real contract is that services are registered (covered by other tests in the same class). No replacement needed.

## `CheckMessageAsync_MimicryDetected_ReturnsBanAction`

- **Full ID**: `ClubDoorman.Test/Unit/Services/ModerationServiceBusinessLogicTests.cs::ModerationServiceBusinessLogicTests::CheckMessageAsync_MimicryDetected_ReturnsBanAction`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `duplicate_conflict`
- **Reason**: Test name is completely misleading: asserts Allow, not Ban. Author comment explicitly says the test is incorrect and should be removed. Mimicry detection is not part of CheckMessageAsync; it belongs t

## `AddStatisticsServices_ShouldRegisterGlobalStatsManager`

- **Full ID**: `ClubDoorman.Test/Unit/Services/StatisticsModuleTests.cs::StatisticsModuleTests::AddStatisticsServices_ShouldRegisterGlobalStatsManager`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists. No production behavior is protected. Compile-time/service registration coverage already exists through the application buildi

## `AddStatisticsServices_ShouldRegisterIStatisticsService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/StatisticsModuleTests.cs::StatisticsModuleTests::AddStatisticsServices_ShouldRegisterIStatisticsService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists and points to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage alre

## `AddStatisticsServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/StatisticsModuleTests.cs::StatisticsModuleTests::AddStatisticsServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies the fluent API return convention. No production behavior is protected. The actual service registration is tested by sibling tests (ShouldRegisterIStatisticsService, Sh

## `AddTelegramServices_ShouldRegisterITelegramBotClientWrapper`

- **Full ID**: `ClubDoorman.Test/Unit/Services/TelegramModuleTests.cs::TelegramModuleTests::AddTelegramServices_ShouldRegisterITelegramBotClientWrapper`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists and points to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage alre

## `AddTelegramServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/TelegramModuleTests.cs::TelegramModuleTests::AddTelegramServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test only verifies the fluent API return convention. No production behavior is protected. The actual service registration is tested by sibling test (ShouldRegisterITelegramBotClientWrapp

## `BanUserForLongName_Integration_WithRealDependencies`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs::UserBanServiceTestsModern::BanUserForLongName_Integration_WithRealDependencies`
- **Decision**: `quarantine`
- **Confidence**: `medium`
- **Flags**: `quarantine`
- **Reason**: Claims to be an integration test with real dependencies but is in a Unit test file and still uses extensive mocking. The behavior (ban for long name) is already well-covered by the two preceding unit 

## `BanUserForLongName_ValidChat_BansUserAndSendsNotification`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs::UserBanServiceTestsModern::BanUserForLongName_ValidChat_BansUserAndSendsNotification`
- **Decision**: `keep`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Strong contract test at the correct seam. Four targeted mock verifies covering ban execution (with correct ban expiry check), message deletion, notification forwarding, and logging. Uses stable test d

## `BanUserForLongName_ValidChat_BansUserAndSendsNotification`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserBanServiceTests.cs::UserBanServiceTests::BanUserForLongName_ValidChat_BansUserAndSendsNotification`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `needs_human_review=true`, `anomaly_flagged`, `high_value_delete`
- **Reason**: Exact duplicate of the same test in UserBanServiceTests.Modern.cs (same method name, same assertions, same behavior). The legacy file UserBanServiceTests.cs is being superseded by the .Modern variant.

## `RemoveUserFromGroupApproval_WhenExceptionOccurs_ReturnsFalseAndLogsError`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserCleanupServiceTests.cs::UserCleanupServiceTests::RemoveUserFromGroupApproval_WhenExceptionOccurs_ReturnsFalseAndLogsError`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Same pattern as the 'WhenExceptionOccurs' test for global approval. Source comment admits exception setup is skipped. No exception configured on any mock. No logging verification. Only asserts return 

## `AddUserManagementServices_ShouldRegisterApprovedUsersStorage`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs::UserManagementModuleTests::AddUserManagementServices_ShouldRegisterApprovedUsersStorage`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists. No production behavior is protected. Compile-time/service registration coverage already exists through the application buildi

## `AddUserManagementServices_ShouldRegisterIUserCleanupService`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs::UserManagementModuleTests::AddUserManagementServices_ShouldRegisterIUserCleanupService`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists and points to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage alre

## `AddUserManagementServices_ShouldRegisterIUserManager`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs::UserManagementModuleTests::AddUserManagementServices_ShouldRegisterIUserManager`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists and points to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage alre

## `AddUserManagementServices_ShouldRegisterUserIndex`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs::UserManagementModuleTests::AddUserManagementServices_ShouldRegisterUserIndex`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Factory smoke test that only verifies a DI registration descriptor exists and points to the expected concrete type. No production behavior is protected. Compile-time/service registration coverage alre

## `AddUserManagementServices_ShouldReturnServiceCollection`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs::UserManagementModuleTests::AddUserManagementServices_ShouldReturnServiceCollection`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Only verifies fluent API return value pattern. No distinct bot contract exists beyond compile-time coverage. The extension method returning the collection is a standard .NET pattern with no production

## `Approved_ValidUserId_ReturnsApprovalStatus`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::Approved_ValidUserId_ReturnsApprovalStatus`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Tests a mock interface against itself with no production implementation under test. The mock is configured to return true and then verified to return true. No meaningful bot contract is protected. Fac

## `ConcurrentOperations_HandleCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::ConcurrentOperations_HandleCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Mock choreography with fake concurrency: creates 10 tasks calling mock methods, verifies call counts. A mock cannot exercise real thread-safety or concurrency concerns in UserManager. The test only pr

## `GetClubUsername_NegativeUserId_ReturnsUsername`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::GetClubUsername_NegativeUserId_ReturnsUsername`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Pure mock choreography: the test sets up a mock to return null, calls the mock, and asserts null. No production logic is exercised. Negative user IDs do not exist in Telegram. Factory smoke test only 

## `GetClubUsername_ZeroUserId_ReturnsUsername`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::GetClubUsername_ZeroUserId_ReturnsUsername`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Pure mock choreography: the test sets up a mock to return null, calls the mock, and asserts null. No production logic is exercised. Zero user ID is a fabricated edge case with no real Telegram equival

## `InBanlist_MaxLongUserId_ReturnsBanlistStatus`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::InBanlist_MaxLongUserId_ReturnsBanlistStatus`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Pure mock choreography: the test sets up a mock to return false, calls the mock, and asserts false. No production logic is exercised. The long.MaxValue edge case is nonsensical for Telegram user IDs. 

## `InBanlist_ValidUserId_ReturnsBanlistStatus`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::InBanlist_ValidUserId_ReturnsBanlistStatus`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Pure mock choreography: the test mocks IUserManager.InBanlist to return true, then asserts true. The actual UserManager implementation is never exercised. No production behavior is protected. If banli

## `LargeUserId_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::LargeUserId_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Mock choreography with an edge-case input: uses a large user ID with mock methods that return predetermined values. The mock does not exercise any real UserManager logic that could fail on large IDs. 

## `MultipleApprovals_SameUser_HandlesCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::MultipleApprovals_SameUser_HandlesCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Mock choreography: calls Approve twice on a mock, verifies Times.Exactly(2). The mock cannot simulate real idempotency or state management behavior. No production UserManager logic is exercised. Testi

## `MultipleUsers_ConcurrentOperations_HandleCorrectly`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::MultipleUsers_ConcurrentOperations_HandleCorrectly`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Mock choreography with fake concurrency across multiple users: sets up mock for 5 user IDs, creates 10 tasks, verifies per-user call counts. A mock cannot exercise real thread-safety or multi-user sta

## `RefreshBanlist_ExecutesSuccessfully`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::RefreshBanlist_ExecutesSuccessfully`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Pure mock choreography: the test sets up a mock to return Task.CompletedTask, calls the mock, and verifies the call. No production logic is exercised. The only assertion is mock verification. Factory 

## `RemoveApproval_WithGroupId_ReturnsRemovalResult`

- **Full ID**: `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs::UserManagerExtendedTests::RemoveApproval_WithGroupId_ReturnsRemovalResult`
- **Decision**: `delete`
- **Confidence**: `high`
- **Flags**: `anomaly_flagged`
- **Reason**: Pure mock choreography: the test mocks IUserManager.RemoveApproval with a group ID to return true, then asserts true. The actual UserManager implementation is never exercised. No production behavior i
