using System;
using System.Collections.Generic;
using System.Text;

namespace Proxemity.UnitTests {

  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Property)]
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

  // this is a helper object that handles copying attributes from interface to emitted class.
  public class SampleAttributeHandler : AttributeHandler {
    public SampleAttributeHandler() : base(addStandardAttributes: true) {
      base.AddDescriptor<SampleCustomAttribute>(a => new SampleCustomAttribute(a.Value1) { Value2 = a.Value2 });
    }
  }
}
