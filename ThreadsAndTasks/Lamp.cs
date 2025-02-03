namespace ThreadsAndTasks
{
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
}
