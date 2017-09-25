using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Proxemity {
  using Util = ProxemityUtil;

  public class InterfaceMemberInfo {
    public MemberInfo Member;
    public PropertyInfo OwnerProperty; //for getters/setters only
  }

  /// <summary>Information to use when emitting a member; returned by the emit controller provided by client code.</summary>
  public class EmitInfo {
    public MemberInfo TargetRef;
    public MethodInfo TargetMethod;
    public object[] Arguments;
    public Action<MemberInfo> ReviewCallback;
    public readonly IList<Expression<Func<Attribute>>> CustomAttributes = new List<Expression<Func<Attribute>>>();

    /// <summary>A factory method creates the emit info object for an inteface method. </summary>
    /// <param name="targetRef">A PropertyInfo or FieldInfo meta-object pointing to the member of the proxy base class that holds 
    /// the referenc to the proxy redirect target.</param>
    /// <param name="targetMethod">The method to call.</param>
    /// <param name="arguments">The arguments for the target method. Can contain primitive constants, references to the parameters of the interface method
    /// (as ParameterInfo object), or special values created using ArgBox class and its methods</param>
    /// <param name="reviewCallback">Optional, a review callback will be invoked after the method is emitted. The actual argument will be a MethodBuilder instance.</param>
    /// <returns>Emit info object.</returns>
    public static EmitInfo CreateForMethod(MemberInfo targetRef, MethodInfo targetMethod, object[] arguments, Action<MemberInfo> reviewCallback = null) {
      Util.CheckParam(targetRef, nameof(targetRef));
      Util.CheckParam(targetMethod, nameof(targetMethod));
      return new EmitInfo() { TargetRef = targetRef, TargetMethod = targetMethod, Arguments = arguments, ReviewCallback = reviewCallback };
    }

    /// <summary>A static factory method creates the info object for properties; you can specify a review callback.</summary>
    /// <param name="reviewCallback">Optional, a review callback will be invoked after the property is emitted. The actual argument will be a PropertyBuilder instance.</param>
    /// <returns>Emit info object.</returns>
    /// <remarks>If you need to specify custom attributes on the emitted property, use the AddCustomAttribute method on the returned object.</remarks>
    public static EmitInfo CreateForProperty(Action<MemberInfo> reviewCallback = null) {
      return new EmitInfo() { ReviewCallback = reviewCallback };
    }

    /// <summary>Instructs the emitter to add a custom attribute to the generated member.</summary>
    /// <param name="attributeExpression">A function returning a new attribute instance.</param>
    /// <remarks>The argument function should be a New operator with optional property/field assignments. Ex: () => new MyAttr(123) { AttrProp = "abc" }. 
    public void AddCustomAttribute(Expression<Func<Attribute>> attributeExpression) {
      ExpressionUtil.VerifyAttributeExpression(attributeExpression);
      CustomAttributes.Add(attributeExpression);
    }

    private EmitInfo() { }
  }

  /// <summary>Basic interface for emit controller. Emit controller is provided to ProxyEmitter by outside code. The controller should 
  /// return detail information for a provided interface member. This info is used to emit the member in the proxy class. 
  /// </summary>
  public interface IProxyEmitController {
    /// <summary>Returns emit info for an interface member. Use static factory methods of EmitInfo class to create an instance of 
    /// the class. </summary>
    /// <param name="memberInfo">Interface member information.</param>
    /// <returns>EmitInfo object.</returns>
    /// <remarks>The method is called for interface properties and methods. For a property the expected object is used only
    /// to add custom attributes. Subsequently the emitter will call this method for the getter and setter of the property - as interface methods.
    /// For an interface method the returned object must specify the information about the target method to call. 
    /// </remarks>
    EmitInfo GetEmitInfo(InterfaceMemberInfo memberInfo);
  }

}//
