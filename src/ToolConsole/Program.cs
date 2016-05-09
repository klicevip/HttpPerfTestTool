using HttpPerfTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToolConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpMonster monster = new HttpMonster(new Log());
            monster.Start("http://www.test1.com/home/test", "GET", null, 10000);
            string input = Console.ReadLine();
            while (input != "exit")
            {
                monster.LogState();
                input = Console.ReadLine();
            }
            monster.Stop();
        }
    }

    class Log : ILog
    {
        public void Info(string format, params object[] pars)
        {
            Console.WriteLine(format, pars);
        }
    }
}
