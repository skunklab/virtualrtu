using System;

namespace VirtualRtu.Configuration
{
    public class ConfigUpdateEventArgs : EventArgs
    {
        public ConfigUpdateEventArgs(bool updated)
        {
            Updated = updated;
        }

        public bool Updated { get; set; }
    }
}