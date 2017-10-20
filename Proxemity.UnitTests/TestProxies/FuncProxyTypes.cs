using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace Proxemity.UnitTests {

  /*
   Models a simple interface with methods
   
   */ 

  // We test that when interface (to build proxy for) is inherited from other interface, it all works ok
  public interface IBaseFuncInterface {
    void BaseMethod(string x); 
  }

  [SampleCustom("v1", Value2 = "v2")]
  public interface ISampleFuncInterface : IBaseFuncInterface {
    void VoidMethod(string a, int b);
    [Description("Base method.")]
    [Category("IntMethods")]
    [SampleCustom("v3", Value2 = "v4")]
    int IntMethod(DateTime d, object e);
  }

  // simple class to use as a singleton - to test ArgBox
  public class Singleton {
    // these static singletons are used in tests
    public static Singleton InstanceProp { get; set; } = new Singleton() { Id = 1 };
    public static Singleton InstanceField = new Singleton() { Id = 2 };


    public int Id;
    public override string ToString() { return Id.ToString(); }
  }

  public class FuncProxyBase {

    public FuncProxyTarget Target { get; }

    public FuncProxyBase(FuncProxyTarget target) {
      Target = target; 
    }
  }

  public class FuncProxyTarget {
    public string LastMethod;
    public object[] LastArgs;
    public object[] ExtraValues; 

    public object MethodCalled(string methodName, object[] args, object[] extraValues) {
      LastMethod = methodName;
      LastArgs = args;
      ExtraValues = extraValues; 
      return 123; 
    }
  }//class

  public class FuncProxyEmitController : ProxyEmitControllerBase {
    MethodInfo _targetMethod = typeof(FuncProxyTarget).GetMethod(nameof(FuncProxyTarget.MethodCalled));
    PropertyInfo _targetRef = typeof(FuncProxyBase).GetProperty(nameof(FuncProxyBase.Target));

    public FuncProxyEmitController(DynamicAssemblyInfo assemblyInfo) 
        : base(assemblyInfo, "Proxemity.UnitTests.EmittedClasses.FuncProxy", typeof(FuncProxyBase),
               new SampleAttributeHandler()) { }

    public override MemberEmitInfo GetMethodEmitInfo(MethodInfo interfaceMethod, PropertyInfo ownerProperty = null) {
      // We want to pass all incoming arguments of the call in one array-type argument; generate smth like: 
      //  -- generated proxy method
      //   public object Foo(a, b, c) {
      //     return Target.MethodCalled("Foo", new object[] {a, b, c});
      //   }
      // For args we use ArgBox.CreateArray that will let emitter know that the arg is a special thing - an array of all arguments passed to the method
      var paramRefArray = ArgBox.CreateArray(interfaceMethod.GetParameters());
      // We also pass extra values array to test ArgBox special functions
      var extra1 = ArgBox.CreateProxySelfRef(); //reference to proxy intance
      var extra2 = ArgBox.CreateStaticInstanceRef(() => Singleton.InstanceField); //reference to singleton
      var extra3 = ArgBox.CreateStaticInstanceRef(() => Singleton.InstanceProp);
      var extraValuesArray = ArgBox.CreateArray( new object[] { extra1, extra2, extra3});
      // final args array
      var args = new object[] { interfaceMethod.Name, paramRefArray,  extraValuesArray  };
      var emitInfo = new MemberEmitInfo(interfaceMethod, _targetRef, _targetMethod, args);
      return emitInfo;
    }//method

  }//class

  
}//ns
