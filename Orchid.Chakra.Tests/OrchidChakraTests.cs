using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Enklu.Orchid.Chakra;
using Enklu.Orchid.Chakra.Interop;
using NUnit.Framework;

namespace Enklu.Orchid.Chakra.Tests
{
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
            context.RunScript("function assert(a) { if (!a) throw new Error('Failed Assertion'); };");

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
        }

        [Test]
        public void ConsoleLogTest()
        {
            RunTest(context =>
            {
                context.RunScript("console.log('Hello World!');");
            });

            Assert.AreEqual(1, _totalLogCalls);
        }

        [Test]
        public void SimpleFunctionCallTest()
        {
            RunTest(context =>
            {
                context.SetValue("simple", _simpleObject);
                context.RunScript(@"
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
                    context.RunScript(@"
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
                context.RunScript(@"
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

                context.RunScript("test.run(5, 'testing 1 2 3');");
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

                context.RunScript(@"
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
                context.RunScript(@"
                    passBar(33, bar);
                    var w = bar.Widget;
                    console.log(w.StrProp);");
            });
        }

        [Test]
        public void ListTest()
        {
            RunTest(context =>
            {
                context.SetValue("container", new Container());
                context.RunScript(@"
                    var all = container.all();
                    for (var i = 0; i < all.Count; ++i) {
                        console.log(all.get_Item(i));
                    }");
            });
        }

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

                context.RunScript(@"
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

                context.RunScript(@"
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

                context.RunScript(@"
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
                context.RunScript(@"
                    function foo(a, b, c) {
                        console.log('a: ' + a + ', b: ' + b + ', c: ' + c);
                    }");

                var callback = context.GetValue<IJsCallback>("foo");
                callback.Invoke("a", 23, 51);
            });
        }

        public class ThisBinding
        {
            private string _name;
            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    Console.WriteLine("Name is: {0}", _name);
                }
            }

        }

        [Test]
        public void ThisBindingTest()
        {
            RunTest(context =>
            {
                var thisBinding = new ThisBinding();

                context.RunScript(thisBinding, @"
                    var self = this;

                    function enter() {
                        self.Name = 'Slim Shady';
                    }
                ");

                IJsCallback enter = context.GetValue<IJsCallback>("enter");
                enter.Invoke();
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

                context.RunScript(@"
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
                context.RunScript(@"
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
        /*
        [Test]
        public void DelegateTest()
        {
            RunTest(context =>
            {
                Action callback1 = () =>
                {
                    Console.WriteLine("Callback #1 Called!");
                };

                Action<string> callback2 = s =>
                {
                    Console.WriteLine("Callback #2 Called With: {0}", s);
                };

                Func<string, int> callback3 = s =>
                {
                    int retVal = 32;
                    Console.WriteLine("Callback #3 Called With: {0}, Returning: {1}", s, retVal);
                    return retVal;
                };

                context.SetValue("callback1", (Delegate) callback1);
                context.SetValue("callback2", (Delegate) callback2);
                context.SetValue("callback3", (Delegate) callback3);

                context.RunScript(@"
                    callback1();
                    callback2('2-input');
                    var result = callback3('3-input');

                    console.log('Result: ' + result);
                ");
            });
        }
        */
    }
}