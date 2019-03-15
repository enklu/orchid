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
                    var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

                    if (type.GetTypeInfo().GetCustomAttribute<JsDeclaredOnly>(false) != null)
                    {
                        bindingFlags = bindingFlags | BindingFlags.DeclaredOnly;
                    }

                    var methods = type.GetMethods(bindingFlags);
                    for (int i = 0; i < methods.Length; ++i)
                    {
                        var method = methods[i];
                        if (!method.IsSpecialName && method.GetCustomAttribute<DenyJsAccess>() == null)
                        {
                            hostType.AddMethod(method);
                        }
                    }

                    var properties = type.GetProperties(bindingFlags);
                    for (int i = 0; i < properties.Length; ++i)
                    {
                        var property = properties[i];
                        if (!property.IsSpecialName && property.GetCustomAttribute<DenyJsAccess>() == null)
                        {
                            hostType.AddProperty(property);
                        }
                    }

                    var fields = type.GetFields(bindingFlags);
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        var field = fields[i];
                        if (!field.IsSpecialName && field.GetCustomAttribute<DenyJsAccess>() == null)
                        {
                            hostType.AddField(field);
                        }
                    }

                    // Generate method name lists and read-only views
                    hostType.Done();

                    return hostType;
                });
        }
    }
}