using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.OrTools.ConstraintSolver;

namespace ThesisPrototype
{
    /// <summary>
    /// Testing decision builder supplied by Alessandro
    /// </summary>
    public class RandomSelectToolHeuristic : NetDecisionBuilder
    {

        private ConstraintProblem constraintProblem;
        private Random rnd;

        public RandomSelectToolHeuristic(ConstraintProblem csp, int seed)
        {
            seed = DateTime.Now.Millisecond % this.GetHashCode();
            this.constraintProblem = csp;
            this.rnd = new Random(seed); // deterministic seed for result reproducibility
        }

        public override Decision Next(Solver solver)
        {
            foreach (ConstraintProblem.ConstraintModelNode node in constraintProblem.Tray)
            {
                IntVar var = node.Route;
                if (!var.Bound())
                {
                    int min = (int)var.Min();
                    if (min == 0) min++; // We erase the 0 since the GCC constraints will fix that
                    int max = (int)var.Max();
                    int rndVal = rnd.Next(min, max + 1);

                    while (!var.Contains(rndVal))
                        rndVal = rnd.Next(min, max + 1);

                    return solver.MakeAssignVariableValue(var, rndVal);
                }
            }
            return null;
        }

    }

    /// <summary>
    /// Branching heuristic for setting route to fixtures, uses
    /// random! Exact replica of alessandros...
    /// </summary>
    public class SelectFixtureRouteHeuristic : NetDecisionBuilder
    {
        private ConstraintProblem constraintProblem;
        private Random rnd;
        private int decisions;

        public SelectFixtureRouteHeuristic(ConstraintProblem csp, int seed)
        {
            //seed = DateTime.Now.Millisecond % this.GetHashCode();
            this.constraintProblem = csp;
            this.rnd = new Random(seed);
        }

        public override Decision Next(Solver solver)
        {
            foreach (ConstraintProblem.ConstraintModelNode node in constraintProblem.Fixture)
            {
                IntVar var = node.Route;
                if (!var.Bound())
                {
                    int min = (int)var.Min();
                    //if (min == 0) min++; // We erase the 0 since the GCC constraints will fix that
                    int max = (int)var.Max();
                    int rndVal = rnd.Next(min, max + 1);

                    while (!var.Contains(rndVal))
                        rndVal = rnd.Next(min, max + 1);
                    decisions++;
                    return solver.MakeAssignVariableValue(var, rndVal);
                }
            }
            return null;
        }


    }

    /// <summary>
    /// Heuristic for setting route to Trays, depends on fixtures
    /// being set to route before.
    /// </summary>
    public class SelectTrayRouteHeuristic : NetDecisionBuilder
    {
        private ConstraintProblem csp;
        private Random rnd;

        public SelectTrayRouteHeuristic(ConstraintProblem csp, int seed)
        {
            //seed = DateTime.Now.Millisecond % this.GetHashCode();
            this.csp = csp;
            this.rnd = new Random(seed);
        }

