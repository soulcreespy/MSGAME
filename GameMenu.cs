using MSGAME;
using Raylib_cs;
using System;
using System.Numerics;

namespace MG
{
    public class GameMenu
    {
        private bool gamestart = false;
        private bool isTypingIp = false;
        private string ipInput = "127.0.0.1";
        private string statusMessage = "";

        private Rectangle gameBtn;
        private Rectangle hostBtn;
        private Rectangle connectBtn;
        private int screenWidth;
        private int screenHeight;

        private NetworkManager networkManager;

        // Флаги режима
        private bool isMultiplayerMode = false;
        private bool isServerMode = false;

        public GameMenu(int width, int height, NetworkManager netManager)
        {
            screenWidth = width;
            screenHeight = height;
            networkManager = netManager;

            gameBtn = new Rectangle(width / 2 - 90, height / 4 - 50, 180, 50);
            hostBtn = new Rectangle(width / 2 - 90, height / 4 + 20, 180, 50);
            connectBtn = new Rectangle(width / 2 - 90, height / 4 + 90, 180, 50);
        }

        public bool UpdateMenu()
        {
            if (gamestart) return true;

            Vector2 mousePoint = Raylib.GetMousePosition();

            // ===== КНОПКА "GAME" (одиночная игра) =====
            if (Raylib.CheckCollisionPointRec(mousePoint, gameBtn) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                isMultiplayerMode = false;
                isServerMode = false;
                gamestart = true;
                return true;
            }

            // ===== КНОПКА "HOST" (сервер) =====
            if (Raylib.CheckCollisionPointRec(mousePoint, hostBtn) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                networkManager.StartServer(7777);
                isMultiplayerMode = true;
                isServerMode = true;
                gamestart = true; // Сразу запускаем игру
                statusMessage = "Server started! Waiting for connections...";
                Console.WriteLine("[MENU] Server started");
                return true;
            }

            // ===== КНОПКА "JOIN" (клиент) =====
            if (Raylib.CheckCollisionPointRec(mousePoint, connectBtn) && Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                isTypingIp = true;
                ipInput = "";
                statusMessage = "Enter IP and press ENTER";
            }

            // ===== ОБРАБОТКА ВВОДА IP =====
            if (isTypingIp)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace))
                {
                    if (ipInput.Length > 0)
                        ipInput = ipInput.Substring(0, ipInput.Length - 1);
                }

                int c = Raylib.GetCharPressed();
                while (c > 0)
                {
                    if ((c >= 48 && c <= 57) || c == 46)
                        ipInput += (char)c;
                    c = Raylib.GetCharPressed();
                }

                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    isTypingIp = false;
                    bool connected = networkManager.ConnectToServer(ipInput, 7777);
                    if (connected)
                    {
                        isMultiplayerMode = true;
                        isServerMode = false;
                        gamestart = true;
                        statusMessage = $"Connected to {ipInput}!";
                        Console.WriteLine($"[MENU] Connected to {ipInput}");
                        return true;
                    }
                    else
                    {
                        isMultiplayerMode = false;
                        isServerMode = false;
                        statusMessage = $"Failed to connect to {ipInput}!";
                        Console.WriteLine($"[MENU] Connection failed");
                    }
                }
            }

            return false;
        }

        public void DrawMenu()
        {
            if (gamestart) return;

            Vector2 mousePoint = Raylib.GetMousePosition();

            DrawButton(gameBtn, "Game", mousePoint, Color.Green, Color.Lime, Color.DarkGreen);
            DrawButton(hostBtn, "Host", mousePoint, Color.DarkBlue, Color.Blue, Color.RayWhite);
            DrawButton(connectBtn, "Join", mousePoint, Color.DarkBlue, Color.Blue, Color.RayWhite);

            if (isTypingIp)
            {
                Raylib.DrawText($"IP: {ipInput}", screenWidth / 2 - 60, screenHeight / 4 + 155, 20, Color.Black);
                Raylib.DrawText("Enter IP and press ENTER", screenWidth / 2 - 120, screenHeight / 4 + 180, 15, Color.DarkGray);
            }
            else if (!string.IsNullOrEmpty(statusMessage))
            {
                Raylib.DrawText(statusMessage, screenWidth / 2 - 150, screenHeight / 4 + 155, 20, Color.DarkGray);
            }
        }

        private void DrawButton(Rectangle rect, string text, Vector2 mousePoint, Color normalColor, Color hoverColor, Color clickColor)
        {
            bool isHovered = Raylib.CheckCollisionPointRec(mousePoint, rect);
            Color btnColor;

            if (isHovered && Raylib.IsMouseButtonDown(MouseButton.Left))
                btnColor = clickColor;
            else if (isHovered)
                btnColor = hoverColor;
            else
                btnColor = normalColor;

            Raylib.DrawRectangleRec(rect, btnColor);
            Raylib.DrawRectangleLinesEx(rect, 2, Color.White);

            Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, 20, 1);
            int textX = (int)(rect.X + (rect.Width - textSize.X) / 2);
            int textY = (int)(rect.Y + (rect.Height - textSize.Y) / 2);
            Raylib.DrawText(text, textX, textY, 20, Color.White);
        }

        public void Reset()
        {
            gamestart = false;
            isMultiplayerMode = false;
            isServerMode = false;
            isTypingIp = false;
            statusMessage = "";
            networkManager.Close();
        }

        // ===== МЕТОДЫ ДЛЯ ПОЛУЧЕНИЯ РЕЖИМА =====
        public bool IsGameStart() => gamestart;
        public bool IsMultiplayerMode() => isMultiplayerMode;
        public bool IsServerMode() => isServerMode;
        public string GetIpInput() => ipInput;
    }
}