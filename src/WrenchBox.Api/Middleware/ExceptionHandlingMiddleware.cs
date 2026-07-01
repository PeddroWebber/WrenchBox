using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using WrenchBox.Application.Common;
using WrenchBox.Domain.Exceptions;

namespace WrenchBox.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationException validation => (HttpStatusCode.BadRequest, "Validation Error",
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))),
            NotFoundException notFound => (HttpStatusCode.NotFound, "Not Found", notFound.Message),
            UnauthorizedApplicationException unauthorized => (HttpStatusCode.Unauthorized, "Unauthorized", unauthorized.Message),
            AppException app => (HttpStatusCode.BadRequest, "Bad Request", app.Message),
            DomainException domain => (HttpStatusCode.BadRequest, "Domain Rule Violation", domain.Message),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
