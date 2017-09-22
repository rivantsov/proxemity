using System;
using System.Collections.Generic;
using System.Text;

namespace Proxemity.UnitTests.Samples {
  //Used to analyze IL in SampleHandCodedProxy class in output assembly using Reflector

  public interface IProxyTarget{
    object GetValue(string name);
    void SetValue(string name, object value);
  }

  public interface IProxyTarget2 {
    object MethodCalled(string methodName, object[] args);
  }

  public class BaseClass {
    public IProxyTarget Target;
    public IProxyTarget2 Target2 { get; set; } // 

    public BaseClass() { }
    public BaseClass(string x) { }
  }

  class SampleHandCodedProxy : BaseClass {
    public static Singleton StaticField = new Singleton() { Id = 1 };
    public static Singleton StaticProp = new Singleton() { Id = 2 };

    public SampleHandCodedProxy() { }
    public SampleHandCodedProxy(string x) : base(x) { }

    public string StringProp {
      get { return (string)Target.GetValue("StringProp"); }
      set { Target.SetValue("StringProp", value); }
    }
    public int IntProp {
      get { return (int)Target.GetValue("IntProp"); }
      set { Target.SetValue("IntProp", value); }
    }

    public int Foo(string a, object b, int c) {
      return (int)Target2.MethodCalled("Foo", new object[] { a, b, StaticField, StaticProp });
    }

    //factory method
    public static SampleHandCodedProxy Create(string x) {
      return new SampleHandCodedProxy(x); 
    }
  } // sample proxy class


  /*
 

.method public hidebysig specialname rtspecialname instance void .ctor(string x) cil managed
{
    .maxstack 8
    L_0000: ldarg.0 
    L_0001: ldarg.1 
    L_0002: call instance void Proxemity.UnitTests.Samples.BaseClass::.ctor(string)
    L_0007: nop 
    L_0008: nop 
    L_0009: ret 
}

 
 



.method public hidebysig instance int32 Foo(string a, object b, int32 c) cil managed
{
    .maxstack 6
    .locals init (
        [0] int32 num)
    L_0000: nop 
    L_0001: ldarg.0 
    L_0002: call instance class Proxemity.UnitTests.Samples.IProxyTarget2 Proxemity.UnitTests.Samples.BaseClass::get_Target2()
    L_0007: ldstr "Foo"
    L_000c: ldc.i4.4 
    L_000d: newarr [System.Runtime]System.Object
    L_0012: dup 
    L_0013: ldc.i4.0 
    L_0014: ldarg.1 
    L_0015: stelem.ref 
    L_0016: dup 
    L_0017: ldc.i4.1 
    L_0018: ldarg.2 
    L_0019: stelem.ref 
    L_001a: dup 
    L_001b: ldc.i4.2 
    L_001c: call class [System.Net.Http]System.Net.Http.HttpMethod [System.Net.Http]System.Net.Http.HttpMethod::get_Get()
    L_0021: stelem.ref 
    L_0022: dup 
    L_0023: ldc.i4.3 
    L_0024: ldsfld string Proxemity.UnitTests.Samples.SampleHandCodedProxy::StaticField
    L_0029: stelem.ref 
    L_002a: callvirt instance object Proxemity.UnitTests.Samples.IProxyTarget2::MethodCalled(string, object[])
    L_002f: unbox.any [System.Runtime]System.Int32
    L_0034: stloc.0 
    L_0035: br.s L_0037
    L_0037: ldloc.0 
    L_0038: ret 
}

 

 
 

  */


}
