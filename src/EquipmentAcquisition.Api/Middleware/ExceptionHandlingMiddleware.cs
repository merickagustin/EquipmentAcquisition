using EquipmentAcquisition.Core.Exceptions;

namespace EquipmentAcquisition.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(new { status = statusCode, detail });
    }
}
