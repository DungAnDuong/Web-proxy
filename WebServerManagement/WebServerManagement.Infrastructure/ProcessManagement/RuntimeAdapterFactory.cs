using System;
using System.Collections.Generic;
using WebServerManagement.Core.Enums;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.ProcessManagement
{
    /// <summary>
    /// Resolves the <see cref="IRuntimeAdapter"/> for a given <see cref="RuntimeType"/>. Adding a
    /// new runtime (Python, PHP, ASP.NET Core, Go, ...) means adding a new adapter class and one
    /// entry here -- no existing adapter is touched (Open/Closed principle).
    /// </summary>
    public class RuntimeAdapterFactory
    {
        private readonly Dictionary<RuntimeType, IRuntimeAdapter> _adapters;

        public RuntimeAdapterFactory(IEnumerable<IRuntimeAdapter> adapters)
        {
            _adapters = new Dictionary<RuntimeType, IRuntimeAdapter>();
            foreach (var adapter in adapters)
            {
                _adapters[adapter.RuntimeType] = adapter;
            }
        }

        public IRuntimeAdapter Resolve(RuntimeType runtimeType)
        {
            if (_adapters.TryGetValue(runtimeType, out var adapter)) return adapter;
            throw new NotSupportedException($"No runtime adapter registered for '{runtimeType}'.");
        }
    }
}
