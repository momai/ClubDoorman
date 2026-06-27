# Keep Summary

Total keep decisions: **258**

Groups by (actual_seam, construction_path): **24**


## `CommandRouter` / `legacy_message_handler_test_factory` (1 tests)

- `HandleCommandAsync_WithStatsCommand_CallsCommandRouter` — Tests that command routing through CommandRouter works from MessageHandler. Single clean mock verify at the correct seam

## `CommandRouter` / `unknown` (4 tests)

- `HandleCommandAsync_NonCommand_ReturnsFalse` — Clean unit test at the correct seam. Verifies the non-command guard path in the router. Completes the trio of routing te
- `HandleCommandAsync_SecondCommand_RoutesToSecondHandler` — Clean unit test at the correct seam. Confirms the router iterates through all registered handlers and matches the correc
- `HandleCommandAsync_UnknownCommand_ReturnsFalse` — Clean unit test at the correct seam. Verifies the negative path: unknown commands are rejected without invoking any hand
- `HandleCommandAsync_ValidCommand_RoutesToCorrectHandler` — Clean unit test at the correct seam. Mock verify assertions confirm both positive (correct handler called once) and nega

## `DI` / `service_collection_di` (8 tests)

- `DI_BackwardCompatibility_OldInterfacesStillWork` — Test verifies backward compatibility of DI registration for old interface types. While the behavior value is low (old in
- `DI_CanResolve_AllCommandHandlers` — Test verifies command handler registration completeness: checks count >= 6 and verifies specific command names (start, s
- `DI_CanResolve_CommandRouter` — Test verifies a real bot contract: the DI container must be able to resolve ICommandRouter. Uses ServiceCollection DI se
- `AddConfigurationServices_ShouldRegisterIAppConfig` — Test verifies a critical DI registration: IAppConfig must be resolvable and must be the concrete AppConfig type. Uses mi
- `AddMessagingServices_ShouldRegisterILoggingConfigurationService` — Simple DI registration test for ILoggingConfigurationService. Uses ServiceCollection DI setup with mocked dependencies. 
- `AddMessagingServices_ShouldRegisterIServiceChatDispatcher` — Simple DI registration test for IServiceChatDispatcher. Uses ServiceCollection DI setup with mocked dependencies. NotNul
- `AddMessagingServices_ShouldRegisterMessageTemplates` — Simple DI registration test for MessageTemplates. Uses ServiceCollection DI setup with mocked dependencies. NotNull asse
- `AddClubDoorman_ShouldRegisterSingleModerationFacade` — Clear, specific assertion that IModerationFacade is registered exactly once. Prevents a real configuration drift bug whe

## `FakeTelegramClient` / `unknown` (9 tests)

- `FakeTelegramClient_ShouldClearOperationLog` — Tests the FakeTelegramClient fake's ClearOperationLog method. Verifies log is cleared while other collections (SentMessa
- `FakeTelegramClient_ShouldHandleExceptions` — Tests FakeTelegramClient's exception injection mechanism with a strong assertion on exception type and message. Isolated
- `FakeTelegramClient_ShouldResetAllCollections` — Tests the FakeTelegramClient fake's Reset method which should clear all tracked collections. Direct state assertions ver
- `FakeTelegramClient_ShouldTrackCallbackQueryAnswers` — Well-structured test for test infrastructure. Exercises AnswerCallbackQuery on the fake client and asserts tracked state
- `FakeTelegramClient_ShouldTrackMessageEdits` — Tests the FakeTelegramClient fake's edit-tracking behavior directly. Assertions are meaningful state checks on the fake'
- `FakeTelegramClient_ShouldTrackOperationLog` — Tests the FakeTelegramClient fake's operation log feature. Verifies that performing multiple operations produces the exp
- `FakeTelegramClient_ShouldTrackPhotoSending` — Tests the FakeTelegramClient fake's photo sending tracking. Direct state assertions on SentPhotos and WasPhotoSent. Test
- `FakeTelegramClient_ShouldTrackReplyMarkupEdits` — Tests the FakeTelegramClient fake's reply markup edit tracking. Direct state assertions on EditedMessages. Same pattern 
- `FakeTelegramClient_ShouldTrackUserRestrictions` — Tests the FakeTelegramClient fake's user restriction tracking. Direct state assertions on RestrictedUsers collection fie

## `Integration` / `unknown` (3 tests)

- `DispatchAsync_AllHandlersCanHandle_CallsAllHandlers` — Clean unit test with meaningful mock verification at the correct seam. Tests the complementary path where all handlers a
- `DispatchAsync_MultipleHandlersWithDifferentResponses_ProcessesCorrectly` — Clean test with meaningful assertions about dispatcher iteration semantics. Uses callback flags to verify both handlers 
- `DispatchAsync_ValidUpdate_ProcessesAllHandlers` — Clean unit test with meaningful mock verification at the correct seam. Tests core dispatch routing logic: CanHandle filt

## `MessageHandler` / `custom_local_world` (4 tests)

- `CanHandle_WithCallbackQuery_ReturnsTrue` — Tests the routing/dispatch contract of the callback query handler. Pure boolean assertion on CanHandle is the correct ap
- `CanHandle_WithoutCallbackQuery_ReturnsFalse` — Tests the negative routing case for the callback query handler. Complements the positive case. Pure boolean assertion on
- `HandleAsync_WithEmptyCallbackData_LogsWarningAndReturns` — Tests a real edge-case behavior: the handler gracefully handles empty callback data by logging a warning. The mock verif
- `HandleUserMessageAsync_WithUserInCaptcha_DeletesMessageAndReturns` — Strong test with meaningful assertion using MessageEnvelope and FakeTelegramClient.WasMessageDeleted to verify actual Te

## `MessageHandler` / `legacy_message_handler_test_factory` (8 tests)

- `DeleteAndReportMessage_WhenModerationReturnsDelete_DeletesMessage` — Well-structured test with a meaningful single assertion: fakeClient.WasMessageDeleted(envelope) is True. Uses MessageEnv
- `HandleMessageAsync_NullMessage_HandlesGracefully` — Strong assertion using Assert.ThrowsAsync for a specific exception type. Tests a clear input validation contract at the 
- `DeleteMessageLater_WhenCancelled_DoesNotInvokeDelete` — Test uses Mock.Verify with Times.Never to assert cancellation prevents the delete call. Uses MessageEnvelope for message
- `DeleteMessageLater_WhenDeleteFails_LogsWarning` — Meaningful assertion via mock verification that error handling produces a log warning. The error handling contract for f
- `DeleteMessageLater_WithShortTimeout_InvokesDelete` — Meaningful test using proper MessageEnvelope and FakeTelegramClient tracking to verify delayed message deletion. The ass
- `HandleAsync_SpamMessage_DeletesAndReports` — Test uses proper test infrastructure: TestKitTelegram.CreateSpamScenario with MessageEnvelope and FakeTelegramClient. As
- `HandleCommandAsync_WithCustomStats_RoutesThroughCommandRouter` — Tests the core MessageHandler routing contract: commands are forwarded to CommandRouter. The mock_verify assertion on Co
- `HandleCommandAsync_WithUnknownCommand_StillCallsCommandRouter` — Tests a distinct behavioral variant from the happy-path test: CommandRouter is called regardless of its return value. Th

## `MessageHandler` / `unknown` (10 tests)

- `HandleUserMessageAsync_WithClubUser_ReturnsEarly` — Clean test with meaningful negative mock verify (Times.Never). Tests a real bot contract: club members bypass moderation
- `HandleUserMessageAsync_WithUserInBlacklist_CallsHandleBlacklistBanAsync` — Clean test with meaningful mock verify assertion. Tests a critical bot contract: blacklisted users are routed to the ban
- `AiProfileRestricted_EmitsSemanticRule` — Meaningful state assertion on golden semantics JSON output. Tests the AiProfileAnalysisStep's observability contract whe
- `AlreadyApproved_EmitsSemanticRule` — Meaningful state assertion on golden semantics JSON output. Tests the AlreadyApproved pipeline step's observability cont
- `CaptchaPending_EmitsSemanticRule` — Meaningful state assertion on golden semantics JSON output. Tests the CaptchaPending pipeline step's observability contr
- `ClubMemberSkip_EmitsSemanticRule` — Meaningful state assertion on golden semantics JSON output. Tests the ClubMemberSkip pipeline step's observability contr
- `Moderated_Ban_MapsToModeratedBan` — Strong dual assertion on both ruleCode (ModeratedBan) and action (Ban) in golden semantics JSON. Tests the moderation ba
- `Moderated_Delete_MapsToModeratedDelete` — Strong dual assertion on both ruleCode (ModeratedDelete) and action (Delete) in golden semantics JSON. Tests the moderat
- `Moderated_EmitsSemanticRule` — Strong dual assertion on both ruleCode (ModeratedAllow) and action (Allow) in golden semantics JSON. Tests the moderatio
- `Moderated_Report_MapsToModeratedReport` — Strong dual assertion on both ruleCode (ModeratedReport) and action (Report) in golden semantics JSON. Tests the moderat

## `MessageService` / `unknown` (8 tests)

- `Constructor_NullBotClient_ThrowsArgumentNullException` — Clean constructor validation test. Verifies both the exception type and the parameter name. Standard defensive programmi
- `Constructor_NullLogger_ThrowsArgumentNullException` — Clean constructor validation test. Verifies both the exception type and the parameter name for the logger parameter. Com
- `ShouldSendToAdminChat_AutoBanData_ReturnsFalse` — Clean unit test at the correct seam. Tests the negative routing case: AutoBanData goes to log chat, not admin chat. Mean
- `ShouldSendToAdminChat_ErrorData_ReturnsTrue` — Clean unit test at the correct seam. Tests the type-based routing: ErrorData maps to admin chat. Meaningful boolean asse
- `ShouldSendToAdminChat_NullNotification_ReturnsFalse` — Clean unit test for null input handling at the correct seam. Direct method call with meaningful boolean assertion. Null 
- `ShouldSendToAdminChat_SimpleNotificationData_ReturnsFalse` — Clean unit test at the correct seam. Tests the default routing case: regular notification data goes to log chat, not adm
- `ShouldSendToAdminChat_SuspiciousMessageData_ReturnsTrue` — Clean unit test at the correct seam. Tests the type-based routing switch in ShouldSendToAdminChat: SuspiciousMessageNoti
- `ShouldSendToAdminChat_SuspiciousUserData_ReturnsTrue` — Clean unit test at the correct seam. Tests the type-based routing switch: SuspiciousUserData maps to admin chat. Meaning

## `ModerationFacade` / `custom_local_world` (1 tests)

- `CheckUserName_WithNullUser_ThrowsArgumentNullException` — Strong assertion verifying specific exception type and parameter name for a null input guard. This is a meaningful API c

## `ModerationFacade` / `none_or_pure` (4 tests)

- `CheckUserNameAsync_EmptyFirstName_ThrowsModerationException` — Strong contract test for a real bot policy: users without first names are rejected. Asserts exception type and message c
- `CheckUserNameAsync_LongFirstName_ReturnsReport` — Strong boundary test for a real bot policy: excessively long names trigger report action. Dual assertion on both action 
- `CheckUserNameAsync_NullUser_ThrowsArgumentNullException` — Strong contract test verifying argument validation on a core moderation service method. Asserts both exception type and 
- `CheckUserNameAsync_ValidFirstName_ReturnsAllow` — Happy-path contract test for the moderation service's username check. Asserts ModerationAction.Allow result. Completes t

## `ModerationFacade` / `testkit_autofixture` (5 tests)

- `CheckUserNameAsync_InvalidUser_ReturnsBanAction` — Strong assertions on both Action (Ban) and Reason (contains specific text). Tests a clear production contract - users wi
- `CheckMessageAsync_MessageWithButtons_ReturnsBanAction` — Strong assertions on both Action and Reason. Tests a real moderation policy (messages with buttons are banned) at the co
- `CheckMessageAsync_SpamDetected_ReturnsBanAction` — Strong assertions on both Action and Reason. Tests a critical moderation contract (spam detection leads to delete) with 
- `CheckMessageAsync_StoryMessage_ReturnsDeleteAction` — Strong assertions on both Action and Reason. Tests a real moderation policy (story messages are deleted) at the correct 
- `CheckMessageAsync_UserInBanlist_ReturnsBanAction` — Strong state assertions on both ModerationResult.Action and Reason. Tests a core moderation contract (banlist enforcemen

## `ModerationFacade` / `unknown` (11 tests)

- `BanAndCleanupUserAsync_TelegramApiError_ReturnsFalse` — Tests error handling in the ban-and-cleanup flow. Meaningful assertion that the method returns false on failure rather t
- `CheckMessageAsync_BadMessage_ReturnsBanAction` — Tests a distinct code path from spam classification: known bad message detection via BadMessageManager. Meaningful asser
- `CheckMessageAsync_ClassifierThrowsException_ThrowsException` — Tests error propagation with strong assertions on both exception type and message content. Uses mock classifier to simul
- `CheckMessageAsync_SpamMessage_ReturnsDeleteAction` — Tests the spam detection code path with meaningful assertions on Action and Reason. Uses mock classifier to simulate spa
- `CheckMessageAsync_ValidMessage_ReturnsAllowAction` — Tests the core moderation happy path with meaningful assertions on both Action and Reason. Uses ModerationServiceTestFac
- `CheckUserNameAsync_ValidUser_ReturnsAllowAction` — Tests user name validation happy path with meaningful assertions on Action and Reason. Uses correct seam with factory se
- `CheckMessageAsync_WithBadMessageManager_ReturnsBan` — Tests core spam detection behavior: known bad messages get banned with appropriate reason. Strong assertion on both Mode
- `CheckMessageAsync_BannedUser_ReturnsBan` — Meaningful state assertions on both result.Action (Ban) and result.Reason content (Contains 'блэклист'). Tests a core bo
- `CheckMessageAsync_EmptyMessage_ReturnsReport` — Tests a real bot contract: empty messages are reported rather than silently allowed or deleted. Assertion is specific (A
- `CheckMessageAsync_NullMessage_ThrowsArgumentNullException` — Strong assertion on both exception type and ParamName. Tests defensive programming contract at the moderation service bo
- `CheckMessageAsync_SpamMessage_ReturnsDelete` — Meaningful state assertions on both result.Action (Delete) and result.Reason content (Contains 'спам'). Tests a core bot

## `Multiple` / `legacy_message_handler_test_factory` (2 tests)

- `AutoBanChannel_WhenChannelSendsMessage_BansChannel` — Tests channel message routing from MessageHandler to ChannelModerationService. Uses MessageEnvelope and factory setup. S
- `AutoBan_WhenUserViolatesRules_BansUser` — Tests the integration seam between MessageHandler and UserBanService for auto-ban flow. Uses MessageEnvelope for proper 

## `NotificationService` / `unknown` (11 tests)

- `DeleteToLogEffect_ShouldCallNotificationService` — Clean unit test for a thin effect delegator. The mock verify assertion is the correct and only assertion needed for a de
- `DeleteWithReportEffect_ShouldCallNotificationService` — Same pattern as DeleteToLogEffect test. Correctly verifies the delegation contract of a thin effect class. The mock veri
- `RequireManualReviewEffect_ShouldCallNotificationService` — Tests the manual review notification path, a meaningful bot contract. Correct mock verify on a thin effect delegator. Cl
- `RequireManualReviewEffect_WithSilentMode_ShouldPassSilentModeToNotificationService` — Tests parameter passthrough for the silent mode variant. Complements the non-silent test by verifying the flag is forwar
- `ReportMessageEffect_ShouldCallNotificationService` — Tests the report message notification path. Correct mock verify on a thin effect delegator. Clean and focused unit test.
- `ReportMessageEffect_WithSilentMode_ShouldPassSilentModeToService` — Tests parameter passthrough for the silent mode variant. Complements the non-silent test. Thin effect delegator with cor
- `ModerationWarning_ShouldContainHtmlTags` — Meaningful multi-assertion test at the correct seam. Verifies HTML output contains expected bold tags, user mention link
- `SuspiciousUserTemplate_ShouldUseHtmlCodeTags` — Meaningful assertions at the correct seam. Verifies both positive (contains HTML code tags around messages) and negative
- `UserMention_ShouldBeHtmlLink` — Strong exact-match assertion on the full output string. Tests the core template substitution behavior: {UserMention} is 
- `SendToAdminChatAsync_BotClientThrowsException_LogsErrorAndRethrows` — Strong test combining exception assertion (Assert.ThrowsAsync with message check) and mock verification (logger called a
- `SendToLogChatAsync_BotClientThrowsException_LogsErrorAndRethrows` — Symmetric to the admin-chat error handling test. Strong assertions combining exception verification and error logging. B

## `Pure` / `custom_local_world` (8 tests)

- `GetAttentionBaitProbability_WithNullUser_ReturnsDefaultResult` — Null safety edge case with specific value assertions (probability=0.0, Photo=Empty, NameBio=Empty). This is a meaningful
- `GetSpamProbability_WithNullMessage_ThrowsArgumentNullException` — Strong assertion via Assert.ThrowsAsync<ArgumentNullException> protecting a real API contract. The test is focused, corr
- `CreateCaptchaAsync_TelegramError_ThrowsException` — Strong throws assertion with exception message verification. Tests real error-handling behavior at the captcha seam. Cle
- `GetCaptchaInfo_ValidKey_ReturnsCaptchaInfo` — Test has meaningful state assertions: verifies captchaInfo is not null AND captchaInfo.User.Id equals 789. Uses custom l
- `RemoveCaptcha_ValidKey_ReturnsTrue` — Two meaningful state assertions: removal returns true and captcha is no longer retrievable. Tests real cleanup behavior 
- `ValidateCaptchaAsync_CorrectAnswer_ReturnsTrue` — Meaningful state assertion on a real captcha validation seam. Tests the happy path of captcha validation with a direct u
- `ValidateCaptchaAsync_InvalidKey_ReturnsFalse` — Meaningful edge-case assertion: invalid/unknown key returns false. Protects against a regression where missing keys migh
- `ValidateCaptchaAsync_WrongAnswer_ReturnsFalse` — Meaningful negative-case state assertion on captcha validation. Complements the correct-answer test. Direct unit test of

## `Pure` / `none_or_pure` (88 tests)

- `SimpleFilters_FindAllRussianWordsWithLookalikeSymbols_HandlesNullInput` — Meaningful assertion on a pure function's null handling contract. The lookalike symbol detection is part of the spam det
- `SimpleFilters_FindAllRussianWordsWithLookalikeSymbols_ReturnsEmptyListForEmptyInput` — Meaningful state assertion on a pure function's edge case behavior. Empty input should produce an empty result list -- a
- `SimpleFilters_HasStopWords_HandlesNullInput` — Meaningful assertion on a pure function's null handling contract. HasStopWords is part of the text filtering pipeline us
- `SimpleFilters_HasStopWords_ReturnsFalseForEmptyInput` — Meaningful state assertion on a pure function's edge case behavior. Empty input should not trigger stop word detection -
- `TextProcessor_NormalizeText_HandlesEmptyString` — Meaningful state assertion on a pure function's edge case behavior. Empty string handling is a real contract for a text 
- `TextProcessor_NormalizeText_HandlesNullInput` — Meaningful assertion on a pure function's null handling contract. TextProcessor.NormalizeText is a core utility used in 
- `TextProcessor_NormalizeText_RemovesFormatting` — Strong behavioral assertion on a core anti-evasion capability. The test uses a realistic adversarial input with zero-wid
- `AiChecks_GetSpamProbability_WithNullMessage_ThrowsArgumentNullException` — Tests real production code via AiChecksTestFactory. Asserts both exception type (ArgumentNullException) and message cont
- `FindAllRussianWordsWithLookalikeSymbolsInNormalizedText_WithEmptyString_ReturnsEmptyList` — Pure unit test with a meaningful state assertion on the empty string edge case of the normalized-text lookalike detector
- `FindAllRussianWordsWithLookalikeSymbolsInNormalizedText_WithNullInput_ThrowsArgumentNullException` — Pure unit test with strong assertions on both exception type and parameter name. Tests the normalized-text variant of th
- `FindAllRussianWordsWithLookalikeSymbols_WithEmptyString_ReturnsEmptyList` — Pure unit test with a meaningful state assertion on the empty string edge case of the lookalike symbol detector. No depe
- `FindAllRussianWordsWithLookalikeSymbols_WithNullInput_ThrowsArgumentNullException` — Pure unit test with strong assertions on both exception type and parameter name. No dependencies, no mocks, no factory. 
- `HasStopWords_WithEmptyString_ReturnsFalse` — Pure unit test with a meaningful state assertion on a public static utility method. No dependencies, no mocks, no factor
- `HasStopWords_WithNormalText_ReturnsFalse` — Pure unit test with a meaningful state assertion on the happy path of the stop words filter. No dependencies, no mocks. 
- `HasStopWords_WithNullInput_ThrowsArgumentNullException` — Pure unit test with strong assertions on exception type and parameter name. No dependencies, no mocks, no factory. Isola
- `TooManyEmojis_WithEmptyString_ReturnsFalse` — Pure unit test with a meaningful state assertion on the empty string edge case of the emoji spam detector. No dependenci
- `TooManyEmojis_WithNullInput_ThrowsArgumentNullException` — Pure unit test with strong assertions on both exception type and parameter name. Tests defensive programming contract on
- `FormatStripped` — Pure unit test in SimpleFiltersTests with state assertions and TestCase attribute. Tests text processing behavior that f
- `HasLookAlikeSymbols_Tests` — Pure parameterized unit test with 18 distinct cases covering the core lookalike symbol detection algorithm. Tests Cyrill
- `HasStopWords_Test` — Pure unit test with meaningful state assertions on stop word detection. Parameterized with 3 test cases covering spam an
- `NormalizeText_WithEmptyString_ReturnsEmptyString` — Clean pure unit test with meaningful equality assertion on an edge case. Tests a real contract on a utility used in mess
- `NormalizeText_WithMultipleLines_ReturnsSingleLine` — Clean pure unit test with meaningful equality assertion on a specific normalization behavior (line collapsing). Tests a 
- `NormalizeText_WithMultipleSpaces_ReturnsSingleSpaces` — Clean pure unit test with meaningful equality assertion on whitespace normalization behavior. Tests a real contract on a
- `NormalizeText_WithNormalText_ReturnsNormalizedText` — Clean pure unit test with meaningful equality assertion on the primary normalization behavior. Tests a real contract on 
- `NormalizeText_WithNullInput_ThrowsArgumentNullException` — Clean pure unit test with strong assertion (Assert.Throws with paramName check). Tests a real defensive contract on a ut
- `NormalizeText_WithRussianText_ReturnsNormalizedText` — Clean pure unit test with meaningful equality assertion on a non-Latin script edge case. Tests a real contract on a util
- `Repository_ShouldNotContain_LegacyOrLocalGoldenArtifacts` — Valid repo hygiene guard that prevents accidental commits of deprecated or transient artifacts. Strong assertions on fil
- `GetChatLink_ChannelWithoutUsername_ReturnsBoldTitle` — Clean focused unit test with exact string assertion against a pure utility. No external dependencies, no mocks, direct i
- `GetChatLink_EmptyTitle_ReturnsEmptyBrackets` — Documents current behavior for empty title edge case. The behavior (empty brackets) is arguably questionable UX, but the
- `GetChatLink_NullTitle_UsesDefaultTitle` — Meaningful edge-case test for null title handling. Exact string assertion verifies both the default title substitution a
- `GetChatLink_PublicGroupWithUsername_ReturnsMarkdownLink` — Clean, focused unit test of a pure utility class. Directly instantiates ChatLinkFormatter, constructs a Chat with userna
- `GetChatLink_RegularGroupWithoutUsername_ReturnsBoldTitle` — Clean, focused unit test of a pure utility class. Tests the fallback formatting path for regular groups without a userna
- `GetChatLink_SupergroupWithoutUsername_ReturnsTelegramLink` — Clean, focused unit test of a pure utility class. Tests the supergroup link format path which strips the -100 prefix to 
- `GetChatLink_TitleWithMarkdownSymbols_EscapesCorrectly` — Critical test for markdown escaping correctness. Unescaped markdown in chat links would break Telegram message formattin
- `GetChatLink_ChannelWithoutUsername_ReturnsBoldTitle` — Clean pure unit test with exact string assertion. Tests the channel-without-username branch of the formatting logic. Dir
- `GetChatLink_EmptyTitle_ReturnsEmptyBrackets` — Clean pure unit test with exact string assertion. Tests empty-string edge case for title handling. The behavior is a min
- `GetChatLink_NullTitle_UsesDefaultTitle` — Clean pure unit test with exact string assertion. Tests null-title handling which prevents null reference issues in user
- `GetChatLink_RegularGroupWithoutUsername_ReturnsBoldTitle` — Clean pure unit test with exact string assertion. Tests a real formatting branch: regular groups without username get bo
- `GetChatLink_SupergroupWithoutUsername_ReturnsTelegramLink` — Clean pure unit test with exact string assertion against a formatting utility. Direct construction, no mocks, no infrast
- `GetChatLink_TitleWithMarkdownSymbols_EscapesCorrectly` — Clean pure unit test with exact string assertion. Tests Markdown escaping which is critical for bot messages to render c
- `GetChatLink_WithChatIdAndTitle_Channel_ReturnsBoldTitle` — Clean pure unit test with exact string assertion. Tests channel formatting branch where positive chatId produces bold ti
- `GetChatLink_WithChatIdAndTitle_EmptyTitle_ReturnsEmptyBold` — Clean pure unit test with exact string assertion. Tests empty-string edge case for title handling. Minor edge case but t
- `GetChatLink_WithChatIdAndTitle_NullTitle_UsesDefaultTitle` — Clean pure unit test with exact string assertion. Tests null-title handling which prevents null reference issues in user
- `GetChatLink_WithChatIdAndTitle_RegularGroup_ReturnsBoldTitle` — Clean pure unit test with exact string assertion. Tests regular group bold formatting through the long/string overload. 
- `GetChatLink_WithChatIdAndTitle_Supergroup_ReturnsTelegramLink` — Clean pure unit test with exact string assertion. Tests supergroup link formatting through the long/string overload. Com
- `GetChatLink_WithChatIdAndTitle_TitleWithMarkdownSymbols_EscapesCorrectly` — Clean pure unit test with exact string assertion. Tests Markdown escaping which is critical for bot messages to render c
- `GetChatLink_WithChatIdAndTitle_UsernameStartsWithAt_ReturnsMarkdownLink` — Clean pure unit test with exact string assertion. Tests the overload that accepts chatId and chatTitle separately, speci
- `AdminDisplayName_WithFirstNameOnly_ReturnsFirstName` — Meaningful state assertion on a pure utility function. Same reflection-based pattern as other Worker tests. Small, focus
- `AdminDisplayName_WithoutUsername_ReturnsFullName` — Meaningful state assertion on a pure utility function. Uses reflection to test non-public static method, which adds frag
- `BanPhrase_With_Banlist_Wins` — Focused unit test for a specific priority rule in the reason code mapper. Tests that 'banlist' substring detection overr
- `Maps_As_Expected` — Excellent parameterized unit test covering 12 distinct mapping cases including null, empty, case variations, and categor
- `AnalyzeMessages_ConsistentResults_ForSameInput` — Clean unit test at the correct seam verifying determinism of the classifier. Direct service call with meaningful equalit
- `AnalyzeMessages_ContextualMessages_ReturnsLowScore` — Clean unit test at the correct seam testing the context analysis dimension of the classifier. Direct service call, meani
- `AnalyzeMessages_DiverseMessages_ReturnsLowScore` — Clean unit test at the correct seam testing the diversity dimension of the mimicry classifier. Direct service call via t
- `AnalyzeMessages_EmptyList_ReturnsZero` — Clean unit test for empty list input at the correct seam. Direct service call, meaningful equality assertion. Complement
- `AnalyzeMessages_EmptyMessages_ReturnsHighScore` — Clean unit test at the correct seam for an important edge case. Three empty strings pass the count==3 guard and exercise
- `AnalyzeMessages_ExactlyThreeMessages_ReturnsValidScore` — Clean unit test verifying the output range contract [0.0, 1.0] at the correct seam. Direct service call with two meaning
- `AnalyzeMessages_LessThanThreeMessages_ReturnsZero` — Clean unit test verifying the exactly-3-messages contract at the correct seam. Direct service call with meaningful equal
- `AnalyzeMessages_LongMessages_ReturnsLowScore` — Strong behavioral test verifying that long informative messages produce low suspicion scores. Direct service call with m
- `AnalyzeMessages_MixedQualityMessages_ReturnsMediumScore` — Clean unit test at the correct seam testing blended scoring across multiple heuristic dimensions. Direct service call wi
- `AnalyzeMessages_MoreThanThreeMessages_ReturnsZero` — Clean unit test verifying the exactly-3-messages contract from the upper bound. Same code path as the less-than-three te
- `AnalyzeMessages_NormalMessages_ReturnsLowScore` — Strong behavioral test verifying that normal conversational messages produce low suspicion scores, preventing false posi
- `AnalyzeMessages_NullMessages_ReturnsZero` — Clean unit test for null input handling at the correct seam. Direct service call via test factory with mocked logger, me
- `AnalyzeMessages_RepetitiveMessages_ReturnsHighScore` — Clean unit test at the correct seam testing the diversity detection dimension. Direct service call, meaningful threshold
- `AnalyzeMessages_TemplatePhrases_ReturnsHighScore` — Strong behavioral test verifying that template and greeting phrases (common spam pattern) produce elevated suspicion sco
- `AnalyzeMessages_VeryShortMessages_ReturnsHighScore` — Strong behavioral test of the core mimicry detection feature. Verifies that very short messages (typical spam pattern) p
- `AnalyzeMessages_WhitespaceOnlyMessages_ReturnsHighScore` — Clean unit test at the correct seam for a whitespace edge case. Whitespace-only messages pass the count==3 guard and are
- `AddSuspiciousUser_DuplicateUserId_HandlesGracefully` — Tests a meaningful business rule: duplicate add returns false. Three assertions (first returns true, second returns fals
- `AddSuspiciousUser_EmptyReason_AddsUserSuccessfully` — Edge case test for empty reason string in SuspiciousUserInfo. Low production value since production code always provides
- `AddSuspiciousUser_NegativeUserId_AddsUserSuccessfully` — Edge case test for negative userId. Low production value since Telegram user IDs are always positive. Same pattern as ze
- `AddSuspiciousUser_NullReason_AddsUserSuccessfully` — Edge case test for null element in the reason list. Low production value since production code always provides non-null 
- `AddSuspiciousUser_ValidUserId_AddsUserSuccessfully` — Straightforward unit test with two meaningful state assertions (add returns true, IsSuspicious returns true). Tests core
- `AddSuspiciousUser_ZeroUserId_AddsUserSuccessfully` — Edge case test for userId=0. Low production value since Telegram never produces zero user IDs, but verifies the storage 
- `EdgeCases_HandleCorrectly` — Tests important defensive behaviors: idempotent add (returns false on duplicate), safe remove of non-existent entry (ret
- `FullLifecycle_AddCheckRemove_WorksCorrectly` — Solid unit test verifying the core CRUD lifecycle of SuspiciousUsersStorage with four meaningful assertions on add resul
- `GetAiDetectUsers_AfterRemoval_ReturnsRemainingUsers` — Clean unit test with meaningful state assertions on add/remove/query behavior of SuspiciousUsersStorage. Tests a real in
- `GetAiDetectUsers_EmptyStorage_ReturnsEmptyList` — Core negative-case test for GetAiDetectUsers. Asserts empty collection from empty storage. Clean unit test with meaningf
- `GetAiDetectUsers_WithUsers_ReturnsAllUsers` — Core positive-case test for GetAiDetectUsers with multiple users. Asserts both count (2) and membership (Does.Contain fo
- `IsSuspiciousUser_ExistingUser_ReturnsTrue` — Core functionality test for the suspicious users storage lookup. Adds a user then asserts IsSuspicious returns true. Cle
- `IsSuspiciousUser_NegativeUserId_ReturnsFalse` — Edge case test for negative userId on IsSuspicious lookup. Low production value since Telegram user IDs are always posit
- `IsSuspiciousUser_NonExistentUser_ReturnsFalse` — Core negative-case test for the suspicious users storage lookup. Asserts IsSuspicious returns false for a user that does
- `IsSuspiciousUser_RemovedUser_ReturnsFalse` — Tests the add-then-remove-then-query lifecycle of the suspicious users storage. Verifies that RemoveSuspicious actually 
- `IsSuspiciousUser_ZeroUserId_ReturnsFalse` — Edge case test for userId=0 on IsSuspicious lookup. Low production value since Telegram never produces zero user IDs. Si
- `MultipleUsers_ConcurrentOperations_HandleCorrectly` — Tests multi-user isolation in the suspicious users storage, verifying that operations on one user key do not affect anot
- `RemoveSuspiciousUser_ExistingUser_RemovesSuccessfully` — Tests core remove functionality with two meaningful state assertions (remove returns true, IsSuspicious returns false af
- `RemoveSuspiciousUser_NegativeUserId_HandlesGracefully` — Edge case test for negative userId on RemoveSuspicious. Low production value since Telegram user IDs are always positive
- `RemoveSuspiciousUser_NonExistentUser_HandlesGracefully` — Edge case test for removing a user that was never added. Low production value but verifies the storage doesn't throw on 
- `RemoveSuspiciousUser_ZeroUserId_HandlesGracefully` — Edge case test for userId=0 on RemoveSuspicious. Low production value since Telegram never produces zero user IDs, but v

## `Pure` / `testkit_autofixture` (2 tests)

- `CheckMessageAsync_MessageWithoutUser_ThrowsModerationException` — Protects a real guard condition: messages without a sender are rejected with a domain exception. Asserts exception type 
- `CheckMessageAsync_NullMessage_ThrowsArgumentNullException` — Clean input validation test. Asserts both exception type and ParamName. Protects a real API contract (null guard on publ

## `Pure` / `unknown` (53 tests)

- `RequireAiAnalysisEffect_ShouldCallAiCascadeService` — Clean unit test at the correct seam. Verifies the effect delegates to the cascade service with exact parameters. Mock ve
- `RequireAiAnalysisEffect_WithSilentMode_ShouldPassSilentModeToAiCascadeService` — Tests a specific parameter variation (silent mode=true) through the effect delegation. Complements the base test by cove
- `RequireAiAnalysisEffect_WithZeroMlScore_ShouldPassZeroToAiCascadeService` — Tests the zero ML score boundary condition through the effect delegation. Complements the base test by covering a distin
- `AllowMessageEffect_WhenAiDetectBlocked_ShouldNotIncrementGoodMessageCount` — Tests the conditional branch where AI detect blocks the user, preventing good message count increment. Uses Times.Never 
- `AllowMessageEffect_WhenAiDetectNotBlocked_ShouldIncrementGoodMessageCount` — Tests the happy path of the allow effect: AI detect check followed by good message count increment. Two mock verificatio
- `AllowMessageEffect_WithCaption_ShouldUseCaptionAsMessageText` — Tests the caption fallback path where message text is null and caption is used instead. This is a real edge case for med
- `BanUserEffect_ShouldCallUserFlowLoggerAndUserBanService` — Tests the core ban effect behavior: logging the ban event and executing the ban. Two mock verifications confirm both sid
- `Aggregates_ShouldAlignWithManifest` — Meaningful consistency check between two golden baseline files. Protects data integrity of the golden master system. Use
- `ManifestTimestamp_ShouldMatchFixedEnv_WhenFixedTimestampIsSet` — Meaningful state assertion on golden baseline integrity. Protects the deterministic timestamp contract of the baseline h
- `StableUsernameHash_IsDeterministic_AndCaseSensitive` — Tests a pure utility function (deterministic username hashing) critical for golden master data sanitization. Uses reflec
- `Manifest_ShouldCoverAllSnapshots_AndHaveNoUnknownRuleCodes` — Strong multi-assertion test protecting golden manifest integrity. Verifies schema version, variant, ID uniqueness, gap w
- `NormalizedSnapshots_ShouldMirrorManifest_AndHaveStableSchema` — Strong assertions protecting the normalization layer contract. Verifies file existence, field parity, schema version, an
- `V2Snapshots_ShouldExist_AndMatchManifest` — Strong assertions protecting the V2 export layer contract. Verifies file existence, field parity, schema version, and ru
- `TelegramBotClientWrapper_BotId_ReturnsCorrectId` — Tests a real production behavior (token ID extraction) with exact assertion. Moderate maintenance risk because it instan
- `TelegramBotClientWrapper_Constructor_ThrowsOnNullBot` — Clean defensive programming test verifying constructor null guard. Uses Assert.Throws with exact exception type. No mean
- `IncrementGoodMessageCountAsync_EmptyMessageText_ThrowsArgumentException` — Same pattern as NullUser/NullChat tests. Clean unit test with strong assertions for input validation. Small, stable, cor
- `IncrementGoodMessageCountAsync_NullChat_ThrowsArgumentNullException` — Same pattern as NullUser test. Clean unit test with strong assertions for a standard guard clause. Small, stable, correc
- `IncrementGoodMessageCountAsync_NullUser_ThrowsArgumentNullException` — Clean, focused unit test with strong assertions (ThrowsAsync + ParamName check). Tests standard .NET guard clause. Low b
- `GetSpamProbability_NullMessage_ThrowsArgumentNullException` — Tests a real AiChecks instance (via factory) for null-input guard behavior. Asserts both exception type and parameter na
- `GetBotChatMemberAsync_WhenCached_ReturnsFromCache` — Clean test of cache-hit behavior. Asserts return value and verifies API was called exactly once despite two invocations.
- `IsSilentModeAsync_ForAdminChat_ReturnsFalse` — Meaningful assertions: verifies return value (false) and that GetChat is never called (short-circuit optimization). The 
- `IsSilentModeAsync_ForLogAdminChat_ReturnsFalse` — Meaningful unit test: asserts return value and verifies the short-circuit path skips the Telegram API call. Low fragilit
- `IsSilentModeAsync_ForPrivateChat_ReturnsFalse` — Tests a specific branch (private chat handling) with meaningful state and mock assertions. The mock verifications are ti
- `IsSilentModeAsync_WhenBotIsAdmin_ReturnsFalse` — Tests real BotPermissionsService.IsSilentModeAsync with mocked ITelegramBotClientWrapper. Verifies a meaningful producti
- `IsSilentModeAsync_WhenBotIsNotAdmin_ReturnsTrue` — Tests real BotPermissionsService.IsSilentModeAsync with mocked ITelegramBotClientWrapper. Verifies a meaningful producti
- `CreateCaptchaAsync_ValidParameters_CreatesCaptchaSuccessfully` — Test has meaningful assertions beyond null checks: verifies result.ChatId equals input chat.Id, result.User.Id equals in
- `CreateCaptchaAsync_WithoutJoinMessage_CreatesCaptchaSuccessfully` — Tests a specific edge case (null join message) at the correct seam (CaptchaService). Has two meaningful assertions: resu
- `GenerateKey_DifferentParameters_ReturnsDifferentKeys` — Tests the uniqueness contract of key generation: different (chat, user) pairs must produce different keys. This prevents
- `GenerateKey_SameParameters_ReturnsSameKey` — Tests the determinism contract of key generation. This is essential for the captcha flow: the same user in the same chat
- `GetCaptchaInfo_ExistingCaptcha_ReturnsCaptchaInfo` — Tests the retrieval path of captcha state at the correct seam. Has four meaningful assertions: result is not null, ChatI
- `RemoveCaptcha_AfterRemoval_CaptchaNotAccessible` — Tests the state change after removal: captcha is truly gone from the store, not just returning true. This is a stronger 
- `RemoveCaptcha_ExistingCaptcha_ReturnsTrue` — Clean unit test at the correct seam. Verifies the success path of captcha cleanup (RemoveCaptcha returns true for existi
- `ValidateCaptchaAsync_AfterValidation_CaptchaRemoved` — Tests the one-time-use cleanup contract of captcha validation. Verifies internal state is cleaned up after validation, p
- `ValidateCaptchaAsync_ConcurrentValidation_HandlesCorrectly` — Tests thread safety of captcha validation: when multiple concurrent callbacks arrive for the same captcha, only one shou
- `ValidateCaptchaAsync_CorrectAnswer_ReturnsTrue` — Clean unit test at the correct seam. Verifies the core captcha validation contract (correct answer accepted). Direct ser
- `ValidateCaptchaAsync_IncorrectAnswer_ReturnsFalse` — Clean unit test at the correct seam. Verifies the core negative validation contract (wrong answer rejected). Complements
- `ValidateCaptchaAsync_NonExistentKey_ReturnsFalse` — Tests a meaningful production scenario: expired, timed-out, or otherwise missing captcha entries gracefully return false
- `IsChannelOwnerAsync_WhenUserIsNotOwner_ShouldReturnFalse` — Clean unit test at the correct seam. Verifies the negative path of channel owner detection with a different user ID in t
- `IsChannelOwnerAsync_WhenUserIsOwner_ShouldReturnTrue` — Clean unit test at the correct seam. Verifies the positive path of channel owner detection by mocking GetChatAdministrat
- `ShouldAllowChannelMessageAsync_WhenChannelDiscussion_ShouldReturnTrue` — Clean unit test at the correct seam. Sets IsAutomaticForward=true on the message, mocks GetChatAsync, and asserts Should
- `ShouldAllowChannelMessageAsync_WhenUnknownChannel_ShouldReturnFalse` — Clean unit test at the correct seam. Complements the positive cases by verifying the negative path: when the sender is n
- `ShouldAllowChannelMessageAsync_WhenUserIsOwner_ShouldReturnTrue` — Tests the core channel moderation decision: channel owners should be allowed to post. Mocks both GetChatAdministratorsAs
- `ShouldSendToAdminChat_AiDetectData_ReturnsTrue` — Clean unit test with a meaningful state assertion on a real routing contract. Tests the type-based dispatch switch that 
- `DispatchAsync_HandlerThrowsException_LogsErrorAndRethrows` — Tests a meaningful dispatcher contract: exceptions from handlers must propagate, not be swallowed. Uses proper mock setu
- `DispatchAsync_NoHandlersCanHandle_LogsDebugInfo` — Meaningful mock verification of core dispatch routing logic: confirms the dispatcher iterates all handlers and only invo
- `DispatchAsync_WithCancellationToken_PassesTokenToHandlers` — Meaningful mock verification of cancellation token propagation through the dispatcher. Tight unit test with a specific b
- `RemoveUserFromAllApprovals_WhenUserExists_ReturnsTrue` — Clean unit test with meaningful state assertions on a concrete service. Tests user cleanup behavior which is a real bot 
- `RemoveUserFromAllApprovals_WhenUserNotExists_ReturnsFalse` — Simple negative case with state assertions. Low individual value but complements the positive case and costs little to m
- `RemoveUserFromGlobalApproval_UserApprovedGlobally_ReturnsTrue` — Core cleanup functionality test with meaningful assertions: verifies return value and confirms storage state changed. Te
- `RemoveUserFromGlobalApproval_UserNotApprovedGlobally_ReturnsFalse` — Clean test with meaningful assertions: verifies both return value and storage state. Tests idempotent removal edge case.
- `RemoveUserFromGroupApproval_UserNotApprovedInGroup_ReturnsFalse` — Clean test with meaningful assertions: verifies both return value and storage state. Tests a real edge case (idempotent 
- `RemoveUserFromGroupApproval_WhenUserExists_ReturnsTrue` — Solid unit test with meaningful state assertions on group approval removal. Tests a real bot contract for per-group user
- `RemoveUserFromGroupApproval_WhenUserNotExists_ReturnsFalse` — Negative case complement to the group approval positive test. Low individual value but straightforward and cheap to main

## `TestInfrastructure` / `testkit_message_handler_builder` (1 tests)

- `MessageHandlerBuilder_WithModerationFacade_UsesConfiguredFacadeInPipeline` — Tests test infrastructure (builder wiring), not production behavior. Mock verification is focused and meaningful for the

## `TestInfrastructure` / `unknown` (1 tests)

- `Recorder_WritesInputAndOptionalSemantics_AfterV1Removal` — Meaningful test with strong assertions on real GoldenMasterRecorder behavior. Verifies file creation, deterministic JSON

## `UserBanService` / `custom_local_world` (8 tests)

- `AutoBanChannel_ExceptionOccurs_LogsWarningAndSendsErrorNotification` — Tests exception handling path in channel banning. Verifies error notification routing when Telegram API call fails. Mock
- `AutoBan_KnownSpamReason_SelectsCorrectNotificationType` — Meaningful assertion on the correct seam: verifies the reason-to-notification-type mapping for known spam. Correct notif
- `AutoBan_PrivateChat_LogsWarningAndReturns` — Clear negative assertion (Times.Never) on the correct seam. Same private-chat-protection contract as the blacklist varia
- `AutoBan_RepeatedViolationsReason_SelectsCorrectNotificationType` — Same pattern as text mention test. Validates notification routing for repeated violation reasons at the UserBanService s
- `AutoBan_TextMentionReason_SelectsCorrectNotificationType` — Clean seam test at UserBanService level. Verifies notification type routing based on reason string. Mock verify is appro
- `AutoBan_UnknownReason_SelectsDefaultNotificationType` — Tests the default/fallback notification routing path. Clean seam test at UserBanService level with appropriate mock veri
- `BanBlacklistedUser_PrivateChat_LogsWarningAndReturns` — Clear negative assertion (Times.Never) on the correct seam. Protects the contract that ban operations are skipped for pr
- `BanUserForLongName_PermanentBan_BansUserPermanently` — Meaningful mock assertion on the correct seam: verifies BanChatMember is called with null unban date (permanent ban) vs 

## `UserBanService` / `testkit_autofixture` (4 tests)

- `BanUserForLongName_PermanentBan_BansUserPermanently` — Tests the permanent ban path where banDuration is null, verifying BanChatMember is called with null unban date. This is 
- `BanUserForLongName_PrivateChat_LogsWarningAndReturns` — Tests a specific edge case: private chats cannot ban members, so the service should log a warning and return early. The 
- `BanUserForLongName_ValidChat_BansUserAndSendsNotification` — Strong contract test at the correct seam. Four targeted mock verifies covering ban execution (with correct ban expiry ch
- `BanUserForLongName_PrivateChat_LogsWarningAndReturns` — Tests a specific and important edge case: private chats cannot ban members, so the service should skip the ban and notif

## `UserBanService` / `unknown` (4 tests)

- `TrackViolationEffect_ShouldCallUserBanService` — Tests violation tracking delegation, which is a meaningful bot contract. The effect correctly wires message/user/reason 
- `BanAndCleanupUserAsync_TelegramApiError_ReturnsFalse` — Tests error handling contract of the ban flow. Verifies the method swallows exceptions and returns false instead of prop
- `BanAndCleanupUserAsync_ValidParameters_ReturnsTrue` — Tests the happy path of the ban-and-cleanup flow, which is a real bot contract. State assertion on return value is meani
- `BanAndCleanupUserAsync_WithoutMessageId_OnlyBansUser` — Tests the overload of BanAndCleanupUserAsync that skips message deletion. Meaningful distinction in the ban flow. State 
