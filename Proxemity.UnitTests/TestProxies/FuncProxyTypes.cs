using System;
using System.Collections.Generic;
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

  public interface ISampleFuncInterface : IBaseFuncInterface {
    void VoidMethod(string a, int b);
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

  public class FuncProxyEmitController : IProxyEmitController {
    MethodInfo _methodCalledMethod = typeof(FuncProxyTarget).GetMethod(nameof(FuncProxyTarget.MethodCalled));
    PropertyInfo _targetRef = typeof(FuncProxyBase).GetProperty(nameof(FuncProxyBase.Target));

    public EmitInfo GetEmitInfo(InterfaceMemberInfo interfaceMember) {
      switch(interfaceMember.Member) {
        case PropertyInfo prop:
          var result = EmitInfo.CreateForProperty();
          return result; 
      }
      var iMethod = (MethodInfo)interfaceMember.Member;
      // We want to pass all incoming arguments of the call in one array-type argument; generate smth like: 
      //  -- generated proxy method
      //   public object Foo(a, b, c) {
      //     return Target.MethodCalled("Foo", new object[] {a, b, c});
      //   }
      // For args we use ArgBox.CreateArray that will let emitter know that the arg is a special thing - an array of all arguments passed to the method
      var paramRefArray = ArgBox.CreateArray(iMethod.GetParameters());
      // We also pass extra values array to test ArgBox special functions
      var extra1 = ArgBox.CreateProxySelfRef(); //reference to proxy intance
      var extra2 = ArgBox.CreateStaticInstanceRef(() => Singleton.InstanceField); //reference to singleton
      var extra3 = ArgBox.CreateStaticInstanceRef(() => Singleton.InstanceProp);
      var extraValuesArray = ArgBox.CreateArray( new object[] { extra1, extra2, extra3});
      // final args array
      var args = new object[] { iMethod.Name, paramRefArray,  extraValuesArray  };
      var emitInfo = EmitInfo.CreateForMethod(_targetRef, _methodCalledMethod, args);

      // Add a custom attribute to generated method; 
      // it works if we directly use literals in the New expression; 
      //  but we need to test the use of variables
      var v1 = "v1";
      var v2 = "v2";
      emitInfo.AddCustomAttribute(() => new SampleCustomAttribute(v1) { Value2 = v2 });
      //  emitInfo.AddCustomAttribute(() => new SampleCustomAttribute()); // to test how it works without args/initializers
      return emitInfo;
    }//method

  }//class

  [AttributeUsage(AttributeTargets.Method)]
  public class SampleCustomAttribute : Attribute {
    public string Value1 { get; set; }
    public string Value2 { get; set; }
    public SampleCustomAttribute(string value1) {
      Value1 = value1;
    }
    public SampleCustomAttribute() {
      Value1 = "x";
    }
  }

}//ns
