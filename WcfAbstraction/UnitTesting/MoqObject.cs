using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;

namespace WcfAbstraction.TestTools
{
    /// <summary>
    /// Represents abstract class when constructing Moq object (Moq.dll)
    /// 
    /// The instance provide an abstract setup method to setup the mock object
    /// The instance will return the mock.MockObject as property
    /// </summary>
    /// <typeparam name="T">Moq object type (usualy interface)</typeparam>
    public class MoqObject<T> where T : class
    {
        #region Proerpties

        /// <summary>
        /// Gets the mock object which is of type T.
        /// </summary>
        /// <value>The mock object.</value>
        public T MockObject
        {
            get { return (T)Mock.Object; }
        }

        /// <summary>
        /// Gets or sets the mock object of T.
        /// </summary>
        /// <value>The mock.</value>
        protected Mock<T> Mock { get; set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="MoqObject&lt;T&gt;"/> class.
        /// </summary>
        public MoqObject()
        {
            Mock = new Mock<T>();
            Setup();
        }

        #endregion

        #region Overridable Methods

        /// <summary>
        /// Setups the mock methods, properties and other class members belonging to T
        /// </summary>
        protected virtual void Setup() 
        {
        }

        #endregion
    }
}
