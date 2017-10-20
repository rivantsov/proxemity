using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>Argument box kind. </summary>
  public enum ArgBoxKind {
    /// <summary>The argument is an array of values. </summary>
    Array,
    /// <summary>The argument is a references to static singleton. </summary>
    StaticInstanceRef,
    /// <summary>The argument is a reference to the proxy instance itself. </summary>
    ProxySelfRef,
  }

  /// <summary><c>ArgBox</c> is used by Proxy emit controller call back method as a special value in Arguments array that 
  /// specifies the arguments to use in the call to target method.
  /// It tells the emitter that an argument is a special value that must be interpreted/emitted in a certain way.  
  /// </summary>
  public class ArgBox {
    /// <summary>Specifies box kind. </summary>
    public ArgBoxKind Kind { get; internal set; }
    //array
    internal object[] Array;
    //singleton ref
    internal MemberInfo SingletonMember;

    private ArgBox() { } //to prevent explicit creation

    /// <summary>Returns string representation of an object. </summary>
    /// <returns>String representation.</returns>
    public override string ToString() {
      return "(" + Kind.ToString() + ")"; 
    }

    //Static factory methods
    /// <summary>Creates an <c>ArgBox</c> representing an array.</summary>
    /// <param name="items">Array content.</param>
    /// <returns>An ArgBox instance.</returns>
    public static ArgBox CreateArray(object[] items) {
      return new ArgBox() { Kind = ArgBoxKind.Array, Array = items };
    }

    /// <summary>Creates a box representing a static global singleton object. </summary>
    /// <param name="descriptor">A Func delegate returning the singleton.</param>
    /// <returns>The created ArgBox object.</returns>
    public static ArgBox CreateStaticInstanceRef(Expression<Func<object>> descriptor) {
      var errMsgTemplate = "Invalid expression for static instance reference, must be a selector of static property or field. Selector: {0}";
      Util.CheckParam(descriptor, nameof(descriptor));
      var target = descriptor.Body;
      // we might have conversion expression here for value types - member access is under it
      if(target.NodeType == ExpressionType.Convert)
        target = ((UnaryExpression)target).Operand;
      var memberAccess = target as MemberExpression;
      bool isOk = memberAccess != null &&
        (memberAccess.Member.MemberType == MemberTypes.Field || memberAccess.Member.MemberType == MemberTypes.Property);
      Util.Check(isOk, errMsgTemplate, descriptor);
      var member = memberAccess.Member;
      Util.Check(member.IsStatic(), errMsgTemplate, descriptor);
      return new ArgBox() { Kind = ArgBoxKind.StaticInstanceRef, SingletonMember = member };
    }

    /// <summary>Creates a box representing a reference to the proxy instance itself. </summary>
    /// <returns>The created ArgBox object.</returns>
    public static ArgBox CreateProxySelfRef() {
      return new ArgBox() { Kind = ArgBoxKind.ProxySelfRef }; 
    }


  }//class
} //ns
