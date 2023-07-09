

using System.Runtime.CompilerServices;

public static class Program
{
    public static Game game = new Game();
    static void Main()
    {
        Console.BackgroundColor = ConsoleColor.DarkBlue; Console.ForegroundColor = ConsoleColor.Gray;
        game.displayBoard();
    }
}

public class Game
{
    public Board board = new Board();

    public Game()
    {
        for (int i = 0; i < 8; i++)
        {
            board.tiles[i, 1].piece = new Piece('P', 0, board.tiles[i, 1]);
            board.tiles[i, 6].piece = new Piece('P', 1, board.tiles[i, 6]);
        }

        //White Pieces
        board.tiles[0, 0].piece = new Piece('R', 0, board.tiles[0, 0]);
        board.tiles[1, 0].piece = new Piece('N', 0, board.tiles[1, 0]);
        board.tiles[2, 0].piece = new Piece('B', 0, board.tiles[2, 0]);
        board.tiles[3, 0].piece = new Piece('Q', 0, board.tiles[3, 0]);
        board.tiles[4, 0].piece = new Piece('K', 0, board.tiles[4, 0]);
        board.tiles[5, 0].piece = new Piece('B', 0, board.tiles[5, 0]);
        board.tiles[6, 0].piece = new Piece('N', 0, board.tiles[6, 0]);
        board.tiles[7, 0].piece = new Piece('R', 0, board.tiles[7, 0]);

        //Black Pieces
        board.tiles[0, 7].piece = new Piece('R', 1, board.tiles[0, 7]);
        board.tiles[1, 7].piece = new Piece('N', 1, board.tiles[1, 7]);
        board.tiles[2, 7].piece = new Piece('B', 1, board.tiles[2, 7]);
        board.tiles[3, 7].piece = new Piece('Q', 1, board.tiles[3, 7]);
        board.tiles[4, 7].piece = new Piece('K', 1, board.tiles[4, 7]);
        board.tiles[5, 7].piece = new Piece('B', 1, board.tiles[5, 7]);
        board.tiles[6, 7].piece = new Piece('N', 1, board.tiles[6, 7]);
        board.tiles[7, 7].piece = new Piece('R', 1, board.tiles[7, 7]);
    }

