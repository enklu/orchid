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

        private List<string> _methodNames;
        private List<string> _propertyNames;
        private List<string> _fieldNames;

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

            _methods[methodName]
                .Add(new HostMethod
                {
                    Method = method,
                    Parameters = method.GetParameters(),
                    ReturnType = method.ReturnType
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
        public HostMethod MethodFor(string methodName, int totalParams)
        {
            // FIXME: This is error prone as overloaded methods can contain the same # of parameters.
            // FIXME: We should determine if each javascript parameter type can be applied or converted
            // FIXME: to the host parameter types to decide which method is "best."
            var list = _methods[methodName];
            for (int i = 0; i < list.Count; ++i)
            {
                var method = list[i];
                if (method.Parameters.Length == totalParams)
                {
                    return method;
                }
            }

            return null;
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
    }
}