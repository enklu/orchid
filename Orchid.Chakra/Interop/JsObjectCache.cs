using System.Runtime.CompilerServices;

namespace Enklu.Orchid.Chakra.Interop
{
    /// <summary>
    /// Tracks managed objects that are passed into the Orchid API, mapping each object to it's bound
    /// JS external object. The implementation uses weak keys, so the host object will not retain a reference
    /// being added. Similarly, the <see cref="JavaScriptValue"/> will not add a reference.
    /// </summary>
    public class JsObjectCache
    {
        /// <summary>
        /// Reference type container for <see cref="JavaScriptValue"/>.
        /// </summary>
        private class JsValueHolder
        {
            /// <summary>
            /// The javascript value entry.
            /// </summary>
            public JavaScriptValue Value { get; set; }
        }

        /// <summary>
        /// We use a <see cref="ConditionalWeakTable{TKey,TValue}"/> here due to it's ability to
        /// dynamically attach object fields to managed objects. In short, this table maintains
        /// weak referenced keys that do not prevent the key instances from being garbage collected.
        /// </summary>
        private readonly ConditionalWeakTable<object, JsValueHolder> _objects;

        /// <summary>
        /// Creates a new <see cref="JsObjectCache"/> instance.
        /// </summary>
        public JsObjectCache()
        {
            _objects = new ConditionalWeakTable<object, JsValueHolder>();
        }

        /// <summary>
        /// Tries to retrieve the value using the object key.
        /// </summary>
        public bool TryGet(object o, out JavaScriptValue jsValue)
        {
            if (_objects.TryGetValue(o, out var holder))
            {
                jsValue = holder.Value;
                return true;
            }

            jsValue = JavaScriptValue.Invalid;
            return false;
        }

        /// <summary>
        /// Adds a new JS object cache entry.
        /// </summary>
        public void Add(object obj, JavaScriptValue jsValue)
        {
            _objects.Add(obj, new JsValueHolder {Value = jsValue});
        }

        /// <summary>
        /// Removes an object from cache.
        /// </summary>
        public void Remove(object obj)
        {
            _objects.Remove(obj);
        }
    }
}