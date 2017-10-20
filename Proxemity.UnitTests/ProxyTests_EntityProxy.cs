using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

namespace Proxemity.UnitTests {
  using DescrAttr = System.ComponentModel.DescriptionAttribute;

  [TestClass]
  public partial class ProxyTests {

    [TestMethod]
    public void TestEntityProxy() {
      var asm = DynamicAssemblyInfo.Create("Proxemity.UnitTests.EntityProxies");
      var controller = new EntityEmitController(asm);
      var emitter = new ProxyEmitter(controller);
      emitter.ImplementInterface(typeof(IMyEntity));
      var proxyType = emitter.CreateClass();

      // create and setup target object 
      var factory = emitter.GetProxyFactory<Func<EntityBase>>();
      var myEntity = factory(); //that's the way to create instance
      myEntity.Record = new EntityRecord();
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

      //check attributes
      //attribute on member
      var stringProp = proxyType.GetProperty("StringProp");
      var mca = stringProp.GetCustomAttribute<CategoryAttribute>();
      Assert.IsNotNull(mca, "Category attr not found on property.");
      Assert.AreEqual("Strings", mca.Category, "Category value on property does not match.");



      var ca = proxyType.GetCustomAttribute<CategoryAttribute>(inherit: true);
      Assert.AreEqual("Cat1", ca.Category, "Category value on type does not match.");

      var da = proxyType.GetCustomAttribute<DescrAttr>(inherit: true);
      Assert.IsNotNull(da, "Expected Description attr on class.");
      Assert.AreEqual("Entity description.", da.Description, "Descr on type does not match.");
      //attribute on member
      //var stringProp = proxyType.GetProperty("StringProp");
      ca = stringProp.GetCustomAttribute<CategoryAttribute>();
      Assert.IsNotNull(ca, "Category attr not found on property.");
      Assert.AreEqual("Strings", ca.Category, "Category value on property does not match.");
      var sc = stringProp.GetCustomAttribute<SampleCustomAttribute>();
      Assert.IsNotNull(sc, "SampleCustom attr not found on property.");
      Assert.AreEqual("v1", sc.Value1, "Value1 prop value on property does not match.");
      Assert.AreEqual("v2", sc.Value2, "Value1 prop value on property does not match.");
    }


  } //class
} //ns
