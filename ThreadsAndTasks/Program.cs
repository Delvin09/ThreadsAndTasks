using System.Drawing;

namespace ThreadsAndTasks
{
    class Lamp
    {
        private readonly ConsoleColor color;
        private readonly int left;
        private readonly int top;

        private int currentTime;

        public Lamp(int timeOn, int timeOff, ConsoleColor color, int left, int top)
        {
            this.TimeOn = timeOn;
            this.TimeOff = timeOff;
            this.color = color;
            this.left = left;
            this.top = top;

            currentTime = timeOff;
        }

        public bool IsOn { get; private set; } = false;

        public int TimeOn { get; }

        public int TimeOff { get; }

        public void TryToggle(int timeLeft)
        {
            currentTime -= timeLeft;
            if (currentTime <= 0)
            {
                IsOn = !IsOn;

                Console.ResetColor();
                Console.SetCursorPosition(left, top);
                if (IsOn) Console.BackgroundColor = color;
                Console.Write(" ");
                Console.ResetColor();

                currentTime = IsOn ? TimeOn : TimeOff;
            }
        }
    }

    class LampScheduler
    {
        private const int quant = 10;

        private readonly Lamp[] lamps;

        public LampScheduler(params Lamp[] lamps)
        {
            this.lamps = lamps;
        }

        public void Run()
        {
            while (true)
            {
                foreach (var item in lamps)
                {
                    item.TryToggle(quant);
                }

                Thread.Sleep(quant);
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            var arr = new Lamp[] {
                new Lamp(1000, 200, ConsoleColor.Red, 1, 1),
                new Lamp(300, 500, ConsoleColor.Green, 2, 2),
                new Lamp(1300, 1500, ConsoleColor.Cyan, 3, 3),
                new Lamp(150, 200, ConsoleColor.Magenta, 4, 4),
                new Lamp(700, 400, ConsoleColor.Yellow, 5, 5),
                new Lamp(200, 100, ConsoleColor.White, 6, 6),
            };

            var scheduler = new LampScheduler(arr);
            scheduler.Run();
        }
    }
}
