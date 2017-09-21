using System;
using System.Linq; 
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Proxemity.UnitTests.Samples;

namespace Proxemity.UnitTests {

  public partial class ProxyTests {

    [TestMethod]
    public void TestFuncProxy() {
      // create ProxyInfo
      var asm = GetAssembly(); 
      var proxyInfo = new ProxyClassInfo(asm, "Proxemity.UnitTests.Proxies.FuncProxyClass", typeof(FuncProxyBase));
      proxyInfo.AddCustomAttribute(() => new SampleCustomAttribute("c1") { Value2 = "c2" });
      // Create emitter
      var emitter = new ProxyEmitter(proxyInfo);
      // create emitter controller and implement interface
      var controller = new FuncProxyEmitController();
      emitter.ImplementInterface(typeof(ISampleFuncInterface), controller);
      // create type
      var proxyType = emitter.CreateTypeInfo().AsType();

      // create and setup target object; FuncProxyBase has a single constructor which accepts single parameter (Target)
      //  emitter will build similar constructor on the generated proxy
      var target = new FuncProxyTarget();
      var proxy =  Activator.CreateInstance(proxyType, target);
      // cast the entity instance to entity interface and test it
      var iFunc = proxy as ISampleFuncInterface;
      Assert.IsNotNull(iFunc, "Failed to retrieve IFunc interface.");

      var d = DateTime.Now;
      var obj = new object();

      var iRes = iFunc.IntMethod(d, obj);
      Assert.AreEqual("IntMethod", target.LastMethod, "Method name mismatch");
      Assert.AreEqual(d, target.LastArgs[0]);
      Assert.AreEqual(obj, target.LastArgs[1]);
      Assert.AreEqual(proxy, target.ExtraValues[0]); //should be reference to proxy instance itself - see FuncProxyBuilderMethodSelector
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
      var classAttr = proxy.GetType().GetCustomAttributes(false).OfType<SampleCustomAttribute>().FirstOrDefault();
      Assert.IsNotNull(classAttr, "Custom attr not found on proxy class.");
      Assert.AreEqual("c1", classAttr.Value1, "Value1 on attr does not match.");
      Assert.AreEqual("c2", classAttr.Value2, "Value2 on attr does not match.");


      //Emit controller creates custom attributes on methods; verify these
      var meth = proxy.GetType().GetMember("IntMethod")[0];
      var methAttr = meth.GetCustomAttributes(false).OfType<SampleCustomAttribute>().FirstOrDefault();
      Assert.IsNotNull(methAttr, "Custom attr not found on method.");
      Assert.AreEqual("v1", methAttr.Value1, "Value1 on attr does not match.");
      Assert.AreEqual("v2", methAttr.Value2, "Value2 on attr does not match.");

    }

  } //class
} //ns
