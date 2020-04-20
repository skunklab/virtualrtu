using System;
using System.Collections.Generic;
using SkunkLab.Channels;

namespace VirtualRtu.Communications.Pipelines
{
    public abstract class Pipeline : IDisposable
    {
        public virtual string Id { get; set; }
        public virtual IChannel InputChannel { get; set; }
        public virtual IChannel OutputChannel { get; set; }
        public virtual List<IFilter> InputFilters { get; set; }
        public virtual List<IFilter> OutputFilters { get; set; }
        public abstract void Dispose();
        public virtual event EventHandler<PipelineErrorEventArgs> OnPipelineError;
        public abstract void Execute();
    }
}