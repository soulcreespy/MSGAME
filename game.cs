using MSGAME;
using Raylib_cs;
using System;
using System.Numerics;

namespace MG
{
    public class Game
    {
        private const int ScreenWidth = Config.Width;
        private const int ScreenHeight = Config.Height;

        private Board board;
        private Mmusic musicManager;
        private SnowFlakes sf;
        private GameMenu menu;
        private NetworkManager networkManager;

        private bool isMultiplayer = false;
        private bool isServer = false;

        public void Run()
        {
            Raylib.InitWindow(ScreenWidth, ScreenHeight, "Крестики-Нолики");
            Raylib.SetTargetFPS(120);
            Raylib.InitAudioDevice();

            networkManager = new NetworkManager();
            menu = new GameMenu(ScreenWidth, ScreenHeight, networkManager);
            board = new Board(); // временно
            musicManager = new Mmusic();
            sf = new SnowFlakes();

            while (!Raylib.WindowShouldClose())
            {
                sf.Update();
                musicManager.UpdateMusic();

                bool gamerunning = menu.UpdateMenu();

                if (gamerunning && !isMultiplayer && menu.IsMultiplayerMode())
                {
                    isMultiplayer = true;
                    isServer = menu.IsServerMode();
                    board = new Board(networkManager, isServer);
                    Console.WriteLine($"[GAME] Multiplayer: {(isServer ? "SERVER" : "CLIENT")}");
                }

                // === ОБРАБОТКА РЕСТАРТА ОТ КЛИЕНТА ===
                if (isMultiplayer && !isServer && board.IsGameOver())
                {
                    // Если игра завершена и нажат R, отправляем рестарт на сервер
                    if (Raylib.IsKeyPressed(KeyboardKey.R))
                    {
                        networkManager.SendMessage("restart");
                        Console.WriteLine("[CLIENT] Restart requested");
                    }
                }

                if (gamerunning)
                {
                    if (!board.IsGameOver() && Raylib.IsMouseButtonPressed(MouseButton.Left))
                    {
                        board.HandleMouseClick();
                    }

                    // Локальный рестарт для одиночной игры
                    if (!isMultiplayer && Raylib.IsKeyPressed(KeyboardKey.R))
                    {
                        board.restart();
                    }
                }

                // ===== ОТРИСОВКА =====
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Gray);

                sf.Draw();

                if (gamerunning)
                {
                    GameInfo();
                    board.DrawGrid();
                    DrawRestartButton();
                }
                else
                {
                    menu.DrawMenu();
                }

                Info();
                musicManager.MusicInfo();

                Raylib.EndDrawing();
            }

            networkManager.Close();
            musicManager.Unload();
            Raylib.CloseAudioDevice();
            Raylib.CloseWindow();
        }

        private void DrawRestartButton()
        {
            if (!board.IsGameOver()) return;

            Rectangle btnBounds = new(ScreenWidth - 200, ScreenHeight / 3, 180, 50);
            Vector2 mousePoint = Raylib.GetMousePosition();
            bool isHovered = Raylib.CheckCollisionPointRec(mousePoint, btnBounds);

            Color btnColor;
            if (isHovered && Raylib.IsMouseButtonDown(MouseButton.Left))
                btnColor = Color.Lime;
            else if (isHovered)
                btnColor = Color.Green;
            else
                btnColor = Color.DarkGreen;

            Raylib.DrawRectangleRec(btnBounds, btnColor);
            Raylib.DrawRectangleLinesEx(btnBounds, 2, Color.White);

            string text = "RESTART";
            int fontSize = 25;
            Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, fontSize, 1);
            int textX = (int)(btnBounds.X + (btnBounds.Width - textSize.X) / 2);
            int textY = (int)(btnBounds.Y + (btnBounds.Height - textSize.Y) / 2);
            Raylib.DrawText(text, textX, textY, fontSize, Color.White);

            if (isHovered && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                musicManager.PlayButtonSound();
                if (isMultiplayer)
                {
                    if (isServer)
                    {
                        board.restart();
                        board.BroadcastBoardState();
                        board.BroadcastRestart();
                    }
                    else
                    {
                        networkManager.SendMessage("restart");
                    }
                }
                else
                {
                    board.restart();
                }
            }
        }

        private void GameInfo()
        {
            if (board.IsGameOver())
            {
                string winner = board.GetWinner();
                if (!string.IsNullOrEmpty(winner))
                {
                    Raylib.DrawText(winner, ScreenWidth / 2 - 90, ScreenHeight / 4 - 25, 50, Color.Green);
                }
            }
            else
            {
                char player = board.Turn() ? 'X' : 'O';
                string status = $"Turn: {player}";
                Raylib.DrawText(status, ScreenWidth / 2 - 90, ScreenHeight / 4 - 25, 50, Color.White);
            }

            if (isMultiplayer)
            {
                string netStatus = isServer ? "[SERVER]" : "[CLIENT]";
                Color netColor = isServer ? Color.Green : Color.Yellow;
                Raylib.DrawText(netStatus, 12, 60, 20, netColor);
                Raylib.DrawText($"Players: {networkManager.GetClientCount() + 1}", 12, 80, 20, Color.White);
                if (!isServer)
                {
                    char mySymbol = board.GetMySymbol();
                    Raylib.DrawText($"You: {mySymbol}", 12, 100, 20, Color.White);
                }
            }
        }

        private void Info()
        {
            int fps = Raylib.GetFPS();
            long totalMemory = GC.GetTotalMemory(false);
            Raylib.DrawText($"FPS:{fps} GC: {totalMemory / 1024}KB", 12, 120, 20, Color.Green);

            Raylib.DrawText("Commands info:", ScreenWidth - 200, 12, 25, Color.White);
            Raylib.DrawText("Pause music - Space", ScreenWidth - 220, 40, 20, Color.White);
            Raylib.DrawText("Restart music - P", ScreenWidth - 220, 60, 20, Color.White);
            Raylib.DrawText("Stop snowflakes - V", ScreenWidth - 220, 80, 20, Color.White);
            Raylib.DrawText("Backup menu - R", ScreenWidth - 220, 100, 20, Color.White);
        }
    }
}