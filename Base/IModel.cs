namespace Zuhid.Base;

public interface IModel
{
  public Guid Id { get; set; }
  public Guid UpdatedById { get; set; }
  public DateTime Updated { get; set; }
}
