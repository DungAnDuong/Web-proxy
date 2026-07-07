using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Core.Validation
{
    /// <summary>
    /// Enforces the invariants required before a <see cref="WebsiteConfig"/> can be saved: unique
    /// name/domain/port across all configured sites, a syntactically valid domain, a port in range,
    /// and (for genuinely new bindings) that the port is not already occupied by something outside
    /// this application.
    /// </summary>
    public class WebsiteValidator
    {
        private static readonly Regex DomainPattern = new Regex(
            @"^(?=.{1,253}$)(?!-)[A-Za-z0-9-]{1,63}(?<!-)(\.(?!-)[A-Za-z0-9-]{1,63}(?<!-))+$",
            RegexOptions.Compiled);

        private const int MinPort = 1;
        private const int MaxPort = 65535;
        private const int RecommendedMinPort = 1024;

        private readonly IWebsiteRepository _repository;
        private readonly IPortAvailabilityChecker _portChecker;

        public WebsiteValidator(IWebsiteRepository repository, IPortAvailabilityChecker portChecker)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _portChecker = portChecker ?? throw new ArgumentNullException(nameof(portChecker));
        }

        /// <summary>Finds the first port at or above <paramref name="startFrom"/> that is free both in the
        /// saved configuration and on the OS, so the Add Website dialog can pre-fill a usable value
        /// instead of always defaulting to the same port for every new site.</summary>
        public int SuggestAvailablePort(int startFrom = 3000)
        {
            var usedPorts = new HashSet<int>(_repository.GetAll().Select(w => w.InternalPort));

            for (var port = startFrom; port <= MaxPort; port++)
            {
                if (usedPorts.Contains(port)) continue;
                if (_portChecker.IsPortInUse(port)) continue;
                return port;
            }

            return startFrom;
        }

        public ValidationResult Validate(WebsiteConfig website)
        {
            if (website == null) throw new ArgumentNullException(nameof(website));

            var result = new ValidationResult();
            var others = _repository.GetAll().Where(w => w.Id != website.Id).ToList();

            ValidateName(website, others, result);
            ValidateDomain(website, others, result);
            ValidatePort(website, others, result);
            ValidateSourceFolder(website, result);
            ValidateSsl(website, result);

            return result;
        }

        private static void ValidateName(WebsiteConfig website, System.Collections.Generic.List<WebsiteConfig> others, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(website.Name))
            {
                result.AddError("Website Name is required.");
                return;
            }

            if (others.Any(w => string.Equals(w.Name, website.Name, StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Website Name '{website.Name}' is already in use.");
            }
        }

        private void ValidateDomain(WebsiteConfig website, System.Collections.Generic.List<WebsiteConfig> others, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(website.Domain))
            {
                result.AddError("Domain is required.");
                return;
            }

            if (!DomainPattern.IsMatch(website.Domain))
            {
                result.AddError($"Domain '{website.Domain}' is not a valid host name.");
            }

            if (others.Any(w => string.Equals(w.Domain, website.Domain, StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Domain '{website.Domain}' is already assigned to another website.");
            }
        }

        private void ValidatePort(WebsiteConfig website, System.Collections.Generic.List<WebsiteConfig> others, ValidationResult result)
        {
            if (website.InternalPort < MinPort || website.InternalPort > MaxPort)
            {
                result.AddError($"Internal Port must be between {MinPort} and {MaxPort}.");
                return;
            }

            if (website.InternalPort < RecommendedMinPort)
            {
                result.AddError($"Internal Port {website.InternalPort} is a well-known/system port; use {RecommendedMinPort} or above.");
            }

            if (others.Any(w => w.InternalPort == website.InternalPort))
            {
                result.AddError($"Internal Port {website.InternalPort} is already used by another website.");
                return;
            }

            // Nothing we manage owns this port (checked above). Skip the OS-level listener check
            // when this is the site's own previously-saved port -- its own running process is
            // expected to already be bound to it. Only a genuinely new port assignment is checked
            // against the OS to catch conflicts with processes outside this application.
            var existing = _repository.GetById(website.Id);
            var portIsUnchanged = existing != null && existing.InternalPort == website.InternalPort;
            if (!portIsUnchanged && _portChecker.IsPortInUse(website.InternalPort))
            {
                result.AddError($"Port {website.InternalPort} is already in use by another process on this machine.");
            }
        }

        private static void ValidateSourceFolder(WebsiteConfig website, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(website.SourceFolder))
            {
                result.AddError("Source Folder is required.");
            }

            if (string.IsNullOrWhiteSpace(website.Command))
            {
                result.AddError("Start Command is required.");
            }
        }

        private static void ValidateSsl(WebsiteConfig website, ValidationResult result)
        {
            if (!website.EnableSsl) return;

            var hasCert = !string.IsNullOrWhiteSpace(website.CertPath);
            var hasKey = !string.IsNullOrWhiteSpace(website.KeyPath);

            if (hasCert != hasKey)
            {
                result.AddError("Both Certificate Path and Key Path must be provided together, or left empty to use automatic HTTPS.");
            }
        }
    }
}
