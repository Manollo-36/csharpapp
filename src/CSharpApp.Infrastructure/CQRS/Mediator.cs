using Microsoft.Extensions.DependencyInjection;
using CSharpApp.Core.CQRS;

namespace CSharpApp.Infrastructure.CQRS;

/// <summary>
/// Simple mediator implementation for CQRS pattern
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.Handle));
        
        if (handleMethod == null)
            throw new InvalidOperationException($"Handle method not found for query {queryType.Name}");

        var result = handleMethod.Invoke(handler, new object[] { query, cancellationToken });
        
        if (result is Task<TResult> task)
            return await task;
            
        throw new InvalidOperationException($"Invalid return type from handler for query {queryType.Name}");
    }

    public async Task Send(ICommand command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.Handle));
        
        if (handleMethod == null)
            throw new InvalidOperationException($"Handle method not found for command {commandType.Name}");

        var result = handleMethod.Invoke(handler, new object[] { command, cancellationToken });
        
        if (result is Task task)
            await task;
        else
            throw new InvalidOperationException($"Invalid return type from handler for command {commandType.Name}");
    }

    public async Task<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));
        
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.Handle));
        
        if (handleMethod == null)
            throw new InvalidOperationException($"Handle method not found for command {commandType.Name}");

        var result = handleMethod.Invoke(handler, new object[] { command, cancellationToken });
        
        if (result is Task<TResult> task)
            return await task;
            
        throw new InvalidOperationException($"Invalid return type from handler for command {commandType.Name}");
    }
}