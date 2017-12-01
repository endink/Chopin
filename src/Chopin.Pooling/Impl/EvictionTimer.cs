using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chopin.Pooling.Impl
{

    /// <summary>
    /// * Provides a shared idle object eviction timer for all pools. This class wraps
    /// the standard {@link Timer}
    /// and keeps track of how many pools are using it.
    /// If no pools are using the timer, it is canceled.This prevents a thread
    /// being left running which, in application server environments, can lead to
    /// memory leads and/or prevent applications from shutting down or reloading
    /// cleanly.
    /// 
    /// This class has package scope to prevent its inclusion in the pool public API.
    /// The class declaration below should /// not/// be changed to public.
    /// 
    /// This class is intended to be thread-safe.
    /// </summary>
    internal class EvictionTimer : IEvictionTimer
    {
        private Dictionary<Evictor, Timer> _taskMap = null;
        private volatile bool _disposed;

        public EvictionTimer()
        {
            _taskMap = new Dictionary<Evictor, Timer>();
        }
        
        public void Schedule(Evictor task, TimeSpan delay, TimeSpan period)
        {
            this.ThrowIfDisposed();
            if (task == null)
            {
                return;
            }
            lock (typeof(EvictionTimer))
            {
                if (_taskMap.TryGetValue(task, out Timer timer))
                {
                    timer.Change(delay, period);
                }
                else
                {
                    var t = new Timer(state => task.Run(), null, delay, period);
                    _taskMap[task] = t;
                }
            }
        }

        public void Cancel(Evictor task)
        {
            this.ThrowIfDisposed();
            lock (typeof(EvictionTimer))
            {
                if (_taskMap.TryGetValue(task, out Timer timer))
                {
                    _taskMap.Remove(task);
                    timer.Dispose();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        ~EvictionTimer()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    var timers = _taskMap?.Values.ToArray() ?? new Timer[0];
                    _taskMap.Clear();
                    _taskMap = null;
                    foreach (var t in timers)
                    {
                        t.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
