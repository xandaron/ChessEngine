

using System.Runtime.CompilerServices;

public static class Program
{
    public static Game game = new Game();
    static void Main()
    {
        Console.BackgroundColor = ConsoleColor.Red; Console.ForegroundColor = ConsoleColor.Gray;
        //Pawns
        for (int i = 0; i < 8; i++)
        {
            game.board.tiles[i, 1].piece = new Piece('P', 0, game.board.tiles[i, 1]);
            game.board.tiles[i, 6].piece = new Piece('P', 1, game.board.tiles[i, 6]);
        }

        //White Pieces

        game.board.tiles[0, 0].piece = new Piece('R', 0, game.board.tiles[0, 0]);
        game.board.tiles[1, 0].piece = new Piece('N', 0, game.board.tiles[1, 0]);
        game.board.tiles[2, 0].piece = new Piece('B', 0, game.board.tiles[2, 0]);
        game.board.tiles[3, 0].piece = new Piece('Q', 0, game.board.tiles[3, 0]);
        game.board.tiles[4, 0].piece = new Piece('K', 0, game.board.tiles[4, 0]);
        game.board.tiles[5, 0].piece = new Piece('B', 0, game.board.tiles[5, 0]);
        game.board.tiles[6, 0].piece = new Piece('N', 0, game.board.tiles[6, 0]);
        game.board.tiles[7, 0].piece = new Piece('R', 0, game.board.tiles[7, 0]);

        //Black Pieces
        game.board.tiles[0, 7].piece = new Piece('R', 1, game.board.tiles[0, 7]);
        game.board.tiles[1, 7].piece = new Piece('N', 1, game.board.tiles[1, 7]);
        game.board.tiles[2, 7].piece = new Piece('B', 1, game.board.tiles[2, 7]);
        game.board.tiles[3, 7].piece = new Piece('Q', 1, game.board.tiles[3, 7]);
        game.board.tiles[4, 7].piece = new Piece('K', 1, game.board.tiles[4, 7]);
        game.board.tiles[5, 7].piece = new Piece('B', 1, game.board.tiles[5, 7]);
        game.board.tiles[6, 7].piece = new Piece('N', 1, game.board.tiles[6, 7]);
        game.board.tiles[7, 7].piece = new Piece('R', 1, game.board.tiles[7, 7]);

        game.displayBoard();
    }
}

public class Game
{
    public Board board = new Board();

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
                break;
            case 'R':
                break;
            case 'B':
                break;
            case 'N':

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