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
                    case "--smt":
                        engine = new Smart();
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
                if (engine.SearchFinished)
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
            List<string> moves = engine.GetBoard().GetLegalMoves();
            foreach (string move in moves)
            {
                engine.GetBoard().Move(move);
                int count = engine.Perft(depth - 1);
                UCIController.AddOutput($"{move}: {count}");
                engine.GetBoard().UndoMove();
            }
        }

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
                Thread t = new(() => { EvaluateMove(move, depth); });
                t.Start();
                evalThreads.Add(t);
            }
        }

        public static void EvaluateMove(string move, int depth)
        {
            double eval = engine.AnalyseMove(engine.GetBoard().Copy(), move, depth);
            UCIController.AddOutput($"{move}: {eval}");
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
        protected BoardController board = new("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        protected bool searchFinished = false;
        protected string currentBestMove = "";
        protected double bestMoveScore = 0;
        protected Random random = new();

        public void NewGame(string fen) { board = new(fen); }

        public int Perft(int depth)
        {
            List<string> moves = board.GetLegalMoves();
            if (depth == 1)
            {
                return moves.Count();
            }
            int count = 0;
            foreach (string move in moves)
            {
                board.Move(move);
                count += Perft(depth - 1);
                board.UndoMove();
            }
            return count;
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

        public virtual double EvaluatePosition(BoardController b)
        {
            return 0;
        }

        public double PieceScore()
        {
            return PieceScore(board);
        }

        public virtual double PieceScore(BoardController b)
        {
            return 0;
        }

        public double PositionScore()
        {
            return PositionScore(board);
        }

        public virtual double PositionScore(BoardController b)
        {
            return 0;
        }
    }

    public class RNGEngine : Engine
    {
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

    public class Greedy : Engine
    {
        public Greedy()
        {
            _name = "Greedy";
        }

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

    public class Smart : Engine
    {
        public Smart()
        {
            _name = "Smart";
        }

        public override void Update(int state)
        {
            if (state == 1)
            {
                SearchBestMove();
            }
        }

        private void SearchBestMove()
        {
            List<string> moves = board.GetLegalMoves();
            if (board.GetTurn() == 0)
            {
                bestMoveScore = double.MinValue;
            }
            else
            {
                bestMoveScore = double.MaxValue;
            }
            List<Thread> searchThreads = new();
            foreach (string move in moves)
            {
                Thread t = new(() => { AnalyseMove(board.Copy(), move, 5); });
                t.Start();
                searchThreads.Add(t);
            }

            while (searchFinished == false)
            {
                searchFinished = true;
                foreach (Thread t in searchThreads)
                {
                    if (t.IsAlive)
                    {
                        searchFinished = false;
                        break;
                    }
                }
            }
        }

        public override double AnalyseMove(BoardController b, string move, int depth)
        {
            int turn = b.GetTurn();
            b.Move(move);
            double evaluation = AnalysePosition(b, depth - 1);
            b.UndoMove();

            if ((turn == 0 && evaluation > bestMoveScore) || (turn == 1 && evaluation < bestMoveScore))
            {
                bestMoveScore = evaluation;
                currentBestMove = move;
            }

            return evaluation;
        }

        public override double AnalysePosition(BoardController b, int depth)
        {
            double evaluation;
            int turn = b.GetTurn();
            List<string> moves = b.GetLegalMoves();

            if (turn == 0)
            {
                evaluation = double.MinValue;
            }
            else
            {
                evaluation = double.MaxValue;
            }

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

            if (depth <= 1)
            {
                if (!b.IsCheck())
                {
                    return EvaluatePosition(b);
                    /*evaluation = EvaluatePosition(b);
                    moves = b.GetCaptures();
                    // moves.AddRange(board.GetChecks());
                    if (depth <= -5)
                    {
                        return evaluation;
                    }*/
                }
            }

            foreach (string move in moves)
            {
                b.Move(move);
                if (turn == 0)
                {
                    evaluation = Math.Max(AnalysePosition(b, depth - 1), evaluation);
                }
                else
                {
                    evaluation = Math.Min(AnalysePosition(b, depth - 1), evaluation);
                }
                b.UndoMove();
            }

            return evaluation;
        }

        public override double EvaluatePosition(BoardController b)
        {
            double evaluation = 0;
            evaluation += PieceScore(b) * EvaluationConstants.PIECE_WEIGHT;
            evaluation += PositionScore(b) * EvaluationConstants.POSITION_WEIGHT;
            return evaluation;
        }

        public override double PieceScore(BoardController b)
        {
            ulong whitePieces = b.GetWhitePieces();
            ulong blackPieces = b.GetBlackPieces();
            ulong pawns = b.GetPawns();
            ulong knights = b.GetKnights();
            ulong bishops = b.GetBishops();
            ulong rooks = b.GetRooks();
            ulong queens = b.GetQueens();
            double value = 0;
            value += (ulong.PopCount(whitePieces & pawns) - ulong.PopCount(blackPieces & pawns)) * EvaluationConstants.PAWN_VALUE;
            value += (ulong.PopCount(whitePieces & knights) - ulong.PopCount(blackPieces & knights)) * EvaluationConstants.KNIGHT_VALUE;
            value += (ulong.PopCount(whitePieces & bishops) - ulong.PopCount(blackPieces & bishops)) * EvaluationConstants.BISHOP_VALUE;
            value += (ulong.PopCount(whitePieces & rooks) - ulong.PopCount(blackPieces & rooks)) * EvaluationConstants.ROOK_VALUE;
            value += (ulong.PopCount(whitePieces & queens) - ulong.PopCount(blackPieces & queens)) * EvaluationConstants.QUEEN_VALUE;
            return value;
        }

        public override double PositionScore(BoardController b)
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
    }
}