using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;



namespace InputTest
{
    interface DataReader
    {
        CellDescription FetchData();
    }

    public class FileReader : DataReader
    {

        public CellDescription FetchData()
        {
            String distances1 = "C:\\SVN\\ThesisApplication\\TimeData\\distanceMatrix1.csv";
            String distances2 = "C:\\SVN\\ThesisApplication\\TimeData\\distanceMatrix2.csv";
            String operations1 = "C:\\SVN\\ThesisApplication\\TimeData\\operations1.csv";
            String operations2 = "C:\\SVN\\ThesisApplication\\TimeData\\operations2.csv";

            int[][] distanceMatrix1 = distancesFromFile(distances1);
            int[][] distanceMatrix2 = distancesFromFile(distances2);
            int[] duration1 = durationsFromFile(operations1);
            int[] duration2 = durationsFromFile(operations2);

            //writeTravelTimeToFile(distanceMatrix1, distanceMatrix2);

            CellDescription cellDescr = new CellDescription(64, 3);
            setNodeAndComponentDescription(cellDescr);

            cellDescr.TravellingTimeArm1 = distanceMatrix1;
            cellDescr.TravellingTimeArm2 = distanceMatrix2;

            // CHANGES!

            //LOOP THROUGH DURATION INDEXES AND SET DURATION FOR EACH ARM
            for (int i = 0; i < 64; i++)
            {
                int duration1Val = duration1[i] != -1 ? duration1[i] : 10000;
                int duration2Val = duration2[i] != -1 ? duration2[i] : 10000;

                int[] durations = new int[] { 0, duration1Val, duration2Val };
                cellDescr.Nodes[i].Durations = durations;
            }

            if (true)
                Console.WriteLine(cellDescr.ToString());
            return cellDescr;
        }

