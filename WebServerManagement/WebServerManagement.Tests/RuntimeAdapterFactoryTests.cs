using System;
using NUnit.Framework;
using WebServerManagement.Core.Enums;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.Infrastructure.ProcessManagement;

namespace WebServerManagement.Tests
{
    [TestFixture]
    public class RuntimeAdapterFactoryTests
    {
        [Test]
        public void Resolve_RegisteredRuntimeType_ReturnsMatchingAdapter()
        {
            var nodeAdapter = new NodeRuntimeAdapter();
            var factory = new RuntimeAdapterFactory(new[] { nodeAdapter });

            var resolved = factory.Resolve(RuntimeType.Node);

            Assert.That(resolved, Is.SameAs(nodeAdapter));
        }

        [Test]
        public void Resolve_UnregisteredRuntimeType_Throws()
        {
            var factory = new RuntimeAdapterFactory(new IRuntimeAdapter[0]);

            Assert.Throws<NotSupportedException>(() => factory.Resolve(RuntimeType.Node));
        }
    }
}
