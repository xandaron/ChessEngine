
namespace ChessEngine
{
    public class BoardController
    {
        ulong pieceBoard = 0;
        ulong whiteMask = 0;
        ulong blackMask = 0;

        // Keeps track of the different piece types for both black and white.
        ulong kingMask = 0;
        ulong queenMask = 0;
        ulong rookMask = 0;
        ulong bishopMask = 0;
        ulong knightMask = 0;
        ulong pawnMask = 0;

        int turn = 0;
        int move = 1;
        int halfMoveTimer = 0;

        ulong enPassant = 0;
        ulong castle = 0;

        public BoardController(string fen)
        {
            LoadFen(fen);
        }

        public List<string> GetLegalMoves()
        {
            ulong pieces = pieceBoard;
            if (turn % 2 == 0)
            {
                pieces &= whiteMask;
            }
            else
            {
                pieces &= blackMask;
            }

            List<string> legalMoves = new();
            ulong oPieces = pieceBoard & ~pieces;
            ulong oAttacks = GetAttacks(oPieces);
            ulong king = pieces & kingMask;
            ulong checks = GetAttacked(king) & ~pieces;
            int numberOfChecks = (int)ulong.PopCount(checks);
            ulong moves;

            // Legal king moves
            ulong temp = checks;
            while (temp != 0)
            {
                ulong check = (ulong)1 << (int)ulong.TrailingZeroCount(temp);
                oAttacks |= AttackLine(king, check);
                temp &= ~check;
            }
            moves = GetAttacks(king) & ~(pieces | oAttacks);
            while (moves > 0)
            {
                ulong move = (ulong)1 << (int)ulong.TrailingZeroCount(moves);
                legalMoves.Add(BinaryToString(king) + BinaryToString(move));
                moves &= ~move;
            }

            // If checked once then block/capture the checking piece
            if (numberOfChecks == 1)
            {
                ulong stopCheck = checks | (AttackLine(king, checks) & GetAttacks(checks) & QueenAttacks(pieces & kingMask));
                ulong checkPieces = pieces & ~PinnedPieces(pieces) & ~king;
                while (checkPieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(checkPieces);
                    moves = (stopCheck & (GetMoves(piece))) | (PawnAttacks(piece & pawnMask) & checks);
                    while (moves != 0)
                    {
                        ulong move = (ulong)1 << (int)ulong.TrailingZeroCount(moves);
                        legalMoves.Add(BinaryToString(piece) + BinaryToString(move));
                        moves &= ~move;
                    }
                    checkPieces &= ~piece;
                }
            }

            // If not in check then add other legal piece moves
            // Bugs:
            // * pinned pieces that can capture their pinning piece not accounted for
            // * no castling check
            // * allows illegal en passant
            if (numberOfChecks == 0)
            {
                ulong pinnedPieces = PinnedPieces(pieces);
                ulong checkPieces = pieces & ~pinnedPieces & ~king;
                while (checkPieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(checkPieces);
                    moves = (GetMoves(piece) & ~pieces) | (PawnAttacks(piece & pawnMask) & (oPieces | enPassant));
                    while (moves != 0)
                    {
                        ulong move = (ulong)1 << (int)ulong.TrailingZeroCount(moves);
                        legalMoves.Add(BinaryToString(piece) + BinaryToString(move));
                        moves &= ~move;
                    }
                    checkPieces &= ~piece;
                }
            }

            return legalMoves;
        }

