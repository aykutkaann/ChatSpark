using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Application.Abstractions
{

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);

        Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    }

}
