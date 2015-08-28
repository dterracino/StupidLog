namespace demo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class Program
    {
        public void Main(string[] args)
        {
            StupidLog.StupidLog.Stupidify();
            Console.WriteLine("[V] this is debug");
            Console.WriteLine("[I] this is info");
            Console.WriteLine("[W] this is warning");
            Console.WriteLine("[e] this is error");
            Console.WriteLine("[e]");
            Console.WriteLine("[e");
            Console.WriteLine("1");
            Console.WriteLine(1);
        }
    }
}
