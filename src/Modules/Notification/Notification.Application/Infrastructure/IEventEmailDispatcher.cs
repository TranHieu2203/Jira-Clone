namespace Notification.Application.Infrastructure;

public interface IEventEmailDispatcher
{
    Task DispatchAsync(string templateKey, IReadOnlyCollection<Guid> recipientUserIds, IReadOnlyDictionary<string, string> args, CancellationToken ct = default);
}

