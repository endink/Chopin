using System;

namespace Chopin.Pooling
{
    public interface IEvictionTimer : IDisposable
    {
        void Schedule(Evictor task, TimeSpan delay, TimeSpan period);

        void Cancel(Evictor task);
    }
}