        public void Move(string move)
        {
            ulong piece = StringToBinary(move[..2]);
            ulong target = StringToBinary(move[2..]);
            ref ulong teamMask = ref whiteMask;
            ref ulong oTeamMask = ref blackMask;

            if ((piece & teamMask) == 0)
            {
                teamMask = ref blackMask;
                oTeamMask = ref whiteMask;
            }

            halfMoveTimer++;

            teamMask &= ~piece;
            teamMask |= target;
            pieceBoard &= ~piece;
            pieceBoard |= target;

            if ((oTeamMask & target) != 0)
            {
                oTeamMask &= ~target;
                kingMask &= ~target;
                queenMask &= ~target;
                rookMask &= ~target;
                bishopMask &= ~target;
                knightMask &= ~target;
                pawnMask &= ~target;
                halfMoveTimer = 0;
            }

            enPassant = 0;

            if ((piece & kingMask) != 0)
            {
                kingMask &= ~piece;
                kingMask |= target;
            }
            else if ((piece & queenMask) != 0)
            {
                queenMask &= ~piece;
                queenMask |= target;
            }
            else if ((piece & rookMask) != 0)
            {
                rookMask &= ~piece;
                rookMask |= target;
            }
            else if((piece & bishopMask) != 0)
            {
                bishopMask &= ~piece;
                bishopMask |= target;
            }
            else if((piece & knightMask) != 0)
            {
                knightMask &= ~piece;
                knightMask |= target;
            }
            else if((piece & pawnMask) != 0)
            {
                pawnMask &= ~piece;
                pawnMask |= target;
                halfMoveTimer = 0;
                if ((piece << 16) == target)
                {
                    enPassant = piece << 8;
                }
                else if ((piece >> 16) == target)
                {
                    enPassant = piece >> 8;
                }
            }

            turn++;
            move += turn % 2 == 0 ? 1 : 0;
        }

        private ulong PinnedPieces(ulong pieces)
        {
            ulong pinnedPieces = 0;
            while (pieces != 0)
            {
                ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(pieces);
                pinnedPieces |= Pinned(piece);
                pieces &= ~piece;
            }
            return pinnedPieces;
        }

        private ulong Pinned(ulong piece)
        {
            ulong teamMask = 0;
            if ((piece & whiteMask) != 0)
            {
                teamMask = whiteMask;
            }
            else if ((piece & blackMask) != 0)
            {
                teamMask = blackMask;
            }
            ulong oPieces = pieceBoard & ~teamMask;
            ulong pieceLines = QueenAttacks(piece);
            ulong king = kingMask & teamMask;

            if ((pieceLines & king) == 0)
            {
                return 0;
            }

            int pieceX = (int)ulong.TrailingZeroCount(piece) % 8;
            int pieceY = (int)ulong.TrailingZeroCount(piece) / 8;
            int kingX = (int)ulong.TrailingZeroCount(king) % 8;
            int kingY = (int)ulong.TrailingZeroCount(king) / 8;

            ulong line = AttackLine(king, piece);
            ulong checkPieces;

            if (kingX == pieceX || kingY == pieceY)
            {
                checkPieces = oPieces & (queenMask | rookMask);
            }
            else
            {
                checkPieces = oPieces & (queenMask | bishopMask);
            }

            if ((line & pieceLines & checkPieces) == 0)
            {
                return 0;
            }

            return piece;
        }

        private static ulong AttackLine(ulong king, ulong piece)
        {
            int pieceX = (int)ulong.TrailingZeroCount(piece) % 8;
            int pieceY = (int)ulong.TrailingZeroCount(piece) / 8;
            int kingX = (int)ulong.TrailingZeroCount(king) % 8;
            int kingY = (int)ulong.TrailingZeroCount(king) / 8;

            ulong line = 0;

            if (kingX == pieceX)
            {
                line = (ulong)0x0101010101010101 << kingX;
            }
            else if (kingY == pieceY)
            {
                line = (ulong)0x00000000000000FF << (kingY * 8);
            }
            else
            {
                int m = (pieceY - kingY) / (pieceX - kingX);
                int c = pieceY - m * pieceX;
                for (int i = 0; i < 8; i++)
                {
                    int y = m * i + c;
                    if (y >= 0 && y < 8)
                    {
                        line |= (ulong)1 << i + (8 * y);
                    }
                }
            }
            return line;
        }

