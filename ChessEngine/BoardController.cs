
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
                            castle |= 0x0000000000000090;
                        }
                        else if (c == 'Q')
                        {
                            castle |= 0x0000000000000011;
                        }
                    }
                    else
                    {
                        if (c == 'k')
                        {
                            castle |= 0x9000000000000000;
                        }
                        else if (c == 'q')
                        {
                            castle |= 0x1100000000000000;
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

        public void Move(string move)
        {
            ulong piece = StringToBinary(move[..2]);
            ulong target = StringToBinary(move[2..4]);
            char promotion = ' ';
            if (move.Length == 5)
            {
                promotion = move[4];
            }

            ref ulong teamMask = ref whiteMask;
            ref ulong oTeamMask = ref blackMask;

            if ((piece & teamMask) == 0)
            {
                teamMask = ref blackMask;
                oTeamMask = ref whiteMask;
            }

            halfMoveTimer++;

            

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
            else if (target == enPassant && (piece & pawnMask) != 0)
            {
                ulong passantTarget = (target.CompareTo(piece) > 0) ? target >> 8 : target << 8;
                oTeamMask &= ~passantTarget;
                pawnMask &= ~passantTarget;
                pieceBoard &= ~passantTarget;
            }

            enPassant = 0;

            if ((piece & kingMask) != 0)
            {
                kingMask &= ~piece;
                kingMask |= target;
                ulong rook = 0;
                if (piece << 2 == target)
                {
                    rook = piece << 3;
                    rookMask &= ~rook;
                    rookMask |= target >> 1;
                    pieceBoard &= ~rook;
                    pieceBoard |= target >> 1;
                    teamMask &= ~rook;
                    teamMask |= target >> 1;
                }
                else if (piece >> 2 == target)
                {
                    rook = piece >> 4;
                    rookMask &= ~rook;
                    rookMask |= target << 1;
                    pieceBoard &= ~rook;
                    pieceBoard |= target << 1;
                    teamMask &= ~rook;
                    teamMask |= target << 1;
                }
                castle &= ~(piece | rook);
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
                castle &= ~piece;
            }
            else if ((piece & bishopMask) != 0)
            {
                bishopMask &= ~piece;
                bishopMask |= target;
            }
            else if ((piece & knightMask) != 0)
            {
                knightMask &= ~piece;
                knightMask |= target;
            }
            else if ((piece & pawnMask) != 0)
            {
                // p and pinned pawns is used to stop enPassant into check on the next move
                // KPpr could result in check if the white pawn was to enPassant which is illegal
                ulong pinnedPawns = PinnedPieces(pieceBoard & oTeamMask & pawnMask);
                ulong p = ((target & 0x7F7F7F7F7F7F7F7F) << 1 | (target & 0xFEFEFEFEFEFEFEFE) >> 1) & ~pinnedPawns & pieceBoard & pawnMask & oTeamMask;
                if ((piece << 16) == target && p != 0)
                {
                    enPassant = piece << 8;
                }
                else if ((piece >> 16) == target && p != 0)
                {
                    enPassant = piece >> 8;
                }


                pawnMask &= ~piece;
                switch (promotion)
                {
                    case 'q':
                        queenMask |= target;
                        break;
                    case 'r':
                        rookMask |= target;
                        break;
                    case 'b':
                        bishopMask |= target;
                        break;
                    case 'n':
                        knightMask |= target;
                        break;
                    default:
                        pawnMask |= target;
                        break;
                }

                halfMoveTimer = 0;
            }

            teamMask &= ~piece;
            teamMask |= target;
            pieceBoard &= ~piece;
            pieceBoard |= target;

            turn++;
            move += turn % 2 == 0 ? 1 : 0;
        }

        public int PieceScore(int team)
        {
            ulong pieces = 0;
            if (team == 0)
            {
                pieces = pieceBoard & whiteMask;
            }
            else
            {
                pieces = pieceBoard & blackMask;
            }
            int value = 0;
            value += (int)ulong.PopCount(pieces & pawnMask);
            value += (int)ulong.PopCount(pieces & knightMask) * 3;
            value += (int)ulong.PopCount(pieces & bishopMask) * 3;
            value += (int)ulong.PopCount(pieces & rookMask) * 5;
            value += (int)ulong.PopCount(pieces & queenMask) * 9;
            return value;
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
            ulong pinnedPieces = PinnedPieces(pieces & ~kingMask);

            // Legal king moves
            ulong temp = checks;
            while (temp != 0)
            {
                ulong check = (ulong)1 << (int)ulong.TrailingZeroCount(temp);
                if ((check & knightMask) == 0)
                {
                    oAttacks |= AttackLine(king, check) & ~check;
                }
                temp &= ~check;
            }
            ulong moves;
            moves = KingAttacks(king) & ~(pieces | oAttacks);
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
                ulong checkPieces = pieces & ~pinnedPieces & ~king;
                while (checkPieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(checkPieces);
                    moves = (stopCheck & (GetMoves(piece))) | (PawnAttacks(piece & pawnMask) & checks);
                    if (moves != 0)
                    {
                        legalMoves.AddRange(MoveList(piece, moves));
                    }
                    checkPieces &= ~piece;
                }

                // En passant out of check
                if ((PawnAttacks(pieces & pawnMask) & enPassant & (((checks & 0x00FFFFFFFFFFFFFF) << 8) | ((checks & 0xFFFFFFFFFFFFFF00) >> 8))) != 0)
                {
                    if ((((checks & 0x7F7F7F7F7F7F7F7F) << 1) & pieces & pawnMask & ~pinnedPieces) != 0)
                    {
                        legalMoves.AddRange(MoveList(((checks & 0x7F7F7F7F7F7F7F7F) << 1) & pieces & pawnMask, enPassant));
                    }
                    if ((((checks & 0xFEFEFEFEFEFEFEFE) >> 1) & pieces & pawnMask & ~pinnedPieces) != 0)
                    {
                        legalMoves.AddRange(MoveList(((checks & 0xFEFEFEFEFEFEFEFE) >> 1) & pieces & pawnMask, enPassant));
                    }
                }
            }

            // If not in check then add other legal piece moves
            if (numberOfChecks == 0)
            {
                ulong checkPieces = pieces & ~pinnedPieces & ~king;
                while (checkPieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(checkPieces);
                    moves = (GetMoves(piece) & ~pieces) | (PawnAttacks(piece & pawnMask) & (oPieces | enPassant));
                    if (moves != 0)
                    {
                        legalMoves.AddRange(MoveList(piece, moves));
                    }
                    checkPieces &= ~piece;
                }

                while (pinnedPieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(pinnedPieces);
                    moves = (GetMoves(piece) & AttackLine(king, piece) & ~king) | (PawnAttacks(piece & pawnMask) & AttackLine(king, piece) & oPieces);
                    if (moves != 0)
                    {
                        legalMoves.AddRange(MoveList(piece, moves));
                    }
                    pinnedPieces &= ~piece;
                }

                if ((king & castle) != 0)
                {
                    ulong s = king << 1 | king << 2;
                    ulong l = king >> 1 | king >> 2;
                    if (((king << 3) & castle) != 0 && (king & RookAttacks(king << 3)) != 0 && (s & oAttacks) == 0)
                    {
                        legalMoves.Add(BinaryToString(king) + BinaryToString(king << 2));
                    }
                    if (((king >> 4) & castle) != 0 && (king & RookAttacks(king >> 4)) != 0 && (l & oAttacks) == 0)
                    {
                        legalMoves.Add(BinaryToString(king) + BinaryToString(king >> 2));
                    }
                }
            }

            return legalMoves;
        }

        public List<string> GetCaptures()
        {
            List<string> legalMoves = GetLegalMoves();
            List<string> captures = new();
            ulong teamMask;
            if (turn % 2 == 0)
            {
                teamMask = blackMask;
            }
            else
            {
                teamMask = whiteMask;
            }

            foreach (string move in legalMoves)
            {
                ulong target = StringToBinary(move[2..4]);
                if ((target & teamMask) != 0 || ((StringToBinary(move[..2]) & pawnMask) != 0 && (target & enPassant) != 0))
                {
                    captures.Add(move);
                }
            }

            return captures;
        }

        private List<string> MoveList(ulong piece, ulong moves)
        {
            List<string> list = new();
            while (moves != 0)
            {
                ulong move = (ulong)1 << (int)ulong.TrailingZeroCount(moves);
                string moveString = BinaryToString(piece) + BinaryToString(move);
                if ((piece & pawnMask) != 0 && (move & 0xFF000000000000FF) != 0)
                {
                    list.Add(moveString + 'q');
                    list.Add(moveString + 'r');
                    list.Add(moveString + 'b');
                    list.Add(moveString + 'n');
                }
                else
                {
                    list.Add(moveString);
                }
                moves &= ~move;
            }
            return list;
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

        private static ulong AttackLine(ulong p1, ulong p2)
        {
            int pieceX = (int)ulong.TrailingZeroCount(p2) % 8;
            int pieceY = (int)ulong.TrailingZeroCount(p2) / 8;
            int kingX = (int)ulong.TrailingZeroCount(p1) % 8;
            int kingY = (int)ulong.TrailingZeroCount(p1) / 8;

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
            attacks |= (pawns & blackMask & 0x7F7F7F7F7F7F7F00) >> 7;
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
            ulong attacks = king | ((king & 0x7F7F7F7F7F7F7F7F) << 1) | ((king & 0xFEFEFEFEFEFEFEFE) >> 1);
            attacks |= ((attacks & 0x00FFFFFFFFFFFFFF) << 8) | ((attacks & 0xFFFFFFFFFFFFFF00) >> 8);
            return attacks & ~king;
        }

        private static ulong StringToBinary(string s) => (ulong)1 << ((s[0] - 'a') + (s[1] - '1') * 8);
        private static string BinaryToString(ulong l)
        {
            int index = (int)ulong.TrailingZeroCount(l);
            return new string(new char[] { (char)((index % 8) + 'a'), (char)((index / 8) + '1') });
        }
    }
}