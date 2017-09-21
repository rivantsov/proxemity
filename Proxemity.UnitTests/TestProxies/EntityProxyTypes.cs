using System;
using System.Collections.Generic;
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
  public interface IMyEntity : IBaseEntity {
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
    public IEntityRecord Record; 
    // public IEntityRecord Record { get; set; }
  }

  // simplified version of EntityRecord
  public interface IEntityRecord {
    void SetValue(string name, object value);
    object GetValue(string name);
  }

  public class EntityRecordMock : IEntityRecord {
    Dictionary<string, object> _values = new Dictionary<string, object>();

    public object GetValue(string name) {
      var v = _values[name];
      return v;
    }
    public void SetValue(string name, object value) {
      _values[name] = value;
    }
  }


  public class EntityEmitController :IProxyEmitController {
    FieldInfo _targetRef = typeof(EntityBase).GetField("Record");
    MethodInfo _recordGetValue = typeof(IEntityRecord).GetMethod("GetValue");
    MethodInfo _recordSetValue = typeof(IEntityRecord).GetMethod("SetValue");

    public EmitInfo GetEmitInfo(InterfaceMemberInfo iMemberInfo) {
      if(iMemberInfo.Member.MemberType == MemberTypes.Property)
        return EmitInfo.CreateForProperty();
      // it is method, not property
      var iMethod = (MethodInfo)iMemberInfo.Member; 
      var propInfo = iMemberInfo.OwnerProperty;
      bool isGet = iMethod.Name.StartsWith("get_");
      if (isGet) {
        var args = new object[] { propInfo.Name };
        return EmitInfo.CreateForMethod(_targetRef, _recordGetValue, args);
      } else {
        var valuePrm = iMethod.GetParameters()[0];
        var args = new object[] { propInfo.Name, valuePrm };
        return EmitInfo.CreateForMethod(_targetRef, _recordSetValue, args); 
      }
    }//method
  }//class

}
