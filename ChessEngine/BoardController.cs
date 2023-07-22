
namespace ChessEngine
{
    public class BoardController
    {
        public int turn;
        public int halfMoveCounter;
        public Tile[,] board = new Tile[8, 8];
        public List<Piece>[] pieces = new List<Piece>[] { new List<Piece>(), new List<Piece>() };
        public List<string> moveList = new();
        public bool enPassant = false;
        public bool checkMate = false;
        public bool gameOver = false;

        public BoardController(string fen)
        {
            for (int i = 0; i < 8; i++)
            {
                for (int k = 0; k < 8; k++)
                {
                    board[i, k] = new Tile(i, k);
                }
            }
            LoadFEN(fen);
        }

        private void LoadFEN(string fen)
        {
            string[] fenComponents = fen.Split(" ");
            string[] rows = fenComponents[0].Split("/");
            int i = 0;
            foreach (string row in rows)
            {
                foreach (char c in row)
                {
                    if (c - '0' >= 1 && c - '0' <= 8)
                    {
                        i += c - '0';
                        continue;
                    }
                    int t = char.IsUpper(c) ? 0 : 1;
                    bool m = char.ToUpper(c) != 'K' && (char.ToUpper(c) != 'P' || 7 - (i / 8) != (t == 0 ? 1 : 6));
                    Piece p = new Piece(this, char.ToUpper(c), t, board[i % 8, 7 - (i / 8)], m);
                    pieces[t].Add(p);
                    board[i % 8, 7 - (i / 8)].SetPiece(p);
                    i++;
                }
            }

            turn = (int.Parse(fenComponents[5]) - 1) * 2 + (fenComponents[1] == "w" ? 0 : 1);
            halfMoveCounter = int.Parse(fenComponents[4]);

            if (fenComponents[3] != "-")
            {
                enPassant = true;
                if (turn % 2 == 1)
                {
                    moveList.Add("");
                }
                moveList.Add(new string(fenComponents[3][0], (char)(fenComponents[3][1] - '0' + (fenComponents[1] == "w" ? 1 : -1))));
            }

            if (fenComponents[2] != "-")
            {
                foreach (char c in fenComponents[2])
                {
                    Piece? p = board[char.ToUpper(c) == 'K' ? 7 : 0, char.IsUpper(c) ? 0 : 7].GetPiece();
                    if (p is not null && p.GetType() == 'R')
                    {
                        p.SetMoved(false);
                    }
                }
            }
        }

        public bool Update()
        {
            if (gameOver) { return true; }

            turn++;
            halfMoveCounter++;
            checkMate = CheckMate();
            gameOver = checkMate;

            if ((GetMoves(turn % 2, true).Count == 0 || halfMoveCounter >= 100) && !checkMate) { gameOver = true; }

            return gameOver;
        }

        // Checks if a move is legal.
        public bool MoveCheck(Piece p, Tile t)
        {
            Tile originalTile = p.GetTile();
            Piece? originalPiece = t.GetPiece();

            p.GetTile().ClearPiece();
            p.SetTile(t);
            t.SetPiece(p);

            bool r = CheckCheck(p.GetTeam(), originalPiece);

            p.SetTile(originalTile);
            originalTile.SetPiece(p);
            t.SetPiece(originalPiece);

            return !r;
        }

        public bool CheckCheck(int team, Piece? captured)
        {
            List<string> moveList = GetMoves((team + 1) % 2, false);
            string kingTile = "";

            foreach (Piece p in pieces[team])
            {
                if (p.GetType() == 'K')
                {
                    kingTile = p.GetTile().ToString();
                    break;
                }
            }

            foreach (string move in moveList)
            {
                if (captured is not null && move[..2] == captured.GetTile().ToString())
                {
                    continue;
                }

                if (move[2..] == kingTile)
                {
                    return true;
                }
            }
            return false;
        }

        /* kingMoves is used to decide if legal king moves should be included in the list.
         * This should always be true unless you are checking for check. 
         */
        public List<string> GetMoves(int team, bool legalMoves)
        {
            List<string> moves = new();
            foreach (Piece piece in pieces[team])
            {
                moves.AddRange(piece.GetMoves(legalMoves));
            }
            return moves;
        }

