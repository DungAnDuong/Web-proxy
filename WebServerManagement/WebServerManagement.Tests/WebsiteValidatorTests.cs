using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.Core.Validation;

namespace WebServerManagement.Tests
{
    [TestFixture]
    public class WebsiteValidatorTests
    {
        private Mock<IWebsiteRepository> _repository;
        private Mock<IPortAvailabilityChecker> _portChecker;
        private WebsiteValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _repository = new Mock<IWebsiteRepository>();
            _portChecker = new Mock<IPortAvailabilityChecker>();
            _validator = new WebsiteValidator(_repository.Object, _portChecker.Object);
        }

        private static WebsiteConfig MakeValidSite(Guid? id = null)
        {
            return new WebsiteConfig
            {
                Id = id ?? Guid.NewGuid(),
                Name = "AIBOM",
                Domain = "ade-aibom.ddns.net",
                InternalPort = 3001,
                SourceFolder = @"D:\Websites\AIBOM",
                Command = "npm run start"
            };
        }

        [Test]
        public void Validate_ValidSite_ReturnsNoErrors()
        {
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig>());
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(It.IsAny<int>())).Returns(false);

            var result = _validator.Validate(MakeValidSite());

            Assert.That(result.IsValid, Is.True, string.Join(",", result.Errors));
        }

        [Test]
        public void Validate_DuplicateName_ReturnsError()
        {
            var existing = MakeValidSite();
            existing.Domain = "other.example.com";
            existing.InternalPort = 3002;
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig> { existing });
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(It.IsAny<int>())).Returns(false);

            var candidate = MakeValidSite();
            candidate.Domain = "different.example.com";
            candidate.InternalPort = 3003;

            var result = _validator.Validate(candidate);

            Assert.That(result.IsValid, Is.False);
            StringAssert.Contains("already in use", result.ToString());
        }

        [Test]
        public void Validate_DuplicateDomain_ReturnsError()
        {
            var existing = MakeValidSite();
            existing.Name = "Other";
            existing.InternalPort = 3002;
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig> { existing });
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(It.IsAny<int>())).Returns(false);

            var candidate = MakeValidSite();
            candidate.Name = "Different";
            candidate.InternalPort = 3003;

            var result = _validator.Validate(candidate);

            Assert.That(result.IsValid, Is.False);
            StringAssert.Contains("already assigned", result.ToString());
        }

        [Test]
        public void Validate_DuplicateInternalPort_ReturnsError()
        {
            var existing = MakeValidSite();
            existing.Name = "Other";
            existing.Domain = "other.example.com";
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig> { existing });
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(It.IsAny<int>())).Returns(false);

            var candidate = MakeValidSite();
            candidate.Name = "Different";
            candidate.Domain = "different.example.com";
            // same InternalPort as existing (3001)

            var result = _validator.Validate(candidate);

            Assert.That(result.IsValid, Is.False);
            StringAssert.Contains("already used by another website", result.ToString());
        }

        [Test]
        public void Validate_PortBoundByExternalProcess_ReturnsError()
        {
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig>());
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(3001)).Returns(true);

            var result = _validator.Validate(MakeValidSite());

            Assert.That(result.IsValid, Is.False);
            StringAssert.Contains("already in use by another process", result.ToString());
        }

        [Test]
        public void Validate_EditingOwnUnchangedPort_DoesNotCheckOsPortInUse()
        {
            var id = Guid.NewGuid();
            var persisted = MakeValidSite(id);
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig> { persisted });
            _repository.Setup(r => r.GetById(id)).Returns(persisted);
            // The site's own process is bound to 3001 right now -- must not be flagged.
            _portChecker.Setup(p => p.IsPortInUse(3001)).Returns(true);

            var edited = MakeValidSite(id);
            edited.Description = "updated description";

            var result = _validator.Validate(edited);

            Assert.That(result.IsValid, Is.True, string.Join(",", result.Errors));
        }

        [Test]
        public void Validate_InvalidDomainFormat_ReturnsError()
        {
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig>());
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(It.IsAny<int>())).Returns(false);

            var candidate = MakeValidSite();
            candidate.Domain = "not a valid domain!!";

            var result = _validator.Validate(candidate);

            Assert.That(result.IsValid, Is.False);
            StringAssert.Contains("not a valid host name", result.ToString());
        }

        [Test]
        public void Validate_SslCertWithoutKey_ReturnsError()
        {
            _repository.Setup(r => r.GetAll()).Returns(new List<WebsiteConfig>());
            _repository.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((WebsiteConfig)null);
            _portChecker.Setup(p => p.IsPortInUse(It.IsAny<int>())).Returns(false);

            var candidate = MakeValidSite();
            candidate.EnableSsl = true;
            candidate.CertPath = @"C:\certs\site.crt";
            candidate.KeyPath = "";

            var result = _validator.Validate(candidate);

            Assert.That(result.IsValid, Is.False);
            StringAssert.Contains("must be provided together", result.ToString());
        }
    }
}
