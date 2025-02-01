using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadsAndTasks
{
    class SingleTaskScheduler : TaskScheduler
    {
        private readonly object sync = new object();

        private readonly Queue<Task> tasks = new Queue<Task>();

        private Thread thread;

        public SingleTaskScheduler()
        {
            thread = new Thread(ExcecuteTask) { Name = "OneForAll", IsBackground = true };
            thread.Start();
        }

        protected override IEnumerable<Task>? GetScheduledTasks()
        {
            return tasks;
        }

        protected override void QueueTask(Task task)
        {
            lock (sync)
            {
                tasks.Enqueue(task);
            }
        }

        private void ExcecuteTask(object? obj)
        {
            while (true)
            {
                lock (sync)
                {
                    if (tasks.Count > 0)
                    {
                        var task = tasks.Peek();
                        if (TryDequeue(task))
                            TryExecuteTask(task);
                    }
                }
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override bool TryDequeue(Task task)
        {
            Task currentTask;
            lock (sync)
            {
                currentTask = tasks.Dequeue();
            }

            return currentTask == task;
        }
    }

    //class MultiTaskRandomProcessor<T>
    //{
    //    private readonly Task[] _tasks;
    //    private readonly T[] _array;
    //    private readonly Func<Random, T> _randomize;
    //    protected readonly Random[] _randoms;

    //    public MultiTaskRandomProcessor(int threadCount, T[] array, Func<Random, T> randomize)
    //    {
    //        _tasks = new Task[threadCount];
    //        _array = array;
    //        _randomize = randomize;
    //        _randoms = new Random[threadCount];
    //    }

    //    public virtual Task Process(CancellationToken cancellationToken = default)
    //    {
    //        for (var i = 0; i < _randoms.Length; i++)
    //        {
    //            _randoms[i] = new Random();
    //        }
    //        for (int i = 0; i < _tasks.Length; i++)
    //        {
    //            var index = i;
    //            _tasks[i] = Task.Run(() => ThreadProc(index, cancellationToken), cancellationToken);
    //        }

    //        return Task.WhenAll(_tasks);
    //    }

    //    private void ThreadProc(int threadIndex, CancellationToken cancellationToken = default)
    //    {
    //        var length = _tasks.Length;
    //        var index = threadIndex;
    //        var count = _array.Length / length;

    //        var span = index == length - 1
    //            ? _array.AsSpan((index * count)..)
    //            : _array.AsSpan((index * count)..((index * count) + count));

    //        for (var i = 0; !cancellationToken.IsCancellationRequested && i < span.Length; i++)
    //        {
    //            ProcessValue(index, i, span);
    //        }
    //    }

    //    protected void ProcessValue(int threadIndex, int itemIndex, Span<T> span)
    //    {
    //        span[itemIndex] = _randomize(_randoms[threadIndex]);
    //    }
    //}

    abstract class MultiThreadingProcessor<T>
    {
        private readonly Thread[] _threads;
        private readonly T[] _array;

        public MultiThreadingProcessor(int threadCount, T[] array)
        {
            _threads = new Thread[threadCount];
            _array = array;
        }

        public virtual void Process()
        {
            for (int i = 0; i < _threads.Length; i++)
            {
                _threads[i] = new Thread(ThreadProc) { IsBackground = true };
                _threads[i].Start(i);
            }

            foreach (var thread in _threads)
                thread.Join();
        }

        private void ThreadProc(object? state)
        {
            var length = _threads.Length;
            var threadNum = (int)state!;
            var count = _array.Length / length; // 250_000_000 - 500_000_000

            var span = threadNum == length - 1
                ? _array.AsSpan((threadNum * count)..)
                : _array.AsSpan((threadNum * count)..((threadNum * count) + count));

            for (var i = 0; i < span.Length; i++)
            {
                ProcessValue(threadNum, i, span);
            }
        }

        protected abstract void ProcessValue(int threadIndex, int itemIndex, Span<T> span);
    }

    class GenRandomArray<T> : MultiThreadingProcessor<T>
    {
        private readonly Func<Random, T> _randomize;
        protected readonly Random[] _randoms;

        public GenRandomArray(int threadCount, T[] array, Func<Random, T> randomize)
            : base(threadCount, array)
        {
            this._randomize = randomize;
            _randoms = new Random[threadCount];
        }

        public override void Process()
        {
            for (var i = 0; i < _randoms.Length; i++)
            {
                _randoms[i] = new Random();
            }

            base.Process();
        }

        protected override void ProcessValue(int threadIndex, int itemIndex, Span<T> span)
        {
            span[itemIndex] = _randomize(_randoms[threadIndex]);
        }
    }

    class GenRandomArray : GenRandomArray<int>
    {
        public GenRandomArray(int threadCount, int[] resultArray)
            : base(threadCount, resultArray, r => r.Next())
        {
        }
    }

    class SumSearch : MultiThreadingProcessor<int>
    {
        private readonly long[] _results;

        public long Result { get; private set; }

        public SumSearch(int threadCount, int[] array)
            : base(threadCount, array)
        {
            _results = new long[threadCount];
        }

        public override void Process()
        {
            base.Process();
            Result = _results.Sum();
        }

        protected override void ProcessValue(int threadIndex, int itemIndex, Span<int> span)
        {
            _results[threadIndex] += span[itemIndex];
        }
    }

    class FreqChar : MultiThreadingProcessor<char>
    {
        private readonly Dictionary<char, int>[] _results;

        public Dictionary<char, int>? Result { get; private set; }

        public FreqChar(int threadCount, char[] array)
            : base(threadCount, array)
        {
            _results = new Dictionary<char, int>[threadCount];
        }

        public override void Process()
        {
            base.Process();
            Result = new Dictionary<char, int>();

            foreach (var item in _results)
            {
                foreach (var pair in item)
                {
                    if (Result.TryGetValue(pair.Key, out int value))
                    {
                        Result[pair.Key] = value + pair.Value;
                    }
                    else
                    {
                        Result[pair.Key] = pair.Value;
                    }
                }
            }
        }

        protected override void ProcessValue(int threadIndex, int itemIndex, Span<char> span)
        {
            var ch = span[itemIndex];
            var dic = _results[threadIndex];
            if (dic == null)
            {
                _results[threadIndex] = dic = new Dictionary<char, int>();
            }

            if (dic.TryGetValue(ch, out int value))
            {
                dic[ch] = value + 1;
            }
            else
            {
                dic[ch] = 1;
            }
        }
    }

    //class RandomArrayThreadProcessor
    //{
    //    private readonly int threadCount;
    //    private readonly int[] arr;
    //    private Thread[] _threads = Array.Empty<Thread>();

    //    public RandomArrayThreadProcessor(int threadCount, int[] arr)
    //    {
    //        this.threadCount = threadCount;
    //        this.arr = arr;
    //    }

    //    public void Run()
    //    {
    //        this._threads = new Thread[threadCount];

    //        for (int i = 0; i < threadCount; i++)
    //        {
    //            var num = i;
    //            _threads[i] = new Thread(() => Process(num))
    //            {
    //                IsBackground = true
    //            };
    //        }

    //        foreach (var thread in _threads)
    //        {
    //            thread.Start();
    //        }

    //        foreach (var thread in _threads) { thread.Join(); }
    //    }

    //    protected void Process(int threadIndex)
    //    {
    //        var rand = new Random();
    //        //todo: should add normal algorithm for getting array span.
    //        var span = arr[0..(arr.Length / threadCount)];

    //        for (int i = 1; i < span.Length; i++)
    //        {
    //            span[i] = rand.Next();
    //        }
    //    }
    //}

    internal class Program
    {
        // thread/task 1
        static async Task RunAddNum(ConcurrentBag<int> target, int num, CancellationToken cancellationToken)
        {
            Console.WriteLine("task started"); // task 1

            var lines = await File.ReadAllLinesAsync("", cancellationToken);

            for (var i = 0; i < 1000_000_000; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                target.Add(num);
            }

            var aa = async () =>
            {
                await File.WriteAllLinesAsync("", lines, cancellationToken);
            };

            await File.WriteAllLinesAsync("", lines, cancellationToken);
            Console.WriteLine("");
        }


        static async Task Main(string[] args)
        {
            ConcurrentBag<int> ints = new ConcurrentBag<int>();
            object lockObj = new object();

            var t1 = new Thread(() =>
            {
                //1. cancel
                //2. orchestration/management
                //3. without result

                for (var i = 0; i < 1_000_000; i++)
                    //lock(lockObj)
                    ints.Add(1);
                //sum++;
            })
            {
                IsBackground = true
            };

            var t2 = new Thread(() =>
            {
                for (var i = 0; i < 1_000_000; i++)
                    //lock(lockObj)
                    ints.Add(2);
                //sum--;
            })
            {
                IsBackground = true
            };

            var sww = Stopwatch.StartNew();
            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            var source = new CancellationTokenSource();

            var task = RunAddNum(ints, 1, source.Token);
            var task2 = RunAddNum(ints, 2, source.Token);

            //await task;
            //await task2;

            await Task.WhenAll(task, task2);

            //source.Cancel();



            Console.WriteLine($"{ints.Count}, {ints.Count(x => x == 1)}, {ints.Count(x => x == 2)}, {sww.Elapsed}");
            // wo/lock 00:00:00.0127101
            //         00:00:00.0610924
            //         00:00:00.0287641
            //                          00:00:00.0667521
            //                          00:00:00.1261556
            //                          00:00:00.1083332

            int threadCount = 128;
            int itemCount = 1_000_000_000;
            do
            {
                do
                {
                    GC.Collect();

                    Console.WriteLine($"=>Start test for treads: {threadCount}, itemCount: {itemCount}");

                    var arr = new int[itemCount];

                    var gen = new GenRandomArray(threadCount, arr);
                    var sw = Stopwatch.StartNew();
                    gen.Process();
                    Console.WriteLine($"--> gen random ints {sw.Elapsed}");

                    var sumProcessor = new SumSearch(threadCount, arr);
                    sw = Stopwatch.StartNew();
                    sumProcessor.Process();
                    Console.WriteLine($"--> search sum {sw.Elapsed} --- result: {sumProcessor.Result}");


                    var charArray = new char[itemCount];
                    sw = Stopwatch.StartNew();
                    var chGen = new GenRandomArray<char>(threadCount, charArray, r => (char)r.Next(32, 58));
                    chGen.Process();
                    Console.WriteLine($"--> gen chars {sw.Elapsed} --- result: {sumProcessor.Result}");

                    var freqDicProc = new FreqChar(threadCount, charArray);

                    sw = Stopwatch.StartNew();
                    freqDicProc.Process();
                    Console.WriteLine($"--> find chars freq {sw.Elapsed} --- result: {freqDicProc.Result!.Count}");

                    itemCount /= 10;
                }
                while (itemCount >= 1_000);

                threadCount /= 2;
            } while (threadCount >= 1);


            await task;
            //RunLamps();
        }

        static void RunLamps()
        {
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
