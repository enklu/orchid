using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;

namespace Enklu.Orchid.Jint
{
    public class JsModule : IJsModule
    {
        private readonly Engine _engine;

        private ObjectInstance _exports;

        public string ModuleId { get; }
        
        public string Name { get; }

        public JsValue Module { get; }

        public JsModule(Engine engine, string moduleId)
        {
            _engine = engine;
            ModuleId = moduleId;

            Module = _engine.Intrinsics.Object.Construct(Arguments.Empty);
        }

        public T GetExportedValue<T>(string name)
        {
            if (null == _exports)
            {
                var moduleObj = Module.AsObject();
                if (!moduleObj.HasProperty("exports"))
                {
                    return default(T);
                }

                _exports = moduleObj.Get("exports").AsObject();
            }

            return _exports.Get(name).To<T>(_engine.TypeConverter);
        }
    }
}