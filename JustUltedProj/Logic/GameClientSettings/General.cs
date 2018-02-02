﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustUltedProj.Logic.GameClientSettings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class General : Attribute
    {
        public string name { get; set; }
        public bool isGeneral { get; set; }
        public General(string name)
        {
            this.name = name;
        }
    }
}
