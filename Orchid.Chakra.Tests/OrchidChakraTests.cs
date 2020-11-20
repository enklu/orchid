using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Timers;
using Enklu.Orchid.Chakra;
using Enklu.Orchid.Chakra.Interop;
using Enklu.Orchid.Logging;
using NUnit.Framework;

namespace Enklu.Orchid.Chakra.Tests
{
    public class TestLogAdapter : ILogAdapter
    {
        public void Debug(object caller, object message, params object[] replacements)
        {
            Console.WriteLine(string.Format((string)message, replacements));
        }

        public void Info(object caller, object message, params object[] replacements)
        {
            Console.WriteLine(string.Format((string)message, replacements));
        }

        public void Warning(object caller, object message, params object[] replacements)
        {
            Console.WriteLine(string.Format((string)message, replacements));
        }

        public void Error(object caller, object message, params object[] replacements)
        {
            Console.WriteLine(string.Format((string)message, replacements));
        }

        public void Fatal(object caller, object message, params object[] replacements)
        {
            Console.WriteLine(string.Format((string)message, replacements));
        }
    }

    public class SimpleObject
    {
        public byte A { get; set; }
        public short B { get; set; }
        public int C { get; set; }
        public float D { get; set; }
        public double E { get; set; }
        public decimal F { get; set; }
        public bool G { get; set; }
        public string H { get; set; }

        public byte[] AA { get; set; }
        public short[] BB { get; set; }
        public int[] CC { get; set; }
        public float[] DD { get; set; }
        public double[] EE { get; set; }
        public decimal[] FF { get; set; }
        public bool[] GG { get; set; }
        public string[] HH { get; set; }

        public int TotalCalls { get; private set; } = 0;

        public void Reset()
        {
            TotalCalls = 0;
        }

        public void SingleByteParameter(byte a)
        {
            Console.WriteLine("SingleByteParameter(byte a)");
            TotalCalls++;
            Assert.IsTrue(a == A);
        }

        public void SingleShortParameter(short b)
        {
            Console.WriteLine("SingleShortParameter(short b)");
            TotalCalls++;
            Assert.IsTrue(b == B);
        }

        public void SingleIntParameter(int c)
        {
            Console.WriteLine("SingleIntParameter(int c)");
            TotalCalls++;
            Assert.IsTrue(c == C);
        }

        public void SingleFloatParameter(float d)
        {
            Console.WriteLine("SingleFloatParameter(float d)");
            TotalCalls++;
            Assert.IsTrue(d == D);
        }

        public void SingleDoubleParameter(double e)
        {
            Console.WriteLine("SingleDoubleParameter(double e)");
            TotalCalls++;
            Assert.AreEqual(e, E, 0.001);
        }

        public void SingleDecimalParameter(decimal f)
        {
            Console.WriteLine("SingleDecimalParameter(decimal f)");
            TotalCalls++;
            Assert.IsTrue(f == F);
        }

        public void SingleBoolParameter(bool g)
        {
            Console.WriteLine("SingleBoolParameter(bool g)");
            TotalCalls++;
            Assert.IsTrue(g == G);
        }

        public void SingleStringParameter(string h)
        {
            Console.WriteLine("SingleStringParameter(string h)");
            TotalCalls++;
            Assert.IsTrue(h == H);
        }


        public void SingleByteArrayParameter(byte[] a)
        {
            Console.WriteLine("SingleByteArrayParameter(byte[] a)");
            TotalCalls++;
            Asserter.AreEqual(a, AA);
        }

        public void SingleShortArrayParameter(short[] b)
        {
            Console.WriteLine("SingleShortArrayParameter(short[] b)");
            TotalCalls++;
            Asserter.AreEqual(b, BB);
        }

        public void SingleIntArrayParameter(int[] c)
        {
            Console.WriteLine("SingleIntArrayParameter(int[] c)");
            TotalCalls++;
            Asserter.AreEqual(c, CC);
        }

