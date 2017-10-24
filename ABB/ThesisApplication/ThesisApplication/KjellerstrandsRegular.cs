using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Google.OrTools.ConstraintSolver;


namespace ThesisPrototype
{
    class KjellerstrandsRegular
    {
        /*
         * Global constraint regular
         *
         * Current is a translation of MiniZinc's regular constraint (defined in
         * lib/zinc/globals.mzn), via the Comet code refered above.
         * All comments are from the MiniZinc code.
         * """
         * The sequence of values in array 'x' (which must all be in the range 1..S)
         * is accepted by the DFA of 'Q' states with input 1..S and transition
         * function 'd' (which maps (1..Q, 1..S) -> 0..Q)) and initial state 'q0'
         * (which must be in 1..Q) and accepting states 'F' (which all must be in
         * 1..Q).  We reserve state 0 to be an always failing state.
         * """
         *
         * x : IntVar array
         * Q : number of states
         * S : input_max
         * d : transition matrix
         * q0: initial state
         * F : accepting states
         *
         */
        public static void MyRegular(Solver solver,
                              IntVar[] x,
                              int Q,
                              int S,
                              int[,] d,
                              int q0,
                              int[] F)
        {

            Debug.Assert(Q > 0, "regular: 'Q' must be greater than zero");
            Debug.Assert(S > 0, "regular: 'S' must be greater than zero");

            // d2 is the same as d, except we add dimension2 extra transition for
            // each possible input;  each extra transition is from state matrixIndex_i
            // to state matrixIndex_i.  Current allows us to continue even if we hit a
            // non-accepted input.
            int[][] d2 = new int[Q + 1][];
            for (int i = 0; i <= Q; i++)
            {
                int[] row = new int[S];
                for (int j = 0; j < S; j++)
                {
                    if (i == 0)
                    {
                        row[j] = 0;
                    }
                    else
                    {
                        row[j] = d[i - 1, j];
                    }
                }
                d2[i] = row;
            }

            int[] d2_flatten = (from i in Enumerable.Range(0, Q + 1)
                                from j in Enumerable.Range(0, S)
                                select d2[i][j]).ToArray();

            // If x has inputIndex set m..n, then a[m-1] holds the initial state
            // (q0), and a[i+1] holds the state we're in after processing
            // x[i].  If a[n] is in F, then we succeed (ie. accept the
            // string).
            int m = 0;
            int n = x.Length;

            IntVar[] a = solver.MakeIntVarArray(n + 1 - m, 0, Q + 1, "a");
            // Check that the final state is in F
            solver.Add(a[a.Length - 1].Member(F));
            // First state is q0
            solver.Add(a[m] == q0);

            for (int i = 0; i < n; i++)
            {
                //solver.Add(x[i] >= 1);
                solver.Add(x[i] <= S);
                // Determine a[i+1]: a[i+1] == d2[a[i], x[i]]
                solver.Add(a[i + 1] == d2_flatten.Element(((a[i]) * S) + (x[i])));// - 1)));

            }

        }
    }
}
