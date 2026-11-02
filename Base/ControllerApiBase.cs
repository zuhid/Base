using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Zuhid.Base.Query;

namespace Zuhid.Base;

public abstract class ControllerApiBase<TContext, TRepository, TEntity, TModel, TMapper, TValidator>(TRepository repository, TMapper mapper, TValidator validator) : ControllerBase
    where TContext : DbContext
    where TRepository : RepositoryBase<TContext, TEntity, TModel>
    where TEntity : class, IEntity
    where TModel : class, IModel
    where TMapper : MapperBase<TEntity, TModel>
    where TValidator : IValidator<TModel>
{
  protected TRepository Repository { get; } = repository;

  protected TMapper Mapper { get; } = mapper;

  protected TValidator Validator { get; } = validator;

  // The authenticated user's id from the JWT nameid claim; entities are always
  // stamped from the token, never from the client-supplied model. Inbound claim mapping
  // folds both sub and nameid into ClaimTypes.NameIdentifier (and may be disabled entirely),
  // so take the first candidate that is actually a Guid rather than the first by position.
  protected Guid UserId => (User?.Claims ?? [])
      .Where(claim => claim.Type is ClaimTypes.NameIdentifier or "nameid")
      .Select(claim => Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty)
      .FirstOrDefault(id => id != Guid.Empty);

  protected async Task<List<TModel>> GetCore() => await Repository.Get().ConfigureAwait(false);

  protected async Task<List<TModel>> GetCore(Guid id) => await Repository.Get(id).ConfigureAwait(false);

  protected async Task<List<TModel>> GetCore(QueryRequest request) => await Repository.Get(request).ConfigureAwait(false);

  protected async Task<List<OptionModel>> GetOptionsCore() => await Repository.GetOptions().ConfigureAwait(false);

  // Streaming variant of GetCore(QueryRequest): writes ALL matching rows (request.Skip and
  // Take are ignored) to the response as NDJSON — one JSON array per MaxTake-sized batch
  // per line — so the client can parse each batch as soon as its line arrives.
  protected async Task GetStreamCore(QueryRequest request, CancellationToken cancellationToken = default)
  {
    Response.ContentType = "application/x-ndjson";
    await foreach (var batch in Repository.GetStream(request, cancellationToken).ConfigureAwait(false))
    {
      await Response.WriteAsync(JsonSerializer.Serialize(batch, JsonSerializerOptions.Web) + '\n', cancellationToken).ConfigureAwait(false);
      // await Task.Delay(100, cancellationToken).ConfigureAwait(false);
    }
  }

  protected async Task<int> CountCore(QueryRequest request) => await Repository.Count(request).ConfigureAwait(false);

  protected async Task<UpdatedResponse?> AddCore(TModel model)
  {
    if (!Validator.Validate(model))
    {
      return null;
    }
    var entity = Mapper.GetEntity(model);
    entity.Updated = DateTime.UtcNow;
    entity.UpdatedById = UserId;
    return await Repository.Add(entity).ConfigureAwait(false);
  }

  protected async Task<UpdatedResponse?> UpdateCore(TModel model)
  {
    if (!Validator.Validate(model))
    {
      return null;
    }
    var entity = Mapper.GetEntity(model);
    entity.Updated = DateTime.UtcNow;
    entity.UpdatedById = UserId;
    return await Repository.Update(entity).ConfigureAwait(false);
  }

  protected async Task DeleteCore(Guid id) => await Repository.Delete(id).ConfigureAwait(false);
}
