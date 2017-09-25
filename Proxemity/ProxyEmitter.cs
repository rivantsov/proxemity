using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Proxemity {
  using Util = ProxemityUtil;

  /// <summary>IL Proxy emitter. Emits dynamic class implementing one or more interfaces. </summary>
  public class ProxyEmitter {

    ProxyClassInfo _proxyClassInfo; 
    TypeBuilder _typeBuilder;
    IList<MethodBuilder> _builtMethods;

    /// <summary>Creates an instance of the ProxyEmitter class. </summary>
    /// <param name="proxyClassInfo">The information about the proxy class to emit.</param>
    public ProxyEmitter(ProxyClassInfo proxyClassInfo) {
      Util.CheckParam(proxyClassInfo, nameof(proxyClassInfo));
      _proxyClassInfo = proxyClassInfo;
      _typeBuilder = _proxyClassInfo.Assembly.ModuleBuilder.DefineType(_proxyClassInfo.ClassName, TypeAttributes.Class, parent: _proxyClassInfo.BaseType);
      // Constructors
      var constrList = _proxyClassInfo.BaseType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      foreach(var constr in constrList)
        BuildConstructor(constr);
      //Attributes 
      foreach(var attrExpr in _proxyClassInfo.CustomAttributes)
          _typeBuilder.SetCustomAttribute(CreateAttributeBuilder(attrExpr));
    } //constr

    /// <summary>Implements interface on a proxy class - emits members of the interface.</summary>
    /// <param name="interfaceType">Interface type.</param>
    /// <param name="controller">Emit controller - an object that will provide details about emitting 
    /// interface members, one at a time.</param>
    public void ImplementInterface(Type interfaceType, IProxyEmitController controller) {
      Util.Check(!IsCompleted(), "The proxy class had been finalized and created, cannot add interface.");
      Util.CheckParam(interfaceType, nameof(interfaceType));
      Util.CheckParam(controller, nameof(controller));
      Util.Check(interfaceType.IsInterface, "Invalid interfaceType argument - expected interface type, provided: {0}.", interfaceType);

      _typeBuilder.AddInterfaceImplementation(interfaceType);

      _builtMethods = new List<MethodBuilder>();
      var allProperties = interfaceType.GetAllProperties();
      var allMethods = interfaceType.GetAllMethods();
      //Properties
      foreach(var iProp in allProperties) {
        BuildProperty(controller, iProp);
      }
      //Methods
      foreach(var iMeth in allMethods) {
        // some methods (getters/setters) are implemented when building properties - skip these 
        var alreadyDone = _builtMethods.FirstOrDefault(m => m.Name == iMeth.Name);
        if(alreadyDone != null)
          continue;
        BuildMethod(controller, iMeth);
      }
    }//method

    /// <summary>Returns current type builder. You can use it to customize the emitted type before it is finalized and created by <see cref="Complete"/> method. </summary>
    /// <returns>Type builder instance.</returns>
    public TypeBuilder GetTypeBuilder() {
      Util.Check(!IsCompleted(), "The proxy class had been finalized and created, type builder is not available.");
      return _typeBuilder; 
    }

    /// <summary>Returns true if emit process is completed and the type is created.</summary>
    public bool IsCompleted() {
      return _proxyClassInfo.EmittedClass != null; 
    }
    /// <summary>Finalizes the emit process and creates a TypeInfo representing the emitted proxy class. </summary>
    /// <returns>Type representing the emitted class.</returns>
    /// <remarks>No more emit actions can be performed (ImplementInterface calls) after this method is called.
    /// The proxy Type instance is also saved in the <see cref="ProxyClassInfo.EmittedClass"/> property of the class info.
    /// </remarks>
    public Type Complete() {
      Util.Check(!IsCompleted(), "The emit process is already completed.");
      var typeInfo = _typeBuilder.CreateTypeInfo();
      _proxyClassInfo.EmittedClass = typeInfo.AsType();
      _typeBuilder = null; 
      return _proxyClassInfo.EmittedClass;
    }

    
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
      var meth = _typeBuilder.DefineMethod(_proxyClassInfo.FactoryMethodName, attrs, _typeBuilder, paramTypes);
      var ilGen = meth.GetILGenerator(); 
      // load args
      for(int i = 0; i < paramTypes.Length; i++) {
        ilGen.Emit(OpCodes.Ldarg, i);
      }
      // call
      ilGen.Emit(OpCodes.Newobj, constr);
      ilGen.Emit(OpCodes.Ret);
    }

    private PropertyBuilder BuildProperty(IProxyEmitController controller, PropertyInfo iprop) {
      var interfaceType = iprop.DeclaringType; 
      var pb = _typeBuilder.DefineProperty(iprop.Name, PropertyAttributes.None, iprop.PropertyType, Type.EmptyTypes);
      var intfPropInfo = new InterfaceMemberInfo() { Member = iprop };
      var emitInfo = controller.GetEmitInfo(intfPropInfo);

      if(iprop.GetMethod != null) {
        var getter = BuildMethod(controller, iprop.GetMethod, iprop);
        pb.SetGetMethod(getter);
        var igetter = interfaceType.GetMethod(iprop.GetMethod.Name);
        _typeBuilder.DefineMethodOverride(getter, igetter);
      }
      if(iprop.SetMethod != null) {
        var setter = BuildMethod(controller, iprop.SetMethod, iprop);
        pb.SetSetMethod(setter);
        var isetter = interfaceType.GetMethod(iprop.SetMethod.Name);
        _typeBuilder.DefineMethodOverride(setter, isetter);
      }
      //Attributes
      foreach(var attrExpr in emitInfo.CustomAttributes)
        pb.SetCustomAttribute(CreateAttributeBuilder(attrExpr));
      // ReviewCallback
      // Note: do not try to change to shortcut targetPropInfo?.ReviewCallback, for some reason it blows up
      if(emitInfo.ReviewCallback != null)
        emitInfo.ReviewCallback(pb); 
      return pb;
    } //method

    private MethodBuilder BuildMethod(IProxyEmitController controller, MethodInfo iMethod, PropertyInfo ownerProperty = null) {
      // Invoke controller to get emit info with target method
      var intfMethodInfo = new InterfaceMemberInfo() { Member = iMethod, OwnerProperty = ownerProperty };
      var emitInfo = controller.GetEmitInfo(intfMethodInfo);
      //validate some basic things
      ValidateEmitInfo(emitInfo, iMethod.Name); 

      // start building method
      var iParams = iMethod.GetParameters().ToList();
      var prmTypes = iParams.Select(p => p.ParameterType).ToArray();
      var attrs = MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Public;
      var mb = _typeBuilder.DefineMethod(iMethod.Name, attrs, iMethod.ReturnType, prmTypes);
      _builtMethods.Add(mb);
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
      foreach(var attrExpr in emitInfo.CustomAttributes)
        mb.SetCustomAttribute(CreateAttributeBuilder(attrExpr));
      //Invoke review callback if provided
      // Note: do not try to change to shortcut emitInfo?.ReviewCallback, for some reason it blows up
      if(emitInfo.ReviewCallback != null)
        emitInfo.ReviewCallback(mb); 
      return mb;
    }

    private void PushTargetRef(ILGenerator ilGen, EmitInfo emitInfo) {
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
          ConvertValueIfNeeded(ilGen, _proxyClassInfo.BaseType, expectedType, iMethod);
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

    private void ValidateEmitInfo(EmitInfo emitInfo, string iMethodName) {
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
