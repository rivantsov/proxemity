using System;
using System.Reflection;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>Information to use when emitting a member; returned by the emit controller provided by client code.</summary>
  public class MemberEmitInfo {
    /// <summary>Interface member.</summary>
    public MemberInfo InterfaceMember;
    /// <summary>A member (property or field) representing the references to the redirect target.</summary>
    public MemberInfo TargetRef;
    /// <summary>The method to call on redirect target.</summary>
    public MethodInfo TargetMethod;
    /// <summary>Target method arguments. Should be either constants,
    /// or instances of <see cref="ArgBox"/> class for special values./></summary>
    public object[] Arguments;

    /// <summary>Creates an instance. </summary>
    /// <param name="interfaceMember">The interface member for which the class member should be generated.</param>
    /// <param name="targetRef">A PropertyInfo or FieldInfo meta-object pointing to the member of the proxy base class that holds 
    /// the referenc to the proxy redirect target.</param>
    /// <param name="targetMethod">The method to call.</param>
    /// <param name="arguments">The arguments for the target method. Can contain primitive constants, references to the parameters of the interface method
    /// (as ParameterInfo object), or special values created using ArgBox class and its methods</param>
    /// <returns>Emit info object.</returns>
    public MemberEmitInfo(MemberInfo interfaceMember, MemberInfo targetRef, MethodInfo targetMethod, object[] arguments) {
      Util.CheckParam(interfaceMember, nameof(interfaceMember));
      Util.CheckParam(targetRef, nameof(targetRef));
      Util.CheckParam(targetMethod, nameof(targetMethod));
      InterfaceMember = interfaceMember;
      TargetRef = targetRef;
      TargetMethod = targetMethod;
      Arguments = arguments;
    }

  }

}
