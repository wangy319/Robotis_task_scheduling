using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InputTest
{
    public class ConstraintProblem
    {
        /// <summary>
        /// Internal class to handle nodes callback functions for each
        /// owned by each node.
        /// </summary>
        public class ConstraintModelNode
        {
            public int Id { get; set; }
            public Node NodeDescription { get; set; }

            public IntVar ActiveVar { get; set; }
            public IntVar Next { get; set; } // Domain: 0 - 64
            public IntVar Prev { get; set; } // Domain: 0 - 64
            public IntVar Route { get; set; } // Domain: 0 - 2
            public IntVar ArrivalTime { get; set; } // Domain: 0 - Max
            public IntVar StartTime { get; set; }
            public IntVar DepartureTime { get; set; } // Domain: 0 - Max
            public IntVar GripLoad { get; set; } // Domain: 0 - Max
            public IntVar SuctionLoad { get; set; } // Domain: 0 - Max
            public IntervalVar NodeInterval { get; set; }

            public IntervalVar IntervalRoute1 { get; set; }
            public IntervalVar IntervalRoute2 { get; set; }

            public IntVar[] performedPerRoute { get; set; }

            public int[] durationPerRoute { get; set; }

            public travelDistanceCallback tdc { get; set; }

            public ConstraintModelNode(int _Id)
            {
                Id = _Id;
                tdc = new travelDistanceCallback(this);
            }

            public IntExpr getTijForNode()
            {
                return solver.MakeElement(tdc, Next, Route);
            }

            public IntExpr getTijPrevForNdoe()
            {
                return solver.MakeElement(tdc, Prev, Route);
            }

            /// <summary>
            /// Each node owns it's traveldistance function so that it can find the distance
            /// _to_ itself from any other node given route.
            /// 
            /// Current can be exchanged to calling an external C library and calculate
            /// traveltime during runtime
            /// </summary>
            public class travelDistanceCallback : LongResultCallback2
            {
                public ConstraintModelNode parent;

                public travelDistanceCallback(ConstraintModelNode _parent)
                {
                    parent = _parent;
                }
                public override long Run(long arg0, long arg1)
                {
                    long returnValue = 0;
                    int matrixValue = 0;
                    switch (arg1)
                    {
                        case 1:
                            // Return transitionValue for arm 1; prev(j) -> j
                            matrixValue = cellData.TravellingTimeArm1[parent.Id][arg0];
                            if (matrixValue > 0)
                            {
                                returnValue = matrixValue;
                            }
                            else if (matrixValue == 0)
                            {
                                returnValue = 0;
                            }
                            else
                            {
                                returnValue = 0;
                            }
                            //returnValue = 2;
                            break;
                        case 2:
                            // Return transitionValue for arm 2; prev(j) -> j
                            matrixValue = cellData.TravellingTimeArm2[parent.Id][arg0];
                            if (matrixValue > 0)
                            {
                                returnValue = matrixValue;
                            }
                            else if (matrixValue == 0)
                            {
                                returnValue = 0;
                            }
                            else
                            {
                                returnValue = 0;
                            }
                            //returnValue = 2;
                            break;
                        case 0:
                            // Current should be a dummy transitionValue that we set.
                            returnValue = 0;
                            break;
                        default:
                            // Current should be imposible! 
                            // Change later for flexibility!
                            Debug.Assert(true == false);
                            returnValue = 0;
                            break;
                    }
                    return returnValue;
                }
            }
        }

        #region Constraint problem data & variables
        public static CellDescription cellData;
        public static SolverInputParameters inputParams;
        public static Solver solver;

        public IntVar[] Route;
        public IntVar[] Next;
        public IntVar[] Prev;
        public IntVar[] Active;

        public IntVar[] AccumulatedGrip;
        public IntVar[] AccumulatedSuction;
        public IntVar[] AccumulatedTime;
        public IntVar[] DepartureTime;

        public IntervalVar[] NodeIntervals;
        public IntervalVar[] IntervaralsInRoute1;
        public IntervalVar[] IntervaralsInRoute2;

        public IntVar[] Performednesses;
        public IntVar[] StartTimes;

        public IntVar[] sameRoute;
        public IntVar[,] Precedes;

        public IntVar[] NodeGripContribution;
        public IntVar[] NodeSuctionContribution;


        public IntVar[] TripTime;
        public IntVar[] PrevTripTime;
        public IntVar[] GripLoad;
        public IntVar[] SuctionLoad;

        public ConstraintModelNode[] Fixture;
        public ConstraintModelNode[] FixtureOperations;
        public ConstraintModelNode[] Tray;
        public ConstraintModelNode[] Camera;

        public ConstraintModelNode[][] ComponentNodes;

        public IntVar TotalCycleTime;
        public IntVar TotalDistanceNext;
        public IntVar TotalDistancePrev;
        public IntVar Route1Nodes;
        public IntVar Route2Nodes;

        public OptimizeVar CycleTime;

        public ConstraintModelNode[] ModelNodes { get; set; }

        public SequenceVar[] disjunctiveSequences;

        SequenceVar sequenceRoute1;
        SequenceVar sequenceRoute2;

        private int suctionCapacity = 2;
        private int gripCapacity = 1;
        private int earliestStartTime = 0;
        private int latestStartTime;

        int upperTravel;
        int sum;

        private OptimizeVar Objective2;
        private OptimizeVar Objective3;

        #endregion

        public ConstraintProblem(CellDescription cd, SolverInputParameters sip) 
        {
            cellData = cd;
            inputParams = sip;
        }

        public void Solve()
        {
            Init();
            InitializeVariables();
            PostConstraints();
            Search();
        }

        private void Init()
        {
            solver = new Solver("ThesisSolver");

            if (inputParams.PreprocessingUpperBound)
            {
                sum = 0;
                for (int i = 0; i < cellData.NbNodes; i++)
                {
                    int arm1 = cellData.TravellingTimeArm1[i].Max();
                    int arm2 = cellData.TravellingTimeArm2[i].Max();

                    int travelsum = arm1 > arm2 ? arm1 : arm2;

                    int durationsum = cellData.Nodes[i].Durations.Max();
                    sum += durationsum + travelsum;
                }
                latestStartTime = sum; 
            }
            else
            {
                latestStartTime = 500000;
            }
        }

        private void InitializeVariables()
        {
            /**
            * Decision variables                  
            */
            #region Intermediate lists
            List<IntVar> initNextVarList = new List<IntVar>();
            List<IntVar> initPrevVarList = new List<IntVar>();
            List<IntVar> initRouteVarList = new List<IntVar>();
            List<IntVar> initGripVarList = new List<IntVar>();
            List<IntVar> initSuctionVarList = new List<IntVar>();
            List<IntVar> initTimeVarList = new List<IntVar>();
            List<IntVar> initDepartureVarList = new List<IntVar>();
            List<IntVar> initActiveVarList = new List<IntVar>();
            List<IntVar> initTripTimeVarList = new List<IntVar>();
            List<IntVar> initStartTimeVarList = new List<IntVar>();

            List<IntervalVar> initIntervalVarList = new List<IntervalVar>();
            List<IntervalVar> initIntervalVarRoute1List = new List<IntervalVar>();
            List<IntervalVar> initIntervalVarRoute2List = new List<IntervalVar>();

            List<ConstraintModelNode> initFixtureList = new List<ConstraintModelNode>();
            List<ConstraintModelNode> initFixtureTappingList = new List<ConstraintModelNode>();
            List<ConstraintModelNode> initFixtureOperationList = new List<ConstraintModelNode>();
            List<ConstraintModelNode> initTrayList = new List<ConstraintModelNode>();
            List<ConstraintModelNode> initCameraList = new List<ConstraintModelNode>();

            List<ConstraintModelNode> initModelNodeList = new List<ConstraintModelNode>();
            List<ConstraintModelNode>[] ComponentNodeLists = new List<ConstraintModelNode>[cellData.NbComponents];
            for (int i = 0; i < cellData.NbComponents; i++)
                ComponentNodeLists[i] = new List<ConstraintModelNode>();
            #endregion

            #region Setting up the decision variables from input data

            #region Values for domain correction
            int firstEndNode = cellData.NbNodes - cellData.NbRoutes;
            int lastEndNode = cellData.NbNodes - 1;
            int firstStartNode = 0;//cellData.NbNodes - cellData.NbRoutes * 2;
            int lastStartNode = cellData.NbRoutes - 1; //cellData.NbNodes - cellData.NbRoutes - 1;
            int dummyNode = cellData.NbNodes;
            #endregion


            foreach (Node currentNode in cellData.Nodes)
            {
                #region Creating the node, setting upp the variables
                // Create a model instance of the node and add it to init list
                ConstraintModelNode node = new ConstraintModelNode(currentNode.Id);
                initModelNodeList.Add(node);


                // Create decision variables for the node TODO: Names can be more describing
                IntVar active = solver.MakeBoolVar("ActiveForNode" + currentNode.Id);
                IntVar next = solver.MakeIntVar(0, cellData.NbNodes, "NextFromNode " + currentNode.Id);
                IntVar prev = solver.MakeIntVar(0, cellData.NbNodes, "PrevFromNode " + currentNode.Id);
                IntVar route = solver.MakeIntVar(0, cellData.NbRoutes - 1, "RouteForNode " + currentNode.Id);
                IntVar ls = solver.MakeIntVar(0, suctionCapacity, "LoadSuctionAfter " + currentNode.Id);
                IntVar lg = solver.MakeIntVar(0, gripCapacity, "LoadGripAfter " + currentNode.Id);
                IntVar arrivalTime = solver.MakeIntVar(0, Int64.MaxValue, "ArrivalTimeAt " + currentNode.Id);
                IntVar departureTime = solver.MakeIntVar(0, Int64.MaxValue, "DepartureTimeFrom " + currentNode.Id);
                IntVar startTime = solver.MakeIntVar(0, Int64.MaxValue, "StartTimeAt" + currentNode.Id);

                #region Setting up Interval variables
                int[] durations = currentNode.Durations;

                IntervalVar intervalRoute1 =
                    solver.MakeFixedDurationIntervalVar(
                        earliestStartTime,
                        latestStartTime,
                        durations[1],
                        true,
                        "Route1Interval" + currentNode.Id);

                IntervalVar intervalRoute2 =
                    solver.MakeFixedDurationIntervalVar(
                        earliestStartTime,
                        latestStartTime,
                        durations[2],
                        true,
                        "Route2Interval" + currentNode.Id);
                #endregion

                #region Add variables to initial variable lists
                initActiveVarList.Add(active);
                initNextVarList.Add(next);
                initPrevVarList.Add(prev);
                initRouteVarList.Add(route);
                initSuctionVarList.Add(ls);
                initGripVarList.Add(lg);
                initTimeVarList.Add(arrivalTime);
                initDepartureVarList.Add(departureTime);
                initStartTimeVarList.Add(startTime);
                initIntervalVarRoute1List.Add(intervalRoute1);
                initIntervalVarRoute2List.Add(intervalRoute2);
                #endregion

                ComponentNodeLists[currentNode.Component].Add(node);
                #region Add variables to the node.
                node.NodeDescription = currentNode;
                node.ActiveVar = active;
                node.Next = next;
                node.Prev = prev;
                node.Route = route;
                node.GripLoad = lg;
                node.SuctionLoad = ls;
                node.ArrivalTime = arrivalTime;
                node.StartTime = startTime;
                node.DepartureTime = departureTime;
                node.IntervalRoute1 = intervalRoute1;
                node.IntervalRoute2 = intervalRoute2;
                node.durationPerRoute = durations;
                node.performedPerRoute = new IntVar[] { solver.MakeIntConst(0), intervalRoute1.PerformedExpr().Var(), intervalRoute2.PerformedExpr().Var() };
                #endregion

                #endregion

                #region Setting variable domains dependent and adding nodes to lists depending on node parameters
                #region If node is not end or start
                if (!currentNode.IsEnd && !currentNode.IsStart)                  //"middle" nodes
                {
                    prev.RemoveInterval(firstEndNode, lastEndNode);              //Removing end nodes
                    prev.RemoveValue(dummyNode);                                 //Removing dummy node

                    next.RemoveInterval(firstStartNode, lastStartNode);          //Removing start nodes
                    next.RemoveValue(dummyNode);                                 //Removing dummy node


                    // If current node is a fixture and has a component
                    if (currentNode.Fixture > 0)
                    {
                        if (currentNode.Component > 0)
                        {
                            initFixtureList.Add(node);

                            if (currentNode.FixtOrd > 0)
                            {
                                lg.SetValue(0);
                            }
                        }
                        else
                        {
                            initFixtureOperationList.Add(node);
                        }
                        if (currentNode.FixtOrd > 0)
                        {
                            lg.SetValue(0);
                        }
                    }
                    else if (currentNode.Tray != 0)
                    {
                        initTrayList.Add(node);
                    }
                    else if (currentNode.Camera != 0)
                    {
                        initCameraList.Add(node);
                    }

                    // Nodes without dummy replicate is not allowed in dummy route
                    if (currentNode.IsAirFlow || currentNode.IsOutput)
                    {
                        route.RemoveValue(0);
                    }

                }
                #endregion
                #region If node is start node
                else if (currentNode.IsStart) //Start nodes
                {
                    //prev.SetValue(dummyNode);                             //Setting prev to dummy node                        

                    //next.RemoveInterval(firstStartNode, lastStartNode);   //Removing start nodes                        
                    next.RemoveValue(dummyNode);                            //Removing dummy node


                    ls.SetValue(0);                                         //Holding nothing in suction cup(s) at start
                    lg.SetValue(0);                                         //Holding nothing in gripper at start
                    //Routes start at arrivalTime = 0

                    route.SetValue((long)currentNode.Id % cellData.NbRoutes);                      //TODO: Ludde would like to set this in a way where the user easier can see what is set

                    if (route.Value() != 0)
                    {
                        prev.SetValue(dummyNode);
                        arrivalTime.SetValue(1);
                        departureTime.SetValue(1);
                    }

                }
                #endregion
                #region If node is end node
                else //End nodes
                {
                    //prev.RemoveInterval(firstEndNode, lastEndNode);       //Removing end nodes
                    prev.RemoveValue(dummyNode);                            //Removing dummy node

                    //next.SetValue(dummyNode);                             //Setting to next dummy node                                                

                    ls.SetValue(0);                                         //Holding nothing in suction cup(s) when finished   (TODO: might change due to changed model of where in the cycle to start)
                    lg.SetValue(0);                                         //Holding nothing in gripper at start               (TODO: might change due to changed model of where in the cycle to start)


                    // This will allways work, I promise!
                    // Order end nodes to have route inputIndex in order (1, ... , nbRoutes-1, 0)
                    route.SetValue((long)(currentNode.Id - (firstEndNode - 1)) % cellData.NbRoutes);
                    Console.WriteLine("Node {0} route {1}", currentNode.Id, route.Value());


                    if (route.Value() != 0)
                    {
                        next.SetValue(dummyNode);
                    }

                }
                #endregion
                #endregion

                if (inputParams.PreprocessingVariables)
                {
                    #region Preprocessing of variable domains
                    #region Pre-process Route variables, i.e. remove route if out of range
                    if (cellData.TravellingTimeArm1[currentNode.Id][currentNode.Id] < 0)
                    {
                        node.Route.RemoveValue(1);
                    }
                    else if (cellData.TravellingTimeArm2[currentNode.Id][currentNode.Id] < 0)
                    {
                        node.Route.RemoveValue(2);
                    } 
                    #endregion
                    #region Pre-process Cumulative Variables, i.e. Gripper & Suction Cup Load


                    bool isStart = false;
                    bool isEnd = false;
                    bool isGripTray = false;
                    bool isSuctionTray = false;
                    bool isCamera = false;
                    bool isGripDOFixture = false;
                    bool isSuctionDOFixture = false;
                    bool isTappOrPeel = false;
                    bool isIsFinalAssembly = false;
                    bool isIsOutput = false;
                    bool isAir = false;

                    generateNodeStatus(node, out isStart, out isEnd, out isGripTray, out isSuctionTray, out isCamera, out isGripDOFixture, out isSuctionDOFixture, out isTappOrPeel, out isIsFinalAssembly, out isIsOutput, out isAir);

                    //IsGripTray, IsSuctionTray, IsCamera, IsGripDOFixture, IsSuctionDOFixture, IsTappOrPeel, IsIsFinalAssembly, IsIsOutput, IsAir

                    if (!isStart && !isEnd)
                    {
                        int setNodeGripValue = -1;
                        int setNodeSuctionValue = -1;

                        int removeNodeGripValue = -1;
                        int removeNodeSuctionValue = -1;

                        if (isGripTray)
                        {
                            setNodeGripValue = 1; // Inactive must be 1                            
                        }

                        if (isSuctionTray)
                        {
                            removeNodeSuctionValue = 0;
                        }

                        if (isCamera)
                        {
                            removeNodeSuctionValue = 0;
                        }

                        if (isGripDOFixture)
                        {
                            setNodeGripValue = 0;
                        }

                        if (isSuctionDOFixture)
                        {
                            removeNodeSuctionValue = 2;
                        }

                        if (isTappOrPeel)
                        {
                            setNodeGripValue = 0;
                        }

                        if (isIsFinalAssembly)
                        {
                            setNodeGripValue = 0;
                            setNodeSuctionValue = 0;
                        }

                        if (isIsOutput)
                        {
                            setNodeGripValue = 0;
                            setNodeSuctionValue = 0;
                        }

                        if (isAir)
                        {
                            if (cellData.Components[node.NodeDescription.Component].grip)
                                setNodeGripValue = 1; //Doesn't have inactive

                            if (cellData.Components[node.NodeDescription.Component].suction)
                                removeNodeSuctionValue = 0; //Doesn't have inactive
                        }


                        // Gripper 
                        if (setNodeGripValue >= 0)
                        {
                            node.GripLoad.SetValue(setNodeGripValue);
                        }
                        else if (removeNodeGripValue >= 0)
                        {
                            node.GripLoad.RemoveValue(removeNodeGripValue);
                        }

                        // Suction
                        if (setNodeSuctionValue >= 0)
                        {
                            node.SuctionLoad.SetValue(setNodeSuctionValue);
                        }
                        else if (removeNodeSuctionValue >= 0)
                        {
                            node.SuctionLoad.RemoveValue(removeNodeSuctionValue);
                        }
                    }

                    #endregion
                    #endregion 
                }

            }

            // Convert lists to arrays!
            #region Initializing node arrays from node lists
            Next = initNextVarList.ToArray();
            Prev = initPrevVarList.ToArray();
            Route = initRouteVarList.ToArray();
            AccumulatedGrip = initGripVarList.ToArray();
            AccumulatedSuction = initSuctionVarList.ToArray();
            AccumulatedTime = initTimeVarList.ToArray();
            DepartureTime = initDepartureVarList.ToArray();
            Active = initActiveVarList.ToArray();
            ModelNodes = initModelNodeList.ToArray();
            Fixture = initFixtureList.ToArray();
            Tray = initTrayList.ToArray();
            Camera = initCameraList.ToArray();
            FixtureOperations = initFixtureOperationList.ToArray();
            NodeIntervals = initIntervalVarList.ToArray();
            StartTimes = initStartTimeVarList.ToArray();
            IntervaralsInRoute1 = initIntervalVarRoute1List.ToArray();
            IntervaralsInRoute2 = initIntervalVarRoute2List.ToArray(); 
        	#endregion

            /**
            * 
            * Current part of the setup divides all nodes
            * into a jagged array. 1st dimension is the
            * common component for nodes in that array.
            * 
            */
            ComponentNodes = new ConstraintModelNode[cellData.NbComponents][];
            for (int i = 0; i < cellData.NbComponents; i++)
            {
                Console.Write("Component {0}: ", i);
                ComponentNodes[i] = ComponentNodeLists[i].ToArray();
                foreach (ConstraintModelNode node in ComponentNodes[i])
                    Console.Write(node.Id + " ");

                Console.Write(Environment.NewLine);
            }


            #endregion
        }

        private void PostConstraints()
        {
            /**
            * Constraints
            */
            // Constraint used for testing consistency of the model.
            // Will never fail, if check constraint == False, then model is inconsistent
            Constraint alwaysTrue = solver.MakeTrueConstraint();

            #region Core constraints
            if (inputParams.CoreConstraints)
            {
                #region Flag 0
                solver.Add(solver.MakeAllDifferentExcept(Next, cellData.NbNodes));
                solver.Add(solver.MakeAllDifferentExcept(Prev, cellData.NbNodes));
                // Also adding the stronger alldiff for 
                solver.Add(solver.MakeAllDifferent((from ConstraintModelNode node in ModelNodes where !node.NodeDescription.IsStart select node.Prev).ToArray(), true));
                solver.Add(solver.MakeAllDifferent((from ConstraintModelNode node in ModelNodes where !node.NodeDescription.IsEnd select node.Next).ToArray(), true));

                solver.Add(solver.MakeNoCycle(Next, Active));

                DisjunctiveConstraint ct1 = solver.MakeDisjunctiveConstraint(IntervaralsInRoute1, "Route1Sequence");
                DisjunctiveConstraint ct2 = solver.MakeDisjunctiveConstraint(IntervaralsInRoute2, "Route2Sequence");

                sequenceRoute1 = ct1.SequenceVar();
                sequenceRoute2 = ct2.SequenceVar();

                // Nasty hard coded shit!
                solver.Add(sequenceRoute1.Next(0) == 2);
                solver.Add(sequenceRoute2.Next(0) == 3);

                IntVar one = solver.MakeIntConst(1);

                #region Intermediate lists
                List<IntVar> initTripTimeVarList = new List<IntVar>();
                List<IntVar> initPrevTimeVarList = new List<IntVar>();
                #endregion
                // Set all variables connected to the starting points
                foreach (ConstraintModelNode node in ModelNodes)
                {
                    int i = node.Id;

                    // Set nodeInterval to unperformed if node is in dummy route
                    if (true)
                    {
                        solver.Add(solver.MakeIsDifferentCstCt(node.Route, 0, node.ActiveVar));

                        if (!solver.CheckConstraint(alwaysTrue))
                            Console.WriteLine("Route 0 unperformed fails for node: {0}", node.Id);

                        for (int j = 1; j < cellData.NbRoutes; j++)
                            solver.Add(solver.MakeIsEqualCstCt(node.Route, j, node.performedPerRoute[j]));

                        solver.Add(solver.MakeIsDifferentCstCt(node.Next, node.Id, node.ActiveVar));
                        solver.Add(solver.MakeIsDifferentCstCt(node.ArrivalTime, 0, node.ActiveVar));
                        solver.Add(solver.MakeIsDifferentCstCt(node.StartTime, 0, node.ActiveVar));
                    }

                    #region Element constraints to set variables from routes
                    IntVar[] possibleNext = new IntVar[] { 
                        solver.MakeIntConst(node.Id), 
                        solver.MakeDifference(sequenceRoute1.Next(i + 1), one).Var(), 
                        solver.MakeDifference(sequenceRoute2.Next(i + 1), one).Var() 
                    };

                    solver.Add(solver.MakeElementEquality(possibleNext, node.Route, node.Next));

                    IntVar[] endTimes = new IntVar[] {
                        solver.MakeIntConst(0),
                        node.IntervalRoute1.SafeEndExpr(0).Var(),
                        node.IntervalRoute2.SafeEndExpr(0).Var()
                    };

                    solver.Add(solver.MakeElementEquality(endTimes, node.Route, node.DepartureTime));

                    IntVar[] startTimes = new IntVar[] {
                        solver.MakeIntConst(0),
                        node.IntervalRoute1.SafeStartExpr(0).Var(),
                        node.IntervalRoute2.SafeStartExpr(0).Var()
                    };

                    solver.Add(solver.MakeElementEquality(startTimes, node.Route, node.StartTime));
                    #endregion

                    if (!node.NodeDescription.IsStart && !node.NodeDescription.IsEnd)
                    #region Nodes "in the middle", J.e. not start nor end nodes
                    {
                        // Path: Next[J] == j <=> Prev[J] == j                        
                        solver.Add(solver.MakeElementEquality(Next, node.Prev, i)); //TODO: Även för end node, då denna pekar från "J -> prev(J) -> J"

                        // Path: Next[J] == j <=> Prev[J] == j  
                        solver.Add(solver.MakeElementEquality(Prev, node.Next, i)); //TODO: Även för start node, då denna pekar från "J -> next(J) -> J"

                        // Route[J] == Route[next[J]]                        
                        solver.Add(solver.MakeElementEquality(Route, node.Next, node.Route)); //TODO: Även för start node, då denna pekar från "J -> next(J) -> route(next(J))"

                        // Route[J] == Route[prev[J]]
                        solver.Add(solver.MakeElementEquality(Route, node.Prev, node.Route)); //TODO: Även för end node, då denna pekar från "J -> prev(J) -> route(prev(J))"

                        IntExpr tij = node.getTijForNode();
                        initTripTimeVarList.Add(tij.Var());

                        IntExpr tji = node.getTijPrevForNdoe();
                        initPrevTimeVarList.Add(tji.Var());

                        solver.Add(solver.MakeElement(AccumulatedTime, node.Next) == node.DepartureTime + tij);
                        //solver.Add(solver.MakeElement(DepartureTime, node.Prev) == node.ArrivalTime - tji); This does not work!(?)

                        // Time window constraints                        
                        solver.Add(node.ArrivalTime <= node.StartTime);

                        //if (!solver.CheckConstraint(alwaysTrue))
                        //    Console.WriteLine("Core constraints fails for middle node: {0}", node.Id);
                    }
                    #endregion
                    else if (node.NodeDescription.IsStart)
                    #region Start nodes
                    {

                        // Path: Next[J] == j <=> Prev[J] == j
                        solver.Add(solver.MakeElementEquality(Prev, node.Next, i)); //Även för start node, då denna pekar från "J -> next(J) -> J"

                        // Route[J] == Route[next[J]]
                        solver.Add(solver.MakeElementEquality(Route, node.Next, node.Route)); //Även för start node, då denna pekar från "J -> next(J) -> J"                            

                        IntVar tij = solver.MakeIntConst(0);
                        initTripTimeVarList.Add(tij);

                        IntVar tji = solver.MakeIntConst(0);
                        initPrevTimeVarList.Add(tji);

                        solver.Add(solver.MakeElement(AccumulatedTime, node.Next) == node.DepartureTime + tij);

                        // Set values of start nodes
                        // Can be done here or preprocessed
                        //solver.Add(node.ArrivalTime == 0);
                        //solver.Add(node.StartTime == 0);
                        //solver.Add(node.DepartureTime == 0);

                        //if (!solver.CheckConstraint(alwaysTrue))
                        //    Console.WriteLine("Core constraints fails for start node: {0}", node.Id);
                    }
                    #endregion
                    else if (node.NodeDescription.IsEnd)
                    #region End nodes
                    {

                        // Path: Next[J] == j <=> Prev[J] == j
                        solver.Add(solver.MakeElementEquality(Next, node.Prev, i)); //Även för end node, då denna pekar från "J -> prev(J) -> J"

                        // Route[J] == Route[Next[J]]
                        solver.Add(solver.MakeElementEquality(Route, node.Prev, node.Route)); //Även för end node, då denna pekar från "J -> prev(J) -> J"                    

                        IntVar tij = solver.MakeIntConst(0);
                        initTripTimeVarList.Add(tij);

                        IntVar tji = solver.MakeIntConst(0);
                        initPrevTimeVarList.Add(tji);

                        solver.Add(node.DepartureTime == node.ArrivalTime);

                        //if (!solver.CheckConstraint(alwaysTrue))
                        //    Console.WriteLine("Core constraints fails for end node: {0}", node.Id);
                    }
                    #endregion
                }
                TripTime = initTripTimeVarList.ToArray();
                PrevTripTime = initPrevTimeVarList.ToArray();

                #endregion
            } 
            #endregion

            #region Precedence constraints
            // For each route in tray, if equal to
            // route in camera and component is equal,
            // then accumulatedTime[tray] < accumulatedTime[camera]
            // And the same for camera -> fixture

            // DO NOT CHANGE FROM TEST! 
            // CHANGE BETWEEN DFA AND CONJUNCTIVE IN
            // PRECEDENCE CONSTRAINT METHOD IN END OF FILE
            /**
                * Current part will set precedence constraints between fixtures and components
                * Given that each component have a fixture dropoff and (at least) dimension2
                * fixture operation.
                * 
                * Dropof < Operation#1 < Operation#2
                * 
                * Operations need an ordering such that 
                * for all operations of order N must come after
                * operations of order N-1
                * 
                * 
                */
            if (inputParams.PrecedenceConstraintsComponent)
            {
                #region Flag 1

                Console.WriteLine("Precedence constraint active");
                #region Set Tray < Camera < Fixture

                #region Tray, Fixture & Camera precedences

                foreach (ConstraintModelNode tray in Tray)
                {
                    foreach (ConstraintModelNode camera in Camera)
                    {
                        if (tray.NodeDescription.Component == camera.NodeDescription.Component)
                        {
                            if (!PrecedenceConstraint(tray, camera))
                                Console.WriteLine("Failed to add constraint!");
                            //Console.WriteLine("Adding Tray < Camera for {0}, {1}", tray.Id, camera.Id);
                        }
                    }

                    foreach (ConstraintModelNode fixture in Fixture)
                    {
                        if (tray.NodeDescription.Component == fixture.NodeDescription.Component)
                        {
                            if (!PrecedenceConstraint(tray, fixture))
                                Console.WriteLine("Failed to add constraint!");
                            //Console.WriteLine("Adding Tray < Fixture for {0}, {1}", tray.Id, fixture.Id);
                        }
                    }
                }
                foreach (ConstraintModelNode camera in Camera)
                {
                    foreach (ConstraintModelNode fixture in Fixture)
                    {
                        if (camera.NodeDescription.Component == fixture.NodeDescription.Component)
                        {
                            if (!PrecedenceConstraint(camera, fixture))
                                Console.WriteLine("Failed to add constraint!");
                            //Console.WriteLine("Adding Camera < Fixture for {0}, {1}", camera.Id, fixture.Id);
                        }
                    }
                }
                #endregion

                #endregion
                #region Set airflow between pickup and dropoff for this component

                // We know for this case that we only have dimension2 airflow node && component
                ConstraintModelNode airFlow =
                    (from node in ModelNodes
                     where node.NodeDescription.IsAirFlow
                     select node).ToArray()[0];

                // Find tray and fixture with same component as airflow node
                ConstraintModelNode[] nodesConnectedToAirflow =
                    (from node in ComponentNodes[airFlow.NodeDescription.Component]
                     where node.NodeDescription.Fixture != 0
                     || node.NodeDescription.Tray != 0
                     select node).ToArray();

                foreach (ConstraintModelNode node in nodesConnectedToAirflow)
                {
                    if (node.NodeDescription.Tray != 0)
                    {
                        PrecedenceConstraint(node, airFlow);
                    }
                    if (node.NodeDescription.Fixture != 0)
                    {
                        PrecedenceConstraint(airFlow, node);
                    }
                }
                #endregion
                #endregion
            }

            if (inputParams.PrecedenceConstraintsFixture)
            {
                #region Flag 2
                Console.WriteLine("Peeling precedence constraint active");
                #region Set ordering on fixtures
                for (int c = 1; c < cellData.NbComponents; c++)
                {
                    #region Set total ordering for component specific nodes
                    for (int i = 1 /*Start from level 1 (Root is dropof)*/;
                        i <= cellData.Components[c].NbFixtureOperations /*Current variable holds number of operations on fixture for component*/;
                        i++)
                    {
                        ConstraintModelNode[] CurOpsLevel = (
                            from node in ComponentNodes[c]
                            where node.NodeDescription.Fixture != 0
                            && node.NodeDescription.FixtOrd == i
                            select node).ToArray();

                        ConstraintModelNode[] PrevOpsLevel = (
                            from node in ComponentNodes[c]
                            where node.NodeDescription.Fixture != 0
                            && node.NodeDescription.FixtOrd == i - 1
                            select node).ToArray();

                        // Post precedence constraint
                        foreach (ConstraintModelNode nextInOrder in CurOpsLevel)
                        {
                            foreach (ConstraintModelNode prevInOrder in PrevOpsLevel)
                            {
                                if (nextInOrder.NodeDescription.Fixture
                                    ==
                                    prevInOrder.NodeDescription.Fixture)
                                {
                                    if (!PrecedenceConstraint(prevInOrder, nextInOrder))
                                        Console.WriteLine("Failed to add constraint!");
                                }
                            }
                        }
                    }
                    #endregion

                    if (c > 1) /* We don't want to post constraints for the first component*/
                    {
                        int subAssembly = cellData.Components[c].subAssembly;
                        ConstraintModelNode[] thisComponent =
                            (from node in Fixture
                             where node.NodeDescription.Component == c && node.NodeDescription.FixtOrd == 0
                             select node).ToArray();

                        ConstraintModelNode[] prevComponent;

                        if (false)
                        {
                            prevComponent =
                                (from node in Fixture
                                 where node.NodeDescription.Component == c - 1
                                 && cellData.Components[node.NodeDescription.Component].subAssembly == subAssembly
                                 select node).ToArray();
                        }
                        else
                        {
                            if (cellData.Components[c - 1].subAssembly == subAssembly)
                            {
                                prevComponent = ComponentNodes[c - 1];
                            }
                            else
                            {
                                prevComponent = new ConstraintModelNode[0];
                            }
                        }

                        foreach (ConstraintModelNode current in thisComponent)
                            foreach (ConstraintModelNode previous in prevComponent)
                            {
                                PrecedenceConstraint(previous, current);
                            }
                    }

                }
                #endregion
                #region Set airflow between pickup and dropoff for this component

                // We know for this case that we only have dimension2 airflow node && component
                ConstraintModelNode airFlow =
                    (from node in ModelNodes
                     where node.NodeDescription.IsAirFlow
                     select node).ToArray()[0];

                // Find tray and fixture with same component as airflow node
                ConstraintModelNode[] nodesConnectedToAirflow =
                    (from node in ComponentNodes[airFlow.NodeDescription.Component]
                     where node.NodeDescription.Fixture != 0
                     || node.NodeDescription.Tray != 0
                     select node).ToArray();

                foreach (ConstraintModelNode node in nodesConnectedToAirflow)
                {
                    if (node.NodeDescription.Tray != 0)
                    {
                        PrecedenceConstraint(node, airFlow);
                    }
                    if (node.NodeDescription.Fixture != 0)
                    {
                        PrecedenceConstraint(airFlow, node);
                    }
                }
                #endregion
                #region Set Final assembly after all other fixtures & output after final assembly
                ConstraintModelNode[] finalAssemblies =
                        (from node in FixtureOperations
                         where node.NodeDescription.IsFinalAssembly
                         select node).ToArray();

                ConstraintModelNode outPut = (from node in ModelNodes where node.NodeDescription.IsOutput select node).ToArray()[0];

                foreach (ConstraintModelNode final in finalAssemblies)
                {
                    foreach (ConstraintModelNode fixture in Fixture)
                    {
                        if (!PrecedenceConstraint(fixture, final))
                            Console.WriteLine("Failed to add constraint!");
                    }
                    if (!PrecedenceConstraint(final, outPut))
                        Console.WriteLine("Failed to add constraint!");
                }
                #endregion
                #endregion
            }
            #endregion

            #region Post disjunctive constraints
            if (inputParams.DisjunctiveConstraints)
            {
                #region Flag 3
                List<SequenceVar> sequences = new List<SequenceVar>();

                Console.WriteLine("disjunctive constraint active");
                #region Fixture nodes (including operations) are disjunctive
                // To use separate fixtures, uncomment for loop.
                //for (int i = 1; i < 3; i++)
                //{
                ConstraintModelNode[] fixtureIntervalNodes =
                    (from node in ModelNodes
                     where node.NodeDescription.Fixture > 0//== i
                     select node).ToArray();

                List<IntervalVar> intervalss = new List<IntervalVar>();
                foreach (ConstraintModelNode node in fixtureIntervalNodes)
                {
                    intervalss.Add(node.IntervalRoute1);
                    intervalss.Add(node.IntervalRoute2);
                }
                DisjunctiveConstraint cst = solver.MakeDisjunctiveConstraint(intervalss.ToArray(), "Fixture");// + i);
                sequences.Add(cst.SequenceVar());
                solver.Add(cst);
                //}
                #endregion

                #region VISION: Camera and tray nodes with the same physical inputIndex are disjunctive
                // These nodes are created with multiples of number of components, therefor i = physical inputIndex.
                for (int i = 1; i < cellData.NbComponents; i++)
                {
                    ConstraintModelNode[] trayAndCameraNodes =
                        (from node in ModelNodes
                         where node.NodeDescription.Tray == i || node.NodeDescription.Camera == i
                         select node).ToArray();

                    List<IntervalVar> intervals = new List<IntervalVar>();
                    foreach (ConstraintModelNode node in trayAndCameraNodes)
                    {
                        intervals.Add(node.IntervalRoute1);
                        intervals.Add(node.IntervalRoute2);
                    }

                    DisjunctiveConstraint ct = solver.MakeDisjunctiveConstraint(intervals.ToArray(), "Tray/Camera" + i);
                    sequences.Add(ct.SequenceVar());
                    solver.Add(ct);
                }
                #endregion

                #region COMPONENT: Nodes with the same component are disjunctive
                for (int i = 1; i < cellData.NbComponents; i++)
                {
                    //if (!solver.CheckConstraint(alwaysTrue))
                    //    Console.WriteLine("disjunctive failing at component " + i);
                    ConstraintModelNode[] nodes = ComponentNodes[i];

                    List<IntervalVar> intervals = new List<IntervalVar>();
                    foreach (ConstraintModelNode node in nodes)
                    {
                        intervals.Add(node.IntervalRoute1);
                        intervals.Add(node.IntervalRoute2);
                    }
                    IntervalVar[] disjunctiveIntervals = intervals.ToArray();
                    DisjunctiveConstraint ct = solver.MakeDisjunctiveConstraint(disjunctiveIntervals, "ComponentDisjunctive" + i);
                    sequences.Add(ct.SequenceVar());
                    solver.Add(ct);
                }
                #endregion
                disjunctiveSequences = sequences.ToArray();
                #endregion
            }
            #endregion

            #region Add GCC to make unused trays fall to route 0
            if (inputParams.TrayGCCConstraints)
            {
                #region Flag 4
                Console.WriteLine("Tray GCC constraint active");
                int step = (int)Math.Sqrt((double)Tray.Length);
                for (int i = 0; i < Tray.Length; i += step)
                {
                    IntVar[] gccRowArray = new IntVar[step];
                    IntVar[] gccColArray = new IntVar[step];
                    for (int j = 0; j < step; j++)
                    {
                        gccRowArray[j] = Tray[j + i].Route;
                        gccColArray[j] = Tray[j * step + i / step].Route;
                    }
                    solver.Add(solver.MakeCount(gccRowArray, 0, step - 1));
                    solver.Add(solver.MakeCount(gccColArray, 0, step - 1));
                }

                //if (!solver.CheckConstraint(alwaysTrue))
                //    Console.WriteLine("Tray GCC constraint failing!");

                #endregion
            }
            #endregion

            #region Add constraints to set unused fixtures to route 0
            if (inputParams.FixtureTransitionConstraints)
            {
                #region Flag 5
                Console.WriteLine("Fixture route constraint active");
                // Ordering is important to get the dfa correct
                // Looks like [Component 1[FixtureOrd1[Fixture1, Fixture 2], FixtureOrd2[ ... ], ...], Component 2[ ... ], ...]
                ConstraintModelNode[] routes =
                    (from node in Fixture
                     orderby node.NodeDescription.Fixture
                     orderby node.NodeDescription.FixtOrd
                     orderby node.NodeDescription.Component
                     select node).ToArray();

                int[,] TransitionMatrix = makeTransitionMatrix(routes);

                IntVar[] FixtureRoutes = (from node in routes select node.Route).ToArray();
                int acceptingState = routes.Length * 2;

                #region old hard coded dfa
                int[,] transitionMatrix = new int[,]
                    {
                        {1,0,2},
                        {2,0,4},
                        {4,1,6},
                        {4,2,6},
                        {6,1,8},
                        {6,2,8},
                        {8,1,10},
                        {8,2,10},
                        {10,1,12},
                        {10,2,12},
                        {12,1,14},
                        {12,2,14},
                        {14,0,16},
                        {16,0,18},
                        {18,0,20},
                        {1,1,3}, 
                        {1,2,3},  
                        {3,1,5}, 
                        {3,2,5}, 
                        {5,0,7},
                        {7,0,9},
                        {9,0,11},
                        {11,0,13},
                        {13,0,15},
                        {15,1,17},
                        {15,2,17},
                        {17,1,19},
                        {17,2,19},
                        {19,1,20},
                        {19,2,20}
                    };
                #endregion

                IntTupleSet transitions = new IntTupleSet(TransitionMatrix.GetLength(1));
                transitions.InsertAll(TransitionMatrix);
                solver.Add(solver.MakeTransitionConstraint(FixtureRoutes, transitions, 1, new int[] { acceptingState }));
                //if (!solver.CheckConstraint(alwaysTrue))
                //    Console.WriteLine("Fixture route constraint failing!");

                #region Add GCC to make unused final assembly fall to route 0
                IntVar[] IsFinalAssemblyRoute = (from node in ModelNodes where node.NodeDescription.IsFinalAssembly select node.Route).ToArray();
                solver.Add(solver.MakeCount(IsFinalAssemblyRoute, 0, 1));
                #endregion
                #endregion

            }
            #endregion

            #region Add GCC to make unused camera fall to route 0
            if (inputParams.CameraGCCConstraints)
            {
                #region Flag 6
                Console.WriteLine("Camera GCC constraint active");
                int photoComponents = Camera.Length / (cellData.NbComponents - 1);
                for (int i = 0; i < photoComponents; i++)
                {
                    IntVar[] gccComponentCameraArray = new IntVar[cellData.NbComponents - 1]; // We have one "dummy" component that we need to compensate for
                    for (int j = 0; j < cellData.NbComponents - 1; j++)
                    {
                        gccComponentCameraArray[j] = Camera[i + j * photoComponents].Route;
                    }
                    solver.Add(solver.MakeCount(gccComponentCameraArray, 0, cellData.NbComponents - 2));
                }
                //if (!solver.CheckConstraint(alwaysTrue))
                //    Console.WriteLine("Camera GCC constraint failing!");
                #endregion
            }
            #endregion

            #region Constraint to make Fixture, Tray and camera go on the same route given component.
            // For each node with one component, they must all be on the same route or route 0
            if (inputParams.ComponentRouteConsistencyConstraints)
            {
                #region Flag 7

                Console.WriteLine("Component transition constraint active");
                for (int component = 1; component < cellData.NbComponents; component++)
                {
                    IntVar[] trayArray =
                        (from node in ComponentNodes[component]
                         where node.NodeDescription.Tray != 0
                         || (node.NodeDescription.Fixture != 0 && node.NodeDescription.FixtOrd == 0)
                         orderby node.NodeDescription.Fixture descending
                         select node.Route).ToArray();

                    IntVar[] cameraArray =
                        (from node in ComponentNodes[component]
                         where node.NodeDescription.Camera != 0
                         || (node.NodeDescription.Fixture != 0 && node.NodeDescription.FixtOrd == 0)
                         orderby node.NodeDescription.Fixture descending
                         select node.Route).ToArray();

                    // Ugly code...
                    // Current is the same for tray and camera!
                    // TODO: MAKE NICE LOOKS LIKE SHIT!
                    int[,] transitionMatrix = new int[,]
                        {
                            {1,0,3}, //
                            {1,1,2}, // These are the starting states
                            {1,2,4}, // Pushes the DFA to follow a route
                            {2,0,5}, // of ones or twos complete with 
                            {3,1,5}, // zeros.
                            {3,2,6}, //
                            {4,0,6}, 
                            {5,0,7}, // Transitions on the path 
                            {5,1,7}, // with only dimension2 or matrixIndex_i
                            {7,0,9}, 
                            {7,1,9},
                            {9,0,11}, 
                            {9,1,11},
                            {11,0,13}, 
                            {11,1,13},
                            {13,0,15}, 
                            {13,1,15},
                            {6,0,8}, // Transitions on the path
                            {6,2,8}, // with only fromState or matrixIndex_i
                            {8,0,10}, 
                            {8,2,10},
                            {10,0,12},
                            {10,2,12},
                            {12,0,14},
                            {12,2,14},
                            {14,0,16},
                            {14,2,16}
                        };
                    IntTupleSet transitions = new IntTupleSet(transitionMatrix.GetLength(1));
                    transitions.InsertAll(transitionMatrix);
                    solver.Add(solver.MakeTransitionConstraint(trayArray, transitions, 1, new int[] { 15, 16 }));
                    if (cellData.Components[component].suction)
                        solver.Add(solver.MakeTransitionConstraint(cameraArray, transitions, 1, new int[] { 15, 16 }));

                    //if (!solver.CheckConstraint(alwaysTrue))
                    //    Console.WriteLine("Component route constraint failing!");
                }
                #endregion
            }
            #endregion

            #region Set airflow to the same route as fixture for component with airflow
            if (inputParams.AirFlowRouteConsistencyConstraints)
            {
                Console.WriteLine("Airgun transition constraint active");
                IntVar[] airFlowWithFixtures =
                    (from node in ModelNodes
                     where
                     (node.NodeDescription.Fixture > 0
                     && node.NodeDescription.FixtOrd == 0
                     && node.NodeDescription.Component == cellData.NodeWithAirFlow)
                     || node.NodeDescription.IsAirFlow
                     orderby node.NodeDescription.Fixture descending
                     select node.Route).ToArray();

                int[,] dfa = new int[,]
                {
                    {1,0,3}, //
                    {1,1,2}, // These are the starting states
                    {1,2,4}, // Pushes the DFA to follow a route
                    {2,0,5}, // of ones or twos complete with 
                    {3,1,5}, // zeros.
                    {3,2,6}, //
                    {4,0,6}, 
                    {5,1,7},
                    {6,2,7}
                };

                IntTupleSet airFlowDFA = new IntTupleSet(dfa.GetLength(1));
                airFlowDFA.InsertAll(dfa);
                solver.Add(solver.MakeTransitionConstraint(airFlowWithFixtures, airFlowDFA, 1, new int[] { 7 }));

                //if (!solver.CheckConstraint(alwaysTrue))
                //    Console.WriteLine("Airflow transition constraint failing!");
            }
            #endregion

            #region Cumulative constraints
            if (inputParams.CumulativeConstraints)
            {
                #region Flag 9
                foreach (ConstraintModelNode node in ModelNodes)
                {
                    bool isStart = false;
                    bool isEnd = false;
                    bool isGripTray = false;
                    bool isSuctionTray = false;
                    bool isCamera = false;
                    bool isGripDOFixture = false;
                    bool isSuctionDOFixture = false;
                    bool isTappOrPeel = false;
                    bool isIsFinalAssembly = false;
                    bool isIsOutput = false;
                    bool isAir = false;

                    generateNodeStatus(node, out isStart, out isEnd, out isGripTray, out isSuctionTray, out isCamera, out isGripDOFixture, out isSuctionDOFixture, out isTappOrPeel, out isIsFinalAssembly, out isIsOutput, out isAir);

                    //IsGripTray, IsSuctionTray, IsCamera, IsGripDOFixture, IsSuctionDOFixture, IsTappOrPeel, IsIsFinalAssembly, IsIsOutput, IsAir
                    if (!isStart && !isEnd)
                    {
                        // Default values - do not add or loose anything from the previous node

                        int toAdd2Gripper = 0;
                        int toAdd2SuctionCups = 0;

                        if (isGripTray)
                        {
                            toAdd2Gripper = 1;
                        }


                        if (isSuctionTray)
                        {
                            toAdd2SuctionCups = 1;
                        }

                        if (isCamera)
                        {
                        }

                        if (isGripDOFixture)
                        {
                            toAdd2Gripper = -1;
                        }

                        if (isSuctionDOFixture)
                        {
                            toAdd2SuctionCups = -1;
                        }

                        if (isTappOrPeel)
                        {
                        }

                        if (isIsFinalAssembly)
                        {
                        }

                        if (isIsOutput)
                        {
                        }

                        if (isAir)
                        {
                        }

                        // ~RegExp on Gripper
                        int[] acceptedGripperStates = new int[0];

                        IntTupleSet gripDFATupleSet = postDFA(toAdd2Gripper, node.GripLoad, 0, 1, out acceptedGripperStates);

                        IntVar gripElement = solver.MakeElement(AccumulatedGrip, node.Prev).Var();
                        IntVar[] gripDFAVariables = new IntVar[] { node.Route, node.GripLoad, gripElement };

                        Constraint gripRegular = solver.MakeTransitionConstraint(
                                                    gripDFAVariables, gripDFATupleSet, 0, acceptedGripperStates);
                        solver.Add(gripRegular);


                        // ~RegExp on Suction
                        int[] acceptedSuctionStates = new int[0];
                        IntTupleSet suctionDFATupleSet = postDFA(toAdd2SuctionCups, node.SuctionLoad, 0, 2, out acceptedSuctionStates);

                        IntVar suctionElement = solver.MakeElement(AccumulatedSuction, node.Prev).Var();
                        IntVar[] suctionDFAVariables = new IntVar[] { node.Route, node.SuctionLoad, suctionElement };

                        Constraint suctionRegular = solver.MakeTransitionConstraint(
                                suctionDFAVariables, suctionDFATupleSet, 0, acceptedSuctionStates);
                        solver.Add(suctionRegular);
                    }
                }
                #endregion
            } 
            #endregion

            #region Objective functions
            List<IntVar> armTimeAtEndNodeList = new List<IntVar>();
            foreach (Node node in cellData.Nodes)
            {
                //armTimeAtEndNodeList.Add(AccumulatedTime[node.Id]);
                armTimeAtEndNodeList.Add(DepartureTime[node.Id]);
            }

            TotalCycleTime = solver.MakeMax(armTimeAtEndNodeList.ToArray()).Var();
            CycleTime = solver.MakeMinimize(TotalCycleTime.Var(), 1);

            //TotalDistanceNext = solver.MakeSum(TripTime).Var();
            //Objective2 = solver.MakeMinimize(TotalDistanceNext, 1);

            //TotalDistancePrev = solver.MakeSum(PrevTripTime).Var();
            //Objective3 = solver.MakeMinimize(solver.MakeSum(TotalDistanceNext, TotalDistancePrev).Var(),1); 
            #endregion

            #region Hard coded routes
            //Set route for testing
            if (inputParams.HardCoded)
            {
                #region Flag 10
                if (inputParams.Naive)
                {
                    #region Naive Route

                    //Route 1:
                    ModelNodes[1].Next.SetValue(16);
                    ModelNodes[16].Next.SetValue(35);
                    ModelNodes[35].Next.SetValue(46);
                    ModelNodes[46].Next.SetValue(56);
                    ModelNodes[56].Next.SetValue(10);
                    ModelNodes[10].Next.SetValue(69);
                    ModelNodes[69].Next.SetValue(7);
                    ModelNodes[7].Next.SetValue(30);
                    ModelNodes[30].Next.SetValue(28);
                    ModelNodes[28].Next.SetValue(50);
                    ModelNodes[50].Next.SetValue(60);
                    ModelNodes[60].Next.SetValue(47);
                    ModelNodes[47].Next.SetValue(57);
                    ModelNodes[57].Next.SetValue(68);
                    ModelNodes[68].Next.SetValue(71);


                    ModelNodes[1].Route.SetValue(1);
                    ModelNodes[16].Route.SetValue(1);
                    ModelNodes[35].Route.SetValue(1);
                    ModelNodes[46].Route.SetValue(1);
                    ModelNodes[56].Route.SetValue(1);
                    ModelNodes[10].Route.SetValue(1);
                    ModelNodes[69].Route.SetValue(1);
                    ModelNodes[7].Route.SetValue(1);
                    ModelNodes[30].Route.SetValue(1);
                    ModelNodes[28].Route.SetValue(1);
                    ModelNodes[50].Route.SetValue(1);
                    ModelNodes[60].Route.SetValue(1);
                    ModelNodes[47].Route.SetValue(1);
                    ModelNodes[57].Route.SetValue(1);
                    ModelNodes[68].Route.SetValue(1);


                    //Route 2:
                    ModelNodes[2].Next.SetValue(23);
                    ModelNodes[23].Next.SetValue(48);
                    ModelNodes[48].Next.SetValue(58);
                    ModelNodes[58].Next.SetValue(19);
                    ModelNodes[19].Next.SetValue(49);
                    ModelNodes[49].Next.SetValue(59);
                    ModelNodes[59].Next.SetValue(63);
                    ModelNodes[63].Next.SetValue(65);
                    ModelNodes[65].Next.SetValue(70);
                    ModelNodes[70].Next.SetValue(72);


                    ModelNodes[2].Route.SetValue(2);
                    ModelNodes[23].Route.SetValue(2);
                    ModelNodes[48].Route.SetValue(2);
                    ModelNodes[58].Route.SetValue(2);
                    ModelNodes[19].Route.SetValue(2);
                    ModelNodes[49].Route.SetValue(2);
                    ModelNodes[59].Route.SetValue(2);
                    ModelNodes[63].Route.SetValue(2);
                    ModelNodes[65].Route.SetValue(2);
                    ModelNodes[70].Route.SetValue(2);

                    //Route 3:

                    ModelNodes[0].Route.SetValue(0);
                    ModelNodes[3].Route.SetValue(0);
                    ModelNodes[4].Route.SetValue(0);
                    ModelNodes[5].Route.SetValue(0);
                    ModelNodes[6].Route.SetValue(0);
                    ModelNodes[8].Route.SetValue(0);
                    ModelNodes[9].Route.SetValue(0);
                    ModelNodes[11].Route.SetValue(0);
                    ModelNodes[12].Route.SetValue(0);
                    ModelNodes[13].Route.SetValue(0);
                    ModelNodes[14].Route.SetValue(0);
                    ModelNodes[15].Route.SetValue(0);
                    ModelNodes[17].Route.SetValue(0);
                    ModelNodes[18].Route.SetValue(0);
                    ModelNodes[20].Route.SetValue(0);
                    ModelNodes[21].Route.SetValue(0);
                    ModelNodes[22].Route.SetValue(0);
                    ModelNodes[24].Route.SetValue(0);
                    ModelNodes[25].Route.SetValue(0);
                    ModelNodes[26].Route.SetValue(0);
                    ModelNodes[27].Route.SetValue(0);
                    ModelNodes[29].Route.SetValue(0);
                    ModelNodes[31].Route.SetValue(0);
                    ModelNodes[32].Route.SetValue(0);
                    ModelNodes[33].Route.SetValue(0);
                    ModelNodes[34].Route.SetValue(0);
                    ModelNodes[36].Route.SetValue(0);
                    ModelNodes[37].Route.SetValue(0);
                    ModelNodes[38].Route.SetValue(0);
                    ModelNodes[39].Route.SetValue(0);
                    ModelNodes[40].Route.SetValue(0);
                    ModelNodes[41].Route.SetValue(0);
                    ModelNodes[42].Route.SetValue(0);
                    ModelNodes[43].Route.SetValue(0);
                    ModelNodes[44].Route.SetValue(0);
                    ModelNodes[45].Route.SetValue(0);
                    ModelNodes[51].Route.SetValue(0);
                    ModelNodes[52].Route.SetValue(0);
                    ModelNodes[53].Route.SetValue(0);
                    ModelNodes[54].Route.SetValue(0);
                    ModelNodes[55].Route.SetValue(0);
                    ModelNodes[61].Route.SetValue(0);
                    ModelNodes[62].Route.SetValue(0);
                    ModelNodes[64].Route.SetValue(0);
                    ModelNodes[66].Route.SetValue(0);
                    ModelNodes[67].Route.SetValue(0);


                    ModelNodes[0].ActiveVar.SetValue(0);
                    ModelNodes[1].ActiveVar.SetValue(1);
                    ModelNodes[2].ActiveVar.SetValue(1);
                    ModelNodes[3].ActiveVar.SetValue(0);
                    ModelNodes[4].ActiveVar.SetValue(0);
                    ModelNodes[5].ActiveVar.SetValue(0);
                    ModelNodes[6].ActiveVar.SetValue(0);
                    ModelNodes[7].ActiveVar.SetValue(1);
                    ModelNodes[8].ActiveVar.SetValue(0);
                    ModelNodes[9].ActiveVar.SetValue(0);
                    ModelNodes[10].ActiveVar.SetValue(1);
                    ModelNodes[11].ActiveVar.SetValue(0);
                    ModelNodes[12].ActiveVar.SetValue(0);
                    ModelNodes[13].ActiveVar.SetValue(0);
                    ModelNodes[14].ActiveVar.SetValue(0);
                    ModelNodes[15].ActiveVar.SetValue(0);
                    ModelNodes[16].ActiveVar.SetValue(1);
                    ModelNodes[17].ActiveVar.SetValue(0);
                    ModelNodes[18].ActiveVar.SetValue(0);
                    ModelNodes[19].ActiveVar.SetValue(1);
                    ModelNodes[20].ActiveVar.SetValue(0);
                    ModelNodes[21].ActiveVar.SetValue(0);
                    ModelNodes[22].ActiveVar.SetValue(0);
                    ModelNodes[23].ActiveVar.SetValue(1);
                    ModelNodes[24].ActiveVar.SetValue(0);
                    ModelNodes[25].ActiveVar.SetValue(0);
                    ModelNodes[26].ActiveVar.SetValue(0);
                    ModelNodes[27].ActiveVar.SetValue(0);
                    ModelNodes[28].ActiveVar.SetValue(1);
                    ModelNodes[29].ActiveVar.SetValue(0);
                    ModelNodes[30].ActiveVar.SetValue(1);
                    ModelNodes[31].ActiveVar.SetValue(0);
                    ModelNodes[32].ActiveVar.SetValue(0);
                    ModelNodes[33].ActiveVar.SetValue(0);
                    ModelNodes[34].ActiveVar.SetValue(0);
                    ModelNodes[35].ActiveVar.SetValue(1);
                    ModelNodes[36].ActiveVar.SetValue(0);
                    ModelNodes[37].ActiveVar.SetValue(0);
                    ModelNodes[38].ActiveVar.SetValue(0);
                    ModelNodes[39].ActiveVar.SetValue(0);
                    ModelNodes[40].ActiveVar.SetValue(0);
                    ModelNodes[41].ActiveVar.SetValue(0);
                    ModelNodes[42].ActiveVar.SetValue(0);
                    ModelNodes[43].ActiveVar.SetValue(0);
                    ModelNodes[44].ActiveVar.SetValue(0);
                    ModelNodes[45].ActiveVar.SetValue(0);
                    ModelNodes[46].ActiveVar.SetValue(1);
                    ModelNodes[47].ActiveVar.SetValue(1);
                    ModelNodes[48].ActiveVar.SetValue(1);
                    ModelNodes[49].ActiveVar.SetValue(1);
                    ModelNodes[50].ActiveVar.SetValue(1);
                    ModelNodes[51].ActiveVar.SetValue(0);
                    ModelNodes[52].ActiveVar.SetValue(0);
                    ModelNodes[53].ActiveVar.SetValue(0);
                    ModelNodes[54].ActiveVar.SetValue(0);
                    ModelNodes[55].ActiveVar.SetValue(0);
                    ModelNodes[56].ActiveVar.SetValue(1);
                    ModelNodes[57].ActiveVar.SetValue(1);
                    ModelNodes[58].ActiveVar.SetValue(1);
                    ModelNodes[59].ActiveVar.SetValue(1);
                    ModelNodes[60].ActiveVar.SetValue(1);
                    ModelNodes[61].ActiveVar.SetValue(0);
                    ModelNodes[62].ActiveVar.SetValue(0);
                    ModelNodes[63].ActiveVar.SetValue(1);
                    ModelNodes[64].ActiveVar.SetValue(0);
                    ModelNodes[65].ActiveVar.SetValue(1);
                    ModelNodes[66].ActiveVar.SetValue(0);
                    ModelNodes[67].ActiveVar.SetValue(0);
                    ModelNodes[68].ActiveVar.SetValue(1);
                    ModelNodes[69].ActiveVar.SetValue(1);
                    ModelNodes[70].ActiveVar.SetValue(1);
                    ModelNodes[71].ActiveVar.SetValue(1);
                    ModelNodes[72].ActiveVar.SetValue(1);
                    ModelNodes[73].ActiveVar.SetValue(0);
                    #endregion
                }
                else if (inputParams.Reference)
                {
                    #region Reference route
                    //Route 1:
                    ModelNodes[1].Next.SetValue(17);
                    ModelNodes[17].Next.SetValue(6);
                    ModelNodes[6].Next.SetValue(30);
                    ModelNodes[30].Next.SetValue(35);
                    ModelNodes[35].Next.SetValue(46);
                    ModelNodes[46].Next.SetValue(56);
                    ModelNodes[56].Next.SetValue(63);
                    ModelNodes[63].Next.SetValue(65);
                    ModelNodes[65].Next.SetValue(47);
                    ModelNodes[47].Next.SetValue(10);
                    ModelNodes[10].Next.SetValue(31);
                    ModelNodes[31].Next.SetValue(69);
                    ModelNodes[69].Next.SetValue(50);
                    ModelNodes[50].Next.SetValue(71);


                    ModelNodes[1].Route.SetValue(1);
                    ModelNodes[17].Route.SetValue(1);
                    ModelNodes[6].Route.SetValue(1);
                    ModelNodes[30].Route.SetValue(1);
                    ModelNodes[35].Route.SetValue(1);
                    ModelNodes[46].Route.SetValue(1);
                    ModelNodes[56].Route.SetValue(1);
                    ModelNodes[63].Route.SetValue(1);
                    ModelNodes[65].Route.SetValue(1);
                    ModelNodes[47].Route.SetValue(1);
                    ModelNodes[10].Route.SetValue(1);
                    ModelNodes[31].Route.SetValue(1);
                    ModelNodes[69].Route.SetValue(1);
                    ModelNodes[50].Route.SetValue(1);


                    //Route 2:
                    ModelNodes[2].Next.SetValue(23);
                    ModelNodes[23].Next.SetValue(19);
                    ModelNodes[19].Next.SetValue(48);
                    ModelNodes[48].Next.SetValue(58);
                    ModelNodes[58].Next.SetValue(49);
                    ModelNodes[49].Next.SetValue(59);
                    ModelNodes[59].Next.SetValue(57);
                    ModelNodes[57].Next.SetValue(60);
                    ModelNodes[60].Next.SetValue(68);
                    ModelNodes[68].Next.SetValue(70);
                    ModelNodes[70].Next.SetValue(72);


                    ModelNodes[2].Route.SetValue(2);
                    ModelNodes[23].Route.SetValue(2);
                    ModelNodes[19].Route.SetValue(2);
                    ModelNodes[48].Route.SetValue(2);
                    ModelNodes[58].Route.SetValue(2);
                    ModelNodes[49].Route.SetValue(2);
                    ModelNodes[59].Route.SetValue(2);
                    ModelNodes[57].Route.SetValue(2);
                    ModelNodes[60].Route.SetValue(2);
                    ModelNodes[68].Route.SetValue(2);
                    ModelNodes[70].Route.SetValue(2);

                    //Route 3:

                    ModelNodes[0].Route.SetValue(0);
                    ModelNodes[3].Route.SetValue(0);
                    ModelNodes[4].Route.SetValue(0);
                    ModelNodes[5].Route.SetValue(0);
                    ModelNodes[7].Route.SetValue(0);
                    ModelNodes[8].Route.SetValue(0);
                    ModelNodes[9].Route.SetValue(0);
                    ModelNodes[11].Route.SetValue(0);
                    ModelNodes[12].Route.SetValue(0);
                    ModelNodes[13].Route.SetValue(0);
                    ModelNodes[14].Route.SetValue(0);
                    ModelNodes[15].Route.SetValue(0);
                    ModelNodes[16].Route.SetValue(0);
                    ModelNodes[18].Route.SetValue(0);
                    ModelNodes[20].Route.SetValue(0);
                    ModelNodes[21].Route.SetValue(0);
                    ModelNodes[22].Route.SetValue(0);
                    ModelNodes[24].Route.SetValue(0);
                    ModelNodes[25].Route.SetValue(0);
                    ModelNodes[26].Route.SetValue(0);
                    ModelNodes[27].Route.SetValue(0);
                    ModelNodes[28].Route.SetValue(0);
                    ModelNodes[29].Route.SetValue(0);
                    ModelNodes[32].Route.SetValue(0);
                    ModelNodes[33].Route.SetValue(0);
                    ModelNodes[34].Route.SetValue(0);
                    ModelNodes[36].Route.SetValue(0);
                    ModelNodes[37].Route.SetValue(0);
                    ModelNodes[38].Route.SetValue(0);
                    ModelNodes[39].Route.SetValue(0);
                    ModelNodes[40].Route.SetValue(0);
                    ModelNodes[41].Route.SetValue(0);
                    ModelNodes[42].Route.SetValue(0);
                    ModelNodes[43].Route.SetValue(0);
                    ModelNodes[44].Route.SetValue(0);
                    ModelNodes[45].Route.SetValue(0);
                    ModelNodes[51].Route.SetValue(0);
                    ModelNodes[52].Route.SetValue(0);
                    ModelNodes[53].Route.SetValue(0);
                    ModelNodes[54].Route.SetValue(0);
                    ModelNodes[55].Route.SetValue(0);
                    ModelNodes[61].Route.SetValue(0);
                    ModelNodes[62].Route.SetValue(0);
                    ModelNodes[64].Route.SetValue(0);
                    ModelNodes[66].Route.SetValue(0);
                    ModelNodes[67].Route.SetValue(0);

                    ModelNodes[0].ActiveVar.SetValue(0);
                    ModelNodes[1].ActiveVar.SetValue(1);
                    ModelNodes[2].ActiveVar.SetValue(1);
                    ModelNodes[3].ActiveVar.SetValue(0);
                    ModelNodes[4].ActiveVar.SetValue(0);
                    ModelNodes[5].ActiveVar.SetValue(0);
                    ModelNodes[6].ActiveVar.SetValue(1);
                    ModelNodes[7].ActiveVar.SetValue(0);
                    ModelNodes[8].ActiveVar.SetValue(0);
                    ModelNodes[9].ActiveVar.SetValue(0);
                    ModelNodes[10].ActiveVar.SetValue(1);
                    ModelNodes[11].ActiveVar.SetValue(0);
                    ModelNodes[12].ActiveVar.SetValue(0);
                    ModelNodes[13].ActiveVar.SetValue(0);
                    ModelNodes[14].ActiveVar.SetValue(0);
                    ModelNodes[15].ActiveVar.SetValue(0);
                    ModelNodes[16].ActiveVar.SetValue(0);
                    ModelNodes[17].ActiveVar.SetValue(1);
                    ModelNodes[18].ActiveVar.SetValue(0);
                    ModelNodes[19].ActiveVar.SetValue(1);
                    ModelNodes[20].ActiveVar.SetValue(0);
                    ModelNodes[21].ActiveVar.SetValue(0);
                    ModelNodes[22].ActiveVar.SetValue(0);
                    ModelNodes[23].ActiveVar.SetValue(1);
                    ModelNodes[24].ActiveVar.SetValue(0);
                    ModelNodes[25].ActiveVar.SetValue(0);
                    ModelNodes[26].ActiveVar.SetValue(0);
                    ModelNodes[27].ActiveVar.SetValue(0);
                    ModelNodes[28].ActiveVar.SetValue(0);
                    ModelNodes[29].ActiveVar.SetValue(0);
                    ModelNodes[30].ActiveVar.SetValue(1);
                    ModelNodes[31].ActiveVar.SetValue(1);
                    ModelNodes[32].ActiveVar.SetValue(0);
                    ModelNodes[33].ActiveVar.SetValue(0);
                    ModelNodes[34].ActiveVar.SetValue(0);
                    ModelNodes[35].ActiveVar.SetValue(1);
                    ModelNodes[36].ActiveVar.SetValue(0);
                    ModelNodes[37].ActiveVar.SetValue(0);
                    ModelNodes[38].ActiveVar.SetValue(0);
                    ModelNodes[39].ActiveVar.SetValue(0);
                    ModelNodes[40].ActiveVar.SetValue(0);
                    ModelNodes[41].ActiveVar.SetValue(0);
                    ModelNodes[42].ActiveVar.SetValue(0);
                    ModelNodes[43].ActiveVar.SetValue(0);
                    ModelNodes[44].ActiveVar.SetValue(0);
                    ModelNodes[45].ActiveVar.SetValue(0);
                    ModelNodes[46].ActiveVar.SetValue(1);
                    ModelNodes[47].ActiveVar.SetValue(1);
                    ModelNodes[48].ActiveVar.SetValue(1);
                    ModelNodes[49].ActiveVar.SetValue(1);
                    ModelNodes[50].ActiveVar.SetValue(1);
                    ModelNodes[51].ActiveVar.SetValue(0);
                    ModelNodes[52].ActiveVar.SetValue(0);
                    ModelNodes[53].ActiveVar.SetValue(0);
                    ModelNodes[54].ActiveVar.SetValue(0);
                    ModelNodes[55].ActiveVar.SetValue(0);
                    ModelNodes[56].ActiveVar.SetValue(1);
                    ModelNodes[57].ActiveVar.SetValue(1);
                    ModelNodes[58].ActiveVar.SetValue(1);
                    ModelNodes[59].ActiveVar.SetValue(1);
                    ModelNodes[60].ActiveVar.SetValue(1);
                    ModelNodes[61].ActiveVar.SetValue(0);
                    ModelNodes[62].ActiveVar.SetValue(0);
                    ModelNodes[63].ActiveVar.SetValue(1);
                    ModelNodes[64].ActiveVar.SetValue(0);
                    ModelNodes[65].ActiveVar.SetValue(1);
                    ModelNodes[66].ActiveVar.SetValue(0);
                    ModelNodes[67].ActiveVar.SetValue(0);
                    ModelNodes[68].ActiveVar.SetValue(1);
                    ModelNodes[69].ActiveVar.SetValue(1);
                    ModelNodes[70].ActiveVar.SetValue(1);
                    ModelNodes[71].ActiveVar.SetValue(1);
                    ModelNodes[72].ActiveVar.SetValue(1);
                    ModelNodes[73].ActiveVar.SetValue(0);
                    #endregion
                }
                #endregion

                #region Print routes
                //for (int r = 1; r < cellData.NbRoutes; r++)
                //{
                //    Console.WriteLine("Route {0}", r);
                //    for (int n = ModelNodes[r].Id; n < cellData.NbNodes; n = (int)ModelNodes[n].Next.Value())
                //    {
                //        if (n == r)
                //            Console.Write("{0} -> {1}", n, ModelNodes[n].Next.Value());
                //        else
                //            Console.Write(" -> {0}", ModelNodes[n].Next.Value());
                //    }
                //    Console.WriteLine("");
                //} 
                #endregion
            } 
            #endregion
        }

        private void Search()
        {
            #region Decision builder initialization

            DecisionBuilder nextDB = solver.MakePhase(Next, Solver.CHOOSE_PATH, Solver.ASSIGN_MIN_VALUE); //Solver.ASSIGN_RANDOM_VALUE);
            DecisionBuilder prevDB = solver.MakePhase(Prev, Solver.CHOOSE_PATH, Solver.ASSIGN_RANDOM_VALUE);//ASSIGN_MAX_VALUE);
            DecisionBuilder routeDB = solver.MakePhase(Route, Solver.CHOOSE_FIRST_UNBOUND, Solver.ASSIGN_RANDOM_VALUE);
            DecisionBuilder accTimeDB = solver.MakePhase(AccumulatedTime, Solver.CHOOSE_MIN_SIZE, Solver.ASSIGN_MAX_VALUE);
            DecisionBuilder intervalDB = solver.MakePhase(NodeIntervals, Solver.INTERVAL_DEFAULT);
            //DecisionBuilder accGripDB = solver.MakePhase(AccumulatedGrip, Solver.CHOOSE_MAX_REGRET_ON_MIN, Solver.ASSIGN_MIN_VALUE);
            DecisionBuilder accSuctionDB = solver.MakePhase(AccumulatedSuction, Solver.CHOOSE_MAX_REGRET_ON_MIN, Solver.ASSIGN_MIN_VALUE);
            DecisionBuilder objectiveDB = solver.MakePhase(TotalCycleTime, Solver.CHOOSE_HIGHEST_MAX, Solver.SPLIT_LOWER_HALF);
            //DecisionBuilder objectiveDB = solver.MakePhase(TotalCycleTime, Solver.CHOOSE_HIGHEST_MAX, Solver.ASSIGN_MIN_VALUE);
            //DecisionBuilder cycleTimeDB = solver.MakePhase(new IntVar[] { TotalCycleTime }, Solver.CHOOSE_DYNAMIC_GLOBAL_BEST, Solver.ASSIGN_MIN_VALUE);
            //DecisionBuilder tripTimeDB = solver.MakePhase(TripTime, Solver.CHOOSE_MIN_SIZE_HIGHEST_MAX, Solver.ASSIGN_MAX_VALUE);
            DecisionBuilder sequensing = solver.MakePhase(disjunctiveSequences, Solver.CHOOSE_MIN_SLACK_RANK_FORWARD);
            DecisionBuilder sequencing = solver.MakePhase(new SequenceVar[] { sequenceRoute1, sequenceRoute2 }, Solver.CHOOSE_MIN_SLACK_RANK_FORWARD);

            DecisionBuilder tripTimeDB2 = solver.MakePhase(CycleTime.Var(), Solver.CHOOSE_LOWEST_MIN, Solver.ASSIGN_MIN_VALUE);

            DecisionBuilder startTimeDB = solver.MakePhase(StartTimes, Solver.CHOOSE_FIRST_UNBOUND, Solver.ASSIGN_MIN_VALUE);
            DecisionBuilder departureDB = solver.MakePhase(DepartureTime, Solver.CHOOSE_HIGHEST_MAX, Solver.ASSIGN_MIN_VALUE);

            // Custom decision builders
            DecisionBuilder fixtureSelection = new SelectFixtureRouteHeuristic(this, 2);
            DecisionBuilder traySelection = new SelectTrayRouteHeuristic(this, 1);
            DecisionBuilder cameraSelection = new SelectCameraRouteHeuristic(this, 1);
            DecisionBuilder startSelection = new SetFirstNextHeuristic(this, 2);
            DecisionBuilder nextSelection = new SetNextFromLowestTimeHeuristicNew(this);
            DecisionBuilder nextForRoute1 = new SetNextFromLowestTimePerRouteHeuristic(this, 1);
            DecisionBuilder nextForRoute2 = new SetNextFromLowestTimePerRouteHeuristic(this, 2);
            #endregion

            #region Decision builder composition
            DecisionBuilder compositeDB = solver.Compose(new DecisionBuilder[] { routeDB, sequensing, sequencing, objectiveDB, startTimeDB });
            //DecisionBuilder compositeDB = solver.Compose(new DecisionBuilder[] { nextDB, startTimeDB });
            //DecisionBuilder compositeDB = solver.Compose(new DecisionBuilder[] { routeDB, sequencing, startTimeDB, objectiveDB});
            DecisionBuilder myCompositeDB = solver.Compose(new DecisionBuilder[] { fixtureSelection, traySelection, cameraSelection, nextDB });// nextForRoute1, nextForRoute2 });
            #endregion

            SolutionCollector sc = solver.MakeLastSolutionCollector();

            sc.Add(disjunctiveSequences); sc.Add(sequenceRoute1); sc.Add(sequenceRoute2);

            SearchLimit limits = solver.MakeLimit(inputParams.TimeLimit, inputParams.BranchLimit, inputParams.FailLimit, inputParams.SolutionLimit);

            /* Restarts */
            SearchMonitor searchRestart = solver.MakeConstantRestart(1000000);
            SearchMonitor log = solver.MakeSearchLog(1000);
            SearchMonitor optimizeLog = solver.MakeSearchLog(1000, CycleTime);
            int candidateNo = 1;
            if (inputParams.TreeSearch)
            {
                #region CP stuff
                if (inputParams.Optimize)
                {
                    //solver.NewSearch(myCompositeDB, CycleTime, sm, sc);
                    solver.NewSearch(compositeDB, CycleTime, optimizeLog, sc, limits);
                    //solver.NewSearch(myCompositeDB, Objective2, sm, sc);//, limits);
                }
                else
                {   
                    //solver.NewSearch(myCompositeDB, sm1);
                    solver.NewSearch(compositeDB, log, sc, limits);
                }
            }
            else if (inputParams.LocalSearch)
            {
                #region Local search stuff
                LocalSearchOperator lsOpt1 = solver.MakeOperator(Next, Solver.TWOOPT);
                LocalSearchOperator lsOpt2 = solver.MakeOperator(Next, Solver.OROPT);
                LocalSearchOperator lsOpt3 = solver.MakeOperator(Next, Solver.EXCHANGE);
                LocalSearchOperator lsOpt4 = solver.MakeOperator(Next, Solver.CROSS);
                LocalSearchOperator lsOpt5 = solver.MakeOperator(Next, Solver.MAKEACTIVE);
                LocalSearchOperator lsOpt6 = solver.MakeOperator(Next, Solver.MAKEINACTIVE);
                LocalSearchOperator lsOpt7 = solver.MakeOperator(Next, Solver.SWAPACTIVE);
                LocalSearchOperator lsOpt8 = solver.MakeOperator(Next, Solver.EXTENDEDSWAPACTIVE);
                LocalSearchOperator lsOpt9 = solver.MakeOperator(Next, Solver.PATHLNS);
                LocalSearchOperator lsOpt10 = solver.MakeOperator(Next, Solver.SIMPLELNS);
                LocalSearchOperator lsOpt11 = solver.MakeOperator(Next, Solver.UNACTIVELNS);
                LocalSearchOperator lsOpt12 = solver.MakeOperator(Next, Solver.LK);
                LocalSearchOperator lsOpt13 = solver.MakeOperator(Next, Solver.TSPOPT);
                LocalSearchOperator lsOpt14 = solver.MakeOperator(Next, Solver.TSPLNS);
                LocalSearchOperator[] ops = new LocalSearchOperator[] { lsOpt1, lsOpt2, lsOpt3, lsOpt4, lsOpt5, lsOpt6, lsOpt7, lsOpt8, lsOpt9, lsOpt10, lsOpt11, lsOpt12, lsOpt13, lsOpt14 };
                LocalSearchOperator concatLSOps = solver.ConcatenateOperators(ops);
                LocalSearchPhaseParameters lsParams = solver.MakeLocalSearchPhaseParameters(ops[1], solver.Compose(objectiveDB, startTimeDB), limits);
                SearchMonitor simulatedAnnealing = solver.MakeSimulatedAnnealing(false, CycleTime.Var(), 1, 100 * 1000);
                SearchMonitor tabuSearch = solver.MakeTabuSearch(false, CycleTime.Var(), 1, Next, 10, 10000, Int32.MaxValue);
                SolutionCollector collector = solver.MakeLastSolutionCollector(); collector.Add(Next); collector.AddObjective(TotalCycleTime);
                DecisionBuilder ls = solver.MakeLocalSearchPhase(Next, compositeDB, lsParams);
                solver.NewSearch(ls, collector, CycleTime, optimizeLog, limits);
                #endregion
            }

            while (solver.NextSolution())
            {
                bool writeToFile = true;
                if (!writeToFile)
                {
                    #region Print to console
                    Console.WriteLine("Solution " + candidateNo);
                    Console.Write(Environment.NewLine);
                    long LatestIndex = 0;
                    List<int> NodesInSolutionArray = new List<int>();
                    int NodesInSolution = 0;
                    long[] startNodes = new long[] { 1, 2 };
                    #region Some printout
                    for (int routeInd = 0; routeInd < startNodes.Length; routeInd++)
                    {
                        long route = Route[startNodes[routeInd]].Value();
                        Console.WriteLine("RouteId: {0}", route);
                        for (long inputIndex = startNodes[routeInd]; inputIndex != cellData.NbNodes; inputIndex = Next[inputIndex].Value()) //change from 0 being dummy node to N+M*2 being dummy node
                        {
                            // Current printout is nicer than the old dimension2, should be used in console print as well.
                            Console.WriteLine(
                                "{0}->{1,-5}\t | TT:{2,-5} | TW:{3}-{4,-15}\t | Dur:{5,-10} | Dep:{6,-5} | G:{7,-4} | S:{8,-4} |"
                                , new Object[] { 
                                        Prev[inputIndex].Value(), 
                                        ModelNodes[inputIndex].Id, 
                                        TripTime[inputIndex].Value(),
                                        AccumulatedTime[inputIndex].Min(),
                                        ModelNodes[inputIndex].StartTime.Min(),
                                        ModelNodes[inputIndex].durationPerRoute[route],
                                        DepartureTime[inputIndex].Min(), //Value(),
                                        AccumulatedGrip[inputIndex].Min(), 
                                        AccumulatedSuction[inputIndex].Min() 
                                    });
                            LatestIndex = inputIndex;
                            NodesInSolutionArray.Add((int)LatestIndex);
                            NodesInSolution++;
                        }
                        Console.Write(Environment.NewLine);
                        NodesInSolutionArray.Add((int)Next[LatestIndex].Value());

                        Console.WriteLine("Route ArrivalTime: {0}", AccumulatedTime[LatestIndex].Min());
                        Console.Write(Environment.NewLine);

                        NodesInSolution++;
                    }

                    Console.Write(Environment.NewLine);
                    Console.WriteLine("Total Cycle Time: {0}", TotalCycleTime.Min());
                    Console.Write(Environment.NewLine);
                    Console.WriteLine("Solution contains {0} nodes", NodesInSolution);
                    Console.Write(Environment.NewLine);
                    Console.WriteLine("End Candidate {0}", candidateNo);
                    Console.Write(Environment.NewLine);
                    #endregion
                    #endregion
                }
                else
                {
                    #region Print to file
                    WriteSolutionToFile(candidateNo);
                    Console.WriteLine("Leaving solution " + candidateNo);
                    #endregion
                }
                candidateNo++;
            }

            solver.EndSearch();
            Console.WriteLine("---------------");
            if ((inputParams.TreeSearch || inputParams.LocalSearch) && candidateNo == 1)
            {
                Console.WriteLine("Problem infeasible!");
            }
                #endregion
        }

        private IntTupleSet postDFA(int toAdd, IntVar cumulVar, int prevMin, int prevMax, out int[] acceptingStates)
        {
            List<int[]> dfaTransitions = new List<int[]>();

            int fromState = 0;
            //int valueInactiveRoute = (int)node.GripLoad.Min();
            int valueInactiveRoute = (int)cumulVar.Min();
            int maxValue = (int)cumulVar.Max();

            Debug.Assert(toAdd <= maxValue, "Trying to add " + toAdd + "to variable of max value = " + maxValue + ". Var = " + cumulVar.Name());
            // Common for -1,+0,+1

            // Create path for inactive routes
            dfaTransitions.Add(new int[] { fromState, 0, ++fromState });
            dfaTransitions.Add(new int[] { fromState, valueInactiveRoute, ++fromState });
            dfaTransitions.Add(new int[] { fromState, valueInactiveRoute, ++fromState });

            int acceptingState = fromState;

            fromState++;
            int baseState4ActiveRoutes = fromState;

            // Path for active routes (this function cannot differentiate between active routes)
            for (int i = 1; i < cellData.NbRoutes; i++) // TODO: make general
                dfaTransitions.Add(new int[] { 0, i, baseState4ActiveRoutes });


            for (int i = 0; i <= maxValue - valueInactiveRoute; i++)
            {
                int accumulatedCurrentNode = valueInactiveRoute + i;
                int accumulatedPrevNode = valueInactiveRoute + i - toAdd;

                if (accumulatedCurrentNode >= valueInactiveRoute && accumulatedCurrentNode <= maxValue && accumulatedPrevNode >= prevMin && accumulatedPrevNode <= prevMax)
                {
                    dfaTransitions.Add(new int[] { baseState4ActiveRoutes, accumulatedCurrentNode, ++fromState });
                    dfaTransitions.Add(new int[] { fromState, accumulatedPrevNode, acceptingState });
                }
            }


            //Console.WriteLine("ToAdd: {0}, range {1} - {2}", toAdd, valueInactiveRoute, maxValue);
            for (int i = 0; i < dfaTransitions.ToArray().Length; i++)
            {
                //Console.WriteLine("{0} {1} {2}", (dfaTransitions.ToArray()[i])[0], (dfaTransitions.ToArray()[i])[1], (dfaTransitions.ToArray()[i])[2]);
            }
            //Console.Write(Environment.NewLine);


            IntTupleSet dfaTupleSet = new IntTupleSet(dfaTransitions.ToArray()[0].Length);

            for (int i = 0; i < dfaTransitions.ToArray().Length; i++)
            {
                int[] tuple = dfaTransitions.ToArray()[i];
                dfaTupleSet.Insert(dfaTransitions.ToArray()[i]);
            }
            acceptingStates = new int[] { acceptingState };

            return dfaTupleSet;
        }

        private void generateNodeStatus(ConstraintModelNode node, out bool IsStart, out bool IsEnd, out bool IsGripTray, out bool IsSuctionTray, out  bool IsCamera, out bool IsGripDOFixture, out bool IsSuctionDOFixture, out bool IsTappOrPeel, out bool IsIsFinalAssembly, out bool IsIsOutput, out bool IsAir)
        {
            Node nd = node.NodeDescription;
            IsStart = nd.IsStart;
            IsEnd = nd.IsEnd;
            IsGripTray = nd.Tray > 0 && cellData.Components[node.NodeDescription.Component].grip;
            IsSuctionTray = nd.Tray > 0 && cellData.Components[node.NodeDescription.Component].suction;
            IsCamera = nd.Camera > 0;
            IsGripDOFixture = nd.Fixture > 0 && nd.FixtOrd == 0 && cellData.Components[node.NodeDescription.Component].grip;
            IsSuctionDOFixture = nd.Fixture > 0 && nd.FixtOrd == 0 && cellData.Components[node.NodeDescription.Component].suction;
            IsTappOrPeel = (nd.Fixture > 0 && nd.FixtOrd > 0);
            IsIsFinalAssembly = nd.IsFinalAssembly;
            IsIsOutput = nd.IsOutput;
            IsAir = nd.IsAirFlow;


            bool[] oneShouldBeTrue = new bool[] { IsAir, IsIsOutput, IsIsFinalAssembly, IsEnd, IsStart, IsGripTray, IsSuctionTray, IsCamera, IsGripDOFixture, IsSuctionDOFixture, IsTappOrPeel };

            PropertyInfo[] props = typeof(Node).GetProperties();
            List<bool> nodeProperties = new List<bool>();
            foreach (PropertyInfo prop in props)
            {
                if (prop.Name.Contains("Is"))
                {
                    //Console.WriteLine(prop.Name);
                    nodeProperties.Add((bool)prop.GetValue(nd));
                }
            }
            bool[] fromNodeGetters = nodeProperties.ToArray();

            int sumShouldBe1 = 0;
            for (int i = 0; i < oneShouldBeTrue.Length; i++)
            {
                sumShouldBe1 = sumShouldBe1 + (oneShouldBeTrue[i] ? 1 : 0);
            }

            if (true)
            {
                //Console.WriteLine("Node {0}: ", nd.Id);
                for (int i = 0; i < fromNodeGetters.Length; i++)
                {
                    //Console.WriteLine("{0} = {1}", fromNodeGetters[i], oneShouldBeTrue[i]);
                    Debug.Assert(fromNodeGetters[i] == oneShouldBeTrue[i], "Property " + i + " for node " + nd.Id + " differs");
                }
            }

            if (sumShouldBe1 != 1)
                Debug.Assert(false, "Sum of true is " + sumShouldBe1.ToString() + " for node.Id " + node.Id.ToString());
        }
        
        private bool PrecedenceConstraint(ConstraintModelNode prev, ConstraintModelNode next)
        {
            /**
             * 
             * Current method posts the chosen constraint for precedence 
             * 
             */
            bool DFA = false;
            bool conjunctive = false;
            bool dualIntervals = true;
            // Here we need to check for flags which constraint to add
            if (DFA)
            {
                #region Precedence DFA
                //
                // Current DFA covers all combinations of variables
                // that can be entered into the DFA
                //
                int[,] pdfa = new int[,]
                                        {
                                            {0,0,1},
                                            {0,1,2},
                                            {0,2,2},
                                            {1,0,3},
                                            {1,1,3},
                                            {1,2,3},
                                            {2,0,3},
                                            {2,1,4},
                                            {2,2,4},
                                            {3,0,5},
                                            {3,1,5},
                                            {4,1,5}
                                        };

                IntTupleSet precedenceDFA = new IntTupleSet(pdfa.GetLength(1));
                precedenceDFA.InsertAll(pdfa);
                #endregion
                IntVar boolean = solver.MakeIsLessOrEqualVar(prev.DepartureTime, next.StartTime).Var();
                Constraint dfa = solver.MakeTransitionConstraint(
                    new IntVar[] { prev.Route, next.Route, boolean }, precedenceDFA, 0, new int[] { 5 });
                //DFA = solver.CheckConstraint(dfa);
                solver.Add(dfa);
            }
            if (conjunctive)
            {
                Constraint ConjunctiveConstraint =
                    solver.MakeIntervalVarRelation(next.NodeInterval, Solver.STARTS_AFTER_END, prev.NodeInterval);

                //conjunctive = solver.CheckConstraint(ConjunctiveConstraint);
                solver.Add(ConjunctiveConstraint);
            }
            if (dualIntervals)
            {
                Constraint ConjunctiveConstraint1 = solver.MakeIntervalVarRelation(next.IntervalRoute1, Solver.STARTS_AFTER_END, prev.IntervalRoute1);
                Constraint ConjunctiveConstraint2 = solver.MakeIntervalVarRelation(next.IntervalRoute2, Solver.STARTS_AFTER_END, prev.IntervalRoute1);
                Constraint ConjunctiveConstraint3 = solver.MakeIntervalVarRelation(next.IntervalRoute1, Solver.STARTS_AFTER_END, prev.IntervalRoute2);
                Constraint ConjunctiveConstraint4 = solver.MakeIntervalVarRelation(next.IntervalRoute2, Solver.STARTS_AFTER_END, prev.IntervalRoute2);

                foreach (Constraint ct in new Constraint[] { ConjunctiveConstraint1, ConjunctiveConstraint2, ConjunctiveConstraint3, ConjunctiveConstraint4 })
                {
                    //dualIntervals = solver.CheckConstraint(ct);
                    solver.Add(ct);
                }
            }

            bool allConstraints = DFA || conjunctive || dualIntervals;
            return allConstraints;
        }

        private int[,] makeTransitionMatrix(ConstraintModelNode[] nodes)
        {
            int nbOfNodes = nodes.Length;
            int nbRoutes = 3;
            int nbFixtures = 2;
            int nbStates = nbOfNodes * 2;
            int nbTransitions = nbOfNodes * 3; // {from, transitionValue, to}

            int[,] returnMatrix = new int[nbTransitions, 3];


            // I will explain this later on!
            for (int i = 0; i < nbOfNodes; i++)
            {
                int subAssembly = cellData.Components[nodes[i].NodeDescription.Component].subAssembly;
                int fixture = nodes[i].NodeDescription.Fixture;
                //Console.WriteLine("Node {0}; Fixture {1}; SubAssembly {2}", nodes[i].Id, fixture, subAssembly);
                for (int j = 0; j < nbRoutes; j++)
                {
                    int matrixIndex = j + i * nbRoutes;
                    int dimension2 = j;
                    int transitionValue = (j + 1 - (nbFixtures - fixture)) % nbRoutes;
                    int fromState = (i * 2) + (transitionValue > 0 ? (subAssembly == fixture ? 1 : 0) : (subAssembly == fixture ? 0 : 1));
                    int toState = fromState + nbFixtures;

                    int[] a = new int[] { (fromState == 0 ? 1 : fromState), transitionValue, (i == nbOfNodes - 1 ? nbStates : toState) };

                    for (int k = 0; k < nbRoutes; k++)
                    {
                        returnMatrix[matrixIndex, k] = a[k];
                    }
                    // Uncomment these printouts and compare with the hardcoded transitionmatrix above.
                    //Console.WriteLine("Mat[{0},_] = {2},{3},{4}", matrixIndex_i, dimension2, (fromState == 0 ? 1 : fromState), transitionValue, (i == nbOfNodes-1 ? nbStates : toState));
                }
            }
            return returnMatrix;
        }

        public void WriteSolutionToFile(int candidateNo)
        {
            DateTime timeStamp = System.DateTime.Now;
            String date = timeStamp.ToShortDateString();
            String time = timeStamp.ToShortTimeString();
            String folder = "C:\\Users\\XJOEJE\\Desktop\\solutions\\";
            //String folder = "C:\\Users\\SEJOWES1\\Desktop\\FridaSolutions\\";
            String file = date + "-Solution" + candidateNo + "-" + +timeStamp.Hour + "" + timeStamp.Minute + "" + timeStamp.Second + "" + timeStamp.Millisecond + ".txt";

            using (System.IO.StreamWriter fileStream = new System.IO.StreamWriter(folder + file))
            {

                fileStream.WriteLine("Solution " + candidateNo + " at time " + time + ":" + timeStamp.Second + ":" + timeStamp.Millisecond);
                fileStream.Write(Environment.NewLine);
                long LatestIndexP = 0;
                List<int> NodesInSolutionArrayP = new List<int>();
                int NodesInSolutionP = 0;
                long[] startNodesP = new long[] { 1, 2 };
                for (int routeInd = 0; routeInd < startNodesP.Length; routeInd++)
                {
                    long route = Route[startNodesP[routeInd]].Value();
                    fileStream.WriteLine("RouteId: {0}", route);
                    for (long index = startNodesP[routeInd]; index != cellData.NbNodes; index = Next[index].Value()) //change from 0 being dummy node to N+M*2 being dummy node
                    {
                        // Current printout is nicer than the old dimension2, should be used in console print as well.
                        fileStream.WriteLine(
                            "{0}->{1,-5}\t | TT:{2,-5} | TW:{3}-{4,-15}\t | Dur:{5,-10} | Dep:{6,-5} | G:{7,-4} | S:{8,-4} |"
                            , new Object[] { 
                            Prev[index].Value(), 
                            ModelNodes[index].Id, 
                            TripTime[index].Value(),
                            AccumulatedTime[index].Value(),
                            ModelNodes[index].StartTime.Value(),
                            ModelNodes[index].durationPerRoute[route],
                            DepartureTime[index].Value(), //Value(),
                            AccumulatedGrip[index].Min(), 
                            AccumulatedSuction[index].Min() 
                        });
                        LatestIndexP = index;
                        NodesInSolutionArrayP.Add((int)LatestIndexP);
                        NodesInSolutionP++;
                    }
                    fileStream.Write(Environment.NewLine);
                    NodesInSolutionArrayP.Add((int)Next[LatestIndexP].Value());

                    fileStream.WriteLine("Route ArrivalTime: {0}", AccumulatedTime[LatestIndexP].Min());
                    fileStream.Write(Environment.NewLine);

                    NodesInSolutionP++;
                }
                fileStream.Write(Environment.NewLine);
                fileStream.WriteLine("Total Cycle Time: {0}", TotalCycleTime.Min());
                fileStream.Write(Environment.NewLine);
                fileStream.WriteLine("Solution contains {0} nodes", NodesInSolutionP);
                fileStream.Write(Environment.NewLine);
                fileStream.WriteLine("End Candidate {0}", candidateNo);
                fileStream.Write(Environment.NewLine);

            }
        }

        public class valueSelectionStrategy : LongResultCallback2
        {
            ConstraintProblem csp;
            int count;

            public valueSelectionStrategy(ConstraintProblem _csp)
            {
                csp = _csp;
                count = 0;
            }

            public override long Run(long arg0, long arg1)
            {
                ConstraintModelNode Current = csp.ModelNodes[arg0];
                ConstraintModelNode PossibleNext = csp.ModelNodes[arg1];
                Component firstComponent = cellData.Components[Current.NodeDescription.Component];
                Component secondComponent = cellData.Components[PossibleNext.NodeDescription.Component];
                return Math.Abs(secondComponent.ComponentId - firstComponent.ComponentId) * 100;
            }
        }
    }
}
