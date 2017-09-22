using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Proxemity.UnitTests.Samples;

namespace Proxemity.UnitTests {

  [TestClass]
  public partial class ProxyTests {

    DynamicAssemblyInfo _dynamicAssembly;

    private DynamicAssemblyInfo GetAssembly() {
      _dynamicAssembly = _dynamicAssembly ?? DynamicAssemblyInfo.Create("Proxemity.UnitTests.Proxies");
      return _dynamicAssembly; 
    }

    [TestMethod]
    public void TestEntityProxy() {
      var asm = GetAssembly();
      var proxyInfo = new ProxyClassInfo(asm, "Proxemity.UnitTests.Proxies.MyEntityClass", typeof(EntityBase));
      var emitter = new ProxyEmitter(proxyInfo);
      var controller = new EntityEmitController();
      emitter.ImplementInterface(typeof(IMyEntity), controller);
      var proxyType = emitter.Complete();

      // create and setup target object 
      var factory = proxyInfo.GetProxyFactory<Func<EntityBase>>();
      var myEntity = factory();
      myEntity.Record = new EntityRecordMock();
      // cast the entity instance to entity interface and test it
      var iMyEntity = myEntity as IMyEntity;
      Assert.IsNotNull(iMyEntity, "Failed to retrieve IMyEntity interface.");
      try {
        //var x = iMyEntity.IntNProp;
        // write/read properties
        iMyEntity.IntProp = 123;
        var intPropBack = iMyEntity.IntProp;
        Assert.AreEqual(123, intPropBack, "Returned int prop value does not match.");
      } catch (Exception ex) {
        var str = ex.ToString();
        throw; 
      }

      iMyEntity.StringProp = "blah";
      var propBack = iMyEntity.StringProp;
      Assert.AreEqual("blah", propBack, "Returned string prop value does not match.");

    }


  } //class
} //ns
