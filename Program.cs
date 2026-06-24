using Raylib_cs;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Numerics;

namespace MG
{
    internal class Program
    {
       
        public static void Main(string[] args)
        {
            Game game=new Game();
            game.Run();
        }
    }
}