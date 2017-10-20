using System;
using System.Linq; 
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.ComponentModel;

namespace Proxemity.UnitTests {

  public partial class ProxyTests {

    [TestMethod]
    public void TestFuncProxy() {
      // create ProxyInfo
      var asm = DynamicAssemblyInfo.Create("Proxemity.UnitTests.FuncProxies");
      var controller = new FuncProxyEmitController(asm);
      // Create emitter
      var emitter = new ProxyEmitter(controller);
      emitter.ImplementInterface(typeof(ISampleFuncInterface));
      // create type
      var proxyType = emitter.CreateClass();

      // create and setup target object; FuncProxyBase has a single constructor which accepts single parameter (Target)
      //  emitter will build similar constructor and static factory method on the generated proxy.
      var target = new FuncProxyTarget();
      var factory = emitter.GetProxyFactory<Func<FuncProxyTarget, FuncProxyBase>>();

      var proxy = factory(target); 
      // cast the entity instance to entity interface and test it
      var iFunc = proxy as ISampleFuncInterface;
      Assert.IsNotNull(iFunc, "Failed to retrieve IFunc interface.");

      var d = DateTime.Now;
      var obj = new object();

      var iRes = iFunc.IntMethod(d, obj);
      Assert.AreEqual("IntMethod", target.LastMethod, "Method name mismatch");
      Assert.AreEqual(d, target.LastArgs[0]);
      Assert.AreEqual(obj, target.LastArgs[1]);
      Assert.AreEqual(proxy, target.ExtraValues[0]); //should be reference to proxy instance itself - see FuncProxyEmitController
      Assert.AreEqual(Singleton.InstanceField, target.ExtraValues[1]);
      Assert.AreEqual(Singleton.InstanceProp, target.ExtraValues[2]);

      iFunc.VoidMethod("a", 123);
      Assert.AreEqual("VoidMethod", target.LastMethod);
      Assert.AreEqual("a", (string)target.LastArgs[0]);
      Assert.AreEqual(123, (int)target.LastArgs[1]);

      iFunc.BaseMethod("x");
      Assert.AreEqual("BaseMethod", target.LastMethod);
      Assert.AreEqual("x", (string)target.LastArgs[0]);

      // We specified a custom attribute on class in ProxyInfo (see above) when we initialized emitter
      var classAttr = proxyType.GetCustomAttributes(false).OfType<SampleCustomAttribute>().FirstOrDefault();
      Assert.IsNotNull(classAttr, "Custom attr not found on proxy class.");
      Assert.AreEqual("v1", classAttr.Value1, "Value1 on attr does not match.");
      Assert.AreEqual("v2", classAttr.Value2, "Value2 on attr does not match.");


      //Emit controller creates custom attributes on methods; verify these
      var intMethod = proxyType.GetMember("IntMethod")[0];
      var scAttr = intMethod.GetCustomAttributes(false).OfType<SampleCustomAttribute>().FirstOrDefault();
      Assert.IsNotNull(scAttr, "Custom attr not found on method.");
      Assert.AreEqual("v3", scAttr.Value1, "Value1 on attr does not match.");
      Assert.AreEqual("v4", scAttr.Value2, "Value2 on attr does not match.");

      var catAttr = intMethod.GetCustomAttributes(false).OfType<CategoryAttribute>().FirstOrDefault();
      Assert.IsNotNull(catAttr, "Category attr not found on method.");
      Assert.AreEqual("IntMethods", catAttr.Category, "Value1 on attr does not match.");

    }

  } //class
} //ns
