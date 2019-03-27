using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Math;

namespace SuperUltraAwesomeAI
{
    /// <summary>
    ///  Custom return class for returning results for the report
    /// </summary>
    public class LabAnswer
    {
        /// <summary>
        /// Number of nodes scanned
        /// </summary>
        public int numberOfNodesScanned;
        /// <summary>
        /// Depth of solution to scanned nodes ratio
        /// </summary>
        public float dnRatio;
        /// <summary>
        /// Solution to the problem as a list of steps
        /// </summary>
        public string solutionStr;
        public int max;
        public int min;
    }
    public class RushHour
    {
        /// <summary>
        /// Class that helps us track the positions
        /// and movements of the cars
        /// </summary>
        class CarDetails
        {
            public enum Axis { X, Y };
            public int size;
            /// <summary>
            /// Left / Right
            /// </summary>
            public int posX;
            /// <summary>
            /// Up / Down
            /// </summary>
            public int posY;
            public Axis axis;
        }

        #region Variables

        //           Constants:
        public const int BOARD_SIZE = 6;
        public const int RED_CAR_Y_INDEX = 2;

        //            Fields:
        Dictionary<char, CarDetails> cars;
        char[,] board;
        bool freedomIncreased;
        bool newCarBlocked;

        #endregion

        #region Search algorithms

        //DLS uninformed search - recursive implementaion
        public LabAnswer DLS(int l)
        {
            DLSNode sol = null;
            int nodesCount = 0;
            void FindSolution(DLSNode n)
            {
                nodesCount++;
                if (n.height == l - 1)
                {
                    if (CanReachGoal())
                    {   //Found solution
                        sol = new DLSNode(n, "XR" + (BOARD_SIZE - cars['X'].posX), l);
                    }
                }
                else
                {   //Add possible moves to the stack
                    var moves = PossibleMoves();
                    for (int i = 0; i < moves.Length && sol == null; i++)
                    {
                        Move(moves[i]);
                        FindSolution(new DLSNode(n, moves[i], n.height + 1));
                        Move(OppositeMove(moves[i]));
                    }
                }
            }
            var root = new DLSNode(null, null, 0);
            FindSolution(root);
            return sol == null ?
            new LabAnswer
            {
                numberOfNodesScanned = nodesCount
            } :
            new LabAnswer
            {
                solutionStr = sol.GetSolution(),
                numberOfNodesScanned = nodesCount,
                dnRatio = (float)l / (float)nodesCount,
                max = l, // In DLS min = max = sol.height = l
                min = l  // In DLS min = max = sol.height = l
            };
        }

        //IDS uninformed search
        public LabAnswer IDS()
        {
            LabAnswer ans;
            int i = 1, nodesCount = 0;
            do
            {
                ans = DLS(i++);
                nodesCount += ans.numberOfNodesScanned;
            } while (ans.solutionStr == null);
            ans.numberOfNodesScanned = nodesCount;
            return ans;
        }

        public LabAnswer DFSC(int f)
        {
            DLSNode sol = null;
            int nodesCount = 0;
            int max = int.MinValue, min = int.MaxValue;
            void FindSolution(DLSNode n)
            {
                nodesCount++;
                max = Max(n.height, max);
                if (CanReachGoal())
                {   //Found solution
                    sol = new DLSNode(n, "XR" + (BOARD_SIZE - cars['X'].posX), n.height + 1);
                    max = Max(n.height + 1, max);
                }
                else
                {
                    if (n.height + AdvancedHeuristicFunction() < f)
                    {   //Add possible moves to the stack
                        var moves = PossibleMoves();
                        for (int i = 0; i < moves.Length && sol == null; i++)
                        {
                            Move(moves[i]);
                            FindSolution(new DLSNode(n, moves[i], n.height + 1));
                            Move(OppositeMove(moves[i]));
                        }
                    }
                    else
                    {
                        min = Min(min, n.height);
                    }
                }
            }
            var root = new DLSNode(null, null, 0);
            FindSolution(root);
            return sol == null ?
            new LabAnswer
            {
                numberOfNodesScanned = nodesCount
            } :
            new LabAnswer
            {
                solutionStr = sol.GetSolution(),
                numberOfNodesScanned = nodesCount,
                dnRatio = (float)sol.height / (float)nodesCount,
                max = max,
                min = min
            };
        }

        public LabAnswer IDAStar()
        {
            LabAnswer ans;
            int i = 1, nodesCount = 0;
            do
            {
                ans = DFSC(i++);
                nodesCount += ans.numberOfNodesScanned;
            } while (ans.solutionStr == null);
            ans.numberOfNodesScanned = nodesCount;
            return ans;
        }

