using System.Net.Mail;

namespace Zuhid.Base;

public class NotificationService(SmtpClient smtpClient) : INotificationService
{
  public async Task<bool> SendEmail(string from, string to, string subject, string message)
  {
    await smtpClient.SendMailAsync(from, to, subject, message);
    return true;
  }

  public async Task<bool> SendSms(string phone, string message)
  {
    return await SendEmail(phone, phone, message, message);
  }
}
