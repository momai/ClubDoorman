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
