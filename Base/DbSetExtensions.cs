using Microsoft.EntityFrameworkCore;

namespace Zuhid.Base;

public static class DbSetExtensions
{
  public static void LoadCsvData<T>(this DbSet<T> dbSet, string fileName) where T : class
  {
    var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
    var data = CsvSerializer.Load<T>(filePath);
    if (data.Count > 0)
    {
      dbSet.AddRange(data);
    }
  }
}
