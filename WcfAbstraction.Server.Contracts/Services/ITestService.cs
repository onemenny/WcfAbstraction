using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using WcfAbstraction.Server.Entities;
using WcfAbstraction.ServiceModel;

namespace WcfAbstraction.Server.Services
{
    [ServiceContract]
    public interface ITestService
    {
        [OperationContract]
        bool IsAlive();

        [OperationContract]
        [FaultContract(typeof(ArgumentNullException))]
        void AssertArgumentNotNull_GenericFault(string testArg);

        [OperationContract]
        void AssertArgumentNotNull_DefaultFault(string testArg); //notice no exception decleration

        [OperationContract]
        TestEntity GetNewTestEntity();

        [OperationContract]
        //[PreserveReferences]
        [NetSerializer]
        CircularRefEntity GetCircularRefEntities();
    }
}
