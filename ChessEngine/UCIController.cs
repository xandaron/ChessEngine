
using System.Diagnostics.Tracing;

namespace ChessEngine
{
    public static class UCIController
    {
        private static Queue<string> inQuery = new();
        private static Queue<string> outQuery = new();

        private static bool run = true;

        public static void Decoder()
        {
            string? input = GetInput();

            if (input is null) { return; }

            string[] inputComponents = input.Split(' ');
            switch (inputComponents[0])
            {
                case "uci":
                    AddOutput("id name " + EngineController.GetEngine().Name);
                    AddOutput("id author Alexander Davis");
                    AddOutput("uciok");
                    break;
                case "isready":
                    AddOutput("readyok");
                    break;
                case "option":
                    AddOutput("Engine has no options");
                    break;
                case "ucinewgame":
                    EngineController.ReadyNewGame();
                    break;
                case "position":
                    if (inputComponents[1] == "startpos")
                    {
                        List<string> moves = new();
                        if (inputComponents.Count() > 2 && inputComponents[2] == "moves")
                        {
                            moves.AddRange(inputComponents[3..]);
                        }
                        EngineController.StartNewGame("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", moves);
                    }
                    else if (inputComponents[1] == "fen")
                    {
                        List<string> moves = new();
                        if (inputComponents.Count() > 8 && inputComponents[8] == "moves")
                        {
                            moves.AddRange(inputComponents[9..]);
                        }
                        EngineController.StartNewGame(string.Join(" ", inputComponents[2..]), moves);
                    }
                    break;
                case "go":
                    if (inputComponents.Count() == 1)
                    {
                        EngineController.SetState(1);
                    }
                    else if (inputComponents[1] == "ponder")
                    {
                        EngineController.SetState(2);
                    }
                    else if (inputComponents[1] == "wtime")
                    {
                        int wtime = Int32.Parse(inputComponents[2]);
                        int btime = Int32.Parse(inputComponents[4]);
                        int winc = Int32.Parse(inputComponents[6]);
                        int binc = Int32.Parse(inputComponents[8]);
                        EngineController.SetTimeControls(winc, binc, wtime, btime);
                        EngineController.SetState(1);
                    }
                    break;
                case "lm":
                    string legalMoves = string.Join(" ", EngineController.GetEngine().GetMoves());
                    Console.WriteLine(legalMoves);
                    break;
                case "ca":
                    string captures = string.Join(" ", EngineController.GetEngine().GetBoard().GetCaptures());
                    Console.WriteLine(captures);
                    break;
                case "quit":
                    EngineController.Quit();
                    break;
                case "exit":
                    EngineController.Quit();
                    break;
                default:
                    break;
            }
        }

        public static void ReadLoop()
        {
            while (run)
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
            while (run)
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

        public static void Stop()
        {
            run = false;
        }
    }
}