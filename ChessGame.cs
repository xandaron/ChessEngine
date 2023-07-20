
using System.Text.RegularExpressions;

public class Game
{
    public int turn = 0;
    public Tile[,] board = new Tile[8, 8];
    public Piece?[][] pieces = new Piece[2][];
    public List<string> moveList = new List<string>();

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

            board[i, 0].setPiece(pieces[0][i]);
            board[i, 1].setPiece(pieces[0][8 + i]);
            board[i, 6].setPiece(pieces[1][8 + i]);
            board[i, 7].setPiece(pieces[1][i]);
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

            string r1 = Regex.Match(input, pawnMove).Value;
            string r2 = Regex.Match(input, pawnCapture).Value;
            string r3 = Regex.Match(input, pieceMove).Value;
            string r4 = Regex.Match(input, check).Value;
            string r5 = Regex.Match(input, capture).Value;

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
            else
            {
                continue;
            }
            Console.WriteLine("Piece: {0}, Move: {1}", piece, move);

            Tile target = board[move[0] - 'a', move[1] - '1'];

            if (r1 != "" || r2 != "")
            {
                for (int i = 8; i < 16; i++)
                {
                    if (pieces[turn % 2][i] is not null
                        && pieces[turn % 2][i]!.getTile().getX() == piece[0] - 'a'
                        && movePiece(pieces[turn % 2][i]!, target))
                    {
                        keepRunning = false;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    if (pieces[turn % 2][i] is not null && pieces[turn % 2][i]!.getType() == piece[0]
                        && ((piece.Length > 1 && (pieces[turn % 2][i]!.getTile().getX() == piece[1] - 'a'
                                                || pieces[turn % 2][i]!.getTile().getY() == piece[1] - '1'))
                          || piece.Length == 1) && movePiece(pieces[turn % 2][i]!, target))
                    {
                        keepRunning = false;
                        break;
                    }
                }
            }
        } while (keepRunning);
        moveList.Add(input!);

