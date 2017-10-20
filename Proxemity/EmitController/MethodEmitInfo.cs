using System;
using System.Reflection;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>Information for emitting a method, returned by the emit controller provided by client code.</summary>
  public class MethodEmitInfo {
    /// <summary>Interface method.</summary>
    public MethodInfo InterfaceMethod;
    /// <summary>A member (property or field) representing the references to the redirect target.</summary>
    public MemberInfo TargetRef;
    /// <summary>The method to call on redirect target.</summary>
    public MethodInfo TargetMethod;
    /// <summary>Target method arguments. Should be either constants,
    /// or instances of <see cref="ArgBox"/> class for special values./></summary>
    public object[] Arguments;

    /// <summary>Creates an instance. </summary>
    /// <param name="interfaceMethod">The interface method for which the class member should be generated.</param>
    /// <param name="targetRef">A PropertyInfo or FieldInfo meta-object pointing to the member of the proxy base class that holds 
    /// the referenc to the proxy redirect target.</param>
    /// <param name="targetMethod">The method to call.</param>
    /// <param name="arguments">The arguments for the target method. Can contain primitive constants, references to the parameters of the interface method
    /// (as ParameterInfo object), or special values created using ArgBox class and its methods</param>
    /// <returns>Emit info object.</returns>
    public MethodEmitInfo(MethodInfo interfaceMethod, MemberInfo targetRef, MethodInfo targetMethod, object[] arguments) {
      Util.CheckParam(interfaceMethod, nameof(interfaceMethod));
      Util.CheckParam(targetRef, nameof(targetRef));
      Util.CheckParam(targetMethod, nameof(targetMethod));
      InterfaceMethod = interfaceMethod;
      TargetRef = targetRef;
      TargetMethod = targetMethod;
      Arguments = arguments;
    }

  }

}
