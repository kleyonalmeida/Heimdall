namespace Heimdall.Abstractions;

/// <summary>
/// Marca uma requisição do tipo Command que altera estado ou dispara uma ação de saída.
/// </summary>
public interface ICommand<out TResponse>
{
}

public interface ICommand
{
}