        // Moves the piece to the target as long as the move is legal
        public bool MovePiece(string move)
        {
            Piece piece = board[move[0] - 'a', move[1] - '1'].GetPiece()!;
            Tile target = board[move[2] - 'a', move[3] - '1'];

            char pawnPromotion = 'P';

            if (move.Length > 4)
            {
                pawnPromotion = Char.ToUpper(move[4]);
            }

            if (!piece.GetMoves(true).Contains(move)) { return false; }

            string moveString = "";

            // Pawn specific moves
            if (piece.GetType() == 'P')
            {
                halfMoveCounter = 0;
                // Pawn capture character
                if (piece.GetTile().X != target.X)
                {
                    moveString += (char)(piece.GetTile().X + 'a');
                }

                // En Passant capture
                if (target.GetPiece() == null && Math.Abs(target.X - piece.GetTile().X) == 1)
                {
                    int d = piece.GetTeam() == 0 ? -1 : 1;
                    pieces[(turn + 1) % 2].Remove(board[target.X, target.Y + d].GetPiece()!);
                    board[target.X, target.Y + d].ClearPiece();
                }

                // Pawn promotion
                if (piece.GetTeam() == 0 && target.Y == 7 || piece.GetTeam() == 1 && target.Y == 0)
                {
                    piece.SetType(pawnPromotion);
                }
            }
            else
            {
                moveString += piece.GetType();
            }
            
            if (target.GetPiece() is not null)
            {
                moveString += 'x';
                halfMoveCounter = 0;
                foreach (Piece p in pieces[(turn + 1) % 2])
                {
                    if (p == target.GetPiece())
                    {
                        pieces[(turn + 1) % 2].Remove(p);
                        break;
                    }
                }
            }

            moveString += (char)(target.X + 'a');
            moveString += (char)(target.Y + '1');

            // Castle
            if (piece.GetType() == 'K' && Math.Abs(piece.GetTile().X - target.X) == 2)
            {
                board[target.X == 6 ? 7 : 0, target.Y].GetPiece()!.SetTile(board[target.X == 6 ? 5 : 3, target.Y]);
                board[target.X == 6 ? 5 : 3, target.Y].SetPiece(board[target.X == 6 ? 7 : 0, target.Y].GetPiece()!);
                board[target.X == 6 ? 7 : 0, target.Y].ClearPiece();
                moveString = "O-O" + (target.X == 2 ? "-O" : "");
            }

            piece.GetTile().ClearPiece();
            piece.SetTile(target);
            target.SetPiece(piece);
            piece.SetMoved(true);

            if (CheckCheck((turn + 1) % 2, null))
            {
                moveString += '+';
            }

            // En Passant
            if (piece.GetType() == 'P' && Math.Abs(piece.GetTile().Y - target.Y) == 2)
            {
                enPassant = true;
            }
            else
            {
                enPassant = false;
            }

            moveList.Add(moveString);
            return true;
        }

        public bool CheckMate()
        {
            if (CheckCheck(turn % 2, null) && GetMoves(turn % 2, true).Count == 0)
            {
                return true;
            }
            return false;
        }
    }

    public class Tile
    {
        private int x;
        private int y;
        private Piece? piece = null;

        public Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int X 
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public Piece? GetPiece()
        {
            return piece;
        }

        public void SetPiece(Piece? piece)
        {
            this.piece = piece;
        }

        public void ClearPiece()
        {
            piece = null;
        }

        override public string ToString() => new string(new char[] { (char)(x + 'a'), (char)(y + '1') });
    }

    public class Piece
    {
        private readonly BoardController owner;
        private char type;
        private readonly int team;
        private Tile tile;
        private bool moved;

        public Piece(BoardController owner, char type, int team, Tile tile, bool moved)
        {
            this.owner = owner;
            this.type = type;
            this.team = team;
            this.tile = tile;
            this.moved = moved;
        }

