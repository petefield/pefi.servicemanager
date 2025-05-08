namespace pefi.servicemanager;

public interface IDataStore
{
    Task<T> Add<T>(string database, string collection, T item);

    Task<IEnumerable<T>> Get<T>(string database, string collection, Func<T, bool> predicate);

    Task<IEnumerable<T>> Get<T>(string database, string collection);

}
