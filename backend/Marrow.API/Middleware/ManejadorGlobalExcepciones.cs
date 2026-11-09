using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Marrow.API.Middleware;

/// <summary>
/// Convierte las excepciones que nadie capturó en una respuesta ProblemDetails.
/// El detalle técnico solo se muestra en desarrollo; los errores 500 los registra UseExceptionHandler.
/// </summary>
public sealed class ManejadorGlobalExcepciones(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = Clasificar(exception);
        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = environment.IsDevelopment() ? exception.ToString() : null
            }
        });
    }

    // Se compara el tipo exacto: una ArgumentOutOfRangeException indica un error interno, no un dato mal enviado.
    private static (int Status, string Title) Clasificar(Exception exception)
    {
        if (exception.GetType() == typeof(ArgumentException))
        {
            return (StatusCodes.Status400BadRequest, "Los datos enviados no son válidos.");
        }

        if (exception is KeyNotFoundException)
        {
            return (StatusCodes.Status404NotFound, "El recurso solicitado no existe.");
        }

        return (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.");
    }
}
