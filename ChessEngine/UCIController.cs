
using System.Diagnostics.Tracing;

namespace ChessEngine
{
    public static class UCIController
    {
        private static Queue<string> inQuery = new();
        private static Queue<string> outQuery = new();

        public static void Decoder()
        {
            string? input = GetInput();

            if (input is null) { return; }

            string[] inputComponents = input.Split(' ');
            if (input == "uci")
            {
                AddOutput("id name " + EngineController.GetEngine().Name);
                AddOutput("id author Alexander Davis");
                AddOutput("uciok");
            }
            else if (input == "isready")
            {
                AddOutput("readyok");
            }
            else if (inputComponents.Length > 0 && inputComponents[0] == "option")
            {
                AddOutput("Engine has no options");
            }
            else if (input == "ucinewgame")
            {
                EngineController.ReadyNewGame();
            }
            else if (inputComponents[0] == "position")
            {
                if (inputComponents[1] == "startpos")
                {
                    List<string> moves = new();
                    if (inputComponents.Count() > 2)
                    {
                        moves.AddRange(inputComponents[3..]);
                    }
                    EngineController.StartNewGame("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", moves);
                }
                else if (inputComponents[1] == "fen")
                {
                    List<string> moves = new();
                    EngineController.StartNewGame(string.Join(" ", inputComponents[2..]), moves);
                }
            }
            else if (inputComponents[0] == "go")
            {
                if (inputComponents.Count() == 1)
                {
                    EngineController.SetState(2);
                }
                else if (inputComponents[1] == "ponder")
                {
                    EngineController.SetState(1);
                }
                else if (inputComponents[1] == "wtime")
                {
                    int wtime = Int32.Parse(inputComponents[2]);
                    int btime = Int32.Parse(inputComponents[4]);
                    int winc = Int32.Parse(inputComponents[6]);
                    int binc = Int32.Parse(inputComponents[8]);
                    EngineController.SetTimeControls(winc, binc, wtime, btime);
                    EngineController.SetState(2);
                }
            }
        }


        public static void ReadLoop()
        {
            while (true)
            {
                string? input = Console.ReadLine();
                if (input is not null)
                {
                    inQuery.Enqueue(input);
                }
            }
        }

        public static void WriteLoop()
        {
            while (true)
            {
                Decoder();
                if (outQuery.Count != 0)
                {
                    Console.WriteLine(outQuery.Dequeue());
                }
            }
        }

        public static string? GetInput()
        {
            if (inQuery.Count != 0)
            {
                return inQuery.Dequeue();
            }
            return null;
        }

        public static void AddOutput(string outQ)
        {
            outQuery.Enqueue(outQ);
        }
    }
}