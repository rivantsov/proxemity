using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace Proxemity {
  /// <summary>A class containing logic for handling custom attributes on interface (type and members). 
  /// The attributes can be copied to the corresponding type and members of the generated proxy class. </summary>
  public class AttributeHandler {
    /// <summary>List of functors describing methods for cloning attributes.</summary>
    internal IList<LambdaExpression> Descriptors = new List<LambdaExpression>();

    /// <summary>Constructs an instance. </summary>
    /// <param name="addStandardAttributes">A flag specifying whether to add handling of standard attributes from 
    /// the ComponentModel namespace: Description, Category, etc.</param>
    public AttributeHandler(bool addStandardAttributes = true) {
      if(addStandardAttributes)
        AddStandardAttributeDescriptors(); 
    }

    /// <summary>Adds a descriptor that encodes handling (copying) of an attribute of a certain type.</summary>
    /// <typeparam name="TAttribute">Attribute type.</typeparam>
    /// <param name="descriptor">A Func expression that clones the attribute, for ex:
    ///   a => new CategoryAttribute(a.Category) .</param>
    public void AddDescriptor<TAttribute>(Expression<Func<TAttribute, TAttribute>> descriptor)
                                              where TAttribute : Attribute {
      this.Descriptors.Add(descriptor);
    }

    internal void AddStandardAttributeDescriptors() {
      AddDescriptor<DescriptionAttribute>((a) => new DescriptionAttribute(a.Description));
      AddDescriptor<BrowsableAttribute>((a) => new BrowsableAttribute(a.Browsable));
      AddDescriptor<DisplayNameAttribute>((a) => new DisplayNameAttribute(a.DisplayName));
      AddDescriptor<CategoryAttribute>((a) => new CategoryAttribute(a.Category));
      AddDescriptor<DebuggerDisplayAttribute>((a) => new DebuggerDisplayAttribute(a.Value));
    }//method



  }
}
