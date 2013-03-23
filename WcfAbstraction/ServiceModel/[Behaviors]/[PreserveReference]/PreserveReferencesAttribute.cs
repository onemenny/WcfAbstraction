using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace WcfAbstraction.ServiceModel
{
    /// <summary>
    /// Provides an attribute for a WCF operation contract that enables circular reference for methods returned object.
    /// </summary>
    public class PreserveReferencesAttribute : Attribute, IOperationBehavior
    {
        /// <summary>
        /// Adds the binding parameters.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="parameters">The parameters.</param>
        public void AddBindingParameters(
            OperationDescription description,
            BindingParameterCollection parameters)
        {
        }

        /// <summary>
        /// Applies the client behavior.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="proxy">The proxy.</param>
        public void ApplyClientBehavior(
            OperationDescription description,
            ClientOperation proxy)
        {
            IOperationBehavior innerBehavior = new PreserveReferencesOperationBehavior(description);
            innerBehavior.ApplyClientBehavior(description, proxy);
        }

        /// <summary>
        /// Applies the dispatch behavior.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="dispatch">The dispatch.</param>
        public void ApplyDispatchBehavior(
            OperationDescription description,
            DispatchOperation dispatch)
        {
            IOperationBehavior innerBehavior = new PreserveReferencesOperationBehavior(description);
            innerBehavior.ApplyDispatchBehavior(description, dispatch);
        }

        /// <summary>
        /// Validates the specified description.
        /// </summary>
        /// <param name="description">The description.</param>
        public void Validate(OperationDescription description)
        {
        }
    }
}
