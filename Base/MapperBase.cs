namespace Zuhid.Base;

public class MapperBase<TEntity, TModel>
{
  public virtual TEntity GetEntity(TModel model)
  {
    var entity = Activator.CreateInstance<TEntity>();
    var modelProperties = typeof(TModel).GetProperties();
    foreach (var entityProperty in typeof(TEntity).GetProperties())
    {
      var modelProperty = modelProperties.FirstOrDefault(p => p.Name == entityProperty.Name);
      if (modelProperty != null && entityProperty.CanWrite)
      {
        entityProperty.SetValue(entity, modelProperty.GetValue(model));
      }
    }
    return entity;
  }

  public List<TEntity> GetEntityList(List<TModel> models) => [.. models.Select(GetEntity)];
}
