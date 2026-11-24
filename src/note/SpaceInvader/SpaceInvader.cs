/*
 *  1. 스페이스 인베이더는 무엇인가? (기획)
 *  윈도우 콘솔 명령 프롬프트에서 실행 하는 환경의 게임을 만들거야
 *  콘솔 화면에서 인베이더 개체 40기 가 5열 8행 짜리 나열되어있고 
 *  시간이 지날수록 조금씩 아래로 내려와서 플레이어 기체에 하나라도 닿으면 게임오버
 *  플레이어는 투사체를 발사해 인베이더들을 공격할 수 있고 하나의 기체가 파괴될때마다
 *  전체 기체의 움직임이 1프레임씩 빨라져서 난이도가 높아지는거야
 *  복잡한것은 필요하지 않고 지금 말한것만 구현하고 게임 오버가 되면 게임을 다시 할지
 *  결정하고 재시도 혹은 종료로 만들면되
 *  
 *  2. 시스템 설계
 *  (1) 게임 루프를 반드시 지켜서 만들어라.
 *  (2) 게임 루프를 입력, 갱신, 렌더링을 프레임 (한 바퀴)로 표현한 것이다.
 *  (3) 입력은 이벤트
 *  (4) 갱신은 delta time
 *  (5) 화면을 그려줘 ->렌더링 기술 무엇으로 만들라고 설명. 내가 알아서 할게. ( 랜덤 돌리는것 )
 *  (6) C#으로 만들어라.
 *  
 *  부록. 최상위문을 활용해서 Main코드를 간략하게 실행할 수 있는 구조로 만드세요. 예를 들어. Game.Run() 같은 형태여야 한다.
 *  Game클래스 시스템 설계를 충실히 구성하기 위해 여러개의 클래스로 구성해라.
 *  
 *  문제점 , 이상한점, 해결해야할 점
 *  
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

// 최상위 문: 진입점을 간략하게 유지
Game.Run();

public sealed class Game
{
    private enum GameState
    {
        Playing,
        GameOver
    }

    private const int ScreenWidth = 60;
    private const int ScreenHeight = 32;

    private readonly ConsoleRenderer _renderer;
    private readonly InputHandler _input;
    private readonly List<Bullet> _bullets = new();

    private Player _player = null!;
    private InvaderSwarm _swarm = null!;
    private GameState _state;
    private int _score;
    private int _wave;
    private bool _shouldExit;

    private Game()
    {
        _renderer = new ConsoleRenderer(ScreenWidth, ScreenHeight);
        _input = new InputHandler();
        _input.KeyPressed += OnKeyPressed;
        ResetGame(resetScore: true);
    }

    public static void Run()
    {
        Game game = new();
        game.Loop();
    }

    private void Loop()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        long previousTicks = stopwatch.ElapsedTicks;

        while (!_shouldExit)
        {
            long currentTicks = stopwatch.ElapsedTicks;
            float deltaTime = (float)(currentTicks - previousTicks) / Stopwatch.Frequency;
            previousTicks = currentTicks;

            if (deltaTime > 0.1f)
            {
                deltaTime = 0.1f;
            }

            // 1. 입력 (이벤트)
            _input.Poll();

            // 2. 갱신 (Delta Time)
            Update(deltaTime);

            // 3. 렌더링 (2차원 더블 버퍼 + 변경 셀 부분 갱신)
            Render();

            Thread.Sleep(15);
        }

        _input.KeyPressed -= OnKeyPressed;
        Console.CursorVisible = true;
        Console.SetCursorPosition(0, ScreenHeight);
    }

    private void OnKeyPressed(ConsoleKey key)
    {
        if (_state == GameState.Playing)
        {
            switch (key)
            {
                case ConsoleKey.LeftArrow:
                case ConsoleKey.A:
                    _player.MoveLeft();
                    break;
                case ConsoleKey.RightArrow:
                case ConsoleKey.D:
                    _player.MoveRight();
                    break;
                case ConsoleKey.Spacebar:
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    if (_player.TryFire(out Bullet? bullet) && bullet is not null)
                    {
                        _bullets.Add(bullet);
                    }
                    break;
                case ConsoleKey.Escape:
                    _shouldExit = true;
                    break;
            }
        }
        else if (_state == GameState.GameOver)
        {
            switch (key)
            {
                case ConsoleKey.R:
                case ConsoleKey.Y:
                case ConsoleKey.Enter:
                    ResetGame(resetScore: true);
                    _renderer.ForceFullRedraw();
                    break;
                case ConsoleKey.Q:
                case ConsoleKey.N:
                case ConsoleKey.Escape:
                    _shouldExit = true;
                    break;
            }
        }
    }

    private void Update(float deltaTime)
    {
        if (_state != GameState.Playing)
        {
            return;
        }

        _player.Update(deltaTime);
        _swarm.Update(deltaTime);

        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            Bullet bullet = _bullets[i];
            bullet.Update(deltaTime);

            if (!bullet.IsActive)
            {
                _bullets.RemoveAt(i);
                continue;
            }

            int bx = (int)Math.Round(bullet.X);
            int by = (int)Math.Round(bullet.Y);

            if (_swarm.TryHit(bx, by))
            {
                bullet.Deactivate();
                _bullets.RemoveAt(i);
                _score += 100;
            }
        }

        if (_swarm.ReachedPlayer)
        {
            _state = GameState.GameOver;
            return;
        }

        if (_swarm.AliveCount == 0)
        {
            _wave++;
            SpawnSwarm(_wave);
            _bullets.Clear();
        }
    }

    private void Render()
    {
        _renderer.ClearBackBuffer(' ');

        DrawBorder();
        _renderer.DrawText(2, 0, $" SCORE: {_score}  WAVE: {_wave}  INVADERS: {_swarm.AliveCount}/{InvaderSwarm.TotalCount} ");

        _swarm.Draw(_renderer);

        foreach (Bullet bullet in _bullets)
        {
            bullet.Draw(_renderer);
        }

        _player.Draw(_renderer);

        if (_state == GameState.GameOver)
        {
            string title = "=== GAME OVER ===";
            string retry = "[R / ENTER] 다시하기";
            string quit = "[Q / ESC]   종료";

            int midY = ScreenHeight / 2;
            _renderer.DrawText((ScreenWidth - title.Length) / 2, midY - 1, title);
            _renderer.DrawText((ScreenWidth - retry.Length) / 2, midY + 1, retry);
            _renderer.DrawText((ScreenWidth - quit.Length) / 2, midY + 2, quit);
        }

        _renderer.Present();
    }

    private void DrawBorder()
    {
        for (int x = 0; x < ScreenWidth; x++)
        {
            _renderer.DrawChar(x, 1, '-');
            _renderer.DrawChar(x, ScreenHeight - 1, '-');
        }

        for (int y = 1; y < ScreenHeight; y++)
        {
            _renderer.DrawChar(0, y, '|');
            _renderer.DrawChar(ScreenWidth - 1, y, '|');
        }
    }

    private void ResetGame(bool resetScore)
    {
        if (resetScore)
        {
            _score = 0;
            _wave = 1;
        }

        _bullets.Clear();
        _state = GameState.Playing;

        int playerY = ScreenHeight - 3;
        _player = new Player(
            startX: ScreenWidth / 2,
            y: playerY,
            minX: 2,
            maxX: ScreenWidth - 3,
            fireCooldown: 0.18f);

        SpawnSwarm(_wave);
    }

    private void SpawnSwarm(int wave)
    {
        float waveSpeedScale = MathF.Pow(0.88f, wave - 1);
        int playerY = ScreenHeight - 3;

        _swarm = new InvaderSwarm(
            startX: 6,
            startY: 3,
            spacingX: 4,
            spacingY: 1,
            minX: 2,
            maxX: ScreenWidth - 3,
            playerY: playerY,
            baseStepInterval: 0.65f * waveSpeedScale,
            minStepInterval: 0.035f);
    }
}

public sealed class ConsoleRenderer
{
    public int Width { get; }
    public int Height { get; }

    private readonly char[,] _frontBuffer;
    private readonly char[,] _backBuffer;

    public ConsoleRenderer(int width, int height)
    {
        Width = width;
        Height = height;
        _frontBuffer = new char[height, width];
        _backBuffer = new char[height, width];

        Console.CursorVisible = false;
        TryConfigureWindow(width, height);
        ClearBuffers(' ');
        Console.Clear();
    }

    public void ClearBackBuffer(char fill = ' ')
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _backBuffer[y, x] = fill;
            }
        }
    }

    public void DrawChar(int x, int y, char glyph)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return;
        }

        _backBuffer[y, x] = glyph;
    }

    public void DrawText(int x, int y, string text)
    {
        if (string.IsNullOrEmpty(text) || y < 0 || y >= Height)
        {
            return;
        }

        for (int i = 0; i < text.Length; i++)
        {
            int targetX = x + i;
            if (targetX >= 0 && targetX < Width)
            {
                _backBuffer[y, targetX] = text[i];
            }
        }
    }

    public void Present()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char next = _backBuffer[y, x];
                if (_frontBuffer[y, x] != next)
                {
                    _frontBuffer[y, x] = next;
                    Console.SetCursorPosition(x, y);
                    Console.Write(next);
                }
            }
        }
    }

    public void ForceFullRedraw()
    {
        Console.Clear();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char ch = _backBuffer[y, x];
                _frontBuffer[y, x] = ch;
                Console.SetCursorPosition(x, y);
                Console.Write(ch);
            }
        }
    }

    private void ClearBuffers(char fill)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _frontBuffer[y, x] = fill;
                _backBuffer[y, x] = fill;
            }
        }
    }

    private static void TryConfigureWindow(int width, int height)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            if (Console.WindowWidth < width || Console.WindowHeight < height)
            {
                Console.SetWindowSize(
                    Math.Max(Console.WindowWidth, width),
                    Math.Max(Console.WindowHeight, height));
            }

            if (Console.BufferWidth < width || Console.BufferHeight < height)
            {
                Console.SetBufferSize(
                    Math.Max(Console.BufferWidth, width),
                    Math.Max(Console.BufferHeight, height));
            }
        }
        catch
        {
            // 일부 터미널 환경에서 창 크기 조정이 제한될 수 있음
        }
    }
}

public sealed class InputHandler
{
    public event Action<ConsoleKey>? KeyPressed;

    public void Poll()
    {
        while (Console.KeyAvailable)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
            KeyPressed?.Invoke(keyInfo.Key);
        }
    }
}

public sealed class Player
{
    public int X { get; private set; }
    public int Y { get; }
    public char Glyph { get; } = 'A';

    private readonly int _minX;
    private readonly int _maxX;
    private readonly float _fireCooldown;
    private float _cooldownTimer;

    public Player(int startX, int y, int minX, int maxX, float fireCooldown = 0.2f)
    {
        X = startX;
        Y = y;
        _minX = minX;
        _maxX = maxX;
        _fireCooldown = fireCooldown;
    }

    public void Update(float deltaTime)
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
        }
    }

    public void MoveLeft()
    {
        if (X > _minX)
        {
            X--;
        }
    }

    public void MoveRight()
    {
        if (X < _maxX)
        {
            X++;
        }
    }

    public bool TryFire(out Bullet? bullet)
    {
        if (_cooldownTimer > 0f)
        {
            bullet = null;
            return false;
        }

        _cooldownTimer = _fireCooldown;
        bullet = new Bullet(X, Y - 1, speed: 26f, glyph: '|');
        return true;
    }

    public void Draw(ConsoleRenderer renderer)
    {
        renderer.DrawChar(X, Y, Glyph);
    }
}

public sealed class Bullet
{
    public float X { get; }
    public float Y { get; private set; }
    public char Glyph { get; }
    public bool IsActive { get; private set; } = true;

    private readonly float _speed;

    public Bullet(int x, int y, float speed, char glyph = '|')
    {
        X = x;
        Y = y;
        _speed = speed;
        Glyph = glyph;
    }

    public void Update(float deltaTime)
    {
        if (!IsActive)
        {
            return;
        }

        Y -= _speed * deltaTime;
        if (Y < 1f)
        {
            IsActive = false;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Draw(ConsoleRenderer renderer)
    {
        if (!IsActive)
        {
            return;
        }

        renderer.DrawChar((int)Math.Round(X), (int)Math.Round(Y), Glyph);
    }
}

public sealed class InvaderSwarm
{
    public const int Columns = 5;
    public const int Rows = 8;
    public const int TotalCount = Columns * Rows; // 40기

    private sealed class InvaderUnit
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsAlive { get; set; } = true;
        public char Glyph { get; init; }
    }

    private readonly InvaderUnit[,] _grid = new InvaderUnit[Rows, Columns];
    private readonly int _minX;
    private readonly int _maxX;
    private readonly int _playerY;
    private readonly float _baseStepInterval;
    private readonly float _minStepInterval;

    private int _direction = 1; // 1: 오른쪽, -1: 왼쪽
    private float _stepTimer;
    private int _destroyedCount;

    public int AliveCount => TotalCount - _destroyedCount;
    public bool ReachedPlayer { get; private set; }

    public InvaderSwarm(
        int startX,
        int startY,
        int spacingX,
        int spacingY,
        int minX,
        int maxX,
        int playerY,
        float baseStepInterval = 0.65f,
        float minStepInterval = 0.04f)
    {
        _minX = minX;
        _maxX = maxX;
        _playerY = playerY;
        _baseStepInterval = baseStepInterval;
        _minStepInterval = minStepInterval;

        char[] rowGlyphs = { 'W', 'M', 'H', 'X', 'O', 'U', 'V', 'Y' };

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                _grid[r, c] = new InvaderUnit
                {
                    X = startX + c * spacingX,
                    Y = startY + r * spacingY,
                    Glyph = rowGlyphs[r % rowGlyphs.Length],
                    IsAlive = true
                };
            }
        }
    }

    public void Update(float deltaTime)
    {
        if (ReachedPlayer || AliveCount == 0)
        {
            return;
        }

        float currentInterval = GetCurrentStepInterval();
        _stepTimer += deltaTime;

        if (_stepTimer < currentInterval)
        {
            return;
        }

        _stepTimer -= currentInterval;
        StepSwarm();
    }

    public bool TryHit(int bulletX, int bulletY)
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                InvaderUnit unit = _grid[r, c];
                if (unit.IsAlive && unit.X == bulletX && unit.Y == bulletY)
                {
                    unit.IsAlive = false;
                    _destroyedCount++;
                    return true;
                }
            }
        }

        return false;
    }

    public void Draw(ConsoleRenderer renderer)
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                InvaderUnit unit = _grid[r, c];
                if (unit.IsAlive)
                {
                    renderer.DrawChar(unit.X, unit.Y, unit.Glyph);
                }
            }
        }
    }

    private float GetCurrentStepInterval()
    {
        float stepReduction = 0.015f;
        float interval = _baseStepInterval - (_destroyedCount * stepReduction);
        return Math.Max(_minStepInterval, interval);
    }

    private void StepSwarm()
    {
        List<InvaderUnit> aliveUnits = GetAliveUnits();
        if (aliveUnits.Count == 0)
        {
            return;
        }

        bool hitEdge = false;
        foreach (InvaderUnit unit in aliveUnits)
        {
            int nextX = unit.X + _direction;
            if (nextX < _minX || nextX > _maxX)
            {
                hitEdge = true;
                break;
            }
        }

        if (hitEdge)
        {
            _direction *= -1;
            foreach (InvaderUnit unit in aliveUnits)
            {
                unit.Y += 1;
                if (unit.Y >= _playerY)
                {
                    ReachedPlayer = true;
                }
            }
        }
        else
        {
            foreach (InvaderUnit unit in aliveUnits)
            {
                unit.X += _direction;
            }
        }
    }

    private List<InvaderUnit> GetAliveUnits()
    {
        List<InvaderUnit> list = new(TotalCount);
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                if (_grid[r, c].IsAlive)
                {
                    list.Add(_grid[r, c]);
                }
            }
        }

        return list;
    }
}