        turn++;
        return checkMate();
    }

    // Checks if a move is legal.
    public bool moveCheck(Piece p, Tile t)
    {
        Tile originalTile = p.getTile();
        Piece? originalPiece = t.getPiece();

        p.getTile().clearPiece();
        p.setTile(t);
        t.setPiece(p);

        bool r = checkCheck(p.getTeam());

        p.setTile(originalTile);
        p.getTile().setPiece(p);
        t.setPiece(originalPiece);

        return !r;
    }

    public bool checkCheck(int team)
    {
        List<Tile> moveList = getMoves((team + 1) % 2, false);
        if (moveList.Contains(pieces[team][4]!.getTile()))
        {
            return true;
        }
        return false;
    }

    /* kingMoves is used to decide if legal king moves should be included in the list.
     * This should always be true unless you are checking for check. 
     */
    public List<Tile> getMoves(int team, bool legalMoves)
    {
        List<Tile> moves = new List<Tile>();
        foreach (Piece? piece in pieces[team])
        {
            if (piece is not null)
            {
                moves.AddRange(piece.getMoves(legalMoves));
            }
        }
        return moves;
    }

    // Moves the piece to the target as long as the move is legal
    public bool movePiece(Piece piece, Tile target)
    {
        if (!piece.getMoves(true).Contains(target))
        {
            return false;
        }

        if (target.getPiece() is not null)
        {
            for (int i = 0; i <= 16; i++)
            {
                if (pieces[(turn + 1) % 2][i] == target.getPiece())
                {
                    pieces[(turn + 1) % 2][i] = null;
                    break;
                }
            }
        }

        piece.getTile().clearPiece();
        piece.setTile(target);
        target.setPiece(piece!);

        return true;
    }

    public bool checkMate()
    {
        if (checkCheck(turn % 2) && getMoves(turn % 2, true).Count == 0)
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
                        if (board[j, 7 - (i - 2) / 4].getPiece() is not null)
                        {
                            pieceChar = board[j, 7 - (i - 2) / 4].getPiece()!.getType();

                            if (board[j, 7 - (i - 2) / 4].getPiece()!.getTeam() == 0)
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

    public int getX() { return x; }
    public void setX(int x) { this.x = x; }

    public int getY() { return y; }
    public void setY(int y) { this.y = y; }

    public Piece? getPiece()
    {
        return piece;
    }

    public void setPiece(Piece? piece)
    {
        this.piece = piece;
    }

    public void clearPiece()
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

    public Piece(Game owner, char type, int team, Tile tile)
    {
        this.owner = owner;
        this.type = type;
        this.team = team;
        this.tile = tile;
    }

    public int tileCheck(Tile t)
    {
        if (t.getPiece() is not null)
        {
            if (t.getPiece()!.getTeam() == team)
            {
                return 0;
            }
            return 1;
        }
        return 2;
    }

    public List<Tile> getMoves(bool checkLegality)
    {
        List<Tile> moves = new List<Tile>();
        Tile t;
        int control = 0b11111111;
        bool p = true;

        switch (type)
        {
            case 'K':
                for (int i = Math.Max(0, tile.getX() - 1); i <= Math.Min(tile.getX() + 1, 7); i++)
                {
                    for (int j = Math.Max(0, tile.getY() - 1); j <= Math.Min(tile.getY() + 1, 7); j++)
                    {
                        if (i == tile.getX() && j == tile.getY())
                        {
                            continue;
                        }

                        t = owner.board[i, j];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                break;
            case 'Q':
                for (int i = 1; i < 7; i++)
                {
                    if (tile.getX() + i < 8)
                    {
                        if ((control & 0b0001) == 0b0001)
                        {
                            t = owner.board[tile.getX() + i, tile.getY()];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b0001;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.getY() + i < 8 && (control & 0b10000000) == 0b10000000)
                        {
                            t = owner.board[tile.getX() + i, tile.getY() + i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b10000000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.getY() - i >= 0 && (control & 0b01000000) == 0b01000000)
                        {
                            t = owner.board[tile.getX() + i, tile.getY() - i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b01000000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    if (tile.getX() - i >= 0)
                    {
                        if ((control & 0b0010) == 0b0010)
                        {
                            t = owner.board[tile.getX() - i, tile.getY()];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b0010;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.getY() + i < 8 && (control & 0b00100000) == 0b00100000)
                        {
                            t = owner.board[tile.getX() - i, tile.getY() + i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b00100000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.getY() - i >= 0 && (control & 0b00010000) == 0b00010000)
                        {
                            t = owner.board[tile.getX() - i, tile.getY() - i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b00010000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    if (tile.getY() + i < 8 && (control & 0b0100) == 0b0100)
                    {
                        t = owner.board[tile.getX(), tile.getY() + i];
                        int tc = tileCheck(t);
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (tc != 2)
                        {
                            control -= 0b0100;
                        }
                        if (!checkLegality || (tc != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() - i >= 0 && (control & 0b1000) == 0b1000)
                    {
                        t = owner.board[tile.getX(), tile.getY() - i];
                        int tc = tileCheck(t);
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (tc != 2)
                        {
                            control -= 0b1000;
                        }
                        if (!checkLegality || (tc != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                break;
            case 'R':
                for (int i = 1; i < 7; i++)
                {
                    if (tile.getX() + i < 8 && (control & 0b0001) == 0b0001)
                    {
                        t = owner.board[tile.getX() + i, tile.getY()];
                        int tc = tileCheck(t);
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (tc != 2)
                        {
                            control -= 0b0001;
                        }
                        if (!checkLegality || (tc != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getX() - i >= 0 && (control & 0b0010) == 0b0010)
                    {
                        t = owner.board[tile.getX() - i, tile.getY()];
                        int tc = tileCheck(t);
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (tc != 2)
                        {
                            control -= 0b0010;
                        }
                        if (!checkLegality || (tc != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() + i < 8 && (control & 0b0100) == 0b0100)
                    {
                        t = owner.board[tile.getX(), tile.getY() + i];
                        int tc = tileCheck(t);
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (tc != 2)
                        {
                            control -= 0b0100;
                        }
                        if (!checkLegality || (tc != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() - i >= 0 && (control & 0b1000) == 0b1000)
                    {
                        t = owner.board[tile.getX(), tile.getY() - i];
                        int tc = tileCheck(t);
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (tc != 2)
                        {
                            control -= 0b1000;
                        }
                        if (!checkLegality || (tc != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                break;
            case 'B':
                for (int i = 1; i < 7; i++)
                {
                    if (tile.getX() + i < 8)
                    {
                        if (tile.getY() + i < 8 && (control & 0b10000000) == 0b10000000)
                        {
                            t = owner.board[tile.getX() + i, tile.getY() + i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b10000000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.getY() - i >= 0 && (control & 0b01000000) == 0b01000000)
                        {
                            t = owner.board[tile.getX() + i, tile.getY() - i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b01000000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                    if (tile.getX() - i >= 0)
                    {
                        if (tile.getY() + i < 8 && (control & 0b00100000) == 0b00100000)
                        {
                            t = owner.board[tile.getX() - i, tile.getY() + i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b00100000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                        if (tile.getY() - i >= 0 && (control & 0b00010000) == 0b00010000)
                        {
                            t = owner.board[tile.getX() - i, tile.getY() - i];
                            int tc = tileCheck(t);
                            p = checkLegality == true ? owner.moveCheck(this, t) : true;
                            if (tc != 2)
                            {
                                control -= 0b00010000;
                            }
                            if (!checkLegality || (tc != 0 && p))
                            {
                                moves.Add(t);
                            }
                        }
                    }
                }
                break;
            case 'N':
                if (tile.getX() + 2 < 8)
                {
                    if (tile.getY() + 1 < 8)
                    {
                        t = owner.board[tile.getX() + 2, tile.getY() + 1];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() - 1 >= 0)
                    {
                        t = owner.board[tile.getX() + 2, tile.getY() - 1];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                if (tile.getX() - 2 >= 0)
                {
                    if (tile.getY() + 1 < 8)
                    {
                        t = owner.board[tile.getX() - 2, tile.getY() + 1];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() - 1 >= 0)
                    {
                        t = owner.board[tile.getX() - 2, tile.getY() - 1];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                if (tile.getX() + 1 < 8)
                {
                    if (tile.getY() + 2 < 8)
                    {
                        t = owner.board[tile.getX() + 1, tile.getY() + 2];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() - 2 >= 0)
                    {
                        t = owner.board[tile.getX() + 1, tile.getY() - 2];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                if (tile.getX() - 1 < 8)
                {
                    if (tile.getY() + 2 < 8)
                    {
                        t = owner.board[tile.getX() - 1, tile.getY() + 2];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                    if (tile.getY() - 2 >= 0)
                    {
                        t = owner.board[tile.getX() - 1, tile.getY() - 2];
                        p = checkLegality == true ? owner.moveCheck(this, t) : true;
                        if (!checkLegality || (tileCheck(t) != 0 && p))
                        {
                            moves.Add(t);
                        }
                    }
                }
                break;
            case 'P':
                int direction = team == 0 ? 1 : -1;
                t = owner.board[tile.getX(), tile.getY() + 1 * direction];
                p = checkLegality == true ? owner.moveCheck(this, t) : true;
                if (!checkLegality || (tileCheck(t) == 2 && p))
                {
                    moves.Add(t);
                }
                if ((team == 0 && tile.getY() == 1) || (team == 1 && tile.getY() == 6))
                {
                    t = owner.board[tile.getX(), tile.getY() + 2 * direction];
                    p = checkLegality == true ? owner.moveCheck(this, t) : true;
                    if (!checkLegality || (tileCheck(t) == 2 && p))
                    {
                        moves.Add(t);
                    }
                }
                if (tile.getX() + 1 < 8)
                {
                    t = owner.board[tile.getX() + 1, tile.getY() + 1 * direction];
                    p = checkLegality == true ? owner.moveCheck(this, t) : true;
                    if (!checkLegality || (tileCheck(t) == 1 && p))
                    {
                        moves.Add(t);
                    }
                }
                if (tile.getX() - 1 >= 0)
                {
                    t = owner.board[tile.getX() - 1, tile.getY() + 1 * direction];
                    p = checkLegality == true ? owner.moveCheck(this, t) : true;
                    if (!checkLegality || (tileCheck(t) == 1 && p))
                    {
                        moves.Add(t);
                    }
                }
                break;
        }
        return moves;
    }

    public char getType() { return type; }
    public void setType(char type) { this.type = type; }

    public Tile getTile() { return tile; }
    public void setTile(Tile tile) { this.tile = tile; }

    public int getTeam() { return team; }
}