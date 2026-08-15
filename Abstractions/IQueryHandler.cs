namespace Heimdall.Abstractions;

/// <summary>
/// Contrato para o manipulador responsável por processar a query <typeparamref name="TQuery"/>
/// e retornar a resposta <typeparamref name="TResponse"/>.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
