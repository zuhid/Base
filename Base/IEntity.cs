namespace Zuhid.Base;

public interface IEntity
{
  public Guid Id { get; set; }

  public Guid UpdatedById { get; set; }

  public DateTime Updated { get; set; }
}
