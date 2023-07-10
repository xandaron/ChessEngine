
using System.Text.RegularExpressions;

public static class Program
{
    public static Game game = new Game();
    static void Main()
    {
        Console.BackgroundColor = ConsoleColor.DarkBlue; Console.ForegroundColor = ConsoleColor.Gray;
        bool checkmate = false;
        while (!checkmate)
        {
            checkmate = game.update();
        }

        game.displayBoard();
        Console.WriteLine("CHECKMATE!!! {0} WINS", game.turn == 0? "Black" : "White");
    }
}

public class Game
{
    public int turn = 0;
    public Tile[,] board = new Tile[8, 8];
    public Piece?[][] pieces = new Piece[2][];

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
        board[0, 0].piece = new Piece('R', 0, board[0, 0]);
        board[1, 0].piece = new Piece('N', 0, board[1, 0]);
        board[2, 0].piece = new Piece('B', 0, board[2, 0]);
        board[3, 0].piece = new Piece('Q', 0, board[3, 0]);
        board[4, 0].piece = new Piece('K', 0, board[4, 0]);
        board[5, 0].piece = new Piece('B', 0, board[5, 0]);
        board[6, 0].piece = new Piece('N', 0, board[6, 0]);
        board[7, 0].piece = new Piece('R', 0, board[7, 0]);

        //Black Pieces
        board[0, 7].piece = new Piece('R', 1, board[0, 7]);
        board[1, 7].piece = new Piece('N', 1, board[1, 7]);
        board[2, 7].piece = new Piece('B', 1, board[2, 7]);
        board[3, 7].piece = new Piece('Q', 1, board[3, 7]);
        board[4, 7].piece = new Piece('K', 1, board[4, 7]);
        board[5, 7].piece = new Piece('B', 1, board[5, 7]);
        board[6, 7].piece = new Piece('N', 1, board[6, 7]);
        board[7, 7].piece = new Piece('R', 1, board[7, 7]);

        for (int i = 0; i < 8; i++)
        {
            pieces[0][8 + i] = board[i, 0].piece!;
            pieces[1][8 + i] = board[i, 7].piece!;
        }

