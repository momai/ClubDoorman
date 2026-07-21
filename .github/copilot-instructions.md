# ClubDoorman/Spampyre Development Guide for Coding Agents

## Project Overview

**ClubDoorman** (also known as "Spampyre") is a sophisticated Telegram anti-spam bot written in C# (.NET 9.0). It's a fork of the original ClubDoorman project with enhanced protection features including ML-based spam detection, AI-powered profile analysis, captcha system, and multi-layered user verification.

### Key Features
- Machine Learning spam classification
- AI-powered user profile analysis via OpenRouter API
- Emoji-based captcha system for new users
- Integration with spam databases (lols.bot)
- Trusted user management with JSON storage
- Multi-language support (primarily Russian with English documentation)
- Docker containerization and automated deployment

### Repository Statistics
- **Language**: C# (.NET 9.0)
- **Architecture**: Worker Service (hosted background service)
- **Lines of Code**: ~50,000+ (estimated)
- **Test Coverage**: 940+ tests with comprehensive unit, integration, and E2E testing
- **Dependencies**: 20+ NuGet packages including Telegram.Bot, Microsoft.ML, Serilog

## Critical Build Requirements

### .NET 9.0 SDK Installation (MANDATORY)
**IMPORTANT**: This project requires .NET 9.0 SDK. The default .NET 8.0 will fail.

```bash
# Install .NET 9.0 using the provided script
chmod +x scripts/dotnet-install.sh
./scripts/dotnet-install.sh --version latest --channel 9.0
export PATH="/home/runner/.dotnet:$PATH"

# Verify installation
dotnet --version  # Should show 9.0.x
```

### Build Commands (Always run in sequence)
```bash
# 1. ALWAYS restore dependencies first
export PATH="/home/runner/.dotnet:$PATH"
dotnet restore

# 2. Build the solution
dotnet build --configuration Release --no-restore

# 3. Run tests (excluding real API calls)
dotnet test ClubDoorman.Test --filter "Category!=real-api&Category!=BDD&Category!=disabled&Category!=demo" --verbosity quiet --no-restore
```

**Build Time**: Restore takes ~70 seconds, build takes ~20 seconds, tests take ~30 seconds.

## Environment Configuration

### Required Environment Variables
Create a `.env` file in the `ClubDoorman/` directory:
```bash
# MANDATORY - Bot will not start without these
DOORMAN_BOT_API=your-telegram-bot-token
DOORMAN_ADMIN_CHAT=your-admin-chat-id

# OPTIONAL - AI Analysis (OpenRouter)
DOORMAN_OPENROUTER_API=your-openrouter-api-key

# OPTIONAL - Advanced features
DOORMAN_SUSPICIOUS_DETECTION_ENABLE=true
DOORMAN_MIMICRY_THRESHOLD=0.7
DOORMAN_CLUB_SERVICE_TOKEN=your-club-service-token
```

### Test Environment Variables
For testing without real APIs, use these safe values:
```bash
export DOORMAN_BOT_API="https://api.telegram.org"
export DOORMAN_ADMIN_CHAT="123456789"
export DOORMAN_OPENROUTER_API="test-api-key-for-tests-only"
```

## Project Architecture

### Solution Structure
```
ClubDoorman/
├── ClubDoorman/                 # Main application
│   ├── Program.cs              # Entry point with DI configuration
│   ├── Worker.cs               # Background service host
│   ├── Handlers/               # Telegram update handlers
│   ├── Services/               # Business logic services
│   ├── Models/                 # Data models and DTOs
│   ├── Infrastructure/         # Configuration and utilities
│   └── data/                   # ML models and static data
├── ClubDoorman.Test/           # Comprehensive test suite
│   ├── Unit/                   # Unit tests
│   ├── Integration/            # Integration tests
│   ├── BDD/                    # Behavior-driven tests
│   └── TestKit/                # Test infrastructure
├── scripts/                    # Development scripts
├── .github/workflows/          # CI/CD pipelines
└── docs/                       # Documentation
```

### Key Service Modules
- **MessageHandler**: Primary message processing
- **ModerationService**: Spam detection and ML classification
- **UserManager**: User state and approval management
- **CaptchaService**: New user verification
- **StatisticsService**: Analytics and reporting
- **AiChecks**: AI-powered profile analysis
- **UserBanService**: Ban management and enforcement

