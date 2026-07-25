using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Handlers;
using ClubDoorman.Services.Logging;
using ClubDoorman.Models.Logging;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Services.UserManagement;
using ClubDoorman.Services.BadMessage;
using ClubDoorman.Services.Statistics;
using ClubDoorman.Services.AI;
using ClubDoorman.Services.Moderation;
using ClubDoorman.Services.Messaging;
using ClubDoorman.Services.Violation;
using ClubDoorman.Services.UserBan;
using ClubDoorman.Services;
using ClubDoorman.Services.Telegram;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Unit.Handlers;

[TestFixture]
[Category("golden")]
public class CallbackQueryHandlerSemanticsTests
{
    private static IOptions<LoggingFlagsOptions> Flags(string basePath) => Options.Create(new LoggingFlagsOptions
    {
        GoldenMasterEnabled = true,
        GoldenSampleRate = 1.0,
        GoldenBasePath = basePath,
        GoldenDeterministicIds = true,
        GoldenFixedDateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd")
    });

    private CallbackQueryHandler BuildHandler(IOptions<LoggingFlagsOptions> flags)
    {
        var recorder = new GoldenMasterRecorder(flags, new NullLogger<GoldenMasterRecorder>());
        var bot = new Mock<ITelegramBotClientWrapper>();
        bot.SetupGet(x => x.BotId).Returns(999);
        var captcha = new Mock<ICaptchaService>();
        captcha.Setup(x => x.GenerateKey(It.IsAny<long>(), It.IsAny<long>())).Returns("k");
        captcha.Setup(x => x.GetCaptchaInfo("k")).Returns(TestDataFactory.CreateValidCaptchaInfo());
        captcha.Setup(x => x.ValidateCaptchaAsync("k", It.IsAny<int>())).ReturnsAsync((string k, int answer) => answer == 1);
        var userManager = new Mock<IUserManager>();
        var badMsg = new Mock<IBadMessageManager>();
        var stats = new Mock<IStatisticsService>();
        var ai = new Mock<IAiChecks>();
        var moderation = new Mock<IModerationService>();
        var msgService = new Mock<IMessageService>();
        msgService.Setup(x => x.SendWelcomeMessageAsync(It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Message?)null);
        var appConfig = new Mock<ClubDoorman.Services.Core.Configuration.IAppConfig>();
        // Ensure captcha test chats are NOT treated as admin/log chats
        appConfig.SetupGet(x => x.AdminChatId).Returns(-9999);
        appConfig.SetupGet(x => x.LogAdminChatId).Returns(-9999);
        var violationTracker = new ViolationTracker(new NullLogger<ViolationTracker>(), appConfig.Object);
        var userBan = new Mock<IUserBanService>();
        var logChat = new Mock<ILogChatService>();
        var eventsPub = new GoldenMasterModerationEventPublisher(recorder, new NullLogger<GoldenMasterModerationEventPublisher>());
        return new CallbackQueryHandler(
            bot.Object,
            captcha.Object,
            userManager.Object,
            badMsg.Object,
            stats.Object,
            ai.Object,
            moderation.Object,
            msgService.Object,
            violationTracker,
            userBan.Object,
            logChat.Object,
            new Mock<IAdminActionStore>().Object,
            new NullLogger<CallbackQueryHandler>(),
            recorder,
            eventsPub,
            appConfig.Object
        );
    }

    private static Update BuildCaptchaUpdate(long chatId, long userId, int answer)
    {
        return new Update
        {
            Id = 42,
            CallbackQuery = new CallbackQuery
            {
                Id = Guid.NewGuid().ToString(),
                From = new User { Id = userId, FirstName = "User" },
                Data = $"cap_{userId}_{answer}",
                Message = new Message
                {
                    Chat = new Chat { Id = chatId, Type = ChatType.Supergroup, Title = "CaptchaChat" }
                }
            }
        };
    }

    [Test]
    public async Task CaptchaFail_EmitsSemanticRule()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_cb_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var flags = Flags(basePath);
        var handler = BuildHandler(flags);
        var update = BuildCaptchaUpdate(-2001, 5001, 99); // wrong answer -> returns false

        await handler.HandleAsync(update, CancellationToken.None);

        var dayDir = Path.Combine(basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        var semFiles = Directory.GetFiles(dayDir, "*.sem.json");
        Assert.That(semFiles.Length, Is.GreaterThanOrEqualTo(1));
        var last = semFiles.OrderBy(f => f).Last();
        using var doc = JsonDocument.Parse(File.ReadAllText(last));
        Assert.That(doc.RootElement.GetProperty("ruleCode").GetString(), Is.EqualTo("CaptchaFail"));
    }

    [Test]
    public async Task CaptchaSuccess_EmitsSemanticRule()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "gm_cb_semantics_" + Path.GetRandomFileName());
        Directory.CreateDirectory(basePath);
        var flags = Flags(basePath);
        var handler = BuildHandler(flags);
        var update = BuildCaptchaUpdate(-2002, 5002, 1); // correct answer

        await handler.HandleAsync(update, CancellationToken.None);

        var dayDir = Path.Combine(basePath, DateTime.UtcNow.ToString("yyyy-MM-dd"));
        var semFiles = Directory.GetFiles(dayDir, "*.sem.json");
        Assert.That(semFiles.Length, Is.GreaterThanOrEqualTo(1));
        var last = semFiles.OrderBy(f => f).Last();
        using var doc = JsonDocument.Parse(File.ReadAllText(last));
        Assert.That(doc.RootElement.GetProperty("ruleCode").GetString(), Is.EqualTo("CaptchaSuccess"));
    }
}
