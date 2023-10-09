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
        private static Thread uciThreadRead = new(UCIController.ReadLoop);
        private static Thread uciThreadWrite = new(UCIController.WriteLoop);
        private static List<Thread> evalThreads = new();
        private static int perftCount = 0;

        static void Main(string[] args)
        {
            if (args.Length != 0)
            {
                switch (args[0])
                {
                    case "--rng":
                        engine = new RNGEngine();
                        break;
                    case "--grd":
                        engine = new Greedy();
                        break;
                    case "--mim":
                        engine = new MiniMax();
                        break;
                    case "--abp":
                        engine = new AlphaBeta();
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
                    UCIController.AddOutput($"bestmove {engine.CurrentBestMove}");
                    state = 0;
                    engine.ResetSearch();
                }
            }
        }

        public static void Quit()
        {
            UCIController.Stop();
            Environment.Exit(0);
        }

        public static Engine GetEngine() { return engine; }

        public static void ReadyNewGame() { }

        public static void StartNewGame(string fen, List<string> moves)
        {
            engine.NewGame(fen);
            foreach (string move in moves) { engine.GetBoard().Move(move); }
        }

        public static void SetState(int s) { state = s; }

        public static void Perft(int depth)
        {
            BoardController board = engine.GetBoard();
            List<string> moves = board.GetLegalMoves();
            perftCount = 0;
            evalThreads = new();
            foreach (string move in moves)
            {
                board.Move(move);
                BoardController b = board.Copy();
                Thread t = new(() =>
                {
                    int count = engine.Perft(b, depth - 1);
                    AddPerftCount(count);
                    UCIController.AddOutput($"{move}: {count}");
                });
                board.UndoMove();
                t.Start();
                evalThreads.Add(t);
            }

            while (evalThreads.Any(x => x.IsAlive)) { }

            UCIController.AddOutput($"\nNodes searched: {perftCount}\n");
        }

        public static void Line(int depth)
        {
            BoardController board = engine.GetBoard().Copy();
            List<string> line = new();
            while (depth >= 1)
            {
                engine.SearchBestMove(board, depth);
                while (!engine.SearchFinished) { }
                board.Move(engine.CurrentBestMove);
                line.Add(engine.CurrentBestMove);
                depth--;
            }
            string lineString = "";
            foreach (string move in line)
            {
                lineString += move + " ";
            }
            UCIController.AddOutput(lineString);
        }

        public static void AddPerftCount(int count) { perftCount += count; }

        public static void EvaluatePosition()
        {
            UCIController.AddOutput($"{engine.EvaluatePosition()}");
        }

        public static void EvaluateMoves(int depth)
        {
            List<string> moves = engine.GetBoard().GetLegalMoves();
            evalThreads = new();
            foreach (string move in moves)
            {
                Thread t = new(() =>
                {
                    double eval = engine.AnalyseMove(engine.GetBoard().Copy(), move, depth);
                    UCIController.AddOutput($"{move}: {eval}");
                });
                t.Start();
                evalThreads.Add(t);
            }

            while (evalThreads.Any(x => x.IsAlive)) { }

            UCIController.AddOutput($"\nDone!\n");
        }

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
        protected int searchDepth = 4;
        protected BoardController board = new("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        protected bool searchFinished = false;
        protected string currentBestMove = "";
        protected double bestMoveScore = 0;
        protected Random random = new();

        public void NewGame(string fen) { board = new(fen); }

        public int Perft(int depth) { return Perft(board, depth); }

        public int Perft(BoardController board, int depth)
        {
            List<string> moves = board.GetLegalMoves();
            if (depth == 1) { return moves.Count; }

            int count = 0;
            foreach (string move in moves)
            {
                board.Move(move);
                count += Perft(board, depth - 1);
                board.UndoMove();
            }
            return count;
        }

        public void Update(int state)
        {
            if (state == 1)
            {
                SearchBestMove(board, searchDepth);
            }
        }

        public abstract void SearchBestMove(BoardController b, int depth);

        public void ResetSearch()
        {
            searchFinished = false;
            currentBestMove = "";
        }

        public BoardController GetBoard()
        {
            return board;
        }

        public double AnalyseMove(string move, int depth)
        {
            return AnalyseMove(board, move, depth);
        }

        public virtual double AnalyseMove(BoardController b, string move, int depth)
        {
            return 0;
        }

        public double AnalysePosition(int depth)
        {
            return AnalysePosition(board, depth);
        }

        public virtual double AnalysePosition(BoardController b, int depth)
        {
            return 0;
        }

        public double EvaluatePosition()
        {
            return EvaluatePosition(board);
        }

        public double EvaluatePosition(BoardController b)
        {
            double evaluation = 0;
            evaluation += PieceScore(b) * EvaluationConstants.PIECE_WEIGHT;
            evaluation += PositionScore(b) * EvaluationConstants.POSITION_WEIGHT;
            return evaluation * (b.GetTurn() == 0 ? 1 : -1);
        }

        public double PieceScore()
        {
            return PieceScore(board);
        }

        public double PieceScore(BoardController b)
        {
            ulong whitePieces = b.GetWhitePieces();
            ulong blackPieces = b.GetBlackPieces();
            ulong pawns = b.GetPawns();
            ulong knights = b.GetKnights();
            ulong bishops = b.GetBishops();
            ulong rooks = b.GetRooks();
            ulong queens = b.GetQueens();
            double value = 0;
            value += ((int)ulong.PopCount(whitePieces & pawns) - (int)ulong.PopCount(blackPieces & pawns)) * EvaluationConstants.PAWN_VALUE;
            value += ((int)ulong.PopCount(whitePieces & knights) - (int)ulong.PopCount(blackPieces & knights)) * EvaluationConstants.KNIGHT_VALUE;
            value += ((int)ulong.PopCount(whitePieces & bishops) - (int)ulong.PopCount(blackPieces & bishops)) * EvaluationConstants.BISHOP_VALUE;
            value += ((int)ulong.PopCount(whitePieces & rooks) - (int)ulong.PopCount(blackPieces & rooks)) * EvaluationConstants.ROOK_VALUE;
            value += ((int)ulong.PopCount(whitePieces & queens) - (int)ulong.PopCount(blackPieces & queens)) * EvaluationConstants.QUEEN_VALUE;
            return value;
        }

        public double PositionScore()
        {
            return PositionScore(board);
        }

        public double PositionScore(BoardController b)
        {
            ulong whitePieces = b.GetWhitePieces();
            ulong pawns = b.GetPawns();
            ulong knights = b.GetKnights();
            ulong bishops = b.GetBishops();
            ulong rooks = b.GetRooks();
            ulong queens = b.GetQueens();
            ulong kings = b.GetKings();
            double value = 0;
            while (pawns != 0)
            {
                ulong pawn = (ulong)1 << (int)ulong.TrailingZeroCount(pawns);
                if ((pawn & whitePieces) != 0)
                {
                    value += EvaluationConstants.PAWN_POSITION[ulong.LeadingZeroCount(pawn)];
                }
                else
                {
                    value -= EvaluationConstants.PAWN_POSITION[ulong.TrailingZeroCount(pawn)];
                }
                pawns &= ~pawn;
            }
            while (knights != 0)
            {
                ulong knight = (ulong)1 << (int)ulong.TrailingZeroCount(knights);
                if ((knight & whitePieces) != 0)
                {
                    value += EvaluationConstants.KNIGHT_POSITION[ulong.LeadingZeroCount(knight)];
                }
                else
                {
                    value -= EvaluationConstants.KNIGHT_POSITION[ulong.TrailingZeroCount(knight)];
                }
                knights &= ~knight;
            }
            while (bishops != 0)
            {
                ulong bishop = (ulong)1 << (int)ulong.TrailingZeroCount(bishops);
                if ((bishop & whitePieces) != 0)
                {
                    value += EvaluationConstants.BISHOP_POSITION[ulong.LeadingZeroCount(bishop)];
                }
                else
                {
                    value -= EvaluationConstants.BISHOP_POSITION[ulong.TrailingZeroCount(bishop)];
                }
                bishops &= ~bishop;
            }
            while (rooks != 0)
            {
                ulong rook = (ulong)1 << (int)ulong.TrailingZeroCount(rooks);
                if ((rook & whitePieces) != 0)
                {
                    value += EvaluationConstants.ROOK_POSITION[ulong.LeadingZeroCount(rook)];
                }
                else
                {
                    value -= EvaluationConstants.ROOK_POSITION[ulong.TrailingZeroCount(rook)];
                }
                rooks &= ~rook;
            }
            while (queens != 0)
            {
                ulong queen = (ulong)1 << (int)ulong.TrailingZeroCount(queens);
                if ((queen & whitePieces) != 0)
                {
                    value += EvaluationConstants.QUEEN_POSITION[ulong.LeadingZeroCount(queen)];
                }
                else
                {
                    value -= EvaluationConstants.QUEEN_POSITION[ulong.TrailingZeroCount(queen)];
                }
                queens &= ~queen;
            }
            while (kings != 0)
            {
                ulong king = (ulong)1 << (int)ulong.TrailingZeroCount(kings);
                if ((king & whitePieces) != 0)
                {
                    value += EvaluationConstants.KING_POSITION[ulong.TrailingZeroCount(king)];
                }
                else
                {
                    value -= EvaluationConstants.KING_POSITION[ulong.TrailingZeroCount(king)];
                }
                kings &= ~king;
            }
            return value;
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
    }

    public class RNGEngine : Engine
    {
        public RNGEngine()
        {
            _name = "EngineRNG";
        }

        public override void SearchBestMove(BoardController b, int depth)
        {
            List<string> moves = board.GetLegalMoves();
            int index = random.Next(moves.Count);
            string move = moves[index];
            currentBestMove = move;
            searchFinished = true;
        }
    }

    public class Greedy : Engine
    {
        public Greedy()
        {
            _name = "Greedy";
        }

        public override void SearchBestMove(BoardController b, int depth)
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

    public class MiniMax : Engine
    {
        public MiniMax()
        {
            _name = "MiniMax";
        }

        public override void SearchBestMove(BoardController b, int depth)
        {
            List<string> moves = b.GetLegalMoves();

            List<Thread> threads = new();
            foreach (string move in moves)
            {
                Thread t = new(() => AnalyseMove(b.Copy(), move, depth - 1));
                t.Start();
                threads.Add(t);
            }

            while (threads.Any(x => x.IsAlive)) { }
            searchFinished = true;
        }

        public override double AnalyseMove(BoardController b, string move, int depth)
        {
            b.Move(move);
            double evaluation = -AnalysePosition(b, depth);
            b.UndoMove();

            if (evaluation > bestMoveScore || currentBestMove == "")
            {
                bestMoveScore = evaluation;
                currentBestMove = move;
            }

            return evaluation;
        }

        public override double AnalysePosition(BoardController b, int depth)
        {
            List<string> moves = b.GetLegalMoves();

            double evaluation = double.MinValue;

            if (moves.Count() == 0)
            {
                if (b.IsCheck())
                {
                    return evaluation;
                }
                else
                {
                    return 0;
                }
            }

            if (depth == 0)
            {
                return EvaluatePosition(b);
            }

            foreach (string move in moves)
            {
                b.Move(move);
                evaluation = Math.Max(-AnalysePosition(b, depth - 1), evaluation);
                b.UndoMove();
            }

            return evaluation;
        }
    }

    public class AlphaBeta : Engine
    {
        public AlphaBeta()
        {
            _name = "AlphaBeta";
        }

        public override void SearchBestMove(BoardController b, int depth)
        {
            List<string> moves = board.GetLegalMoves();
            foreach (string move in moves)
            {
                board.Move(move);
                double score = -AlphaBetaSearch(double.MinValue, double.MaxValue, depth);
                board.UndoMove();
                if (score > bestMoveScore || currentBestMove == "")
                {
                    bestMoveScore = score;
                    currentBestMove = move;
                }
            }
            searchFinished = true;
        }

        private double AlphaBetaSearch(double alpha, double beta, int depth)
        {
            if (depth == 0) { return EvaluatePosition(); }
            List<string> moves = board.GetLegalMoves();
            foreach (string move in moves)
            {
                board.Move(move);
                double score = -AlphaBetaSearch(-beta, -alpha, depth - 1);
                board.UndoMove();
                if (score >= beta) { return beta; }
                if (score > alpha) { alpha = score; }
            }
            return alpha;
        }
    }
}