namespace Heimdall.Abstractions;

/// <summary>
/// Marca uma requisição do tipo Query que produz uma resposta do tipo <typeparamref name="TResponse"/>.
/// Operações de Query são estritamente somente leitura (Read-Only).
/// </summary>
public interface IQuery<out TResponse>
{
}
