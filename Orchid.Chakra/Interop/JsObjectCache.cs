using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private class JsValueHolder : SafeHandle
        {
            /// <summary>
            /// The context scope this value exists within.
            /// </summary>
            private readonly JsContextScope _scope;

            /// <summary>
            /// JS Value
            /// </summary>
            private JavaScriptValue _value;

            /// <summary>
            /// The javascript value entry.
            /// </summary>
            public JavaScriptValue Value => _value;

            /// <summary>
            /// Creates a new safe handle for the JS Value.
            /// </summary>
            public JsValueHolder(JavaScriptValue value, JsContextScope scope)
                : base(value.Reference, true)
            {
                _value = value;
                _value.AddRef();
            }

            /// <inheritdoc/>
            protected override bool ReleaseHandle()
            {
                // When C# object is GC'd, we'll release the JS reference
                if (_value.IsValid)
                {
                    _scope.QueueRelease(_value);
                }

                return true;
            }

            /// <inheritdoc/>
            public override bool IsInvalid => !_value.IsValid;
        }

        /// <summary>
        /// Context scope the objects added are part of.
        /// </summary>
        private readonly JsContextScope _scope;

        /// <summary>
        /// We use a <see cref="ConditionalWeakTable{TKey,TValue}"/> here due to it's ability to
        /// dynamically attach object fields to managed objects. In short, this table maintains
        /// weak referenced keys that do not prevent the key instances from being garbage collected.
        /// </summary>
        private readonly ConditionalWeakTable<object, JsValueHolder> _objects;

        /// <summary>
        /// Creates a new <see cref="JsObjectCache"/> instance.
        /// </summary>
        public JsObjectCache(JsContextScope scope)
        {
            _scope = scope;
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
            _objects.Add(obj, new JsValueHolder(jsValue, _scope));
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