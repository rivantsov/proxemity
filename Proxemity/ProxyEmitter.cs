using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>IL Proxy emitter. Emits dynamic class implementing one or more interfaces. </summary>
  /// <remarks>Uses a controller - a sub-class of <see cref="ProxyEmitControllerBase"/> class provided by 
  /// the client code - that provides the information about target methods to call from emitted proxy 
  /// methods. </remarks>
  public class ProxyEmitter {

    ProxyEmitControllerBase _controller; 
    TypeBuilder _typeBuilder;
    HashSet<string> _gettersSetters;

    /// <summary>The class (Type) of the emitted proxy. Set by emitter when emit process is completed.</summary>
    public Type EmittedClass { get; internal set; }

    /// <summary>Creates an instance of the ProxyEmitter class. </summary>
    /// <param name="controller">The information about the proxy class to emit.</param>
    public ProxyEmitter(ProxyEmitControllerBase controller) {
      Util.CheckParam(controller, nameof(controller));
      _controller = controller;
      _typeBuilder = _controller.Assembly.ModuleBuilder.DefineType(_controller.ClassName, TypeAttributes.Class, parent: _controller.BaseType);
      // Constructors
      var constrList = _controller.BaseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      foreach(var constr in constrList)
        BuildConstructor(constr);
    } //constr

    /// <summary>Implements interface on a proxy class - emits members of the interface.</summary>
    /// <param name="interfaceType">Interface type.</param>
    public void ImplementInterface(Type interfaceType) {
      Util.Check(EmittedClass == null, "The proxy class had been finalized and created, cannot add interface.");
      Util.CheckParam(interfaceType, nameof(interfaceType));
      Util.Check(interfaceType.IsInterface, "Invalid interfaceType argument - expected interface type, provided: {0}.", interfaceType);
      _typeBuilder.AddInterfaceImplementation(interfaceType);

      _gettersSetters = new HashSet<string>();
      var allProperties = interfaceType.GetAllProperties();
      var allMethods = interfaceType.GetAllMethods();
      //Properties
      foreach(var iProp in allProperties) {
        BuildProperty(iProp);
      }
      //Methods
      foreach(var iMeth in allMethods) {
        // some methods (getters/setters) are implemented when building properties - skip these 
        if(_gettersSetters.Contains(iMeth.Name))
          continue;
        BuildMethod(iMeth);
      }
      //Attributes
      if (_controller.AttributeHandler != null) {
        var allAttrs = interfaceType.GetCustomAttributes(inherit: true).OfType<Attribute>().ToList();
        var attrBuilders = CreateAttributeBuilders(allAttrs);
        foreach(var atb in attrBuilders)
          _typeBuilder.SetCustomAttribute(atb);
      }
      // call controller to review imlementation
      _controller.OnInterfaceImplemented(interfaceType, _typeBuilder);
    }//method

    /// <summary>Finalizes the emit process and creates a Type representing the emitted proxy class. </summary>
    /// <returns>Type representing the emitted class.</returns>
    /// <remarks>No more emit actions can be performed (ImplementInterface calls) after this method is called.
    /// The proxy Type instance is also saved in the <see cref="EmittedClass"/> property.
    /// </remarks>
    public Type CreateClass() {
      Util.Check(EmittedClass == null, "The emit process is already completed.");
      // last call to controller before finalizing and creating type. Last chance for controller to add smth.
      _controller.OnClassEmitted(_typeBuilder);
      //actually create type
      var typeInfo = _typeBuilder.CreateTypeInfo();
      EmittedClass = typeInfo.AsType();
      _typeBuilder = null; 
      return EmittedClass;
    }

    /// <summary>Returns a proxy factory method corresponding to a constructor with parameter types matching the type arguments of the Func type parameter.</summary>
    /// <typeparam name="TFunc">Func-based generic delegate. The type arguments must match the types of arguments of one of the constructors of the proxy class.
    /// The retun type of the Func must be the proxy base type. </typeparam>
    /// <returns>A function that creates an instance of the proxy.</returns>
    public TFunc GetProxyFactory<TFunc>() {
      Util.Check(EmittedClass != null, "Proxy emit process not completed, proxy class and factories are not available. Call ProxyEmitter.Complete() to complete the process.");
      var func = ProxemityUtil.GetFactoryMethod(EmittedClass, _controller.FactoryMethodName, typeof(TFunc));
      return (TFunc)func;
    }

    /// <summary>Returns current type builder. You can use it to customize the emitted type before it is finalized and created by <see cref="CreateClass"/> method. </summary>
    /// <returns>Type builder instance.</returns>
    public TypeBuilder GetTypeBuilder() {
      Util.Check(EmittedClass == null, "The proxy class had been finalized and created, type builder is not available.");
      return _typeBuilder;
    }

    // Private methods ====================================================================================

    private ConstructorBuilder BuildConstructor(ConstructorInfo constructor) {
      var attrs = MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
      if (constructor.IsPublic)
        attrs |=  MethodAttributes.Public;
      var paramTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
      var cb = _typeBuilder.DefineConstructor(attrs, CallingConventions.Standard, paramTypes);
      var ilGen = cb.GetILGenerator();
      ilGen.Emit(OpCodes.Ldarg_0);
      for(int i = 1; i <= paramTypes.Length; i++)
        ilGen.Emit(OpCodes.Ldarg, i);
      ilGen.Emit(OpCodes.Call, constructor);
      ilGen.Emit(OpCodes.Ret);
      BuildProxyFactory(cb, paramTypes); 
      return cb; 
    }

    private void BuildProxyFactory(ConstructorBuilder constr, Type[] paramTypes) {
      var attrs = MethodAttributes.HideBySig | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.Static | MethodAttributes.Public;
      var meth = _typeBuilder.DefineMethod(_controller.FactoryMethodName, attrs, _typeBuilder, paramTypes);
      var ilGen = meth.GetILGenerator(); 
      // load args
      for(int i = 0; i < paramTypes.Length; i++) {
        ilGen.Emit(OpCodes.Ldarg, i);
      }
      // call
      ilGen.Emit(OpCodes.Newobj, constr);
      ilGen.Emit(OpCodes.Ret);
    }

    private PropertyBuilder BuildProperty(PropertyInfo iprop) {
      var interfaceType = iprop.DeclaringType; 
      var pb = _typeBuilder.DefineProperty(iprop.Name, PropertyAttributes.None, iprop.PropertyType, Type.EmptyTypes);

      if(iprop.GetMethod != null) {
        var getter = BuildMethod(iprop.GetMethod, iprop);
        pb.SetGetMethod(getter);
        var igetter = interfaceType.GetMethod(iprop.GetMethod.Name);
        _typeBuilder.DefineMethodOverride(getter, igetter);
        _gettersSetters.Add(getter.Name);
      }
      if(iprop.SetMethod != null) {
        var setter = BuildMethod(iprop.SetMethod, iprop);
        pb.SetSetMethod(setter);
        var isetter = interfaceType.GetMethod(iprop.SetMethod.Name);
        _typeBuilder.DefineMethodOverride(setter, isetter);
        _gettersSetters.Add(setter.Name);
      }
      //Attributes
      // foreach(var attrExpr in emitInfo.AttributeFactories)
      // pb.SetCustomAttribute(CreateAttributeBuilder(attrExpr));

      //Attributes
      if(_controller.AttributeHandler != null) {
        var allAttrs = iprop.GetCustomAttributes(inherit: true).OfType<Attribute>().ToList();
        var attrBuilders = CreateAttributeBuilders(allAttrs);
        foreach(var atb in attrBuilders)
          pb.SetCustomAttribute(atb);
      }
      _controller.OnPropertyEmitted(iprop, pb);
      return pb;
    } //method

    private MethodBuilder BuildMethod(MethodInfo iMethod, PropertyInfo ownerProperty = null) {
      // Invoke controller to get emit info with target method
      var emitInfo = _controller.GetMethodEmitInfo(iMethod, ownerProperty);
      //validate some basic things
      ValidateEmitInfo(emitInfo, iMethod.Name); 

      // start building method
      var iParams = iMethod.GetParameters().ToList();
      var prmTypes = iParams.Select(p => p.ParameterType).ToArray();
      var attrs = MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Public;
      var mb = _typeBuilder.DefineMethod(iMethod.Name, attrs, iMethod.ReturnType, prmTypes);
      if(ownerProperty == null)
        _typeBuilder.DefineMethodOverride(mb, iMethod);

      var ilGen = mb.GetILGenerator();
      PushTargetRef(ilGen, emitInfo);
      // we now have target obj on top of the stack
      // load arguments of target method
      var targetParams = emitInfo.TargetMethod.GetParameters();
      for(int i = 0; i < emitInfo.Arguments.Length; i++) {
        var targetArg = emitInfo.Arguments[i];
        var targetPrm = targetParams[i];
        PushArgument(ilGen, iMethod, targetArg, targetPrm.ParameterType);
      }
      // invoke target method
      ilGen.Emit(OpCodes.Callvirt, (MethodInfo)emitInfo.TargetMethod);
      // process return value
      ConvertValueIfNeeded(ilGen, emitInfo.TargetMethod.ReturnType, iMethod.ReturnType, iMethod);
      //return
      ilGen.Emit(OpCodes.Ret);
      //custom attributes
      if (_controller.AttributeHandler != null) {
        var allAttrs = iMethod.GetCustomAttributes(inherit: true).OfType<Attribute>().ToList();
        var attrBuilders = CreateAttributeBuilders(allAttrs);
        foreach(var clone in attrBuilders)
          mb.SetCustomAttribute(clone);
      }

      _controller.OnMethodEmitted(iMethod, mb);
      return mb;
    }

    private IList<CustomAttributeBuilder> CreateAttributeBuilders(IList<Attribute> attrs) {
      var result = new List<CustomAttributeBuilder>();
      if(_controller.AttributeHandler == null)
        return result; 
      foreach(var lambda in this._controller.AttributeHandler.Descriptors) {
        var attrType = lambda.Body.Type;
        var attrsOfType = attrs.Where(a => a.GetType() == attrType).ToList();
        foreach(var attr in attrsOfType)
          result.Add(CreateAttributeBuilderFromCloner(lambda, attr));
      }
      return result;
    }

    private void PushTargetRef(ILGenerator ilGen, MemberEmitInfo emitInfo) {
      // push ref to target from field/prop
      var targetRef = emitInfo.TargetRef; 
      switch(targetRef) {
        case FieldInfo fld:
          ilGen.Emit(OpCodes.Ldarg_0); //load ref to this object
          ilGen.Emit(OpCodes.Ldfld, fld); // load field value (of this object)
          break;
        case PropertyInfo prop:
          var getter = prop.GetMethod; //getter method
          ilGen.Emit(OpCodes.Ldarg_0); //load ref to this object
          ilGen.Emit(OpCodes.Callvirt, getter);
          break;
        default:
          Util.Throw("Invalid target ref type: {0}, expected field or property.", targetRef.MemberType);
          break;
      }
    }

    private void PushArgument(ILGenerator ilGen, MethodInfo iMethod, object arg, Type expectedType) {
      //Null
      if(arg == null) {
        var allowNulll = expectedType.GetTypeInfo().IsValueType;
        Util.Check(allowNulll, "Method {0}: Invalid argument value (null), expected a value type {1}", iMethod.Name, expectedType.Name);
        ilGen.Emit(OpCodes.Ldnull);
        return;
      }
      //Parameter
      if(arg is ParameterInfo) {
        var iprm = (ParameterInfo)arg;
        var iParams = iMethod.GetParameters().ToList();
        var iPrmIndex = iParams.IndexOf(iprm);
        Util.Check(iPrmIndex != -1, "Invalid parameter object reference ({0}) in arguments of method {1}. Must be an input parameter of the interface method. ",
           iprm.Name, iMethod.Name);
        ilGen.Emit(OpCodes.Ldarg, iPrmIndex + 1); //plus 1 to account for 'this' which is always #0
        ConvertValueIfNeeded(ilGen, iprm.ParameterType, expectedType, iMethod);
        return;
      }
      //ArgBox
      if(arg is ArgBox) {
        PushArgBoxArgument(ilGen, iMethod, arg, expectedType);
        return; 
      }
      //Primitive value
      var argType = arg.GetType();
      var isPrimitive = argType == typeof(string) || argType.IsValueType;
      if (isPrimitive) {
        PushPrimitiveValueArg(ilGen, arg, expectedType, iMethod);
        return; 
      }
      Util.Check(isPrimitive, "Method {0}: Invalid argument value {1} (type {2}). Must be string or primitive value type.", 
        iMethod.Name, arg, arg.GetType().Name);
    }

    private void PushArgBoxArgument(ILGenerator ilGen, MethodInfo iMethod, object targetArg, Type expectedType) {
      var argBox = (ArgBox)targetArg;
      switch(argBox.Kind) {
        case ArgBoxKind.ProxySelfRef:
          ilGen.Emit(OpCodes.Ldarg_0); //load ref to self 
          ConvertValueIfNeeded(ilGen, _controller.BaseType, expectedType, iMethod);
          break;
        case ArgBoxKind.StaticInstanceRef:
          var member = argBox.SingletonMember;
          switch(member.MemberType) {
            case MemberTypes.Field:
              var fld = (FieldInfo)member;
              ilGen.Emit(OpCodes.Ldsfld, fld);
              ConvertValueIfNeeded(ilGen, fld.FieldType, expectedType, iMethod);
              break;
            case MemberTypes.Property:
              var prop = (PropertyInfo)member;
              ilGen.Emit(OpCodes.Call, prop.GetMethod);
              ConvertValueIfNeeded(ilGen, prop.PropertyType, expectedType, iMethod);
              break; 

          }
          break;
        case ArgBoxKind.Array:
          //load array length and create array
          ilGen.Emit(OpCodes.Ldc_I4, argBox.Array.Length);
          ilGen.Emit(OpCodes.Newarr, typeof(object));
          // copy values to array
          for(int i=0; i < argBox.Array.Length; i++) {
            ilGen.Emit(OpCodes.Dup); //duplicate ref to array; it will be used to push value into array
            ilGen.Emit(OpCodes.Ldc_I4, i); //array index
            var arg = argBox.Array[i];
            PushArgument(ilGen, iMethod, arg, typeof(object));
            // copy returned value to array; we have now array ref, array index, value on the stack
            ilGen.Emit(OpCodes.Stelem_Ref);
          }
          break;
        default:
          Util.Throw("Invalid ArgBox.Kind value: {0}, method: {1}. ", argBox.Kind, iMethod.Name);
          break; 
      }
    }

    private void PushPrimitiveValueArg(ILGenerator ilGen, object arg, Type expectedType, MethodInfo iMethod) {
      //value type
      switch(arg) {
        case string strArg:
          ilGen.Emit(OpCodes.Ldstr, strArg);
          break;
        case int intArg:
          ilGen.Emit(OpCodes.Ldc_I4, intArg);
          break;
        default:
          //Otherwize throw
          Util.Throw("Invalid arg value {0} (type: {1}); method: {2}", arg, arg.GetType(), iMethod.Name);
          break; 
      }
      ConvertValueIfNeeded(ilGen, arg.GetType(), expectedType, iMethod);
    }

    private void ConvertValueIfNeeded(ILGenerator ilGen, Type type, Type expectedType, MethodInfo iMethod) {
      if(type == expectedType)
        return; 
      //special case for return value: if no return value is expected, pop it
      if (expectedType == typeof(void)) {
        ilGen.Emit(OpCodes.Pop);
        return; 
      }
      // if it is (valueType)-> object, then it is boxing
      if (expectedType == typeof(object) && type.GetTypeInfo().IsValueType) {
        ilGen.Emit(OpCodes.Box, type);
        return;
      }
      // if it is object -> (valueType), then it is unboxing
      if(type == typeof(object) && expectedType.GetTypeInfo().IsValueType) {
        ilGen.Emit(OpCodes.Unbox_Any, expectedType);
        return;
      }
      
      // simply convert
      ilGen.Emit(OpCodes.Castclass, expectedType);

    }

    private CustomAttributeBuilder CreateAttributeBuilder(Expression<Func<Attribute>> attrExpr) {
      var cInfo = ExpressionUtil.ParseAttributeExpression(attrExpr);
      var attrBuilder = new CustomAttributeBuilder(cInfo.Constructor, cInfo.Args, cInfo.Properties, cInfo.PropertyValues, cInfo.Fields, cInfo.FieldValues); //, constrArgs, named)
      return attrBuilder; 
    }
    private CustomAttributeBuilder CreateAttributeBuilderFromCloner(LambdaExpression cloner, Attribute attr) {
      var cInfo = ExpressionUtil.ParseAttributeClonerExpression(cloner, attr);
      var attrBuilder = new CustomAttributeBuilder(cInfo.Constructor, cInfo.Args, cInfo.Properties, cInfo.PropertyValues, cInfo.Fields, cInfo.FieldValues); //, constrArgs, named)
      return attrBuilder;

    }
    private void ValidateEmitInfo(MemberEmitInfo emitInfo, string iMethodName) {
      var targetRef = emitInfo.TargetRef;
      Util.Check(targetRef != null, "EmitInfo.TargetRef may not be null; method name: {0}", iMethodName);
      Util.Check(targetRef.MemberType == MemberTypes.Field || targetRef.MemberType == MemberTypes.Property,
        "Invalid target ref member type: {0}, expected field or property.", targetRef.MemberType);
      var targetParams = emitInfo.TargetMethod.GetParameters().ToList();
      Util.Check(targetParams.Count == emitInfo.Arguments.Length,
            "Invalid EmitInfo for method {0}, arg count {1} does not match target method parameter count {2}",
                   iMethodName, emitInfo.Arguments.Length, targetParams.Count);
    }


  }//class
}//ns
