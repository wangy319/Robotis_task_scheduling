using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

//TODO: add transition times and objective based on transition
//TODO: different duration for different tool
//TODO: blast starts can be different
//TODO: try disjunctive on drift

namespace InputTest
{
    /// <summary>
    /// Current should be the generic representation of a component
    /// in the assembly. Needs to contain information on what kind
    /// of tools can pick it, and how large it is.
    /// </summary>
    public class Component
    {
        // Information about the current component
        public int ComponentId { get; set; }
        public string Description { get; set; }

        // Boolean values determining what kind of
        // tool used to pick it up.
        public bool suction { get; set; }
        public bool grip { get; set; }

        // Number of operations on fixtures that
        // are associated with this component
        public int NbFixtureOperations { get; set; }

        public int subAssembly { get; set; }

        public Component(int _Id, string _Description)
        {
            ComponentId = _Id;
            Description = _Description;
        }
    }

    /// <summary>
    /// Current is just a node that the robot needs to visit,
    /// could be a component or a process step.
    /// </summary>
    public class Node
    {
        private CellDescription parent;
        //The actual ID of the current NodeDescription.
        public int Id { get; set; }

        // Current is > 0 if there is a component added to the node
        public int Component { get; set; }

        // Current is used to order the node on fixtures, 
        // 0 if node is not on fixture or is dropof
        public int FixtOrd { get; set; }

        // Integers describing which fixture/tray/camera the node is representing
        public int Fixture { get; set; }
        public int Tray { get; set; }
        public int Camera { get; set; }

        // Boolean variables to decide wether a node is Air flow, IsOutput, Peeling, Final assembly or start-/endnode
        public bool IsAirFlow { get; set; }
        public bool IsOutput { get; set; }
        public bool Peel { get; set; }
        public bool IsFinalAssembly { get; set; }
        public bool IsEnd { get; set; }
        public bool IsStart { get; set; }
        public int[] Durations { get; set; }

        public Node(int nodeId, CellDescription _parent)
        {
            parent = _parent;
            Id = nodeId;
        }

        public bool IsGripTray { get { return (Tray > 0 && parent.Components[Component].grip); } }
        public bool IsSuctionTray { get { return (Tray > 0 && parent.Components[Component].suction); } }
        public bool IsCamera { get { return (Camera > 0); } }
        public bool IsGripDOFixture { get { return (Fixture > 0 && FixtOrd == 0 && parent.Components[Component].grip); } }
        public bool IsSuctionDOFixture { get { return (Fixture > 0 && FixtOrd == 0 && parent.Components[Component].suction); } }
        public bool IsTappOrPeel { get { return (Fixture > 0 && FixtOrd > 0); } }
    }

    /// <summary>
    /// Current should describe the robot configuration that might
    /// come in handy when modeling the prototype.
    /// </summary>
    public class ArmDescription
    {
        public int numberOfSuction { get; set; }
        public int numberOfGrip { get; set; }
    }

    /// <summary>
    /// The decription of the assembly cell. Contains all general data
    /// and is used to get information on individual components.
    /// </summary>
    public class CellDescription
    {
        public int[][] TravellingTimeArm1 { get; set; }
        public int[][] TravellingTimeArm2 { get; set; }

        public Node[] Nodes { get; set; }
        public Component[] Components { get; set; }
        public ArmDescription[] Arms { get; set; }

        public int NbNodes { get { return Nodes.Length; } }
        public int NbRoutes { get { return Arms.Length; } }
        public int NbComponents { get { return Components.Length; } }

        public int NbTrays { get; set; }
        public int NbCameras { get; set; }
        public int NbFixtures { get; set; }

        public int NodeWithAirFlow { get { return (from node in Nodes where node.IsAirFlow select node.Component).ToArray()[0]; } }

        public CellDescription(int nodes, int routes)
        {
            Nodes = new Node[nodes];
            Arms = new ArmDescription[routes];
            for (int i = 0; i < nodes; i++)
            {
                Nodes[i] = new Node(i, this);
            }
            for (int i = 0; i < routes; i++)
            {
                Arms[i] = new ArmDescription();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Nodes: " + Nodes.Length);
            sb.Append(Environment.NewLine);
            sb.Append("----------------------");
            sb.Append(Environment.NewLine);
            #region Printout of each Distancematrix
            if (false)
            {
                for (int i = 0; i < Nodes.Length; i++)
                {
                    for (int j = 0; j < Nodes.Length; j++)
                    {
                        sb.Append(TravellingTimeArm1[i][j] + "\t");
                    }
                    sb.Append(Environment.NewLine);
                }
                for (int i = 0; i < Nodes.Length; i++)
                {
                    for (int j = 0; j < Nodes.Length; j++)
                    {
                        sb.Append(TravellingTimeArm2[i][j] + "\t");
                    }
                    sb.Append(Environment.NewLine);
                }
            }
            #endregion

            #region Printout of each node
            foreach (Node n in Nodes)
            {
                sb.Append(Environment.NewLine);
                sb.Append("----------------------");
                sb.Append(Environment.NewLine);
                sb.Append("Node: " + n.Id);
                sb.Append(", is start: " + n.IsStart);
                sb.Append(", is end: " + n.IsEnd);
                sb.Append(Environment.NewLine);

                // Print if node is fixture, tray, output, etc etc.

                if (n.Fixture != 0)
                {
                    sb.Append("Node is fixture: " + n.Fixture);
                }
                if (n.Camera != 0)
                {
                    sb.Append("Node is camera: " + n.Camera);
                }
                if (n.IsAirFlow)
                {
                    sb.Append("Node is air flow");
                }
                if (n.IsOutput)
                {
                    sb.Append("Node is output");
                }
                if (n.Peel)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append("Node is peeling");
                }
                if (n.IsFinalAssembly)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append("Node is final assembly");
                }
                if (n.Tray != 0)
                {
                    sb.Append("Node is tray: " + n.Tray);
                }
                if (n.Component != 0)
                {
                    sb.Append(", Component is: " + n.Component);
                }
            }
            #endregion

            #region Printout of each component
            for (int i = 0; i < Components.Length; i++)
            {
                sb.Append(Environment.NewLine);
                sb.Append("----------------------");
                sb.Append(Environment.NewLine);
                sb.Append("Component id: " + i);
                sb.Append(", pickup grip: " + Components[i].grip);
                sb.Append(", pickup suction: " + Components[i].suction);
            }
            #endregion
            sb.Append(Environment.NewLine);
            sb.Append("----------------------");
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        // TODO: Current should be enhanced
        public void SanityCheck()
        {
            // Verify that: 
            // - travelling arrivalTime matrix is squared 
            // - its dimension is equal to the nb of nodes
            // - the diagonal is 0
            Debug.Assert(TravellingTimeArm1.Length == Nodes.Length);
            Debug.Assert(TravellingTimeArm2.Length == Nodes.Length);
            for (int i = 0; i < Nodes.Length; i++)
            {
                Debug.Assert(TravellingTimeArm1[i].Length == Nodes.Length);
                Debug.Assert(TravellingTimeArm1[i][i] == 0);
                Debug.Assert(TravellingTimeArm2[i].Length == Nodes.Length);
                Debug.Assert(TravellingTimeArm2[i][i] == 0);
            }
        }
    }
}
