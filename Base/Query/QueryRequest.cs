namespace Zuhid.Base.Query;

public class QueryRequest
{
  // Upper bound applied to Take; configured from SettingBase.MaxTake by AddServices.
  public static int MaxTake { get; set; } = 100;

  private int take = 50;

  public List<FilterRule> Filters { get; set; } = [];
  public List<SortRule> Sorts { get; set; } = [];
  public int Skip { get; set; }
  public int Take { get => Math.Min(take, MaxTake); set => take = value; }
}
