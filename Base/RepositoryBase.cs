using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using Zuhid.Base.Query;

namespace Zuhid.Base;

public abstract class RepositoryBase<TContext, TEntity, TModel>(TContext context)
    where TContext : DbContext
    where TEntity : class, IEntity
    where TModel : class, IModel
{
  protected TContext Context { get; } = context;
  protected abstract IQueryable<TModel> Query { get; }

  // Bare-minimum rows for dropdowns/autocompletes: id + display text of active records
  // only, unordered — the client sorts. Repositories without a dropdown use case keep
  // this default, which reports the operation as unsupported.
  public virtual Task<List<OptionModel>> GetOptions() => throw new NotSupportedException($"{GetType().Name} does not support {nameof(GetOptions)}.");

  // The empty id returns every record (routes served this way are cached whole client
  // side); a real id returns the single matching record.
  public async Task<List<TModel>> Get(Guid id = default) => id == Guid.Empty
      ? await Query.ToListAsync()
      : await Query.Where(n => n.Id.Equals(id)).ToListAsync();

  // Id is always appended as the final sort so Skip/Take page over a deterministic
  // order (EF warns and may return unpredictable pages otherwise).
  public async Task<List<TModel>> Get(QueryRequest request) => await Query
      .Filter(request.Filters)
      .Sort(request.Sorts)
      .Sort(nameof(IModel.Id))
      .Skip(request.Skip)
      .Take(request.Take)
      .ToListAsync();

  // Streams every row matching the request's filters (Skip and Take are ignored) as batches of
  // up to MaxTake rows, read from a single query kept open for the whole enumeration. Id is
  // always appended as the final sort so the stream order stays deterministic.
  public async IAsyncEnumerable<TModel[]> GetStream(QueryRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var batches = Query
        .Filter(request.Filters)
        .Sort(request.Sorts)
        .Sort(nameof(IModel.Id))
        .AsAsyncEnumerable()
        .Chunk(QueryRequest.MaxTake);
    await foreach (var batch in batches.WithCancellation(cancellationToken))
    {
      yield return batch;
    }
  }

  public async Task<int> Count() => await Context.Set<TEntity>().CountAsync();

  public async Task<int> Count(QueryRequest request) => await Query.Filter(request.Filters).CountAsync();

  public virtual async Task<UpdatedResponse> Add(TEntity entity)
  {
    try
    {
      Context.Set<TEntity>().Add(entity);
      await Context.SaveChangesAsync();
      return new UpdatedResponse { Updated = entity.Updated };
    }
    catch (Exception ex)
    {
      ex.Data.Add("entity", entity);
      throw;
    }
  }

  public virtual async Task<UpdatedResponse> Update(TEntity entity)
  {
    try
    {
      Context.Set<TEntity>().Update(entity);
      await Context.SaveChangesAsync();
      return new UpdatedResponse { Updated = entity.Updated };
    }
    catch (Exception ex)
    {
      ex.Data.Add("entity", entity);
      throw;
    }
  }

  public virtual async Task Delete(Guid id)
  {
    await Context.Set<TEntity>().Where(e => e.Id.Equals(id)).ExecuteDeleteAsync();
  }
}
