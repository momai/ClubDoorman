# Rewrite by Seam Queue

Total rewrite candidates: **178**

Groups by (replacement_seam, actual_seam): **30**


## `AiChecks` ← `Multiple` (1 tests)

- `E2E_AI_Analysis_PhotoWithCaption_ShouldIncludePhoto` — Current test is bad: name says should include photo but the constructed message has no photo field set (only Text). Asse

## `CallbackQueryHandler` ← `MessageHandler` (3 tests)

- `HandleAsync_WithAdminChatId_CallsHandleAdminCallback` — Current test only verifies a debug log message was written, not that HandleAdminCallback was actually invoked. The admin
- `HandleAsync_WithException_LogsErrorAndAnswersCallback` — Current test is fragile: heavy mock setup for captcha service, assertions on specific Russian log strings and AnswerCall
- `HandleAsync_WithRegularChatId_CallsHandleCaptchaCallback` — Current test only verifies a debug log message was written, not that HandleCaptchaCallback was actually invoked. The rou

## `CaptchaService` ← `Pure` (6 tests)

- `BanExpiredCaptchaUsersAsync_ExpiredCaptchaWithViolationLimit_ReturnsTrueAndBansPermanently` — Current test uses reflection to inject expired captcha state and relies on mock verification of BanChatMemberAsync with 
- `BanExpiredCaptchaUsersAsync_ExpiredCaptchaWithoutViolationLimit_ReturnsFalseAndBansTemporarily` — Current test uses reflection to inject expired captcha state and verifies BanChatMemberAsync is called with a non-null u
- `BanExpiredCaptchaUsersAsync_ExpiredCaptcha_RegistersViolation` — Current test uses reflection to inject an expired captcha into a private field (_captchaNeededUsers), making it brittle 
- `BanExpiredCaptchaUsersAsync_WithActiveCaptchas_CompletesSuccessfully` — Current test uses DoesNotThrow which does not verify the key behavior: that active (non-expired) captchas are NOT banned
- `CreateCaptchaAsync_TelegramApiError_ThrowsException` — Test is fundamentally broken: name says ThrowsException but assertion is DoesNotThrow. Additionally flagged as making re
- `CreateCaptchaAsync_UserWithInappropriateName_UsesGenericName` — Test name promises verification that a generic name is used for inappropriate user names, but the only assertion is mock

## `CaptchaService` ← `UserBanService` (1 tests)

- `CreateCaptchaAsync_ValidUser_SendsWelcomeMessage` — Current test has weak assertions (Not.Null on a just-created object, state assertion on user ID) and uses mock choreogra

## `CaptchaService (Pure)` ← `Pure` (2 tests)

