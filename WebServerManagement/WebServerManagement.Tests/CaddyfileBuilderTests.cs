using System.Collections.Generic;
using NUnit.Framework;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Services;

namespace WebServerManagement.Tests
{
    [TestFixture]
    public class CaddyfileBuilderTests
    {
        private static WebsiteConfig MakeSite(string domain, int port, bool enabled = true)
        {
            return new WebsiteConfig { Name = domain, Domain = domain, InternalPort = port, Enabled = enabled };
        }

        [Test]
        public void Build_SingleSite_WithoutSsl_UsesAutomaticHttps()
        {
            var output = CaddyfileBuilder.Build(new[] { MakeSite("ade-aibom.ddns.net", 3001) });

            StringAssert.Contains("ade-aibom.ddns.net {", output);
            StringAssert.Contains("reverse_proxy localhost:3001", output);
            StringAssert.DoesNotContain("tls ", output);
        }

        [Test]
        public void Build_MultipleSites_ProducesOneBlockPerSite()
        {
            var sites = new[]
            {
                MakeSite("erp247.ddns.net", 3002),
                MakeSite("ade-aibom.ddns.net", 3001),
            };

            var output = CaddyfileBuilder.Build(sites);

            StringAssert.Contains("ade-aibom.ddns.net {", output);
            StringAssert.Contains("reverse_proxy localhost:3001", output);
            StringAssert.Contains("erp247.ddns.net {", output);
            StringAssert.Contains("reverse_proxy localhost:3002", output);
        }

        [Test]
        public void Build_SiteWithExplicitCertificate_EmitsTlsDirective()
        {
            var site = MakeSite("mes.company.vn", 3003);
            site.EnableSsl = true;
            site.CertPath = @"C:\certs\mes.crt";
            site.KeyPath = @"C:\certs\mes.key";

            var output = CaddyfileBuilder.Build(new[] { site });

            StringAssert.Contains("tls C:/certs/mes.crt C:/certs/mes.key", output);
        }

        [Test]
        public void Build_DisabledSite_IsExcluded()
        {
            var sites = new[]
            {
                MakeSite("enabled.example.com", 3001, enabled: true),
                MakeSite("disabled.example.com", 3002, enabled: false),
            };

            var output = CaddyfileBuilder.Build(sites);

            StringAssert.Contains("enabled.example.com {", output);
            StringAssert.DoesNotContain("disabled.example.com", output);
        }

        [Test]
        public void Build_NoSites_ReturnsEmptyString()
        {
            var output = CaddyfileBuilder.Build(new List<WebsiteConfig>());

            Assert.That(output.Trim(), Is.Empty);
        }
    }
}
