
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;

namespace ChessGame
{
    public class Game
    {
        private readonly string fen;
        public int turn;
        public int halfMoveCounter;
        public Tile[,] board = new Tile[8, 8];
        public List<Piece>[] pieces = new List<Piece>[] { new List<Piece>(), new List<Piece>() };
        public List<string> moveList = new List<string>();
        public bool enPassant = false;
        public bool checkMate = false;
        public bool gameOver = false;

        public Game(string fen)
        {
            this.fen = fen;

            for (int i = 0; i < 8; i++)
            {
                for (int k = 0; k < 8; k++)
                {
                    board[i, k] = new Tile(i, k);
                }
            }

            LoadFEN();
        }

        public Game() : this("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1") { }

        private void LoadFEN()
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

            DisplayBoard();
            string? input;
            bool keepRunning = true;
            do
            {
                Console.WriteLine("\nNext move:");
                input = Console.ReadLine();

                if (input is null)
                {
                    continue;
                }

                string pawnMove = @"^[abcdefgh][12345678]\+?$";
                string pawnCapture = @"^[abcdefgh]x[abcdefgh][12345678]\+?$";
                string pieceMove = @"^[KQNBR][abcdefgh12345678]?x?[abcdefgh][12345678]\+?$";
                string check = @"\+$";
                string capture = @"x";
                string castle = @"^O-O(-O)?$";

                string r1 = Regex.Match(input, pawnMove).Value;
                string r2 = Regex.Match(input, pawnCapture).Value;
                string r3 = Regex.Match(input, pieceMove).Value;
                string r4 = Regex.Match(input, check).Value;
                string r5 = Regex.Match(input, capture).Value;
                string r6 = Regex.Match(input, castle).Value;

                string piece;
                string move;

                if (r1 != "")
                {
                    piece = r1[..1];
                    move = r1[..2];
                }
                else if (r2 != "")
                {
                    piece = r2[..1];
                    move = r2[2..3];
                }
                else if (r3 != "")
                {
                    piece = r3[..(r3.Length - r4.Length - r5.Length - 2)];
                    move = r3[(r3.Length - r4.Length - 2)..(r3.Length - r4.Length)];
                }
                else if (r6 != "")
                {
                    piece = "K";
                    string side = turn % 2 == 0 ? "1" : "8";
                    if (r6.Length == 3)
                    {
                        move = "g" + side;
                    }
                    else
                    {
                        move = "c" + side;
                    }
                }
                else
                {
                    Console.WriteLine("Enter a valid move in algebraic chess notation");
                    continue;
                }

                Tile target = board[move[0] - 'a', move[1] - '1'];

                if (r1 != "" || r2 != "")
                {
                    foreach (Piece p in pieces[turn % 2])
                    {
                        if (p.GetTile().GetX() == piece[0] - 'a' && MovePiece(p, target))
                        {
                            keepRunning = false;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (Piece p in pieces[turn % 2])
                    {
                        if (p.GetType() == piece[0]  && (piece.Length > 1 && (p.GetTile().GetX() == piece[1] - 'a'
                         || p.GetTile().GetY() == piece[1] - '1') || piece.Length == 1) && MovePiece(p, target))
                        {
                            keepRunning = false;
                            break;
                        }
                    }
                }
            } while (keepRunning);

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

            bool r = CheckCheck(p.GetTeam());

            p.SetTile(originalTile);
            p.GetTile().SetPiece(p);
            t.SetPiece(originalPiece);

            return !r;
        }

        public bool CheckCheck(int team)
        {
            List<Tile> moveList = GetMoves((team + 1) % 2, false);
            Tile? kingTile = null;

            foreach (Piece p in pieces[team])
            {
                if (p.GetType() == 'K')
                {
                    kingTile = p.GetTile();
                    break;
                }
            }

            if (!moveList.Contains(kingTile!))
            {
                return false;
            }
            return true;
        }

        /* kingMoves is used to decide if legal king moves should be included in the list.
         * This should always be true unless you are checking for check. 
         */
        public List<Tile> GetMoves(int team, bool legalMoves)
        {
            List<Tile> moves = new();
            foreach (Piece piece in pieces[team])
            {
                moves.AddRange(piece.GetMoves(legalMoves));
            }
            return moves;
        }

        // Moves the piece to the target as long as the move is legal
        public bool MovePiece(Piece piece, Tile target)
        {
            if (!piece.GetMoves(true).Contains(target)) { return false; }

            string moveString = "";

            // Pawn specific moves
            if (piece.GetType() == 'P')
            {
                halfMoveCounter = 0;
                // Pawn capture character
                if (piece.GetTile().GetX() != target.GetX())
                {
                    moveString += (char)(piece.GetTile().GetX() + 'a');
                }

                // En Passant capture
                if (target.GetPiece() == null && Math.Abs(target.GetX() - piece.GetTile().GetX()) == 1)
                {
                    int d = piece.GetTeam() == 0 ? -1 : 1;
                    pieces[(turn + 1) % 2].Remove(board[target.GetX(), target.GetY() + d].GetPiece()!);
                    board[target.GetX(), target.GetY() + d].ClearPiece();
                }

                // Pawn promotion
                if (piece.GetTeam() == 0 && target.GetY() == 7 || piece.GetTeam() == 1 && target.GetY() == 0)
                {
                    while (true)
                    {
                        string? input;
                        Console.WriteLine("Pawn promotion (Q, R, B, N):");
                        input = Console.ReadLine();
                        if (input == "Q" || input == "R" || input == "B" || input == "N")
                        {
                            piece.SetType(input[0]);
                            break;
                        }
                    }
                }
            }
            else
            {
                moveString += piece.GetType();
            }

            foreach (Piece p in pieces[piece.GetTeam()])
            {
                if (p != piece && p.GetType() == piece.GetType() && p.GetMoves(true).Contains(target))
                {
                    if (p.GetTile().GetX() == piece.GetTile().GetX())
                    {
                        moveString += (char)(piece.GetTile().GetY() + '1');
                    }
                    else
                    {
                        moveString += (char)(piece.GetTile().GetX() + 'a');
                    }
                }
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

            moveString += (char)(target.GetX() + 'a');
            moveString += (char)(target.GetY() + '1');

            piece.GetTile().ClearPiece();
            piece.SetTile(target);
            target.SetPiece(piece);
            piece.SetMoved(true);

            // Castle
            if (piece.GetType() == 'K' && Math.Abs(piece.GetTile().GetX() - target.GetX()) == 2)
            {
                board[target.GetX() == 6 ? 7 : 0, target.GetY()].GetPiece()!.SetTile(board[target.GetX() == 6 ? 5 : 3, target.GetY()]);
                board[target.GetX() == 6 ? 5 : 3, target.GetY()].SetPiece(board[target.GetX() == 6 ? 7 : 0, target.GetY()].GetPiece()!);
                board[target.GetX() == 6 ? 7 : 0, target.GetY()].ClearPiece();
                moveString = "O-O" + (target.GetX() == 2 ? "-O" : "");
            }

            if (CheckCheck((turn + 1) % 2))
            {
                moveString += '+';
            }

            // En Passant
            if (piece.GetType() == 'P' && Math.Abs(piece.GetTile().GetY() - target.GetY()) == 2)
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
            if (CheckCheck(turn % 2) && GetMoves(turn % 2, true).Count == 0)
            {
                return true;
            }
            return false;
        }

        public void DisplayBoard()
        {
            Console.WriteLine();
            for (int i = 0; i < 33; i++)
            {
                int mod = i % 4;
                switch (mod)
                {
                    case 0:
                        Console.Write("   ");
                        for (int j = 0; j < 8; j++)
                        {
                            Console.Write("*-----");
                        }
                        Console.Write("* \n");
                        break;
                    case 1:
                        Console.Write("   ");
                        for (int j = 0; j < 8; j++)
                        {
                            Console.Write("|     ");
                        }
                        Console.Write("| \n");
                        break;
                    case 2:
                        Console.Write(" {0} ", 8 - (i - 2) / 4);
                        for (int j = 0; j < 8; j++)
                        {
                            char pieceChar = ' ';
                            Console.Write("|  ");
                            if (board[j, 7 - (i - 2) / 4].GetPiece() is not null)
                            {
                                pieceChar = board[j, 7 - (i - 2) / 4].GetPiece()!.GetType();

                                if (board[j, 7 - (i - 2) / 4].GetPiece()!.GetTeam() == 0)
                                {
                                    Console.ForegroundColor = ConsoleColor.White;
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Black;
                                }
                            }
                            Console.Write(pieceChar + "  ");
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }
                        Console.Write("| \n");
                        break;
                    case 3:
                        Console.Write("   ");
                        for (int j = 0; j < 8; j++)
                        {
                            Console.Write("|     ");
                        }
                        Console.Write("| \n");
                        break;
                }
            }
            Console.Write(" ");
            for (int i = 0; i < 8; i++)
            {
                Console.Write("     {0}", (char)('a' + i));
            }
            Console.WriteLine();
        }

        public void PrintMoves()
        {
            for (int i = 0; i < moveList.Count; i++)
            {
                Console.Write("{0} ", moveList[i]);
                for (int j = 0; j < 6 - moveList[i].Length; j++)
                {
                    Console.Write(" ");
                }
                if (i % 2 == 1)
                {
                    Console.Write("\n");
                }
            }
            Console.Write("\n");
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

        public int GetX() { return x; }
        public void SetX(int x) { this.x = x; }

        public int GetY() { return y; }
        public void SetY(int y) { this.y = y; }

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

        override public string ToString() => new string((char)(x + 'a'), (char)(y + '1'));
    }

    public class Piece
    {
        private readonly Game owner;
        private char type;
        private readonly int team;
        private Tile tile;
        private bool moved;

        public Piece(Game owner, char type, int team, Tile tile, bool moved)
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

        public List<Tile> GetMoves(bool checkLegality)
        {
            List<Tile> moves = new();
            Tile t;
            int control = 0b11111111;

            switch (type)
            {
                case 'K':
                    for (int i = Math.Max(0, tile.GetX() - 1); i <= Math.Min(tile.GetX() + 1, 7); i++)
                    {
                        for (int j = Math.Max(0, tile.GetY() - 1); j <= Math.Min(tile.GetY() + 1, 7); j++)
                        {
                            if (i == tile.GetX() && j == tile.GetY())
                            {
                                continue;
                            }

                            t = owner.board[i, j];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }

                    if (checkLegality && !moved && !owner.CheckCheck(team))
                    {
                        List<Tile> opponentMoves = owner.GetMoves(team == 0 ? 1 : 0, false);
                        if (owner.board[0, team == 0 ? 0 : 7].GetPiece() is not null
                            && !owner.board[0, team == 0 ? 0 : 7].GetPiece()!.GetMoved()
                            && TileCheck(owner.board[3, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[3, team == 0 ? 0 : 7])
                            && TileCheck(owner.board[2, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[2, team == 0 ? 0 : 7]))
                        {
                            moves.Add(owner.board[2, team == 0 ? 0 : 7]);
                        }
                        if (owner.board[7, team == 0 ? 0 : 7].GetPiece() is not null
                            && !owner.board[7, team == 0 ? 0 : 7].GetPiece()!.GetMoved()
                            && TileCheck(owner.board[5, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[5, team == 0 ? 0 : 7])
                            && TileCheck(owner.board[6, team == 0 ? 0 : 7]) == 2
                            && !opponentMoves.Contains(owner.board[6, team == 0 ? 0 : 7]))
                        {
                            moves.Add(owner.board[6, team == 0 ? 0 : 7]);
                        }
                    }

                    break;
                case 'Q':
                    for (int i = 1; i < 7; i++)
                    {
                        if (tile.GetX() + i < 8)
                        {
                            if ((control & 0b0001) == 0b0001)
                            {
                                t = owner.board[tile.GetX() + i, tile.GetY()];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b0001;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                            if (tile.GetY() + i < 8 && (control & 0b10000000) == 0b10000000)
                            {
                                t = owner.board[tile.GetX() + i, tile.GetY() + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b10000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                            if (tile.GetY() - i >= 0 && (control & 0b01000000) == 0b01000000)
                            {
                                t = owner.board[tile.GetX() + i, tile.GetY() - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b01000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                        }
                        if (tile.GetX() - i >= 0)
                        {
                            if ((control & 0b0010) == 0b0010)
                            {
                                t = owner.board[tile.GetX() - i, tile.GetY()];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b0010;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                            if (tile.GetY() + i < 8 && (control & 0b00100000) == 0b00100000)
                            {
                                t = owner.board[tile.GetX() - i, tile.GetY() + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00100000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                            if (tile.GetY() - i >= 0 && (control & 0b00010000) == 0b00010000)
                            {
                                t = owner.board[tile.GetX() - i, tile.GetY() - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00010000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                        }
                        if (tile.GetY() + i < 8 && (control & 0b0100) == 0b0100)
                        {
                            t = owner.board[tile.GetX(), tile.GetY() + i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0100;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() - i >= 0 && (control & 0b1000) == 0b1000)
                        {
                            t = owner.board[tile.GetX(), tile.GetY() - i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b1000;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    break;
                case 'R':
                    for (int i = 1; i < 7; i++)
                    {
                        if (tile.GetX() + i < 8 && (control & 0b0001) == 0b0001)
                        {
                            t = owner.board[tile.GetX() + i, tile.GetY()];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0001;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetX() - i >= 0 && (control & 0b0010) == 0b0010)
                        {
                            t = owner.board[tile.GetX() - i, tile.GetY()];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0010;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() + i < 8 && (control & 0b0100) == 0b0100)
                        {
                            t = owner.board[tile.GetX(), tile.GetY() + i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b0100;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() - i >= 0 && (control & 0b1000) == 0b1000)
                        {
                            t = owner.board[tile.GetX(), tile.GetY() - i];
                            int tc = TileCheck(t);
                            if (tc != 2)
                            {
                                control -= 0b1000;
                            }
                            if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    break;
                case 'B':
                    for (int i = 1; i < 7; i++)
                    {
                        if (tile.GetX() + i < 8)
                        {
                            if (tile.GetY() + i < 8 && (control & 0b10000000) == 0b10000000)
                            {
                                t = owner.board[tile.GetX() + i, tile.GetY() + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b10000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                            if (tile.GetY() - i >= 0 && (control & 0b01000000) == 0b01000000)
                            {
                                t = owner.board[tile.GetX() + i, tile.GetY() - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b01000000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                        }
                        if (tile.GetX() - i >= 0)
                        {
                            if (tile.GetY() + i < 8 && (control & 0b00100000) == 0b00100000)
                            {
                                t = owner.board[tile.GetX() - i, tile.GetY() + i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00100000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                            if (tile.GetY() - i >= 0 && (control & 0b00010000) == 0b00010000)
                            {
                                t = owner.board[tile.GetX() - i, tile.GetY() - i];
                                int tc = TileCheck(t);
                                if (tc != 2)
                                {
                                    control -= 0b00010000;
                                }
                                if (!checkLegality || tc != 0 && owner.MoveCheck(this, t))
                                {
                                    moves.Add(t);
                                }
                            }
                        }
                    }
                    break;
                case 'N':
                    if (tile.GetX() + 2 < 8)
                    {
                        if (tile.GetY() + 1 < 8)
                        {
                            t = owner.board[tile.GetX() + 2, tile.GetY() + 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() - 1 >= 0)
                        {
                            t = owner.board[tile.GetX() + 2, tile.GetY() - 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    if (tile.GetX() - 2 >= 0)
                    {
                        if (tile.GetY() + 1 < 8)
                        {
                            t = owner.board[tile.GetX() - 2, tile.GetY() + 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() - 1 >= 0)
                        {
                            t = owner.board[tile.GetX() - 2, tile.GetY() - 1];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    if (tile.GetX() + 1 < 8)
                    {
                        if (tile.GetY() + 2 < 8)
                        {
                            t = owner.board[tile.GetX() + 1, tile.GetY() + 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() - 2 >= 0)
                        {
                            t = owner.board[tile.GetX() + 1, tile.GetY() - 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    if (tile.GetX() - 1 < 8)
                    {
                        if (tile.GetY() + 2 < 8)
                        {
                            t = owner.board[tile.GetX() - 1, tile.GetY() + 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.GetY() - 2 >= 0)
                        {
                            t = owner.board[tile.GetX() - 1, tile.GetY() - 2];
                            if (!checkLegality || TileCheck(t) != 0 && owner.MoveCheck(this, t))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    break;
                case 'P':
                    int direction = team == 0 ? 1 : -1;
                    t = owner.board[tile.GetX(), tile.GetY() + direction];
                    if (checkLegality && TileCheck(t) == 2 && owner.MoveCheck(this, t))
                    {
                        moves.Add(t);
                    }
                    if (checkLegality && !moved)
                    {
                        t = owner.board[tile.GetX(), tile.GetY() + 2 * direction];
                        if (TileCheck(t) == 2 && owner.MoveCheck(this, t))
                        {
                            moves.Add(t);
                        }
                    }

                    if (tile.GetX() + 1 < 8)
                    {
                        t = owner.board[tile.GetX() + 1, tile.GetY() + direction];
                        if (!checkLegality || owner.MoveCheck(this, t) && (TileCheck(t) == 1
                        || owner.enPassant && owner.moveList[^1][0] - 'a' - 1 == tile.GetX()))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.GetX() - 1 >= 0)
                    {
                        t = owner.board[tile.GetX() - 1, tile.GetY() + direction];
                        if (!checkLegality || owner.MoveCheck(this, t) && (TileCheck(t) == 1
                        || owner.enPassant && owner.moveList[^1][0] - 'a' + 1 == tile.GetX()))
                        {
                            moves.Add(t);
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