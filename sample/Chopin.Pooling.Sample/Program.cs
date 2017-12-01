using System;
using System.Threading;
using System.Threading.Tasks;
using Chopin.Pooling.Impl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Chopin.Pooling.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            //var factory = new LoggerFactory();
            //factory.AddConsole(LogLevel.Debug);
            //var ss = new StringStringFactory(factory);

            //int j = 1;
            //while (true)
            //{
            //    Console.WriteLine($"第{j}次运行");
            //    var pool = new GenericKeyedObjectPool<string, string>(new StringStringFactory(factory), new GenericKeyedObjectPoolConfig(){TestOnBorrow = true,TestWhileIdle = true,TimeBetweenEvictionRunsMillis = 1000});
            //    ParallelLoopResult runResult = Parallel.For(
            //        0,
            //        10, new ParallelOptions(){MaxDegreeOfParallelism = 1},
            //        (i) =>
            //        {
            //            string result = pool.BorrowObject("test");
            //            pool.ReturnObject("test", result);
            //        });
            //    if (!runResult.IsCompleted)
            //    {
            //        Thread.Sleep(100);
            //    }

            //    Thread.Sleep(1000);
            //    j++;
            //}
            var s = new GenericPoolSample();
            s.Test();
            Console.WriteLine("test is finish");
            Console.ReadKey();
            s.TestParalle();
            Console.WriteLine("Finish");
            Console.ReadLine();
        }
    }

    public class StringStringFactory : IKeyedPooledObjectFactory<string, string>
    {
        private ILogger logger;
        private static int i;

        /// <summary>初始化 <see cref="T:System.Object" /> 类的新实例。</summary>
        public StringStringFactory(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger("StringStringFactory");
        }

        public IPooledObject<string> MakeObject(string key)
        {
            this.logger.LogDebug("begin MakeObject");
            return new DefaultPooledObject<string>(Interlocked.Increment(ref i).ToString());
        }

        public void DestroyObject(string key, IPooledObject<string> p)
        {
            this.logger.LogDebug($"DestroyObject:[{key}][{p}]");
        }

        public bool ValidateObject(string key, IPooledObject<string> p)
        {
            bool result = (DateTime.Now - p.CreateTime) < TimeSpan.FromSeconds(10);
            this.logger.LogDebug($"ValidateObject:[{key}][{p}],ValidateObject result:{result}");
            return result;
        }

        public void ActivateObject(string key, IPooledObject<string> p)
        {
            this.logger.LogDebug($"ActivateObject:[{key}][{p}]");
        }

        public void PassivateObject(string key, IPooledObject<string> p)
        {
            this.logger.LogDebug($"PassivateObject:[{key}][{p}]");
        }
    }
}