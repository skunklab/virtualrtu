using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRtu.Communications.Pipelines
{
    public class PipelineErrorEventArgs : EventArgs
    {
        public PipelineErrorEventArgs(string id, Exception error = null)
        {
            Id = id;
            Error = error;
        }

        public string Id { get; internal set; }

        public Exception Error { get; internal set; }
    }
}
