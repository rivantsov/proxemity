using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Proxemity.UnitTests {
  /*
   These test types mock entity generation in VITA fwk. 
   Proxemity will be used to generate actual classes behind entities (interfaces).
   IL-generated entity class (one per entity) is a proxy; it implements an entity interface and redirects
   property get/set calls to the underlying EntityRecord's GetValue and SetValue methods.
   */

  /* the proxy emitter will generate a class implementing IMyEntity, inherited from Entity. The class will redirect 
     all calls to properties to Record.SetValue and Record.GetValue, with Record being a reference to IEntityRecord/EntityRecordMock
     instance. 
   */

  // We test that when interface is inherited from another, it works - the emitter finds all inherited methods
  public interface IBaseEntity {
    int Id { get; set; }
  }
  [Description("Entity description.")] //should be copied to emitted class
  [Category("Cat1")]
  public interface IMyEntity : IBaseEntity {
    [Category("Strings"), SampleCustom("v1", Value2 = "v2")] //should be copied to emitted class member
    string StringProp { get; set; }

    int IntProp { get; set; }

    Guid GuidProp { get; set; }

    int? IntNProp { get; set; }

    DateTime DateTimeProp { get; set; }

    IOtherEntity OtherEnt { get; set; }

    IList<IOtherEntity> OtherEntList { get; set; }
  }

  // used in props of IMyEntity
  public interface IOtherEntity {}

  //base class for generated proxy
  public class EntityBase {
    // try either field or property
    public EntityRecord Record; 
    // public EntityRecord Record { get; set; }
  }

  // simplified version of EntityRecord
  public class EntityRecord {
    Dictionary<string, object> _values = new Dictionary<string, object>();

    public object GetValue(string name) {
      var v = _values[name];
      return v;
    }
    public void SetValue(string name, object value) {
      _values[name] = value;
    }
  }


  public class EntityEmitController :ProxyEmitControllerBase {
    FieldInfo _targetRef = typeof(EntityBase).GetField("Record");
    MethodInfo _recordGetValue = typeof(EntityRecord).GetMethod("GetValue");
    MethodInfo _recordSetValue = typeof(EntityRecord).GetMethod("SetValue");

    public EntityEmitController(DynamicAssemblyInfo assemblyInfo)
        : base(assemblyInfo, "Proxemity.UnitTests.EmittedClasses.EntityProxy", typeof(EntityBase), 
                new SampleAttributeHandler()) {
    }

    public override MemberEmitInfo GetMethodEmitInfo(MethodInfo interfaceMethod, PropertyInfo ownerProperty) {
      // it is method, not property
      var propName = ownerProperty.Name; 
      bool isGet = interfaceMethod.Name.StartsWith("get_");
      if (isGet) {
        var args = new object[] { propName };
        return new MemberEmitInfo(interfaceMethod, _targetRef, _recordGetValue, args);
      } else {
        var valuePrm = interfaceMethod.GetParameters()[0];
        var args = new object[] { propName, valuePrm };
        return new MemberEmitInfo(interfaceMethod, _targetRef, _recordSetValue, args); 
      }
    }//method
  }//class

}