        ///<summary>
        ///Best-first search informed search
        ///</summary>
        public LabAnswer BestFS(Func<RushHour, int, string, string, int> score = null)
        {
            Node root = new Node(null, this, null, 0, score);
            var  heap = new NodesMinHeap(root);
            var  set  = new HashSet<string>() { GetHash() };
            Node ans  = null;
            int  totalNumberOfScannedNodes = 1;

            while (ans == null)
            {   //While we haven't found solution
                var top = heap.Remove(); //Get from the heap the best move
                foreach (var move in top.state.PossibleMoves())
                {
                    RushHour nextState = top.state.Clone();
                    nextState.Move(move);
                    if (set.Add(nextState.GetHash()))
                    {   //If the state is new
                        totalNumberOfScannedNodes++;
                        Node next = new Node(top, nextState, move, top.height + 1, score);
                        if (nextState.CanReachGoal())
                        {   //Found solution
                            ans = new Node(next, null, "XR" + nextState.Heuristic2(), top.height + 2, score);
                        }
                        else
                        {   //Add node to the heap (if the state is new)
                            heap.Insert(next);
                        }
                    }
                }
            }

            return new LabAnswer
            {
                solutionStr = ans.GetSolution(),
                numberOfNodesScanned = totalNumberOfScannedNodes,
                dnRatio = (float)ans.height / (float)totalNumberOfScannedNodes,
                max = root.MaxDepth(),
                min = root.MinDepth(), 
            };
        }
        public struct BiDirectionalVsAStar
        {
            public LabAnswer a_star, bidirectional;
        }
        public BiDirectionalVsAStar BidirectionalAstar()
        {
            LabAnswer a_star    = BestFS();
            RushHour goal_state = Clone();
            string[] moves      = a_star.solutionStr.Split(' ');
            for (int i = 0; i < moves.Length - 1; i++)
            {
                goal_state.Move(moves[i]);
            }

            int ForwardScore(RushHour st, int h, string a, string p)  => h + st.Distance(goal_state);
            int BackwardScore(RushHour st, int h, string a, string p) => h + Distance(st);
          
            Node forward_ans   = null;
            Node backward_ans  = null;
            bool solutionFound = false;
            Node forward_root  = new Node(null, this, null, 0, ForwardScore);
            Node backward_root = new Node(null, goal_state, null, 0, BackwardScore);
            var  forward_dict  = new Dictionary<string, Node> { { GetHash(), forward_root } };
            var  backward_dict = new Dictionary<string, Node> { { goal_state.GetHash(), backward_root } };
            var  forward_heap  = new NodesMinHeap(forward_root);
            var  backward_heap = new NodesMinHeap(backward_root);

            int totalNumberOfScannedNodes = 2;
            while (!solutionFound)
            {
                //Forward Move (from current to goal):
                var forward_top = forward_heap.Remove(); //Get from the heap the best move
                foreach (var move in forward_top.state.PossibleMoves())
                {
                    RushHour nextState = forward_top.state.Clone();
                    nextState.Move(move);
                    string next_hash = nextState.GetHash();
                    if (!forward_dict.ContainsKey(next_hash))
                    {   //If the state is new
                        totalNumberOfScannedNodes++;
                        Node next = new Node(forward_top, nextState, move, forward_top.height + 1, ForwardScore);
                        forward_dict.Add(next_hash, next);
                        if (backward_dict.ContainsKey(next_hash))
                        {   //If found solution
                            forward_ans   = next;
                            backward_ans  = backward_dict[next_hash];
                            solutionFound = true;
                        }
                        else
                        {   //Add node to the heap (if the state is new)
                            forward_heap.Insert(next);
                        }
                    }
                }
                if (!solutionFound)
                {   //Backward Move (from goal to current):
                    var backward_top = backward_heap.Remove(); //Get from the heap the best move
                    foreach (var move in backward_top.state.PossibleMoves())
                    {
                        RushHour nextState = backward_top.state.Clone();
                        nextState.Move(move);
                        string next_hash = nextState.GetHash();
                        if (!backward_dict.ContainsKey(next_hash))
                        {   //If the state is new
                            totalNumberOfScannedNodes++;
                            Node next = new Node(backward_top, nextState, move, backward_top.height + 1, BackwardScore);
                            backward_dict.Add(next_hash, next);
                            if (forward_dict.ContainsKey(next_hash))
                            {   //If found solution
                                backward_ans  = next;
                                forward_ans   = forward_dict[next_hash];
                                solutionFound = true;
                            }
                            else
                            {   //Add node to the heap (if the state is new)
                                backward_heap.Insert(next);
                            }
                        }
                    }
                }
            }
            
            return new BiDirectionalVsAStar
            {
                a_star = a_star,
                bidirectional = new LabAnswer
                {
                    solutionStr          = forward_ans.GetSolution() + " " + string.Join(" ", backward_ans.GetSolution().Split(' ').Reverse().ToArray()) + " " + moves[moves.Length - 1],
                    dnRatio              = (float)(forward_ans.height + backward_ans.height - 1) / totalNumberOfScannedNodes,
                    numberOfNodesScanned = totalNumberOfScannedNodes,
                    max                  = Max(forward_root.MaxDepth(), backward_root.MaxDepth()),
                    min                  = Min(forward_root.MinDepth(), backward_root.MinDepth())
                }
            };
        }

        #endregion

        #region Nodes

        //Node used in the DLS function
        class DLSNode
        {
            public readonly DLSNode parent;
            public readonly string action;
            public readonly int height;

            //Set the class feilds
            public DLSNode(DLSNode p,
                            string a,
                            int h)
            {
                parent = p;
                action = a;
                height = h;
            }

            /// <summary>
            /// If this Node instance is the solution Node it will use the parent pointer to get a solution string
            /// </summary>
            /// <returns>returns the solution of the level</returns>
            public string GetSolution()
            {
                DLSNode sol = this;
                string ans = string.Empty;
                while (sol.parent != null)
                {
                    ans = sol.action + (ans != "" ? (" " + ans) : "");
                    sol = sol.parent;
                }
                return ans;
            }
        }


        struct PerceptronInput
        {
            public string stateHash;
            public string action;
        }

        //Table of the weights of each action
        readonly static Dictionary<PerceptronInput, int> weights = new Dictionary<PerceptronInput, int>();

