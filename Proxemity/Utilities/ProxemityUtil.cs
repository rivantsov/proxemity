using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Proxemity {

  internal static class ProxemityUtil {
    public static void Check(bool condition, string message, params object[] args) {
      if(!condition)
        Throw(message, args);
    }

    // note: not using other utility methods here (Check, Throw) to make reported call stack shorter (stack in exception)
    public static void CheckParam(object parameter, string parameterName) {
      if(parameter != null)
        return;
      var msg = SafeFormat("Parameter {0} must be provided.", parameterName);
      throw new Exception(msg);
    }

    public static void CheckNotEmpty(string value, string message, params object[] args) {
      if(string.IsNullOrWhiteSpace(value))
        Throw(message, args);
    }

    public static Exception Throw(string message, params object[] args) {
      var msg = message.SafeFormat(args);
      throw new Exception(msg);
    }

    public static string SafeFormat(this string message, params object[] args) {
      if(args == null || args.Length == 0)
        return message;
      try {
        return string.Format(CultureInfo.InvariantCulture, message, args);
      } catch(Exception ex) {
        return message + " (System error: failed to format message. " + ex.Message + ")";
      }
    }

    public static IList<MethodInfo> GetAllMethods(this Type interfaceType) {
      var list = new List<MethodInfo>();
      list.AddRange(interfaceType.GetMethods());
      var bases = interfaceType.GetInterfaces();
      foreach(var bi in bases)
        list.AddRange(bi.GetMethods());
      return list; 
    }

    public static IList<PropertyInfo> GetAllProperties(this Type interfaceType) {
      var list = new List<PropertyInfo>();
      list.AddRange(interfaceType.GetProperties());
      var bases = interfaceType.GetInterfaces();
      foreach(var bi in bases)
        list.AddRange(bi.GetProperties());
      return list;
    }

    internal static bool IsStatic(this MemberInfo member) {
      switch(member) {
        case PropertyInfo prop:
          return prop.GetMethod.IsStatic;
        case FieldInfo field:
          return field.IsStatic;
        case MethodInfo method:
          return method.IsStatic;
        default:
          return false;
      }
    }

    internal static object GetFactoryMethod(Type proxyType, string methodName, Type funcType) {
      var genParams = funcType.GetGenericArguments();
      var returnType = genParams[genParams.Length - 1];
      Check(returnType.IsAssignableFrom(proxyType), "Invalid Func return type {0}; must be compatible with proxy base type {1}).", returnType, proxyType.BaseType);
      var paramTypes = genParams.Take(genParams.Length - 1).ToArray();
      // var flags = BindingFlags.Static | BindingFlags.Public;
      var method = proxyType.GetMethod(methodName, paramTypes);
      Check(method != null, "Factory method with provided parameter types not found.");
      var func = method.CreateDelegate(funcType);
      return func;

    }

  } //class

}//ns