        public override Decision Next(Solver solver)
        {
            /*
            int max = csp.Tray.Length;
            int min = 0;
            int rndVal = rnd.Next(min, max);

            while (csp.Tray[rndVal].Route.Bound())
                rndVal = rnd.Next(min, max);

            // this part really requires the route to be set in
            // fixtures and propagated throughout trays and cameras
            IntVar trayRoute = csp.Tray[rndVal].Route;
            return solver.MakeAssignVariableValue(trayRoute, trayRoute.Max());
            */

            foreach (ConstraintProblem.ConstraintModelNode item in csp.Tray)
            {
                if (!item.Route.Bound())
                {
                    return solver.MakeAssignVariableValue(item.Route, item.Route.Max());
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Heuristic for setting route for cameras, depends on trays
    /// being set before.
    /// </summary>
    public class SelectCameraRouteHeuristic : NetDecisionBuilder
    {
        private ConstraintProblem csp;
        private Random rnd;

        public SelectCameraRouteHeuristic(ConstraintProblem csp, int seed)
        {
            //seed = DateTime.Now.Millisecond % this.GetHashCode();
            this.csp = csp;
            this.rnd = new Random(seed);
        }

        public override Decision Next(Solver solver)
        {
            // Hur ful kod kan man skriva? Detta är okej i python....
            foreach (ConstraintProblem.ConstraintModelNode node in csp.Camera)
                if (!node.Route.Bound())
                    return solver.MakeAssignVariableValue(node.Route, node.Route.Max());
            return null;
        }
    }

    /// <summary>
    /// Heuristic to set the first node from start.
    /// </summary>
    public class SetFirstNextHeuristic : NetDecisionBuilder
    {
        private ConstraintProblem csp;
        private Random rnd;
        public SetFirstNextHeuristic(ConstraintProblem csp, int seed)
        {
            //seed = DateTime.Now.Millisecond % this.GetHashCode();
            this.csp = csp;
            this.rnd = new Random(seed);
        }

        public override Decision Next(Solver solver)
        {
            foreach (ConstraintProblem.ConstraintModelNode node in csp.ModelNodes)
            {
                if (node.NodeDescription.IsStart)
                {
                    if (!node.Next.Bound())
                    {
                        int max = csp.Tray.Length;
                        ConstraintProblem.ConstraintModelNode possibleNext = csp.Tray[rnd.Next(0, max)];

                        while (possibleNext.Route.Value() != node.Route.Value())
                            possibleNext = csp.Tray[rnd.Next(0, max)];

                        return solver.MakeAssignVariableValue(node.Next, possibleNext.Id);
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Heuristic to set the next node given that we can find the
    /// lowest distance between fromState nodes.
    /// </summary>
    public class SetNextFromLowestTimeHeuristic : NetDecisionBuilder
    {
        private ConstraintProblem csp;
        private ConstraintProblem.ConstraintModelNode lastDecision;
        private ConstraintProblem.ConstraintModelNode lastDecisionRoute1;
        private ConstraintProblem.ConstraintModelNode lastDecisionRoute2;
        private ConstraintProblem.ConstraintModelNode nextNode;

        public SetNextFromLowestTimeHeuristic(ConstraintProblem csp)
        {
            this.csp = csp;
        }

        public override Decision Next(Solver solver)
        {
            #region Set the first node to start.Next if lastDecisionNode is unbound
            if (lastDecision == null)
            {
                ConstraintProblem.ConstraintModelNode[] startNodes =
                    (from node in csp.ModelNodes
                     where node.Route.Value() != 0 && node.NodeDescription.IsStart
                     select node).ToArray();

                // Some nasty hardcoded shit!
                lastDecisionRoute1 = (from node in startNodes where node.Route.Value() == 1 select node).ToArray().First();
                lastDecisionRoute2 = (from node in startNodes where node.Route.Value() == 2 select node).ToArray().First();
                lastDecision = lastDecisionRoute1;

                while (lastDecision.Next.Bound())
                {
                    if (lastDecision.Route.Value() == 1)
                        if (!lastDecisionRoute2.Next.Bound())
                        {
                            lastDecision = lastDecisionRoute2;
                        }
                        else
                        {
                            Console.WriteLine("Checkpoint 1");
                            lastDecision = csp.ModelNodes[lastDecisionRoute2.Next.Value()];
                        }
                    else
                        if (!lastDecisionRoute2.Next.Bound())
                        {
                            lastDecision = lastDecisionRoute2;
                        }
                        else
                        {
                            Console.WriteLine("Checkpoint 2");
                            lastDecision = csp.ModelNodes[lastDecisionRoute2.Next.Value()];
                        }
                }
            }
            #endregion

            #region If both latestDecisions are end, return null
            if (lastDecisionRoute1.NodeDescription.IsEnd &&
                lastDecisionRoute2.NodeDescription.IsEnd)
                return null;
            #endregion

            if (lastDecision.Next.Bound())
            {
                #region Switch between routes
                if (!lastDecision.Equals(lastDecisionRoute1))
                    lastDecision = lastDecisionRoute1;
                else
                    lastDecision = lastDecisionRoute2;
                #endregion

                #region If selected route is at an end set route to other route
                if (lastDecision.NodeDescription.IsEnd)
                    if (lastDecision.Route.Value() != 1)
                        lastDecision = lastDecisionRoute1;
                    else
                        lastDecision = lastDecisionRoute2;
                #endregion

                if (lastDecision.Next.Bound())
                    nextNode = csp.ModelNodes[lastDecision.Next.Value()];
                else
                    nextNode = lastDecision;
            }
            else
                nextNode = lastDecision;

            #region Look for the shortest distance to any available node from nextNode
            ConstraintProblem.ConstraintModelNode closest = null;
            ConstraintProblem.ConstraintModelNode[] NextsFromNextNode =
                (from node in csp.ModelNodes
                 where nextNode.Next.Contains(node.Id) && !node.NodeDescription.IsEnd
                 orderby node.Id
                 select node).ToArray();
            if (nextNode.NodeDescription.IsEnd)
                closest = nextNode;
            foreach (ConstraintProblem.ConstraintModelNode node in NextsFromNextNode)
            {
                if (node.Route.Bound())
                {
                    if (node.Route.Value() == nextNode.Route.Value())
                    {
                        long closestSoFar = 0;
                        if (closest != null)
                            closestSoFar = closest.tdc.Run(nextNode.Id, nextNode.Route.Value());
                        long distance = node.tdc.Run(nextNode.Id, nextNode.Route.Value());
                        if (closest == null || closestSoFar > distance)
                        {
                            if (!node.NodeDescription.IsEnd || nextNode.Next.Bound())
                                closest = node;
                        }
                    }
                }
            }
            #endregion

            #region If the closest node is an end node, set that node as latest decision
            if (closest.NodeDescription.IsEnd)
                lastDecision = closest;
            else
                lastDecision = nextNode;
            #endregion

            #region Save the current decision node for future decisions
            if (lastDecision.Route.Value() == 1)
                lastDecisionRoute1 = lastDecision;
            else
                lastDecisionRoute2 = lastDecision;
            #endregion

            return solver.MakeAssignVariableValue(nextNode.Next, closest.Id);
        }
    }

    public class SetNextFromLowestTimeHeuristicNew : NetDecisionBuilder
    {
        private ConstraintProblem csp;
        private ConstraintProblem.ConstraintModelNode lastDecisionRoute1;
        private ConstraintProblem.ConstraintModelNode lastDecisionRoute2;
        private Decision lastDecision;
        private int constraints;

        public SetNextFromLowestTimeHeuristicNew(ConstraintProblem csp)
        {
            this.csp = csp;
        }

        public override Decision Next(Solver solver)
        {
            if (lastDecisionRoute1 == null || lastDecisionRoute2 == null)
            {
                ConstraintProblem.ConstraintModelNode[] startNodes =
                    (from node in csp.ModelNodes
                     where node.Route.Value() != 0 && node.NodeDescription.IsStart
                     select node).ToArray();

                lastDecisionRoute1 = (from node in startNodes where node.Route.Value() == 1 select node).ToArray().First();
                lastDecisionRoute2 = (from node in startNodes where node.Route.Value() == 2 select node).ToArray().First();
            }

            if (lastDecisionRoute1.NodeDescription.IsEnd &&
                lastDecisionRoute2.NodeDescription.IsEnd)
                return null;

            ConstraintProblem.ConstraintModelNode nextRoute1 = null;
            ConstraintProblem.ConstraintModelNode nextRoute2 = null;
            if (lastDecisionRoute1.Next.Bound())
                nextRoute1 = csp.ModelNodes[lastDecisionRoute1.Next.Value()];
            else
                nextRoute1 = lastDecisionRoute1;
            if (lastDecisionRoute2.Next.Bound())
                nextRoute2 = csp.ModelNodes[lastDecisionRoute2.Next.Value()];
            else
                nextRoute2 = lastDecisionRoute2;

            ConstraintProblem.ConstraintModelNode closestRoute1 = null;
            ConstraintProblem.ConstraintModelNode closestRoute2 = null;

            ConstraintProblem.ConstraintModelNode[] NextsFromNextNodes =
                (from node in csp.ModelNodes
                 where nextRoute1.Next.Contains(node.Id) || nextRoute2.Next.Contains(node.Id)
                 orderby node.Id
                 select node).ToArray();

            ConstraintProblem.ConstraintModelNode[] NextsFromNextNodeRoute1 =
                (from node in NextsFromNextNodes
                 where node.Route.Value() == nextRoute1.Route.Value() && nextRoute1.Next.Contains(node.Id)
                 select node).ToArray();

            ConstraintProblem.ConstraintModelNode[] NextsFromNextNodeRoute2 =
                (from node in NextsFromNextNodes
                 where node.Route.Value() == nextRoute2.Route.Value() && nextRoute2.Next.Contains(node.Id)
                 select node).ToArray();


            closestRoute1 = findClosestNode(nextRoute1, NextsFromNextNodeRoute1);
            closestRoute2 = findClosestNode(nextRoute2, NextsFromNextNodeRoute2);

            if (closestRoute1.NodeDescription.IsEnd)
                lastDecisionRoute1 = closestRoute1;
            else
                lastDecisionRoute1 = nextRoute1;

            if (closestRoute2.NodeDescription.IsEnd)
                lastDecisionRoute2 = closestRoute2;
            else
                lastDecisionRoute2 = nextRoute2;

            lastDecision = solver.MakeAssignVariablesValues(new IntVar[] { nextRoute1.Next, nextRoute2.Next }, new long[] { closestRoute1.Id, closestRoute2.Id });
            return lastDecision;
        }

        private ConstraintProblem.ConstraintModelNode findClosestNode(ConstraintProblem.ConstraintModelNode node, ConstraintProblem.ConstraintModelNode[] nexts)
        {
            ConstraintProblem.ConstraintModelNode closest = null;
            foreach (ConstraintProblem.ConstraintModelNode nextNode in nexts)
            {

                long closestSoFar = 0;
                if (closest != null)
                    closestSoFar = closest.tdc.Run(node.Id, node.Route.Value());
                long distance = nextNode.tdc.Run(node.Id, node.Route.Value());
                if (closest == null || closestSoFar > distance)
                {
                    if (!nextNode.NodeDescription.IsEnd || node.Next.Bound())
                        closest = nextNode;
                }
            }
            return closest;
        }
    }

    public class SetNextFromLowestTimePerRouteHeuristic : NetDecisionBuilder
    {
        private ConstraintProblem csp;
        private int routeId;

        private ConstraintProblem.ConstraintModelNode lastDecisionNode;
        private Decision lastDecision;

        public SetNextFromLowestTimePerRouteHeuristic(ConstraintProblem csp, int routeId)
        {
            this.csp = csp;
            this.routeId = routeId;
        }

        public override Decision Next(Solver solver)
        {
            if (lastDecisionNode == null)
            {
                ConstraintProblem.ConstraintModelNode[] startNodes =
                    (from node in csp.ModelNodes
                     where node.Route.Value() == routeId && node.NodeDescription.IsStart
                     select node).ToArray();

                lastDecisionNode = startNodes[0];
            }

            if (lastDecisionNode.NodeDescription.IsEnd)
                return null;

            ConstraintProblem.ConstraintModelNode next = null;
            if (lastDecisionNode.Next.Bound())
            {
                next = csp.ModelNodes[lastDecisionNode.Next.Value()];
                if (next.Next.Bound())
                    return null;
            }
            else
                next = lastDecisionNode;

            ConstraintProblem.ConstraintModelNode closest = null;

            ConstraintProblem.ConstraintModelNode[] NextsFromNext =
                (from node in csp.ModelNodes
                 where next.Next.Contains(node.Id) && node.Route.Value() == routeId
                 orderby node.Id
                 select node).ToArray();

            closest = findClosestNode(next, NextsFromNext);

            lastDecisionNode = next;

            if (closest == null)
                closest = next;

            lastDecision = solver.MakeAssignVariableValue(next.Next, closest.Id);
            return lastDecision;
        }

        private ConstraintProblem.ConstraintModelNode findClosestNode(ConstraintProblem.ConstraintModelNode node, ConstraintProblem.ConstraintModelNode[] nexts)
        {
            ConstraintProblem.ConstraintModelNode closest = null;
            foreach (ConstraintProblem.ConstraintModelNode nextNode in nexts)
            {

                long closestSoFar = 0;
                if (closest != null)
                    closestSoFar = closest.tdc.Run(node.Id, node.Route.Value());
                long distance = nextNode.tdc.Run(node.Id, node.Route.Value());
                if (closest == null || closestSoFar > distance)
                {
                    if (!nextNode.NodeDescription.IsEnd || node.Next.Bound())
                        closest = nextNode;
                }
            }
            return closest;
        }

        private bool hasCoveredAllNodes()
        {
            ConstraintProblem.ConstraintModelNode[] nodes =
                (from node in csp.ModelNodes
                 where node.Route.Value() == routeId
                 select node).ToArray();

            for (int i = 0; i < nodes.Length; i++)
            {
                if (!nodes[i].Next.Bound())
                    return false;
            }
            return true;
        }
    }
}
