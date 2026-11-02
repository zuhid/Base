using System.Net.Mail;

namespace Zuhid.Base.Tests;

public class NotificationServiceTests : IDisposable
{
    private readonly string _pickupDir;

    public NotificationServiceTests()
    {
        _pickupDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_pickupDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_pickupDir))
        {
            Directory.Delete(_pickupDir, recursive: true);
        }
    }

    private SmtpClient CreateSmtpClient() => new()
    {
        DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
        PickupDirectoryLocation = _pickupDir
    };

    [Fact]
    public async Task SendEmail_Should_ReturnTrue()
    {
        using var smtp = CreateSmtpClient();
        var service = new NotificationService(smtp);

        var result = await service.SendEmail("from@test.com", "to@test.com", "Subject", "Body");

        Assert.True(result);
    }

    [Fact]
    public async Task SendSms_Should_DelegateToSendEmail_And_ReturnTrue()
    {
        using var smtp = CreateSmtpClient();
        var service = new NotificationService(smtp);

        var result = await service.SendSms("contact@test.com", "SMS message text");

        Assert.True(result);
    }
}
