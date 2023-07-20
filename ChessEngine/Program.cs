
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
        Console.WriteLine("CHECKMATE!!! {0} WINS", game.turn % 2 == 0? "BLACK" : "WHITE");

        Console.WriteLine("Move list:");
        game.printMoves();
    }
}

