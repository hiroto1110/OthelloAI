using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OthelloAI
{
    class MoveComparerNMoves : IComparer<Move>
    {
        public int Compare([AllowNull] Move x, [AllowNull] Move y)
        {
			return y.n_moves - x.n_moves;
        }
    }

    class MoveTupleComparer<T> : IComparer<(T, Move)>
    {
		IComparer<Move> Comparer { get; }

		public MoveTupleComparer(IComparer<Move> comparer)
        {
			Comparer = comparer;
		}

		public int Compare([AllowNull] (T, Move) x, [AllowNull] (T, Move) y)
        {
            return Comparer.Compare(x.Item2, y.Item2);
        }
    }

    public readonly struct Move : IComparable<Move>
	{
		public static readonly IComparer<Move> comparer = new MoveComparerNMoves();

		public readonly Board reversed;
		public readonly ulong moves;
		public readonly int n_moves;
		
		public Move(Board board, ulong move)
        {
			reversed = board.Reversed(move);
			moves = reversed.GetMoves();
			n_moves = Board.BitCount(moves);
        }

		public Move(Board reversed)
		{
			this.reversed = reversed;
			moves = reversed.GetMoves();
			n_moves = Board.BitCount(moves);
		}

		public Move(Board reversed, ulong moves, int count)
		{
			this.reversed = reversed;
			this.moves = moves;
			this.n_moves = count;
		}

		public Move[] NextMoves()
		{
			ulong moves_tmp = moves;

			Move[] array = new Move[n_moves];
			for (int i = 0; i < array.Length; i++)
			{
				ulong move = Board.NextMove(moves_tmp);
				moves_tmp = Board.RemoveMove(moves_tmp, move);
				array[i] = new Move(reversed, move);
			}
			return array;
		}

		public (ulong, Move)[] NextMovesKeepingPrevMove()
		{
			ulong moves_tmp = moves;

			(ulong, Move)[] array = new (ulong, Move)[n_moves];
			for (int i = 0; i < array.Length; i++)
			{
				ulong move = Board.NextMove(moves_tmp);
				moves_tmp = Board.RemoveMove(moves_tmp, move);
				array[i] = (move, new Move(reversed, move));
			}
			return array;
		}

		public Move[] OrderedNextMoves()
        {
			Move[] moves = NextMoves();
			Array.Sort(moves);
			return moves;
        }

		public int CompareTo([AllowNull] Move other)
        {
			return n_moves - other.n_moves;
        }
    }
}
