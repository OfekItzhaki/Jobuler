namespace Jobuler.Infrastructure.Persistence.Configurations;

internal static class StringExtensions
{
    /// <summary>Converts PascalCase to snake_case for PostgreSQL enum values.</summary>
    internal static string ToSnakeCase(this string s) =>
        string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));

    /// <summary>Converts snake_case back to PascalCase for C# enum parsing.</summary>
    internal static string ToPascalCase(this string s) =>
        string.Concat(s.Split('_').Select(w => char.ToUpper(w[0]) + w[1..]));
}