- `CreateCaptchaAsync_MultipleCaptchasSameUser_HandlesCorrectly` — The test verifies a real contract: second captcha for same user/chat overwrites the first. However, assertions are weak:
- `CreateCaptchaAsync_MultipleUsers_CreatesUniqueCaptchas` — The test is brittle and expensive: uses Task.Delay(10) to seed RNG differences, has a retry loop (up to 3 attempts with 

## `CommandRouter` ← `CommandRouter` (6 tests)

- `HandleSayCommandAsync_WhenBotThrowsException_SendsErrorMessage` — Current test uses legacy MessageHandlerTestFactory and mock choreography on MessageServiceMock. The behavior (error hand
- `HandleSayCommandAsync_WithInsufficientParts_SendsWarningMessage` — Current test verifies that /say with only a username (no message body) triggers a warning notification via MessageServic
- `HandleSayCommandAsync_WithInvalidUserId_SendsWarningMessage` — Current test verifies that '/say invalid_id' triggers a warning notification via MessageServiceMock.Verify. The error-ha
- `HandleSayCommandAsync_WithInvalidUsername_SendsWarningMessage` — Current test verifies that '/say @nonexistentuser' triggers a warning notification via MessageServiceMock.Verify. The er
- `HandleSayCommandAsync_WithUserId_AttemptsToSendMessage` — Current test verifies BotMock.SendMessage was called with the correct ChatId (12345L) and message text ('Привет!') via m
- `HandleSayCommandAsync_WithUsername_AttemptsToFindUserAndSendMessage` — Current test name says 'WithUsername' but the test body uses '/say 12345' (numeric user ID), making the name misleading.

## `CommandRouter` ← `Multiple` (3 tests)

- `E2E_AI_Analysis_AdminButton_Ban_ShouldBanUser` — Identical structure to the approve test. Uses FakeServicesFactory to create a callback handler that only records callbac
- `E2E_AI_Analysis_AdminButton_Own_ShouldApproveUser` — Current test uses FakeServicesFactory.CreateCallbackQueryHandler() which appears to be a fake/recording wrapper, not the
- `E2E_AI_Analysis_AdminButton_Skip_ShouldSkipUser` — Identical structure to the approve and ban tests. Uses FakeServicesFactory to create a callback handler that only record

## `FakeTelegramClient` ← `FakeTelegramClient` (1 tests)

- `E2E_FakeTelegramClient_ShouldSupportMessageDeletion` — id_sensitive_suspected=true: test reads message.MessageId directly, which is unreliable in test construction (often rema

## `Integration` ← `Integration` (1 tests)

- `DispatchAsync_OperationCanceledException_LogsInfoAndRethrows` — Test name claims logging verification ('LogsInfoAndRethrows') but only asserts exception identity rethrow. The exception

## `Logging` ← `Multiple` (10 tests)

- `CaptchaFail_EmitsSemanticRule` — Current test is brittle: builds a full CallbackQueryHandler with 13+ mocked dependencies, then asserts on temp filesyste
- `CaptchaSuccess_EmitsSemanticRule` — Current test is brittle: builds a full CallbackQueryHandler with 13+ mocked dependencies, then asserts on temp filesyste
- `Banlist_Hit_WritesDeleteBanlistSemantics` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, then asserts on 
- `BotMessage_EmitsSemanticRule` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, then asserts on 
- `ChannelMessage_EmitsSemanticRule` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, then asserts on 
- `Command_EarlyExit_WritesAllowCommandSemantics` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies including a real pipeline with 14 ste
- `LeftMemberCleanup_EmitsSemanticRule` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, requires BotId s
- `NewMembers_TriggersSemanticRule` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, then asserts on 
- `PrivateChat_NonCommand_SkipsAndWritesSemanticRule` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, then asserts on 
- `SystemNoUser_EmitsSemanticRule` — Current test is brittle: builds a full MessageHandler with 13+ mocked dependencies and a real pipeline, then asserts on 

## `MessageHandler` ← `MessageHandler` (21 tests)

- `HandleAsync_ExceptionInUserManager_HandlesGracefully` — DoesNotThrow is a weak assertion that only confirms the exception is swallowed, not that it is handled correctly. Error 
- `CanHandle_EditedMessage_ReturnsTrue` — Meaningful boolean assertion on CanHandle routing for edited messages. Relies on heavy legacy MessageHandlerTestFactory.
- `CanHandle_MessageWithCaption_ReturnsTrue` — Tests that caption messages are accepted by the handler. Meaningful routing assertion but uses heavy legacy factory. Cap
- `CanHandle_MessageWithoutText_ReturnsTrue` — Tests that media-only messages are accepted by the handler. Meaningful routing assertion but uses heavy legacy factory. 
- `CanHandle_NullUpdate_ReturnsFalse` — Tests null-safety guard on CanHandle. The assertion is meaningful (false for null), but the test uses the heavy legacy f
- `CanHandle_UpdateWithCallbackQuery_ReturnsFalse` — Tests that callback queries are correctly rejected by the message handler (they belong to a different handler). Meaningf
- `CanHandle_UpdateWithoutMessage_ReturnsFalse` — Tests that updates without a message payload are rejected by CanHandle. Meaningful assertion but uses heavy legacy facto
- `CanHandle_ValidMessage_ReturnsTrue` — The current test has a meaningful boolean assertion on CanHandle routing, but relies on the heavy legacy MessageHandlerT
- `DeleteMessageLater_WithCustomTimeout_SchedulesMessageDeletion` — Assert.Pass provides zero verification -- the test cannot fail even if scheduling is completely broken. The behavior of 
- `CanHandle_EditedMessage_ReturnsTrue` — Meaningful boolean assertion on CanHandle routing for edited messages. Uses heavy legacy MessageHandlerTestFactory with 
- `CanHandle_NullUpdate_ReturnsFalse` — Tests null-safety guard on CanHandle. Meaningful assertion (false for null) but uses heavy legacy factory and has real_e
- `CanHandle_UpdateWithCallbackQuery_ReturnsFalse` — Tests that callback queries are correctly rejected by the message handler (they belong to a different handler). Meaningf
- `CanHandle_UpdateWithoutMessage_ReturnsFalse` — Tests that updates without a message payload are rejected by CanHandle. Meaningful assertion but uses heavy legacy facto
- `CanHandle_ValidMessage_ReturnsTrue` — Meaningful boolean assertion on CanHandle routing for valid messages. However, the test relies on the heavy legacy Messa
- `HandleAsync_NullUpdate_ThrowsArgumentNullException` — Strong assertion verifying both exception type and parameter name. However, uses heavy legacy MessageHandlerTestFactory 
- `HandleAsync_ModerationServiceError_LogsAndContinues` — Current test is bad: DoesNotThrow is a weak assertion, relies on 4 mock setups and legacy MessageHandlerTestFactory maki
- `HandleAsync_TelegramError_HandlesGracefully` — The error-handling contract (catch Telegram API failures and log) is a real bot contract. Current test uses FakeTelegram
- `HandleUserMessageAsync_WithBotUser_IgnoresMessage` — Current test uses Assert.Pass which provides no behavioral verification. The named behavior (ignoring bot messages) is a
- `HandleUserMessageAsync_WithLeftChatMember_IgnoresMessage` — Current test uses Assert.Pass which provides no behavioral verification. The named behavior (handling left-chat-member s
- `HandleUserMessageAsync_WithModerationError_HandlesGracefully` — Assert.Pass provides zero behavioral verification. The test sets up an exception on the facade but only asserts that no 
- `HandleUserMessageAsync_WithNullUser_HandlesGracefully` — Current test uses Assert.Pass which provides no behavioral verification -- it only confirms the method returned without 

## `MessageHandler` ← `Multiple` (2 tests)

- `HandleAsync_ExceptionInBotClient_HandlesGracefully` — DoesNotThrow is a weak assertion that does not verify what graceful handling actually looks like. The graceful error han
- `HandleAsync_ExceptionInModerationService_HandlesGracefully` — DoesNotThrow is a weak assertion that does not verify what graceful handling actually looks like (e.g., logging, fallbac

## `MessagePipeline` ← `MessageHandler` (2 tests)

- `HandleAsync_NewUser_SendsCaptcha` — The new-user-joins-gets-captcha flow is a real bot contract worth preserving. However, the test exercises MessageHandler
- `HandleUserMessageAsync_WithForwardedMessage_ProcessesCorrectly` — Assert.Pass provides zero behavioral verification. The test sets up a forwarded message but asserts nothing about whethe

## `MessagePipeline` ← `MessagePipeline` (16 tests)

- `HandleMessageAsync_ApprovedUser_ProcessesSuccessfully` — Assert.Pass provides no meaningful assertion. The test sets up UserManager.Approved=true, which is a real bot contract: 
- `HandleMessageAsync_BannedUser_HandlesGracefully` — Assert.Pass provides no meaningful assertion. The test sets up UserManager.InBanlist=true, which is a real bot contract:
- `HandleMessageAsync_SuspiciousUser_HandlesGracefully` — Assert.Pass provides no meaningful assertion. The test sets up SuspiciousUsersStorage.IsSuspicious=true, which is a real
- `HandleAsync_ChannelMessage_HandlesSuccessfully` — Assert.Pass() provides zero assertions. Channel messages have distinct handling (ChannelMessageStep) and may trigger mod
- `HandleAsync_LeftChatMemberFromBot_HandlesSuccessfully` — Assert.Pass() provides zero assertions. When a member leaves via bot action, the LeftMemberCleanupStep should clean up u
- `HandleAsync_LeftChatMemberFromUser_HandlesSuccessfully` — Assert.Pass() provides zero assertions. Voluntary departure triggers LeftMemberCleanupStep to clean up user state and pe
- `HandleAsync_MessageWithoutFrom_HandlesSuccessfully` — Assert.Pass() provides zero assertions. Messages without a From field represent a real defensive edge case -- the pipeli
- `HandleAsync_MultipleNewChatMembers_HandlesSuccessfully` — Assert.Pass() provides zero assertions. Multiple new members is a real edge case where each member should be evaluated i
- `HandleAsync_NewChatMembersInAdminChat_HandlesSuccessfully` — Assert.Pass() provides zero assertions. Admin chat membership changes may have different handling (e.g., skip moderation
- `HandleAsync_NewChatMembers_HandlesSuccessfully` — Assert.Pass() provides zero assertions. The NewChatMembers pipeline step is a core bot contract: new members should trig
- `HandleAsync_ApprovedUser_ProcessesNormally` — The contract that approved users bypass captcha is a real and owner-visible behavior. Current test verifies CaptchaServi
- `HandleAsync_BotMessage_Ignores` — Current test exercises the legacy MessageHandler seam; bot-message filtering is now implemented in SystemOrBotMessageSte
- `HandleAsync_ServiceMessage_Ignores` — The test name says 'Ignores' but actually asserts that new-member service messages ARE processed via UserJoinFacade. Thi
- `HandleAsync_EditedMessage_CallsModerationAndLogs` — Current test verifies edited messages go through moderation via mock.Verify on ModerationFacade.CheckMessageAsync and Ch
- `HandleAsync_ValidMessage_VerifiesSpecificCalls` — Current test verifies three distinct pipeline stages (moderation, banlist, AI analysis) via mock.Verify with parameter-s
- `HandleAsync_ValidTextMessage_CallsModerationAndLogs` — This is the core happy-path test: a text message should be moderated and logged. It has three assertions (CheckMessageAs

## `MessagePipeline` ← `Multiple` (3 tests)

- `E2E_AI_Analysis_OperationOrder_ShouldBeCorrect` — Current test is bad: name says operation order should be correct but only asserts handler not null and fake bot sent mes
- `HandleAsync_ValidMessage_ProcessesSuccessfully` — Tests the full message processing pipeline from HandleAsync through moderation to completion. Uses FakeTelegramClient an
- `HandleUserMessageAsync_WithAiProfileAnalysis_ExecutesAnalysis` — Assert.Pass provides zero behavioral verification. The test sets up an allow result but does not verify that AI profile 

## `ModerationFacade` ← `Integration` (1 tests)

- `E2E_ModerationFlow_ShouldProcessMessageWithCorrectOrder` — Test is misnamed as E2E but calls a mocked moderation service that returns a hardcoded Allow result, making the test a t

## `ModerationFacade` ← `ModerationFacade` (8 tests)

- `E2E_ModerationService_ShouldHandleMimicryDetection` — Current test sends a normal message and asserts it gets Allow. This is a weak negative test - it only verifies that a be
- `E2E_ModerationService_ShouldHandleSpamMessage` — Current test accepts either Allow or Delete as valid action (BeOneOf), which means it passes regardless of whether spam 
- `HandleUserMessageAsync_WithAllowedMessage_ProcessesCorrectly` — The test only verifies that CheckMessageAsync was called once on the facade mock. This is mock choreography that confirm
- `CheckMessageAsync_ChatWithoutId_HandlesGracefully` — Tests a real edge case: messages from chats without an ID should not crash. The behavior (graceful handling of missing c
- `CheckMessageAsync_UserWithoutFirstName_HandlesGracefully` — Tests a real edge case: messages from users without a first name should not crash or block. The behavior (graceful handl
- `CheckMessageAsync_WithMimicryDetection_ReturnsAllow` — Tests the integration of spam classifier and mimicry classifier in the moderation pipeline. The behavior (allowing messa
- `CheckMessageAsync_ClassifierException_ThrowsException` — Current test asserts that classifier exceptions propagate through ModerationService. In production, error handling (Poll
- `CheckMessageAsync_ValidMessage_ReturnsAllow` — Current test has meaningful assertions (Action == Allow, Reason not empty) and exercises the core moderation decision pa

## `ModerationFacade` ← `Multiple` (1 tests)

- `WhenModerationReturnsBan_ShouldCallAutoBanAsync` — Tests the critical moderation-to-ban flow, a core bot contract. Current test goes through full MessageHandler with legac

## `ModerationFacade` ← `TestInfrastructure` (2 tests)

- `CheckMessageAsync_GoodMessage_ReturnsAllowAction` — Current test asserts against FakeModerationService (test infrastructure), not the real ModerationPolicy. The fake's Chec
- `CheckUserNameAsync_EmptyFirstName_ThrowsException` — Current test asserts against FakeModerationService.CheckUserNameAsync, which is test infrastructure, not production code

## `NotificationService` ← `Multiple` (1 tests)

- `E2E_AI_Analysis_MessageHandler_ShouldSendNotification` — Current test is bad: only asserts handler is not null and fake bot has sent messages. SentMessages.NotBeEmpty is too bro

## `NotificationService` ← `NotificationService` (8 tests)

- `SendSuspiciousMessageWithButtons_WhenBotThrowsException_SendsFallbackMessage` — Current test is in a MessageHandler-named file but exercises NotificationService directly. The fallback notification pat
- `SendSuspiciousMessageWithButtons_WhenForwardFails_SendsMessageWithoutForward` — Current test is in a MessageHandler-named file but exercises NotificationService directly. The error-handling path (forw
- `SendSuspiciousMessageWithButtons_WithSilentMode_AddsSilentModePrefix` — Current test is in a MessageHandler-named file but exercises NotificationService directly. The mock verification on Send
- `SendSuspiciousMessageWithButtons_WithValidData_SendsMessageWithButtons` — The behavior (suspicious message notification with buttons) is a real bot contract. The test is at the correct seam (Not
- `SendToAdminChatAsync_AllNotificationTypes_HandledCorrectly` — Current test only verifies SendMessageAsync call count (7 times) without confirming messages go to the correct destinati
- `SendToAdminChatAsync_ValidNotification_SendsMessage` — Current test uses It.IsAny matchers for all SendMessageAsync parameters, verifying only that the method was called once.
- `SendToLogChatAsync_AllNotificationTypes_HandledCorrectly` — Current test only verifies SendMessageAsync call count (7 times) without confirming messages go to the correct destinati
- `SendToLogChatAsync_ValidNotification_SendsMessage` — Same weakness as the admin-chat variant: It.IsAny matchers on all parameters verify only call count. Log chat dispatch i

## `PipelineStep` ← `Multiple` (2 tests)

- `E2E_AI_Analysis_Channel_ShouldNotShowCaptcha` — Current test is bad: name says should not show captcha but assertions only check handler not null and fake bot sent mess
- `E2E_AI_Analysis_FirstMessage_ShouldTriggerAnalysis` — Current test is bad: assertion is NotBeNull on a handler just created, contains Console.WriteLine debug output instead o

## `Pure` ← `Integration` (3 tests)

- `E2E_MimicryClassifier_ShouldDetectMimicry` — Mimicry detection is a core anti-impersonation bot contract with high owner value. Current test loads real .env files an
- `E2E_SpamHamClassifier_ShouldDetectHam` — The ham (non-spam) classification behavior is a core bot contract with high owner value - false positives directly impac
- `E2E_SpamHamClassifier_ShouldDetectSpam` — The spam detection behavior is a core bot contract with high owner value. However, the current test loads real .env file

## `Pure` ← `Pure` (50 tests)

- `SpamHamClassifier_Timeout_ReturnsGracefulFallback` — Current test has meaningless assertions: Assert.That(result.Spam, Is.TypeOf<bool>()) and Assert.That(result.Score, Is.Ty
- `TryFindUserIdByUsername_WithCaseInsensitiveSearch_FindsUser` — The case-insensitive search behavior (StringComparison.OrdinalIgnoreCase at line 20 of UserIndex.cs) is a real productio
- `TryFindUserIdByUsername_WithEmptyUsername_ReturnsNull` — The empty-string guard (string.IsNullOrEmpty check at line 13 of UserIndex.cs) is a real defensive contract worth preser
- `TryFindUserIdByUsername_WithMultipleCacheEntries_FindsCorrectUser` — Current test is tightly coupled to UserIndex internal cache implementation: it manually populates System.Runtime.Caching
- `TryFindUserIdByUsername_WithNullUsername_ReturnsNull` — The null guard (string.IsNullOrEmpty check at line 13 of UserIndex.cs) is a real defensive contract worth preserving. Th
- `TryFindUserIdByUsername_WithUsernameWithoutAtSymbol_FindsUser` — Current test is tightly coupled to UserIndex internal cache value format ('Message from {username}' without @). If the c
- `FullName_WithEmptyLastName_ReturnsFirstName` — Same reflection-based fragility as other Worker tests. Tests empty-string-last-name handling of an internal utility. The
- `FullName_WithFirstNameAndLastName_ReturnsCombinedName` — Current test uses reflection (typeof(Worker).GetMethod with BindingFlags.NonPublic) to invoke a private static method, w
- `FullName_WithFirstNameOnly_ReturnsFirstName` — Same reflection-based fragility as other Worker tests. Tests null-last-name handling of an internal utility. The behavio
- `UserToKey_ReturnsCorrectFormat` — Current test uses reflection to invoke a private static method, which is fragile. The UserToKey method generates cache k
- `GetAttentionBaitProbability_NullUser_ReturnsDefaultResult` — Current test only asserts Not.Null on result and three sub-properties (SpamProbability, Photo, NameBio). These are weak 
- `GetAttentionBaitProbability_UserWithNullLastName_ReturnsSpamPhotoBio` — Current test only asserts Not.Null on result and SpamProbability. A null last name is a very common scenario (most Teleg
- `GetAttentionBaitProbability_UserWithoutUsername_ReturnsSpamPhotoBio` — Current test only asserts Not.Null on result and SpamProbability. The edge case of a user without a username is a real s
- `GetAttentionBaitProbability_ValidUser_ReturnsSpamPhotoBio` — Current test only asserts Not.Null on result and three sub-properties. This is a structural smoke check that doesn't ver
- `GetAttentionBaitProbability_WithCallback_ReturnsSpamPhotoBio` — Current test sets up a callback that tracks whether it was called, but never asserts on callbackCalled. The comment ackn
- `GetSpamProbability_ConcurrentCalls_HandlesCorrectly` — Tests real AiChecks service concurrency, which is a legitimate concern since the service is called during message proces
- `GetSpamProbability_EmptyMessage_ReturnsSpamProbability` — Current test asserts result not null and probability in [0,1]. Empty message handling is a real edge case -- empty messa
- `GetSpamProbability_MessageWithoutFrom_ReturnsSpamProbability` — Current test asserts result not null and probability in [0,1]. A message without a sender is a defensive edge case that 
- `GetSpamProbability_MessageWithoutText_ReturnsSpamProbability` — Current test asserts result not null and probability in [0,1]. A message with null Text is a real edge case (system mess
- `GetSpamProbability_SpamMessage_ReturnsSpamProbability` — Current test asserts result not null and probability in [0,1]. The test constructs a spam message via TK.CreateSpamMessa
- `GetSpamProbability_ValidMessage_ReturnsSpamProbability` — Current test asserts result not null, probability in [0,1], and reason not null. The range check is meaningful but permi
- `GetSuspiciousUserSpamProbability_EmptyFirstMessages_ReturnsSpamProbability` — Current test only asserts result not null and probability in [0,1]. An empty message history is a common scenario (new u
- `GetSuspiciousUserSpamProbability_HighMimicryScore_ReturnsSpamProbability` — Current test only asserts result not null and probability in [0,1]. The test passes a high mimicry score (0.9) which sho
- `GetSuspiciousUserSpamProbability_LowMimicryScore_ReturnsSpamProbability` — Current test only asserts result not null and probability in [0,1]. The test passes a low mimicry score (0.1) which shou
- `GetSuspiciousUserSpamProbability_NullFirstMessages_ReturnsDefaultSpamProbability` — Current test only asserts result not null and probability in [0,1]. Null first messages is a real defensive edge case wh
- `GetSuspiciousUserSpamProbability_NullMessage_ReturnsDefaultSpamProbability` — Current test only asserts result not null and probability in [0,1]. These are trivially true for any implementation that
- `GetSuspiciousUserSpamProbability_NullUser_ReturnsDefaultSpamProbability` — Current test only asserts result not null and probability in [0,1]. A null user is a real defensive edge case -- the met
- `GetSuspiciousUserSpamProbability_ValidParameters_ReturnsSpamProbability` — Current test asserts result not null, probability in [0,1], and reason not null. This is the happy path test for the sus
- `MarkUserOkay_ThenGetAttentionBaitProbability_ReturnsCachedResult` — Current test only asserts result not null and SpamProbability not null. It never verifies that MarkUserOkay actually cha
- `MarkUserOkay_ValidUserId_MarksUserAsOkay` — Current test uses Assert.Pass which provides zero behavioral coverage. The MarkUserOkay behavior is a real bot contract:
- `GetSpamProbability_WithEmptyMessage_ReturnsDefaultProbability` — The test asserts a specific edge case: empty messages get default probability 0.0. This is a real contract about how the
- `GetBotChatMemberAsync_WhenNotCached_ReturnsFromApiAndCaches` — The caching behavior is a real contract worth preserving. The test is weakened by a brittle log assertion matching Russi
- `IsBotAdminAsync_WhenExceptionOccurs_ReturnsFalseAndLogsWarning` — Current test uses fragile mock verification on ILogger.Log with a Russian-language string match, which is brittle and ti
- `IsBotAdminAsync_WithDifferentStatuses_ReturnsExpectedResult` — Current test has two issues: (1) the Creator/ChatMemberOwner case expects false, which is likely incorrect since the cha
- `IsSilentModeAsync_ForRegularChats_ReturnsExpectedResult` — Tests real BotPermissionsService.IsSilentModeAsync behavior with mocked Telegram client, which is a valid seam. However,
- `IsSilentModeAsync_WhenChatGetFails_ReturnsFalseAndLogsWarning` — The graceful error handling behavior is important (bot should not crash when Telegram API fails). However, the log asser
- `CreateCaptchaAsync_UserWithInappropriateName_UsesGenericName` — Current test only asserts result not null. The comment admits the name sanitization is checked indirectly through servic
- `CreateCaptchaAsync_UserWithInappropriateUsername_UsesGenericName` — Current test only asserts result not null. The comment admits the username sanitization is checked indirectly through se
- `ValidateCaptchaAsync_ExpiredCaptcha_ReturnsFalse` — Current test is misleading: it is named 'ExpiredCaptcha' but does not test expiration at all. It sends wrong answer 999 
- `Allow_Returns_LogOnly` — Assertion only checks effects.Length==1, which provides no confidence that the effect is actually a LogOnly effect. If t
- `Ban_Returns_BanEffect` — Assertion only checks effects.Length==1, providing no confidence that the effect is a BanEffect. If the builder returns 
- `Delete_Returns_DeleteEffect` — Assertion only checks effects.Length==1, providing no confidence that the effect is a DeleteEffect. If the builder retur
- `Report_Returns_ReportEffect` — Assertion only checks effects.Length==1, providing no confidence that the effect is a ReportEffect. If the builder retur
- `Constructor_CreatesDataDirectory_WhenNotExists` — Test has dead code (tempDir/testDataDir created then unused), relies on relative path 'data' in the working directory, a
- `EnsureChatAsync_BotThrowsException_HandlesGracefully` — Current test uses DoesNotThrowAsync which is a weak assertion -- it only confirms no exception escapes but does not veri
- `EnsureChatAsync_ExistingChat_UpdatesTitle` — Current test uses Assert.Pass after calling the method twice, providing no assertion about whether the title was actuall
- `EnsureChatAsync_NewChat_AddsChatToStats` — Current test uses Assert.Pass after calling the method, which only verifies no exception was thrown. The comment admits 
- `GenerateGlobalJson_ValidData_GeneratesJsonFile` — Same issues as GenerateHtml test: real file system IO with relative path 'data/global_stats.json', fragile and non-isola
- `GenerateHtml_ValidData_GeneratesHtmlFile` — Test has real file system IO using relative path 'data/stats.html', which makes it fragile and non-isolated from other t
- `UpdateAllMembersAsync_BotThrowsException_HandlesGracefully` — Current test uses DoesNotThrowAsync which is weak -- it only confirms no exception escapes but does not verify that non-

## `Pure (IAiChecks seam)` ← `Pure` (6 tests)

- `GetAttentionBaitProbability_WithValidUser_ReturnsSpamPhotoBio` — Assertions only verify return type (SpamPhotoBio) and non-null fields, not actual attention bait scoring logic. Under go
- `GetSpamProbability_WithLongMessage_ReturnsSpamProbability` — Assertions only check probability in [0,1] range, identical to valid input test. Long message text does not produce diff
- `GetSpamProbability_WithSpecialCharacters_ReturnsSpamProbability` — Assertions only check probability in [0,1] range, identical to the valid input test pattern. Special characters in messa
- `GetSuspiciousUserSpamProbability_WithEmptyMessages_ReturnsSpamProbability` — Tests graceful handling of empty message list input, which is a valid edge case contract. Assertions only check probabil
- `GetSuspiciousUserSpamProbability_WithNullMessages_ReturnsSpamProbability` — Tests null safety for the firstMessages parameter, which is a valid edge case contract. Assertions only verify probabili
- `GetSuspiciousUserSpamProbability_WithValidInput_ReturnsSpamProbability` — Only verifies return type and trivial probability range [0,1], which adds no signal beyond compile-time type checking. U

## `UpdateDispatcher` ← `Multiple` (1 tests)

- `HandleAsync_CancellationToken_RespectsCancellation` — Current test uses Assert.Pass() after calling HandleAsync with a cancelled token, which does not verify cancellation is 

## `UserBanService` ← `Multiple` (4 tests)

- `HandleBlacklistBan_WhenUserInBlacklist_BansUser` — Current test verifies a meaningful blacklist ban contract but goes through full MessageHandler with heavy legacy factory
- `WhenAiRejectsMlSuspicion_ShouldNotCallAutoBanAsync` — Tests that low-confidence AI results do not trigger bans, a valid negative case for false positive prevention. Current t
- `WhenRepeatedViolations_ShouldCallAutoBanAsync` — Tests the critical repeated-violation-ban threshold, a core bot contract. Current test has 65 lines of fragile manual mo
- `HandleUserMessageAsync_WithDeleteMessage_CallsDeleteAndReportMessage` — Heavy mock setup across ModerationFacade, MessageService, and Bot, but only verifies that HandleUserMessageAsync on the 

## `UserBanService` ← `TestInfrastructure` (2 tests)

- `BanAndCleanupUserAsync_ValidUser_ReturnsTrue` — Current test asserts against FakeModerationService.BanAndCleanupUserAsync (test infrastructure). It sets up a mock for B
- `UnrestrictAndApproveUserAsync_ValidUser_ReturnsTrue` — Current test asserts against FakeModerationService.UnrestrictAndApproveUserAsync (test infrastructure). The fake always 

## `UserBanService` ← `UserBanService` (10 tests)

- `WhenChannelAutoBanDisabled_ShouldNotCallAutoBanChannelAsync` — Current test is bad: relies on legacy MessageHandlerTestFactory, uses mock_verify with Times.Never which is fragile mock
- `WhenChannelSendsMessage_ShouldCallAutoBanChannelAsync` — Current test is bad: relies on legacy MessageHandlerTestFactory with broad setup, uses mock_verify on ChannelModerationS
- `BanAndCleanupUserAsync_ValidUser_BansAndCleansUpSuccessfully` — The test verifies coordination between ban and message deletion through mock verification (Verify calls). While the asse
- `BanAndCleanupUserAsync_WithoutMessageId_OnlyBansUser` — Similar to the previous test but for the no-message-id path. Uses mock Verify to confirm DeleteMessageByIdAsync is never
- `Given_SuspiciousUserWithLongName_When_BanAttempted_Then_UserGetsBanned` — Current test uses mock choreography (Verify BanChatMember called once with correct chat/user IDs). The assertion is stru
- `AutoBanChannel_ValidMessage_BansChannelAndSendsNotification` — Current test verifies three meaningful interactions: DeleteMessage with message.MessageId, BanChatSenderChat with correc
- `AutoBan_ValidChat_BansUserAndForwardsMessage` — Test name promises ban and forward verification but assertion only checks notification type, missing critical side effec
- `BanBlacklistedUser_ExceptionOccurs_LogsWarning` — The test verifies important exception propagation behavior in UserBanService. However it has a real_env_or_api smell fla
- `BanBlacklistedUser_ValidChat_BansUserFor4Hours` — Current test verifies three meaningful interactions: BanChatMember with correct 4-hour duration (using It.Is for time ra
- `HandleBlacklistBan_ValidUser_BansUserAndDeletesMessage` — Current test verifies two interactions: DeleteMessage with message.MessageId and LogUserBanned with correct reason strin

## `ViolationTracker` ← `Multiple` (1 tests)

- `WhenFirstViolation_ShouldNotCallAutoBanAsync` — Tests that first violation does not trigger ban, a valid negative case complementing the repeated violations test. Curre
