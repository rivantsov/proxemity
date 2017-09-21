using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>A container for dynamic assembly builder. Use ProxyEmitter.CreateDynamicAssembly method to create an instance.</summary>
  /// <remarks>You can create an assembly once and then use it to build multiple proxy classes.</remarks>
  public class DynamicAssemblyInfo {
    public readonly AssemblyBuilder AssemblyBuilder;
    public readonly ModuleBuilder ModuleBuilder;
    public DynamicAssemblyInfo(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder) {
      AssemblyBuilder = assemblyBuilder;
      ModuleBuilder = moduleBuilder; 
    }
  }


  /// <summary>Container for information about a proxy class to emit. </summary>
  public class ProxyClassInfo {
    /// <summary>Dynamic assembly information.</summary>
    public readonly DynamicAssemblyInfo Assembly;
    /// <summary>The full name (including namespace) of the proxy class to emit.</summary>
    public readonly string ClassName;
    /// <summary>The base class of the proxy.</summary>
    public readonly Type BaseType;
    /// <summary>List of functors describing the attributes to put on the emitted proxy class.</summary>
    public readonly IList<Expression<Func<Attribute>>> CustomAttributes = new List<Expression<Func<Attribute>>>();

    /// <summary>Creates a proxy class info instance. </summary>
    /// <param name="assembly">Dynamic assembly information. Use ProxyEmitter.CreateDynamicAssembly static method to create dynamic assembly.</param>
    /// <param name="className">The full class name of IL-emitted proxy, including namespace.</param>
    /// <param name="baseType">The base type of the proxy class.</param>
    public ProxyClassInfo(DynamicAssemblyInfo assembly, string className, Type baseType) {
      Util.CheckParam(assembly, nameof(assembly));
      Util.CheckParam(className, nameof(className));
      Util.CheckParam(baseType, nameof(baseType));
      Assembly = assembly;
      ClassName = className;
      BaseType = baseType; 
    }

    /// <summary>Instructs the emitter to add a custom attribute to the generated class.</summary>
    /// <param name="attributeExpression">A function returning a new attribute instance. </param>
    /// <remarks>The argument function should be a New operator with optional property/field assignments. Ex: () => new MyAttr(123) { AttrProp = "abc" }. 
    /// </remarks>
    public void AddCustomAttribute(Expression<Func<Attribute>> attributeExpression) {
      ExpressionUtil.VerifyAttributeExpression(attributeExpression);
      CustomAttributes.Add(attributeExpression);
    }
  }//class

} //ns
