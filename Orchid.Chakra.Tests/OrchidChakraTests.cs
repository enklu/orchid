using System;
using System.Linq;
using System.Runtime.InteropServices;
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

    [TestFixture]
    public class OrchidChakraTests
    {
        private SimpleObject _simpleObject;
        private int _totalLogCalls = 0;

        /// <summary>
        /// Creates a new test <see cref="JsEngine"/>
        /// </summary>
        private JsEngine NewTestEngine()
        {
            var jsEngine = new JsEngine();
            jsEngine.RunScript("function assert(a) { if (!a) throw new Error('Failed Assertion'); };");

            var binding = jsEngine.NewJsObject();
            binding.AddFunction("log",
                (callee, call, arguments, count, data) =>
                {
                    _totalLogCalls++;
                    Console.WriteLine(arguments[1].ConvertToString().ToString());
                    return JavaScriptValue.Invalid;
                });

            jsEngine.SetValue("console", binding);
            return jsEngine;
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
            using (var engine = NewTestEngine())
            {
                engine.RunScript("console.log('Hello World!');");
            }

            Assert.AreEqual(1, _totalLogCalls);
        }

        [Test]
        public void SimpleFunctionCallTest()
        {
            using (var engine = NewTestEngine())
            {
                engine.SetValue("simple", _simpleObject);
                engine.RunScript(@"
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
            }
        }

        [Test]
        public void SimpleFunctionCallFromParameterTest()
        {
            using (var engine = NewTestEngine())
            {
                try
                {
                    engine.SetValue("simple", _simpleObject);
                    engine.RunScript(@"
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

                Assert.AreEqual((double) ret, 32.4, 0.0001);
            }
        }

        [Test]
        public void FunctionTest()
        {
            using (var engine = NewTestEngine())
            {
                var foo = new Foo();
                engine.SetValue("foo", foo);
                engine.RunScript(@"
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
            }
        }

        [Test]
        public void SetActionTest()
        {
            int callCount = 0;
            using (var engine = NewTestEngine())
            {
                Action<int, string> a = (i, s) =>
                {
                    callCount++;
                    Console.WriteLine($"[i: {i}, s: {s}]");
                };

                var jsObj = engine.NewJsObject();
                jsObj.SetValue("run", a);
                engine.SetValue("test", jsObj);

                engine.RunScript("test.run(5, 'testing 1 2 3');");
            }

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void SetFuncTest()
        {
            int callCount = 0;
            using (var engine = NewTestEngine())
            {
                Func<int, string, float> a = (i, s) =>
                {
                    callCount++;
                    Console.WriteLine($"[i: {i}, s: {s}]");
                    return 23.4F;
                };

                var jsObj = engine.NewJsObject();
                jsObj.SetValue("run", a);
                engine.SetValue("test", jsObj);

                engine.RunScript(@"
                    var result = test.run(5, 'testing 1 2 3');
                    console.log(result);

                    var r = result - 23.4;
                    r = r < 0 ? -r : r;
                    assert(r < 0.0001);

                    console.log(r + ' is less than ' + 0.0001);
                ");
            }

            Assert.AreEqual(1, callCount);
        }

        public class Bar
        {
            public int Property { get; }

            public Bar(int prop)
            {
                Property = prop;
            }
        }

        [Test]
        public void TrackingObjectTest()
        {
            using (var engine = NewTestEngine())
            {
                engine.SetValue<Action<int, Bar>>("passBar",
                    (i, b) =>
                    {
                        Console.WriteLine($"i: {i}, Bar: {b.Property}");
                    });

                engine.SetValue("bar", new Bar(52));
                engine.RunScript("passBar(33, bar);");
            }
        }
    }
}