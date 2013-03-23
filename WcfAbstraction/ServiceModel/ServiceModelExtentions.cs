using System.Diagnostics;
using System.ServiceModel;

namespace WcfAbstraction.ServiceModel
{
    /// <summary>
    /// Service model extention methods
    /// </summary>
    public static class ServiceModelExtentions
    {
        /// <summary>
        /// Gets the current windows identity sid.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static string GetSid(this ServiceSecurityContext context)
        {
            return context.WindowsIdentity.User.Value;
        }
    }
}
