
using System.Reflection.Metadata.Ecma335;

namespace ChessEngine
{
    public static class EngineController
    {
        private static Engine engine = new EngineRNG();
        private static int state = 0;
        private static int wTime = 0;
        private static int bTime = 0;
        private static int wInc = 0;
        private static int bInc = 0;

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "--rng":
                        engine = new EngineRNG();
                        break;
                    default:
                        throw new Exception("Invalid engine parameter");
                }
            }
            var watch = new System.Diagnostics.Stopwatch();

            /*watch.Start();
            watch.Stop();
            var w1 = watch.ElapsedTicks;
            watch.Reset();
            watch.Start();
            watch.Stop();
            var w2 = watch.ElapsedTicks;
            
            Console.WriteLine("{0}, {1}", t1, w1);
            Console.WriteLine("{0}, {1}", t2, w2);*/


            Thread uciThreadRead = new(UCIController.ReadLoop);
            Thread uciThreadWrite = new(UCIController.WriteLoop);
            uciThreadRead.Start();
            uciThreadWrite.Start();

            while (true)
            {
                engine.Update(state);
                if (engine.SearchFinished)
                {
                    UCIController.AddOutput("bestmove " + engine.CurrentBestMove);
                    state = 0;
                    engine.ResetSearch();
                }
            }
        }

        public static Engine GetEngine() { return engine; }
        
        public static void ReadyNewGame() { }

        public static void StartNewGame(string fen, List<string> moves) 
        {
            engine.NewGame(fen);
            foreach (string move in moves) { engine.MovePiece(move); }
        }

        public static void SetState(int s) { state = s; }

        public static void SetTimeControls(int wt, int bt, int wi, int bi) 
        {
            wTime = wt;
            bTime = bt;
            wInc = wi;
            bInc = bi;
        }
    }

    public abstract class Engine
    {
        protected string _name = "Engine";
        protected BoardController board = new("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        protected bool searchFinished = false;
        protected string currentBestMove = "";
        
        public void NewGame(string fen) { board = new(fen); }

        public void MovePiece(string move)
        {
            /*board.MovePiece(move);
            board.Update();*/
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public bool SearchFinished
        {
            get
            {
                return searchFinished;
            }
        }

        public string CurrentBestMove
        {
            get
            {
                return currentBestMove;
            }
        }

        public abstract void Update(int state);

        public void ResetSearch()
        {
            searchFinished = false;
            currentBestMove = "";
        }

        public BoardController GetBoard()
        {
            return board;
        }
    }

    public class EngineRNG : Engine
    {
        private Random random = new();
        public EngineRNG()
        {
            _name = "EngineRNG";
        }

        public override void Update(int state)
        {
            /*if (state == 2)
            {
                List<string> moves = board.GetMoves(board.turn % 2, true);
                int index = random.Next(moves.Count);
                string move = moves[index];
                board.MovePiece(move);
                board.Update();
                currentBestMove = move;
                searchFinished = true;
            }*/
        }
    }
}