        private ulong GetAttacked(ulong tile)
        {
            ulong attacks = 0;
            ulong pieces = pieceBoard;

            while (pieces != 0)
            {
                ulong check = (ulong)1 << (int)ulong.TrailingZeroCount(pieces);
                if ((GetAttacks(check) & tile) != 0)
                {
                    attacks |= check;
                }
                pieces &= ~check;
            }

            return attacks;
        }

        private ulong GetMoves(ulong pieces)
        {
            ulong attacks = 0;
            attacks |= PawnPushes(pieces & pawnMask);
            attacks |= KnightAttacks(pieces & knightMask);
            attacks |= BishopAttacks(pieces & bishopMask);
            attacks |= RookAttacks(pieces & rookMask);
            attacks |= QueenAttacks(pieces & queenMask);
            attacks |= KingAttacks(pieces & kingMask);
            return attacks;
        }

        private ulong GetAttacks(ulong pieces)
        {
            ulong attacks = 0;
            ulong pawns = pieces & pawnMask;
            ulong knights = pieces & knightMask;
            ulong bishops = pieces & bishopMask;
            ulong rooks = pieces & rookMask;
            ulong queen = pieces & queenMask;
            ulong king = pieces & kingMask;

            if (pawns != 0)
            {
                attacks |= PawnAttacks(pawns);
            }
            if (knights != 0)
            {
                attacks |= KnightAttacks(knights);
            }
            if (bishops != 0)
            {
                attacks |= BishopAttacks(bishops);
            }
            if (rooks != 0)
            {
                attacks |= RookAttacks(rooks);
            }
            if (queen != 0)
            {
                attacks |= QueenAttacks(queen);
            }
            if (king != 0)
            {
                attacks |= KingAttacks(king);
            }
            
            return attacks;
        }

        private ulong PawnPushes(ulong pieces)
        {
            ulong moves = 0;
            if ((pieces & whiteMask) != 0)
            {
                moves |= (pieces << 8) & ~pieceBoard;
                moves |= ((moves & 0x0000000000FF0000) << 8) & ~pieceBoard;
            }
            else if ((pieces & blackMask) != 0)
            {
                moves |= (pieces >> 8) & ~pieceBoard;
                moves |= ((moves & 0x0000FF0000000000) >> 8) & ~pieceBoard;
            }
            return moves;
        }

        private ulong PawnAttacks(ulong pawns)
        {
            ulong attacks = 0;
            // White pawns
            attacks |= (pawns & whiteMask & 0x007F7F7F7F7F7F7F) << 9;
            attacks |= (pawns & whiteMask & 0x00FEFEFEFEFEFEFE) << 7;
            // Black pawns
            attacks |= (pawns & blackMask & 0xFEFEFEFEFEFEFE00) >> 9;
            attacks |= (pawns & blackMask & 0xF7F7F7F7F7F7F700) >> 7;
            return attacks;
        }

        // Source: https://www.chessprogramming.org/Knight_Pattern
        private static ulong KnightAttacks(ulong knights)
        {
            ulong l1 = (knights >> 1) & 0x7f7f7f7f7f7f7f7f;
            ulong l2 = (knights >> 2) & 0x3f3f3f3f3f3f3f3f;
            ulong r1 = (knights << 1) & 0xfefefefefefefefe;
            ulong r2 = (knights << 2) & 0xfcfcfcfcfcfcfcfc;
            ulong h1 = l1 | r1;
            ulong h2 = l2 | r2;
            return (h1 << 16) | (h1 >> 16) | (h2 << 8) | (h2 >> 8);
        }