        public int TileCheck(Tile t)
        {
            if (t.GetPiece() is not null)
            {
                if (t.GetPiece()!.GetTeam() == team)
                {
                    return 0;
                }
                return 1;
            }
            return 2;
        }

        public List<string> GetMoves(bool checkLegality)
        {
            List<string> moves = new(); 
            Tile t;
            int control = 0b11111111;

            switch (type)
            {
                case 'K':
                    for (int i = Math.Max(0, tile.X - 1); i <= Math.Min(tile.X + 1, 7); i++)
                    {
                        for (int j = Math.Max(0, tile.Y - 1); j <= Math.Min(tile.Y + 1, 7); j++)
                        {
                            if (i == tile.X && j == tile.Y)
                            {
                                continue;
                            }

                            t = owner.board[i, j];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }

                    if (checkLegality && !moved && !owner.CheckCheck(team, null))
                    {
                        List<string> opponentMoves = owner.GetMoves(team == 0 ? 1 : 0, false);
                        for (int i = 0; i < opponentMoves.Count; i++)
                        {
                            opponentMoves[i] = opponentMoves[i][2..];
                        }
                        if (owner.board[0, team == 0 ? 0 : 7].GetPiece() is not null
                            && !owner.board[0, team == 0 ? 0 : 7].GetPiece()!.GetMoved()
                            && TileCheck(owner.board[3, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[3, team == 0 ? 0 : 7].ToString())
                            && TileCheck(owner.board[2, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[2, team == 0 ? 0 : 7].ToString()))
                        {
                            moves.Add(tile.ToString() + owner.board[2, team == 0 ? 0 : 7].ToString());
                        }
                        if (owner.board[7, team == 0 ? 0 : 7].GetPiece() is not null
                            && !owner.board[7, team == 0 ? 0 : 7].GetPiece()!.GetMoved()
                            && TileCheck(owner.board[5, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[5, team == 0 ? 0 : 7].ToString())
                            && TileCheck(owner.board[6, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[6, team == 0 ? 0 : 7].ToString()))
                        {
                            moves.Add(tile.ToString() + owner.board[2, team == 0 ? 0 : 7].ToString());
                        }
                    }

                    break;
                case 'Q':
                    for (int i = 1; i < 7; i++)
                    {
                        if (tile.X + i < 8)
                        {
                            if ((control & 0b0001) == 0b0001)
                            {
                                t = owner.board[tile.X + i, tile.Y];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b0001;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                            if (tile.Y + i < 8 && (control & 0b10000000) == 0b10000000)
                            {
                                t = owner.board[tile.X + i, tile.Y + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b10000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                            if (tile.Y - i >= 0 && (control & 0b01000000) == 0b01000000)
                            {
                                t = owner.board[tile.X + i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b01000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                        }
                        if (tile.X - i >= 0)
                        {
                            if ((control & 0b0010) == 0b0010)
                            {
                                t = owner.board[tile.X - i, tile.Y];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b0010;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                            if (tile.Y + i < 8 && (control & 0b00100000) == 0b00100000)
                            {
                                t = owner.board[tile.X - i, tile.Y + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00100000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                            if (tile.Y - i >= 0 && (control & 0b00010000) == 0b00010000)
                            {
                                t = owner.board[tile.X - i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00010000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                        }
                        if (tile.Y + i < 8 && (control & 0b0100) == 0b0100)
                        {
                            t = owner.board[tile.X, tile.Y + i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0100;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y - i >= 0 && (control & 0b1000) == 0b1000)
                        {
                            t = owner.board[tile.X, tile.Y - i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b1000;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    break;
                case 'R':
                    for (int i = 1; i < 8; i++)
                    {
                        if (tile.X + i < 8 && (control & 0b0001) == 0b0001)
                        {
                            t = owner.board[tile.X + i, tile.Y];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0001;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.X - i >= 0 && (control & 0b0010) == 0b0010)
                        {
                            t = owner.board[tile.X - i, tile.Y];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0010;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y + i < 8 && (control & 0b0100) == 0b0100)
                        {
                            t = owner.board[tile.X, tile.Y + i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0100;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y - i >= 0 && (control & 0b1000) == 0b1000)
                        {
                            t = owner.board[tile.X, tile.Y - i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b1000;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    break;
                case 'B':
                    for (int i = 1; i < 7; i++)
                    {
                        if (tile.X + i < 8)
                        {
                            if (tile.Y + i < 8 && (control & 0b10000000) == 0b10000000)
                            {
                                t = owner.board[tile.X + i, tile.Y + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b10000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                            if (tile.Y - i >= 0 && (control & 0b01000000) == 0b01000000)
                            {
                                t = owner.board[tile.X + i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b01000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                        }
                        if (tile.X - i >= 0)
                        {
                            if (tile.Y + i < 8 && (control & 0b00100000) == 0b00100000)
                            {
                                t = owner.board[tile.X - i, tile.Y + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00100000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                            if (tile.Y - i >= 0 && (control & 0b00010000) == 0b00010000)
                            {
                                t = owner.board[tile.X - i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00010000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                        }
                    }
                    break;
                case 'N':
                    if (tile.X + 2 < 8)
                    {
                        if (tile.Y + 1 < 8)
                        {
                            t = owner.board[tile.X + 2, tile.Y + 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y - 1 >= 0)
                        {
                            t = owner.board[tile.X + 2, tile.Y - 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    if (tile.X - 2 >= 0)
                    {
                        if (tile.Y + 1 < 8)
                        {
                            t = owner.board[tile.X - 2, tile.Y + 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y - 1 >= 0)
                        {
                            t = owner.board[tile.X - 2, tile.Y - 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    if (tile.X + 1 < 8)
                    {
                        if (tile.Y + 2 < 8)
                        {
                            t = owner.board[tile.X + 1, tile.Y + 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y - 2 >= 0)
                        {
                            t = owner.board[tile.X + 1, tile.Y - 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    if (tile.X - 1 >= 0)
                    {
                        if (tile.Y + 2 < 8)
                        {
                            t = owner.board[tile.X - 1, tile.Y + 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.Y - 2 >= 0)
                        {
                            t = owner.board[tile.X - 1, tile.Y - 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    break;
                case 'P':
                    int direction = team == 0 ? 1 : -1;
                    if (tile.Y + direction >= 0 && tile.Y + direction < 8)
                    {
                        t = owner.board[tile.X, tile.Y + direction];
                        if (checkLegality && TileCheck(t) == 2 && owner.MoveCheck(this, t))
                        {
                            string ms = tile.ToString() + t.ToString();
                            if (t.Y == 0 || t.Y == 7)
                            {
                                ms += 'q';
                            }
                            moves.Add(ms);
                            if (!moved)
                            {
                                t = owner.board[tile.X, tile.Y + 2 * direction];
                                if (TileCheck(t) == 2 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(tile.ToString() + t.ToString());
                                }
                            }
                        }

                        if (tile.X + 1 < 8)
                        {
                            t = owner.board[tile.X + 1, tile.Y + direction];
                            if (!checkLegality || owner.MoveCheck(this, t) && (TileCheck(t) == 1
                            || owner.enPassant && owner.moveList[^1][0] - 'a' - 1 == tile.X))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                        if (tile.X - 1 >= 0)
                        {
                            t = owner.board[tile.X - 1, tile.Y + direction];
                            if (!checkLegality || owner.MoveCheck(this, t) && (TileCheck(t) == 1
                            || owner.enPassant && owner.moveList[^1][0] - 'a' + 1 == tile.X))
                            {
                                moves.Add(tile.ToString() + t.ToString());
                            }
                        }
                    }
                    break;
            }
            return moves;
        }

        public new char GetType() { return type; }
        public void SetType(char type) { this.type = type; }

        public Tile GetTile() { return tile!; }
        public void SetTile(Tile tile) { this.tile = tile; }

        public int GetTeam() { return team; }

        public bool GetMoved() { return moved; }
        public void SetMoved(bool moved) { this.moved = moved; }
    }
}