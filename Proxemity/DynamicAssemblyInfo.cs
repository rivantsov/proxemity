using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Proxemity {
  /// <summary>A container for dynamic assembly builder. Use <see cref="Create(string)"/>  method to create an instance.</summary>
  /// <remarks>You can create an assembly once and then use it to build multiple proxy classes.</remarks>
  public class DynamicAssemblyInfo {
    /// <summary>Assembly builder.</summary>
    public readonly AssemblyBuilder AssemblyBuilder;
    /// <summary>Module builder.</summary>
    public readonly ModuleBuilder ModuleBuilder;

    /// <summary>Creates a dynamic assembly, returns an object containing information about it. The returned 
    /// object can be used to emit multiple proxy classes using ProxyEmitter.</summary>
    /// <param name="assemblyName">Full assembly name.</param>
    /// <returns>Assembly info object.</returns>
    public static DynamicAssemblyInfo Create(string assemblyName) {
      var asmName = new AssemblyName(assemblyName);
      asmName.Version = new Version(1, 0, 0, 0);
      var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
      var moduleBuilder = asmBuilder.DefineDynamicModule("Main");
      return new DynamicAssemblyInfo(asmBuilder, moduleBuilder);
    }

    private DynamicAssemblyInfo(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder) {
      AssemblyBuilder = assemblyBuilder;
      ModuleBuilder = moduleBuilder;
    }
  }


}
