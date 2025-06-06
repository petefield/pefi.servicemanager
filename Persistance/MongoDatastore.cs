using System.Linq.Expressions;
using MongoDB.Driver;

namespace pefi.servicemanager
{
    public class MongoDatastore(IMongoClient client) : IDataStore
    {
        private IMongoCollection<T> GetCollection<T>(string database, string collection) 
            => client.GetDatabase(database).GetCollection<T>(collection);
        
        public async Task<IEnumerable<T>> Get<T>(string database, string collection, Expression< Func<T, bool>> predicate)
        {
            var a = await GetCollection<T>(database, collection)
                .FindAsync(predicate);

            return a.ToEnumerable();
        }

        public async Task<IEnumerable<T>> Get<T>(string database, string collection) => await Get<T>(database, collection, _ => true);

        public async Task<T> Add<T>(string database, string collection, T item)
        {
            await GetCollection<T>(database, collection)
                .InsertOneAsync(item);

            return item;
        }

        public async Task Delete<T>(string database, string collection, Expression<Func<T, bool>> predicate)
        {
            await GetCollection<T>(database, collection)
                .DeleteManyAsync(predicate);

        }
    }
}
