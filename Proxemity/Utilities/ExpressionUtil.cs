using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Proxemity {
  using Util = ProxemityUtil; 

  internal static class ExpressionUtil {

    internal static void VerifyAttributeDescriptor<TAttribute>(Expression<Func<TAttribute, TAttribute>> expr) where TAttribute : Attribute {
      Util.Check(expr != null, "Attribute expression parameter may not be null.");
      Util.Check(expr.Body.NodeType == ExpressionType.New || expr.Body.NodeType == ExpressionType.MemberInit,
        "Invalid attribute expression, must be a New expression, for ex: '()=>new DescriptionAttribute(descr)'. "
       + "Expr: {0}", expr);
    }

    internal class AttributeConstructorInfo {
      public ConstructorInfo Constructor;
      public object[] Args = new object[] { };
      public PropertyInfo[] Properties = new PropertyInfo[]  { };
      public object[] PropertyValues = new object[] { };
      public FieldInfo[] Fields = new FieldInfo[] { };
      public object[] FieldValues = new object[] { };
    }

    internal static AttributeConstructorInfo ParseAttributeDescriptor(LambdaExpression descriptor, Attribute attrInstance) {
      var attrParam = descriptor.Parameters[0];
      // VerifyAttributeExpression(cloner);
      var res = new AttributeConstructorInfo();
      // We might have 2 cases: 
      //  body is NewExpression - call to constructor only ( ()=> new A(1, 2, 3); )
      //  body is MemberInit expr - call to constructor with extra property initialization ( ()=> new A(1, 2, 3) {p = smth}; )
      NewExpression newExpr = null;
      switch(descriptor.Body.NodeType) {
        case ExpressionType.MemberInit:
          var initExpr = (MemberInitExpression)descriptor.Body;
          newExpr = initExpr.NewExpression;
          //Check that every binding is an assignment
          var firstNonAssignment = initExpr.Bindings.FirstOrDefault(b => b.BindingType != MemberBindingType.Assignment);
          Util.Check(firstNonAssignment == null, "Invalid attribute initialization expression for member {0}, must be assignment.", firstNonAssignment?.Member.Name);
          var bindings = initExpr.Bindings.OfType<MemberAssignment>().ToList();
          var propBindings = bindings.Where(b => b.Member.MemberType == MemberTypes.Property).ToList();
          res.Properties = propBindings.Select(b => (PropertyInfo)b.Member).ToArray();
          res.PropertyValues = propBindings.Select(b => Evaluate(b.Expression, attrParam, attrInstance)).ToArray();
          var fieldBindings = bindings.Where(b => b.Member.MemberType == MemberTypes.Field).ToList();
          res.Fields = fieldBindings.Select(b => (FieldInfo)b.Member).ToArray();
          res.FieldValues = fieldBindings.Select(b => Evaluate(b.Expression, attrParam, attrInstance)).ToArray();
          break;
        case ExpressionType.New:
          newExpr = (NewExpression)descriptor.Body;
          break;
        default:
          Util.Throw("Invalid attribute descriptor expression, must be 'new' operator. Expression: {0}", descriptor.Body);
          break; 
      }
      res.Constructor = newExpr.Constructor;
      res.Args = newExpr.Arguments.Select(ae => Evaluate(ae, attrParam, attrInstance)).ToArray();
      return res;
    }
    private static object Evaluate(Expression expr, ParameterExpression attrParam, Attribute attr) {
      switch(expr) {
        case ConstantExpression ce:
          return ce.Value;
        default:
          var fn = Expression.Lambda(expr, attrParam).Compile();
          var result = fn.DynamicInvoke(attr);
          return result;
      }
    }


  }//class
}
