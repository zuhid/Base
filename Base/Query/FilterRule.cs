namespace Zuhid.Base.Query;

public class FilterRule
{
  public string FilterBy { get; set; } = "";
  public string? FilterValue { get; set; }
  public FieldOperator FilterOperator { get; set; }
}
