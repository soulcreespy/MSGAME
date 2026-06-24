using Raylib_cs;
using System;
using System.Numerics;

namespace MG
{
    public class Board
    {
        private const int GridSize = Config.GridSize;
        private const int CellSize = Config.CellSize;
        private const int GridX = Config.GridX;
        private const int GridY = Config.GridY;

        private char[,] gameboard = new char[GridSize, GridSize];
        private bool IsX = true;
        private bool gameover = false;
        private string winner = "";

        // Мультиплеер
        private NetworkManager networkManager;
        private bool isMultiplayer = false;
        private bool isServer = false;
        private char mySymbol = ' '; // Символ текущего игрока (X или O)
        private bool gameReady = false;

        // ===== КОНСТРУКТОРЫ =====
        public Board()
        {
            InitBoard();
        }

        public Board(NetworkManager netManager, bool server)
        {
            networkManager = netManager;
            isMultiplayer = true;
            isServer = server;
            mySymbol = isServer ? 'X' : 'O'; // Сервер всегда X, клиент O
            InitBoard();

            if (isServer)
            {
                networkManager.OnMoveReceived += OnMoveFromClient;
                networkManager.OnRestartReceived += OnRestartFromClient;
            }
            else
            {
                networkManager.OnBoardStateReceived += OnStateFromServer;
                networkManager.OnRestartReceived += OnRestartFromServer;
            }
        }

        private void InitBoard()
        {
            for (int row = 0; row < GridSize; row++)
                for (int col = 0; col < GridSize; col++)
                    gameboard[row, col] = ' ';
            gameover = false;
            IsX = true;
            winner = "";
            gameReady = false;
        }

        // ===== ОТРИСОВКА =====
        public void DrawGrid()
        {
            DrawHighLights();
            for (int row = 0; row < GridSize; row++)
                for (int col = 0; col < GridSize; col++)
                {
                    int x = col * CellSize + GridX;
                    int y = row * CellSize + GridY;
                    Raylib.DrawRectangleLines(x, y, CellSize, CellSize, Color.White);
                    if (gameboard[row, col] == 'X')
                        DrawX(x, y);
                    else if (gameboard[row, col] == 'O')
                        DrawO(x, y);
                }
        }

        private void DrawHighLights()
        {
            if (gameover) return;
            Vector2 mousePos = Raylib.GetMousePosition();
            if (mousePos.X < GridX || mousePos.X > GridX + GridSize * CellSize ||
                mousePos.Y < GridY || mousePos.Y > GridY + GridSize * CellSize)
                return;

            int col = (int)((mousePos.X - GridX) / CellSize);
            int row = (int)((mousePos.Y - GridY) / CellSize);

            if (row >= 0 && row < GridSize && col >= 0 && col < GridSize && gameboard[row, col] == ' ')
            {
                int x = col * CellSize + GridX;
                int y = row * CellSize + GridY;
                Raylib.DrawRectangleV(new Vector2(x, y), new Vector2(CellSize, CellSize), IsX ? Color.Red : Color.Blue);
            }
        }

        private void DrawX(int x, int y)
        {
            int padding = 20;
            Raylib.DrawLine(x + padding, y + padding, x + CellSize - padding, y + CellSize - padding, Color.Red);
            Raylib.DrawLine(x + CellSize - padding, y + padding, x + padding, y + CellSize - padding, Color.Red);
        }

        private void DrawO(int x, int y)
        {
            float radius = CellSize / 2 - 20;
            Raylib.DrawCircleLines(CellSize / 2 + x, CellSize / 2 + y, radius, Color.Blue);
        }

        // ===== ОБРАБОТКА КЛИКА =====
        public void HandleMouseClick()
        {
            if (gameover) return;

            // === НОВАЯ ПРОВЕРКА: может ли текущий игрок ходить? ===
            if (!CanMakeMove()) return;

            // Для сервера: не даём ходить, пока нет клиентов
            if (isMultiplayer && isServer && !networkManager.HasConnectedClients())
            {
                Console.WriteLine("[SERVER] Waiting for clients...");
                return;
            }

            Vector2 mousePos = Raylib.GetMousePosition();
            if (mousePos.X < GridX || mousePos.Y < GridY) return;

            int col = (int)((mousePos.X - GridX) / CellSize);
            int row = (int)((mousePos.Y - GridY) / CellSize);

            if (row < 0 || row >= GridSize || col < 0 || col >= GridSize) return;
            if (gameboard[row, col] != ' ') return;

            int cellIndex = row * GridSize + col;

            if (isMultiplayer)
            {
                if (isServer)
                {
                    if (ProcessMove(row, col))
                        BroadcastBoardState();
                }
                else
                {
                    networkManager.SendMessage(cellIndex.ToString());
                }
            }
            else
            {
                ProcessMove(row, col);
            }
        }

        // ===== ИГРОВАЯ ЛОГИКА =====
        private bool ProcessMove(int row, int col)
        {
            if (gameboard[row, col] != ' ') return false;
            gameboard[row, col] = IsX ? 'X' : 'O';
            PrintBoard();

            CheckWin();

            if (!gameover)
            {
                IsX = !IsX;
            }
            return true;
        }

