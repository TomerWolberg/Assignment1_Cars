using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace SuperUltraAwesomeAI
{
    /// <summary>
    ///  Custom return class for returning results for the report
    /// </summary>
    public class LabAnswer
    {
        public int numberOfNodesScanned;
        public float dnRatio;
        public string solutionStr;
        public int max;
        public int min;
    }
    class RushHour
    {
        //Class that helps us track the positions
        //and movements of the cars
        class CarDetails
        {
            public enum Axis { X, Y };
            public int  size;
            public int  posX;
            public int  posY;
            public Axis axis;
        }

        #region Variables

        //           Constants:
        public const int BOARD_SIZE      = 6;
        public const int RED_CAR_Y_INDEX = 2;

        //            Fields:
        Dictionary<char, CarDetails> cars;
        char[,]                      board;

        #endregion

        #region Search algorithms

        //DLS uninformed search - recursive implementaion
        public string DLS( int l )
        {
            DLSNode sol = null;
            var     set = new HashSet<string>();
            void FindSolution(DLSNode n)
            {
                if (set.Add(GetHash()))
                {   //If the state is new
                    if (CanReachGoal())
                    {   //Found solution
                        sol = new DLSNode(n, "XR" + (BOARD_SIZE - cars['X'].posX), n.height + 1);
                    }
                    else if (n.height < (l - 1) && sol == null)
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
            }
            FindSolution(new DLSNode(null, null, 0));
            return sol?.GetSolution();
        }

        //IDS uninformed search
        public string IDS()
        {
            string s;
            int i = 1;
            do s = DLS(i++);
            while (s == null);
            return s;
        }

        ///<summary>
        ///Best-first search informed search
        ///</summary>
        public LabAnswer BestFS()
        {
            Node root = new Node(null, this, null, 0);
            var  heap = new NodesMinHeap(root);
            var  set  = new HashSet<string>() { GetHash() };
            Node ans  = null;
            int totalNumberOfScannedNodes = 1;

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
                        Node next = new Node(top, nextState, move, top.height + 1);
                        if (nextState.CanReachGoal())
                        {   //Found solution
                            ans = new Node(next, null, "XR" + nextState.Heuristic2(), top.height + 2);
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
                solutionStr          = ans.GetSolution(),
                numberOfNodesScanned = totalNumberOfScannedNodes,
                dnRatio              = (float)ans.height / (float)totalNumberOfScannedNodes,
                max                  = root.MaxDepth(),
                min                  = root.MinDepth()
            };
        }

        #endregion

        #region Nodes

        //Node used in the DLS function
        class DLSNode
        {
            public readonly DLSNode parent;
            public readonly string  action;
            public readonly int     height;
            public DLSNode( DLSNode p ,
                            string  a ,
                            int     h )
            {
                parent = p;
                action = a;
                height = h;
            }
            public string GetSolution()
            {
                DLSNode sol = this;
                string  ans = string.Empty;
                while (sol.parent != null)
                {
                    ans = sol.action + " " + ans;
                    sol = sol.parent;
                }
                return ans;
            }
        }
		
        //Node used in the BestFS function
        class Node
        {
            public readonly Node       parent;
            public readonly string     action;
            public readonly RushHour   state;
            public readonly int        heuristic;
            public readonly int        height;
            public readonly List<Node> sons;
            public Node( Node     p  ,
                         RushHour st ,
                         string   a  ,
                         int      h  )
            {
                sons   = new List<Node>();
                action = a;
                parent = p;
                height = h;
                if (st != null)
                {
                    state     = st.Clone();
                    heuristic = st.Heuristic1() + h;
                }
                if (p != null)
                {
                    p.sons.Add(this);
                }
            }
            public int MinDepth() => sons.Count > 0 ? sons.Select(n => n.MinDepth()).Min() + 1 : 0;
            public int MaxDepth() => sons.Count > 0 ? sons.Select(n => n.MaxDepth()).Max() + 1 : 0;
            public string GetSolution()
            {
                Node   sol = this;
                string ans = string.Empty;
                while (sol.parent != null)
                {
                    ans = sol.action + " " + ans;
                    sol = sol.parent;
                }
                return ans;
            }
        }

        #endregion

        #region Constructors
        
        //Private empty C'tor
        private RushHour() { }

        //Public C'tor, save all the car names, positions and moving axes.
		//It also transfers the level's string to a 2D-array.
        public RushHour( string level )
        {
            board = new char[BOARD_SIZE, BOARD_SIZE];
            cars  = new Dictionary<char, CarDetails>(BOARD_SIZE * BOARD_SIZE / 2);
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    char item = level[i * BOARD_SIZE + j];
                    if(item != '.')
                    {
                        if(cars.ContainsKey(item))
                        {
                            var car  = cars[item];
                            car.axis = car.posX == j ? CarDetails.Axis.Y:
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
        
        //Number of cars blocking the red car
        int Heuristic1()
        {
            int count  = 0;
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

        #endregion

        ///<summary>
        ///Create a deep copy of the RushHour class
        ///</summary>
        ///<returns>
        ///A copy of RushHour class
        /// </returns>
        public RushHour Clone()
        {
            var dict = new Dictionary<char, CarDetails>(cars.Count);
            foreach (var item in cars)
            {
                var car = item.Value;
                dict.Add(item.Key, new CarDetails
                {
                    size = car.size,
                    axis = car.axis,
                    posX = car.posX,
                    posY = car.posY
                });
            }
            return new RushHour
            {
                cars  = dict,
                board = (char[,])board.Clone()
            };
        }

        //Check if there aren't any cars that are blocking the way
        bool CanReachGoal() => Heuristic1() == 0;

        ///<summary>Returns string that represent the board state</summary>
        public string GetHash()
        {
            string s = string.Empty;
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    s += board[i, j];
                }
            }
            return s;
        }

        ///<summary>Moves a car according to a given action.</summary>
        ///<example>For example: AU2 moves car A up by 2 cells </example>
        void Move( string action )
        {
            if (action != null)
            {
                var car    = cars[action[0]];
                int length = action[2] & 15;
                for (int i = 0; i < length; i++)
                {
                    switch (action[1])
                    {
                        case 'U': //Move up
                            board[--car.posY         , car.posX]     = action[0];
                            board[car.posY + car.size, car.posX]     = '.';
                            break;
                        case 'D': //Move down
                            board[car.posY               , car.posX] = '.';
                            board[(car.posY++) + car.size, car.posX] = action[0];
                            break;
                        case 'L': //Move left
                            board[car.posY, --car.posX]              = action[0];
                            board[car.posY, car.posX + car.size]     = '.';
                            break;
                        case 'R': //Move right
                            board[car.posY, car.posX]                = '.';
                            board[car.posY, (car.posX++) + car.size] = action[0];
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the opposite move
        /// </summary>
        /// <example>
        /// AL4 -> AR4, BU1 -> BD1
        /// </example>
        string OppositeMove( string action )
        {
            if(action != null)
            {   //Replace action's direction
                string d = action[1] == 'R' ? "L" :
                           action[1] == 'L' ? "R" :
                           action[1] == 'U' ? "D" :
                                              "U" ;
                action = action[0] + d + action[2];
            }
            return action;
        }

        //Return every possible move from current state.
        string[] PossibleMoves()
        {
            var moves = new List<string>();
            foreach (var p in cars)
            {
                var car = p.Value;
                int x1  = car.posX, x2 = car.posX + car.size - 1;
                int y1  = car.posY, y2 = car.posY + car.size - 1;
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

            return moves.ToArray();
        }

        //Min heap struct for the BestFS Node
        struct NodesMinHeap
        {
            //Heap data
            public readonly List<Node> nodes;
            
            //Number of items in the heap
            public int Count => nodes.Count;

            //Constructor
            public NodesMinHeap( Node first )
            {
                nodes = new List<Node>();
                Insert(first);
            }

            //Adds the new node to the heap
            public void Insert( Node node )
            {
                nodes.Add(node);
                int i = nodes.Count - 1, j;
                while (i > 0 && nodes[j = (i - 1) / 2].heuristic > nodes[i].heuristic)
                {   //while *parent(i) > *i:
                    Node temp = nodes[j]; //*i <=> *parent(i)
                    nodes[j]  = nodes[i]; //...
                    nodes[i]  = temp;     //...
                    i         = j;        //i = parent(i)
                }
            }

            //Remove min node from the heap and return it
            public Node Remove()
            {
                //Remove min value
                var top   = nodes[0];
                int count = nodes.Count - 1;
                nodes[0]  = nodes[count];
                nodes.RemoveAt(count);

                //Fix heap property if it's violated
                int index = 0;
                bool flag = nodes.Count > 0;
                while(flag)
                {
                    int smallest = index;
                    int l        = index + index + 1;
                    int r        = l + 1;
                    if ((l < count) && (nodes[l].heuristic < nodes[index].heuristic))
                    {
                        smallest = l;
                    }
                    if ((r < count) && (nodes[r].heuristic < nodes[smallest].heuristic))
                    {
                        smallest = r;
                    }
                    var temp        = nodes[smallest];
                    nodes[smallest] = nodes[index];
                    nodes[index]    = temp;
                    flag            = index != smallest;
                    index           = smallest;
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

            int waitingTime = 0;
            if ( args.Length == 0 ) waitingTime = 10;
            else waitingTime = Int32.Parse(args[0]);
            Stopwatch s           = new Stopwatch();
            TimeSpan  t           = TimeSpan.Zero;
            string[]  levels      = text.Split('\n');
            int       level       = 1;
            string    finalOutput = string.Empty;
            string    docPath     = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            int       avgDepth    = 0;

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "Lab1_Output.txt")))
            {
                foreach (var item in levels)
                {
                    var task = Task.Run(() => new RushHour(item).BestFS());
                    s.Start();
                    bool finished = task.Wait(TimeSpan.FromSeconds(waitingTime));
                    s.Stop();
                    t += s.Elapsed / levels.Length;
                    if (finished)
                    {
                        LabAnswer _tr = task.Result;
                        int len = _tr.solutionStr.Split(' ').Length - 1;
                        avgDepth += _tr.max;
                        outputFile.WriteLine("Level " + level++ + " - Succeded in " + len + " moves");
                        outputFile.WriteLine("Solution: "+_tr.solutionStr);
                        outputFile.WriteLine(String.Format("Number of nodes scanned:{0:D} | Depth to nodes ratio:{1:F3}", _tr.numberOfNodesScanned, _tr.dnRatio));
                        outputFile.WriteLine(String.Format("Maximum reached depth:{0}", _tr.max));
                        outputFile.WriteLine(String.Format("Minimum reached depth:{0}", _tr.min));
                    }
                    else
                    {
                        outputFile.WriteLine("Level " + level++ + "Failed");
                    }
                    Console.WriteLine(s.Elapsed);
                    s.Reset();
                }
                outputFile.WriteLine(String.Format("Avg search depth:{0:F3}",avgDepth / level));
                outputFile.WriteLine("Avg Time  = " + t);
            }
        }
    }
}