using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Xml;

namespace WcfAbstraction.ServiceModel
{
    /// <summary>
    /// Provides a serializer for WCF proxies that 
    /// allows passing objects of types, which are not known at compile time.
    /// Cross-platform interoperability is not supported.
    /// </summary>
    public class NetSerializerOperationBehavior : DataContractSerializerOperationBehavior
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="NetSerializerOperationBehavior"/> class.
        /// </summary>
        /// <param name="operationDescription">The operation description.</param>
        public NetSerializerOperationBehavior(OperationDescription operationDescription)
            : base(operationDescription)
        {
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Initializes a new instance of the <see cref="PreserveReferencesOperationBehavior"/> class
        /// that inherits from <see cref="T:System.Runtime.Serialization.XmlObjectSerializer"></see> 
        /// for serialization and deserialization operations.
        /// </summary>
        /// <param name="type">The <see cref="T:System.Type"></see> to create the serializer for.</param>
        /// <param name="name">The name of the generated type.</param>
        /// <param name="ns">The namespace of the generated type.</param>
        /// <param name="knownTypes">An <see cref="T:System.Collections.Generic.IList`1"></see> of <see cref="T:System.Type"></see> that contains known types.</param>
        /// <returns>
        /// An instance of a class that inherits from the <see cref="T:System.Runtime.Serialization.XmlObjectSerializer"></see> class.
        /// </returns>
        public override XmlObjectSerializer CreateSerializer(Type type, string name, string ns, IList<Type> knownTypes)
        {
            NetDataContractSerializer ser = new NetDataContractSerializer();
            ser.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;
            
            return ser;
        }

        /// <summary>
        /// Creates an instance of a class that inherits from <see cref="T:System.Runtime.Serialization.XmlObjectSerializer"></see> for serialization and deserialization operations with an <see cref="T:System.Xml.XmlDictionaryString"></see> that contains the namespace with the <c>preserveObjectReferences</c> attribute set to true.
        /// </summary>
        /// <param name="type">The type to serialize or deserialize.</param>
        /// <param name="name">The name of the serialized type.</param>
        /// <param name="ns">An <see cref="T:System.Xml.XmlDictionaryString"></see> that contains the namespace of the serialized type.</param>
        /// <param name="knownTypes">An <see cref="T:System.Collections.Generic.IList`1"></see> of <see cref="T:System.Type"></see> that contains known types.</param>
        /// <returns>
        /// An instance of a class that inherits from the <see cref="T:System.Runtime.Serialization.XmlObjectSerializer"></see> class.
        /// </returns>
        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
        {
            NetDataContractSerializer ser = new NetDataContractSerializer();
            ser.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

            return ser;
        }

        #endregion
    }
}
