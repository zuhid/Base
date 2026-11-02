namespace Zuhid.Base;

public interface INotificationService
{
  Task<bool> SendEmail(string from, string to, string subject, string message);
  Task<bool> SendSms(string phone, string message);
}
