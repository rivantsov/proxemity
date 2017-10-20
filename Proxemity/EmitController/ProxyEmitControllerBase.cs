using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>Controls the process of emitting the proxy class. Client code should implement sub-class with custom logic
  /// for guiding the emitter. </summary>
  /// <remarks>The only requied method to implement is <see cref="GetMethodEmitInfo(MethodInfo, PropertyInfo)"/>.</remarks>
  public abstract class ProxyEmitControllerBase {
    /// <summary>Dynamic assembly information.</summary>
    public readonly DynamicAssemblyInfo Assembly;
    /// <summary>The full name (including namespace) of the proxy class to emit.</summary>
    public readonly string ClassName;
    /// <summary>The base class of the proxy.</summary>
    public readonly Type BaseType;
    /// <summary>The object responsible for copying attributes from interface (members) to the emitted type (members). </summary>
    public AttributeHandler AttributeHandler; 
    /// <summary>The name of the emitted static factory method(s). </summary>
    public string FactoryMethodName = "Create_";

    /// <summary>Creates an instance. </summary>
    /// <param name="assembly">Dynamic assembly information. Use <see cref="DynamicAssemblyInfo.Create"/> static factory method to create an instance.</param>
    /// <param name="className">The full class name of IL-emitted proxy, including namespace.</param>
    /// <param name="baseType">The base type of the proxy class.</param>
    /// <param name="attributeHandler">Attribute handler, optional.</param>
    public ProxyEmitControllerBase(DynamicAssemblyInfo assembly, string className, Type baseType, 
          AttributeHandler attributeHandler = null) {
      Util.CheckParam(assembly, nameof(assembly));
      Util.CheckParam(className, nameof(className));
      Util.CheckParam(baseType, nameof(baseType));
      Assembly = assembly;
      ClassName = className;
      BaseType = baseType;
      AttributeHandler = attributeHandler; 
    }

    /// <summary>Returns emit info for an interface method.</summary>
    /// <param name="interfaceMethod">Interface method.</param>
    /// <param name="parentProperty">For property get/set methods only, the owner property info.</param>
    /// <returns>Emit info object.</returns>
    /// <remarks>The method is called for methods and properties getters/setters. The returned object provides the information 
    /// about the target method to call and its arguments. </remarks>
    public abstract MemberEmitInfo GetMethodEmitInfo(MethodInfo interfaceMethod, PropertyInfo parentProperty = null);

    /// <summary>Called by the emitter after the proxy method is emitted.</summary>
    /// <param name="interfaceMethod">Interface member.</param>
    /// <param name="builder">Method builder for the emitted method.</param>
    public virtual void OnMethodEmitted(MethodInfo interfaceMethod, MethodBuilder builder) {  }

    /// <summary>Called by the emitter after the proxy property is emitted.</summary>
    /// <param name="interfaceProperty">Interfact property.</param>
    /// <param name="builder">Property builder for the emitted property. </param>
    public virtual void OnPropertyEmitted(PropertyInfo interfaceProperty, PropertyBuilder builder) {  }

    /// <summary>Called by the emitter after an interface is implemented (all members are emitted). </summary>
    /// <param name="interfaceType">Interface type.</param>
    /// <param name="builder">Type builder representing the emitted class.</param>
    public virtual void OnInterfaceImplemented(Type interfaceType, TypeBuilder builder) { }

    /// <summary>Called by the emitter right before the emitted class is finalized and Type instance is created. </summary>
    /// <param name="builder">Type builder representing the emitted class.</param>
    public virtual void OnClassEmitted(TypeBuilder builder) { }

  }//class

} //ns