        for (int i = 0; i < 8; i++)
        {
            board[i, 1].piece = new Piece('P', 0, board[i, 1]);
            board[i, 6].piece = new Piece('P', 1, board[i, 6]);
            pieces[0][i] = board[i, 1].piece!;
            pieces[1][i] = board[i, 6].piece!;
        }
    }

    public struct Tile
    {
        public int x;
        public int y;
        public Piece? piece = null;

        public Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Tile t)
        {
            if (x == t.x && y == t.y) return true;
            return false;
        }
    }

    public bool update()
    {
        displayBoard();
        string? input;
        string move;
        string piece;
        bool keepRunning = true;
        do
        {
            Console.WriteLine("Next move:");
            input = Console.ReadLine();

            if (input is null)
            {
                continue;
            }
            string pieceString = @"^[KQNBR][abcdefgh12345678]?";
            string moveString = @"[abcdefgh][12345678]\+?$";
            string movePawn = @"^[abcdefgh][12345678]\+?$";
            string pawnCapture = @"^[abcdefgh]x?[abcdefgh][12345678]\+?$";
            string pieceMove = @"^[KQNBR][abcdefgh12345678]?x?[abcdefgh][12345678]\+?$";

            string r1 = Regex.Match(input, pieceString).Value;
            string r2 = Regex.Match(input, moveString).Value;
            string r3 = Regex.Match(input, movePawn).Value;
            string r4 = Regex.Match(input, pawnCapture).Value;
            string r5 = Regex.Match(input, pieceMove).Value;

            if (r4 != "")
            {
                piece = r4.Substring(0, 1);
                move = r2.Substring(0, 2);

            }
            else if (r3 != "")
            {
                piece = r3.Substring(0, 1);
                move = r3.Substring(0, 2);
            }
            else if (r2 != "" && r1 != "")
            {
                piece = r1.Substring(0, 1);
                move = r2.Substring(0, 2);
            }
            else
            {
                continue;
            }
            Console.WriteLine("Piece: " + piece + " Move: " + move);

            int tileX = move[0] - 'a';
            int tileY = move[1] - '1';
            Tile t = board[tileX, tileY];
            Console.WriteLine("Tile coord ({0},{1})", t.x, t.y);

            char[] validPieceLetters = new char[] { 'K', 'Q', 'N', 'B', 'R' };
            char[] validPawnLetters = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };

            if (validPawnLetters.Contains(piece[0]))
            {
                int x = piece[0] - 'a';
                for (int i = 0; i < 8; i++)
                {
                    if (pieces[turn % 2][i] is not null
                        && pieces[turn % 2][i]!.type == 'P'
                        && pieces[turn % 2][i]!.tile.x == x)
                    {
                        if (movePiece(pieces[turn % 2][i]!, t))
                        {
                            keepRunning = false;
                            break;
                        }
                    }
                }
            }
            if (validPieceLetters.Contains(piece[0]))
            {
                if (r1.Length > 1)
                {
                    if (r1[1] - 'a' >= 0 && r1[1] - 'a' < 8)
                    {
                        for (int i = 8; i < 16; i++)
                        {
                            if (pieces[turn % 2][i] is not null
                                && pieces[turn % 2][i]!.type == piece[0]
                                && pieces[turn % 2][i]!.tile.x == r1[1] - 'a')
                            {
                                if (movePiece(pieces[turn % 2][i]!, t))
                                {
                                    keepRunning = false;
                                    break;
                                }
                            }
                        }
                    }
                    else if (r1[1] - '1' >= 0 && r1[1] - '1' < 8)
                    {
                        for (int i = 8; i < 16; i++)
                        {
                            if (pieces[turn % 2][i] is not null
                                && pieces[turn % 2][i]!.type == piece[0]
                                && pieces[turn % 2][i]!.tile.y == r1[1] - '1')
                            {
                                if (movePiece(pieces[turn % 2][i]!, t))
                                {
                                    keepRunning = false;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 8; i < 16; i++)
                    {
                        if (pieces[turn % 2][i] is not null
                            && pieces[turn % 2][i]!.type == piece[0])
                        {
                            if (movePiece(pieces[turn % 2][i]!, t))
                            {
                                keepRunning = false;
                                break;
                            }
                        }
                    }
                }
            }
        } while (keepRunning);

        turn++;
        return checkMateCheck();
    }

    // Moves the piece to the target if it can
    public bool movePiece(Piece piece, Tile target)
    {
        if (!piece.getMoves().Contains(target))
        {
            return false;
        }

        string move = "";
        if (piece.type == 'P')
        {
            move += (char)(piece.tile.x + 'a');
        }
        else
        {
            move += piece.type;
            for (int i = 0; i < 8; i++)
            {
                if (Program.game.pieces[piece.team][8 + i] == piece
                    && Program.game.pieces[piece.team][15 - i] != null)
                {
                    if (Program.game.pieces[piece.team][15 - i]!.tile.x == piece.tile.x)
                    {
                        move += (char)(piece.tile.x + 'a');
                    }
                    else if (Program.game.pieces[piece.team][15 - i]!.tile.y == piece.tile.y)
                    {
                        move += (char)(piece.tile.y + '0');
                    }
                }
            }
        }

        if (target.piece is not null)
        {
            move += 'x';
            move += (char)(target.x + 'a') + (char)(target.y + '0');
            for (int i = 0; i <= 16; i++)
            {
                if (pieces[1 - (turn % 2)][i] == target.piece)
                {
                    pieces[1 - (turn % 2)][i] = null;
                    break;
                }
            }
        }
        else
        {
            if (piece.type == 'P')
            {
                move += (char)(target.y + '1');
            }
            else
            {
                move += (char)(target.x + 'a');
                move += (char)(target.y + '1');
            }
        }

        board[piece.tile.x, piece.tile.y].piece = null;
        piece.tile = target;
        board[target.x, target.y].piece = piece;

        if (checkForCheck((turn + 1) % 2))
        {
            move += '+';
        }

        Console.WriteLine("Move: {0}", move);
        return true;
    }

    // Checks if the current player is in checkmate
    public bool checkMateCheck()
    {
        if (checkForCheck(turn % 2))
        {
            List<Tile> moves = moveList(turn % 2);
            for (int i = 0; i < moves.Count; i++)
            {
                Tile tile = moves[i];
                Piece? originalPiece = null;

                if (tile.piece is not null)
                {
                    originalPiece = tile.piece;
                }

                tile.piece = new Piece('P', turn % 2, tile);
                bool check = !checkForCheck(turn % 2);
                tile.piece = originalPiece;

                if (check)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

    // Checks if the provided team is in check
    public bool checkForCheck(int team)
    {
        if (moveList(1 - team).Contains(pieces[team][12]!.tile))
        {
            return true;
        }
        return false;
    }

    public List<Tile> moveList(int team)
    {
        List<Tile> moves = new List<Tile>();
        foreach (Piece? piece in pieces[team])
        {
            if (piece is not null)
            {
                moves.AddRange(piece.getMoves());
            }
        }
        return moves;
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
                        if (board[j, 7 - (i - 2) / 4].piece is not null)
                        {
                            pieceChar = board[j, 7 - (i - 2) / 4].piece!.type;

                            if (board[j, 7 - (i - 2) / 4].piece!.team == 0)
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

public class Piece
{
    public char type;
    public int team;
    public Game.Tile tile;

    public Piece(char type, int team, Game.Tile tile)
    {
        this.type = type;
        this.team = team;
        this.tile = tile;
    }

    public List<Game.Tile> getMoves()
    {
        List<Game.Tile> ts = new List<Game.Tile>();

        if (Program.game.turn % 2 == team)
        {
            tile.piece = null;
            bool check = Program.game.checkForCheck(team);
            tile.piece = this;

            if (check)
            {
                return ts;
            }
        }

        bool u = true;
        bool d = true;
        bool l = true;
        bool r = true;
        bool ul = true;
        bool ur = true;
        bool dl = true;
        bool dr = true;
        switch (type)
        {
            case 'K':
                for (int i = Math.Max(tile.x - 1, 0); i <= Math.Min(tile.x + 1, 7); i++)
                {
                    for (int k = Math.Max(tile.y - 1, 0); k <= Math.Min(tile.y + 1, 7); k++)
                    {
                        Game.Tile checkTile = Program.game.board[i, k];
                        if ((checkTile.x == tile.x && checkTile.y == tile.y)
                            || (checkTile.piece is not null && checkTile.piece!.team == this.team))
                        {
                            continue;
                        }

                        ts.Add(checkTile);
                    }
                }
                break;
            case 'Q':
                for (int i = 1; i < 8; i++)
                {
                    if (u && tile.x + i < 8)
                    {
                        if (Program.game.board[tile.x + i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x + i, tile.y]);
                        }
                        else
                        {
                            u = false;
                            if (Program.game.board[tile.x + i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x + i, tile.y]);
                            }
                        }
                    }
                    if (d && tile.x - i >= 0)
                    {
                        if (Program.game.board[tile.x - i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x - i, tile.y]);
                        }
                        else
                        {
                            d = false;
                            if (Program.game.board[tile.x - i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x - i, tile.y]);
                            }
                        }
                    }
                    if (l && tile.y - i >= 0)
                    {
                        if (Program.game.board[tile.x, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x, tile.y - i]);
                        }
                        else
                        {
                            l = false;
                            if (Program.game.board[tile.x, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x, tile.y - i]);
                            }
                        }
                    }
                    if (r && tile.y + i < 8)
                    {
                        if (Program.game.board[tile.x, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x, tile.y + i]);
                        }
                        else
                        {
                            r = false;
                            if (Program.game.board[tile.x, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x, tile.y + i]);
                            }
                        }
                    }
                    if (ul && tile.x + i < 8 && tile.y - i >= 0)
                    {
                        if (Program.game.board[tile.x + i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x + i, tile.y - i]);
                        }
                        else
                        {
                            ul = false;
                            if (Program.game.board[tile.x + i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x + i, tile.y - i]);
                            }
                        }
                    }
                    if (ur && tile.x + i < 8 && tile.y + i < 8)
                    {
                        if (Program.game.board[tile.x + i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x + i, tile.y + i]);
                        }
                        else
                        {
                            ur = false;
                            if (Program.game.board[tile.x + i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x + i, tile.y + i]);
                            }
                        }
                    }
                    if (dl && tile.x - i >= 0 && tile.y - i >= 0)
                    {
                        if (Program.game.board[tile.x - i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x - i, tile.y - i]);
                        }
                        else
                        {
                            dl = false;
                            if (Program.game.board[tile.x - i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x - i, tile.y - i]);
                            }
                        }
                    }
                    if (dr && tile.x - i >= 0 && tile.y + i < 8)
                    {
                        if (Program.game.board[tile.x - i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x - i, tile.y + i]);
                        }
                        else
                        {
                            dr = false;
                            if (Program.game.board[tile.x - i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x - i, tile.y + i]);
                            }
                        }
                    }
                }
                break;
            case 'R':
                for (int i = 1; i < 8; i++)
                {
                    if (u && tile.x + i < 8)
                    {
                        if (Program.game.board[tile.x + i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x + i, tile.y]);
                        }
                        else
                        {
                            u = false;
                            if (Program.game.board[tile.x + i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x + i, tile.y]);
                            }
                        }
                    }
                    if (d && tile.x - i >= 0)
                    {
                        if (Program.game.board[tile.x - i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x - i, tile.y]);
                        }
                        else
                        {
                            d = false;
                            if (Program.game.board[tile.x - i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x - i, tile.y]);
                            }
                        }
                    }
                    if (l && tile.y - i >= 0)
                    {
                        if (Program.game.board[tile.x, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x, tile.y - i]);
                        }
                        else
                        {
                            l = false;
                            if (Program.game.board[tile.x, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x, tile.y - i]);
                            }
                        }
                    }
                    if (r && tile.y + i < 8)
                    {
                        if (Program.game.board[tile.x, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x, tile.y + i]);
                        }
                        else
                        {
                            r = false;
                            if (Program.game.board[tile.x, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x, tile.y + i]);
                            }
                        }
                    }
                }
                break;
            case 'B':
                for (int i = 1; i < 8; i++)
                {
                    if (ul && tile.x + i < 8 && tile.y - i >= 0)
                    {
                        if (Program.game.board[tile.x + i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x + i, tile.y - i]);
                        }
                        else
                        {
                            ul = false;
                            if (Program.game.board[tile.x + i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x + i, tile.y - i]);
                            }
                        }
                    }
                    if (ur && tile.x + i < 8 && tile.y + i < 8)
                    {
                        if (Program.game.board[tile.x + i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x + i, tile.y + i]);
                        }
                        else
                        {
                            ur = false;
                            if (Program.game.board[tile.x + i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x + i, tile.y + i]);
                            }
                        }
                    }
                    if (dl && tile.x - i >= 0 && tile.y - i >= 0)
                    {
                        if (Program.game.board[tile.x - i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x - i, tile.y - i]);
                        }
                        else
                        {
                            dl = false;
                            if (Program.game.board[tile.x - i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x - i, tile.y - i]);
                            }
                        }
                    }
                    if (dr && tile.x - i >= 0 && tile.y + i < 8)
                    {
                        if (Program.game.board[tile.x - i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board[tile.x - i, tile.y + i]);
                        }
                        else
                        {
                            dr = false;
                            if (Program.game.board[tile.x - i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board[tile.x - i, tile.y + i]);
                            }
                        }
                    }
                }
                break;
            case 'N':
                if (tile.x - 2 >= 0 && tile.y - 1 >= 0
                    && (Program.game.board[tile.x - 2, tile.y - 1].piece is null
                    || Program.game.board[tile.x - 2, tile.y - 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x - 2, tile.y - 1]);
                }
                if (tile.x - 2 >= 0 && tile.y + 1 < 8
                    && (Program.game.board[tile.x - 2, tile.y + 1].piece is null
                    || Program.game.board[tile.x - 2, tile.y + 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x - 2, tile.y + 1]);
                }
                if (tile.x - 1 >= 0 && tile.y - 2 >= 0
                    && (Program.game.board[tile.x - 1, tile.y - 2].piece is null
                    || Program.game.board[tile.x - 1, tile.y - 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x - 1, tile.y - 2]);
                }
                if (tile.x - 1 >= 0 && tile.y + 2 < 8
                    && (Program.game.board[tile.x - 1, tile.y + 2].piece is null
                    || Program.game.board[tile.x - 1, tile.y + 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x - 1, tile.y + 2]);
                }
                if (tile.x + 1 < 8 && tile.y - 2 >= 0
                    && (Program.game.board[tile.x + 1, tile.y - 2].piece is null
                    || Program.game.board[tile.x + 1, tile.y - 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x + 1, tile.y - 2]);
                }
                if (tile.x + 1 < 8 && tile.y + 2 < 8
                    && (Program.game.board[tile.x + 1, tile.y + 2].piece is null
                    || Program.game.board[tile.x + 1, tile.y + 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x + 1, tile.y + 2]);
                }
                if (tile.x + 2 < 8 && tile.y - 1 >= 0
                    && (Program.game.board[tile.x + 2, tile.y - 1].piece is null
                    || Program.game.board[tile.x + 2, tile.y - 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x + 2, tile.y - 1]);
                }
                if (tile.x + 2 < 8 && tile.y + 1 < 8
                    && (Program.game.board[tile.x + 2, tile.y + 1].piece is null
                    || Program.game.board[tile.x + 2, tile.y + 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board[tile.x + 2, tile.y + 1]);
                }
                break;
            case 'P':
                int direction = 0;
                if (team == 0)
                {
                    direction = 1;
                }
                else
                {
                    direction = -1;
                }

                if (Program.game.board[tile.x, tile.y + direction].piece is null)
                {
                    ts.Add(Program.game.board[tile.x, tile.y + direction]);
                    if (((tile.y == 1 && direction == 1) || (tile.y == 6 && direction == -1))
                        && Program.game.board[tile.x, tile.y + 2 * direction].piece is null)
                    {
                        ts.Add(Program.game.board[tile.x, tile.y + 2 * direction]);
                    }
                }

                if (tile.x - 1 >= 0)
                {
                    if (Program.game.board[tile.x - 1, tile.y + direction].piece is not null
                        && Program.game.board[tile.x - 1, tile.y + direction].piece!.team != team)
                    {
                        ts.Add(Program.game.board[tile.x - 1, tile.y + direction]);
                    }
                }

                if (tile.x + 1 < 8)
                {
                    if (Program.game.board[tile.x + 1, tile.y + direction].piece is not null
                        && Program.game.board[tile.x + 1, tile.y + direction].piece!.team != team)
                    {
                        ts.Add(Program.game.board[tile.x + 1, tile.y + direction]);
                    }
                }

                break;
        }
        return ts;
    }
}