        public void SingleFloatArrayParameter(float[] d)
        {
            Console.WriteLine("SingleFloatArrayParameter(float[] d)");
            TotalCalls++;
            Asserter.AreEqual(d, DD);
        }

        public void SingleDoubleArrayParameter(double[] e)
        {
            Console.WriteLine("SingleDoubleArrayParameter(double[] e)");
            TotalCalls++;
            Asserter.AreEqual(e, EE);
        }

        public void SingleDecimalArrayParameter(decimal[] f)
        {
            Console.WriteLine("SingleDecimalArrayParameter(decimal[] f)");
            TotalCalls++;
            Asserter.AreEqual(f, FF);
        }

        public void SingleBoolArrayParameter(bool[] g)
        {
            Console.WriteLine("SingleBoolArrayParameter(bool[] g)");
            TotalCalls++;
            Asserter.AreEqual(g, GG);
        }

        public void SingleStringArrayParameter(string[] h)
        {
            Console.WriteLine("SingleStringArrayParameter(string[] h)");
            TotalCalls++;
            Asserter.AreEqual(h, HH);
        }

        public void TwoIntParameters(int a, int b)
        {
            Console.WriteLine("TwoIntParameters(int a, int b)");
            Assert.IsTrue(a == A);
            Assert.IsTrue(b == B);
        }
    }

    public class Foo
    {
        public void test(Action<int, string, float> callback)
        {
            callback(10, "hello", 20.0f);
        }

        public void testFunc(Func<int, string, float> callback)
        {
            var ret = callback(15, "hello");
            Console.WriteLine($"Return Value: {ret}");

            Assert.AreEqual((double)ret, 32.4, 0.0001);
        }
    }

    public class Bar
    {
        public int Property { get; }
        public Widget Widget { get; set; }

        public Bar(int prop)
        {
            Property = prop;
        }
    }

    public class Widget
    {
        public string StrProp { get; set; }
        public int IntProp { get; set; }
    }

    public class Container
    {
        private int[] ints = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public List<int> all() => new List<int>(ints);
    }

    [TestFixture]
    public class OrchidChakraTests
    {
        private SimpleObject _simpleObject;
        private int _totalLogCalls = 0;

        /// <summary>
        /// Creates a new test <see cref="JsRuntime"/>
        /// </summary>
        private JsExecutionContext NewTestExecutionContext(JsRuntime runtime)
        {
            var context = (JsExecutionContext) runtime.NewExecutionContext();
            context.RunScript(string.Empty, "function assert(a) { if (!a) throw new Error('Failed Assertion'); };");

            var binding = context.NewJsObject();
            binding.AddFunction("log",
                (callee, call, arguments, count, data) =>
                {
                    _totalLogCalls++;
                    Console.WriteLine(arguments[1].ConvertToString().ToString());
                    return JavaScriptValue.Invalid;
                });

            context.SetValue("console", binding);

            return context;
        }

        private void RunTest(Action<JsExecutionContext> test)
        {
            using (var runtime = new JsRuntime())
            {
                var context = NewTestExecutionContext(runtime);

                test(context);
            }
        }

        [SetUp]
        public void Setup()
        {
            _simpleObject = new SimpleObject
            {
                A = 6,
                B = 55,
                C = 1024,
                D = -15.2f,
                E = 3021.456,
                F = 300.5m,
                G = true,
                H = "testing",
                AA = new byte[] {2, 3, 4},
                BB = new short[] {23, 24, 25},
                CC = new[] {1052, 1053, 1054},
                DD = new[] {54.23f, 55.15f, 345.23f},
                EE = new[] {5001.254, 5002.321, 5005.2351},
                FF = new[] {3.2m, 4.5m, 10.2m},
                GG = new[] {true, false, true},
                HH = new[] {"hello", "world", "test"}
            };
            _totalLogCalls = 0;

            Log.SetAdapter(new TestLogAdapter());
        }

        [Test]
        public void ConsoleLogTest()
        {
            RunTest(context =>
            {
                context.RunScript(string.Empty, "console.log('Hello World!');");
            });

            Assert.AreEqual(1, _totalLogCalls);
        }

