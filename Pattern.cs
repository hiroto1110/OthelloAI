using System;
using System.IO;

namespace OthelloAI.Patterns
{
    public enum PatternType
    {
        X_SYMETRIC,
        XY_SYMETRIC,
        DIAGONAL,
    }

    public class Boards
    {
        public Board Original { get; }
        public Board Transposed { get; }
        public Board HorizontalMirrored { get; }
        public Board Rotated90 { get; }
        public Board Rotated270 { get; }

        public Boards(Board source)
        {
            Original = source;
            Transposed = source.Transposed();
            HorizontalMirrored = source.HorizontalMirrored();
            Rotated270 = HorizontalMirrored.Transposed();
            Rotated90 = Transposed.HorizontalMirrored();
        }
    }

    public class Pattern
    {
        public const int STAGES = 60;
        public const string EVAL_DIR = "eval/";

        protected string FilePath { get; }

        protected PatternType Type { get; }
        public BoardHasher Hasher { get; }

        public int ArrayLength { get; }
        public int NumOfStates { get; }

        protected int[][] StageBasedGameCount { get; } = new int[STAGES][];
        protected int[][] StageBasedWinCount { get; } = new int[STAGES][];
        protected byte[][] StageBasedEvaluationsB { get; } = new byte[STAGES][];

        public Pattern(string filePath, BoardHasher hasher, PatternType type)
        {
            FilePath = EVAL_DIR + filePath;
            Hasher = hasher;
            Type = type;

            NumOfStates = (int)Math.Pow(3, Hasher.HashLength);

#if BIN_HASH
            ArrayLength = (int)Math.Pow(2, 2 * Hasher.HashLength);
#else
            ArrayLength = NumOfStates;
#endif

            for (int i = 0; i < STAGES; i++)
            {
                StageBasedGameCount[i] = new int[ArrayLength];
                StageBasedWinCount[i] = new int[ArrayLength];
                StageBasedEvaluationsB[i] = new byte[ArrayLength];
            }
        }

        protected int GetStage(Board board)
        {
            return board.n_stone - 5;
        }

        public int EvalByPEXTHashing(Boards b)
        {
            byte[] e = StageBasedEvaluationsB[GetStage(b.Original)];
            byte _Eval(in Board borad) => e[Hasher.HashByPEXT(borad)];

            return Type switch
            {
                PatternType.X_SYMETRIC => _Eval(b.Original) + _Eval(b.HorizontalMirrored) + _Eval(b.Transposed) + _Eval(b.Rotated90) - 128 * 4,
                PatternType.XY_SYMETRIC => _Eval(b.Original) + _Eval(b.HorizontalMirrored) + _Eval(b.Transposed) + _Eval(b.Rotated270) - 128 * 4,
                PatternType.DIAGONAL => _Eval(b.Original) + _Eval(b.HorizontalMirrored) - 128 * 2,
                _ => throw new NotImplementedException(),
            };
        }

        protected int ConvertStateToHash(int i) => Hasher.ConvertStateToHash(i);

        protected Board FromHash(int hash) => Hasher.FromHash(hash);

        public void Load()
        {
            using var reader = new BinaryReader(new FileStream(FilePath, FileMode.Open));

            static byte ConvertToInt8(float e)
            {
                return (byte)Math.Clamp(128 + (e - 0.5F) * 255, 0, 255);
            }

            for (int stage = 0; stage < STAGES; stage++)
            {
                for (int i = 0; i < NumOfStates; i++)
                {
                    int game = reader.ReadInt32();
                    int win = reader.ReadInt32();

                    float e = game > 10 ? (float)win / game : 0.5F;

                    int index = ConvertStateToHash(i);
                    StageBasedGameCount[stage][index] = game;
                    StageBasedWinCount[stage][index] = win;
                    StageBasedEvaluationsB[stage][index] = ConvertToInt8(e);
                }
            }
        }

        public void Save()
        {
            using var writer = new BinaryWriter(new FileStream(FilePath, FileMode.Create));

            for (int stage = 0; stage < STAGES; stage++)
            {
                for (int i = 0; i < NumOfStates; i++)
                {
                    int index = ConvertStateToHash(i);
                    writer.Write(StageBasedGameCount[stage][index]);
                    writer.Write(StageBasedWinCount[stage][index]);
                }
            }
        }

        public bool Test()
        {
            for (int i = 0; i < NumOfStates; i++)
            {
                int hash = ConvertStateToHash(i);

                if (Hasher.HashByPEXT(FromHash(hash)) != hash)
                {
                    Console.WriteLine(FromHash(hash));
                    Console.WriteLine($"{hash}, {Hasher.HashByPEXT(FromHash(hash))}");
                    Console.WriteLine($"{hash}, {(hash >> Hasher.HashLength)}, {(hash & ((1 << Hasher.HashLength) - 1)) << Hasher.HashLength}");
                    return false;
                }
            }
            return true;
        }

        public void TestRotation()
        {
            var br = new Board(0b10101010_01010101_10101010_01010101_10101010_01010101_10101010_01010101UL,
                    0b01010101_10101010_01010101_10101010_01010101_10101010_01010101_10101010UL);
            var b = Hasher.FromTerHash(Hasher.HashTerByPEXT(br));
            var bs = new Boards(b);

            Console.WriteLine(bs.Original);
            Console.WriteLine(bs.HorizontalMirrored);
            Console.WriteLine(bs.Transposed);
            Console.WriteLine(bs.Rotated90);
            Console.WriteLine(bs.Rotated270);
        }

        public void Info(int stage, float threshold)
        {
            for (int i = 0; i < NumOfStates; i++)
            {
                int index = ConvertStateToHash(i);

                if (StageBasedEvaluationsB[stage][index] > threshold)
                    InfoHash(stage, index);
            }
        }

        public void InfoHash(int stage, int hash)
        {
            Console.WriteLine(FromHash(hash));
            Console.WriteLine($"Stage : {stage}, Hash : {hash}");
            Console.WriteLine($"Eval : {StageBasedEvaluationsB[stage][hash]}");
            Console.WriteLine();
        }
    }
}
