using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InputTest
{
    public class InputData
    {
        public int nbc;
        public int nbf;

        public string componentFile;
        public string distance1File;
        public string distance2File;
        public string durations1File;
        public string durations2File;

        string nbcKey = "components";
        string nbfKey = "fixtures";
        string cdKey = "componentDescriptions";
        string di1Key = "distances1";
        string di2Key = "distances2";
        string du1Key = "operations1";
        string du2Key = "operations2";


        public InputData(string fileName)
        {
            Dictionary<string, string> tmpDict = new Dictionary<string, string>();
            using (TextReader reader = new StreamReader(fileName))
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

                    string[] items = line.Split(new char[] {';'});//null);
                    //Debug.Assert(items.Length == 2);

                    tmpDict.Add(items[0], items[1]);
                    counter++;
                    line = reader.ReadLine();
                }
            }

            string nbcomponents;
            string nbfixtures;
            if (tmpDict.TryGetValue(nbcKey, out nbcomponents))
                int.TryParse(nbcomponents, out nbc);
            if (tmpDict.TryGetValue(nbfKey, out nbfixtures))
                int.TryParse(nbfixtures, out nbf);
            if (!tmpDict.TryGetValue(cdKey, out componentFile))
                Console.WriteLine("No input for file with component descriptions");
            if (!tmpDict.TryGetValue(di1Key, out distance1File))
                Console.WriteLine("No input for file with distance matrix 1");
            if (!tmpDict.TryGetValue(di2Key, out distance2File))
                Console.WriteLine("No input for file with distance matrix 2");
            if (!tmpDict.TryGetValue(du1Key, out durations1File))
                Console.WriteLine("No input for file with durations for arm 1");
            if (!tmpDict.TryGetValue(du2Key, out durations2File))
                Console.WriteLine("No input for file with durations for arm 2");
        }
    }
}