## Testing Infrastructure

### Test Categories and Execution
```bash
# Standard unit tests (fastest)
dotnet test --filter "Category!=real-api&Category!=BDD&Category!=disabled&Category!=demo"

# Integration tests (require environment setup)
./scripts/run_integration_tests.sh

# E2E tests (require real API keys)
./scripts/run_e2e_tests.sh

# BDD tests (behavioral scenarios)
dotnet test --filter "Category=BDD"

# All tests without demos
./scripts/run_tests_without_demos.sh
```

### Test Configuration Notes
- Demo tests are excluded by default (Category!=demo)
- Real API tests require valid tokens
- Test timeout configured in ClubDoorman.Test/.runsettings
- Parallel execution enabled (MaxCpuCount=4)

## CI/CD Workflows

### GitHub Actions Pipelines
1. **ci.yml**: PR validation (build + basic tests)
2. **deploy.yml**: Docker build/push on `next*` branches
3. **e2e-tests.yml**: Full E2E testing with secrets

### Deployment Process
- Builds Docker image with .NET 9.0 runtime
- Pushes to GitHub Container Registry (ghcr.io)
- Auto-deploys to production server via SSH
- Logs captured as build artifacts

## Common Development Patterns

### Service Registration (Program.cs)
Services are registered via extension methods:
```csharp
services.AddConfigurationServices();
services.AddTelegramServices();
services.AddModerationServices();
services.AddUserManagementServices();
```

### Dependency Injection Pattern
Most services follow this pattern:
```csharp
public class SomeService(ILogger<SomeService> logger, IDependency dep)
{
    private readonly ILogger<SomeService> _logger = logger;
    private readonly IDependency _dependency = dep;
}
```

### Error Handling
- All services use Serilog for structured logging
- Exceptions are logged but don't crash the worker
- Telegram API failures are handled gracefully

## Troubleshooting Guide

### Common Build Issues
1. **"NETSDK1045: .NET SDK does not support targeting .NET 9.0"**
   - Solution: Install .NET 9.0 SDK using `./scripts/dotnet-install.sh`

2. **NuGet restore timeouts**
   - Solution: Increase timeout, check network connectivity
   - May take 60-70 seconds on first run

3. **Test failures with "real-api" category**
   - Solution: Exclude real API tests or set up valid tokens

### Performance Warnings
- Package vulnerabilities in SixLabors.ImageSharp (moderate severity)
- 160+ build warnings (mostly CS0105 duplicate using statements)
- These are non-blocking and don't affect functionality

### Security Considerations
- Never commit real API tokens
- Use `.env` files for local development
- Production secrets managed via GitHub Environment variables

## File Reference

### Configuration Files
- `ClubDoorman.sln`: Solution file
- `ClubDoorman/ClubDoorman.csproj`: Main project file (.NET 9.0 Worker)
- `ClubDoorman.Test/ClubDoorman.Test.csproj`: Test project (NUnit, SpecFlow)
- `.editorconfig`: Code style configuration
- `stryker-config.json`: Mutation testing configuration

### Scripts Directory
- `scripts/run_tests.sh`: Standard test execution
- `scripts/run_e2e_tests.sh`: End-to-end testing
- `scripts/coverage.sh`: Test coverage analysis
- `scripts/check_diff.sh`: Development utility for comparing changes

### Data Files
- `ClubDoorman/data/stop-words.txt`: Spam keyword dictionary
- `ClubDoorman/data/*.ml`: Machine learning model files
- `appsettings.json`: Application configuration

### Docker Configuration
- `Dockerfile`: Multi-stage build with .NET 9.0
- `ClubDoorman/Dockerfile`: Alternative container build
- `docker-compose.yml`: Local development setup

## Agent Instructions
0. **ALL** new branches for development must be based on the `next-lab` branch.
1. **ALWAYS** use .NET 9.0 SDK before any dotnet commands
2. **ALWAYS** run `dotnet restore` before building
3. **EXCLUDE** real-api, BDD, disabled, and demo tests in standard runs
4. **USE** provided test scripts for specific scenarios
5. **CHECK** .env configuration before running integration tests
6. **VERIFY** build warnings don't introduce new issues
7. **FOLLOW** existing service registration patterns in Program.cs
8. **MAINTAIN** current logging and error handling approaches

Trust these instructions. Only search for additional information if something is incomplete or incorrect.