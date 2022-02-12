using OthelloAI.Patterns;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OthelloAI
{
    static class Program
    {
        public static readonly Pattern PATTERN_EDGE2X = new Pattern("e_edge_x.dat", new BoardHasherMask(0b01000010_11111111UL), PatternType.X_SYMETRIC);
        public static readonly Pattern PATTERN_EDGE_BLOCK = new Pattern("e_edge_block.dat", new BoardHasherMask(0b00111100_10111101UL), PatternType.X_SYMETRIC);
        public static readonly Pattern PATTERN_CORNER_BLOCK = new Pattern("e_corner_block.dat", new BoardHasherMask(0b00000111_00000111_00000111UL), PatternType.XY_SYMETRIC);
        public static readonly Pattern PATTERN_CORNER = new Pattern("e_corner.dat", new BoardHasherMask(0b00000001_00000001_00000001_00000011_00011111UL), PatternType.XY_SYMETRIC);
        public static readonly Pattern PATTERN_LINE1 = new Pattern("e_line1.dat", new BoardHasherLine1(1), PatternType.X_SYMETRIC);
        public static readonly Pattern PATTERN_LINE2 = new Pattern("e_line2.dat", new BoardHasherLine1(2), PatternType.X_SYMETRIC);
        public static readonly Pattern PATTERN_LINE3 = new Pattern("e_line3.dat", new BoardHasherLine1(3), PatternType.X_SYMETRIC);
        public static readonly Pattern PATTERN_DIAGONAL8 = new Pattern("e_diag8.dat", new BoardHasherMask(0x8040201008040201UL), PatternType.DIAGONAL);
        public static readonly Pattern PATTERN_DIAGONAL7 = new Pattern("e_diag7.dat", new BoardHasherMask(0x1020408102040UL), PatternType.XY_SYMETRIC);
        public static readonly Pattern PATTERN_DIAGONAL6 = new Pattern("e_diag6.dat", new BoardHasherMask(0x10204081020UL), PatternType.XY_SYMETRIC);
        public static readonly Pattern PATTERN_DIAGONAL5 = new Pattern("e_diag5.dat", new BoardHasherMask(0x102040810UL), PatternType.XY_SYMETRIC);

        public static readonly Pattern[] PATTERNS = { PATTERN_EDGE2X, PATTERN_EDGE_BLOCK, PATTERN_CORNER_BLOCK, PATTERN_CORNER,
            PATTERN_LINE1, PATTERN_LINE2, PATTERN_LINE3, PATTERN_DIAGONAL8, PATTERN_DIAGONAL7, PATTERN_DIAGONAL6,
            PATTERN_DIAGONAL5 };

        static void Main()
        {
            Console.WriteLine($"Support BMI2 : {System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported}");
            Console.WriteLine($"Support AVX2 : {System.Runtime.Intrinsics.X86.Avx2.IsSupported}");
            Console.WriteLine($"Support AVX : {System.Runtime.Intrinsics.X86.Avx.IsSupported}");

            Console.WriteLine();
#if BIN_HASH
            Console.WriteLine("Pattern Hashing Type : Bin");
#elif TER_HASH
            Console.WriteLine("Pattern Hashing Type : Ter");
#else
            Console.WriteLine("Pattern Hashing Type : Undefined");
            return;
#endif
            Console.WriteLine();

            foreach (Pattern p in PATTERNS)
            {
                p.Load();
                Console.WriteLine(p);
                Console.WriteLine(p.Test());
            }

            // PATTERN_EDGE2X.Info(32, 0.9F);

            StartGame();
            // StartManualGame();
        }

        public static bool Step(ref Board board, Player player, int stone, bool print)
        {
            (_, _, ulong move) = player.DecideMove(board, stone);
            if (move != 0)
            {
                board = board.Reversed(move, stone);
                if (print)
                    Console.WriteLine(board);
                return true;
            }
            return false;
        }

        static void StartGame()
        {
            Board board;

            Evaluator evaluator = new EvaluatorPatternBased();
            PlayerAI p = new PlayerAI(evaluator)
            {
                ParamBeg = new SearchParameters(depth: 11, stage: 0, new CutoffParameters(true, true, false)),
                ParamMid = new SearchParameters(depth: 11, stage: 16, new CutoffParameters(true, true, false)),
                ParamEnd = new SearchParameters(depth: 64, stage: 44, new CutoffParameters(true, true, false)),
            };

            for (int i = 0; i < 1; i++)
            {
                board = new Board(Board.InitB, Board.InitW);

                while (Step(ref board, p, 1, true) | Step(ref board, p, -1, true))
                {
                }

                Console.WriteLine($"B: {board.GetStoneCount(1)}");
                Console.WriteLine($"W: {board.GetStoneCount(-1)}");
                Console.WriteLine(i);
            }

            Console.WriteLine($"Average : {p.Times.Average()}");
            Console.WriteLine($"Min : {p.Times.Min()}");
            Console.WriteLine($"Max : {p.Times.Max()}");
            Console.WriteLine();
            Console.WriteLine(string.Join("\r\n", p.Times));
            Console.WriteLine();
        }

        static void StartManualGame()
        {
            Evaluator evaluator = new EvaluatorPatternBased();
            Player p1 = new PlayerAI(evaluator)
            {
                ParamBeg = new SearchParameters(depth: 11, stage: 0, new CutoffParameters(true, true, false)),
                ParamMid = new SearchParameters(depth: 11, stage: 16, new CutoffParameters(true, true, false)),
                ParamEnd = new SearchParameters(depth: 64, stage: 40, new CutoffParameters(true, true, false)),
            };

            Player p2 = new PlayerManual();

            Board board = new Board();

            for (int i = 0; i < 1; i++)
            {
                board = new Board(Board.InitB, Board.InitW, 4);

                while (Step(ref board, p1, 1, true) | Step(ref board, p2, -1, true))
                {
                }
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"B: {board.GetStoneCount(1)}");
            Console.WriteLine($"W: {board.GetStoneCount(-1)}");
        }
    }
}
