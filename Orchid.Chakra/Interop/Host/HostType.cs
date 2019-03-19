using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// This class is used to cache methods, properties, and field reflection data for specific types.
    /// </summary>
    public class HostType : IHostType
    {
        private readonly Dictionary<string, List<HostMethod>> _methods = new Dictionary<string, List<HostMethod>>();
        private readonly Dictionary<string, HostProperty> _properties = new Dictionary<string, HostProperty>();
        private readonly Dictionary<string, HostField> _fields = new Dictionary<string, HostField>();
        private readonly Dictionary<string, HostMethod> _methodInvocations = new Dictionary<string, HostMethod>();

        private List<string> _methodNames;
        private List<string> _propertyNames;
        private List<string> _fieldNames;

        /// <summary>
        /// Scratch list for returning method lookups.
        /// </summary>
        private List<HostMethod> _scratch = new List<HostMethod>();

        /// <summary>
        /// Method Names
        /// </summary>
        public IList<string> MethodNames { get; private set; }

        /// <summary>
        /// Property Names
        /// </summary>
        public IList<string> PropertyNames { get; private set; }

        /// <summary>
        /// Field Names
        /// </summary>
        public IList<string> FieldNames { get; private set; }

        /// <summary>
        /// Adds a method to the host type.
        /// </summary>
        public void AddMethod(MethodInfo method)
        {
            var methodName = method.Name;
            if (!_methods.ContainsKey(methodName))
            {
                _methods[methodName] = new List<HostMethod>();
            }

            var parameters = method.GetParameters();
            bool isVarArgs = false;
            if (parameters.Length > 0)
            {
                var lastParam = parameters[parameters.Length - 1];
                isVarArgs = lastParam.GetCustomAttribute<ParamArrayAttribute>() != null;
            }

            var totalOptional = 0;
            for (var i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i].IsOptional)
                {
                    totalOptional++;
                }
            }

            _methods[methodName]
                .Add(new HostMethod
                {
                    Method = method,
                    Parameters = parameters,
                    ReturnType = method.ReturnType,
                    OptionalParameters = totalOptional,
                    IsVarArgs = isVarArgs
                });
        }

        /// <summary>
        /// Adds a property to the host type.
        /// </summary>
        public void AddProperty(PropertyInfo property)
        {
            _properties[property.Name] = new HostProperty
            {
                Getter = property.GetMethod,
                Setter = property.SetMethod,
                PropertyType = property.PropertyType
            };
        }

        /// <summary>
        /// Adds a field to the host type
        /// </summary>
        /// <param name="field"></param>
        public void AddField(FieldInfo field)
        {
            _fields[field.Name] = new HostField
            {
                Field = field
            };
        }

        /// <summary>
        /// Creates a key lists and read-only views.
        /// </summary>
        public void Done()
        {
            _methodNames = _methods.Keys.ToList();
            _propertyNames = _properties.Keys.ToList();
            _fieldNames = _fields.Keys.ToList();

            MethodNames = _methodNames.AsReadOnly();
            PropertyNames = _propertyNames.AsReadOnly();
            FieldNames = _fieldNames.AsReadOnly();
        }

        /// <summary>
        /// This method returns the first method available that matches the name and the # of parameters.
        /// </summary>
        public List<HostMethod> MethodsFor(string methodName, int totalParams)
        {
            _scratch.Clear();

            var list = _methods[methodName];
            for (int i = 0; i < list.Count; ++i)
            {
                var method = list[i];
                var parameters = method.Parameters;

                if (!method.IsVarArgs)
                {
                    var required = parameters.Length - method.OptionalParameters;
                    if (totalParams >= required && totalParams <= parameters.Length)
                    {
                        _scratch.Add(method);
                    }
                }
                else
                {
                    var paramCount = method.VarArgIndex;
                    var required = paramCount - method.OptionalParameters;
                    if (totalParams >= required)
                    {
                        _scratch.Add(method);
                    }
                }
            }

            return _scratch;
        }

        /// <summary>
        /// This method returns a <see cref="HostProperty"/> for the property name provided.
        /// </summary>
        public HostProperty PropertyFor(string propertyName)
        {
            if (!_properties.ContainsKey(propertyName))
            {
                return null;
            }

            return _properties[propertyName];
        }

        /// <summary>
        /// This method returns a <see cref="HostField"/> for the property name provided.
        /// </summary>
        public HostField FieldFor(string fieldName)
        {
            if (!_fields.ContainsKey(fieldName))
            {
                return null;
            }

            return _fields[fieldName];
        }

        /// <inheritdoc/>
        public void CacheInvocation(string invokeKey, HostMethod method) => _methodInvocations[invokeKey] = method;

        /// <inheritdoc/>
        public bool TryGetInvocation(string invokeKey, out HostMethod method) => _methodInvocations.TryGetValue(invokeKey, out method);
    }
}