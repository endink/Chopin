using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chopin.Pooling.Impl;
using Xunit;

namespace Chopin.Pooling.UnitTest
{
    public class GenericKeyedObjectPoolTest
    {
        public GenericKeyedObjectPoolTest()
        {

        }

        [Fact]
        public void PoolTest()
        {
            var pool = new GenericKeyedObjectPool<string, string>(new StringStringFactory(), new GenericKeyedObjectPoolConfig());

            //pool.AddObject("test");
            for (int i = 0; i < 1024; i++)
            {

                string result = pool.BorrowObject("test");
                pool.ReturnObject("test", result);

                Assert.NotEmpty(result);
                Debug.WriteLine(result);
            }
        }

        [Fact]
        public void MulitityThreadGetTet()
        {
            var pool = new GenericKeyedObjectPool<string, string>(new StringStringFactory(), new GenericKeyedObjectPoolConfig() { BlockWhenExhausted = false, MaxTotalPerKey = int.MaxValue });

            for (int i = 0; i < 1024; i++)
            {
                ConcurrentQueue<string> set = new ConcurrentQueue<string>();
                Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 4 },
                    () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }, () =>
                    {
                        string result = pool.BorrowObject("test");
                        set.Enqueue(result);
                    }
                );

                Assert.Equal(set.Distinct().Count(), 11);
            }

        }

        [Fact]
        public void ParallelTest()
        {
            var pool = new GenericKeyedObjectPool<string, string>(new StringStringFactory(), new GenericKeyedObjectPoolConfig());
            var runResult = Parallel.For(
                0,
                1024,
                (i) =>
                {
                    string result = pool.BorrowObject("test");
                    pool.ReturnObject("test", result);

                    Assert.NotEmpty(result);
                });
            if (!runResult.IsCompleted)
            {
                Thread.Sleep(100);
            }
        }

        public class StringStringFactory : IKeyedPooledObjectFactory<string, string>
        {
            internal static int i;
            public IPooledObject<string> MakeObject(string key)
            {
                return new DefaultPooledObject<string>(Interlocked.Increment(ref i).ToString());
            }

            public void DestroyObject(string key, IPooledObject<string> p)
            {

            }

            public bool ValidateObject(string key, IPooledObject<string> p)
            {
                return !string.IsNullOrWhiteSpace(p.Object);
            }

            public void ActivateObject(string key, IPooledObject<string> p)
            {

            }

            public void PassivateObject(string key, IPooledObject<string> p)
            {

            }
        }
    }
}