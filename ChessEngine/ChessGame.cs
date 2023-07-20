
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

public class Game
{
    public int turn = 0;
    public Tile[,] board = new Tile[8, 8];
    public Piece?[][] pieces = new Piece[2][];
    public List<string> moveList = new List<string>();
    public bool enPassant = false;

    public Game()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int k = 0; k < 8; k++)
            {
                board[i, k] = new Tile(i, k);
            }
        }

        pieces[0] = new Piece[16];
        pieces[1] = new Piece[16];

        //White Pieces
        pieces[0][0] = new Piece(this, 'R', 0, board[0, 0]);
        pieces[0][1] = new Piece(this, 'N', 0, board[1, 0]);
        pieces[0][2] = new Piece(this, 'B', 0, board[2, 0]);
        pieces[0][3] = new Piece(this, 'Q', 0, board[3, 0]);
        pieces[0][4] = new Piece(this, 'K', 0, board[4, 0]);
        pieces[0][5] = new Piece(this, 'B', 0, board[5, 0]);
        pieces[0][6] = new Piece(this, 'N', 0, board[6, 0]);
        pieces[0][7] = new Piece(this, 'R', 0, board[7, 0]);

        //Black Pieces
        pieces[1][0] = new Piece(this, 'R', 1, board[0, 7]);
        pieces[1][1] = new Piece(this, 'N', 1, board[1, 7]);
        pieces[1][2] = new Piece(this, 'B', 1, board[2, 7]);
        pieces[1][3] = new Piece(this, 'Q', 1, board[3, 7]);
        pieces[1][4] = new Piece(this, 'K', 1, board[4, 7]);
        pieces[1][5] = new Piece(this, 'B', 1, board[5, 7]);
        pieces[1][6] = new Piece(this, 'N', 1, board[6, 7]);
        pieces[1][7] = new Piece(this, 'R', 1, board[7, 7]);

        for (int i = 0; i < 8; i++)
        {
            pieces[0][8 + i] = new Piece(this, 'P', 0, board[i, 1]);
            pieces[1][8 + i] = new Piece(this, 'P', 1, board[i, 6]);

            board[i, 0].SetPiece(pieces[0][i]);
            board[i, 1].SetPiece(pieces[0][8 + i]);
            board[i, 6].SetPiece(pieces[1][8 + i]);
            board[i, 7].SetPiece(pieces[1][i]);
        }
    }

    public bool update()
    {
        displayBoard();
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
                piece = r1.Substring(0, 1);
                move = r1.Substring(0, 2);
            }
            else if (r2 != "")
            {
                piece = r2.Substring(0, 1);
                move = r2.Substring(2, 2);
            }
            else if (r3 != "")
            {
                piece = r3.Substring(0, r3.Length - r4.Length - r5.Length - 2);
                move = r3.Substring(r3.Length - r4.Length - 2, 2);
            }
            else if (r6 != "")
            {
                piece = "K";
                string side = (turn % 2) == 0 ? "1" : "8";
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
                continue;
            }

            Tile target = board[move[0] - 'a', move[1] - '1'];

            if (r1 != "" || r2 != "")
            {
                for (int i = 8; i < 16; i++)
                {
                    pieces[turn % 2][i] = pieces[turn % 2][i];
                    if (pieces[turn % 2][i] is not null && pieces[turn % 2][i]!.GetTile().GetX() == piece[0] - 'a'
                        && movePiece(pieces[turn % 2][i]!, target))
                    {
                        keepRunning = false;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    if (pieces[turn % 2][i] is not null && pieces[turn % 2][i]!.GetType() == piece[0]
                        && ((piece.Length > 1 && (pieces[turn % 2][i]!.GetTile().GetX() == piece[1] - 'a'
                                                || pieces[turn % 2][i]!.GetTile().GetY() == piece[1] - '1'))
                          || piece.Length == 1) && movePiece(pieces[turn % 2][i]!, target))
                    {
                        keepRunning = false;
                        break;
                    }
                }
            }
        } while (keepRunning);

        turn++;
        return checkMate();
    }

    // Checks if a move is legal.
    public bool moveCheck(Piece p, Tile t)
    {
        Tile originalTile = p.GetTile();
        Piece? originalPiece = t.GetPiece();

        p.GetTile().ClearPiece();
        p.SetTile(t);
        t.SetPiece(p);

        bool r = checkCheck(p.GetTeam());

        p.SetTile(originalTile);
        p.GetTile().SetPiece(p);
        t.SetPiece(originalPiece);

        return !r;
    }

    public bool checkCheck(int team)
    {
        List<Tile> moveList = GetMoves((team + 1) % 2, false);
        if (moveList.Contains(pieces[team][4]!.GetTile()))
        {
            return true;
        }
        return false;
    }

    /* kingMoves is used to decide if legal king moves should be included in the list.
     * This should always be true unless you are checking for check. 
     */
    public List<Tile> GetMoves(int team, bool legalMoves)
    {
        List<Tile> moves = new List<Tile>();
        foreach (Piece? piece in pieces[team])
        {
            if (piece is not null)
            {
                moves.AddRange(piece.GetMoves(legalMoves));
            }
        }
        return moves;
    }

    // Moves the piece to the target as long as the move is legal
    public bool movePiece(Piece piece, Tile target)
    {
        if (!piece.GetMoves(true).Contains(target))
        {
            Console.WriteLine("Illegal move");
            return false;
        }

        string moveString = "";

        if (piece.GetType() == 'P' && piece.GetTile().GetX() != target.GetX())
        {
            moveString += (char)(piece.GetTile().GetX() + 'a');
        }
        else if (piece.GetType() != 'P')
        {
            moveString += piece.GetType();
        }

        foreach (Piece? p in pieces[piece.GetTeam()])
        {
            if (p is not null && p != piece
                && p!.GetType() == piece.GetType() && p.GetMoves(true).Contains(target))
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
            for (int i = 0; i <= 16; i++)
            {
                if (pieces[(turn + 1) % 2][i] == target.GetPiece())
                {
                    pieces[(turn + 1) % 2][i] = null;
                    break;
                }
            }
        }

        if (piece.GetType() == 'P' && target.GetPiece() == null
            && Math.Abs(target.GetX() - piece.GetTile().GetX()) == 1)
        {
            int a = piece.GetTeam() == 0 ? -1 : 1;
            for (int i = 0; i <= 16; i++)
            {
                if (pieces[(turn + 1) % 2][i] == board[target.GetX(), target.GetY() + a].GetPiece())
                {
                    pieces[(turn + 1) % 2][i] = null;
                    break;
                }
            }
            board[target.GetX(), target.GetY() + a].ClearPiece();
        }

        // En Pessent
        if (piece.GetType() == 'P' && Math.Abs(piece.GetTile().GetY() - target.GetY()) == 2)
        {
            enPassant = true;
        }
        else
        {
            enPassant = false;
        }

        // Castle
        if (piece.GetType() == 'K' && Math.Abs(piece.GetTile().GetX() - target.GetX()) == 2) {
            if (target.GetX() == 6)
            {
                board[7, piece.GetTeam() == 0 ? 0 : 7].ClearPiece();
                board[5, piece.GetTeam() == 0 ? 0 : 7].SetPiece(pieces[piece.GetTeam()][7]!);
                pieces[piece.GetTeam()][7]!.SetTile(board[5, piece.GetTeam() == 0 ? 0 : 7]);
                pieces[piece.GetTeam()][7]!.SetMoved(true);
            }
            else
            {
                board[0, piece.GetTeam() == 0 ? 0 : 7].ClearPiece();
                board[3, piece.GetTeam() == 0 ? 0 : 7].SetPiece(pieces[piece.GetTeam()][0]!);
                pieces[piece.GetTeam()][0]!.SetTile(board[3, piece.GetTeam() == 0 ? 0 : 7]);
                pieces[piece.GetTeam()][7]!.SetMoved(true);
            }
        }

        piece.GetTile().ClearPiece();
        piece.SetTile(target);
        target.SetPiece(piece!);
        piece.SetMoved(true);

        moveString += (char)(target.GetX() + 'a');
        moveString += (char)(target.GetY() + '1');

        // Pawn promotion
        if (piece.GetType() == 'P' && ((piece.GetTeam() == 0 && target.GetY() == 7) ||
                                       (piece.GetTeam() == 1 && target.GetY() == 0)))
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

        if (checkCheck((turn + 1) % 2))
        {
            moveString += '+';
        }

        moveList.Add(moveString);
        return true;
    }

    public bool checkMate()
    {
        if (checkCheck(turn % 2) && GetMoves(turn % 2, true).Count == 0)
        {
            return true;
        }
        return false;
    }

    public void displayBoard()
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
                    Console.Write(" {0} ", (8 - (i - 2) / 4));
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

    public void printMoves()
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
}

