using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ThesisPrototype
{

    public class SolverInputParameters
    {
        // Constraint flags
        public bool CoreConstraints { get; set; }
        public bool PrecedenceConstraintsComponent { get; set; }
        public bool PrecedenceConstraintsFixture { get; set; }
        public bool DisjunctiveConstraints { get; set; }
        public bool TrayGCCConstraints { get; set; }
        public bool FixtureTransitionConstraints { get; set; }
        public bool CameraGCCConstraints { get; set; }
        public bool ComponentRouteConsistencyConstraints { get; set; }
        public bool AirFlowRouteConsistencyConstraints { get; set; }
        public bool CumulativeConstraints { get; set; }

        // Pre processing 
        public bool PreprocessingUpperBound { get; set; }
        public bool PreprocessingVariables { get; set; }

        // Constraint variation
        public bool DensePrecedence { get; set; }
        public bool AllDiffStrong { get; set; }


        // Search
        public bool LocalSearch { get; set; }
        public bool TreeSearch { get; set; }

        public bool ObjectiveMakeSpan { get; set; }
        public bool ObjectiveMinimizeTravel { get; set; }

        public bool Optimize { get; set; }

        public int TimeLimit { get; set; }
        public int BranchLimit { get; set; }
        public int SolutionLimit { get; set; }
        public int FailLimit { get; set; }

        public int LogFrequency { get; set; }
        public bool Random { get; set; }

        // PrintSolution
        public bool PrintToFile { get; set; }

        // Hard Coded Routes
        public bool HardCoded { get { return (Reference || Naive); } }
        public bool Reference { get; set; }
        public bool Naive { get; set; }


        //Local Search Parameters
        public int[] OperatorSequence { get; set; }
        public bool RandomLS { get; set; }
        public bool SimulatedAnnealing { get; set; }
        public bool TabuSearch { get; set; }
        public int LsTimeLimit { get; set; }
        public int LsSolutionLimit { get; set; }

        public bool NextLNS { get; set; }
        public bool RouteLNS { get; set; }
        public bool ActiveLNS { get; set; }
        public bool ArrivalLNS { get; set; }

        public bool InitialAssignment { get; set; }


        // Tree Search Parameters

        public bool Restart { get { return (LubyRestart || ConstantRestart); } }
        public bool LubyRestart { get; set; }
        public bool ConstantRestart { get; set; }
        public int ConstantFrequency { get; set; }
        public int LubyScale { get; set; }

        public bool SeparateFixtures { get; set; }




        public SolverInputParameters()
        {
            TimeLimit = int.MaxValue;
            BranchLimit = int.MaxValue;
            SolutionLimit = int.MaxValue;
            FailLimit = int.MaxValue;
            LsSolutionLimit = SolutionLimit;
            LsTimeLimit = TimeLimit;
            LogFrequency = 1000;
            InitParameters();
        }

        public void CleanInputParams()
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                // this loop will set all input parameters to false
                Type t = property.PropertyType;
                if (t == typeof(int))
                {
                    property.SetValue(this, int.MaxValue);
                }
                else if (t == typeof(bool))
                {
                    if (property.CanWrite)
                        property.SetValue(this, false);
                }
                else
                {
                    //Unhandled!
                    Console.WriteLine("This property cannot be reset: " + property.Name);
                }
            }
        }

        public void InitParameters()
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (property.Name.Contains("Constraints"))
                    property.SetValue(this, true);
            }
        }

        public void SanityCheck()
        {
            // To be implemented
        }

        public void Inspect()
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                Object value;
                Type t = property.PropertyType;
                if (t == typeof(int[]))
                {
                    StringBuilder sb = new StringBuilder();
                    int[] array = (int[])property.GetValue(this);
                    if (array != null)
                    {
                        foreach (int i in array)
                            sb.Append(i + " ");
                    }
                    value = sb.ToString();
                }
                else
                {
                    value = property.GetValue(this);
                }

                Console.WriteLine("{0} ={1}", property.Name, value);
            }
        }

        public void ParseInput(String input)
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();
            String[] inputArgs = input.Split();

            try
            {
                switch (inputArgs[0])
                {
                    case "constraints":
                        // Sets all constraints to true
                        this.CleanInputParams();
                        this.InitParameters();
                        break;
                    case "treesearch":
                        LocalSearch = false;
                        TreeSearch = true;
                        if (inputArgs.Length > 1)
                        {
                            switch (inputArgs[1])
                            {
                                case "restart":
                                    int numericInput = int.Parse(inputArgs[3]);
                                    PropertyInfo restart = null;
                                    PropertyInfo numericValue = null;
                                    switch (inputArgs[2])
                                    {
                                        case "luby":
                                            restart = typeof(SolverInputParameters).GetProperty("LubyRestart");
                                            numericValue = typeof(SolverInputParameters).GetProperty("LubyScale");
                                            break;
                                        case "constant":
                                            restart = typeof(SolverInputParameters).GetProperty("ConstantRestart");
                                            numericValue = typeof(SolverInputParameters).GetProperty("ConstantFrequency");
                                            break;
                                        case "none":
                                            typeof(SolverInputParameters).GetProperty("LubyRestart").SetValue(this, false);
                                            typeof(SolverInputParameters).GetProperty("ConstantRestart").SetValue(this, false);
                                            break;
                                        default:
                                            break;
                                    }
                                    restart.SetValue(this, true);
                                    numericValue.SetValue(this, numericInput);
                                    break;

                                default:
                                    break;
                            }
                        }
                        break;
                    case "localsearch":
                        #region Local search parameters
                        LocalSearch = true;
                        TreeSearch = false;
                        if (inputArgs.Length > 1)
                        {
                            if (inputArgs[1].Equals("random"))
                            {
                                PropertyInfo prop = typeof(SolverInputParameters).GetProperty("RandomLS");
                                prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                            }
                            else if (inputArgs[1].Equals("operators"))
                            {
                                List<int> ops = new List<int>();
                                for (int i = 2; i < inputArgs.Length; i++)
                                {
                                    int op;
                                    if (int.TryParse(inputArgs[i], out op))
                                    {
                                        if (op < 0 || op > 9)
                                        {
                                            Console.WriteLine("Selected operator {0} does not exist, must be 0 <= op < 10", op);
                                            continue;
                                        }
                                        else
                                        {
                                            ops.Add(op);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Input must be number, input:" + inputArgs[i]);
                                        continue;
                                    }
                                }
                                if (ops.Count == (inputArgs.Length - 2))
                                    typeof(SolverInputParameters).GetProperty("OperatorSequence").SetValue(this, ops.ToArray());
                                else
                                {
                                    Console.WriteLine("Invalid argument: Make sure no operator is out of bounds or non numerical");
                                }
                            }
                            else if (inputArgs[1].Equals("limit"))
                            {
                                int limitValue = int.Parse(inputArgs[3]);
                                switch (inputArgs[2])
                                {
                                    case "time":
                                        typeof(SolverInputParameters).GetProperty("LsTimeLimit").SetValue(this, limitValue);
                                        break;
                                    case "solutions":
                                        typeof(SolverInputParameters).GetProperty("LsSolutionLimit").SetValue(this, limitValue);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else if (inputArgs[1].Equals("searchmonitor"))
                            {
                                switch (inputArgs[2])
                                {
                                    case "tabusearch":
                                        PropertyInfo reference = typeof(SolverInputParameters).GetProperty("TabuSearch");
                                        reference.SetValue(this, !(bool)reference.GetValue(this));
                                        break;
                                    case "simulated":
                                        PropertyInfo naive = typeof(SolverInputParameters).GetProperty("SimulatedAnnealing");
                                        naive.SetValue(this, !(bool)naive.GetValue(this));
                                        break;
                                    case "none":
                                        typeof(SolverInputParameters).GetProperty("TabuSearch").SetValue(this, false);
                                        typeof(SolverInputParameters).GetProperty("SimulatedAnnealing").SetValue(this, false);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else if (inputArgs[1].Equals("simplelns"))
                            {
                                typeof(SolverInputParameters).GetProperty("OperatorSequence").SetValue(this, new int[] { 8 });
                                switch (inputArgs[2])
                                {
                                    case "next":
                                        NextLNS = true;
                                        RouteLNS = false;
                                        ActiveLNS = false;
                                        ArrivalLNS = false;
                                        break;
                                    case "route":
                                        NextLNS = false;
                                        RouteLNS = true;
                                        ActiveLNS = false;
                                        ArrivalLNS = false;
                                        break;
                                    case "active":
                                        NextLNS = false;
                                        RouteLNS = false;
                                        ActiveLNS = true;
                                        ArrivalLNS = false;
                                        break;
                                    case "arrival":
                                        NextLNS = false;
                                        RouteLNS = false;
                                        ActiveLNS = false;
                                        ArrivalLNS = true;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else if (inputArgs[1].Equals("assignment"))
                            {

                                PropertyInfo assignment = typeof(SolverInputParameters).GetProperty("InitialAssignment");
                                assignment.SetValue(this, !(bool)assignment.GetValue(this));
                            }
                        }
                        break;
                        #endregion
                    case "route":
                        switch (inputArgs[1])
                        {
                            case "reference":
                                PropertyInfo reference = typeof(SolverInputParameters).GetProperty("Reference");
                                reference.SetValue(this, !(bool)reference.GetValue(this));
                                break;
                            case "naive":
                                PropertyInfo naive = typeof(SolverInputParameters).GetProperty("Naive");
                                naive.SetValue(this, !(bool)naive.GetValue(this));
                                break;
                            case "none":
                                typeof(SolverInputParameters).GetProperty("Naive").SetValue(this, false);
                                typeof(SolverInputParameters).GetProperty("Reference").SetValue(this, false);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "set":
                        #region Set parameters
                        #region Set Constraints
                        if (inputArgs[1].Equals("constraint"))
                        {
                            PropertyInfo[] constraintProperties = (from prop in properties where prop.Name.Contains("Constraint") select prop).ToArray();
                            int index = 0;
                            if (int.TryParse(inputArgs[2], out index))
                                constraintProperties[index].SetValue(this, !(bool)constraintProperties[index].GetValue(this));
                        }
                        #endregion
                        #region Set preprocessing
                        else if (inputArgs[1].Equals("preprocess"))
                        {
                            PropertyInfo prop = null;
                            if (inputArgs[2].Equals("bounds"))
                                prop = typeof(SolverInputParameters).GetProperty("PreprocessingUpperBound");
                            else if (inputArgs[2].Equals("variables"))
                                prop = typeof(SolverInputParameters).GetProperty("PreprocessingVariables");
                            prop.SetValue(this, !(bool)prop.GetValue(this));
                        }
                        #endregion
                        #region Set limits
                        else if (inputArgs[1].Equals("limit"))
                        {
                            int limitValue = int.Parse(inputArgs[3]);
                            switch (inputArgs[2])
                            {
                                case "time":
                                    typeof(SolverInputParameters).GetProperty("TimeLimit").SetValue(this, limitValue);
                                    break;
                                case "solutions":
                                    typeof(SolverInputParameters).GetProperty("SolutionLimit").SetValue(this, limitValue);
                                    break;
                                case "failures":
                                    typeof(SolverInputParameters).GetProperty("BranchLimit").SetValue(this, limitValue);
                                    break;
                                case "branches":
                                    typeof(SolverInputParameters).GetProperty("FailLimit").SetValue(this, limitValue);
                                    break;
                                default:
                                    break;
                            }
                        }
                        #endregion
                        #region Set Objective
                        else if (inputArgs[1].Equals("objective"))
                        {
                            switch (inputArgs[2])
                            {
                                case "makespan":
                                    ObjectiveMakeSpan = true;
                                    ObjectiveMinimizeTravel = false;
                                    break;
                                case "traveltime":
                                    ObjectiveMakeSpan = false;
                                    ObjectiveMinimizeTravel = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                        #endregion
                        #region Set Opimisation
                        else if (inputArgs[1].Equals("optimize"))
                        {
                            PropertyInfo prop = typeof(SolverInputParameters).GetProperty("Optimize");
                            prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                        }
                        #endregion
                        #region Set Logging
                        else if (inputArgs[1].Equals("log"))
                        {
                            int logFrequency = int.Parse(inputArgs[2]);
                            typeof(SolverInputParameters).GetProperty("LogFrequency").SetValue(this, logFrequency);
                        }
                        #endregion
                        #region Set Printing
                        else if (inputArgs[1].Equals("print"))
                        {
                            PropertyInfo prop = typeof(SolverInputParameters).GetProperty("PrintToFile");
                            prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                        }
                        #endregion
                        #region Set Precedences
                        else if (inputArgs[1].Equals("precedence"))
                        {
                            PropertyInfo prop = typeof(SolverInputParameters).GetProperty("DensePrecedence");
                            prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                        }
                        #endregion
                        #region Set Alldifferent
                        else if (inputArgs[1].Equals("alldiff"))
                        {
                            PropertyInfo prop = typeof(SolverInputParameters).GetProperty("AllDiffStrong");
                            prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                        }
                        #endregion
                        #region Set Random
                        else if (inputArgs[1].Equals("random"))
                        {
                            PropertyInfo prop = typeof(SolverInputParameters).GetProperty("Random");
                            prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                        }
                        #endregion
                        #region Set Separate Fixtures
                        else if (inputArgs[1].Equals("separated"))
                        {
                            PropertyInfo prop = typeof(SolverInputParameters).GetProperty("SeparateFixtures");
                            prop.SetValue(this, !(bool)prop.GetValue(this)); //Change the value, true if false, false if true
                        }
                        #endregion
                        #region Set Default Test Parameters
                        if (inputArgs[1].Equals("test-params"))
                        {
                            TimeLimit = 1200000;
                            LogFrequency = 1000;
                            PrintToFile = true;
                            Optimize = true;
                        }
                        #endregion
                        break;
                        #endregion
                    default:
                        break;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine("Wrong number of arguments! Try again!");
            }
        }
    }

    public class ThesisProgram
    {
        public CellDescription cellData;
        public string problemDescriptionFile;
        public string homeFolder;
        public SolverInputParameters inputParams;

        public ThesisProgram()
        {
            this.homeFolder = System.IO.Directory.GetCurrentDirectory();
            while (!System.IO.Directory.Exists(homeFolder + "\\ProblemData"))
                homeFolder = System.IO.Directory.GetParent(homeFolder).FullName;
            string destinationFolder = homeFolder + "\\ProblemData\\";
            problemDescriptionFile = destinationFolder + "problemdescription";
            cellData = new FileInputData(new InputData(problemDescriptionFile)).FetchData();
            inputParams = new SolverInputParameters();
        }
        public void EvalLoop()
        {
            Console.WriteLine("ThesisApplication: Google OR-Tools solver for the FRIDA Vehicle Routing Problem");
            bool EndFlag = false;
            while (!EndFlag)
            {
                //Console.Write(">> ");
                //String input = Console.ReadLine().Trim();
                //String input = "localsearch-test";//
                String input = "treesearch-test";// "solve";
                switch (input)
                {
                    case "quit":
                        EndFlag = true;
                        break;
                    case "reset":
                        inputParams.CleanInputParams();
                        break;
                    case "init":
                        inputParams.InitParameters();
                        break;
                    case "help":
                        PrintHelp();
                        break;
                    case "solve":
                        ConstraintProblem csp = new ConstraintProblem(cellData, inputParams);
                        csp.Solve(); // This will cause the program to stall, wait for the solver to finish and return.
                        break;
                    case "inspect":
                        Console.WriteLine("Inspect input parameter values");
                        inputParams.Inspect();
                        break;
                    case "localsearch-test":
                        Console.WriteLine("Testing possible variations for local search");
                        testLocalSearch();
                        break;
                    case "simplelns-test":
                        Console.WriteLine("Testing possible variations for simplelns");
                        testSimpleLNS();
                        break;
                    case "treesearch-test":
                        testTreeSearch();
                        break;
                    case "random-test":
                        testRandom();
                        break;
                    case "component-test":
                        testComponentPickups();
                        break;
                    default:
                        inputParams.ParseInput(input);
                        break;
                }
            }
        }

        public void testLocalSearch()
        {
            //Test all 9 local search operators.
            ConstraintProblem csp = null;
            inputParams.LocalSearch = true;
            inputParams.TreeSearch = false;
            for (int j = 0; j < 2; j++)
            {
                if (j < 1)
                {
                    inputParams.ObjectiveMakeSpan = true;
                    inputParams.ObjectiveMinimizeTravel = false;
                }
                else
                {
                    inputParams.ObjectiveMakeSpan = false;
                    inputParams.ObjectiveMinimizeTravel = true;
                }

                for (int i = 0; i < 9; i++)
                {
                    inputParams.OperatorSequence = new int[] { i };
                    csp = new ConstraintProblem(cellData, inputParams);
                    csp.Solve();
                }
            }
        }

        public void testSimpleLNS()
        {
            //Test all 9 local search operators.
            ConstraintProblem csp = null;

            inputParams.LocalSearch = true;
            inputParams.TreeSearch = false;

            inputParams.OperatorSequence = new int[] { 8 };

            Console.WriteLine("Testing SimpleLNS on Next Variables");
            inputParams.NextLNS = true;
            inputParams.RouteLNS = false;
            inputParams.ActiveLNS = false;
            inputParams.ArrivalLNS = false;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            Console.WriteLine("Testing SimpleLNS on Route variables");

            inputParams.NextLNS = false;
            inputParams.RouteLNS = true;
            inputParams.ActiveLNS = false;
            inputParams.ArrivalLNS = false;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            Console.WriteLine("Testing SimpleLNS on active variables");

            inputParams.NextLNS = false;
            inputParams.RouteLNS = false;
            inputParams.ActiveLNS = true;
            inputParams.ArrivalLNS = false;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            Console.WriteLine("Testing SimpleLNS on arrivaltime variables");

            inputParams.NextLNS = false;
            inputParams.RouteLNS = false;
            inputParams.ActiveLNS = false;
            inputParams.ArrivalLNS = true;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

        }

        public void testTreeSearch()
        {
            inputParams.TreeSearch = true;
            ConstraintProblem csp;

            inputParams.DensePrecedence = true;
            inputParams.PreprocessingUpperBound = true;
            inputParams.PreprocessingVariables = true;
            inputParams.AllDiffStrong = false;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = true;
            inputParams.PreprocessingUpperBound = false;
            inputParams.PreprocessingVariables = false;
            inputParams.AllDiffStrong = false;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = false;
            inputParams.PreprocessingUpperBound = false;
            inputParams.PreprocessingVariables = false;
            inputParams.AllDiffStrong = false;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = false;
            inputParams.PreprocessingUpperBound = true;
            inputParams.PreprocessingVariables = true;
            inputParams.AllDiffStrong = true;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = false;
            inputParams.PreprocessingUpperBound = false;
            inputParams.PreprocessingVariables = false;
            inputParams.AllDiffStrong = true;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = true;
            inputParams.PreprocessingUpperBound = false;
            inputParams.PreprocessingVariables = false;
            inputParams.AllDiffStrong = true;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = true;
            inputParams.PreprocessingUpperBound = true;
            inputParams.PreprocessingVariables = true;
            inputParams.AllDiffStrong = true;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();

            inputParams.DensePrecedence = false;
            inputParams.PreprocessingUpperBound = true;
            inputParams.PreprocessingVariables = true;
            inputParams.AllDiffStrong = true;

            csp = new ConstraintProblem(cellData, inputParams);
            csp.Solve();


        }

        public void testRandom()
        {
            ConstraintProblem csp;
            inputParams.Random = true;
            for (int i = 0; i < 10; i++)
            {
                csp = new ConstraintProblem(cellData, inputParams);
                csp.Solve();
            }
        }

        public void testComponentPickups()
        {
            string problemDescription = "C:\\Dropbox\\skola\\Exjobb\\testparameters\\problemdescription";
            CellDescription cellDataChange = null;
            for (int i = 1; i <= 6; i++)
            {
                string testFile = problemDescription + i;
                cellDataChange = new FileInputData(new InputData(testFile)).FetchData();
                for (int j = 0; j < 15; j++)
                {
                    ConstraintProblem csp = new ConstraintProblem(cellDataChange, inputParams);
                    csp.Solve();
                }
            }
        }

        public void PrintHelp()
        {
            if (!System.IO.File.Exists(homeFolder + "\\help.txt"))
            {
                Console.WriteLine("No help file in home directory");
                return;
            }
            else
            {
                using (TextReader reader = new StreamReader(homeFolder + "\\help.txt"))
                {
                    String line = reader.ReadLine();
                    while (line != null)
                    {
                        line = line.Trim();
                        if (line.Length == 0)
                        {
                            line = reader.ReadLine();
                            continue;
                        }

                        //Skip comments
                        if (line.StartsWith("#"))
                        {
                            line = reader.ReadLine();
                            continue;
                        }
                        Console.WriteLine(line);

                        line = reader.ReadLine();
                    }
                }
                return;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ThesisProgram p = new ThesisProgram();
            p.EvalLoop();
        }
    }
}