        /// <param name="input">state and action</param>
        /// <returns>Cost of action</returns>
        static int Perceptron(PerceptronInput input) => ( ( weights.ContainsKey(input) ?
                                                           weights[input]             :
                                                           weights[input] = 0         ) < 0 ) ?
                                                           1 : 0;

        /// <summary>
        /// Updates the weights of a given solution according to given optimal solution
        /// </summary>
        /// <param name="y0">Optimal solution</param>
        /// <param name="y1">Solution currently found</param>
        void UpdateWeights(List<PerceptronInput> y0, List<PerceptronInput> y1)
        {
            foreach (var a in y1) weights[a]--;
            foreach (var a in y0) weights[a]++;
        }

        public void ReinforcementLearning()
        {
            //We first run BFS to find optimal solution
            string optimalSolution = BestFS((state, h, a, p) => h).solutionStr;
            var y0 = GetActions(optimalSolution);
            foreach (var a in y0) weights.Add(a, 0);

            while (true)
            {
                //Run BFS according to the weights of the actions
                string solution = BestFS((s, h, a, p) =>
                {
                    if (p == null) return 0;
                    return Perceptron(new PerceptronInput
                    {
                        stateHash = p,
                        action = a
                    });
                }).solutionStr;
                if (solution.Length == optimalSolution.Length)
                {   //If we found optimal solution
                    break;
                }
                Console.WriteLine(solution.Length / 4);
                //Update the weights of the actions
                UpdateWeights(y0, GetActions(solution));
            }
        }

        List<PerceptronInput> GetActions(string solution)
        {
            string[] moves = solution.Split(' ');
            RushHour temp  = Clone();
            var      y     = new List<PerceptronInput>();
            for (int i = 0; i < moves.Length - 1; i++)
            {
                y.Add(new PerceptronInput
                {
                    stateHash = temp.GetHash(),
                    action    = moves[i]
                });
                temp.Move(moves[i]);
            }
            return y;
        }

        //Node used in the BestFS function
        class Node
        {
            public readonly Node parent;
            public readonly string action;
            public readonly RushHour state;
            public readonly int nodeScore;
            public readonly int height;
            public readonly List<Node> sons;
            
            /// <summary>
            ///Sets the class feilds.
            ///If the state isn't null it saves a copy of the RushHour class and caculate the heuristic value.
            ///If the parent isn't null it adds this node to it's sons.
            /// </summary>
            public Node(Node     p  ,
                        RushHour st ,
                        string   a  ,
                        int      h  ,
                        Func<RushHour, int, string, string, int> score = null)
            {
                sons = new List<Node>();
                action = a;
                parent = p;
                height = h;
                if (st != null)
                {
                    state = st.Clone();
                    if (score == null)
                        nodeScore = h + st.AdvancedHeuristicFunction();
                    else
                        nodeScore = score(st, h, a, parent?.state?.GetHash());
                }
                if (p != null)
                {
                    p.sons.Add(this);
                }
            }
            public int MinDepth() => sons.Count > 0 ? sons.Select(n => n.MinDepth()).Min() + 1 : 0;
            public int MaxDepth() => sons.Count > 0 ? sons.Select(n => n.MaxDepth()).Max() + 1 : 0;

            /// <summary>
            /// If this Node instance is the solution Node it will use the parent pointer to get a solution string
            /// </summary>
            /// <returns>returns the solution of the level</returns>
            public string GetSolution()
            {
                Node sol = this;
                string ans = string.Empty;
                while (sol.parent != null)
                {
                    ans = sol.action + (ans != "" ? (" " + ans) : "");
                    sol = sol.parent;
                }
                return ans;
            }
        }

        #endregion

        #region Constructors

        //Private empty C'tor
        private RushHour() { }

