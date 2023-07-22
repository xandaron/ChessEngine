
using System.Runtime.CompilerServices;

namespace ChessEngine
{
    public class BoardController
    {
        public int turn;
        public int halfMoveCounter;
        private Tile[,] board = new Tile[8, 8];
        public List<Piece>[] pieces = new List<Piece>[] { new List<Piece>(), new List<Piece>() };
        public bool enPassant = false;
        public string passant = "";
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
                    int t = char.IsUpper(c) ? 1 : -1;
                    bool m = char.ToUpper(c) != 'K' && (char.ToUpper(c) != 'P' || 7 - (i / 8) != (t == 1 ? 1 : 6));
                    Piece p = new Piece(this, char.ToUpper(c), t, board[i % 8, 7 - (i / 8)], m);
                    pieces[t].Add(p);
                    board[i % 8, 7 - (i / 8)].SetPiece(p);
                    i++;
                }
            }

            turn = (int.Parse(fenComponents[5]) - 1) * 2 + (fenComponents[1] == "w" ? 0 : 1);
            halfMoveCounter = int.Parse(fenComponents[4]);
            
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

            if (fenComponents[3] != "-")
            {
                enPassant = true;
                passant = fenComponents[3];
            }
        }

        public bool Update()
        {
            if (gameOver) { return true; }

            turn++;
            halfMoveCounter++;
            checkMate = CheckMate();
            gameOver = checkMate;

            if ((GetMoves(turn % 2, true, null).Count == 0 || halfMoveCounter >= 100) && !checkMate) { gameOver = true; }

            return gameOver;
        }

        public bool LegalMove(string move)
        {
            Tile oTile = GetTile(move[..2]);
            Tile tTile = GetTile(move[2..4]);

            if (oTile.GetPiece() is null)
            {
                throw new Exception("No piece found at " + oTile.ToString());
            }

            if (tTile.GetPiece() is not null && tTile.GetPiece()!.GetTeam() == oTile.GetPiece()!.GetTeam())
            {
                return false;
            }

            if (oTile.GetPiece()!.GetType() == 'P' && oTile.X != tTile.X && tTile.GetPiece() is null)
            {
                return false;
            }

            return false;
        }

        public bool IsCheck(int team)
        {
            ulong kingTile = 0b0;
            foreach (Piece piece in pieces[team == 1 ? 0 : 1])
            {
                if (piece.GetType() == 'K')
                {
                    kingTile = piece.GetTile().ToBinary();
                }
            }

            if ((GetControlledTiles(team) & kingTile) == kingTile)
            {
                return true;
            }
            return false;
        }

        public bool CheckMate()
        {
            if (IsCheck() && GetLegalMoves(turn % 2).Count == 0)
            {
                return true;
            }
            return false;
        }

        public List<string> GetLegalMoves(int team)
        {
            List<string> moves = new();
            foreach (Piece piece in pieces[team == 1 ? 0 : 1])
            {
                ulong pieceMoves = piece.GetMoves();
                for (int i = 63; i >= 0; i--)
                {
                    ulong check = (ulong)0b1 << i;
                    if (pieceMoves.CompareTo(check) < 0)
                    {
                        continue;
                    }

                    pieceMoves -= check;
                    moves.Add(piece.GetTile().ToString() + Tile.BinaryToString(check));

                    if (pieceMoves == 0) { break; }
                }
            }
            return moves;
        }

        public ulong GetControlledTiles(int team)
        {
            ulong controlled = 0b0;
            foreach (Piece piece in pieces[team == 1 ? 0 : 1])
            {
                controlled |= piece.ControledTiles();
            }
            return controlled;
        }
        
        public List<Piece> GetRooks(int team)
        {
            List<Piece> rooks = new();
            foreach (Piece piece in pieces[team == 1 ? 0 : 1])
            {
                if (piece.GetType() == 'R')
                {
                    rooks.Add(piece);
                }
            }
            return rooks;
        }

        public Tile GetTile(ulong l) { return board[l % 8, l / 8]; }
        public Tile GetTile(string s) { return board[s[0] - 'a', s[1] - '1']; }
        public Tile GetTile(int x, int y) { return board[x, y]; }
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

        public ulong ToBinary() => (ulong)0b1 << x + 8 * y;

        override public string ToString() => new string(new char[] { (char)(x + 'a'), (char)(y + '1') });

        public static ulong StringToBinary(string s)
        {
            return (ulong)0b1 << (s[0] - 'a') + 8 * (s[1] - '1');
        }

        public static string BinaryToString(ulong l)
        {
            return new string(new char[] { (char)((l % 8) + 'a'), (char)((l / 8) + '1') });
        }
    }

    public class Piece
    {
        private readonly BoardController owner;
        private char type;
        // White 1 Black -1
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

        public ulong GetMoves()
        {
            ulong moves = ControledTiles();
            ulong temp = moves;

            for (int i = 63; i >= 0; i--) 
            {
                ulong check = (ulong)0b1 << i;
                if (temp.CompareTo(check) < 0)
                {
                    continue;
                }

                temp -= check;
                if (!owner.LegalMove(tile.ToString() + Tile.BinaryToString(check)))
                {
                    moves -= check;
                }

                if (temp == 0) { break; }
            }

            if (type == 'P' && owner.LegalMove(tile.ToString() + owner.GetTile(tile.X, tile.Y + 1).ToString()))
            {
                moves |= owner.GetTile(tile.X, tile.Y + 1).ToBinary();
                if (!moved && owner.LegalMove(tile.ToString() + owner.GetTile(tile.X, tile.Y + 2).ToString())) 
                {
                    moves |= owner.GetTile(tile.X, tile.Y + 2).ToBinary();
                }
            }
            else if (type == 'K' && !moved)
            {
                List<Piece> rooks = owner.GetRooks(team);
                foreach (Piece rook in rooks)
                {
                    if (!rook.moved && (owner.GetTile(tile.X + 1, tile.Y).GetPiece() is null && owner.GetTile(tile.X + 2, tile.Y).GetPiece() is null)
                        || (owner.GetTile(tile.X - 1, tile.Y).GetPiece() is null && owner.GetTile(tile.X - 2, tile.Y).GetPiece() is null
                        && owner.GetTile(tile.X - 3, tile.Y).GetPiece() is null))
                    {
                        moves |= owner.GetTile(tile.X + 2 * (rook.GetTile().X - tile.X > 0 ? 1 : -1), tile.Y).ToBinary();
                    }
                }
            }

            return moves;
        }

        public ulong ControledTiles()
        {
            ulong pieceControl = 0b0;
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
                            ulong k = (ulong) 0b1 << (tile.X + i + 8 * (tile.Y + j));
                            pieceControl |= k;
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
                                pieceControl |= t.ToBinary();
                            }
                            if (tile.Y + i < 8 && (control & 0b10000000) == 0b10000000)
                            {
                                t = owner.board[tile.X + i, tile.Y + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b10000000;
                                }
                                pieceControl |= t.ToBinary();
                            }
                            if (tile.Y - i >= 0 && (control & 0b01000000) == 0b01000000)
                            {
                                t = owner.board[tile.X + i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b01000000;
                                }
                                pieceControl |= t.ToBinary();
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
                                pieceControl |= t.ToBinary();
                            }
                            if (tile.Y + i < 8 && (control & 0b00100000) == 0b00100000)
                            {
                                t = owner.board[tile.X - i, tile.Y + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00100000;
                                }
                                pieceControl |= t.ToBinary();
                            }
                            if (tile.Y - i >= 0 && (control & 0b00010000) == 0b00010000)
                            {
                                t = owner.board[tile.X - i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00010000;
                                }
                                pieceControl |= t.ToBinary();
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
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y - i >= 0 && (control & 0b1000) == 0b1000)
                        {
                            t = owner.board[tile.X, tile.Y - i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b1000;
                            }
                            pieceControl |= t.ToBinary();
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
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.X - i >= 0 && (control & 0b0010) == 0b0010)
                        {
                            t = owner.board[tile.X - i, tile.Y];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0010;
                            }
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y + i < 8 && (control & 0b0100) == 0b0100)
                        {
                            t = owner.board[tile.X, tile.Y + i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0100;
                            }
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y - i >= 0 && (control & 0b1000) == 0b1000)
                        {
                            t = owner.board[tile.X, tile.Y - i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b1000;
                            }
                            pieceControl |= t.ToBinary();
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
                                pieceControl |= t.ToBinary();
                            }
                            if (tile.Y - i >= 0 && (control & 0b01000000) == 0b01000000)
                            {
                                t = owner.board[tile.X + i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b01000000;
                                }
                                pieceControl |= t.ToBinary();
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
                                pieceControl |= t.ToBinary();
                            }
                            if (tile.Y - i >= 0 && (control & 0b00010000) == 0b00010000)
                            {
                                t = owner.board[tile.X - i, tile.Y - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00010000;
                                }
                                pieceControl |= t.ToBinary();
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
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y - 1 >= 0)
                        {
                            t = owner.board[tile.X + 2, tile.Y - 1];
                            pieceControl |= t.ToBinary();
                        }
                    }
                    if (tile.X - 2 >= 0)
                    {
                        if (tile.Y + 1 < 8)
                        {
                            t = owner.board[tile.X - 2, tile.Y + 1];
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y - 1 >= 0)
                        {
                            t = owner.board[tile.X - 2, tile.Y - 1];
                            pieceControl |= t.ToBinary();
                        }
                    }
                    if (tile.X + 1 < 8)
                    {
                        if (tile.Y + 2 < 8)
                        {
                            t = owner.board[tile.X + 1, tile.Y + 2];
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y - 2 >= 0)
                        {
                            t = owner.board[tile.X + 1, tile.Y - 2];
                            pieceControl |= t.ToBinary();
                        }
                    }
                    if (tile.X - 1 >= 0)
                    {
                        if (tile.Y + 2 < 8)
                        {
                            t = owner.board[tile.X - 1, tile.Y + 2];
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.Y - 2 >= 0)
                        {
                            t = owner.board[tile.X - 1, tile.Y - 2];
                            pieceControl |= t.ToBinary();
                        }
                    }
                    break;
                case 'P':
                    if (tile.Y + team >= 0 && tile.Y + team < 8)
                    {
                        if (tile.X + 1 < 8)
                        {
                            t = owner.board[tile.X + 1, tile.Y + team];
                            pieceControl |= t.ToBinary();
                        }
                        if (tile.X - 1 >= 0)
                        {
                            t = owner.board[tile.X - 1, tile.Y + team];
                            pieceControl |= t.ToBinary();
                        }
                    }
                    break;
            }
            return pieceControl;
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