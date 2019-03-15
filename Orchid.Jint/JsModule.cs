using Jint;
using Jint.Native;
using Jint.Native.Object;

namespace Enklu.Orchid.Jint
{
    public class JsModule : IJsModule
    {
        private readonly Engine _engine;

        private ObjectInstance _exports;

        public string ModuleId { get; }

        public JsValue Module { get; }

        public JsModule(Engine engine, string moduleId)
        {
            _engine = engine;
            ModuleId = moduleId;

            // FIXME: Probably a faster way to create an Object
            _engine.Execute("var " + moduleId + " = { };");
            Module = _engine.GetValue(moduleId);
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

            return _exports.Get(name).To<T>(_engine.ClrTypeConverter);
        }
    }
}