        public bool ProcessMoveByIndex(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= 9) return false;
            if (gameover) return false;
            int row = cellIndex / GridSize;
            int col = cellIndex % GridSize;
            if (gameboard[row, col] != ' ') return false;
            return ProcessMove(row, col);
        }

        // ===== ПРОВЕРКА, МОЖЕТ ЛИ ИГРОК ХОДИТЬ (ИСПРАВЛЕНО) =====
        private bool CanMakeMove()
        {
            if (!isMultiplayer) return true;

            // Текущий символ, который должен ходить
            char currentSymbol = IsX ? 'X' : 'O';

            // Игрок может ходить, только если его символ совпадает с текущим
            return mySymbol == currentSymbol;
        }

        // ===== ПРОВЕРКА ПОБЕДЫ =====
        public void CheckWin()
        {
            for (int row = 0; row < GridSize; row++)
            {
                if (gameboard[row, 0] != ' ' &&
                    gameboard[row, 0] == gameboard[row, 1] &&
                    gameboard[row, 1] == gameboard[row, 2])
                {
                    gameover = true;
                    winner = $"Winner: {gameboard[row, 0]}";
                    return;
                }
            }

            for (int col = 0; col < GridSize; col++)
            {
                if (gameboard[0, col] != ' ' &&
                    gameboard[0, col] == gameboard[1, col] &&
                    gameboard[1, col] == gameboard[2, col])
                {
                    gameover = true;
                    winner = $"Winner: {gameboard[0, col]}";
                    return;
                }
            }

            if (gameboard[0, 0] != ' ' &&
                gameboard[0, 0] == gameboard[1, 1] &&
                gameboard[1, 1] == gameboard[2, 2])
            {
                gameover = true;
                winner = $"Winner: {gameboard[0, 0]}";
                return;
            }

            if (gameboard[0, 2] != ' ' &&
                gameboard[0, 2] == gameboard[1, 1] &&
                gameboard[1, 1] == gameboard[2, 0])
            {
                gameover = true;
                winner = $"Winner: {gameboard[0, 2]}";
                return;
            }

            bool draw = true;
            for (int row = 0; row < GridSize; row++)
                for (int col = 0; col < GridSize; col++)
                    if (gameboard[row, col] == ' ')
                    {
                        draw = false;
                        break;
                    }

            if (draw)
            {
                gameover = true;
                winner = "Draw";
            }
        }

        // ===== МЕТОДЫ ДЛЯ СЕТИ =====

        public string GetBoardState()
        {
            string state = "";
            for (int row = 0; row < GridSize; row++)
                for (int col = 0; col < GridSize; col++)
                    state += gameboard[row, col] == ' ' ? '0' : gameboard[row, col];
            state += IsX ? 'X' : 'O'; // добавляем информацию о ходе
            return state;
        }

        public void ApplyBoardState(string state)
        {
            if (state.Length != 10) return;

            for (int i = 0; i < 9; i++)
            {
                int row = i / GridSize;
                int col = i % GridSize;
                char c = state[i];
                gameboard[row, col] = c == '0' ? ' ' : c;
            }

            char turn = state[9];
            IsX = (turn == 'X');
            CheckWin();

            if (!gameover && !gameReady && isMultiplayer && !isServer)
            {
                gameReady = true;
                Console.WriteLine("[CLIENT] Game ready");
            }
        }

        public void BroadcastBoardState()
        {
            if (isMultiplayer && isServer && networkManager != null)
            {
                networkManager.BroadcastMessage(GetBoardState());
            }
        }

        public void BroadcastRestart()
        {
            if (isMultiplayer && isServer && networkManager != null)
            {
                networkManager.BroadcastRestart();
            }
        }

        // === ОБРАБОТЧИКИ СОБЫТИЙ ===

        public void OnStateFromServer(string state)
        {
            if (!isServer)
            {
                ApplyBoardState(state);
                Console.WriteLine("[CLIENT] Board updated");
            }
        }

        public void OnMoveFromClient(int cellIndex)
        {
            if (isServer)
            {
                Console.WriteLine($"[SERVER] Move from client: {cellIndex}");
                if (ProcessMoveByIndex(cellIndex))
                {
                    BroadcastBoardState();
                }
            }
        }

        public void OnRestartFromClient()
        {
            if (isServer)
            {
                Console.WriteLine("[SERVER] Restart requested by client");
                restart();
                BroadcastBoardState();
                BroadcastRestart();
            }
        }

        public void OnRestartFromServer()
        {
            if (!isServer)
            {
                Console.WriteLine("[CLIENT] Restart from server");
                restart();
            }
        }

        // ===== ОБЩИЕ МЕТОДЫ =====

        public void restart()
        {
            InitBoard();
            gameReady = false;
            if (isMultiplayer && isServer)
            {
                IsX = true; // Сервер начинает первым
            }
        }

        public void SetGameReady(bool ready) => gameReady = ready;

        private void PrintBoard()
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                    Console.Write(gameboard[row, col] + " ");
                Console.WriteLine();
            }
        }

        public bool IsGameOver() => gameover;
        public bool Turn() => IsX;
        public string GetWinner() => winner;
        public char GetMySymbol() => mySymbol;
    }
}