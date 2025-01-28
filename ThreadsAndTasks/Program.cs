using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace ThreadsAndTasks
{
    public class Thread_A
    {
        private readonly Thread_B thread_B;

        public Thread_A(Thread_B thread_B)
        {
            this.thread_B = thread_B;
        }

        public void Run()
        {
            //...
            lock (this) //thread_A 1.
            {
                //.... 2.

                lock (thread_B) // 6. wait
                {

                }

                //...
            }
            //....
        }
    }

    public class Thread_B
    {
        private readonly Thread_A thread_A;

        public Thread_B(Thread_A thread_A)
        {
            this.thread_A = thread_A;
        }

        public void Run()
        {
            //...
            lock (this) // thread_B 3.
            {
                //.... 4.

                lock (thread_A) // 5. wait
                {

                }

                //...
            }
            //....
        }
    }


    class Lamp
    {
        private readonly ConsoleColor color;
        private readonly int left;
        private readonly int top;

        private static readonly object _lock = new object();

        public Lamp(int timeOn, int timeOff, ConsoleColor color, int left, int top)
        {
            this.TimeOn = timeOn;
            this.TimeOff = timeOff;
            this.color = color;
            this.left = left;
            this.top = top;
        }

        public bool IsOn { get; private set; } = false;

        public int TimeOn { get; }

        public int TimeOff { get; }

        public Thread? Thread { get; private set; } = null;

        public void Run()
        {
            var thread = new Thread(Toggle)
            {
                IsBackground = true,
            };

            thread.Start();
            Thread = thread;
        }

        private void Toggle()
        {
            while (true)
            {
                IsOn = !IsOn;

                lock (_lock)
                {
                    Console.ResetColor();
                    Console.SetCursorPosition(left, top);
                    if (IsOn) Console.BackgroundColor = color;
                    Console.Write(" ");
                    Console.ResetColor();
                }

                Thread.Sleep(IsOn ? TimeOn : TimeOff);
            }
        }
    }

    class List
    {
        object _lock = new object();

        public void Add()
        {
            lock (_lock)
            {
                ///
            }
        }

        public object this[int i]
        {
            get
            {
                lock (_lock)
                {
                    return 1;
                }
            }
        }
    }

    internal class Program
    {
        class RandomArrayThreadProcessor
        {
            private readonly int threadCount;
            private readonly int[] arr;
            private Thread[] _threads = Array.Empty<Thread>();

            public RandomArrayThreadProcessor(int threadCount, int[] arr)
            {
                this.threadCount = threadCount;
                this.arr = arr;
            }

            public void Run()
            {
                this._threads = new Thread[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    var num = i;
                    _threads[i] = new Thread(() => Process(num))
                    {
                        IsBackground = true
                    };
                }

                foreach (var thread in _threads)
                {
                    thread.Start();
                }

                foreach (var thread in _threads) { thread.Join(); }
            }

            protected void Process(int threadIndex)
            {
                var rand = new Random();
                //todo: should add normal algorithm for getting array span.
                var span = arr[0..(arr.Length / threadCount)];

                for (int i = 1; i < span.Length; i++)
                {
                    span[i] = rand.Next();
                }
            }
        }

        static void Main(string[] args)
        {
            var arr = new int[10_000_000_000];
            var p = new RandomArrayThreadProcessor(4, arr);
            var sw = Stopwatch.StartNew();
            p.Run();
            sw.Stop();

            Console.CursorVisible = false;

            var lamps = new Lamp[] {
                new Lamp(1000, 200, ConsoleColor.Red, 1, 1),
                new Lamp(300, 500, ConsoleColor.Green, 2, 2),
                new Lamp(1300, 1500, ConsoleColor.Cyan, 3, 3),
                new Lamp(150, 200, ConsoleColor.Magenta, 4, 4),
                new Lamp(700, 400, ConsoleColor.Yellow, 5, 5),
                new Lamp(200, 100, ConsoleColor.White, 6, 6),
            };

            foreach (var lamp in lamps)
            {
                lamp.Run();
            }

            foreach (var lamp in lamps)
            {
                lamp.Thread?.Join();
            }
        }
    }
}
