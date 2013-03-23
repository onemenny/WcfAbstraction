using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WcfAbstraction.Server.Entities
{
    [DataContract]
    public class CircularRefEntity
    {
        [DataMember]
        public CircularRefEntity Parent { get; set; }

        [DataMember]
        public List<CircularRefEntity> Children { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Id { get; set; }
    }
}
