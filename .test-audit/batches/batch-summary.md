# Batch Summary

- **Total raw records read:** 847
- **Total batches created:** 107
- **Total records batched:** 847
- **Unique IDs batched:** 847
- **Duplicate IDs:** 0 (validated before batching)
- **Average tests per batch:** 7.9
- **Batch size range:** 1-10
- **Total source excerpt chars:** 1,719,183

## Batches

| Batch | Tests | Source Chars | Top Files | Top Smells |
|-------|-------|-------------|-----------|------------|
| batch-0001.json | 7 | 16,627 | Integration/AiAnalysisTests.cs, Unit/Handlers/MessageHandlerFakeTests.cs | does_not_throw, fake_services_factory, message_handler_test_factory, mock_verify, real_env_or_api |
| batch-0002.json | 6 | 15,984 | Integration/AiAnalysisTests.cs, TestKit/TestKit.BuilderTests.cs, Unit/Handlers/MessageHandlerFakeTests.cs, Unit/Handlers/MessageHandlerSendSuspiciousMessageTests.cs | fake_services_factory, long_test_body, message_handler_test_factory, mock_verify, reads_message_message_id |
| batch-0003.json | 6 | 19,511 | Unit/Handlers/MessageHandlerSendSuspiciousMessageTests.cs | does_not_throw, message_handler_test_factory, mock_verify, reads_message_message_id, uses_message_handler_test_factory |
| batch-0004.json | 6 | 16,929 | Unit/Handlers/MessageHandlerSendSuspiciousMessageTests.cs, Unit/Handlers/MessageHandlerStatsCommandTests.cs | does_not_throw, message_handler_test_factory, mock_verify, reads_message_message_id, testkit_autofixture |
| batch-0005.json | 7 | 17,011 | Integration/AiAnalysisTests.cs, Integration/MessageHandlerBanTests.cs, Unit/Handlers/MessageHandlerStatsCommandTests.cs, Unit/Services/UserBanServiceTests.Modern.cs, Unit/Services/UserBanServiceTests.cs | fake_services_factory, message_handler_test_factory, mock_verify, reads_message_message_id, real_env_or_api |
| batch-0006.json | 7 | 16,130 | Integration/MessageHandlerBanTests.cs | message_handler_test_factory, mock_verify, uses_message_handler_test_factory |
| batch-0007.json | 8 | 16,632 | Integration/MessageHandlerBanTests.cs, Integration/MessageHandlerIntegrationTests.cs | assert_pass, message_handler_test_factory, mock_verify, uses_message_handler_test_factory |
| batch-0008.json | 8 | 15,851 | Integration/MessageHandlerIntegrationTests.cs | assert_pass, does_not_throw, message_handler_test_factory, uses_message_handler_test_factory |
| batch-0009.json | 8 | 16,223 | Integration/MessageHandlerIntegrationTests.cs, Integration/UserJoinFacadeIntegrationTests.cs, TestKit/TestKit.BuilderTests.cs | does_not_throw, message_handler_test_factory, mock_verify, testkit_autofixture, uses_message_handler_test_factory |
| batch-0010.json | 7 | 16,310 | TestKit/TestKit.BuilderTests.cs, Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs | assert_pass, does_not_throw, message_handler_test_factory, mock_verify, testkit_autofixture |
| batch-0011.json | 8 | 15,483 | Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs, Unit/Handlers/MessageHandlerExtendedTests.cs | assert_pass, message_handler_test_factory, mock_verify, real_env_or_api, uses_message_handler_test_factory |
| batch-0012.json | 8 | 16,112 | Unit/Handlers/MessageHandlerExtendedTests.cs | assert_pass, message_handler_test_factory, real_env_or_api, uses_message_handler_test_factory |
| batch-0013.json | 8 | 16,050 | Unit/Handlers/MessageHandlerExtendedTests.cs | assert_pass, message_handler_test_factory, real_env_or_api, uses_message_handler_test_factory |
| batch-0014.json | 8 | 14,569 | Unit/Handlers/MessageHandlerExtendedTests.cs | assert_pass, message_handler_test_factory, real_env_or_api, uses_message_handler_test_factory |
| batch-0015.json | 7 | 16,332 | Unit/Handlers/MessageHandlerFakeTests.cs, Unit/Handlers/MessageHandlerHandleAsyncBasicTests.cs | assert_pass, message_handler_test_factory, mock_verify, real_env_or_api, uses_message_handler_test_factory |
| batch-0016.json | 7 | 17,373 | Unit/Handlers/MessageHandlerHandleAsyncBasicTests.cs, Unit/Handlers/MessageHandlerHandleSayCommandTests.cs | message_handler_test_factory, mock_verify, uses_message_handler_test_factory |
| batch-0017.json | 7 | 15,938 | Unit/Handlers/MessageHandlerHandleSayCommandTests.cs, Unit/Handlers/MessageHandlerNullCoalescingTests.cs | does_not_throw, message_handler_test_factory, mock_verify, uses_message_handler_test_factory |
| batch-0018.json | 7 | 18,081 | Unit/Handlers/MessageHandlerNullCoalescingTests.cs | message_handler_test_factory, mock_verify, uses_message_handler_test_factory |
| batch-0019.json | 7 | 17,942 | Unit/Handlers/MessageHandlerNullCoalescingTests.cs, Unit/Handlers/MessageHandlerSendSuspiciousMessageTests.cs, Unit/Services/CaptchaServiceFakeTests.cs, Unit/Services/UserBanServiceTests.Modern.cs | does_not_throw, message_handler_test_factory, mock_verify, real_env_or_api, testkit_autofixture |
| batch-0020.json | 7 | 16,787 | Unit/Services/UserBanServiceTests.Modern.cs, Unit/Services/UserBanServiceTests.cs | mock_verify, reads_message_message_id, real_env_or_api, testkit_autofixture, uses_testkit_autofixture |
| batch-0021.json | 8 | 17,436 | Integration/AiAnalysisTests.cs, Integration/MessageHandlerBanBasicTests.cs, Integration/UserJoinFacadeIntegrationTests.cs, TestInfrastructure/MessageHandlerTestFactoryTests.cs | fake_services_factory, message_handler_test_factory, real_env_or_api, testkit_autofixture, uses_fake_services_factory |
| batch-0022.json | 8 | 14,835 | TestInfrastructure/MessageHandlerTestFactoryTests.cs | message_handler_test_factory, uses_message_handler_test_factory, weak_not_null |
| batch-0023.json | 9 | 15,622 | TestInfrastructure/MessageHandlerTestFactoryTests.cs, TestKit/TestKit.BuilderTests.cs, Unit/Handlers/CallbackQueryHandlerTests.cs | message_handler_test_factory, mock_verify, real_env_or_api, testkit_autofixture, uses_message_handler_test_factory |
| batch-0024.json | 8 | 16,381 | Unit/Handlers/CallbackQueryHandlerTests.cs, Unit/Handlers/MessageHandlerFakeTests.cs, Unit/Services/AiChecksTests.cs | does_not_throw, message_handler_test_factory, mock_verify, real_env_or_api, testkit_autofixture |
| batch-0025.json | 7 | 16,035 | Unit/Services/CaptchaServiceFakeTests.cs, Unit/Services/UserBanServiceTests.cs | mock_verify, real_env_or_api |
| batch-0026.json | 7 | 16,051 | Integration/AiChecksPhotoLoggingTest.cs, Integration/InfrastructureE2ETests.cs, Unit/Services/UserBanServiceTests.cs | mock_verify, real_env_or_api, weak_not_null |
| batch-0027.json | 8 | 17,598 | Integration/InfrastructureE2ETests.cs, Integration/MessageHandlerIntegrationTests.cs, Integration/SimpleE2ETests.cs, ModerationServiceTests.cs | message_handler_test_factory, reads_message_message_id, real_env_or_api, testkit_autofixture, uses_message_handler_test_factory |
| batch-0028.json | 8 | 17,716 | TestInfrastructure/MessageHandlerTestFactoryTests.cs, Unit/Handlers/MessageHandlerCanHandleTests.cs | message_handler_test_factory, uses_message_handler_test_factory |
| batch-0029.json | 8 | 15,213 | Unit/Handlers/MessageHandlerDeleteMessageLaterTests.cs, Unit/Handlers/MessageHandlerExtendedTests.cs, Unit/Handlers/MessageHandlerFakeTests.cs | message_handler_test_factory, real_env_or_api, uses_message_handler_test_factory |
| batch-0030.json | 8 | 15,163 | Unit/Handlers/MessageHandlerFakeTests.cs, Unit/Handlers/MessageHandlerHandleAsyncBasicTests.cs, Unit/Services/AIModuleTests.cs, Unit/Services/AiChecksTests.cs | message_handler_test_factory, real_env_or_api, uses_message_handler_test_factory, weak_not_null |
| batch-0031.json | 8 | 16,181 | Unit/Services/AiChecksTests.cs, Unit/Services/CaptchaModuleTests.cs | real_env_or_api, weak_not_null |
| batch-0032.json | 8 | 17,085 | Unit/Services/CaptchaServiceFakeTests.cs, Unit/Services/CommandRouterIntegrationTests.cs, Unit/Services/ConfigurationModuleTests.cs, Unit/Services/MessagingModuleTests.cs | real_env_or_api, weak_not_null |
| batch-0033.json | 8 | 16,371 | Unit/Services/MessagingModuleTests.cs, Unit/Services/ModerationServiceBusinessLogicTests.cs | real_env_or_api, testkit_autofixture, uses_testkit_autofixture, weak_not_null |
| batch-0034.json | 8 | 15,127 | Unit/Services/ModerationServiceBusinessLogicTests.cs | testkit_autofixture, uses_testkit_autofixture |
| batch-0035.json | 9 | 14,963 | Unit/Services/ModerationServiceBusinessLogicTests.cs, Unit/Services/StatisticsModuleTests.cs, Unit/Services/TelegramModuleTests.cs, Unit/Services/UserManagementModuleTests.cs | real_env_or_api, testkit_autofixture, uses_testkit_autofixture, weak_not_null |
| batch-0036.json | 7 | 18,098 | Integration/Effects/EffectsConfigurationIntegrationTest.cs, Integration/EnvironmentTest.cs, Integration/InfrastructureE2ETests.cs, Unit/Services/UserManagementModuleTests.cs | real_env_or_api, weak_not_empty, weak_not_null |
| batch-0037.json | 7 | 16,474 | Integration/InfrastructureE2ETests.cs, Integration/SimpleE2ETests.cs, ModerationServiceSimpleTests.cs | real_env_or_api |
| batch-0038.json | 7 | 16,424 | ModerationServiceSimpleTests.cs, ModerationServiceTests.cs | mock_verify, real_env_or_api |
| batch-0039.json | 8 | 15,580 | TestInfrastructure/MockAiChecksFactoryTests.cs, Unit/Effects/AiAnalysisEffectsTest.cs, Unit/Effects/AllowEffectsTest.cs, Unit/Effects/BanEffectsTest.cs | does_not_throw, mock_verify |
| batch-0040.json | 9 | 15,929 | Unit/Effects/DeleteEffectsTest.cs, Unit/Effects/ManualReviewEffectsTest.cs, Unit/Effects/ReportEffectsTest.cs, Unit/Handlers/CallbackQueryHandlerTests.cs | mock_verify, real_env_or_api |
| batch-0041.json | 7 | 16,486 | Unit/Handlers/CallbackQueryHandlerTests.cs, Unit/Handlers/MessageHandlerHandleUserMessageTests.cs | assert_pass, mock_verify, real_env_or_api |
| batch-0042.json | 7 | 17,274 | Unit/Handlers/MessageHandlerHandleUserMessageTests.cs, Unit/Handlers/MessageHandlerSemanticsTests.cs, Unit/Moderation/ModerationServiceExtendedTests.cs | assert_pass, mock_verify, real_env_or_api |
| batch-0043.json | 9 | 16,342 | Unit/Moderation/SpamHamClassifierTests.cs | assert_pass |
| batch-0044.json | 9 | 16,100 | Unit/Moderation/SpamHamClassifierTests.cs, Unit/Services/AIModuleTests.cs, Unit/Services/AiChecksExtendedTests.cs, Unit/Services/AiChecksTests.cs, Unit/Services/BotPermissionsServiceTests.cs | assert_pass, mock_verify, multiple_test_cases, real_env_or_api |
| batch-0045.json | 8 | 16,314 | Unit/Services/BotPermissionsServiceTests.cs, Unit/Services/CaptchaModuleTests.cs | mock_verify, real_env_or_api |
| batch-0046.json | 7 | 16,522 | Unit/Services/CaptchaServiceExtendedTests.cs | does_not_throw, mock_verify, real_env_or_api |
| batch-0047.json | 8 | 16,167 | Unit/Services/CaptchaServiceFakeTests.cs, Unit/Services/CommandProcessingServiceTests.cs | mock_verify, real_env_or_api |
| batch-0048.json | 8 | 16,870 | Unit/Services/CommandRouterTests.cs, Unit/Services/CommandsModuleTests.cs, Unit/Services/ConfigurationModuleTests.cs, Unit/Services/GlobalStatsManagerTests.cs | assert_pass, mock_verify, real_env_or_api |
| batch-0049.json | 8 | 14,725 | Unit/Services/GlobalStatsManagerTests.cs | assert_pass, does_not_throw |
| batch-0050.json | 8 | 15,620 | Unit/Services/GlobalStatsManagerTests.cs, Unit/Services/MessagingModuleTests.cs, Unit/Services/Moderation/ModerationRegistrationTests.cs, Unit/Services/ServiceChatDispatcherTests.cs | does_not_throw, mock_verify, real_env_or_api |
| batch-0051.json | 8 | 16,501 | Unit/Services/ServiceChatDispatcherTests.cs, Unit/Services/StatisticsModuleTests.cs, Unit/Services/TelegramModuleTests.cs, Unit/Services/UpdateDispatcherTests.cs | mock_verify |
| batch-0052.json | 8 | 16,836 | Unit/Services/UpdateDispatcherTests.cs, Unit/Services/UserBanServiceTests.cs | assert_pass, does_not_throw, mock_verify, real_env_or_api |
| batch-0053.json | 8 | 15,890 | Unit/Services/UserBanServiceTests.cs, Unit/Services/UserCleanupServiceTests.cs, Unit/Services/UserManagementModuleTests.cs, Unit/Services/UserManagerExtendedTests.cs | mock_verify, real_env_or_api |
| batch-0054.json | 9 | 15,863 | Unit/Services/UserManagerExtendedTests.cs | mock_verify |
| batch-0055.json | 8 | 15,128 | Unit/Services/UserManagerExtendedTests.cs | mock_verify |
| batch-0056.json | 8 | 15,094 | Unit/Services/UserManagerExtendedTests.cs | mock_verify |
| batch-0057.json | 8 | 18,113 | GoldenMaster/GoldenMasterRecorderTests.cs, Unit/Services/UserManagerExtendedTests.cs | mock_verify, weak_not_null |
| batch-0058.json | 10 | 14,495 | TestInfrastructure/AiChecksTestFactoryTests.cs, TestInfrastructure/AiServiceExceptionTestFactoryTests.cs, TestInfrastructure/ApprovedUsersStorageTestFactoryTests.cs, TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs | weak_not_null |
| batch-0059.json | 9 | 16,032 | TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs | weak_not_null |
| batch-0060.json | 10 | 14,690 | TestInfrastructure/CaptchaServiceTestFactoryTests.cs, TestInfrastructure/ChatMemberHandlerTestFactoryTests.cs, TestInfrastructure/ConfigurationExceptionTestFactoryTests.cs | weak_not_null |
| batch-0061.json | 9 | 14,124 | TestInfrastructure/ConfigurationExceptionTestFactoryTests.cs, TestInfrastructure/MimicryClassifierTestFactoryTests.cs, TestInfrastructure/MockAiChecksFactoryTests.cs | weak_not_null |
| batch-0062.json | 9 | 15,263 | TestInfrastructure/MockAiChecksFactoryTests.cs, TestInfrastructure/ModerationExceptionTestFactoryTests.cs, TestInfrastructure/ModerationServiceTestFactoryTests.cs | weak_not_null |
| batch-0063.json | 9 | 14,360 | TestInfrastructure/ModerationServiceTestFactoryTests.cs, TestInfrastructure/SpamHamClassifierTestFactoryTests.cs, TestInfrastructure/StatisticsServiceTestFactoryTests.cs | weak_not_null |
| batch-0064.json | 9 | 13,770 | TestInfrastructure/StatisticsServiceTestFactoryTests.cs, TestInfrastructure/SuspiciousUsersStorageTestFactoryTests.cs, TestInfrastructure/TelegramApiExceptionTestFactoryTests.cs | weak_not_null |
| batch-0065.json | 9 | 13,928 | TestInfrastructure/TelegramBotClientWrapperTestFactoryTests.cs, TestInfrastructure/UpdateDispatcherTestFactoryTests.cs, TestInfrastructure/UserCleanupServiceTestFactoryTests.cs | real_env_or_api, weak_not_null |
| batch-0066.json | 8 | 14,622 | TestInfrastructure/UserCleanupServiceTestFactoryTests.cs, TestInfrastructure/UserManagementExceptionTestFactoryTests.cs, TestKit/TestKit.BuilderTests.cs | real_env_or_api, weak_not_null |
| batch-0067.json | 8 | 17,384 | Unit/Golden/GoldenDeterminismTests.cs, Unit/Handlers/MessageHandlerTryFindUserIdTests.cs, Unit/Infrastructure/TelegramBotClientWrapperTests.cs, Unit/Infrastructure/WorkerTests.cs, Unit/Moderation/ModerationServiceTests.cs | real_env_or_api, weak_not_empty, weak_not_null |
| batch-0068.json | 8 | 14,972 | Unit/Services/AiChecksExtendedTests.cs | weak_not_null |
| batch-0069.json | 8 | 17,911 | Unit/Services/AiChecksExtendedTests.cs | weak_not_null |
| batch-0070.json | 8 | 16,799 | Unit/Services/AiChecksExtendedTests.cs, Unit/Services/BotPermissionsServiceTests.cs, Unit/Services/CaptchaServiceExtendedTests.cs | weak_not_null |
| batch-0071.json | 7 | 16,630 | Unit/Services/CaptchaServiceExtendedTests.cs, Unit/Services/ServiceChatDispatcherTests.cs | weak_not_null |
| batch-0072.json | 9 | 14,920 | CriticalFunctionalityTests.cs, Unit/Services/ServiceChatDispatcherTests.cs, Unit/Services/UpdateDispatcherTests.cs | very_short_test, weak_not_null |
| batch-0073.json | 8 | 15,532 | CriticalFunctionalityTests.cs, ErrorHandlingTests.cs | (none) |
| batch-0074.json | 8 | 18,423 | ErrorHandlingTests.cs, Integration/EnvironmentTest.cs, Integration/FakeTelegramClientExtendedTests.cs | real_env_or_api |
| batch-0075.json | 7 | 16,452 | Integration/FakeTelegramClientExtendedTests.cs | (none) |
| batch-0076.json | 9 | 16,942 | Integration/FakeTelegramClientExtendedTests.cs, ModerationServiceTests.cs, SimpleFiltersNullTests.cs | (none) |
| batch-0077.json | 9 | 16,404 | SimpleFiltersNullTests.cs, SimpleFiltersTests.cs | multiple_test_cases, very_short_test |
| batch-0078.json | 8 | 15,482 | SimpleFiltersTests.cs, TestInfrastructure/AiChecksTestFactoryTests.cs, TestInfrastructure/AiServiceExceptionTestFactoryTests.cs, TestInfrastructure/ApprovedUsersStorageTestFactoryTests.cs, TestInfrastructure/CallbackQueryHandlerTestFactoryTests.cs | multiple_test_cases |
| batch-0079.json | 8 | 16,982 | TestInfrastructure/ConfigurationExceptionTestFactoryTests.cs, TestInfrastructure/MimicryClassifierTestFactoryTests.cs, TestInfrastructure/MockAiChecksFactoryTests.cs | (none) |
| batch-0080.json | 9 | 14,514 | TestInfrastructure/MockAiChecksFactoryTests.cs, TestInfrastructure/ModerationExceptionTestFactoryTests.cs, TestInfrastructure/ModerationServiceTestFactoryTests.cs, TestInfrastructure/SpamHamClassifierTestFactoryTests.cs, TestInfrastructure/StatisticsServiceTestFactoryTests.cs | real_env_or_api |
| batch-0081.json | 10 | 14,748 | TestInfrastructure/UserCleanupServiceTestFactoryTests.cs, TestInfrastructure/UserManagementExceptionTestFactoryTests.cs, TestKit/TestKit.BuilderTests.cs, TextProcessorNullTests.cs, Unit/Golden/GoldenAggregateTests.cs | (none) |
| batch-0082.json | 7 | 18,199 | Unit/Golden/GoldenDeterminismTests.cs, Unit/Golden/GoldenHygieneTests.cs, Unit/Golden/GoldenManifestTests.cs, Unit/Golden/GoldenNormalizationTests.cs, Unit/Golden/GoldenV2ExportTests.cs | (none) |
| batch-0083.json | 7 | 19,322 | Unit/Handlers/MessageHandlerSemanticsTests.cs | (none) |
| batch-0084.json | 6 | 18,004 | Unit/Handlers/MessageHandlerSemanticsTests.cs | (none) |
| batch-0085.json | 7 | 16,447 | Unit/Handlers/MessageHandlerSemanticsTests.cs, Unit/Handlers/MessageHandlerTryFindUserIdTests.cs | (none) |
| batch-0086.json | 8 | 17,601 | Unit/Handlers/MessageHandlerTryFindUserIdTests.cs, Unit/Infrastructure/StatisticsServiceGetChatLinkTests.cs | (none) |
| batch-0087.json | 8 | 16,387 | Unit/Infrastructure/StatisticsServiceGetChatLinkTests.cs, Unit/Infrastructure/TelegramBotClientWrapperTests.cs, Unit/Infrastructure/WorkerGetChatLinkTests.cs | real_env_or_api, very_short_test |
| batch-0088.json | 9 | 16,616 | Unit/Infrastructure/WorkerGetChatLinkTests.cs | (none) |
| batch-0089.json | 9 | 15,268 | Unit/Infrastructure/WorkerGetChatLinkTests.cs, Unit/Infrastructure/WorkerTests.cs | (none) |
| batch-0090.json | 9 | 16,361 | Unit/Infrastructure/WorkerTests.cs, Unit/Logging/ReasonCodeMapperEdgeTests.cs, Unit/Logging/RuleCodeWhitelistTests.cs, Unit/Moderation/ModerationServiceExtendedTests.cs | multiple_test_cases, very_short_test |
| batch-0091.json | 8 | 15,738 | Unit/Moderation/ModerationServiceExtendedTests.cs | (none) |
| batch-0092.json | 9 | 16,291 | Unit/Moderation/ModerationServiceExtendedTests.cs | (none) |
| batch-0093.json | 8 | 16,459 | Unit/Moderation/ModerationServiceTests.cs, Unit/Moderation/SpamHamClassifierTests.cs | (none) |
| batch-0094.json | 8 | 16,386 | Unit/Moderation/SpamHamClassifierTests.cs, Unit/Services/AiChecksExtendedTests.cs | (none) |
| batch-0095.json | 7 | 16,676 | Unit/Services/BotPermissionsServiceTests.cs, Unit/Services/CaptchaServiceExtendedTests.cs | (none) |
| batch-0096.json | 8 | 15,684 | Unit/Services/CaptchaServiceExtendedTests.cs | (none) |
| batch-0097.json | 9 | 16,438 | Unit/Services/CaptchaServiceExtendedTests.cs | (none) |
| batch-0098.json | 7 | 16,589 | Unit/Services/ChannelModerationEffectsBuilderTests.cs, Unit/Services/ChannelModerationServiceTests.cs | real_env_or_api |
| batch-0099.json | 8 | 17,154 | Unit/Services/ChannelModerationServiceTests.cs, Unit/Services/GlobalStatsManagerTests.cs, Unit/Services/MessageTemplatesHtmlTests.cs | real_env_or_api |
| batch-0100.json | 9 | 14,795 | Unit/Services/MimicryClassifierTests.cs | (none) |
| batch-0101.json | 9 | 14,686 | Unit/Services/MimicryClassifierTests.cs | (none) |
| batch-0102.json | 8 | 15,362 | Unit/Services/ServiceChatDispatcherTests.cs | (none) |
| batch-0103.json | 9 | 16,058 | Unit/Services/ServiceChatDispatcherTests.cs, Unit/Services/SuspiciousUsersStorageTests.cs | (none) |
| batch-0104.json | 9 | 14,043 | Unit/Services/SuspiciousUsersStorageTests.cs | (none) |
| batch-0105.json | 8 | 16,798 | Unit/Services/SuspiciousUsersStorageTests.cs, Unit/Services/UpdateDispatcherTests.cs | (none) |
| batch-0106.json | 8 | 16,031 | Unit/Services/UpdateDispatcherTests.cs, Unit/Services/UserCleanupServiceTests.cs, Unit/Services/UserManagerExtendedTests.cs | (none) |
| batch-0107.json | 1 | 2,409 | Unit/Services/UserManagerExtendedTests.cs | (none) |

## Known Limitations

- Source excerpts use a fixed line radius (±30 lines). Method boundaries are not parsed.
- If a test method spans more than 60 lines, the excerpt may be truncated.
- Source files that no longer exist get a placeholder excerpt.
- Priority scoring is heuristic; not all suspicious tests may be in early batches.
- Records with `construction_path: unknown` are not prioritized.
