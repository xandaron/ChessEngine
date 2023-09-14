namespace ChessEngine
{
    public class BoardController
    {
        ulong pieceBoard;
        ulong whiteMask;
        ulong blackMask;

        // Keeps track of the different piece types for both black and white.
        ulong kingMask;
        ulong queenMask;
        ulong rookMask;
        ulong bishopMask;
        ulong knightMask;
        ulong pawnMask;

        uint turn;
        uint move;
        uint halfMoveTimer;

        ulong enPassant;
        ulong castle;

        readonly string? fen;
        readonly List<string> pastPositions = new();

        public BoardController(string fen)
        {
            this.fen = fen;
            LoadFen(fen);
        }

        public BoardController(ulong pieceBoard, ulong whiteMask, ulong kingMask, ulong queenMask, ulong rookMask, ulong bishopMask,
            ulong knightMask, ulong pawnMask, ulong enPassant, ulong castle, uint turn, uint move, uint halfMoveTimer, List<string> pastPositions)
        {
            this.pieceBoard = pieceBoard;
            this.whiteMask = whiteMask;
            this.blackMask = pieceBoard & ~whiteMask;
            this.kingMask = kingMask;
            this.queenMask = queenMask;
            this.rookMask = rookMask;
            this.bishopMask = bishopMask;
            this.knightMask = knightMask;
            this.pawnMask = pawnMask;
            this.enPassant = enPassant;
            this.castle = castle;
            this.turn = turn;
            this.move = move;
            this.halfMoveTimer = halfMoveTimer;
            this.pastPositions = pastPositions;
        }

        public void LoadFen(string fen)
        {
            string[] strings = fen.Split(" ");
            string[] rows = strings[0].Split("/");

            pieceBoard = 0;
            whiteMask = 0;
            blackMask = 0;

            kingMask = 0;
            queenMask = 0;
            rookMask = 0;
            bishopMask = 0;
            knightMask = 0;
            pawnMask = 0;

            castle = 0;
            enPassant = 0;

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

            turn = strings[1] == "w" ? (uint)0 : 1;
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
            halfMoveTimer = uint.Parse(strings[4]);
            move = uint.Parse(strings[5]);
        }

        public void Move(string move)
        {
            StorePosition();
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
                ulong pinnedPawns = PinnedPieces(oTeamMask & kingMask, pieceBoard & oTeamMask & pawnMask);
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

        public void StorePosition()
        {
            string positionString = "";

            positionString += pieceBoard.ToString() + " ";
            positionString += whiteMask.ToString() + " ";
            positionString += kingMask.ToString() + " ";
            positionString += queenMask.ToString() + " ";
            positionString += rookMask.ToString() + " ";
            positionString += bishopMask.ToString() + " ";
            positionString += knightMask.ToString() + " ";
            positionString += pawnMask.ToString() + " ";
            positionString += enPassant.ToString() + " ";
            positionString += castle.ToString() + " ";
            positionString += turn.ToString() + " ";
            positionString += move.ToString() + " ";
            positionString += halfMoveTimer.ToString();

            pastPositions.Add(positionString);
        }

        public void UndoMove()
        {
            string position = pastPositions.Last();
            pastPositions.RemoveAt(pastPositions.Count - 1);

            string[] positionElements = position.Split(' ');

            pieceBoard = ulong.Parse(positionElements[0]);
            whiteMask = ulong.Parse(positionElements[1]);
            blackMask = pieceBoard & ~whiteMask;
            kingMask = ulong.Parse(positionElements[2]);
            queenMask = ulong.Parse(positionElements[3]);
            rookMask = ulong.Parse(positionElements[4]);
            bishopMask = ulong.Parse(positionElements[5]);
            knightMask = ulong.Parse(positionElements[6]);
            pawnMask = ulong.Parse(positionElements[7]);
            enPassant = ulong.Parse(positionElements[8]);
            castle = ulong.Parse(positionElements[9]);
            turn = uint.Parse(positionElements[10]);
            move = uint.Parse(positionElements[11]);
            halfMoveTimer = uint.Parse(positionElements[12]);
        }

        public bool IsCheck()
        {
            ulong teamMask = whiteMask;
            if (turn % 2 == 1)
            {
                teamMask = blackMask;
            }

            if ((GetAttacks(pieceBoard & ~teamMask) & kingMask & teamMask) != 0)
            {
                return true;
            }
            return false;
        }

        public List<string> GetLegalMoves()
        {
            ulong pieces = turn % 2 == 0 ? whiteMask : blackMask;

            List<string> legalMoves = new();

            ulong king = pieces & kingMask;
            ulong checks = GetAttacked(king) & ~pieces;

            uint numberOfChecks = (uint)ulong.PopCount(checks);
            ulong pinnedPieces = PinnedPieces(king, pieces & ~king);

            ulong opponentPieces = pieceBoard & ~pieces;
            ulong opponentAttacks = GetAttacks(opponentPieces);

            // Have to move king
            if (numberOfChecks == 2)
            {
                ulong attackLines = 0;
                while (checks > 0)
                {
                    ulong check = (ulong)1 << (int)ulong.TrailingZeroCount(checks);
                    attackLines |= AttackLine(king, check) & ~check;
                    checks &= ~check;
                }

                legalMoves.AddRange(MoveList(king, KingAttacks(king) & ~(pieces | opponentAttacks | attackLines)));
            }
            // Block checks or move king
            else if (numberOfChecks == 1)
            {
                legalMoves.AddRange(MoveList(king, KingAttacks(king) & ~(pieces | opponentAttacks | ((checks & pawnMask) == 0 ? (AttackLine(king, checks) & ~checks) : 0))));

                ulong block = (AttackLine(king, checks) & QueenAttacks(king) & QueenAttacks(checks)) | checks;
                pieces &= ~(pinnedPieces | king);
                while (pieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(pieces);
                    legalMoves.AddRange(MoveList(piece, (block | ((piece & pawnMask) == 0 ? 0 : enPassant)) & GetMoves(piece)));
                    pieces &= ~piece;
                }
            }
            // Move any not pinned piece
            else if (numberOfChecks == 0)
            {
                legalMoves.AddRange(MoveList(king, KingAttacks(king) & ~(pieces | opponentAttacks)));

                ulong temp = pieces & ~(pinnedPieces | king);
                while (temp != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(temp);
                    legalMoves.AddRange(MoveList(piece, GetMoves(piece) & ~pieces));
                    temp &= ~piece;
                }
                // pinned pieces can capture their pinner or move along direction of the pin
                while (pinnedPieces != 0)
                {
                    ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(pinnedPieces);
                    legalMoves.AddRange(MoveList(piece, GetMoves(piece) & AttackLine(piece, king) & ~king));
                    pinnedPieces &= ~piece;
                }
                ulong rooks = pieces & rookMask & castle;
                if ((king & castle) != 0 && rooks != 0)
                {
                    if ((0x0100000000000001 & rooks) != 0 && (((king >> 1 | king >> 2) & (opponentAttacks | pieceBoard)) | (king >> 3 & pieceBoard)) == 0)
                    {
                        legalMoves.AddRange(MoveList(king, king >> 2));
                    }
                    if ((0x8000000000000080 & rooks) != 0 && ((king << 1 | king << 2) & (opponentAttacks | pieceBoard)) == 0)
                    {
                        legalMoves.AddRange(MoveList(king, king << 2));
                    }
                }
            }
            return legalMoves;
        }

        private ulong PinnedPieces(ulong king, ulong pieces)
        {
            ulong pinnedPieces = 0;
            while (pieces != 0)
            {
                ulong piece = (ulong)1 << (int)ulong.TrailingZeroCount(pieces);
                pinnedPieces |= Pinned(king, piece);
                pieces &= ~piece;
            }
            return pinnedPieces;
        }

        private ulong Pinned(ulong target, ulong piece)
        {
            ulong pinLine = AttackLine(target, piece);
            pinLine &= QueenAttacks(piece);

            if ((pinLine & target) == 0) { return 0; }

            ulong opponentPieces = pieceBoard & ((piece & whiteMask) != 0 ? blackMask: whiteMask);
            ulong opponentPiece = opponentPieces & pinLine & (queenMask | rookMask | bishopMask);

            if (opponentPiece == 0 || (GetAttacks(opponentPiece) & piece) == 0) { return 0; }

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
                if (m != 1 && m != -1)
                {
                    return 0;
                }
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

        public List<string> GetChecks()
        {
            List<string> moves = GetLegalMoves();
            List<string> checks = new();
            foreach (string move in moves)
            {
                Move(move);
                if (IsCheck())
                {
                    checks.Add(move);
                }
                UndoMove();
            }
            return checks;
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
            ulong Moves = 0;
            Moves |= PawnMoves(pieces & pawnMask);
            Moves |= KnightAttacks(pieces & knightMask);
            Moves |= BishopAttacks(pieces & bishopMask);
            Moves |= RookAttacks(pieces & rookMask);
            Moves |= QueenAttacks(pieces & queenMask);
            Moves |= KingAttacks(pieces & kingMask);
            return Moves;
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

        private ulong PawnMoves(ulong pawns)
        {
            ulong moves = 0;
            if ((pawns & whiteMask) != 0)
            {
                moves |= (pawns << 8) & ~pieceBoard;
                moves |= ((moves & 0x0000000000FF0000) << 8) & ~pieceBoard;
                moves |= (blackMask | enPassant) & (pawns & 0x007F7F7F7F7F7F7F) << 9;
                moves |= (blackMask | enPassant) & (pawns & 0x00FEFEFEFEFEFEFE) << 7;
            }
            else if ((pawns & blackMask) != 0)
            {
                moves |= (pawns >> 8) & ~pieceBoard;
                moves |= ((moves & 0x0000FF0000000000) >> 8) & ~pieceBoard;
                moves |= (whiteMask | enPassant) & (pawns & 0xFEFEFEFEFEFEFE00) >> 9;
                moves |= (whiteMask | enPassant) & (pawns & 0x7F7F7F7F7F7F7F00) >> 7;
            }
            return moves;
        }

        private ulong PawnAttacks(ulong pawns)
        {
            ulong attacks = 0;
            if ((pawns & whiteMask) != 0)
            {
                attacks |= (pawns & 0x007F7F7F7F7F7F7F) << 9;
                attacks |= (pawns & 0x00FEFEFEFEFEFEFE) << 7;
            }
            else
            {
                // Black pawns
                attacks |= (pawns & 0xFEFEFEFEFEFEFE00) >> 9;
                attacks |= (pawns & 0x7F7F7F7F7F7F7F00) >> 7;
            }
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
        private static ulong KingAttacks(ulong king)
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

        public BoardController Copy()
        {
            return new(pieceBoard, whiteMask, kingMask, queenMask, rookMask, bishopMask,
                knightMask, pawnMask, enPassant, castle, turn, move, halfMoveTimer, pastPositions);
        }

        public int GetTurn() { return (int)turn % 2; }
        public ulong GetWhitePieces() { return whiteMask; }
        public ulong GetBlackPieces() { return blackMask; }
        public ulong GetPawns() { return pawnMask; }
        public ulong GetKnights() { return knightMask; }
        public ulong GetBishops() { return bishopMask; }
        public ulong GetRooks() { return rookMask; }
        public ulong GetQueens() { return queenMask; }
        public ulong GetKings() { return kingMask; }
        public string? GetFen() { return fen; }
        public List<string> GetPastPositions() { return pastPositions; }
    }
}