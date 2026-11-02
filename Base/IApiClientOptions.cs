namespace Zuhid.Base;

public interface IApiClientOptions
{
  string BaseUrl { get; set; }
  string Authorization { get; }
}