        private ulong BishopAttacks(ulong bishops)
        {
            ulong xIncyInc = 0;
            ulong xIncyDec = 0;
            ulong xDecyInc = 0;
            ulong xDecyDec = 0;
            // x++, y++
            xIncyInc |= (bishops & 0x007F7F7F7F7F7F7F) << 9;
            // x--, y++
            xDecyInc |= (bishops & 0x00FEFEFEFEFEFEFE) << 7;
            // x++. y--
            xIncyDec |= (bishops & 0x7F7F7F7F7F7F7F00) >> 7;
            // x--, y--
            xDecyDec |= (bishops & 0xFEFEFEFEFEFEFE00) >> 9;

            ulong mask = ~pieceBoard;
            for (int i = 2; i <= 7; i++)
            {
                xIncyInc |= (xIncyInc & mask & 0x007F7F7F7F7F7F7F) << 9;
                xDecyInc |= (xDecyInc & mask & 0x00FEFEFEFEFEFEFE) << 7;
                xIncyDec |= (xIncyDec & mask & 0x7F7F7F7F7F7F7F00) >> 7;
                xDecyDec |= (xDecyDec & mask & 0xFEFEFEFEFEFEFE00) >> 9;
            }
            return xIncyInc | xIncyDec | xDecyInc | xDecyDec;
        }

        private ulong RookAttacks(ulong rooks)
        {
            ulong xInc = 0;
            ulong xDec = 0;
            ulong yInc = 0;
            ulong yDec = 0;
            // x++
            xInc |= (rooks & 0x7F7F7F7F7F7F7F7F) << 1;
            // x--
            xDec |= (rooks & 0xFEFEFEFEFEFEFEFE) >> 1;
            // y++
            yInc |= (rooks & 0x00FFFFFFFFFFFFFF) << 8;
            // y--
            yDec |= (rooks & 0xFFFFFFFFFFFFFF00) >> 8;

            ulong mask = ~pieceBoard;
            for (int i = 2; i <= 7; i++)
            {
                yInc |= (yInc & mask & 0x00FFFFFFFFFFFFFF) << 8;
                xInc |= (xInc & mask & 0x7F7F7F7F7F7F7F7F) << 1;
                xDec |= (xDec & mask & 0xFEFEFEFEFEFEFEFE) >> 1;
                yDec |= (yDec & mask & 0xFFFFFFFFFFFFFF00) >> 8;
            }
            return xInc | xDec | yInc | yDec;
        }

        private ulong QueenAttacks(ulong queens)
        {
            ulong attacks = 0;
            attacks |= BishopAttacks(queens);
            attacks |= RookAttacks(queens);
            return attacks;
        }

        // Source: https://www.chessprogramming.org/King_Pattern
        private ulong KingAttacks(ulong king)
        {
            ulong attacks = ((king & 0x7F7F7F7F7F7F7F7F) << 1) | ((king & 0xFEFEFEFEFEFEFEFE) >> 1);
            attacks |= ((attacks & 0x00FFFFFFFFFFFFFF) << 8) | ((attacks & 0xFFFFFFFFFFFFFF00) >> 8);
            return attacks;
        }

        public void LoadFen(string fen)
        {
            string[] strings = fen.Split(" ");
            string[] rows = strings[0].Split("/");

            int index = 0;
            foreach (string row in rows)
            {
                foreach (char c in row)
                {
                    if (c - '0' > 0 && c - '0' <= 9)
                    {
                        index += (c - '0');
                        continue;
                    }

                    ulong position = (ulong)1 << ((index % 8) + (7 - index / 8) * 8);
                    pieceBoard |= position;
                    if (Char.IsUpper(c))
                    {
                        whiteMask |= position;
                    }
                    else
                    {
                        blackMask |= position;
                    }

                    if (Char.ToUpper(c) == 'K')
                    {
                        kingMask |= position;
                    }
                    else if (Char.ToUpper(c) == 'Q')
                    {
                        queenMask |= position;
                    }
                    else if (Char.ToUpper(c) == 'R')
                    {
                        rookMask |= position;
                    }
                    else if (Char.ToUpper(c) == 'B')
                    {
                        bishopMask |= position;
                    }
                    else if (Char.ToUpper(c) == 'N')
                    {
                        knightMask |= position;
                    }
                    else if (Char.ToUpper(c) == 'P')
                    {
                        pawnMask |= position;
                    }
                    index++;
                }
            }

            turn = strings[1] == "w" ? 0 : 1;
            if (strings[2] != "-")
            {
                foreach (char c in strings[2])
                {
                    if (Char.IsUpper(c))
                    {
                        if (c == 'K')
                        {
                            castle |= 0x0000000000000080;
                        }
                        else if (c == 'Q')
                        {
                            castle |= 0x0000000000000001;
                        }
                    }
                    else
                    {
                        if (c == 'k')
                        {
                            castle |= 0x8000000000000000;
                        }
                        else if (c == 'q')
                        {
                            castle |= 0x0100000000000000;
                        }
                    }
                }
            }
            if (strings[3] != "-")
            {
                enPassant = StringToBinary(strings[3]);
            }
            halfMoveTimer = Int32.Parse(strings[4]);
            move = Int32.Parse(strings[5]);
        }

