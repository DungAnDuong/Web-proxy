using System.Collections.Generic;
using System.Linq;

namespace WebServerManagement.Core.Validation
{
    /// <summary>Result of validating a <see cref="Domain.WebsiteConfig"/> -- a list of human-readable errors, empty when valid.</summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();

        public bool IsValid => Errors.Count == 0;

        public void AddError(string message) => Errors.Add(message);

        public override string ToString() => string.Join(System.Environment.NewLine, Errors);
    }
}
