using ChessGame;

namespace ChessEngine
{
    public static class Program
    {
        static Game game = new("7k/5Q2/8/8/8/8/8/K7 w - - 0 1");
        static void Main()
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue; Console.ForegroundColor = ConsoleColor.Gray;
            bool checkmate = false;
            while (!checkmate)
            {
                checkmate = game.Update();
            }

            game.DisplayBoard();
            if (game.checkMate)
            {
                Console.WriteLine("CHECKMATE!!! {0} WINS", game.turn % 2 == 0 ? "BLACK" : "WHITE");
            }
            else
            {
                Console.WriteLine("Draw");
            }

            Console.WriteLine("Move list:");
            game.PrintMoves();
        }
    }
}