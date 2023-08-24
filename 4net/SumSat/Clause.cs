using System.Numerics;
using static SumSat.HelperFunctions;

namespace SumSat
{
    /// <summary>
    /// Struct for representing clauses for building cnf formulas
    /// </summary>
    internal struct Clause
    {
        public int variable1;
        public int variable2;
        public int variable3;


        /// <summary>
        /// The number of variables that this clause contains. 
        /// Note: If this is less than 3 then there may still be garbage data in <see cref="variable1"/>, <see cref="variable2"/> or <see cref="variable3"/>.
        /// </summary>
        public int varcnt = 0;

        /// <summary>
        /// Creates a new instance of <see cref="Clause"/>.
        /// </summary>
        /// <param name="variableIds"></param>
        public Clause(int variable1, int variable2, int variable3)
        {
            this.variable1 = variable1;
            this.variable2 = variable2;
            this.variable3 = variable3;


            // any nonzero variable is valid and thus contributes to the variable count
            if (variable1 != 0)
                varcnt++;
            else
                Assert(variable2 == 0 && variable3 == 0); // Zeros must only appear at the end of the clause, in one contiguous block

            if (variable2 != 0)
                varcnt++;
            else
                Assert(variable3 == 0);

            if (variable3 != 0)
                varcnt++;

        }

        /// <summary>
        /// This getter should only be used in performance uncritical scenarios. It is marked as obsolete to indicate uses.
        /// </summary>
        [Obsolete("Use direct access if performance is important")]
        public int[] Variables => new[] { variable1, variable2, variable3 }.Take(varcnt).ToArray();

        //public Clause(int[] vars) : this(vars.Length > 0 ? vars[0] : 0, vars.Length > 1? vars[1] : 0, vars.Length > 2 ? vars[2] : 0) { }

        public static Clause FromArray(int[] vars)
        {
            Assert(vars.Length <= 3);
            return new(vars.Length > 0 ? vars[0] : 0, vars.Length > 1 ? vars[1] : 0, vars.Length > 2 ? vars[2] : 0);
        }

        /// <summary>
        /// Creates a deep copy of <see cref="Clause"/>, sharing no references.
        /// </summary>
        /// <returns></returns>
        public Clause Clone()
        {
            return new Clause(variable1, variable2, variable3) { varcnt = varcnt };
        }

        /// <summary>
        /// Removes the first variable of the clause that matches the given one
        /// </summary>
        /// <param name="id">The variable to remove from this clause</param>
        /// <returns>The current clause after the removal.</returns>
        public Clause RemoveFirstVariable(int id)
        {
            if (variable1 == id)
            {
                variable1 = variable2;
                variable2 = variable3;
                varcnt--;
            }
            else if (variable2 == id)
            {
                variable2 = variable3;
                varcnt--;
            }
            else if (variable3 == id)
            {
                variable3 = 0;
                varcnt--;
            }

            return this;
        }

        /// <summary>
        /// Removes the variable at the given position.
        /// </summary>
        /// <param name="idx">The zerobased position (index) of the variable to be removed. Valid values are [0,<see cref="varcnt"/>-1].</param>
        /// <returns>The current clause after the removal.</returns>
        public Clause RemoveNthVariable(int idx)
        {
            if (idx == 0)
            {
                variable1 = variable2;
                variable2 = variable3;
                varcnt--;
            }
            else if (idx == 1)
            {
                variable2 = variable3;
                varcnt--;
            }
            else if (idx == 2)
            {
                variable3 = 0;
                varcnt--;
            }
            else
            {
                Assert(false);
            }

            return this;
        }

        // The ToString override is mainly used in the debugger to visualize clauses when looking through objects.
        public override string ToString()
        {
            return string.Join("∨", Variables.Where(x => x != 0));
        }

        /// <summary>
        /// Returns whether or not the given literal (variable) is contained in this clause exactly (not including negations).
        /// </summary>
        /// <param name="id">The literal (variable) to check for.</param>
        /// <returns>Whether or not this literal is contained.</returns>
        public bool ContainsExactLiteral(int id) =>
            id == variable1 || id == variable2 || id == variable3;

        /// <summary>
        /// Returns whether or not the given literal (variable) or its negation is contained in this clause.
        /// </summary>
        /// <param name="id">The literal (variable) to check for.</param>
        /// <returns>Whether or not this literal (or its negation) is contained.</returns>
        public bool ContainsLiteralOrInverted(int id) =>
            id == variable1 || id == variable2 || id == variable3 || -id == variable1 || -id == variable2 || -id == variable3;

        /// <summary>
        /// Checks whether the given clause is considered to be equal to the current clause.
        /// </summary>
        /// <param name="obj">The clause to compare.</param>
        /// <returns>Whether or not the current clause is equal to the given clause.</returns>
        public bool Equals(Clause obj)
        {
            return variable1 == obj.variable1 && variable2 == obj.variable2 && variable3 == obj.variable3;
        }

        // this is used for quickly checking whether or not two clauses are definitely unequal.
        // In case they appear to be equal then a proper equality check needs to be done.
        public override int GetHashCode()
        {
            return (int)((uint)variable1 | BitOperations.RotateLeft((uint)variable2, 4)
                | BitOperations.RotateLeft((uint)variable2, 8));
        }
    }
}
