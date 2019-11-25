namespace VirtualRtu.Communications.IoTHub
{
    public class ModuleTwinEventArgs
    {
        public ModuleTwinEventArgs(string configString, string luss)
        {
            if(!string.IsNullOrEmpty(configString))
            {
                JsonConfigString = configString;
            }
        }

        public string JsonConfigString { get; internal set; }
        public string Luss { get; internal set; }
        
    }
}
