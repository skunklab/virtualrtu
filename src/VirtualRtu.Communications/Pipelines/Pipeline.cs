using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualRtu.Communications.Pipelines
{
    public abstract class Pipeline : IDisposable
    {
        public virtual event System.EventHandler<PipelineErrorEventArgs> OnPipelineError;
        public virtual string Id { get; set; }
        public virtual IChannel InputChannel { get; set; }
        public virtual IChannel OutputChannel { get; set; }
        public virtual List<IFilter> InputFilters { get; set; }
        public virtual List<IFilter> OutputFilters { get; set; }
        public abstract void Execute();
        public abstract void Dispose();

    }
}
