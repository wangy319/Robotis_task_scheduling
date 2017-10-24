using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InputTest
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
        

        // PrintSolution
        public bool PrintToFile { get; set; }

        // Hard Coded Routes
        public bool HardCoded { get; set; }
        public bool Reference { get; set; }
        public bool Naive { get; set; }


        //Local Search Parameters

        // Tree Search Parameters


        public SolverInputParameters()
        {
            TimeLimit = 1000;
            BranchLimit = 1000;
            SolutionLimit = 10;
            FailLimit = 1000;
            InitParameters();
        }

        public void CleanInputParams()
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                // this loop will set all input parameters to false
                property.SetValue(this, false);
            }
        }

        public void InitParameters()
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if(property.Name.Contains("Constraints"))
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
                Console.WriteLine("{0} ={1}",property.Name, property.GetValue(this));
            } 
        }

        public void ParseInput(String input)
        {
            PropertyInfo[] properties = typeof(SolverInputParameters).GetProperties();
            String[] inputArgs = input.Split();

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
                    break;
                case "localsearch":
                    LocalSearch = true;
                    TreeSearch = false;
                    break;
                case "route":
                    typeof(SolverInputParameters).GetProperty("HardCoded").SetValue(this, true);
                    switch (inputArgs[1])
	                {
                        case "reference":
                            typeof(SolverInputParameters).GetProperty("Reference").SetValue(this, true);
                            break;
                        case "nave":
                            typeof(SolverInputParameters).GetProperty("Naive").SetValue(this, true);
                            break;
                        default:
                            break;
	                }
                    break;
                case "set":
                    if(inputArgs[1].Equals("constraint"))
                    {
                        PropertyInfo[] constraintProperties = (from prop in properties where prop.Name.Contains("Constraint") select prop).ToArray();
                        int index = 0;
                        if (int.TryParse(inputArgs[2], out index))
                            constraintProperties[index].SetValue(this, true);
                    }
                    else if (inputArgs[1].Equals("preprocess"))
                    {
                        if (inputArgs[2].Equals("bounds"))
                            typeof(SolverInputParameters).GetProperty("PreprocessingUpperBound").SetValue(this, true);
                        else if (inputArgs[2].Equals("variables"))
                            typeof(SolverInputParameters).GetProperty("PreprocessingVariables").SetValue(this, true);
                    }
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
                    break;
                default:
                    break;
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
                Console.Write(">> ");
                String input = Console.ReadLine().Trim();
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
                    default:
                        inputParams.ParseInput(input);
                        break;
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