public class Piece
{
    Game owner;
    private char type;
    private int team;
    private Tile tile;
    private bool moved = false;

    public Piece(Game owner, char type, int team, Tile tile)
    {
        this.owner = owner;
        this.type = type;
        this.team = team;
        this.tile = tile;
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
        List<Tile> moves = new List<Tile>();
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
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                }

                if (checkLegality && !moved && !owner.checkCheck(team))
                {
                    List<Tile> opponentMoves = owner.GetMoves(team == 0 ? 1 : 0, false);
                    if (owner.pieces[team][0] is not null && !owner.pieces[team][0]!.GetMoved()
                        && TileCheck(owner.board[3, team == 0 ? 0 : 7]) == 2
                        && !opponentMoves.Contains(owner.board[3, team == 0 ? 0 : 7])
                        && TileCheck(owner.board[2, team == 0 ? 0 : 7]) == 2
                        && !opponentMoves.Contains(owner.board[2, team == 0 ? 0 : 7]))
                    {
                        moves.Add(owner.board[2, team == 0 ? 0 : 7]);
                    }
                    if (owner.pieces[team][7] is not null && !owner.pieces[team][7]!.GetMoved()
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                            if (!checkLegality || (tc != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.GetY() - 1 >= 0)
                    {
                        t = owner.board[tile.GetX() + 2, tile.GetY() - 1];
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.GetY() - 1 >= 0)
                    {
                        t = owner.board[tile.GetX() - 2, tile.GetY() - 1];
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.GetY() - 2 >= 0)
                    {
                        t = owner.board[tile.GetX() + 1, tile.GetY() - 2];
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
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
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.GetY() - 2 >= 0)
                    {
                        t = owner.board[tile.GetX() - 1, tile.GetY() - 2];
                        if (!checkLegality || (TileCheck(t) != 0 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                }
                break;
            case 'P':
                int direction = team == 0 ? 1 : -1;
                if (tile.GetY() + 1 * direction < 8 && tile.GetY() + 1 * direction >= 0)
                {
                    t = owner.board[tile.GetX(), tile.GetY() + 1 * direction];
                    if (!checkLegality || (TileCheck(t) == 2 && owner.moveCheck(this, t)))
                    {
                        moves.Add(t);
                    }
                    if (!moved)
                    {
                        t = owner.board[tile.GetX(), tile.GetY() + 2 * direction];
                        if (!checkLegality || (TileCheck(t) == 2 && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }

                    if (tile.GetX() + 1 < 8)
                    {
                        t = owner.board[tile.GetX() + 1, tile.GetY() + 1 * direction];
                        if (!checkLegality || (((owner.enPassant && (owner.moveList[owner.moveList.Count() - 1][0] - 'a') - 1 == tile.GetX())
                                                || TileCheck(t) == 1) && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.GetX() - 1 >= 0)
                    {
                        t = owner.board[tile.GetX() - 1, tile.GetY() + 1 * direction];
                        if (!checkLegality || (((owner.enPassant && (owner.moveList[owner.moveList.Count() - 1][0] - 'a') + 1 == tile.GetX())
                                                || TileCheck(t) == 1) && owner.moveCheck(this, t)))
                        {
                            moves.Add(t);
                        }
                    }
                }
                break;
        }
        return moves;
    }

    public new char GetType() { return type; }
    public void SetType(char type) { this.type = type; }

    public Tile GetTile() { return tile; }
    public void SetTile(Tile tile) { this.tile = tile; }

    public int GetTeam() { return team; }

    public bool GetMoved() { return moved; }
    public void SetMoved(bool moved) { this.moved = moved; }
}