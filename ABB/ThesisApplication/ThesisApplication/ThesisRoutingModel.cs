using Google.OrTools.ConstraintSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThesisPrototype
{
    class ThesisRoutingModel
    {
        /**
         * Instance variables for the Routing model solver.
         */
        private int[][] travellingDistance;

        private RoutingModel routing;
        private static int nbNodes;
        static int nbRoutes;
        int[] starts;
        int[] ends;




        private void init()
        {
            DataReader dr = new SyntheticData();
            CellDescription cellData = dr.FetchData();

            travellingDistance = cellData.TravellingTimeArm1; //LUDDE: Why is this set to Arm1's Travelling arrivalTime?
            
            // number of vehicles we have
            nbRoutes = 3;

            // number of visists we have to make
            nbNodes = 58 + nbRoutes * 2;

            List<int> initStarts = new List<int>();
            List<int> initEnds = new List<int>();

            // ENDS AND STARTS NEED TO BE IN THE BEGINNING!
            for (int i = 0; i < nbRoutes; i++)
            {
                initStarts.Add(i);
                initEnds.Add(i + nbRoutes);
            }

            starts = initStarts.ToArray();
            ends = initEnds.ToArray();
            routing = new RoutingModel(nbNodes,nbRoutes,starts,ends);
        }

        private void model()
        {            
            routing.SetVehicleCost(0, new dummyDistance());
            routing.SetVehicleCost(1, new leftDistance());
            routing.SetVehicleCost(2, new rightDistance());
            routing.AddAllActive();
            
            //Add the different dimensions (Grip, Suction and ArrivalTime)
            //routing.AddDimension(new suctionCapacity(),0,2,true,"Suction Capacity");
            //routing.AddDimension(new gripCapacity(),0,1,true,"Grip Capacity");
            //Adding a arrivalTime dimension might not be necessary since we have arrivalTime=distance
            //routing.AddDimension(,0,Int64.MaxValue,true,"ArrivalTime accumulation");

            /**
             * Our additional constraints
             * 
             */

            Solver solver =routing.solver();

            routing.CloseModel();
        }

        class dummySuctionCapacity : NodeEvaluator2
        {
            public override long Run(int i, int j)
            {
                return base.Run(i, j);
            }
        }

        private void search()
        {
            
            
            
            //Solver solver = routing.solver();
            routing.SetMetaheuristic(RoutingModel.ROUTING_GUIDED_LOCAL_SEARCH);
            Assignment solution = routing.Solve();
            //SolutionCollector collector = solver.MakeLastSolutionCollector();
            //IntVar[] nexts = new IntVar[nbNodes + nbRoutes * 2];
            //for (int J = 0; J < nbNodes + nbRoutes * 2; J++)
           // {
            //    nexts[J] = routing.NextVar(J);
            //}
            //DecisionBuilder db = solver.MakePhase(nexts, Solver.CHOOSE_FIRST_UNBOUND, Solver.ASSIGN_RANDOM_VALUE);
            //solver.NewSearch(db);
            //if (!solver.NextSolution())
            //{
            //    Console.WriteLine("No solution found!");
            //}
            while ((routing.solver()).NextSolution())
                {
                    Console.WriteLine("Cost:\t" + solution.ObjectiveValue());
                    for (int r = 0; r < nbRoutes; r++)
                    {
                        Console.Write("Route {0}:", r);
                        for (long i = routing.Start(r); !routing.IsEnd(i); i = solution.Value(routing.NextVar(i))) 
                        {
                            Console.Write(routing.IndexToNode(i) + "\t");
                        }
                        Console.WriteLine("");
                    }
                }
                       
        }

        public void Solve()
        {
            init();
            model();
            search();            
        }

        #region Distance functions

        /// <summary>
        /// Distance methods for the left arm between nodes.
        /// Currently this metric is 5 for the
        /// </summary>
        class leftDistance : NodeEvaluator2
        {
            public override long Run(int firstIndex, int secondIndex)
            {
                if (firstIndex == secondIndex)
                {
                    return 0;
                }
                else
                {
                    if (firstIndex < nbNodes / 2)
                    {
                        return 5;
                    }
                    else
                    {
                        return 10;
                    }
                }
            }
        }

        /// <summary>
        /// Distance methods for the right arm between nodes.
        /// Currently this metric is 5 for the 
        /// </summary>
        public class rightDistance : NodeEvaluator2
        {
            public override long Run(int firstIndex, int secondIndex)
            {
                if (firstIndex == secondIndex)
                {
                    return 0;
                }
                else
                {
                    if (firstIndex < nbNodes / 2)
                    {
                        return 10;
                    }
                    else
                    {
                        return 5;
                    }
                }
            }
        }

        /// <summary>
        /// Dummy distance methods for the third trashroute
        /// </summary>
        class dummyDistance : NodeEvaluator2
        {
            public override long Run(int firstIndex, int secondIndex)
            {
                if (firstIndex == secondIndex)
                {
                    return 0;
                }
                else
                {
                    return 20;
                }
            }
        }
        #endregion

        #region Capacity functions

        class gripCapacity : NodeEvaluator2
        {
            public override long Run(int i, int j)
            {
                if (i % 2 == 0)
                {
                    return 1;
                }
                return-1;
            }
        }

        class suctionCapacity : NodeEvaluator2
        {
            public override long Run(int i, int j)
            {
                if (i % 2 == 0)
                {
                    return 1;
                }
                return -1;
            }
        }
        #endregion

    }
}
