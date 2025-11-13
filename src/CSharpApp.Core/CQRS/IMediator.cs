namespace CSharpApp.Core.CQRS;

/// <summary>
/// Mediator interface for sending queries and commands
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Send a query and get a result
    /// </summary>
    Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a command
    /// </summary>
    Task Send(ICommand command, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a command and get a result
    /// </summary>
    Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}