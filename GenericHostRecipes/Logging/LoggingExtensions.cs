using System;
using System.Linq;

using Microsoft.Extensions.Logging;

namespace GenericHostBuilderRecipes.Logging
{
    public static class LoggingExtensions
    {
        public static IDisposable BeginStructuredScope(this ILogger logger, ScopeValue[] values)
        {
            var scopeValues = values
                .ToDictionary(value => value.Destructure ? $"@{value.Key}" : value.Key, value => value.Value);

            return logger.BeginScope(scopeValues);
        }
    }
}