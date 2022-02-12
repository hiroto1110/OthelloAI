using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Diagnostics;

namespace OthelloAI
{
    static class ReverseUtil
    {
        public static ulong ReverseAvx(ulong move, ulong p, ulong o)
        {
            Vector256<ulong> PP, mask, reversed, flip_l, flip_r, flags;
            Vector128<ulong> reversed128;
            Vector256<ulong> offset = Vector256.Create(7UL, 9UL, 8UL, 1UL);
            Vector256<ulong> move_v = Vector256.Create(move);

            PP = Vector256.Create(p);
            mask = Avx2.And(Vector256.Create(o), Vector256.Create(0x7e7e7e7e7e7e7e7eUL, 0x7e7e7e7e7e7e7e7eUL, 0xffffffffffffffffUL, 0x7e7e7e7e7e7e7e7eUL));

            flip_l = Avx2.And(mask, Avx2.ShiftLeftLogicalVariable(move_v, offset));
            flip_l = Avx2.Or(flip_l, Avx2.And(mask, Avx2.ShiftLeftLogicalVariable(flip_l, offset)));
            flip_l = Avx2.Or(flip_l, Avx2.And(mask, Avx2.ShiftLeftLogicalVariable(flip_l, offset)));
            flip_l = Avx2.Or(flip_l, Avx2.And(mask, Avx2.ShiftLeftLogicalVariable(flip_l, offset)));
            flip_l = Avx2.Or(flip_l, Avx2.And(mask, Avx2.ShiftLeftLogicalVariable(flip_l, offset)));
            flip_l = Avx2.Or(flip_l, Avx2.And(mask, Avx2.ShiftLeftLogicalVariable(flip_l, offset)));

            flags = Avx2.And(PP, Avx2.ShiftLeftLogicalVariable(flip_l, offset));
            flip_l = Avx2.And(flip_l, Avx2.Xor(Vector256.Create(0xffffffffffffffffUL), Avx2.CompareEqual(flags, Vector256.Create(0UL))));

            flip_r = Avx2.And(mask, Avx2.ShiftRightLogicalVariable(move_v, offset));
            flip_r = Avx2.Or(flip_r, Avx2.And(mask, Avx2.ShiftRightLogicalVariable(flip_r, offset)));
            flip_r = Avx2.Or(flip_r, Avx2.And(mask, Avx2.ShiftRightLogicalVariable(flip_r, offset)));
            flip_r = Avx2.Or(flip_r, Avx2.And(mask, Avx2.ShiftRightLogicalVariable(flip_r, offset)));
            flip_r = Avx2.Or(flip_r, Avx2.And(mask, Avx2.ShiftRightLogicalVariable(flip_r, offset)));
            flip_r = Avx2.Or(flip_r, Avx2.And(mask, Avx2.ShiftRightLogicalVariable(flip_r, offset)));

            flags = Avx2.And(PP, Avx2.ShiftRightLogicalVariable(flip_r, offset));
            flip_r = Avx2.And(flip_r, Avx2.Xor(Vector256.Create(0xffffffffffffffffUL), Avx2.CompareEqual(flags, Vector256.Create(0UL))));

            reversed = Avx2.Or(flip_l, flip_r);

            reversed128 = Sse2.Or(Avx2.ExtractVector128(reversed, 0), Avx2.ExtractVector128(reversed, 1));
            reversed128 = Sse2.Or(reversed128, Sse2.UnpackHigh(reversed128, reversed128));
            return reversed128.ToScalar();
        }

        public static ulong Reverse(ulong move, ulong player, ulong opponent)
        {
            ulong verticalMask = opponent & 0x7e7e7e7e7e7e7e7eUL;

            ulong reversed = 0;
            reversed |= GetReversedL(move, player, 8, opponent);
            reversed |= GetReversedR(move, player, 8, opponent);
            reversed |= GetReversedL(move, player, 1, verticalMask);
            reversed |= GetReversedR(move, player, 1, verticalMask);
            reversed |= GetReversedL(move, player, 9, verticalMask);
            reversed |= GetReversedR(move, player, 9, verticalMask);
            reversed |= GetReversedL(move, player, 7, verticalMask);
            reversed |= GetReversedR(move, player, 7, verticalMask);

            return reversed;
        }

        private static ulong GetReversedL(ulong move, ulong player, int offset, ulong mask)
        {
            ulong r = (move << offset) & mask;
            r |= (r << offset) & mask;
            r |= (r << offset) & mask;
            r |= (r << offset) & mask;
            r |= (r << offset) & mask;
            r |= (r << offset) & mask;

            if (((r << offset) & player) != 0)
            {
                return r;
            }

            return 0;
        }

        private static ulong GetReversedR(ulong move, ulong player, int offset, ulong mask)
        {
            ulong r = (move >> offset) & mask;
            r |= (r >> offset) & mask;
            r |= (r >> offset) & mask;
            r |= (r >> offset) & mask;
            r |= (r >> offset) & mask;
            r |= (r >> offset) & mask;

            if (((r >> offset) & player) != 0)
            {
                return r;
            }

            return 0;
        }
    }
}
