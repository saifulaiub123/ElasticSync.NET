using System;

namespace ElasticSync.NET.Models
{
    public class ElasticSyncServiceProviders
    {
        public Type? ChangeLogServiceType { get; set; }
        public Type? ChangeLogInstallerServiceType { get; set; }
    }
}
