# Delete After Human Review

Purpose: files with remaining delete candidates and human-review overlap that can be safely split after explicit approval of the human-review delete/anomaly items.

## Safe whole-file delete

- `ClubDoorman.Test/Unit/Services/StatisticsModuleTests.cs`
- `ClubDoorman.Test/Unit/Services/TelegramModuleTests.cs`
- `ClubDoorman.Test/Unit/Services/UserManagementModuleTests.cs`
- `ClubDoorman.Test/Unit/Services/UserManagerExtendedTests.cs`

Reason: current audited test methods in these files are delete-only. Human-review overlap is delete/anomaly or delete/duplicate only. No remaining keep/rewrite/quarantine/high-value audited tests in the same files.

## Safe partial delete

- `ClubDoorman.Test/ErrorHandlingTests.cs`
- `ClubDoorman.Test/ModerationServiceSimpleTests.cs`
- `ClubDoorman.Test/Unit/Infrastructure/TelegramBotClientWrapperTests.cs`
- `ClubDoorman.Test/Unit/Moderation/ModerationServiceTests.cs`
- `ClubDoorman.Test/Unit/Services/UserCleanupServiceTests.cs`

Reason: delete candidates are separable low-value tests. Remaining keep/rewrite tests should be left untouched.
