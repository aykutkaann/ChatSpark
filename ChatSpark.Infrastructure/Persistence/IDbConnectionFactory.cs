
using System.Data;


namespace ChatSpark.Infrastructure.Persistence
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateAsync(CancellationToken ct = default);
    }

}
