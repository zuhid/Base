namespace Zuhid.Base;

public class NoValidator<TModel> : IValidator<TModel> where TModel : class
{
  public bool Validate(TModel model) => true;
}
