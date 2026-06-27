# Manual Review Required

Purpose: files with contradictory/high-value human-review conflicts. Do not delete automatically.

## `ClubDoorman.Test/Unit/Services/UserBanServiceTests.Modern.cs`

- Delete candidates: `BanUserForLongName_EdgeCases_MassiveDataGeneration`.
- Human-review overlap: `BanUserForLongName_Integration_WithRealDependencies` quarantine, `BanUserForLongName_ValidChat_BansUserAndSendsNotification` keep.
- Safe split: no.
- Classification: `manual-review-required`.
- Why: same file contains keep/quarantine/high-value ban behavior. Human must decide target coverage before cleanup.

## `ClubDoorman.Test/Unit/Services/UserBanServiceTests.cs`

- Delete candidates: `BanUserForLongName_ExceptionOccurs_LogsWarning`, `BanUserForLongName_PrivateChat_LogsWarningAndReturns_Obsolete`, `BanUserForLongName_ValidChat_BansUserAndSendsNotification`, `BanUserForLongName_ValidChat_BansUserAndSendsNotification_Obsolete`.
- Human-review overlap: `BanUserForLongName_ValidChat_BansUserAndSendsNotification` delete with `high_value_delete`.
- Safe split: no.
- Classification: `manual-review-required`.
- Why: high-value delete conflict on user-ban side effects. Requires explicit human decision.