    public void displayBoard()
    {
        for (int i = 0; i < 33; i++)
        {
            int mod = i % 4;
            switch (mod)
            {
                case 0:
                    Console.Write(" ");
                    for(int j = 0; j < 8; j++)
                    {
                        Console.Write("*-----");
                    }
                    Console.Write("* \n");
                    break;
                case 1:
                    Console.Write(" ");
                    for (int j = 0; j < 8; j++)
                    {
                        Console.Write("|     ");
                    }
                    Console.Write("| \n");
                    break;
                case 2:
                    Console.Write(" ");
                    for (int j = 0; j < 8; j++)
                    {
                        char pieceChar = ' ';
                        Console.Write("|  ");
                        if (board.tiles[j, 7 - (i-2) / 4].piece is not null)
                        {
                            pieceChar = board.tiles[j, 7 - (i-2) / 4].piece!.type;

                            if (board.tiles[j, 7 - (i - 2) / 4].piece!.team == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                            } else
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
                    Console.Write(" ");
                    for (int j = 0; j < 8; j++)
                    {
                        Console.Write("|     ");
                    }
                    Console.Write("| \n");
                    break;
            }
        }
    }
}

public class Board
{
    public Tile[,] tiles = new Tile[8,8];

    public Board()
    {
        for(int i = 0; i < 8; i++)
        {
            for(int k = 0; k < 8; k++)
            {
                tiles[i,k] = new Tile(i, k);
            }
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
}

public class Piece
{
    public char type;
    public int team;
    public Board.Tile tile;

    public Piece(char type, int team, Board.Tile tile)
    {
        this.type = type;
        this.team = team;
        this.tile = tile;
    }

    public List<Board.Tile> getMoves()
    {
        List<Board.Tile> ts = new List<Board.Tile>();
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
                for(int i = Math.Max(tile.x - 1, 0); i <= Math.Min(tile.x + 1, 7); i++)
                {
                    for (int k = Math.Max(tile.y - 1, 0); k <= Math.Min(tile.y + 1, 7); k++)
                    {
                        Board.Tile checkTile = Program.game.board.tiles[i, k];
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
                        if (Program.game.board.tiles[tile.x + i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x + i, tile.y]);
                        }
                        else
                        {
                            u = false;
                            if (Program.game.board.tiles[tile.x + i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x + i, tile.y]);
                            }
                        }
                    }
                    if (d && tile.x - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x - i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x - i, tile.y]);
                        }
                        else
                        {
                            d = false;
                            if (Program.game.board.tiles[tile.x - i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x - i, tile.y]);
                            }
                        }
                    }
                    if (l && tile.y - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x, tile.y - i]);
                        }
                        else
                        {
                            l = false;
                            if (Program.game.board.tiles[tile.x, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x, tile.y - i]);
                            }
                        }
                    }
                    if (r && tile.y + i < 8)
                    {
                        if (Program.game.board.tiles[tile.x, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x, tile.y + i]);
                        }
                        else
                        {
                            r = false;
                            if (Program.game.board.tiles[tile.x, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x, tile.y + i]);
                            }
                        }
                    }
                    if (ul && tile.x + i < 8 && tile.y - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x + i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x + i, tile.y - i]);
                        }
                        else
                        {
                            ul = false;
                            if (Program.game.board.tiles[tile.x + i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x + i, tile.y - i]);
                            }
                        }
                    }
                    if (ur && tile.x + i < 8 && tile.y + i < 8)
                    {
                        if (Program.game.board.tiles[tile.x + i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x + i, tile.y + i]);
                        }
                        else
                        {
                            ur = false;
                            if (Program.game.board.tiles[tile.x + i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x + i, tile.y + i]);
                            }
                        }
                    }
                    if (dl && tile.x - i >= 0 && tile.y - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x - i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x - i, tile.y - i]);
                        }
                        else
                        {
                            dl = false;
                            if (Program.game.board.tiles[tile.x - i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x - i, tile.y - i]);
                            }
                        }
                    }
                    if (dr && tile.x - i >= 0 && tile.y + i < 8)
                    {
                        if (Program.game.board.tiles[tile.x - i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x - i, tile.y + i]);
                        }
                        else
                        {
                            dr = false;
                            if (Program.game.board.tiles[tile.x - i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x - i, tile.y + i]);
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
                        if (Program.game.board.tiles[tile.x + i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x + i, tile.y]);
                        }
                        else
                        {
                            u = false;
                            if (Program.game.board.tiles[tile.x + i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x + i, tile.y]);
                            }
                        }
                    }
                    if (d && tile.x - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x - i, tile.y].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x - i, tile.y]);
                        }
                        else
                        {
                            d = false;
                            if (Program.game.board.tiles[tile.x - i, tile.y].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x - i, tile.y]);
                            }
                        }
                    }
                    if (l && tile.y - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x, tile.y - i]);
                        }
                        else
                        {
                            l = false;
                            if (Program.game.board.tiles[tile.x, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x, tile.y - i]);
                            }
                        }
                    }
                    if (r && tile.y + i < 8)
                    {
                        if (Program.game.board.tiles[tile.x, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x, tile.y + i]);
                        }
                        else
                        {
                            r = false;
                            if (Program.game.board.tiles[tile.x, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x, tile.y + i]);
                            }
                        }
                    }
                }
                break;
            case 'B':
                for(int i = 1; i < 8; i++)
                {
                    if (ul && tile.x + i < 8 && tile.y - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x + i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x + i, tile.y - i]);
                        }
                        else
                        {
                            ul = false;
                            if (Program.game.board.tiles[tile.x + i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x + i, tile.y - i]);
                            }
                        }
                    }
                    if (ur && tile.x + i < 8 && tile.y + i < 8)
                    {
                        if (Program.game.board.tiles[tile.x + i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x + i, tile.y + i]);
                        }
                        else
                        {
                            ur = false;
                            if (Program.game.board.tiles[tile.x + i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x + i, tile.y + i]);
                            }
                        }
                    }
                    if (dl && tile.x - i >= 0 && tile.y - i >= 0)
                    {
                        if (Program.game.board.tiles[tile.x - i, tile.y - i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x - i, tile.y - i]);
                        }
                        else
                        {
                            dl = false;
                            if (Program.game.board.tiles[tile.x - i, tile.y - i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x - i, tile.y - i]);
                            }
                        }
                    }
                    if (dr && tile.x - i >= 0 && tile.y + i < 8)
                    {
                        if (Program.game.board.tiles[tile.x - i, tile.y + i].piece is null)
                        {
                            ts.Add(Program.game.board.tiles[tile.x - i, tile.y + i]);
                        }
                        else
                        {
                            dr = false;
                            if (Program.game.board.tiles[tile.x - i, tile.y + i].piece!.team != this.team)
                            {
                                ts.Add(Program.game.board.tiles[tile.x - i, tile.y + i]);
                            }
                        }
                    }
                }
                break;
            case 'N':
                if (tile.x - 2 >= 0 && tile.y - 1 >= 0
                    && (Program.game.board.tiles[tile.x - 2, tile.y - 1].piece is null
                    || Program.game.board.tiles[tile.x - 2, tile.y - 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x - 2, tile.y - 1]);
                }
                if (tile.x - 2 >= 0 && tile.y + 1 < 8 
                    && (Program.game.board.tiles[tile.x - 2, tile.y + 1].piece is null 
                    || Program.game.board.tiles[tile.x - 2, tile.y + 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x - 2, tile.y + 1]);
                }
                if (tile.x - 1 >= 0 && tile.y - 2 >= 0
                    && (Program.game.board.tiles[tile.x - 1, tile.y - 2].piece is null
                    || Program.game.board.tiles[tile.x - 1, tile.y - 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x - 1, tile.y - 2]);
                }
                if (tile.x - 1 >= 0 && tile.y + 2 < 8
                    && (Program.game.board.tiles[tile.x - 1, tile.y + 2].piece is null
                    || Program.game.board.tiles[tile.x - 1, tile.y + 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x - 1, tile.y + 2]);
                }
                if (tile.x + 1 < 8 && tile.y - 2 >= 0
                    && (Program.game.board.tiles[tile.x + 1, tile.y - 2].piece is null
                    || Program.game.board.tiles[tile.x + 1, tile.y - 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x + 1, tile.y - 2]);
                }
                if (tile.x + 1 < 8 && tile.y + 2 < 8
                    && (Program.game.board.tiles[tile.x + 1, tile.y + 2].piece is null
                    || Program.game.board.tiles[tile.x + 1, tile.y + 2].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x + 1, tile.y + 2]);
                }
                if (tile.x + 2 < 8 && tile.y - 1 >= 0
                    && (Program.game.board.tiles[tile.x + 2, tile.y - 1].piece is null
                    || Program.game.board.tiles[tile.x + 2, tile.y - 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x + 2, tile.y - 1]);
                }
                if (tile.x + 2 < 8 && tile.y + 1 < 8
                    && (Program.game.board.tiles[tile.x + 2, tile.y + 1].piece is null
                    || Program.game.board.tiles[tile.x + 2, tile.y + 1].piece!.team != this.team))
                {
                    ts.Add(Program.game.board.tiles[tile.x + 2, tile.y + 1]);
                }
                break;
            case 'P':
                int direction = 0;
                if(team == 0)
                {
                    direction = 1;
                } else
                {
                    direction = -1;
                }

                if (Program.game.board.tiles[tile.x, tile.y + direction].piece is null)
                {
                    ts.Add(Program.game.board.tiles[tile.x, tile.y + direction]);
                    if (((tile.y == 1 && direction == 1) || (tile.y == 6 && direction == -1))
                        && Program.game.board.tiles[tile.x, tile.y + 2 * direction].piece is null)
                    {
                        ts.Add(Program.game.board.tiles[tile.x, tile.y + 2 * direction]);
                    }
                }

                if (tile.x - 1 >= 0)
                {
                    if (Program.game.board.tiles[tile.x - 1, tile.y + direction].piece is not null
                        && Program.game.board.tiles[tile.x - 1, tile.y + direction].piece!.team != team)
                    {
                        ts.Add(Program.game.board.tiles[tile.x - 1, tile.y + direction]);
                    }
                }

                if (tile.x + 1 < 8)
                {
                    if (Program.game.board.tiles[tile.x + 1, tile.y + direction].piece is not null
                        && Program.game.board.tiles[tile.x + 1, tile.y + direction].piece!.team != team)
                    {
                        ts.Add(Program.game.board.tiles[tile.x + 1, tile.y + direction]);
                    }
                }

                break;
        }
        return ts;
    }
}