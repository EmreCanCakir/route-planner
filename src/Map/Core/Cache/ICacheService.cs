namespace Map.Core.Cache
{
    public interface ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellation = default) where T : class;

        public Task SetAsync<T>(string key, T value, CancellationToken cancellation = default);

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        public Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);
    }
}