        [Test]
        public void SimpleFunctionCallTest()
        {
            RunTest(context =>
            {
                context.SetValue("simple", _simpleObject);
                context.RunScript(string.Empty, @"
                    simple.SingleByteParameter(6);
                    simple.SingleShortParameter(55);
                    simple.SingleIntParameter(1024);
                    simple.SingleFloatParameter(-15.2);
                    simple.SingleDoubleParameter(3021.456);
                    simple.SingleDecimalParameter(300.5);
                    simple.SingleBoolParameter(true);
                    simple.SingleStringParameter('testing');
                    simple.SingleByteArrayParameter([ 2, 3, 4 ]);
                    simple.SingleShortArrayParameter([ 23, 24, 25 ]);
                    simple.SingleIntArrayParameter([ 1052, 1053, 1054 ]);
                    simple.SingleFloatArrayParameter([ 54.23, 55.15, 345.23 ]);
                    simple.SingleDoubleArrayParameter([ 5001.254, 5002.321, 5005.2351 ]);
                    simple.SingleDecimalArrayParameter([ 3.2, 4.5, 10.2 ]);
                    simple.SingleBoolArrayParameter([ true, false, true ]);
                    simple.SingleStringArrayParameter([ 'hello', 'world', 'test' ]);
                ");

                Assert.IsTrue(_simpleObject.TotalCalls == 16);
            });
        }

        [Test]
        public void SimpleFunctionCallFromParameterTest()
        {
            RunTest(context =>
            {
                try
                {
                    context.SetValue("simple", _simpleObject);
                    context.RunScript(string.Empty, @"
                        simple.SingleByteParameter(simple.A);
                        simple.SingleShortParameter(simple.B);
                        simple.SingleIntParameter(simple.C);
                        simple.SingleFloatParameter(simple.D);
                        simple.SingleDoubleParameter(simple.E);
                        simple.SingleDecimalParameter(simple.F);
                        simple.SingleBoolParameter(simple.G);
                        simple.SingleStringParameter(simple.H);
                        simple.SingleByteArrayParameter(simple.AA);
                        simple.SingleShortArrayParameter(simple.BB);
                        simple.SingleIntArrayParameter(simple.CC);
                        simple.SingleFloatArrayParameter(simple.DD);
                        simple.SingleDoubleArrayParameter(simple.EE);
                        simple.SingleDecimalArrayParameter(simple.FF);
                        simple.SingleBoolArrayParameter(simple.GG);
                        simple.SingleStringArrayParameter(simple.HH);
                    ");
                }
                catch (Exception e)
                {
                    Assert.Fail(e.Message);
                }


                Assert.IsTrue(_simpleObject.TotalCalls == 16);
            });
        }

        [Test]
        public void FunctionTest()
        {
            RunTest(context =>
            {
                var foo = new Foo();
                context.SetValue("foo", foo);
                context.RunScript(string.Empty, @"
                    foo.test(function(a, b, c) {
                        console.log('a: ' + a + ', b: ' + b + ', c: ' + c);

                        assert(a == 10);
                        assert(b == 'hello');
                        assert(c == 20.0);
                    });

                    foo.testFunc(function(a, b) {
                        console.log('a: ' + a + ', b: ' + b);
                        return 32.4;
                    });
                ");
            });
        }

        [Test]
        public void SetActionTest()
        {
            int callCount = 0;
            RunTest(context =>
            {
                Action<int, string> a = (i, s) =>
                {
                    callCount++;
                    Console.WriteLine($"[i: {i}, s: {s}]");
                };

                var jsObj = context.NewJsObject();
                jsObj.SetValue("run", a);
                context.SetValue("test", jsObj);

                context.RunScript(string.Empty, "test.run(5, 'testing 1 2 3');");
            });

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void SetFuncTest()
        {
            int callCount = 0;
            RunTest(context =>
            {
                Func<int, string, float> a = (i, s) =>
                {
                    callCount++;
                    Console.WriteLine($"[i: {i}, s: {s}]");
                    return 23.4F;
                };

                var jsObj = context.NewJsObject();
                jsObj.SetValue("run", a);
                context.SetValue("test", jsObj);

                context.RunScript(string.Empty, @"
                    var result = test.run(5, 'testing 1 2 3');
                    console.log(result);

                    var r = result - 23.4;
                    r = r < 0 ? -r : r;
                    assert(r < 0.0001);

                    console.log(r + ' is less than ' + 0.0001);
                ");
            });

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TrackingObjectTest()
        {
            RunTest(context =>
            {
                context.SetValue<Action<int, Bar>>("passBar",
                    (i, b) =>
                    {
                        Console.WriteLine($"i: {i}, Bar: {b.Property}");
                    });

                context.SetValue("bar", new Bar(52) {Widget = new Widget() {StrProp = "WidgetProp", IntProp = 5}});
                context.RunScript(string.Empty, @"
                    passBar(33, bar);
                    var w = bar.Widget;
                    console.log(w.StrProp);");
            });
        }
        /*
        [Test]
        public void ListTest()
        {
            RunTest(context =>
            {
                context.SetValue("container", new Container());
                context.RunScript(string.Empty, @"
                    var all = container.all();
                    for (var i = 0; i < all.Count; ++i) {
                        console.log(all.get_Item(i));
                    }");
            });
        }
        */

        public class DelegateRefTester
        {
            private Action<int, string> _action;

            public void Set(Action<int, string> action)
            {
                _action = action;
            }

            public bool Compare(Action<int, string> action)
            {
                return action == _action;
            }
        }

        [Test]
        public void DelegateReferenceTester()
        {
            RunTest(context =>
            {
                var tester = new DelegateRefTester();
                context.SetValue("tester", tester);

                context.RunScript(string.Empty, @"
                    function doStuff(i, s) {
                        console.log('i: ' + i  + ', s: ' + s);
                    }

                    tester.Set(doStuff);
                    assert(tester.Compare(doStuff));
                    assert(!tester.Compare(function(a, b) { }));
                ");
            });
        }

        [Test]
        public void ActionTest()
        {
            RunTest(context =>
            {
                bool called = false;
                Action callback = () =>
                {
                    Console.WriteLine("Callback Called!");
                    called = true;
                };
                context.SetValue("callback", callback);

                context.RunScript(string.Empty, @"
                    callback();
                ");

                Assert.IsTrue(called);
            });
        }

        [Test]
        public void TestDel()
        {
            RunTest(context =>
            {
                Action<IJsCallback> DoSomething = callback =>
                {
                    var result = callback.Invoke("hello", 32, "world");
                    Console.WriteLine("result: " + result?.GetType());
                };

                context.SetValue("DoSomething", DoSomething);

                context.RunScript(string.Empty, @"
                    function onDoSomething(a, b, c) {
                        console.log('a: ' + a + ', b: ' + b + ', c: ' + c);
                        return [ 'a', 'b', 'c' ];
                    }

                    DoSomething(onDoSomething);
                ");
            });
        }

        [Test]
        public void GetWithJsCallback()
        {
            RunTest(context =>
            {
                context.RunScript(string.Empty, @"
                    function foo(a, b, c) {
                        console.log('a: ' + a + ', b: ' + b + ', c: ' + c);
                    }");

                var callback = context.GetValue<IJsCallback>("foo");
                callback.Invoke("a", 23, 51);
            });
        }

        public class RequiresFoo
        {
            public int IntProp { get; set; }

            private IJsCallback _callback;

            public void register(string name, IJsCallback callback)
            {
                Console.WriteLine("register: " + name);
                _callback = callback;
            }

            public void Update(Context context)
            {
                _callback.Apply(this, context);
            }
        }

        public class Context
        {
            public Context scale(float s)
            {
                return this;
            }

            public Context add(float x, float y)
            {
                return this;
            }

            public Context color(float r, float g, float b)
            {
                return this;
            }
        }

        [Test]
        public void TestRequires()
        {
            Context c = new Context();
            RequiresFoo foo = new RequiresFoo() {IntProp = 15};

            Func<string, object> Resolve = s => foo;

            RunTest(context =>
            {

                context.SetValue("require", new Func<string, object>(value => Resolve(value)));

                context.RunScript(string.Empty, @"
                    var drawing = require('drawing') || { register: function() {} };

                    drawing.register('test', draw);

                    function draw(context) {
                        context
                            .scale(0.1).add(3.2, 1.5).color(23, 21, 23)
                            .add(5.2, 2.3).scale(0.1);
                    }
                ");

                for (int i = 0; i < 100; ++i)
                {
                    foo.Update(c);
                    Thread.Sleep(25);
                }

            });
        }

        [Test]
        public void TestEcma6()
        {
            RunTest(context =>
            {
                context.RunScript(string.Empty, @"
                    class Shape {
                        constructor (id, x, y) {
                            this.id = id
                            this.move(x, y)
                        }
                        move (x, y) {
                            this.x = x
                            this.y = y
                        }
                    }

                    var s = new Shape('hello', 10, 5);
                    console.log(s.x + ' ' + s.y);
                ");
            });
        }

        public class BaseClass
        {
            public void DoSomething(int a)
            {
                Console.WriteLine("A: " + a);
            }
        }

        public class SubClass : BaseClass
        {
            public void DoAThing(int b)
            {
                //
            }
        }

        [Test]
        public void BaseClassTest()
        {
            RunTest(context =>
            {
                var a = new SubClass();

                context.RunScript(string.Empty, a, @"
                    this.DoAThing(5);
                    this.DoSomething(10);
                ");
            });
        }

        public class InnerInner
        {
            public void Dive(Action<Action<int>> callback)
            {
                Console.WriteLine("InnerInner::Dive()");

                callback((i) =>
                {
                    Console.WriteLine("InnerInner::Dive::callback()");

                    Boom();
                });
            }

            private void Boom()
            {
                throw new Exception("Boom!");
            }
        }

        public class Inner
        {
            public InnerInner Dive()
            {
                Console.WriteLine("Inner::Dive()");
                return new InnerInner();
            }
        }

        public class Outer
        {
            public void Dive(Action<Inner> action)
            {
                Console.WriteLine("Outer::Dive()");
                action(new Inner());
            }
        }

        [Test]
        public void DeepExceptionPropagationTest()
        {
            RunTest(context =>
            {
                var outer = new Outer();
                //context.SetValue("outer", outer);
                context.RunScript(string.Empty, @"
                    function acceptOuter(o) {
                        o.Dive(function(inner) {
                            var innerInner = inner.Dive();

                            innerInner.Dive(function(callback) {
                                console.log('innerInner.Dive()');
                                callback(5);
                            });
                        });
                    }
                ");

                var callback = context.GetValue<IJsCallback>("acceptOuter");
                Action a = () => callback.Invoke(outer);
                context.SetValue("execute", a);

                Exception e = null;
                try
                {
                    context.RunScript(string.Empty, "execute();");
                }
                catch (Exception ee)
                {
                    e = ee;
                }

                Assert.IsNotNull(e);
            });
        }

        public class CallCount
        {
            public int CallCounter { get; set; } = 0;

            public void doThing()
            {
                Console.WriteLine("doThing()");
                CallCounter++;
            }
        }

        [Test]
        public void ModuleTest()
        {
            RunTest(context =>
            {
                var script = @"
                    const self = this;

                    function enter() {
                        console.log('enter()');
                        self.doThing();
                    }

                    function update() {
                        console.log('update()');
                    }

                    function exit()
                    {
                        console.log('exit');
                    }

                    function msgMissing()
                    {
                        console.log('msgMissing');
                    }

                    if (typeof module !== 'undefined')
                    {
                        module.exports = {
                            enter: enter,
                            update: update,
                            exit: exit,
                            msgMissing: msgMissing
                        };
                    }";

                var cc = new CallCount();
                var module = context.NewModule("module_1234");

                context.RunScript(string.Empty, cc, script, module);

                var fn = module.GetExportedValue<IJsCallback>("enter");
                fn.Invoke();
            });
        }

        public class BaseReflectionTestObject
        {
            public string BaseField = "bfield";

            public int BaseProp { get; set; }

            public void BaseMethod()
            { }
        }

        [JsDeclaredOnly]
        public class ReflectionTestObject : BaseReflectionTestObject
        {
            public string Field = "field";

            [DenyJsAccess]
            public string IgnoredField = "ignored";

            public int Prop { get; set; }

            [DenyJsAccess]
            public int IgnoredProp { get; set; }

            public void Method() { }

            [DenyJsAccess]
            public void IgnoredMethod() { }
        }


        [Test]
        public void TestHostTypeCacheReflection()
        {
            var typeCache = new HostTypeCache();

            var t = typeCache.Get<ReflectionTestObject>();

            Assert.IsFalse(t.MethodNames.Contains("BaseMethod"));
            Assert.IsFalse(t.PropertyNames.Contains("BaseProp"));
            Assert.IsFalse(t.FieldNames.Contains("BaseField"));

            Assert.IsTrue(t.MethodNames.Contains("Method"));
            Assert.IsFalse(t.MethodNames.Contains("IgnoredMethod"));
            Assert.IsTrue(t.PropertyNames.Contains("Prop"));
            Assert.IsFalse(t.PropertyNames.Contains("IgnoredProp"));
            Assert.IsTrue(t.FieldNames.Contains("Field"));
            Assert.IsFalse(t.FieldNames.Contains("IgnoredField"));

        }

        public class Element
        {
            public string Name { get; set; }
        }

        public class ArrayContainer
        {
            public Element[] Elements { get; set; } =
            {
                new Element {Name = "A"},
                new Element {Name = "B"},
                new Element {Name = "C"},
                new Element {Name = "D"},
                new Element {Name = "E"},
                new Element {Name = "F"},
                new Element {Name = "G"},
                new Element {Name = "H"}
            };
        }

        [Test]
        public void TestArrayStuff()
        {
            RunTest(context =>
            {
                context.SetValue("ele", new ArrayContainer());
                context.RunScript(string.Empty, @"
                    var elements = ele.Elements;

                    for (var i in elements) {
                        console.log(elements[i].Name);
                    }
                ");
            });
        }

        [Test]
        public void DisposeEventTest()
        {
            RunTest(context =>
            {
                bool flag = false;

                Action<IJsCallback> receiveCallback = cb =>
                {
                    cb.ExecutionContext.OnExecutionContextDisposing += ctx => flag = true;
                };

                context.SetValue("makeCallback", receiveCallback);
                context.SetValue("ele", new ArrayContainer());
                context.RunScript(string.Empty, @"
                    var elements = ele.Elements;

                    for (var i in elements) {
                        console.log(elements[i].Name);
                    }

                    makeCallback(function() { });
                ");

                context.Dispose();
                Assert.IsTrue(flag);
            });
        }

        public class FooObj
        {
            public string Name { get; set; }
        }

        public class VarArgTest
        {
            private readonly Action<int> OnCall;

            public VarArgTest(Action<int> onCall)
            {
                OnCall = onCall;
            }

            public void Accept(string a, string b)
            {
                Console.WriteLine("First Accept");
                OnCall(1);
            }
            public void Accept(string a, int b, params object[] strings)
            {
                Console.WriteLine("Second Accept");
                OnCall(2);
            }

            public void Accept(FooObj foo, params object[] obj)
            {
                Console.WriteLine("Third Accept");
                OnCall(3);
            }

            public void Accept(int i = 5, params object[] obj)
            {
                Console.WriteLine("Fourth Accept");
                OnCall(4);
            }

            public void Accept(bool a, string b = "hello", int r = 23, params string[] strs)
            {
                Console.WriteLine("Fifth Accept");
                OnCall(5);
            }

            public void Accept(int a, int[] ints, params string[] strs)
            {
                Console.WriteLine("Sixth Accept");
                OnCall(6);
            }
        }

        [Test]
        public void TestVarArgs()
        {
            int[] callArray = new int[6];

            Action<int> onCall = i =>
            {
                callArray[i - 1]++;
            };

            RunTest(context =>
            {
                var vaTest = new VarArgTest(onCall);
                context.SetValue("vaTest", vaTest);
                context.SetValue("t", new CallCount());
                context.RunScript(string.Empty, "vaTest.Accept('hello', 13);");
                context.RunScript(string.Empty, "vaTest.Accept('test', 13, 'a', 'b', 'c', 'd', 'e', 'f', 32.5, t);");
                context.RunScript(string.Empty, "vaTest.Accept('test', 'me again');");
                context.SetValue("foo", new FooObj {Name = "Bill"});
                context.RunScript(string.Empty, "vaTest.Accept(foo, 1, 2, 3);");
                context.RunScript(string.Empty, "vaTest.Accept();");
                Assert.Throws<Exception>(() =>
                {
                    context.RunScript(string.Empty, "vaTest.Accept('test', 'me', 1, 2, 3);");
                });
                context.SetValue("foo", new FooObj {Name = "Bill"});
                context.RunScript(string.Empty, "vaTest.Accept(foo, 1, 2, 3);");

                context.RunScript(string.Empty, "vaTest.Accept(55, 'hi');");
                context.RunScript(string.Empty, "vaTest.Accept(true, 'hiyo', 15, 'what', 'up', 'brother?');");
                context.RunScript(string.Empty, "vaTest.Accept(23, 'hiyo', 15, 'what', 'up', 'brother?', foo);");
                Assert.Throws<Exception>(() =>
                {
                    context.RunScript(string.Empty, "vaTest.Accept(false, 'hiyo', 15, 'what', 'up', 'brother?', foo);");
                });
                context.RunScript(string.Empty, "vaTest.Accept(5, [ 1, 2, 3, 4, 5 ], 'whee', 'whee', 'what');");
            });

            Asserter.AreEqual(callArray, new int[] {1, 2, 2, 3, 1, 1});
        }

        public class OptParam
        {
            public void dispatch(string eventType, object evt = null)
            {
                Console.WriteLine($"EventType: {eventType}, evt: {evt}");
            }
        }

        [Test]
        public void OptionalParamTest()
        {
            RunTest(context =>
            {
                context.SetValue("d", new OptParam());
                context.RunScript(string.Empty, @"
                    d.dispatch('my-evt');
                ");
            });
        }

        public class NullElement
        {
            public NullElement()
            {

            }
        }

        public class NullReturner
        {
            public NullElement GetProperty(string s)
            {
                return null;
            }
        }

        [Test]
        public void TestNullCarryThrough()
        {
            RunTest(context =>
            {
                context.SetValue("thing", new NullReturner());
                context.RunScript(string.Empty, @"
                    var aThing = thing.GetProperty('foo');
                    assert(!aThing);
                ");
            });
        }

        [Test]
        public void DynamicObjTest()
        {
            RunTest(context =>
            {
                context.SetValue("foo", new FooObj {Name="TestFoo"});
                context.RunScript(string.Empty, @"
                    function output(someObj) {
                        for (var k in someObj) {
                            console.log('key: ' + k + ' = ' + someObj[k]);
                        }
                    }
                ");

                var callback = context.GetValue<IJsCallback>("output");
                context.SetValue("receiver", new Action<object>(o =>
                {
                    Console.WriteLine("Receiver");

                    var d = (Dictionary<string, object>) o;
                    foreach (var key in d.Keys)
                    {
                        Console.WriteLine($"Key: {key}, Value: {d[key]}");
                    }

                    callback.Invoke(o);
                }));

                context.RunScript(string.Empty, @"
                    receiver({
                        prop1: 'test 1 2 3',
                        prop2: 24,
                        prop3: true,
                        prop4: foo
                    });
                ");
            });
        }

        public class InvokeCacheObj
        {
            public FooObj Foo { get; set; } = new FooObj();
            public InvokeCacheObj()
            {

            }

            public void DoIt(string a, int b)
            {
                Console.WriteLine("DoIt #1");
            }

            public void DoIt(string a, int b, float c)
            {
                Console.WriteLine("DoIt #2");
            }

            public void DoIt(string a, string b)
            {
                Console.WriteLine("DoIt #3");
            }

            public void DoIt(string a, int b, FooObj obj)
            {
                Console.WriteLine("DoIt #4");
            }
        }

        [Test]
        public void InvokeCacheTest()
        {
            RunTest(context =>
            {
                var modA = context.NewModule("modA");
                var modB = context.NewModule("modB");
                var modC = context.NewModule("modC");

                var script = @"
                    const self = this;
                    const fooObj = this.Foo;

                    function callAll() {
                        self.DoIt('test', 15);
                        self.DoIt('test', 123, 23.4);
                        self.DoIt('test', '1, 2, 3');
                        self.DoIt('test', 52, fooObj);
                    }

                    module.exports = {
                        callAll: callAll
                    };";

                var a = new InvokeCacheObj();
                var b = new InvokeCacheObj();
                var c = new InvokeCacheObj();

                context.RunScript(string.Empty, a, script, modA);
                context.RunScript(string.Empty, b, script, modB);
                context.RunScript(string.Empty, c, script, modC);

                var callA = modA.GetExportedValue<IJsCallback>("callAll");
                var callB = modB.GetExportedValue<IJsCallback>("callAll");
                var callC = modC.GetExportedValue<IJsCallback>("callAll");

                callA.Invoke();
                callB.Invoke();
                callC.Invoke();
            });
        }

        public class Another
        {
            public override string ToString()
            {
                return "[Another]";
            }
        }

        [Test]
        public void TestLateCallback()
        {
            IJsCallback callback;

            Action<IJsCallback, int> setTimer = (cb, ms) =>
            {
                Console.WriteLine("Adding Callback: {0}", ms);
                callback = cb;
            };
            var runtime = new JsRuntime();
            var context = NewTestExecutionContext(runtime);

            context.SetValue<Action<IJsCallback, int>>("setProxyTimeout",
                (jsCallback, i) =>
                {
                    jsCallback.Apply(this);
                });
            context.RunScript(string.Empty, @"
'use strict';

class JsElementApi {
    constructor() {

    }

    doThing() {
        console.log('doThing()');
    }

    toString() {
        return '[JsElementApi]';
    }
}


class AnotherContext {
    constructor() {

    }
    toString() {
        return '[AnotherContext]';
    }
}

var another = new AnotherContext();
//function setProxyTimeout(fn, to) {
    //setTimeout(function() {
//        fn.apply(another);
    //}, to);
//}

// thisBinding
var thisBinding = new JsElementApi();
var module_1234 = { };

// RunScript(string.Empty, Program)
(function(module) {
    const self = this;

    var foo = 'Hello World';

    function enter() {
        start = start.bind(this);
        console.log('enter()');
        setProxyTimeout(start, 1000);
    }

    function start() {
        console.log(this.toString());
        console.log(foo);
    }

    function update() {
        console.log('update()');
    }

    function exit() {
        console.log('exit');
    }

    function msgMissing() {
        console.log('msgMissing');
    }

    if (typeof module !== 'undefined') {
        module.exports = {
            enter: enter,
            update: update,
            exit: exit,
            msgMissing: msgMissing
        };
    }

}).call(thisBinding, module_1234);

// Assuming thisBinding is global
// GetFunction('enter'), etc...
var enter = module_1234.exports.enter;
var update = module_1234.exports.update;
var exit = module_1234.exports.exit;
var msgMissing = module_1234.exports.msgMissing;

enter.apply(thisBinding);
");
            runtime.Dispose();
        }

    }
}