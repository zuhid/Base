namespace Zuhid.Base;

// Bare-minimum shape for dropdowns and autocompletes: the id plus the display text of an
// active record. Served by the options endpoints instead of the full model.
public class OptionModel
{
  public Guid Id { get; set; }
  public required string Text { get; set; }
}
