using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using WcfAbstraction.Server.Entities;

namespace WcfAbstraction.Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class TestService : ITestService
    {
        #region Ctor

        /// <summary>
        /// Prevents a default instance of the <see cref="TestService"/> class from being created.
        /// Private Constructor ensure no initialization can be made for this type. Use Unity instead
        /// </summary>
        private TestService()
        {
        }

        #endregion

        public bool IsAlive()
        {

            return true;

            #region Security Context
            //ServiceSecurityContext.Current.WindowsIdentity.Name;
            #endregion
        }

        public void AssertArgumentNotNull_GenericFault(string testArg)
        {
            if (String.IsNullOrEmpty(testArg))
            {
                var ex = new ArgumentNullException("AssertArgumentNotNull_GenericFault - the argument value is null or empty");
                throw new FaultException<ArgumentNullException>(ex);

            }
        }

        public void AssertArgumentNotNull_DefaultFault(string testArg)
        {
            if (String.IsNullOrEmpty(testArg))
            {
                throw new ArgumentNullException("AssertArgumentNotNull_DefaultFault - the argument value is null or empty");
            }
        }

        public TestEntity GetNewTestEntity()
        {
            return new TestEntity()
                {
                    Id = "1",
                    Value = "hello",
                };
        }

        public CircularRefEntity GetCircularRefEntities()
        {
            var root = new CircularRefEntity()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Root",
                    Children = new List<CircularRefEntity>(),
                };

            root.Children.Add(new CircularRefEntity()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = "Child 1",
                                Parent = root,
                                Children = new List<CircularRefEntity>(),
                            });

            root.Children.Add(new CircularRefEntity()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Child 2",
                Parent = root,
                Children = new List<CircularRefEntity>(),
            });

            return root;
        }
    }
}
