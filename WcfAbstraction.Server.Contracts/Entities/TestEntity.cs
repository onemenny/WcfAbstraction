﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WcfAbstraction.Server.Entities
{
    [DataContract]
    public class TestEntity
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Value { get; set; }
    }
}
