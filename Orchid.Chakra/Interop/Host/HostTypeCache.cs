using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Holds a very basic cache for host type information used for interop.
    /// </summary>
    public class HostTypeCache
    {
        private readonly ConcurrentDictionary<Type, HostType> _cache = new ConcurrentDictionary<Type, HostType>();

        /// <summary>
        /// Gets the <see cref="IHostType"/> cache entry for the specified type.
        /// </summary>
        public IHostType Get<T>() => Get(typeof(T));

        /// <summary>
        /// Gets the <see cref="IHostType"/> cache entry for the specified type.
        /// </summary>
        public IHostType Get(Type t)
        {
            return _cache.GetOrAdd(t,
                type =>
                {
                    var hostType = new HostType();

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < methods.Length; ++i)
                    {
                        hostType.AddMethod(methods[i]);
                    }

                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < properties.Length; ++i)
                    {
                        hostType.AddProperty(properties[i]);
                    }

                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        hostType.AddField(fields[i]);
                    }

                    // Generate method name lists and read-only views
                    hostType.Done();

                    return hostType;
                });
        }
    }
}