        private static ulong StringToBinary(string s) => (ulong)1 << ((s[0] - 'a') + (s[1] - '1') * 8);
        private static string BinaryToString(ulong l)
        {
            int index = (int)ulong.TrailingZeroCount(l);
            return new string(new char[] { (char)((index % 8) + 'a'), (char)((index / 8) + '1') });
        }

        private static ulong FlipVertical(ulong x) => BitConverter.ToUInt64(BitConverter.GetBytes(x).Reverse().ToArray());

        /**
         * Source: https://www.chessprogramming.org/Flipping_Mirroring_and_Rotating
         * Mirror a bitboard horizontally about the center files.
         * File a is mapped to file h and vice versa.
         * @param x any bitboard
         * @return bitboard x mirrored horizontally
         */
        private static ulong MirrorHorizontal(ulong x)
        {
            const ulong k1 = 0x5555555555555555;
            const ulong k2 = 0x3333333333333333;
            const ulong k4 = 0x0f0f0f0f0f0f0f0f;
            x ^= k4 & (x ^ ulong.RotateLeft(x, 8));
            x ^= k2 & (x ^ ulong.RotateLeft(x, 4));
            x ^= k1 & (x ^ ulong.RotateLeft(x, 2));
            return ulong.RotateRight(x, 7);
        }

        /**
         * Source: https://www.chessprogramming.org/Flipping_Mirroring_and_Rotating
         * Flip a bitboard about the diagonal a1-h8.
         * Square h1 is mapped to a8 and vice versa.
         * @param x any bitboard
         * @return bitboard x flipped about diagonal a1-h8
         */
        private static ulong FlipDiagA1H8(ulong x)
        {
            ulong t;
            const ulong k1 = 0x5500550055005500;
            const ulong k2 = 0x3333000033330000;
            const ulong k4 = 0x0f0f0f0f00000000;
            t = k4 & (x ^ (x << 28));
            x ^= t ^ (t >> 28);
            t = k2 & (x ^ (x << 14));
            x ^= t ^ (t >> 14);
            t = k1 & (x ^ (x << 7));
            x ^= t ^ (t >> 7);
            return x;
        }

        /**
         * Source: https://www.chessprogramming.org/Flipping_Mirroring_and_Rotating
         * Flip a bitboard about the antidiagonal a8-h1.
         * Square a1 is mapped to h8 and vice versa.
         * @param x any bitboard
         * @return bitboard x flipped about antidiagonal a8-h1
         */
        private static ulong FlipDiagA8H1(ulong x)
        {
            ulong t;
            const ulong k1 = 0xaa00aa00aa00aa00;
            const ulong k2 = 0xcccc0000cccc0000;
            const ulong k4 = 0xf0f0f0f00f0f0f0f;
            t = x ^ (x << 36);
            x ^= k4 & (t ^ (x >> 36));
            t = k2 & (x ^ (x << 18));
            x ^= t ^ (t >> 18);
            t = k1 & (x ^ (x << 9));
            x ^= t ^ (t >> 9);
            return x;
        }
    }
}