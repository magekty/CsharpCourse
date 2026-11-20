using System;
using System.Collections.Generic;
using System.Threading;

namespace ItJustMining
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var engine = new GameEngine();
            engine.Run();
        }
    }

    internal enum TileType
    {
        Rock,       // # 미개척 암반
        Dug,        // . 채굴 완료 통로
        Vein,       // $ 풍부한 광맥 징후
        Hazard      // ! 고위험 균열 징후
    }

    internal enum GameOverReason
    {
        None,
        Bankrupt,       // 자금 파산
        FatalCollapse   // 심층/다차원 붕괴 후 재기 불능
    }

    internal sealed class GameState
    {
        public long Gold { get; set; } = 100;
        public long BagGold { get; set; } = 0;
        public int Depth { get; set; } = 0; // 0 = 지상, 1+ = 지하/다차원 층
        public int Col { get; set; } = 3;   // 0~6 (3이 중앙)

        public int MaxDurability { get; set; } = 100;
        public int Durability { get; set; } = 100;
        public int LateralStepsInFloor { get; set; } = 0; // 현재 층에서 좌우 횡단 횟수

        public long BestGold { get; set; } = 100;
        public int BestDepth { get; set; } = 0;
        public int RankIndex { get; set; } = 0;
        public long CurrentTarget { get; set; } = MineRules.RankTargets[0];

        public string Message { get; set; } = "광산에 내려가 좌·우를 탐색하거나 아래로 파고들며 정산 타이밍을 잡으세요.";
        public bool Running { get; set; } = true;
        public GameOverReason OverReason { get; set; } = GameOverReason.None;
    }

    internal sealed class MineMap
    {
        public const int Width = 7; // 0~6 (좌3, 중1, 우3)
        private readonly Dictionary<int, TileType[]> _floors = new Dictionary<int, TileType[]>();
        private readonly Random _rng;

        public MineMap(Random rng)
        {
            _rng = rng;
        }

        public void Reset()
        {
            _floors.Clear();
        }

        public TileType[] GetOrCreateFloor(int depth)
        {
            if (depth <= 0) return Array.Empty<TileType>();
            if (_floors.TryGetValue(depth, out var existing)) return existing;

            var tiles = new TileType[Width];
            for (int c = 0; c < Width; c++)
            {
                if (c == 3)
                {
                    tiles[c] = TileType.Dug;
                    continue;
                }

                int roll = _rng.Next(100);
                if (roll < 28) tiles[c] = TileType.Vein;
                else if (roll < 48 + Math.Min(25, depth * 2)) tiles[c] = TileType.Hazard;
                else tiles[c] = TileType.Rock;
            }

            _floors[depth] = tiles;
            return tiles;
        }

        public TileType GetTile(int depth, int col)
        {
            if (depth <= 0 || col < 0 || col >= Width) return TileType.Rock;
            return GetOrCreateFloor(depth)[col];
        }

        public void MarkDug(int depth, int col)
        {
            if (depth <= 0 || col < 0 || col >= Width) return;
            GetOrCreateFloor(depth)[col] = TileType.Dug;
        }
    }

    internal static class MineRules
    {
        public const long MinSurviveGold = 15;

        public static readonly string[] RankNames =
        {
            "초급 (Novice)",
            "중급 (Intermediate)",
            "고급 (Advanced)",
            "숙련 (Skilled)",
            "베테랑 (Veteran)",
            "마스터 (Master)",
            "그랜드마스터 (Grandmaster)"
        };

        public static readonly long[] RankTargets =
        {
            1_500,
            4_000,
            10_000,
            25_000,
            60_000,
            150_000,
            400_000
        };

        public static string GetRankTitle(int rankIndex)
        {
            if (rankIndex < RankNames.Length)
                return RankNames[rankIndex];

            int endlessLv = rankIndex - RankNames.Length + 1;
            return $"무한 (Endless Lv.{endlessLv})";
        }

        public static string GetZoneName(int depth)
        {
            if (depth == 0) return "지상 캠프";
            if (depth <= 3) return "상층 갱도";
            if (depth <= 6) return "중층 암반";
            if (depth <= 9) return "심층 단층";
            if (depth == 10) return "심연의 바닥";
            if (depth <= 15) return "아공간 균열";
            if (depth <= 20) return "공허의 회랑";

            int phase = (depth - 21) / 5 + 4;
            return $"미지의 심연(제{phase}차원)";
        }

        public static string GetColLabel(int col)
        {
            int offset = col - 3;
            if (offset == 0) return "중앙";
            return offset < 0 ? $"좌 {Math.Abs(offset)}" : $"우 {offset}";
        }

        // 아래 하강 비용
        public static long GetDownCost(int nextDepth)
        {
            if (nextDepth <= 1) return 15;
            if (nextDepth <= 10) return 15 + (nextDepth - 1) * 14;
            return 141 + (nextDepth - 10) * 36;
        }

        // 같은 층 좌/우 굴진 비용 (연속 횡단 시 할증)
        public static long GetLateralCost(int depth, int lateralSteps)
        {
            long baseCost = depth <= 3 ? 10 : depth <= 10 ? 18 : 35;
            return (long)Math.Round(baseCost * (1.0 + lateralSteps * 0.25));
        }

        public static int GetSuccessChance(int depth, bool isLateral, TileType tile, int lateralSteps)
        {
            double chance = depth <= 10
                ? 95.0 - depth * 3.5
                : 58.0 - (depth - 10) * 2.0;

            if (isLateral)
            {
                chance -= 4.0 + lateralSteps * 4.5;
                if (tile == TileType.Vein) chance += 6.0;
                if (tile == TileType.Hazard) chance -= 10.0;
                if (tile == TileType.Dug) chance += 8.0;
            }

            return Math.Clamp((int)Math.Round(chance), 18, 95);
        }

        public static (int minDmg, int maxDmg) GetDamageRange(int depth, bool isLateral, TileType tile, long bagGold)
        {
            int zoneBonus = depth <= 3 ? 0
                : depth <= 6 ? 7
                : depth <= 10 ? 14
                : depth <= 15 ? 20 : 26;

            int latBonus = isLateral ? 6 : 0;
            int tileBonus = tile == TileType.Hazard ? 10 : 0;
            int bagPressure = (int)Math.Min(16, bagGold / 110);

            int minDmg = 24 + zoneBonus + latBonus + tileBonus + bagPressure / 2;
            int maxDmg = 36 + zoneBonus + latBonus + tileBonus + bagPressure;
            return (minDmg, maxDmg);
        }

        public static long GetReward(int depth, bool isLateral, TileType tile)
        {
            double zoneMult = 1.0;
            if (depth == 3 || depth == 6 || depth == 9) zoneMult = 1.5;
            else if (depth == 10) zoneMult = 2.0;
            else if (depth >= 11 && depth <= 14) zoneMult = 2.5;
            else if (depth == 15) zoneMult = 3.2;
            else if (depth >= 16 && depth <= 19) zoneMult = 3.5;
            else if (depth == 20) zoneMult = 4.5;
            else if (depth >= 21) zoneMult = 4.5 + (depth - 20) * 0.4;

            double tileMult = isLateral
                ? tile switch
                {
                    TileType.Vein => 1.65,
                    TileType.Hazard => 1.85,
                    TileType.Rock => 1.15,
                    _ => 0.65 // 이미 뚫린 통로 재탐색
                }
                : 1.0;

            return Math.Max(10, (long)Math.Round(depth * 25 * zoneMult * tileMult));
        }

        public static long GetReturnFee(int depth, long bagGold)
        {
            if (depth == 0) return 0;
            bool isJump = depth == 3 || depth == 6 || depth == 9 || depth == 10 || depth == 15 || depth == 20;
            if (isJump) return 0;
            return Math.Max(10, (long)Math.Round(bagGold * 0.10));
        }

        public static long GetNetReturn(int depth, long bagGold)
        {
            if (depth == 0) return 0;
            return Math.Max(0, bagGold - GetReturnFee(depth, bagGold));
        }
    }

    internal static class ConsoleRenderer
    {
        private const int BoxWidth = 76;

        public static void RenderTurn(GameState s, MineMap map)
        {
            Console.Clear();
            string rankTitle = MineRules.GetRankTitle(s.RankIndex);
            string zoneName = MineRules.GetZoneName(s.Depth);

            WriteTop($"심연의 광산: 다차원 채굴  |  {rankTitle}");
            WriteRow($"지상 골드: {s.Gold,9:N0} G / 목표: {s.CurrentTarget,9:N0} G | 가방(미정산): {s.BagGold,8:N0} G");
            WriteRow($"현재 위치: {zoneName} {s.Depth}층 ({MineRules.GetColLabel(s.Col)}) | 최고기록: {s.BestGold:N0} G / {s.BestDepth}층");
            WriteRow($"내구도   : {MakeBar(s.Durability, s.MaxDurability, 18)} {s.Durability,3}/{s.MaxDurability}  (층내 횡단: {s.LateralStepsInFloor}회)");
            WriteMid();

            // 실시간 2D 광산 맵 렌더링 (현재 층 기준 위 1층 ~ 아래 2층)
            RenderMineGrid(s, map);
            WriteMid();

            // 행동별 수치 요약
            int nextDepth = s.Depth + 1;
            long downCost = MineRules.GetDownCost(nextDepth);
            int downChance = MineRules.GetSuccessChance(nextDepth, false, TileType.Rock, 0);
            long downReward = MineRules.GetReward(nextDepth, false, TileType.Rock);

            if (s.Depth == 0)
            {
                WriteRow($"[하강 1층] 비용: {downCost:N0} G | 성공률: {downChance}% | 예상 보상: {downReward:N0} G");
                WriteRow("[지상 캠프] 안전 지대입니다. [3] 아래 하강으로 1층에 진입하세요.");
            }
            else
            {
                bool canLeft = s.Col > 0;
                bool canRight = s.Col < MineMap.Width - 1;
                long latCost = MineRules.GetLateralCost(s.Depth, s.LateralStepsInFloor);

                TileType leftTile = canLeft ? map.GetTile(s.Depth, s.Col - 1) : TileType.Rock;
                TileType rightTile = canRight ? map.GetTile(s.Depth, s.Col + 1) : TileType.Rock;

                string leftInfo = canLeft
                    ? $"[1]좌({SymbolOf(leftTile)}): {latCost}G/{MineRules.GetSuccessChance(s.Depth, true, leftTile, s.LateralStepsInFloor)}%/+{MineRules.GetReward(s.Depth, true, leftTile)}G"
                    : "[1]좌: 막힘";

                string rightInfo = canRight
                    ? $"[2]우({SymbolOf(rightTile)}): {latCost}G/{MineRules.GetSuccessChance(s.Depth, true, rightTile, s.LateralStepsInFloor)}%/+{MineRules.GetReward(s.Depth, true, rightTile)}G"
                    : "[2]우: 막힘";

                WriteRow($"{leftInfo} | {rightInfo}");
                WriteRow($"[3]아래({nextDepth}층): {downCost}G/{downChance}%/+{downReward}G | [4]복귀 정산: +{MineRules.GetNetReturn(s.Depth, s.BagGold):N0}G");
            }

            WriteMid();
            WriteWrapped("안내: " + s.Message);
            WriteBottom();

            Console.WriteLine();
            if (s.Depth == 0)
                Console.WriteLine("  [3] 광산 진입(1층 하강)    [0] 게임 종료");
            else
                Console.WriteLine("  [1] 좌측 굴진    [2] 우측 굴진    [3] 아래 하강    [4] 지상 복귀(정산)    [0] 종료");
        }

        private static void RenderMineGrid(GameState s, MineMap map)
        {
            WriteRow("광산 단면도 (@: 내 위치, .: 통로, #: 암반, $: 광맥, !: 위험균열)");
            int startRow = Math.Max(1, s.Depth - 1);
            int endRow = s.Depth == 0 ? 2 : s.Depth + 2;

            if (s.Depth == 0)
            {
                WriteRow("  [지상]    ● ─── 입구 (안전 지대) ─── ●");
            }

            for (int r = startRow; r <= endRow; r++)
            {
                var row = map.GetOrCreateFloor(r);
                string marker = r == s.Depth ? "▶" : " ";
                string cells = "";
                for (int c = 0; c < MineMap.Width; c++)
                {
                    char ch = (r == s.Depth && c == s.Col) ? '@' : SymbolOf(row[c]);
                    cells += $"[{ch}] ";
                }
                WriteRow($"  {marker}{r,2}층 ({MineRules.GetZoneName(r),-10}): {cells.TrimEnd()}");
            }
        }

        public static void PlayDigProcess(string actionName, int depth, int col, bool success, bool isJackpot, long earned, int dmg)
        {
            Console.WriteLine();
            string pos = $"{depth}층 · {MineRules.GetColLabel(col)}";
            string[] steps =
            {
                $"  ⛏  [{pos}] {actionName} — 암반 균열 타격 중...",
                $"  ⛏  [{pos}] {actionName} — 지층 반응 확인 중...",
                $"  ⛏  [{pos}] {actionName} — 결과 판정!"
            };

            foreach (string st in steps)
            {
                Console.Write($"\r{st,-62}");
                Thread.Sleep(220);
            }

            Console.WriteLine();
            if (success && isJackpot)
                Console.WriteLine($"  ✦ ★대박 광맥★ +{earned:N0} G 채취 성공!");
            else if (success)
                Console.WriteLine($"  ✦ 채굴 성공! +{earned:N0} G 확보!");
            else
                Console.WriteLine($"  ✖ 채굴 실패! 내구도 -{dmg} 손실!");

            Thread.Sleep(450);
        }

        private static char SymbolOf(TileType t) => t switch
        {
            TileType.Dug => '.',
            TileType.Vein => '$',
            TileType.Hazard => '!',
            _ => '#'
        };

        private static string MakeBar(int cur, int max, int len)
        {
            int filled = Math.Clamp((int)Math.Round((double)cur / max * len), 0, len);
            return "[" + new string('█', filled) + new string('░', len - filled) + "]";
        }

        private static void WriteTop(string title)
        {
            Console.WriteLine("┌" + new string('─', BoxWidth - 2) + "┐");
            WriteRow(title);
            Console.WriteLine("├" + new string('─', BoxWidth - 2) + "┤");
        }

        private static void WriteMid() => Console.WriteLine("├" + new string('─', BoxWidth - 2) + "┤");
        private static void WriteBottom() => Console.WriteLine("└" + new string('─', BoxWidth - 2) + "┘");

        private static void WriteWrapped(string text)
        {
            int inner = BoxWidth - 6;
            int idx = 0;
            bool first = true;
            while (idx < text.Length)
            {
                int take = Math.Min(first ? inner : inner - 4, text.Length - idx);
                string part = text.Substring(idx, take);
                WriteRow(first ? part : "    " + part);
                idx += take;
                first = false;
            }
        }

        private static void WriteRow(string content)
        {
            int inner = BoxWidth - 4;
            int w = DisplayWidth(content);
            int pad = Math.Max(0, inner - w);
            Console.WriteLine("│ " + content + new string(' ', pad) + " │");
        }

        private static int DisplayWidth(string s)
        {
            int w = 0;
            foreach (char c in s)
            {
                if ((c >= 0x1100 && c <= 0x9FFF) || (c >= 0xAC00 && c <= 0xD7AF) || (c >= 0x2500 && c <= 0x26FF) || c >= 0xFF00)
                    w += 2;
                else
                    w += 1;
            }
            return w;
        }
    }

    internal sealed class GameEngine
    {
        private readonly GameState _s = new GameState();
        private readonly Random _rng = new Random();
        private readonly MineMap _map;

        public GameEngine()
        {
            _map = new MineMap(_rng);
        }

        public void Run()
        {
            while (_s.Running)
            {
                if (_s.Gold > _s.BestGold) _s.BestGold = _s.Gold;

                // 확실한 게임오버 판정 (지상에 있고 가방도 비었는데 최소 생존 자금 미만일 때)
                if (_s.Depth == 0 && _s.BagGold == 0 && _s.Gold < MineRules.MinSurviveGold)
                {
                    _s.OverReason = GameOverReason.Bankrupt;
                    _s.Running = false;
                    break;
                }

                ConsoleRenderer.RenderTurn(_s, _map);
                Console.Write("\n  선택 > ");
                string input = (Console.ReadLine() ?? "").Trim();

                if (input == "0")
                {
                    _s.Running = false;
                    break;
                }

                if (_s.Depth == 0)
                {
                    if (input == "3") DigDown();
                    else _s.Message = "[안내] 지상에서는 [3] 광산 진입 또는 [0] 종료만 선택할 수 있습니다.";
                }
                else
                {
                    switch (input)
                    {
                        case "1":
                            DigLateral(-1, "좌측 굴진");
                            break;
                        case "2":
                            DigLateral(1, "우측 굴진");
                            break;
                        case "3":
                            DigDown();
                            break;
                        case "4":
                            ReturnAndSettle();
                            break;
                        default:
                            _s.Message = "[입력 오류] [1] 좌측 [2] 우측 [3] 아래 [4] 복귀 [0] 종료 중 입력하세요.";
                            break;
                    }
                }
            }

            RenderEndScreen();
        }

        private void DigLateral(int deltaCol, string label)
        {
            int targetCol = _s.Col + deltaCol;
            if (targetCol < 0 || targetCol >= MineMap.Width)
            {
                _s.Message = $"[이동 불가] 해당 층의 {(deltaCol < 0 ? "좌측" : "우측")} 끝 암반입니다. 다른 방향이나 하강을 선택하세요.";
                return;
            }

            long cost = MineRules.GetLateralCost(_s.Depth, _s.LateralStepsInFloor);
            if (_s.Gold < cost)
            {
                _s.Message = $"[골드 부족] {label} 비용({cost:N0} G)이 부족합니다. [4] 복귀로 정산하거나 다른 행동을 고르세요.";
                return;
            }

            _s.Gold -= cost;
            _s.Col = targetCol;
            _s.LateralStepsInFloor++;

            TileType tile = _map.GetTile(_s.Depth, _s.Col);
            int chance = MineRules.GetSuccessChance(_s.Depth, true, tile, _s.LateralStepsInFloor - 1);
            long baseReward = MineRules.GetReward(_s.Depth, true, tile);
            long jackpot = (long)Math.Round(baseReward * 2.5);

            ResolveDigResult(label, true, tile, chance, baseReward, jackpot);
        }

        private void DigDown()
        {
            int nextDepth = _s.Depth + 1;
            long cost = MineRules.GetDownCost(nextDepth);

            if (_s.Gold < cost)
            {
                _s.Message = $"[진입 불가] {nextDepth}층 하강 비용({cost:N0} G)이 부족합니다. [4] 복귀로 정산하세요.";
                return;
            }

            _s.Gold -= cost;
            _s.Depth = nextDepth;
            _s.Col = 3; // 새 층 중앙 진입
            _s.LateralStepsInFloor = 0;
            _map.GetOrCreateFloor(_s.Depth);

            if (_s.Depth > _s.BestDepth) _s.BestDepth = _s.Depth;

            TileType tile = _map.GetTile(_s.Depth, _s.Col);
            int chance = MineRules.GetSuccessChance(_s.Depth, false, tile, 0);
            long baseReward = MineRules.GetReward(_s.Depth, false, tile);
            long jackpot = (long)Math.Round(baseReward * 2.5);

            ResolveDigResult($"{nextDepth}층 하강", false, tile, chance, baseReward, jackpot);
        }

        private void ResolveDigResult(string label, bool isLateral, TileType tile, int chance, long baseReward, long jackpot)
        {
            string zone = MineRules.GetZoneName(_s.Depth);

            if (_rng.Next(100) < chance)
            {
                bool isJackpot = _rng.Next(100) < 10;
                long earned = isJackpot ? jackpot : baseReward;
                _s.BagGold += earned;
                _map.MarkDug(_s.Depth, _s.Col);

                ConsoleRenderer.PlayDigProcess(label, _s.Depth, _s.Col, true, isJackpot, earned, 0);
                _s.Message = isJackpot
                    ? $"[★대박 광맥★] {zone} {_s.Depth}층({MineRules.GetColLabel(_s.Col)}) +{earned:N0} G! (가방: {_s.BagGold:N0} G)"
                    : $"[채굴 성공] {zone} {_s.Depth}층({MineRules.GetColLabel(_s.Col)}) +{earned:N0} G! (가방: {_s.BagGold:N0} G)";
            }
            else
            {
                var (minDmg, maxDmg) = MineRules.GetDamageRange(_s.Depth, isLateral, tile, _s.BagGold);
                int dmg = _rng.Next(minDmg, maxDmg + 1);
                _s.Durability -= dmg;

                ConsoleRenderer.PlayDigProcess(label, _s.Depth, _s.Col, false, false, 0, dmg);

                if (_s.Durability > 0)
                {
                    _s.Message = $"[채굴 실패] 내구도 -{dmg} (남은 내구도: {_s.Durability}/{_s.MaxDurability}) | 가방: {_s.BagGold:N0} G";
                    return;
                }

                // 내구도 0 도달 -> 붕괴 판정
                _s.Durability = 0;

                // 기적의 구사일생(12%)
                if (_rng.Next(100) < 12)
                {
                    long lost = _s.BagGold / 2;
                    _s.BagGold -= lost;
                    _s.Durability = 20;
                    _s.Message = $"[구사일생!] 붕괴 직전 기적 생존! 가방 50%(-{lost:N0} G) 손실, 내구도 20으로 {_s.Depth}층에 남습니다.";
                    return;
                }

                int collapsedDepth = _s.Depth;
                _s.Depth = 0;
                _s.Col = 3;
                _s.LateralStepsInFloor = 0;
                _s.Durability = _s.MaxDurability;

                if (collapsedDepth <= 3)
                {
                    long recovered = (long)Math.Round(_s.BagGold * 0.30);
                    long fee = Math.Min(_s.Gold, 30);
                    _s.Gold = _s.Gold - fee + recovered;
                    _s.BagGold = 0;

                    string promo = ApplyPromotions();
                    _s.Message = $"[얕은 층 구조] {collapsedDepth}층 구조! 가방 30%(+{recovered:N0} G) 회수, 치료비 -{fee:N0} G 후 복귀.{promo}";

                    if (_s.Gold < MineRules.MinSurviveGold)
                    {
                        _s.OverReason = GameOverReason.Bankrupt;
                        _s.Running = false;
                    }
                }
                else
                {
                    // 심층·다차원(4층+) 붕괴: 가방 전액 소실 + 지상 골드 45% 손실
                    long lostBag = _s.BagGold;
                    long goldLoss = (long)Math.Round(_s.Gold * 0.45);
                    _s.Gold = Math.Max(0, _s.Gold - goldLoss);
                    _s.BagGold = 0;

                    _s.Message = $"[치명적 조난!] {zone} {collapsedDepth}층 붕괴! 가방(-{lostBag:N0} G) 소실 및 지상 골드 -{goldLoss:N0} G 손실!";

                    if (_s.Gold < MineRules.MinSurviveGold)
                    {
                        _s.OverReason = GameOverReason.FatalCollapse;
                        _s.Running = false;
                    }
                }
            }
        }

        private void ReturnAndSettle()
        {
            if (_s.Depth == 0)
            {
                _s.Message = "[안내] 이미 지상에 있습니다. [3] 광산 진입을 선택하세요.";
                return;
            }

            long net = MineRules.GetNetReturn(_s.Depth, _s.BagGold);
            long fee = MineRules.GetReturnFee(_s.Depth, _s.BagGold);

            _s.Gold += net;
            _s.BagGold = 0;
            _s.Depth = 0;
            _s.Col = 3;
            _s.LateralStepsInFloor = 0;
            _s.Durability = _s.MaxDurability;

            if (_s.Gold > _s.BestGold) _s.BestGold = _s.Gold;

            string promo = ApplyPromotions();
            string feeNote = fee > 0 ? $"(귀환비 -{fee:N0} G)" : "(점핑 층 귀환비 면제!)";
            _s.Message = $"[정산 완료] +{net:N0} G 입금{feeNote}! 현재 지상 골드: {_s.Gold:N0} G{promo}";
        }

        private string ApplyPromotions()
        {
            bool promoted = false;
            while (_s.Gold >= _s.CurrentTarget)
            {
                promoted = true;
                _s.RankIndex++;
                if (_s.RankIndex < MineRules.RankTargets.Length)
                    _s.CurrentTarget = MineRules.RankTargets[_s.RankIndex];
                else
                    _s.CurrentTarget *= 2;
            }

            if (!promoted) return "";
            return $" ★승급! [{MineRules.GetRankTitle(_s.RankIndex)}] (다음 목표: {_s.CurrentTarget:N0} G)";
        }

        private void RenderEndScreen()
        {
            Console.WriteLine();
            Console.WriteLine("============================================================================");
            if (_s.OverReason == GameOverReason.Bankrupt)
                Console.WriteLine("  [게임 오버 — 자금 파산] 광산 탐험을 이어갈 최소 골드와 정산 광물이 모두 소진되었습니다.");
            else if (_s.OverReason == GameOverReason.FatalCollapse)
                Console.WriteLine("  [게임 오버 — 심층/차원 조난] 치명적 붕괴로 막대한 손실을 입고 탐험 자격을 상실했습니다.");
            else
                Console.WriteLine("  [탐험 종료] 플레이어가 자발적으로 탐험을 마쳤습니다.");

            Console.WriteLine($"  최종 등급: {MineRules.GetRankTitle(_s.RankIndex)} | 지상 골드: {_s.Gold:N0} G");
            Console.WriteLine($"  최고 보유 골드: {_s.BestGold:N0} G | 최고 도달 깊이: {_s.BestDepth}층");
            Console.WriteLine("============================================================================");
        }
    }
}