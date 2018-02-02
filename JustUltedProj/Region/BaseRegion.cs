﻿using System;
using System.Net;

namespace JustUlted.Region
{
    public abstract class BaseRegion
    {
        public abstract string RegionName { get; }

        public abstract bool Garena { get; }

        public abstract string InternalName { get; }

        public abstract string ChatName { get; }

        public abstract Uri NewsAddress { get; }

        public abstract string Locale { get; }

        public abstract PVPNetConnect.Region PVPRegion { get; }

        public abstract IPAddress[] PingAddresses { get; }

        public abstract Uri SpectatorLink { get; }

        public abstract string SpectatorIpAddress { get; set; }

        public abstract string Location { get; }

        public static BaseRegion GetRegion(String RequestedRegion)
        {
            RequestedRegion = RequestedRegion.ToUpper();
            Type t = Type.GetType("JustUltedProj.Region." + RequestedRegion);

            if (t != null)
                return (BaseRegion)Activator.CreateInstance(t);

            t = Type.GetType("JustUltedProj.Region.Garena." + RequestedRegion);
            if (t != null)
                return (BaseRegion)Activator.CreateInstance(t);

            return null;
        }
    }
}