        public void setNodeAndComponentDescription(CellDescription cd)
        {
            int nbNodes = cd.NbNodes;
            int nbComponents = cd.NbComponents - 1;
            #region Set component properties
            for (int i = 1; i <= nbComponents; i++)
            {
                if (i < 4)
                {
                    cd.Components[i].suction = true;
                }
                else
                {
                    cd.Components[i].grip = true;
                }
                if (i == 1)
                    cd.Components[i].NbFixtureOperations = 3;
                else
                    cd.Components[i].NbFixtureOperations = 2;
                if (i < 3)
                    cd.Components[i].subAssembly = 1;
                else
                    cd.Components[i].subAssembly = 2;
            }
            #endregion

            #region Set node descriptions
            Node[] nodes = cd.Nodes;
            int counter = 1;
            for (int i = 0; i <= nbNodes; i++)
            {
                if (i < 3)
                #region Start nodes
                {
                    nodes[i].IsStart = true;
                }
                #endregion
                if (i >= 3 && i < 28)
                #region Trays
                {
                    if (i < 8)
                    {
                        nodes[i].Tray = 1;
                    }
                    else if (i < 13)
                    {
                        nodes[i].Tray = 2;
                    }
                    else if (i < 18)
                    {
                        nodes[i].Tray = 3;
                    }
                    else if (i < 23)
                    {
                        nodes[i].Tray = 4;
                    }
                    else if (i < 28)
                    {
                        nodes[i].Tray = 5;
                    }
                    if (counter > 5) counter = 1;
                    nodes[i].Component = counter;
                    counter++;
                }
                #endregion
                if (i >= 28 && i < 43)
                #region Cameras
                {
                    #region Code to set what camera node it is
                    if (i < 31)
                    {
                        nodes[i].Camera = 1;
                    }
                    else if (i < 34)
                    {
                        nodes[i].Camera = 2;
                    }
                    else if (i < 37)
                    {
                        nodes[i].Camera = 3;
                    }
                    else if (i < 40)
                    {
                        nodes[i].Camera = 4;
                    }
                    else if (i < 43)
                    {
                        nodes[i].Camera = 5;
                    }
                    #endregion

                    #region Ugly code to set the component on each camera

                    switch (i)
                    {
                        case (28):
                            nodes[i].Component = 1;
                            break;
                        case (29):
                            nodes[i].Component = 2;
                            break;
                        case (30):
                            nodes[i].Component = 3;
                            break;
                        case (31):
                            nodes[i].Component = 1;
                            break;
                        case (32):
                            nodes[i].Component = 2;
                            break;
                        case (33):
                            nodes[i].Component = 3;
                            break;
                        case (34):
                            nodes[i].Component = 1;
                            break;
                        case (35):
                            nodes[i].Component = 2;
                            break;
                        case (36):
                            nodes[i].Component = 3;
                            break;
                        case (37):
                            nodes[i].Component = 1;
                            break;
                        case (38):
                            nodes[i].Component = 2;
                            break;
                        case (39):
                            nodes[i].Component = 3;
                            break;
                        case (40):
                            nodes[i].Component = 1;
                            break;
                        case (41):
                            nodes[i].Component = 2;
                            break;
                        case (42):
                            nodes[i].Component = 3;
                            break;
                        default:
                            nodes[i].Component = 0;
                            break;
                    }

                    #endregion

                    #region Less intuitive code (Much nicer though!)
                    // Current should be (i - X) mod Y + 1 
                    // where Y = number of components with suction 
                    // and X = numbers of components with suction - 1 (Indexing)
                    //nodes[i].Component = (i - 2) % 3 + 1;
                    #endregion
                }
                #endregion
                if (i >= 43 && i < 53)
                #region Fixtures
                {
                    // Set correct fixture to each node
                    // first 5 are fixture 1.
                    if (i < 48)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }

                    switch (i)
                    {
                        case 43:
                            nodes[i].Component = 1;
                            break;
                        case 44:
                            nodes[i].Component = 2;
                            break;
                        case 45:
                            nodes[i].Component = 3;
                            break;
                        case 46:
                            nodes[i].Component = 4;
                            break;
                        case 47:
                            nodes[i].Component = 5;
                            break;
                        case 48:
                            nodes[i].Component = 1;
                            break;
                        case 49:
                            nodes[i].Component = 2;
                            break;
                        case 50:
                            nodes[i].Component = 3;
                            break;
                        case 51:
                            nodes[i].Component = 4;
                            break;
                        case 52:
                            nodes[i].Component = 5;
                            break;
                        default:
                            break;
                    }
                }
                #endregion
                //if (i >= 53 && i < 63)
                //#region Fixtures Tapping
                //{
                //    // Set correct fixture to each node
                //    // first 5 are fixture 1.
                //    if (i < 58)
                //    {
                //        nodes[i].Fixture = 1;
                //    }
                //    else
                //    {
                //        nodes[i].Fixture = 2;
                //    }
                //    nodes[i].Tapp = true;
                //}
                //#endregion
                if (i >= 53 && i < 55)
                #region Final assembly
                {
                    if (i % 2 == 0)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }
                    nodes[i].IsFinalAssembly = true;
                }
                #endregion
                if (i >= 55 && i < 59)
                #region Peeling
                {
                    if (i % 2 == 0)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }
                    nodes[i].Component = 1;
                    nodes[i].FixtOrd = 1;
                    nodes[i].Peel = true;
                }
                #endregion
                if (i == 59)
                #region Air flow
                {
                    nodes[i].Component = 3; // Only hardcoded in this place!!!!
                    nodes[i].IsAirFlow = true;
                }
                #endregion
                if (i == 60)
                #region IsOutput
                {
                    nodes[i].IsOutput = true;
                }
                #endregion
                if (i >= 61 && i < 64)
                #region End nodes
                {
                    nodes[i].IsEnd = true;
                }
                #endregion
                if (i == 64)
                #region Dummy node
                {
                }
                #endregion
            }
            #endregion
        }

        public int[][] distancesFromFile(String filename)
        {
            int numberOfRealNodes;
            Dictionary<int, int[]> fileDict = fileToDictionary(filename, out numberOfRealNodes);
            int[][] distanceMatrix = new int[64][];
            // Set distance for start and end nodes

            for (int i = 0; i < 64; i++)
            {
                if (i < 3 || (i >= 61 && i < 64))
                {
                    distanceMatrix[i] = new int[64];
                    continue;
                }

                List<int> distanceFromNode = new List<int>();
                int[] distances = new int[numberOfRealNodes];
                int i_index = Id2RealId(i);
                if (!fileDict.TryGetValue(i_index, out distances)) { Debug.Assert(0 == 1); } // If the I node does not exist in this we fail! Error!

                for (int j = 0; j < 64; j++)
                {
                    if (j < 3 || j >= 61)
                        distanceFromNode.Add(0);
                    else
                    {
                        int j_index = Id2RealId(j);
                        distanceFromNode.Add(distances[j_index]);
                    }
                }

                int[] distanceArray = distanceFromNode.ToArray();
                distanceMatrix[i] = distanceArray;
            }
            return distanceMatrix;
        }

        public int Id2RealId(int i)
        {
            int index = 0;
            #region Set distance for all 25 Trays
            if (i >= 3 && i < 28)
            {
                if (i < 8)
                    index = 0;
                else if (i < 13)
                    index = 1;
                else if (i < 18)
                    index = 2;
                else if (i < 23)
                    index = 3;
                else if (i < 28)
                    index = 4;
                else
                    Console.WriteLine("Shouldn't be here");
            }
            #endregion
            #region Set distance for all 15 Cameras
            if (i >= 28 && i < 43)
            {
                if (i < 31)
                    index = 5;
                else if (i < 34)
                    index = 6;
                else if (i < 37)
                    index = 7;
                else if (i < 40)
                    index = 8;
                else if (i < 43)
                    index = 9;
                else
                    Console.WriteLine("Shouldn't be here");
            }
            #endregion
            #region Set distance for all 10 Fixtures allignment
            if (i >= 43 && i < 53)
            {
                if (i < 48)
                    index = 12;
                else
                    index = 13;
            }
            #endregion
            //#region Set distance for all 10 Fixtures tapping
            //if (i >= 53 && i < 63)
            //{
            //    if (i < 58)
            //        inputIndex = 12;
            //    else
            //        inputIndex = 13;
            //}
            //#endregion
            #region Set distance for Final Assembly
            if (i >= 53 && i < 55)
            {
                if (i % 2 == 0)
                    index = 12;
                else
                    index = 13;
            }
            #endregion
            #region Set distance for Peeling nodes
            if (i >= 55 && i < 59)
            {
                if (i % 2 == 0)
                    index = 12;
                else
                    index = 13;
            }
            #endregion
            #region Set distance for Airflow
            if (i == 59)
            {
                index = 10;
            }
            #endregion
            #region Set distance output
            if (i == 60)
            {
                index = 11;
            }
            #endregion
            return index;
        }

        public int[] durationsFromFile(String filename)
        {
            int index = 0;
            int numberOfRealNodes;
            Dictionary<int, int[]> fileDict = fileToDictionary(filename, out numberOfRealNodes);
            int[] durationArray = new int[64];
            for (int i = 0; i < 64; i++)
            {
                // Set duration for start and end nodes to 0
                if (i < 3 || (i >= 61 && i < 64))
                {
                    durationArray[i] = 0;
                    continue;
                }
                index = Id2RealId(i);
                if (false)
                {

                    #region Set distance for all 25 Trays
                    if (i >= 3 && i < 28)
                    {
                        if (i < 8)
                        {
                            if (i == 3)
                                index = 0;
                            else if (i == 4)
                                index = 1;
                            else if (i == 5)
                                index = 2;
                            else if (i == 6)
                                index = 3;
                            else
                                index = 4;
                        }
                        else if (i < 13)
                            if (i == 8)
                                index = 0;
                            else if (i == 9)
                                index = 1;
                            else if (i == 10)
                                index = 2;
                            else if (i == 11)
                                index = 3;
                            else
                                index = 4;
                        else if (i < 18)
                            if (i == 13)
                                index = 0;
                            else if (i == 14)
                                index = 1;
                            else if (i == 15)
                                index = 2;
                            else if (i == 16)
                                index = 3;
                            else
                                index = 4;
                        else if (i < 23)
                            if (i == 18)
                                index = 0;
                            else if (i == 19)
                                index = 1;
                            else if (i == 20)
                                index = 2;
                            else if (i == 21)
                                index = 3;
                            else
                                index = 4;
                        else if (i < 28)
                            if (i == 23)
                                index = 0;
                            else if (i == 24)
                                index = 1;
                            else if (i == 25)
                                index = 2;
                            else if (i == 26)
                                index = 3;
                            else
                                index = 4;
                        else
                            Console.WriteLine("Shouldn't be here");
                    }
                    #endregion
                    #region Set distance for all 15 Cameras
                    if (i >= 28 && i < 43)
                    {
                        if (i < 31)
                            index = 5;
                        else if (i < 34)
                            index = 6;
                        else if (i < 37)
                            index = 7;
                        else if (i < 40)
                            index = 8;
                        else if (i < 43)
                            index = 9;
                        else
                            Console.WriteLine("Shouldn't be here");
                    }
                    #endregion
                    #region Set distance for all 10 Fixtures allignment
                    if (i >= 43 && i < 53)
                    {
                        if (i < 48)
                            index = 12;
                        else
                            index = 13;
                    }
                    #endregion
                    //#region Set distance for all 10 Fixtures tapps
                    //if (i >= 53 && i < 63)
                    //{
                    //    if (i < 48)
                    //        inputIndex = 11;
                    //    else
                    //        inputIndex = 12;
                    //}
                    //#endregion
                    #region Set distance for Final Assembly
                    if (i >= 53 && i < 55)
                    {
                        if (i % 2 == 0)
                            index = 12;
                        else
                            index = 13;
                    }
                    #endregion
                    #region Set distance for Peeling nodes
                    if (i >= 55 && i < 59)
                    {
                        if (i % 2 == 0)
                            index = 12;
                        else
                            index = 13;
                    }
                    #endregion
                    #region Set distance for Airflow
                    if (i == 59)
                    {
                        index = 10;
                    }
                    #endregion
                    #region Set distance output
                    if (i == 60)
                    {
                        // Current should probably be changed later on!
                        index = 11;
                    }
                    #endregion

                }
                int[] duration;
                if (fileDict.TryGetValue(index, out duration))
                {
                    durationArray[i] = duration[0];
                }
            }
            return durationArray;
        }

        /// <summary>
        /// Method to parse input files into dictionary
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>Dictionary with each line as integer transitionValue <lineNumber,line[]></returns>
        public Dictionary<int, int[]> fileToDictionary(String filename, out int counter)
        {
            counter = 0;
            Dictionary<int, int[]> tmpDict = new Dictionary<int, int[]>();
            using (TextReader reader = new StreamReader(filename))
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

                    string[] items = line.Split(new char[] { ';' });
                    List<int> distances = new List<int>();

                    foreach (string item in items)
                    {
                        //string item = Stringitem.Replace(",",".");
                        int value;
                        //string itemSubZero = item.TrimStart(new Char[] { '0' });
                        if (item.Equals("-"))
                            distances.Add(-1);
                        else if (int.TryParse(item, out value))
                        {
                            distances.Add(value);
                        }
                        else
                            Console.WriteLine("Not a number");
                    }

                    tmpDict.Add(counter, distances.ToArray());
                    counter++;
                    line = reader.ReadLine();
                }
            }
            return tmpDict;
        }

        private void writeTravelTimeToFile(int[][] Array1, int[][] Array2)
        {
            DateTime now = DateTime.Now;
            String folder = "C:\\Users\\XJOEJE\\Desktop\\solutions\\distance\\";
            String fileName = now.ToShortDateString() + "-" + now.Hour + "" + now.Minute + "" + now.Second;

            using (StreamWriter file = new StreamWriter(folder + "DM1-" + fileName + ".txt"))
            {
                foreach (int[] array in Array1)
                {
                    foreach (int item in array)
                    {
                        file.Write(item + "\t");
                    }
                    file.Write(Environment.NewLine);
                }
            }

            using (StreamWriter file = new StreamWriter(folder + "DM2-" + fileName + ".txt"))
            {
                foreach (int[] array in Array2)
                {
                    foreach (int item in array)
                    {
                        file.Write(item + ",");
                    }
                    file.Write(Environment.NewLine);
                }
            }
        }
    }
    public class SyntheticData : DataReader
    {

        public SyntheticData()
        {
        }

        public CellDescription FetchData()
        {
            /**
             * Numbering of the nodes.
             * Nodes 0 - 2 = Start nodes,
             * Nodes 3 - 27 = Trays,
             * Nodes 28 - 42 = Cameras,
             * Nodes 43 - 52 = Fixtures,
             * Nodes 53 - 54 = Final assembly,
             * Nodes 55 - 58 = Peeling,
             * Nodes 59 = Air flow,
             * Nodes 60 = IsOutput,
             * Nodes 61 - 63 = End nodes
             * 
             * Components:
             * 1 = Shield
             * 2 = LCD
             * 3 = PCB
             * 4 = Antenna
             * 5 = Cover
             */
            // Don't forget the dummy node!
            int nbNodes = 74;
            int nbComponents = 5;
            // Don't forget the dummy route!
            int nbRoutes = 2 + 1;
            CellDescription cellDescr = new CellDescription(nbNodes, nbRoutes);

            #region Set component properties
            for (int i = 1; i <= nbComponents; i++)
            {
                if (i < 4)
                {
                    cellDescr.Components[i].suction = true;
                }
                else
                {
                    cellDescr.Components[i].grip = true;
                }
            }
            #endregion

            #region Set node descriptions
            Node[] nodes = cellDescr.Nodes;
            int counter = 1;
            for (int i = 0; i <= nbNodes; i++)
            {
                if (i < 3)
                #region Start nodes
                {
                    nodes[i].IsStart = true;
                }
                #endregion
                if (i >= 3 && i < 28)
                #region Trays
                {
                    if (i < 8)
                    {
                        nodes[i].Tray = 1;
                    }
                    else if (i < 13)
                    {
                        nodes[i].Tray = 2;
                    }
                    else if (i < 18)
                    {
                        nodes[i].Tray = 3;
                    }
                    else if (i < 23)
                    {
                        nodes[i].Tray = 4;
                    }
                    else if (i < 28)
                    {
                        nodes[i].Tray = 5;
                    }
                    if (counter > 5) counter = 1;
                    nodes[i].Component = counter;
                    counter++;
                }
                #endregion
                if (i >= 28 && i < 43)
                #region Cameras
                {
                    #region Code to set what camera node it is
                    if (i < 31)
                    {
                        nodes[i].Camera = 1;
                    }
                    else if (i < 34)
                    {
                        nodes[i].Camera = 2;
                    }
                    else if (i < 37)
                    {
                        nodes[i].Camera = 3;
                    }
                    else if (i < 40)
                    {
                        nodes[i].Camera = 4;
                    }
                    else if (i < 43)
                    {
                        nodes[i].Camera = 5;
                    }
                    #endregion

                    #region Ugly code to set the component on each camera

                    switch (i)
                    {
                        case (28):
                            nodes[i].Component = 1;
                            break;
                        case (29):
                            nodes[i].Component = 2;
                            break;
                        case (30):
                            nodes[i].Component = 3;
                            break;
                        case (31):
                            nodes[i].Component = 1;
                            break;
                        case (32):
                            nodes[i].Component = 2;
                            break;
                        case (33):
                            nodes[i].Component = 3;
                            break;
                        case (34):
                            nodes[i].Component = 1;
                            break;
                        case (35):
                            nodes[i].Component = 2;
                            break;
                        case (36):
                            nodes[i].Component = 3;
                            break;
                        case (37):
                            nodes[i].Component = 1;
                            break;
                        case (38):
                            nodes[i].Component = 2;
                            break;
                        case (39):
                            nodes[i].Component = 3;
                            break;
                        case (40):
                            nodes[i].Component = 1;
                            break;
                        case (41):
                            nodes[i].Component = 2;
                            break;
                        case (42):
                            nodes[i].Component = 3;
                            break;
                        default:
                            nodes[i].Component = 0;
                            break;
                    }

                    #endregion

                    #region Less intuitive code (Much nicer though!)
                    // Current should be (i - X) mod Y + 1 
                    // where Y = number of components with suction 
                    // and X = numbers of components with suction - 1 (Indexing)
                    //nodes[i].Component = (i - 2) % 3 + 1;
                    #endregion
                }
                #endregion
                if (i >= 43 && i < 53)
                #region FixturesAllignment
                {
                    // Set correct fixture to each node
                    // first 5 are fixture 1.
                    if (i < 48)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }
                    int c = 0;
                    if (i == 43)
                        c = 1;
                    else if (i == 44)
                        c = 2;
                    else if (i == 45)
                        c = 3;
                    else if (i == 46)
                        c = 4;
                    else if (i == 47)
                        c = 5;
                    else if (i == 48)
                        c = 1;
                    else if (i == 49)
                        c = 2;
                    else if (i == 50)
                        c = 3;
                    else if (i == 51)
                        c = 4;
                    else if (i == 52)
                        c = 5;

                    nodes[i].Component = c;

                }
                #endregion
                if (i >= 53 && i < 63)
                #region FixturesTapping
                {
                    // Set correct fixture to each node
                    // first 5 are fixture 1.
                    if (i < 53)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }
                }
                #endregion
                if (i >= 63 && i < 65)
                #region Final assembly
                {
                    if (i % 2 == 0)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }
                    nodes[i].IsFinalAssembly = true;
                }
                #endregion
                if (i >= 65 && i < 69)
                #region Peeling
                {
                    if (i % 2 == 0)
                    {
                        nodes[i].Fixture = 1;
                    }
                    else
                    {
                        nodes[i].Fixture = 2;
                    }
                    nodes[i].Peel = true;
                }
                #endregion
                if (i == 69)
                #region Air flow
                {
                    nodes[i].Component = 3; // Only hardcoded in this place!!!!
                    nodes[i].IsAirFlow = true;
                }
                #endregion
                if (i == 70)
                #region IsOutput
                {
                    nodes[i].IsOutput = true;
                }
                #endregion
                if (i >= 71 && i < 74)
                #region End nodes
                {
                    nodes[i].IsEnd = true;
                }
                #endregion
                if (i != 74)
                #region All but dummy node
                {
                    nodes[i].Durations = new int[] { 1, 1, 1 };
                }
                #endregion

            }
            #endregion

            Random rand = new Random(DateTime.Now.Millisecond);
            #region Create TravellingTime
            int[][] travellingTimeArm1 = new int[nbNodes][];
            for (int i = 0; i < nbNodes; i++)
            {
                travellingTimeArm1[i] = new int[nbNodes];
                for (int j = 0; j < nbNodes; j++)
                {
                    if (i == j)
                        travellingTimeArm1[i][j] = 0;
                    else
                        // Current needs to change
                        travellingTimeArm1[i][j] = 5 + 5 * Math.Abs(i - j);// rand.Next(10,100);
                }
            }
            int[][] travellingTimeArm2 = new int[nbNodes][];
            for (int i = 0; i < nbNodes; i++)
            {
                travellingTimeArm2[i] = new int[nbNodes];
                for (int j = 0; j < nbNodes; j++)
                {
                    if (i == j)
                        travellingTimeArm2[i][j] = 0;
                    else
                        // Current needs to change
                        travellingTimeArm2[i][j] = 5 + 5 * Math.Abs(j - i); // rand.Next(10, 100);
                }
            }
            cellDescr.TravellingTimeArm1 = travellingTimeArm1;
            cellDescr.TravellingTimeArm2 = travellingTimeArm2;
            #endregion

            writeTravelTimeToFile(travellingTimeArm1, travellingTimeArm2);
            Console.Write(cellDescr.ToString());
            cellDescr.SanityCheck();

            return cellDescr;
        }

        private void writeTravelTimeToFile(int[][] Array1, int[][] Array2)
        {
            DateTime now = DateTime.Now;
            String folder = "C:\\Users\\XJOEJE\\Desktop\\solutions\\distance\\";
            String fileName = now.ToShortDateString() + "-" + now.Hour + "" + now.Minute + "" + now.Second;

            using (StreamWriter file = new StreamWriter(folder + "DM1-" + fileName + ".txt"))
            {
                foreach (int[] array in Array1)
                {
                    foreach (int item in array)
                    {
                        file.Write(item + ",");
                    }
                    file.Write(Environment.NewLine);
                }
            }

            using (StreamWriter file = new StreamWriter(folder + "DM2-" + fileName + ".txt"))
            {
                foreach (int[] array in Array2)
                {
                    foreach (int item in array)
                    {
                        file.Write(item + ",");
                    }
                    file.Write(Environment.NewLine);
                }
            }
        }
    }
    public class FileInputData : DataReader
    {
        CellDescription cell;
        InputData input;

        public FileInputData(InputData _input)
        { input = _input; }

        public CellDescription FetchData()
        {
            /**
                * For this model we need to get a file containing
                * information about the problem instance to be 
                * solved. Current file should be a reference to 
                * the actual file.
                */

            // Read components from file, create components and nodes
            // TODO! Read assembly graph
            cell = initializeDataModel();

            // TODO Initialize subassemblies from assembly graph
            for (int i = 1; i < cell.NbComponents; i++)
                if (i < 4)
                    cell.Components[i].subAssembly = 1;
                else
                    cell.Components[i].subAssembly = 2;


            // Read distances from file
            String distances1 = input.distance1File;
            int[][] distanceMatrix1 = distancesFromFile(distances1);

            String distances2 = input.distance2File;
            int[][] distanceMatrix2 = distancesFromFile(distances2);

            cell.TravellingTimeArm1 = distanceMatrix1;
            cell.TravellingTimeArm2 = distanceMatrix2;

            for (int i = 0; i < cell.NbNodes; i++)
                if (distanceMatrix1[i][i] == -1 && distanceMatrix2[i][i] == -1)
                    Debug.Assert(true == false, "Error, distance is unreachable for index " + i);

            // Read durations from file and add them to nodes
            setDurationsFromFile();

            return cell;
        }

        public CellDescription initializeDataModel()
        {
            int nbc = input.nbc;
            int nbf = input.nbf;
            // Begin by reading component data
            Component[] components = ReadComponentsFromFile();

            // Calculate number of nodes needed
            // We need dimension2 tray for each tray/component combination
            int trays = nbc * nbc;

            // We need dimension2 fixture for each component and fixture combination
            // Also atleast dimension2 tapping for each fixture node so 2 fixture nodes
            int fixtures = nbc * nbf;

            // These are things that need to be found from the assembly graph
            int postProcessingOperations = 14;
            int preProcessingOperations = 1;
            int assemblies = nbf;

            // We need dimension2 fixture for each component that has an operation
            // associated with each fixture node
            int cameras = nbc * (from c in components where c.suction select c).Count(); // Number of components with fixture suction

            // Robot has fromState arms so there will be fromState routes
            // with matching start and end nodes. Also fromState for
            // the virtual dummy route
            int routes = 2 + 1;
            int startAndEnd = routes * 2;

            int nbNodes = trays + fixtures + postProcessingOperations + preProcessingOperations + assemblies + cameras + startAndEnd + 1; // We need dimension2 extra for output

            CellDescription cd = new CellDescription(nbNodes, routes);
            cd.Components = components;
            cd.NbTrays = trays;
            cd.NbFixtures = fixtures;
            cd.NbCameras = cameras;

            setNodeDescriptions(cd);
            return cd;
        }

        public Component[] ReadComponentsFromFile()
        {
            List<Component> components = new List<Component>();

            components.Add(new Component(0, "Dummy")); // Start by adding the dummy component

            Dictionary<int, int[]> tmpDict = new Dictionary<int, int[]>();
            using (TextReader reader = new StreamReader(input.componentFile))//"C:\\Users\\XJOEJE\\Documents\\Visual Studio 2012\\Projects\\InputDataTest\\components"))
            {
                int counter = 1;
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

                    string[] items = line.Split(null);
                    Debug.Assert(items.Length == 3);
                    int grip;
                    int suction;
                    Component c = new Component(counter, items[0]);
                    if (int.TryParse(items[1], out grip))
                        c.grip = grip == 1 ? true : false;
                    if (int.TryParse(items[2], out suction))
                        c.suction = suction == 1 ? true : false;

                    // TODO: To be read from input assembly graph
                    c.NbFixtureOperations = 1;
                    if (counter == 4)
                        c.NbFixtureOperations = 2;

                    components.Add(c);
                    counter++;
                    line = reader.ReadLine();
                }
            }
            return components.ToArray();
        }

        static void setNodeDescriptions(CellDescription cell)
        {
            // Counter variable to use as offset during the procedure
            int counter = 0;
            Node[] nodes = cell.Nodes;

            #region Begin by setting all start nodes
            for (int i = 0; i < cell.NbRoutes; i++)
            {
                nodes[i].IsStart = true;
                // Current will only happen on the last iteration
                if (i == cell.NbRoutes - 1)
                    counter += cell.NbRoutes;
            }
            #endregion

            #region Tray nodes
            for (int i = 0; i < cell.NbTrays; i += cell.NbComponents - 1)
            {
                for (int j = 0; j < cell.NbComponents - 1; j++)
                {
                    int index = i + j + counter;
                    nodes[index].Tray = i / (cell.NbComponents - 1) + 1; // Variable i loops over each row in the tray matrix, NbComponents = 6 for dummy component, -1 to only count real component, + 1 to remove matrixIndex_i indexing
                    nodes[index].Component = j + 1;
                }
                // Current will only happen on the last iteration
                if (i == cell.NbTrays - (cell.NbComponents - 1))
                    counter += cell.NbTrays;
            }
            #endregion

            #region Camera nodes
            // Number of components with cameras
            Component[] cs = (from c in cell.Components where c.suction select c).ToArray();
            int cc = cs.Length;
            for (int i = 0; i < cell.NbCameras; i += cc)
            {
                for (int j = 0; j < cc; j++)
                {
                    int index = i + j + counter;
                    nodes[index].Camera = i / cc + 1;
                    nodes[index].Component = cs[j].ComponentId;
                }
                // Current will only happen on the last iteration
                if (i == cell.NbCameras - cc)
                    counter += cell.NbCameras;
            }
            #endregion

            #region Fixture nodes
            for (int i = 0; i < cell.NbFixtures; i += cell.NbComponents - 1)
            {
                for (int j = 0; j < cell.NbComponents - 1; j++)
                {
                    int index = i + j + counter;
                    nodes[index].Fixture = i / (cell.NbComponents - 1) + 1;
                    nodes[index].Component = j + 1;
                }
                // Current will only happen on the last iteration
                if (i == cell.NbFixtures - (cell.NbComponents - 1))
                    counter += cell.NbFixtures;
            }
            #endregion

            #region Fixture operation nodes

            // First of all set nodes for the tapping operations
            for (int i = 0; i < cell.NbFixtures; i += cell.NbComponents - 1)
            {
                for (int j = 0; j < cell.NbComponents - 1; j++)
                {
                    int index = i + j + counter;
                    nodes[index].Fixture = i / (cell.NbComponents - 1) + 1;
                    nodes[index].FixtOrd = 1;
                    nodes[index].Component = j + 1;
                }
                // Current will only happen on the last iteration
                if (i == cell.NbFixtures - (cell.NbComponents - 1))
                    counter += cell.NbFixtures;
            }

            // next set the nodes for other operations (like peeling)
            // Current needs to be read from the assemblygraph
            int cwo = 1;
            int numberOfOperations = 2; // Current will be found in the assemblyGraph as well
            int fixtures = cell.NbFixtures / (cell.NbComponents - 1);
            for (int i = 0; i < cwo; i++)
                for (int j = 0; j < numberOfOperations; j++)
                    for (int k = 0; k < fixtures; k++)
                    {
                        int index = counter;
                        nodes[counter].Fixture = k + 1;
                        nodes[counter].FixtOrd = 1; // Need to find some way to get this right
                        nodes[counter].Component = 4; // Current is wrong need to fetch from graph
                        nodes[counter].Peel = true;
                        counter++;
                    }
            #endregion

            #region Final assembly nodes
            for (int i = 0; i < fixtures; i++)
            {
                int index = i + counter;
                nodes[index].Fixture = i + 1;
                nodes[index].IsFinalAssembly = true;
                if (i == fixtures - 1)
                    counter += fixtures;
            }
            #endregion

            #region Process nodes before fixtures
            // Needs to be set using assembly graph
            nodes[counter].IsAirFlow = true;
            nodes[counter].Component = 3;
            counter++;
            #endregion

            #region IsOutput node
            nodes[counter].IsOutput = true;
            counter++;
            #endregion

            #region End nodes
            for (int i = 0; i < cell.NbRoutes; i++)
            {
                int index = i + counter;
                nodes[index].IsEnd = true;
                // Current will only happen on the last iteration
                if (i == cell.NbRoutes - 1)
                    counter += cell.NbRoutes;
            }
            #endregion
        }

        public int[][] distancesFromFile(String filename)
        {
            int numberOfRealNodes;
            Dictionary<int, int[]> fileDict = fileToDictionary(filename, out numberOfRealNodes);
            int[][] distanceMatrix = new int[cell.NbNodes][];
            // Set distance for start and end nodes

            for (int i = 0; i < cell.NbNodes; i++)
            {
                if (i < cell.NbRoutes || (i >= cell.NbNodes - cell.NbRoutes))
                {
                    distanceMatrix[i] = new int[cell.NbNodes];
                    continue;
                }

                List<int> distanceFromNode = new List<int>();
                int[] distances = new int[numberOfRealNodes];
                int i_index = Id2RealId(i);
                if (!fileDict.TryGetValue(i_index, out distances)) { Debug.Assert(true == false); } // If the I node does not exist in this we fail! Error!

                for (int j = 0; j < cell.NbNodes; j++)
                {
                    if (j < cell.NbRoutes || j >= cell.NbNodes - cell.NbRoutes)
                        distanceFromNode.Add(0);
                    else
                    {
                        int j_index = Id2RealId(j);
                        distanceFromNode.Add(distances[j_index]);
                    }
                }

                int[] distanceArray = distanceFromNode.ToArray();
                distanceMatrix[i] = distanceArray;
            }
            return distanceMatrix;
        }

        public int[] durationsFromFile(String filename)
        {
            int index = 0;
            int numberOfRealNodes;
            Dictionary<int, int[]> fileDict = fileToDictionary(filename, out numberOfRealNodes);
            int[] durationArray = new int[cell.NbNodes];
            for (int i = 0; i < cell.NbNodes; i++)
            {
                // Set duration for start and end nodes to 0
                if (i < cell.NbRoutes || (i >= cell.NbNodes - cell.NbRoutes))
                {
                    durationArray[i] = 0;
                    continue;
                }
                index = Id2RealId(i);

                int[] duration;
                if (fileDict.TryGetValue(index, out duration))
                {
                    if (i >= 3 && i < 43)
                        durationArray[i] = 100; // Snittet av durations på trays
                    else
                        durationArray[i] = duration[0];
                }
            }
            return durationArray;
        }

        public Dictionary<int, int[]> fileToDictionary(String filename, out int counter)
        {
            counter = 0;
            Dictionary<int, int[]> tmpDict = new Dictionary<int, int[]>();
            using (TextReader reader = new StreamReader(filename))
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

                    string[] items = line.Split(new char[] { ';' });
                    List<int> distances = new List<int>();

                    foreach (string item in items)
                    {
                        int value;
                        if (item.Equals("-"))
                            distances.Add(-1);
                        else if (int.TryParse(item, out value))
                        {
                            distances.Add(value);
                        }
                    }

                    tmpDict.Add(counter, distances.ToArray());
                    counter++;
                    line = reader.ReadLine();
                }
            }
            return tmpDict;
        }

        public int Id2RealId(int i)
        {
            /**
                * Current will given node attributes give the correct inputIndex of the input distance matrix
                * 
                * First are trays, ordered by component
                * Next is cameras ordered by component
                * Next is fixture ordered by component
                * Then comes specials such as output and air
                */
            int index = 0;
            Node[] nodes = cell.Nodes;
            if (nodes[i].Tray != 0)
            {
                // Tray nodes are first in the distance matrix so
                index = nodes[i].Tray - 1;
            }
            else if (nodes[i].Camera != 0)
            {
                // Cameras are number 2 so we need to offset by nbComponents
                index = nodes[i].Camera - 1 + (cell.NbComponents - 1); // -1 for real components
            }
            else if (nodes[i].Fixture != 0)
            {
                // Fixtures are number 3 so we need to offset by 2*nbComponents
                index = nodes[i].Fixture - 1 + 2 * (cell.NbComponents - 1); // -1 for real components
            }
            else if (nodes[i].IsAirFlow)
                index =
                    (2 * (cell.NbComponents - 1)) // Number of trays and cameras
                    + 2 // Number of fixtures offset 
                    ;
            else if (nodes[i].IsOutput)
                index =
                    (2 * (cell.NbComponents - 1)) // Number of trays and cameras
                    + 2 // Number of fixturesoffset
                    + 1; // Airflow

            return index;
        }

        public void setDurationsFromFile()
        {
            String operations1 = input.durations1File;
            String operations2 = input.durations2File;
            int[] duration1 = durationsFromFile(operations1);
            int[] duration2 = durationsFromFile(operations2);

            for (int i = 0; i < cell.NbNodes; i++)
            {
                //int duration1Val = duration1[i] != -1 ? duration1[i] : 10000;
                //int duration2Val = duration2[i] != -1 ? duration2[i] : 10000;
                //int[] durations = new int[] { 0, duration1Val, duration2Val };
                int[] durations = new int[] { 0, duration1[i], duration2[i] };
                //Console.WriteLine("{0},{1},{2}",durations[0], durations[1], durations[2]);
                cell.Nodes[i].Durations = durations;
            }
        }

        private void writeTravelTimeToFile(int[][] Array1, int[][] Array2)
        {
            DateTime now = DateTime.Now;
            String folder = "C:\\Users\\XJOEJE\\Desktop\\solutions\\distance\\"; // Change this to a folder of your choice
            //String folder = "C:\\Users\\SEJOWES1\\Desktop\\FridaSolutions\\distance\\"; // Change this to a folder of your choice
            String fileName = now.ToShortDateString() + "-" + now.Hour + "" + now.Minute + "" + now.Second;

            using (StreamWriter file = new StreamWriter(folder + "DM1-" + fileName + ".txt"))
            {
                foreach (int[] array in Array1)
                {
                    foreach (int item in array)
                    {
                        file.Write(item + " ");
                    }
                    file.Write(Environment.NewLine);
                }
            }

            using (StreamWriter file = new StreamWriter(folder + "DM2-" + fileName + ".txt"))
            {
                foreach (int[] array in Array2)
                {
                    foreach (int item in array)
                    {
                        file.Write(item + " ");
                    }
                    file.Write(Environment.NewLine);
                }
            }
        }
    }


}
