namespace Zuhid.Base;

public interface IValidator<TModel>
{
  // Implementations add failures to ModelState.AddModelError and return false if there is any error.
  bool Validate(TModel model);
}
