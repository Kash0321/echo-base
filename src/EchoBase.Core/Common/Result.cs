namespace EchoBase.Core.Common;

/// <summary>
/// Resultado de una operación que no devuelve valor.
/// Encapsula el éxito o un mensaje de error descriptivo.
/// </summary>
public sealed class Result
{
    /// <summary>Indica si la operación fue exitosa.</summary>
    public bool IsSuccess { get; }

    /// <summary>Código o mensaje de error. <see langword="null"/> cuando la operación fue exitosa.</summary>
    public string? Error { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Crea un resultado exitoso.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Crea un resultado fallido con el error indicado.</summary>
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Resultado de una operación que devuelve un valor de tipo <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Tipo del valor devuelto en caso de éxito.</typeparam>
public sealed class Result<T>
{
    /// <summary>Indica si la operación fue exitosa.</summary>
    public bool IsSuccess { get; }

    /// <summary>Valor devuelto por la operación. Solo válido cuando <see cref="IsSuccess"/> es <see langword="true"/>.</summary>
    public T? Value { get; }

    /// <summary>Código o mensaje de error. <see langword="null"/> cuando la operación fue exitosa.</summary>
    public string? Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private Result(string error)
    {
        IsSuccess = false;
        Error = error;
    }

    /// <summary>Crea un resultado exitoso con el valor indicado.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Crea un resultado fallido con el error indicado.</summary>
    public static Result<T> Failure(string error) => new(error);
}
