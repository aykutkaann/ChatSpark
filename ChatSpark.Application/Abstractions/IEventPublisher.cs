using System;
using System.Collections.Generic;
using System.Text;

namespace ChatSpark.Application.Abstractions
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default);

    }
}
