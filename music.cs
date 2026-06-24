using Raylib_cs;
using System.Numerics;

namespace MG
{
    public class Mmusic
    {
        private Sound fxButton;
        private Music music;
        private bool pause = false;
        private float LengthMusic = 0;
       
        private  const string SoundPath ="sounds/boom.wav";
        private  string MusicPath = "sounds/minecraft.mp3";

        public Mmusic()
        {
            fxButton = Raylib.LoadSound(SoundPath);  
            InitMusic();
        }

        private void InitMusic()
        {
            music = Raylib.LoadMusicStream(MusicPath);
            Raylib.PlayMusicStream(music);
            LengthMusic = Raylib.GetMusicTimeLength(music);
            pause = false;
        }

        public void UpdateMusic()  
        {
            if (Raylib.IsKeyPressed(KeyboardKey.P))
            {
                Raylib.StopMusicStream(music);
                Raylib.PlayMusicStream(music);
            }
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                pause = !pause;
                if (pause)
                    Raylib.PauseMusicStream(music);
                else
                    Raylib.ResumeMusicStream(music);
            }
            Raylib.UpdateMusicStream(music);  
        }

        public void MusicInfo() 
        {
            float CurrentSecond = Raylib.GetMusicTimePlayed(music);
            string status = pause ? "paused" : "playing";
            Raylib.DrawText($"Status:{status}", 12, 12, 20, Color.White);
            Raylib.DrawText($"TimePlayed:{CurrentSecond:F0}/{LengthMusic:F0}", 12, 36, 20, Color.White);
        }

        
        public void PlayButtonSound()
        {
            Raylib.PlaySound(fxButton);
        }

        
        public void Unload()
        {
            Raylib.UnloadSound(fxButton);
            Raylib.UnloadMusicStream(music);
        }
    }
}