using NUnit.Framework;
using WebServerManagement.Core.Domain;
using WebServerManagement.Core.Enums;
using WebServerManagement.Infrastructure.ProcessManagement;

namespace WebServerManagement.Tests
{
    [TestFixture]
    public class NodeRuntimeAdapterTests
    {
        private NodeRuntimeAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _adapter = new NodeRuntimeAdapter();
        }

        private static WebsiteConfig MakeSite(string command, string arguments = "")
        {
            return new WebsiteConfig
            {
                Name = "Test",
                SourceFolder = @"D:\Websites\Test",
                WorkingDirectory = @"D:\Websites\Test",
                Command = command,
                Arguments = arguments,
                InternalPort = 3001,
                Environment = EnvironmentMode.Production
            };
        }

        [Test]
        public void BuildStartInfo_NpmCommand_IsWrappedInCmdExe()
        {
            var startInfo = _adapter.BuildStartInfo(MakeSite("npm run start"));

            StringAssert.EndsWith("cmd.exe", startInfo.FileName.ToLowerInvariant());
            StringAssert.Contains("npm run start", startInfo.Arguments);
        }

        [Test]
        public void BuildStartInfo_Pm2Command_IsWrappedInCmdExe()
        {
            var startInfo = _adapter.BuildStartInfo(MakeSite("pm2 start ecosystem.config.js"));

            StringAssert.EndsWith("cmd.exe", startInfo.FileName.ToLowerInvariant());
        }

        [Test]
        public void BuildStartInfo_DirectNodeCommand_LaunchesNodeExecutableDirectly()
        {
            var site = MakeSite("node server.js");
            site.NodeExecutablePath = @"C:\Program Files\nodejs\node.exe";

            var startInfo = _adapter.BuildStartInfo(site);

            Assert.That(startInfo.FileName, Is.EqualTo(@"C:\Program Files\nodejs\node.exe"));
            StringAssert.Contains("server.js", startInfo.Arguments);
        }

        [Test]
        public void BuildStartInfo_SetsNodeEnvAndPortEnvironmentVariables()
        {
            var startInfo = _adapter.BuildStartInfo(MakeSite("npm run start"));

            Assert.That(startInfo.EnvironmentVariables["NODE_ENV"], Is.EqualTo("production"));
            Assert.That(startInfo.EnvironmentVariables["PORT"], Is.EqualTo("3001"));
        }

        [Test]
        public void BuildStartInfo_RedirectsStandardOutputAndError()
        {
            var startInfo = _adapter.BuildStartInfo(MakeSite("npm run start"));

            Assert.That(startInfo.RedirectStandardOutput, Is.True);
            Assert.That(startInfo.RedirectStandardError, Is.True);
            Assert.That(startInfo.UseShellExecute, Is.False);
        }
    }
}
