﻿
using System.Resources;

namespace ChessEngine
{
    public static class EngineController
    {
        private static Engine engine = new RNGEngine();
        private static int state = 0;
        private static int wTime = 0;
        private static int bTime = 0;
        private static int wInc = 0;
        private static int bInc = 0;
        private static Thread uciThreadRead = new (UCIController.ReadLoop);
        private static Thread uciThreadWrite = new(UCIController.WriteLoop);

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "--rng":
                        engine = new RNGEngine();
                        break;
                    case "--rgd":
                        engine = new RNGGreedyEngine();
                        break;
                    default:
                        throw new Exception("Invalid engine parameter");
                }
            }

            uciThreadRead.Start();
            uciThreadWrite.Start();

            while (true)
            {
                engine.Update(state);
                if (state == 1 && engine.SearchFinished)
                {
                    UCIController.AddOutput("bestmove " + engine.CurrentBestMove);
                    state = 0;
                    engine.ResetSearch();
                }
            }
        }
        
        public static void Quit()
        {
            UCIController.run = false;
            Environment.Exit(0);
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
            board.Move(move);
        }

        public List<string> GetMoves()
        {
            return board.GetLegalMoves();
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

    public class RNGEngine : Engine
    {
        private Random random = new();
        public RNGEngine()
        {
            _name = "EngineRNG";
        }

        public override void Update(int state)
        {
            if (state == 1)
            {
                List<string> moves = board.GetLegalMoves();
                int index = random.Next(moves.Count);
                string move = moves[index];
                currentBestMove = move;
                searchFinished = true;
            }
        }
    }

    public class RNGGreedyEngine : Engine
    {
        private Random random = new();
        public override void Update(int state)
        {
            if (state == 1)
            {
                List<string> captures = board.GetCaptures();
                if (captures.Count > 0)
                {
                    currentBestMove = captures[random.Next(captures.Count)];
                }
                else
                {
                    List<string> legalMoves = board.GetLegalMoves();
                    currentBestMove = legalMoves[random.Next(legalMoves.Count())];
                }
                searchFinished = true;
            }
        }
    }
}