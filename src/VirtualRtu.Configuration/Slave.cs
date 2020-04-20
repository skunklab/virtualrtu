﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtualRtu.Configuration.Vrtu;

namespace VirtualRtu.Configuration
{
    [Serializable]
    [JsonObject]
    public class Slave
    {
        public Slave()
        {
        }

        public Slave(byte unitId, string ipAddress, int port, byte? alias)
        {
            UnitId = unitId;
            IPAddress = ipAddress;
            Port = port;
            Alias = alias;
        }

        [JsonProperty("unitId")] public byte UnitId { get; set; }

        [JsonProperty("ipAddress")] public string IPAddress { get; set; }

        [JsonProperty("port")] public int Port { get; set; }

        [JsonProperty("alias")] public byte? Alias { get; set; }

        [JsonProperty("constraints")] public List<Constraint> Constraints { get; set; }

        public void RemoveConstraints()
        {
            Constraints = null;
        }
    }
}