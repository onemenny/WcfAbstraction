using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace WcfAbstraction.TestTools.Unity
{
    /* Explenation:
     * 
     * the following classes represent interface-instance configuration mapping for use with unity.
     * These mapping state for each interface its type and optional instance: Concrete, Mock or Empty(default).
     * If empty (instance) - default instance will be created as implemented in proper UnityRegistry class.
     * 
     */

    /// <summary>
    /// Represents a contract-sevice mapping configuration section.
    /// </summary>
    public class UnityMappingSection : ConfigurationSection
    {
        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <value>The items.</value>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public UnityMappingElementCollection Items
        {
            get { return (UnityMappingElementCollection)this[string.Empty]; }
        }

        /// <summary>
        /// Gets the mappings as a dictionary.
        /// </summary>
        /// <value>The mappings.</value>
        public List<UnityMappingElement> Mappings
        {
            get
            {
                return Items.Cast<UnityMappingElement>().ToList();
            }
        }
    }

    /// <summary>
    /// Represents a interface-instance mapping collection configuration element.
    /// </summary>
    public class UnityMappingElementCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>
        /// A new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new UnityMappingElement();
        }

        /// <summary>
        /// Gets the element key for a specified configuration element.
        /// </summary>
        /// <param name="element">The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.</param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((UnityMappingElement)element).ContractType;
        }
    }

    /// <summary>
    /// Represents a interface-instance mapping configuration element.
    /// </summary>
    public class UnityMappingElement : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the type of the contract.
        /// </summary>
        /// <value>The type of the contract.</value>
        [ConfigurationProperty("contractType", IsKey = true, IsRequired = true)]
        public string ContractType
        {
            get { return (string)this["contractType"]; }
            set { this["contractType"] = value; }
        }

        /// <summary>
        /// Gets or sets the concrete type implementing the contract type
        /// If not set - trasparent proxy is created to the server (using the serverAddress in app.config)
        /// <remarks>
        /// Concrete type will be wrapped with policy injection if any exists
        /// </remarks>
        /// </summary>
        /// <value>The is transparent.</value>
        [ConfigurationProperty("concreteType", IsRequired = false)]
        public string ConcreteType
        {
            get { return (string)this["concreteType"]; }
            set { this["concreteType"] = value; }
        }

        /// <summary>
        /// Gets or sets the mock object type mocking the contract type (using Moq.dll)
        /// If not set - trasparent proxy is created to the server (using the serverAddress in app.config)
        /// </summary>
        /// <value>The is transparent.</value>
        [ConfigurationProperty("mockType", IsRequired = false)]
        public string MockType
        {
            get { return (string)this["mockType"]; }
            set { this["mockType"] = value; }
        }
    }
}