        /// <summary>
        /// Public C'tor, save all the car names, positions and moving axes.
        /// It also transfers the level's string to a 2D-array.
        /// </summary>
        /// <param name="level"> The level encoded as a 36 length string </param>
        public RushHour(string level)
        {
            board = new char[BOARD_SIZE, BOARD_SIZE];
            cars = new Dictionary<char, CarDetails>(BOARD_SIZE * BOARD_SIZE / 2);
            freedomIncreased = false;
            newCarBlocked = false;
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    char item = level[i * BOARD_SIZE + j];
                    if (item != '.')
                    {
                        if (cars.ContainsKey(item))
                        {
                            var car = cars[item];
                            car.axis = car.posX == j ? CarDetails.Axis.Y :
                                                       CarDetails.Axis.X;
                            ++car.size;
                        }
                        else
                        {
                            cars.Add(item, new CarDetails
                            {
                                size = 1,
                                posX = j,
                                posY = i
                            });
                        }
                    }
                    board[i, j] = item;
                }
            }
        }
        #endregion

        #region Heuristics

        /// <summary>
        /// This method is for setting the preferred heuristic for Rush Hour agent
        /// </summary>
        /// <returns> Perceived value of current state </returns>
        int AdvancedHeuristicFunction()
        {
            int h1 = 1;
            int h2 = 1;
            int h3 = 1;
            int h4 = 1;
            int h5 = 1;
            int h6 = 1;
            int h7 = 1;
            int h8 = 1;
            int h9 = 1;
            int h10 = 0;

            return new int[]
            {
                h1 * Heuristic1(),
                h2 * Heuristic2(),
                h3 * Heuristic3(),
                h4 * Heuristic4(),
                h5 * Heuristic5(),
                h6 * Heuristic6(),
                h7 * Heuristic7(),
                h8 * Heuristic8(),
                h9 * Heuristic9(),
                h10 //??? always returns 0 because h10 = 0
            }.Min();
        }

        //Number of cars blocking the red car
        int Heuristic1()
        {
            int count = 0;
            var redCar = cars['X'];
            for (int i = redCar.posX + redCar.size; i < BOARD_SIZE; i++)
            {
                if (board[RED_CAR_Y_INDEX, i] != '.')
                    count++;
            }
            return count;
        }

        //Distance from goal
        int Heuristic2() => BOARD_SIZE - cars['X'].posX;

        /// <summary>
        /// This heuristic will check how many cars have to be moved
        /// </summary>
        /// <param name="carIdentifier"> Name of a car to check</param>
        /// <param name="position">Position we want to clear for movement</param>
        /// <param name="depth">Depth left</param>
        /// <returns>Score of current position</returns>
        int Heuristic3(int depth = 2, char carIdentifier = 'X', int position = -1)
        {
            const int CAR_IN_A_WAY_PENALTY = 1;
            if (depth == 0) return 0;
            int count = 0;
            if (!cars.Keys.Contains(carIdentifier)) return -1;
            // First call will have a default car (red car)
            CarDetails car = cars[carIdentifier];
            if (carIdentifier == 'X' && position == -1)
            {
                // First call will have position as -1
                if (position == -1) position = car.posY;
                for (int i = car.posX + car.size; i < BOARD_SIZE; i++)
                {
                    char obst = board[car.posY, i];
                    if (obst == '.') count++;
                    else
                        count += CAR_IN_A_WAY_PENALTY + Heuristic3(depth - 1, obst, car.posY);
                }
            }
            else
            {
                // Since Axis is an enum, X==0 & Y==1. Thus, one of these will be one and the other one zero:
                int moveX = 1 - (int)car.axis;
                int moveY = (int)car.axis;
                // Check if there is enough space for the car up the axis (UP or LEFT)
                if ((car.size - 1) * moveX + (car.size - 1) * moveY < position)
                {
                    for (int i = (car.posX * moveX + car.posY * moveY); i >= 0; i--)
                    {
                        char obst = board[(car.posY * moveX) + (i * moveY), (car.posX * moveY) + (i * moveX)];
                        if (obst != '.' && obst != carIdentifier)
                            count += CAR_IN_A_WAY_PENALTY + Heuristic3(depth - 1, obst, (car.posY * moveX) + (car.posX * moveY));
                    }
                }
                //check down the axis (DOWN or RIGHT)
                if ((BOARD_SIZE - car.size) * moveX + (BOARD_SIZE - car.size) * moveY > position)
                {
                    for (int i = ((car.posX + car.size - 1) * moveX + (car.posY + car.size - 1) * moveY); i < BOARD_SIZE; i++)
                    {
                        char obst = board[(car.posY * moveX) + (i * moveY), (car.posX * moveY) + (i * moveX)];
                        if (obst != '.' && obst != carIdentifier)
                            count += CAR_IN_A_WAY_PENALTY + Heuristic3(depth - 1, obst, (car.posY * moveX) + (car.posX * moveY));
                    }
                }
            }
            return count;
        }

        /// <returns>Lower bound on the number of cars needed to move</returns>
        int Heuristic4()
        {
            var carsSet = new HashSet<char>();
            int CountCars(char carName)
            {
                int count = 0;
                if (carName != '.' && carsSet.Add(carName))
                {
                    var car = cars[carName];
                    count++;
                    if (carName == 'X')
                    {
                        for (int i = car.posX + car.size; i < BOARD_SIZE; i++)
                        {
                            count += CountCars(board[RED_CAR_Y_INDEX, i]);
                        }
                    }
                    else
                    {
                        if (car.axis == CarDetails.Axis.X)
                        {
                            int left = car.posX - 1, right = car.posX + car.size;
                            if (left < 0)
                            {
                                count += CountCars(board[car.posY, right]);
                            }
                            else if (right == BOARD_SIZE)
                            {
                                count += CountCars(board[car.posY, left]);
                            }
                            else if (board[car.posY, left] != '.' && board[car.posY, right] != '.')
                            {
                                count += Max(1, Min(CountCars(board[car.posY, right]), CountCars(board[car.posY, left])));
                            }
                        }
                        else
                        {
                            int up = car.posY - 1, down = car.posY + car.size;
                            if (up < 0)
                            {
                                CountCars(board[down, car.posX]);
                            }
                            else if (down == BOARD_SIZE)
                            {
                                CountCars(board[up, car.posX]);
                            }
                            else if (board[up, car.posX] != '.' && board[down, car.posX] != '.')
                            {
                                count += Max(1, Min(CountCars(board[up, car.posX]), CountCars(board[up, car.posX])));
                            }
                        }
                    }
                }
                return count;
            }
            return CountCars('X');
        }

        /// <summary>
        /// Calculate how many cars need to be moved and how far.
        /// Depth of one.
        /// </summary>
        int Heuristic5()
        {
            int _r = 0; //Result
            CarDetails _c = cars['X']; // Red car
            // Check every position from the car to exit
            for (int _p = _c.posX + _c.size; _p < BOARD_SIZE; _p++)
            {
                // Check for Blocking Car
                char _bc = board[_c.posY, _p];
                if (_bc != '.')
                {
                    // Count blocking car
                    _r++;
                    // Count tiles to move the blocking car
                    var car = cars[_bc];
                    int up = car.posY - 1, down = car.posY + car.size;
                    int _d1 = 8, _d2 = 8;
                    // check if we can move the car up
                    if (car.size - 1 < _c.posY)
                        _d1 = (down - 1) - _c.posY + 1; // count minimum tiles to move up
                    // check if we can move the car down
                    else if (BOARD_SIZE - car.size > _c.posY)
                        _d2 = _c.posY - car.posY + 1;    // count minimum tiles to move up
                    _r += Min(_d1, _d2);
                }
            }
            return _r;
        }

        // This is a dirty solution, not to be 
        // implemented in any respectable workplace
        /// <summary>
        /// Calculate how many cars need to be moved, and how far.
        /// To the depth of two.
        /// </summary>
        /// <returns></returns>
        int Heuristic6()
        {
            int _r = 0; //Result
            char _bc;
            CarDetails _c = cars['X']; // Red car
            // Check every position from the red car to exit
            for (int _p = _c.posX + _c.size; _p < BOARD_SIZE; _p++)
            {
                // Check for Blocking Car
                _bc = board[_c.posY, _p];
                if (_bc != '.')
                {
                    // Count blocking car
                    _r++;
                    // Count the minimum required tiles to move the blocking car
                    var car = cars[_bc];
                    int up = car.posY - 1, down = car.posY + car.size;
                    int _d1 = 8, _d2 = 8;
                    // check if we can move the car up
                    if (car.size - 1 < _c.posY)
                    {
                        int cost = 0;
                        _d1 = down - _c.posY; // count minimum tiles to move up
                        // Count blocking cars on the way
                        for (int x = up; x < up - _d1; x--)
                            if (board[x, car.posX] != '.') cost++;
                        _d1 += cost; // add the cost
                    }
                    // check if we can move the car down
                    else if (BOARD_SIZE - car.size > _c.posY)
                    {
                        int cost = 0;
                        _d2 = _c.posY - car.posY + 1;    // count minimum tiles to move down
                        // Count blocking cars on the way
                        for (int x = down; x < down + _d2; x++)
                            if (board[x, car.posX] != '.') cost++;
                        _d2 += cost;
                    }
                    _r += Min(_d1, _d2);
                }
            }
            return _r;
        }

        /// <summary>
        /// Heuristics 5 with indicators
        /// Calculate how many cars need to be moved and how far.
        /// Depth of one. Includes indicators
        /// </summary>
        int Heuristic7()
        {
            int _r = 0; //Result
            CarDetails _c = cars['X']; // Red car
            // Check every position from the car to exit
            for (int _p = _c.posX + _c.size; _p < BOARD_SIZE; _p++)
            {
                // Check for Blocking Car
                char _bc = board[_c.posY, _p];
                if (_bc != '.')
                {
                    // Count blocking car
                    _r++;
                    // Count tiles to move the blocking car
                    var car = cars[_bc];
                    int up = car.posY - 1, down = car.posY + car.size;
                    int _d1 = 8, _d2 = 8;
                    // check if we can move the car up
                    if (car.size - 1 < _c.posY)
                        _d1 = (down - 1) - _c.posY + 1; // count minimum tiles to move up
                    // check if we can move the car down
                    else if (BOARD_SIZE - car.size > _c.posY)
                        _d2 = _c.posY - car.posY + 1;    // count minimum tiles to move up
                    _r += Min(_d1, _d2);
                }
            }
            //_r += (newCarBlocked ? 1 : 0);
            //_r += (freedomIncreased ? 0 : 1);
            _r += (newCarBlocked ? 1 : 0) + (freedomIncreased ? 0 : 1);
            return _r;
        }

        // This is a dirty solution, not to be 
        // implemented in any respectable workplace
        /// <summary>
        /// Heuristics 6 with indicators
        /// Calculate how many cars need to be moved, and how far.
        /// To the depth of two.
        /// </summary>
        /// <returns></returns>
        int Heuristic8()
        {
            int _r = 0; //Result
            char _bc;
            CarDetails _c = cars['X']; // Red car
            // Check every position from the red car to exit
            for (int _p = _c.posX + _c.size; _p < BOARD_SIZE; _p++)
            {
                // Check for Blocking Car
                _bc = board[_c.posY, _p];
                if (_bc != '.')
                {
                    // Count blocking car
                    _r++;
                    // Count the minimum required tiles to move the blocking car
                    var car = cars[_bc];
                    int up = car.posY - 1, down = car.posY + car.size;
                    int _d1 = 8, _d2 = 8;
                    // check if we can move the car up
                    if (car.size - 1 < _c.posY)
                    {
                        int cost = 0;
                        _d1 = down - _c.posY; // count minimum tiles to move up
                        // Count blocking cars on the way
                        for (int x = up; x < up - _d1; x--)
                            if (board[x, car.posX] != '.') cost++;
                        _d1 += cost; // add the cost
                    }
                    // check if we can move the car down
                    else if (BOARD_SIZE - car.size > _c.posY)
                    {
                        int cost = 0;
                        _d2 = _c.posY - car.posY + 1;    // count minimum tiles to move down
                        // Count blocking cars on the way
                        for (int x = down; x < down + _d2; x++)
                            if (board[x, car.posX] != '.') cost++;
                        _d2 += cost;
                    }
                    _r += Min(_d1, _d2);
                }
            }
            //_r += (newCarBlocked ? 1 : 0);
            //_r += (freedomIncreased ? 0 : 1);
            _r += (newCarBlocked ? 1 : 0) + (freedomIncreased ? 0 : 1);
            return _r;
        }

        ///<summary>This is Heuristics 4 with indicators from lab 2</summary>
        /// <returns>Lower bound on the number of cars needed to move</returns>
        int Heuristic9()
        {
            var carsSet = new HashSet<char>();
            int CountCars(char carName)
            {
                int count = 0;
                if (carName != '.' && carsSet.Add(carName))
                {
                    var car = cars[carName];
                    count++;
                    if (carName == 'X')
                    {
                        for (int i = car.posX + car.size; i < BOARD_SIZE; i++)
                        {
                            count += CountCars(board[RED_CAR_Y_INDEX, i]);
                        }
                    }
                    else
                    {
                        if (car.axis == CarDetails.Axis.X)
                        {
                            int left = car.posX - 1, right = car.posX + car.size;
                            if (left < 0)
                            {
                                count += CountCars(board[car.posY, right]);
                            }
                            else if (right == BOARD_SIZE)
                            {
                                count += CountCars(board[car.posY, left]);
                            }
                            else if (board[car.posY, left] != '.' && board[car.posY, right] != '.')
                            {
                                count += Max(1, Min(CountCars(board[car.posY, right]), CountCars(board[car.posY, left])));
                            }
                        }
                        else
                        {
                            int up = car.posY - 1, down = car.posY + car.size;
                            if (up < 0)
                            {
                                CountCars(board[down, car.posX]);
                            }
                            else if (down == BOARD_SIZE)
                            {
                                CountCars(board[up, car.posX]);
                            }
                            else if (board[up, car.posX] != '.' && board[down, car.posX] != '.')
                            {
                                count += Max(1, Min(CountCars(board[up, car.posX]), CountCars(board[up, car.posX])));
                            }
                        }
                    }
                }
                return count;
            }
            int a = 0;
            //a = (newCarBlocked ? 1 : 0);
            //a = (freedomIncreased ? 0 : 1);
            a = (newCarBlocked ? 1 : 0) + (freedomIncreased ? 0 : 1);
            return CountCars('X') + a;
        }

        /// <summary>
        /// Heuristic function for the Bidirectional A*
        /// </summary>
        /// <returns>Number of cars with different positions</returns>
        int Distance(RushHour goal) => cars.Keys.Count(c => (cars[c].posX + cars[c].posY) != (goal.cars[c].posX + goal.cars[c].posY));

        #endregion

        /// <returns>Number of cars that can move</returns>
        public int FreedomLevel() => cars.Keys.Count(car => !Blocked(car));

        /// <summary>
        /// Check if there are any cars that can't move and returns a hash of their keys
        /// </summary>
        /// <returns>HashSet of keys of blocked cars</returns>
        public HashSet<char> ListBlockedCars() => new HashSet<char>(cars.Keys.Where(key => Blocked(key)), null);

        /// <summary>
        /// Checks whenether the car with a given key is blocked
        /// </summary>
        /// <param name="_cd">Car key to check</param>
        /// <returns>True is the car cannot move</returns>
        private bool Blocked(char _cd)
        {
            CarDetails _c = cars[_cd];
            return !(_c.axis == CarDetails.Axis.X ? (_c.posX != 0 && board[_c.posY, _c.posX - 1] == '.') ||
                    (_c.posX + _c.size != BOARD_SIZE && board[_c.posY, _c.posX + _c.size - 1] == '.') :
                                                   (_c.posY != 0 && board[_c.posY - 1, _c.posX] == '.') ||
                    (_c.posY + _c.size != BOARD_SIZE && board[_c.posY + _c.size - 1, _c.posX] == '.'));
        }

        public bool CarBecameBlocked()
        {
            
            return true;
        }



        ///<summary>
        ///Create a deep copy of the RushHour class
        ///</summary>
        ///<returns>
        ///A copy of RushHour class
        /// </returns>
        public RushHour Clone() => new RushHour
        {
            freedomIncreased = freedomIncreased,
            newCarBlocked    = newCarBlocked,
            cars = cars.ToDictionary(item => item.Key, item => new CarDetails
            {
                size = item.Value.size,
                axis = item.Value.axis,
                posX = item.Value.posX,
                posY = item.Value.posY
            }),
            board = (char[,])board.Clone()
        };
        
        //Check if there aren't any cars that are blocking the way
        bool CanReachGoal() => Heuristic1() == 0;

        ///<summary>Returns string that represent the board state</summary>
        public string GetHash() => new string(board.Cast<char>().ToArray());

        ///<summary>Moves a car according to a given action.</summary>
        ///<example>AU2 moves car A up by 2 cells </example>
        void Move(string action)
        {
            // save blocked cars and level of freedom before moving
            int fr = this.FreedomLevel();
            HashSet<char> _blc = ListBlockedCars();

            if (action != null)
            {
                var car = cars[action[0]];
                int length = action[2] & 15;
                for (int i = 0; i < length; i++)
                {
                    switch (action[1])
                    {
                        case 'U': //Move up
                            board[--car.posY, car.posX] = action[0];
                            board[car.posY + car.size, car.posX] = '.';
                            break;
                        case 'D': //Move down
                            board[car.posY, car.posX] = '.';
                            board[(car.posY++) + car.size, car.posX] = action[0];
                            break;
                        case 'L': //Move left
                            board[car.posY, --car.posX] = action[0];
                            board[car.posY, car.posX + car.size] = '.';
                            break;
                        case 'R': //Move right
                            board[car.posY, car.posX] = '.';
                            board[car.posY, (car.posX++) + car.size] = action[0];
                            break;
                    }
                }
            }
            // update internal variables after the move
            freedomIncreased = fr < FreedomLevel();
            newCarBlocked = !ListBlockedCars().IsSubsetOf(_blc);
        }

        /// <summary>
        /// Returns the opposite move
        /// </summary>
        /// <example>
        /// AL4 -> AR4, BU1 -> BD1
        /// </example>
        string OppositeMove(string action)
        {
            if (action != null)
            {   //Replace action's direction
                string d = action[1] == 'R' ? "L" :
                           action[1] == 'L' ? "R" :
                           action[1] == 'U' ? "D" :
                                              "U";
                action = action[0] + d + action[2];
            }
            return action;
        }

        static readonly Random random = new Random(); //Randomize the order of the moves

        //Return every possible move from current state.
        string[] PossibleMoves()
        {
            var moves = new List<string>();
            foreach (var p in cars)
            {
                var car = p.Value;
                int x1 = car.posX, x2 = car.posX + car.size - 1;
                int y1 = car.posY, y2 = car.posY + car.size - 1;
                bool l, r, u, d;
                l = r = car.axis == CarDetails.Axis.X;
                u = d = car.axis == CarDetails.Axis.Y;
                for (int i = 1; i < BOARD_SIZE; i++)
                {
                    if (l = (l && ((x1 - i >= 0) && board[car.posY, x1 - i] == '.')))
                    {   //If car,L,i is a legal move
                        moves.Add(p.Key + "L" + i);
                    }
                    if (r = (r && ((x2 + i < BOARD_SIZE) && board[car.posY, x2 + i] == '.')))
                    {   //If car,R,i is a legal move
                        moves.Add(p.Key + "R" + i);
                    }
                    if (u = (u && ((y1 - i >= 0) && board[y1 - i, car.posX] == '.')))
                    {   //If car,U,i is a legal move
                        moves.Add(p.Key + "U" + i);
                    }
                    if (d = (d && ((y2 + i < BOARD_SIZE) && board[y2 + i, car.posX] == '.')))
                    {   //If car,D,i is a legal move
                        moves.Add(p.Key + "D" + i);
                    }
                }
            }

            string[] movesArray = moves.ToArray();

            //Shuffle array
            for (int i = 0; i < movesArray.Length; i++)
            {
                int rand         = random.Next(i, movesArray.Length);
                var temp         = movesArray[i];
                movesArray[i]    = movesArray[rand];
                movesArray[rand] = temp;
            }

            return movesArray;
        }

        //Min heap struct for the BestFS Node
        struct NodesMinHeap
        {
            //Heap data
            public readonly List<Node> nodes;

            //Number of items in the heap
            public int Count => nodes.Count;

            //Constructor
            public NodesMinHeap(Node first) => nodes = new List<Node> { first };

            //Adds the new node to the heap
            public void Insert(Node node)
            {
                nodes.Add(node);
                int i = nodes.Count - 1, j;
                while (i > 0 && nodes[j = (i - 1) / 2].nodeScore > nodes[i].nodeScore)
                {   //while *parent(i) > *i:
                    Node temp = nodes[j]; //*i <=> *parent(i)
                    nodes[j] = nodes[i]; //...
                    nodes[i] = temp;     //...
                    i = j;        //i = parent(i)
                }
            }

            //Remove min node from the heap and return it
            public Node Remove()
            {
                //Remove min value
                var top = nodes[0];
                int count = nodes.Count - 1;
                nodes[0] = nodes[count];
                nodes.RemoveAt(count);

                //Fix heap property if it's violated
                int index = 0;
                bool flag = nodes.Count > 0;
                while (flag)
                {
                    int smallest = index;
                    int l = index + index + 1;
                    int r = l + 1;
                    if ((l < count) && (nodes[l].nodeScore < nodes[index].nodeScore))
                    {
                        smallest = l;
                    }
                    if ((r < count) && (nodes[r].nodeScore < nodes[smallest].nodeScore))
                    {
                        smallest = r;
                    }
                    var temp = nodes[smallest];
                    nodes[smallest] = nodes[index];
                    nodes[index] = temp;
                    flag = index != smallest;
                    index = smallest;
                }

                return top;
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            string text = @"AA...OP..Q.OPXXQ.OP..Q..B...CCB.RRR.
A..OOOA..B.PXX.BCPQQQ.CP..D.EEFFDGG.
.............XXO...AAO.P.B.O.P.BCC.P
O..P..O..P..OXXP....AQQQ..A..B..RRRB
AA.O.BP..OQBPXXOQGPRRRQGD...EED...FF
AA.B..CC.BOP.XXQOPDDEQOPF.EQ..F..RRR
.ABBCD.A.ECD.XXE.F..II.F...H.....H..
...AAO..BBCOXXDECOFFDEGGHHIPPPKKIQQQ
.ABBCC.A.DEEXX.DOFPQQQOFP.G.OHP.G..H
AAB.CCDDB..OPXX..OPQQQ.OP..EFFGG.EHH
OAAP..O..P..OXXP....BQQQ..B..E..RRRE
ABB..OA.P..OXXP..O..PQQQ....C.RRR.C.
AABBC...D.CO.EDXXOPE.FFOP..GHHPIIGKK
AAB.....B.CCDEXXFGDEHHFG..I.JJKKI...
.AABB.CCDDOPQRXXOPQREFOPQREFGG.HHII.
AABBCOD.EECODFPXXO.FPQQQ..P...GG....
AOOO..A.BBCCXXD...EEDP..QQQPFGRRRPFG
AABO..CCBO..PXXO..PQQQ..PDD...RRR...
..ABB...A.J..DXXJ..DEEF..OOOF.......
A..OOOABBC..XXDC.P..D..P..EFFP..EQQQ
AABO..P.BO..PXXO..PQQQ...........RRR
..AOOOB.APCCBXXP...D.PEEFDGG.HFQQQ.H
..OOOP..ABBP..AXXP..CDEE..CDFF..QQQ.
..ABB..CA...DCXXE.DFF.E.OOO.G.HH..G.
AAB.CCDDB..OPXX.EOPQQQEOPF.GHH.F.GII
.A.OOOBA.CP.BXXCPDERRRPDE.F..G..FHHG
ABBO..ACCO..XXDO.P..DEEP..F..P..FRRR
OOOA....PABBXXP...CDPEEQCDRRRQFFGG.Q
OOO.P...A.P.XXA.PBCDDEEBCFFG.HRRRG.H
O.APPPO.AB..OXXB..CCDD.Q.....QEEFF.Q
AA.OOO...BCCDXXB.PD.QEEPFFQ..P..QRRR
AAOBCC..OB..XXO...DEEFFPD..K.PHH.K.P
.AR.BB.AR...XXR...IDDEEPIFFGHPQQQGHP
A..RRRA..B.PXX.BCPQQQDCP..EDFFIIEHH.
..OAAP..OB.PXXOB.PKQQQ..KDDEF.GG.EF.
OPPPAAOBCC.QOBXX.QRRRD.Q..EDFFGGE...
AAB.CCDDB.OPQXX.OPQRRROPQ..EFFGG.EHH
A..OOOABBC..XXDC.R..DEER..FGGR..FQQQ
..AOOO..AB..XXCB.RDDCEERFGHH.RFGII..
OAA.B.OCD.BPOCDXXPQQQE.P..FEGGHHFII.";
            
            int waitingTime = 1;
            foreach (var item in text.Split('\n'))
            {
                new RushHour(item).ReinforcementLearning();
                Console.WriteLine("Suceess");
            }
            #region Input Arguments
            // If arguments are not provided - print usage and exit
            if (args.Length < 1)
            {
                Console.WriteLine("Usage:\nAssignment1_Cars filename [T] \nfilename - required argument, a path to the file containing list of problems.\nT - allocated time (seconds) to solve every problem (default 1) \n\nExample: Assignment1_Cars input.txt 1\n\n");
                return;
            }
            else
            {
                if (args[0] != "buildin")
                    try
                    {
                        text = System.IO.File.ReadAllText(args[0]);
                    }
                    catch
                    {
                        Console.WriteLine("{0} is not a valid input file argument.\n", args[0]);
                        Console.WriteLine("Usage:\nAssignment1_Cars filename [T] \nfilename - required argument, a path to the file containing list of problems.\nT - allocated time (seconds) to solve every problem (default 1) \n\nExample: Assignment1_Cars input.txt 1\n\n");
                        return;
                    }
                if (args.Length == 2)
                    try
                    {
                        waitingTime = Int32.Parse(args[1]);
                    }
                    catch
                    {
                        Console.WriteLine("{0} is not a valid input for seconds. Using the default 1 second limit.", args[1]);
                        waitingTime = 1;
                    }
                else
                    waitingTime = 1;
            }
            #endregion

            Stopwatch s = new Stopwatch();
            TimeSpan t = TimeSpan.Zero;
            string[] levels = text.Split('\n');
            int level = 1;
            string finalOutput = string.Empty;
            int avgDepth = 0;

            using (StreamWriter outputFile = new StreamWriter("Lab1_Output.txt"))
            {
                foreach (var item in levels)
                {
                    var task = Task.Run(() => new RushHour(item).BestFS());
                    s.Start();
                    bool finished = task.Wait(TimeSpan.FromSeconds(waitingTime));
                    s.Stop();
                    t += TimeSpan.FromMilliseconds(s.Elapsed.Milliseconds / levels.Length);
                    TimeSpan ts = s.Elapsed;
                    if (finished)
                    {
                        LabAnswer _tr = task.Result;
                        int len = _tr.solutionStr.Split(' ').Length;
                        avgDepth += _tr.max;
                        Console.WriteLine("Level " + level + " - Succeeded in " + len + " moves");
                        outputFile.WriteLine("Level " + level++ + " - Succeeded in " + len + " moves");
                        outputFile.WriteLine("Solution: " + _tr.solutionStr);
                        outputFile.WriteLine(String.Format("Number of nodes scanned:{0:D} | Depth to nodes ratio:{1:F3}", _tr.numberOfNodesScanned, _tr.dnRatio));
                        outputFile.WriteLine(String.Format("Maximum reached depth:{0} | Minimum reached depth:{1}", _tr.max, _tr.min));
                        outputFile.WriteLine(String.Format("Solved in : {0:00}:{1:000}", ts.Seconds, ts.Milliseconds));
                    }
                    else
                    {
                        Console.WriteLine("Level " + level + "Failed");
                        outputFile.WriteLine("Level " + level++ + "Failed");
                        outputFile.WriteLine("Timeout reached at {0:00}.{1:000}", ts.Seconds, ts.Milliseconds);
                    }
                    s.Reset();
                }
                outputFile.WriteLine(String.Format("Avg search depth:{0:F3}", (float)avgDepth / level));
                outputFile.WriteLine("Avg Time  = " + t);
                Console.WriteLine("The data was saved in: Lab1_Output.txt");
            }
        }
    }
}