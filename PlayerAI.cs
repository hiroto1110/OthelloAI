using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OthelloAI
{
    public class Search
    {
        public IDictionary<Board, (int, int)> Table { get; set; } = new Dictionary<Board, (int, int)>();
        public virtual bool IsCanceled { get; set; }

        public virtual Move[] OrderMoves(Move[] moves, int depth)
        {
            Array.Sort(moves);
            return moves;
        }

        public virtual IComparer<Move> GetComparer(int depth) => Move.comparer;

        public void StoreTranspositionTable(Move move, int alpha, int beta, int lower, int upper, int value)
        {
            if (value <= alpha)
                Table[move.reversed] = (lower, value);
            else if (value >= beta)
                Table[move.reversed] = (value, upper);
            else
                Table[move.reversed] = (value, value);
        }

        public bool TryTranspositionCutoff(Move move, CutoffParameters param, int depth, ref int alpha, ref int beta, out int lower, out int upper, ref int value)
        {
            if (depth <= PlayerAI.transposition || move.reversed.n_stone > PlayerAI.ordering_depth || !param.shouldTranspositionCut || !Table.ContainsKey(move.reversed))
            {
                lower = -1000000;
                upper = 1000000;
                return false;
            }

            (lower, upper) = Table[move.reversed];

            if (lower >= beta)
            {
                value = lower;
                return true;
            }

            if (upper <= alpha || upper == lower)
            {
                value = upper;
                return true;
            }

            alpha = Math.Max(alpha, lower);
            beta = Math.Min(beta, upper);

            return false;
        }
    }

    public class SearchIterativeDeepening : Search
    {
        public IComparer<Move> Comparer { get; }

        public int DepthInterval { get; }
        public IDictionary<Board, (int, int)> TablePrev { get; }

        public SearchIterativeDeepening(IDictionary<Board, (int, int)> prev, int depthPrev)
        {
            TablePrev = prev;
            DepthInterval = depthPrev;
            Comparer = new MoveComparer(prev);
        }

        public override IComparer<Move> GetComparer(int depth) => depth >= 4 ? Comparer : base.GetComparer(depth);

        public override Move[] OrderMoves(Move[] moves, int depth)
        {
            if(depth >= 4)
                Array.Sort(moves, Comparer);
            else
                Array.Sort(moves);
            return moves;
        }

        class MoveComparer : IComparer<Move>
        {
            const int INTERVAL = 200;

            IDictionary<Board, (int, int)> Dict { get; }

            public MoveComparer(IDictionary<Board, (int, int)> dict)
            {
                Dict = dict;
            }

            public int Eval(Move move)
            {
                if (Dict.TryGetValue(move.reversed, out (int min, int max) t))
                {
                    if (-PlayerAI.INF < t.min && t.max < PlayerAI.INF)
                        return (t.min + t.max) / 2;
                    else if (-PlayerAI.INF < t.min)
                        return t.min / 2 + INTERVAL;
                    else if (PlayerAI.INF > t.max)
                        return t.max / 2 - INTERVAL;
                }
                return PlayerAI.INF + move.n_moves;
            }

            public int Compare([AllowNull] Move x, [AllowNull] Move y)
            {
                return Eval(x) - Eval(y);
            }
        }
    }

    public readonly struct CutoffParameters
    {
        public readonly bool shouldTranspositionCut;
        public readonly bool shouldStoreTranspositionTable;
        public readonly bool shouldProbCut;

        public CutoffParameters(bool transposition, bool storeTransposition, bool probcut)
        {
            shouldTranspositionCut = transposition;
            shouldStoreTranspositionTable = storeTransposition;
            shouldProbCut = probcut;
        }
    }

    public class SearchParameters
    {
        public readonly int depth;
        public readonly int stage;
        public readonly CutoffParameters cutoff_param;

        public SearchParameters(int depth, int stage, CutoffParameters cutoff_param)
        {
            this.depth = depth;
            this.stage = stage;
            this.cutoff_param = cutoff_param;
        }
    }

    public class PlayerAI : Player
    {
        public const int INF = 1000000;

        public SearchParameters ParamBeg { get; set; }
        public SearchParameters ParamMid { get; set; }
        public SearchParameters ParamEnd { get; set; }

        public Evaluator Evaluator { get; set; }

        public List<float> Times { get; } = new List<float>();
        public long SearchedNodeCount { get; set; }
        public bool PrintInfo { get; set; } = true;

        public PlayerAI(Evaluator evaluator)
        {
            Evaluator = evaluator;
        }

        protected int EvalFinishedGame(Board board)
        {
            SearchedNodeCount++;
            return board.GetStoneCountGap() * 10000;
        }

        public int Eval(Board board)
        {
            SearchedNodeCount++;
            return Evaluator.Eval(board);
        }

        public override (int x, int y, ulong move) DecideMove(Board board, int stone)
        {
            SearchedNodeCount = 0;

            Search search = new Search();

            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (stone == -1)
                board = board.ColorFliped();

            ulong result;
            if (board.n_stone < ParamMid.stage)
            {
                result = SolveIterativeDeepening(board, ParamBeg.cutoff_param, ParamBeg.depth, 2, 3);
            }
            else if (board.n_stone < ParamEnd.stage)
            {
                result = SolveIterativeDeepening(board, ParamMid.cutoff_param, ParamMid.depth, 2, 3);
            }
            else 
            {
                (result, _) = SolveRoot(search, board, ParamEnd.cutoff_param, 64);
            }

            sw.Stop();

            if (result != 0)
            {
                float time = 1000F * sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency;
                Times.Add(time);

                if (PrintInfo)
                {
                    Console.WriteLine($"Taken Time : {time} ms");
                    Console.WriteLine($"Visited Nodes : {SearchedNodeCount}");
                }
            }

            (int x, int y) = Board.ToPos(result);

            return (x, y, result);
        }

        public ulong SolveIterativeDeepening(Board board, CutoffParameters param, int depth, int interval, int times)
        {
            var search = new Search();
            int d = depth - interval * (times - 1);

            while (true)
            {
                if (PrintInfo)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Depth {d}");
                }

                (ulong move, _) = SolveRoot(search, board, param, d);

                if (d >= depth)
                    return move;

                search = new SearchIterativeDeepening(search.Table, interval);
                d += interval;
            }
        }

        public (ulong, int) SolveRoot(Search search, Board board, CutoffParameters param, int depth)
        {
            Move root = new Move(board);

            if (root.n_moves <= 1)
            {
                return (root.moves, 0);
            }

            (ulong prev, Move move)[] array = root.NextMovesKeepingPrevMove();
            Array.Sort(array, new MoveTupleComparer<ulong>(search.GetComparer(depth)));

            ulong result = array[0].prev;
            int max = -Solve(search, array[0].move, param, depth - 1, -1000000, 1000000);
            int alpha = max;

            if (PrintInfo)
                Console.WriteLine($"{Board.ToPos(result)} : {max}");

            for (int i = 1; i < array.Length; i++)
            {
                (ulong prev, Move move) = array[i];

                int eval = -Solve(search, move, param, depth - 1, -alpha - 1, -alpha);

                if (alpha < eval)
                {
                    alpha = eval;
                    eval = -Solve(search, move, param, depth - 1, -1000000, -alpha);
                    alpha = Math.Max(alpha, eval);

                    if (PrintInfo)
                        Console.WriteLine($"{Board.ToPos(prev)} : {eval}");
                }
                else if (PrintInfo)
                {
                    Console.WriteLine($"{Board.ToPos(prev)} : Pruned");
                }

                if (max < eval)
                {
                    max = eval;
                    result = prev;
                }
            }
            return (result, max);
        }

        public int NullWindowSearch(Search search, Move move, CutoffParameters param, int depth, int border)
        {
            return -Solve(search, move, param, depth - 1, -border - 1, -border);
        }

        public int Negascout(Search search, Board board, ulong moves, CutoffParameters param, int depth, int alpha, int beta)
        {
            ulong move = Board.NextMove(moves);
            moves = Board.RemoveMove(moves, move);
            int max = -Solve(search, new Move(board, move), param, depth - 1, -beta, -alpha);

            if (beta <= max)
                return max;

            alpha = Math.Max(alpha, max);

            while (Board.NextMove(ref moves, out move))
            {
                Move m = new Move(board, move);

                int eval = NullWindowSearch(search, m, param, depth, alpha);

                if (beta <= eval)
                    return eval;

                if (alpha < eval)
                {
                    alpha = eval;
                    eval = -Solve(search, m, param, depth - 1, -beta, -alpha);

                    if (beta <= eval)
                    {
                        return eval;
                    }

                    alpha = Math.Max(alpha, eval);
                }
                max = Math.Max(max, eval);
            }
            return max;
        }

        public int Negascout(Search search, Move[] moves, CutoffParameters param, int depth, int alpha, int beta)
        {
            int max = -Solve(search, moves[0], param, depth - 1, -beta, -alpha);

            if (beta <= max)
                return max;

            alpha = Math.Max(alpha, max);

            foreach (Move move in moves.AsSpan(1, moves.Length - 1))
            {
                int eval = NullWindowSearch(search, move, param, depth, alpha);

                if (beta <= eval)
                    return eval;

                if (alpha < eval)
                {
                    alpha = eval;
                    eval = -Solve(search, move, param, depth - 1, -beta, -alpha);

                    if (beta <= eval)
                    {
                        return eval;
                    }

                    alpha = Math.Max(alpha, eval);
                }
                max = Math.Max(max, eval);
            }
            return max;
        }

        public int Negamax(Search search, Move[] moves, CutoffParameters param, int depth, int alpha, int beta)
        {
            int max = -1000000;

            for (int i = 0; i < moves.Length; i++)
            {
                int e = -Solve(search, moves[i], param, depth - 1, -beta, -alpha);
                max = Math.Max(max, e);
                alpha = Math.Max(alpha, e);

                if (alpha >= beta)
                    return max;
            }
            return max;
        }

        public int Negamax(Search search, Board board, ulong moves, CutoffParameters param, int depth, int alpha, int beta)
        {
            int max = -1000000;
            while (Board.NextMove(ref moves, out ulong move))
            {
                int e = -Solve(search, new Move(board, move), param, depth - 1, -beta, -alpha);
                max = Math.Max(max, e);
                alpha = Math.Max(alpha, e);

                if (alpha >= beta)
                    return max;
            }
            return max;
        }

        public static int ordering_depth = 57;
        public static int transposition = 1;

        public virtual int Solve(Search search, Move move, CutoffParameters param, int depth, int alpha, int beta)
        {
            if (search.IsCanceled)
                return -1000000;

            if (depth <= 0)
                return Eval(move.reversed);

            int value = 0;

            if (move.moves == 0)
            {
                ulong opponentMoves = move.reversed.GetOpponentMoves();
                if (opponentMoves == 0)
                {
                    return EvalFinishedGame(move.reversed);
                }
                else
                {
                    Move next = new Move(move.reversed.ColorFliped(), opponentMoves, Board.BitCount(opponentMoves));
                    return -Solve(search, next, param, depth, -beta, -alpha);
                }
            }

            if (move.reversed.n_stone == 63 && move.n_moves == 1)
                return -EvalFinishedGame(move.reversed.Reversed(move.moves));

            if (search.TryTranspositionCutoff(move, param, depth, ref alpha, ref beta, out int lower, out int upper, ref value))
                return value;

            if (depth >= 3 && move.reversed.n_stone < 60)
            {
                if (move.n_moves > 3)
                    value = Negascout(search, search.OrderMoves(move.NextMoves(), depth), param, depth, alpha, beta);
                else
                    value = Negascout(search, move.reversed, move.moves, param, depth, alpha, beta);
            }
            else
            {
                value = Negamax(search, move.reversed, move.moves, param, depth, alpha, beta);
            }

            if (depth > transposition && move.reversed.n_stone <= ordering_depth && param.shouldStoreTranspositionTable)
                search.StoreTranspositionTable(move, alpha, beta, lower, upper, value);

            return value;
        }
    }
}
