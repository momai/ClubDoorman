# Test Quarantine Decisions

Scope: the seven quarantine records in the final audit summary. No real Telegram or AI calls are enabled by these changes.

| Test | Decision | Evidence / replacement |
|---|---|---|
| `AiAnalysisTests.E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis` | Delete | The test only asserted that the handler and fake output were non-empty. It did not observe deduplication. The current deduplication/cache boundary is not exposed by this handler setup, so keeping it would create false confidence. |
| `AiAnalysisTests.E2E_AI_Analysis_WithRealApi_ShouldWork` | Delete | Ignored real-API smoke test with weak range assertions. It is not a normal CI contract and has no stable HTTP seam in the current code. |
| `AiChecksPhotoLoggingTest.GetAttentionBaitProbability_WithRealPhoto_ShouldAnalyzePhotoInAPI` | Delete | Depends on `.env`, a hardcoded absolute photo path, and a real API. Photo notification delivery is covered at the dispatcher seam; real AI photo quality is not a CI contract. |
| `MessageHandlerBanTests.WhenAiConfirmsMlSuspicion_ShouldCallAutoBanAsync` | Delete | Ignored legacy test asserts direct bot calls through a mixed handler world. The upstream AI/ML branch it describes is unavailable; existing `UserBanService` side-effect tests cover supported ban behavior. |
| `SimpleE2ETests.E2E_CompleteAIAnalysis_ShouldWorkEndToEnd` | Delete | Broad test combines photo AI, text classification, local file I/O, and environment/API assumptions with weak assertions. Focused AI and notification seams replace its useful parts. |
| `AiChecksTests.GetSpamProbability_WithValidMessage_ReturnsSpamProbability` | Rewrite | Construct `AiChecks` with `AppConfigTestFactory.CreateWithoutAi()`. The test now verifies the safe bounded result without external calls. |
| `UserBanServiceTestsModern.BanUserForLongName_Integration_WithRealDependencies` | Already deleted | The audited `UserBanServiceTests.Modern.cs` file is no longer present after the cleanup series; supported long-name contracts remain in `UserBanServiceTests`. |

## Follow-up

- A real AI HTTP contract test would require an injectable HTTP/provider seam. That is outside this cleanup slice and should not be recreated with a broad test harness.
- Repeated-message AI deduplication should only receive a test after its owning seam is identified and observable without external calls.

## BDD Feature Disposition

Date: 2026-07-18

Scope: every current `.feature` file under `ClubDoorman.Test/Features`. These decisions record whether the executable feature provides trustworthy evidence. They do not delete tests or claim replacement coverage.

| Feature | Decision | Evidence |
|---|---|---|
| `ModerationFlow` | DELETE | Most bindings in `ModerationFlowSteps` are empty TODO methods. The scenarios do not execute or observe moderation order, deletion, logging, model training, or forward handling. |
| `StatisticsAndCommands` | DELETE | Command execution and one statistics assertion binding are empty; the remaining statistics bindings create `mock_stats` and fake sent messages, then assert that binding-created state exists. No production statistics, scheduling, or authorization behavior is observed. |
| `AiChecksPhotoLoggingTest` | DELETE | The Given and When bindings are empty, and the Then binding only asserts `true.Should().BeTrue()`. No photo or AI API behavior is executed. |
| `SpamHamCommands` | DELETE | The scenarios route through a broad handler test factory, but every outcome assertion only checks that no exception was captured. They do not observe dataset updates, access denial, missing-reply handling, or command output. |
| `AiAnalysis` | DELETE | The bindings synthesize restrictions, notifications, approvals, bans, deletions, and channel outcomes in test state or reduce assertions to no captured exception. They do not execute the owning production orchestration for the named outcomes. |
| `CaptchaSystem` | DELETE | Timeout is represented by `Task.Delay(100)` without invoking expiry behavior; other outcomes are partly injected through reflection or recorded directly on a separate fake. The feature does not prove timeout, ban, deletion, logging, or silent-mode behavior. |
| `PermissionsAndQuietMode` | DELETE | The bindings assign permission, quiet-mode, and captcha-disabled values to `ScenarioContext`, add their own fake captcha message, and assert those same values. No production permission or quiet-mode decision is exercised. |
| `CheckCommand` | KEEP | `CheckCommandSteps` constructs a concrete `CheckCommandHandler`, places it behind a concrete `CommandRouter`, routes the command through `MessageHandler`, captures the produced notification reason, and asserts formatted `/check` output including emoji, stop-word, and classifier fields. |

### Coverage Meaning

- `DELETE` means the executable feature creates false confidence because it does not prove its named behavior.
- `DELETE` does not mean the named behavior is covered elsewhere. Any valuable missing contract requires a later focused test at its owning production seam.
- Deleting `CaptchaSystem` does not claim captcha timeout coverage. Scheduler ownership and controllable timeout testing remain a later production slice.
- `KEEP` applies only to the concrete behavior evidenced above; it is not a claim that every possible `/check` behavior is covered.
- Actual deletion remains limited to separately approved follow-up slices.
