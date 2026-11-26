using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace JoaoLoureiro.Portfolio.Infrastructure.Persistence;

public sealed class ContentStoreOptionsValidator(IHostEnvironment environment)
    : IValidateOptions<ContentStoreOptions>
{
    public ValidateOptionsResult Validate(string? name, ContentStoreOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Path))
        {
            failures.Add("ContentStore:Path is required.");
            return ValidateOptionsResult.Fail(failures);
        }

        if (environment.IsProduction() && !System.IO.Path.IsPathRooted(options.Path))
        {
            failures.Add("ContentStore:Path must be an absolute path in Production.");
        }

        try
        {
            var fullPath = System.IO.Path.GetFullPath(options.Path);
            if (string.IsNullOrWhiteSpace(System.IO.Path.GetFileName(fullPath)))
            {
                failures.Add("ContentStore:Path must identify a database file.");
            }
        }
        catch (ArgumentException)
        {
            failures.Add("ContentStore:Path must be a valid database file path.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
