using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadingTest
{
    class Test1
    {
        static void Main(string[] args)
        {
            //Создадим пул потоков из 3-х потоков.
            FixedThreadPool testPool = new FixedThreadPool(3);
            Task tranquilityHigh = new Task(TranquilityHigh);
            Task tranquilityNormal = new Task(TranquilityNormal);
            Task tranquilityLow = new Task(TranquilityLow);
            Console.WriteLine("Before signal Stop");
            Console.WriteLine("First tasks");
            //Заполним очередь задачами с разными приоритетами.
            for (int i = 0; i < 4 ; i++)
            {
                testPool.Execute(tranquilityHigh, Priority.HIGH);
                testPool.Execute(tranquilityNormal, Priority.NORMAL);
                testPool.Execute(tranquilityLow, Priority.LOW);
            }
            //Дождемся пока очередь опустеет.
            Thread.Sleep(20000);
            //Добавим еще задач.
            Console.WriteLine("Second tasks");
            for (int i = 0; i < 2; i++)
            {
                testPool.Execute(tranquilityHigh, Priority.HIGH);
                testPool.Execute(tranquilityNormal, Priority.NORMAL);
                testPool.Execute(tranquilityLow, Priority.LOW);
            }
            Console.WriteLine("Signal Stop");
            //Подаем сигнал Stop.
            testPool.Stop();
            Console.WriteLine("After signal Stop");
            bool isStopped;
            //Пытаемся добавить задачи в очередь.
            for (int i = 0; i < 4; i++)
            {
                isStopped = testPool.Execute(tranquilityHigh, Priority.HIGH);
                Console.WriteLine("Execute() returns " + isStopped.ToString());
            }
            //Даем время потокам завершить все задачи.
            Thread.Sleep(10000);
            //Снова запускаем работу очереди.
            testPool.Go();
            Console.WriteLine("After signal Go");
            //Пытаемся добавить задачи в очередь.
            for (int i = 0; i < 4; i++)
            {
                isStopped = testPool.Execute(tranquilityLow, Priority.LOW);
                Console.WriteLine("Execute() returns " + isStopped.ToString());
            }
            //Даем время потокам завершить все задачи.
            Thread.Sleep(5000);
            Console.WriteLine("............ END ..............");
        }

        /// <summary>
        /// Имитирует задачу с приоритетом <see cref="Priority.HIGH"/>.
        /// </summary>
        public static void TranquilityHigh()
        {
            Console.WriteLine(Thread.CurrentThread.Name + " is now working on high priority task");
            Thread.Sleep(5000);
            Console.WriteLine(Thread.CurrentThread.Name + " High priority task is done");
        }

        /// <summary>
        /// Имитирует задачу с приоритетом <see cref="Priority.NORMAL"/>.
        /// </summary>
        public static void TranquilityNormal()
        {
            Console.WriteLine(Thread.CurrentThread.Name + " is now working on normal priority task");
            Thread.Sleep(3000);
            Console.WriteLine(Thread.CurrentThread.Name + " Normal priority task is done");
        }

        /// <summary>
        /// Имитирует задачу с приоритетом <see cref="Priority.LOW"/>.
        /// </summary>
        public static void TranquilityLow()
        {
            Console.WriteLine(Thread.CurrentThread.Name + " is now working on low priority task");
            Thread.Sleep(1000);
            Console.WriteLine(Thread.CurrentThread.Name + " Low priority task is done");
        }
    }
}
