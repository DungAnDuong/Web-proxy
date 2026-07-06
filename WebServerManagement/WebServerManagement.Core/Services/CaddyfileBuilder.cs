using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebServerManagement.Core.Domain;

namespace WebServerManagement.Core.Services
{
    /// <summary>
    /// Pure text generator for the Caddyfile: one reverse_proxy block per enabled website,
    /// keyed by its public domain and pointed at its private localhost port. No file or process
    /// I/O happens here -- that is the job of the Infrastructure reverse-proxy manager -- which
    /// keeps this class trivially unit-testable.
    /// </summary>
    public static class CaddyfileBuilder
    {
        public static string Build(IEnumerable<WebsiteConfig> websites)
        {
            var sb = new StringBuilder();
            var enabledSites = websites.Where(w => w.Enabled).OrderBy(w => w.Domain).ToList();

            foreach (var site in enabledSites)
            {
                sb.AppendLine($"{site.Domain} {{");
                sb.AppendLine($"    reverse_proxy localhost:{site.InternalPort}");

                if (site.EnableSsl && !string.IsNullOrWhiteSpace(site.CertPath) && !string.IsNullOrWhiteSpace(site.KeyPath))
                {
                    // Explicit certificate/key pair (e.g. a custom or wildcard certificate).
                    sb.AppendLine($"    tls {EscapePath(site.CertPath)} {EscapePath(site.KeyPath)}");
                }
                // else: no tls directive -- Caddy automatically provisions/renews HTTPS (Let's Encrypt).

                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string EscapePath(string path) => path.Replace('\\', '/');
    }
}
