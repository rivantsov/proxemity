using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Proxemity {

  // ArgBox is used to encode special value as a desired argument of the call to target function. 
  using Util = ProxemityUtil;

  public enum ArgBoxKind {
    Array,
    StaticInstanceRef,
    ProxySelfRef,
  }

  /// <summary>ArgBox is used by Proxy emit controller call back method as a special value in one or more of the returned Arguments.
  /// It tells the emitter that an argument is a special value that must be interpreted/emitted in a certain way.  
  /// </summary>
  public class ArgBox {
    public ArgBoxKind Kind { get; internal set; }
    //array
    internal object[] Array;
    //singleton ref
    internal MemberInfo SingletonMember;

    private ArgBox() { } //to prevent explicit creation

    public override string ToString() {
      return "(" + Kind.ToString() + ")"; 
    }

    //Static factory methods
    public static ArgBox CreateArray(object[] items) {
      return new ArgBox() { Kind = ArgBoxKind.Array, Array = items };
    }
    public static ArgBox CreateStaticInstanceRef(Expression<Func<object>> selector) {
      var errMsgTemplate = "Invalid expression for static instance reference, must be a selector of static property or field. Selector: {0}";
      Util.CheckParam(selector, nameof(selector));
      var target = selector.Body;
      // we might have conversion expression here for value types - member access is under it
      if(target.NodeType == ExpressionType.Convert)
        target = ((UnaryExpression)target).Operand;
      var memberAccess = target as MemberExpression;
      bool isOk = memberAccess != null &&
        (memberAccess.Member.MemberType == MemberTypes.Field || memberAccess.Member.MemberType == MemberTypes.Property);
      Util.Check(isOk, errMsgTemplate, selector);
      var member = memberAccess.Member;
      Util.Check(member.IsStatic(), errMsgTemplate, selector);
      return new ArgBox() { Kind = ArgBoxKind.StaticInstanceRef, SingletonMember = member };
    }

    public static ArgBox CreateProxySelfRef() {
      return new ArgBox() { Kind = ArgBoxKind.ProxySelfRef }; 
    }


  }//class

} //ns
