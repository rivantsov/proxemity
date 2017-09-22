using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>A container for dynamic assembly builder. Use <see cref="Create(string)"/>  method to create an instance.</summary>
  /// <remarks>You can create an assembly once and then use it to build multiple proxy classes.</remarks>
  public class DynamicAssemblyInfo {
    /// <summary>Assembly builder.</summary>
    public readonly AssemblyBuilder AssemblyBuilder;
    /// <summary>Module builder.</summary>
    public readonly ModuleBuilder ModuleBuilder;
    /// <summary>Creates a new instance.</summary>
    public DynamicAssemblyInfo(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder) {
      AssemblyBuilder = assemblyBuilder;
      ModuleBuilder = moduleBuilder; 
    }
    /// <summary>Creates a dynamic assembly, returns an object containing information about it. The returned 
    /// object can be used to emit multiple proxy classes using ProxyEmitter.</summary>
    /// <param name="assemblyName">Full assembly name.</param>
    /// <returns>Assembly info object, a descriptor to use as parameter for other emitter methods.</returns>
    public static DynamicAssemblyInfo Create(string assemblyName) {
      var asmName = new AssemblyName(assemblyName);
      asmName.Version = new Version(1, 0, 0, 0);
      var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
      var moduleBuilder = asmBuilder.DefineDynamicModule("Main");
      return new DynamicAssemblyInfo(asmBuilder, moduleBuilder);
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

    /// <summary>The class (Type) of the emitted proxy. Set by emitter when emit process is completed.</summary>
    public Type EmittedClass { get; internal set; }

    /// <summary>Creates a proxy class info instance. </summary>
    /// <param name="assembly">Dynamic assembly information. Use <see cref="DynamicAssemblyInfo.Create"/> static factory method to create dynamic assembly.</param>
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

    /// <summary>Returns a proxy factory method corresponding to constructor with parameter types matching the type arguments of the Func type parameter.</summary>
    /// <typeparam name="TFunc">Func-based generic delegate. The type arguments must match the types of arguments of one of the constructors of the proxy class.
    /// The retun type of the Func must be the proxy base type. </typeparam>
    /// <returns>A function that creates an instance of the proxy.</returns>
    public TFunc GetProxyFactory<TFunc>() {
      Util.Check(EmittedClass != null, "Proxy emit process not completed, proxy class and factories are not available. Call ProxyEmitter.Complete() to complete the process.");
      var func = ProxemityUtil.GetFactory(EmittedClass, typeof(TFunc)); 
      return (TFunc)func; 
    }
  }//class

} //ns
