namespace DairyDNA.Api.Infrastructure;

/// <summary>Problem Details is registered via AddProblemDetails(); validation helpers live here for extension.</summary>
public static class ProblemDetailsExtensions
{
    public static IResult ValidationProblem(IDictionary<string, string[]> errors)
        => Results.ValidationProblem(errors);
}
