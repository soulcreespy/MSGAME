using MG;
using Raylib_cs;
using System.Numerics;

public class SnowFlakes
{
    private const int screenWidth = Config.Width;
    private const int screenHeight = Config.Height;
    private const int countSF=Config.countSF;
    private static readonly Random rnd = new Random();
    
    private SF[] snowflakes;
    private bool paused = false;
    

    public SnowFlakes()
    {
        int screenWidth = Config.Width;
        int screenHeight = Config.Height;
        int countSF = Config.countSF;

        snowflakes = new SF[countSF];

        for (int i = 0; i < snowflakes.Length; i++)
        {
            snowflakes[i] = new SF
            {
                position = new Vector2(
                    rnd.Next(5, screenWidth - 5),
                    rnd.Next(-screenHeight, 0)
                ),
                speed = new Vector2(0, (float)rnd.NextDouble() ),
                size = rnd.Next(1, 4)
            };
        }
    }

    public void Update()
    {
        
        if (Raylib.IsKeyPressed(KeyboardKey.V))
            paused = !paused;

        if (paused) return;

        for (int i = 0; i < snowflakes.Length; i++)
        {
            snowflakes[i].position += snowflakes[i].speed;

            
            if (snowflakes[i].position.Y > screenHeight + snowflakes[i].size)
            {
                snowflakes[i].position.Y = -snowflakes[i].size;
                snowflakes[i].position.X = rnd.Next(5, screenWidth - 5);
            }
        }
    }

    public void Draw()
    {
        

        for (int i = 0; i < snowflakes.Length; i++)
        {
          Raylib.DrawCircleV(snowflakes[i].position, snowflakes[i].size, Color.White);
            
        }
    }

   
    //public bool IsPaused => paused;

    public struct SF
    {
        public Vector2 position;
        public Vector2 speed;
        public int size;
        
    }
}