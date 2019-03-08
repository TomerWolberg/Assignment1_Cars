using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SuperUltraAwesomeAI
{
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

        //Constants
        public const int BOARD_SIZE      = 6;
        public const int RED_CAR_Y_INDEX = 2;

        //Fields
        Dictionary<char, CarDetails> cars;
        char[,]                      board;

        //Private empty C'tor
        private RushHour() { }
		
		//Returns board state as string (instead of 2D-array)
		public string GetBoardString()
		{
			string s = "";
			for(int i = 0; i < BOARD_SIZE; i++)
			{
				for(int j = 0; j < BOARD_SIZE; j++)
				{
					s += board[i, j];
				}
			}
			return s;
		}

		//Save all the car names, positions and moving axes.
		//It also transfer the level's string to 2D array
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
                            car.size++;
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

        //Check if there aren't any cars that are blocking the way
        bool CanReachGoal() => Heuristic1() == 0;

        //Distance from goal
        int Heuristic2() => BOARD_SIZE - cars['X'].posX;

        //BFS with blocked number of nodes at each level - Best Heuristic function
        const  int H3BFS = 5000;
        static int H3MOD = 0;
        static int H3Val = int.MaxValue;
        int Heuristic3()
        {
            if (--H3MOD == 0)
            {
                H3MOD = H3BFS;
            }
            else if (H3MOD < H3BFS / 2)
            {
                H3MOD = H3BFS;
                --H3Val;
            }
            return H3Val--;
        }

        //Create a deep copy of RushHour
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
                board = (char[,])board.Clone(),
            };
        }

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

        //returns the opposite move: AL4 -> AR4, BU1 -> BD1, ...
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

        static Random rand = new Random(); //used to randomize moves order
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

            //randomize the order of the moves
            for (int i = 0; i < moves.Count; i++)
            {
                int u    = rand.Next(i, moves.Count);
                var temp = moves[u];
                moves[u] = moves[i];
                moves[i] = temp;
            }

            return moves.ToArray();
        }

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
        }
		
        class Node
        {
            public readonly Node     parent;
            public readonly string   action;
            public readonly RushHour state;
            public readonly int      heuristic;
            public Node( Node     p ,
                         RushHour r ,
                         string   a )
            {
                action = a;
                parent = p;
                if (r != null)
                {
                    state     = r.Clone();
                    heuristic = state.Heuristic3(); //Selected heuristic function
                }
            }
        }
        class NodesMinHeap
        {
            public readonly List<Node> nodes = new List<Node>();
            public int Count { get => nodes.Count; }

            //Add new node to the heap
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

            //Remove min node from the heap and returns it
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
		
        //IDS uninformed search
        public string IDS()
        {
            string s;
            int i = 1;
            do  s = DLS(i++);
            while (s == "failed");
            return s;
        }

        //DLS uninformed search - recursive implementation
        public string DLS(int l)
        {
            DLSNode sol = null;

            void FindSolution(DLSNode n)
            {
                Move(n.action);
                string back = OppositeMove(n.action);
                if (CanReachGoal())
                {
                    sol = new DLSNode(n, "XR" + (BOARD_SIZE - cars['X'].posX), n.height + 1);
                }
                else
                {
                    if (n.height < l - 1 && sol == null)
                    {
                        string[] moves = PossibleMoves();
                        for (int i = 0; i < moves.Length && sol == null; i++)
                        {
                            FindSolution(new DLSNode(n, moves[i], n.height + 1));
                        }
                    }
                }
                Move(back);
            }
            FindSolution(new DLSNode(null, null, 0));
            string ans;
            if (sol != null)
            {
                ans = string.Empty;
                while (sol.parent != null)
                {
                    ans = sol.action + " " + ans;
                    sol = sol.parent;
                }
            }
            else
            {
                ans = "failed";
            }
            return ans;
        }

        //Best-first search informed search
        public string BestFS()
        {
            var  set   = new HashSet<string> { GetBoardString() };
            var  heap  = new NodesMinHeap();
            var  moves = PossibleMoves();
            Node ans   = null;

            //Insert all possible moves to the heap
            foreach (var move in moves)
            {
                RushHour r = Clone();
                r.Move(move);
                heap.Insert(new Node(null, r, move));
                set.Add(r.GetBoardString());
            }

            while (heap.Count > 0 && ans == null)
            {   //While there are possible moves and we didn't find solution
                var top = heap.Remove(); //Get from the heap the best move
                if (top.state.CanReachGoal())
                {   //If nothing blocks the red car
                    ans = new Node(top, null, "XR" + (BOARD_SIZE - cars['X'].posX));
                }
                else
                {   //Add to the heap every possible move from current state
                    foreach (var move in top.state.PossibleMoves())
                    {
                        RushHour r = top.state.Clone();
                        r.Move(move);
                        if (!set.Contains(r.GetBoardString()))
                        {
                            heap.Insert(new Node(top, r, move));
                        }
                    }
                }
            }
            //Get the solution string
            string s = string.Empty;
            while (ans != null)
            {
                s   = ans.action + " " + s;
                ans = ans.parent;
            }
            return s;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            string k =
@"AA...OP..Q.OPXXQ.OP..Q..B...CCB.RRR.
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

            Stopwatch s      = new Stopwatch();
            double    c      = 0;
            TimeSpan  t      = TimeSpan.Zero;
            string[]  levels =  k.Split('\n');

            foreach (var item in levels)
            {
                var task = Task.Run(() => new RushHour(item).BestFS());
                s.Start();
                bool finished = task.Wait(TimeSpan.FromSeconds(10));
                s.Stop();
                t += s.Elapsed / levels.Length;
                if (finished)
                {
                    Console.WriteLine("Succeded");
                    c += task.Result.Split(' ').Length / (double)levels.Length;
                }
                else
                {
                    Console.WriteLine("Failed");
                }
                s.Stop();
                Console.WriteLine(s.Elapsed);
                s.Reset();
            }
            Console.WriteLine("Nodes = " + c);
            Console.WriteLine("Time  = " + t);
        }
    }
}