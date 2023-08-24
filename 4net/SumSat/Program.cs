// comment out to disable producing a cnf string during each solver iteration that can be used to validate the solving process
//#define GENERATE_DEBUG_CNF_STRING


// in non debug builds this is automatically disabled to improve performance
#if !DEBUG
#undef GENERATE_DEBUG_CNF_STRING
#endif

using System.Numerics;

namespace SumSat
{
    public class Program
    {
        /// <summary>
        /// Helper function that turns a dimacs file into a single cnf string
        /// </summary>
        /// <param name="dimacsLines">The lines of the dimacs file to parse</param>
        /// <returns>The cnf string representation that was described in the dimacs file</returns>
        private static string DimacsToCnf(string[] dimacsLines)
        {
            var clauses = new List<string>();
            foreach (var ul in dimacsLines)
            {
                var trimmed = ul.Trim();
                if (!trimmed.StartsWith("c") && !trimmed.StartsWith("p") && trimmed.Length > 1)
                {
                    clauses.Add(trimmed.TrimEnd('0').TrimEnd().Replace(" ", "|").Replace("-", "!"));
                }
            }
            return string.Join("&", clauses.Select(x => $"({x})"));
        }

        /// <summary>
        /// Checks whether or not the given dimacs representation is satisfiable, and if it is returns a model also.
        /// </summary>
        /// <param name="dimacsLines">The lines of the dimacs file for which to check satisfiability</param>
        /// <returns>Whether or not this formula is satisfiable, A model represented as a variable-value map</returns>
        public static (bool, Dictionary<int, bool>?) CheckSatisfiableDimacs(string[] dimacsLines, CancellationToken? ctok = null)
        {
            var cnfString = DimacsToCnf(dimacsLines);
            var (sat, model) = CheckSatisfiable(cnfString, ctok);
            return (sat, model?.ToDictionary(x => int.Parse(x.Key), x => x.Value)); // TODO directly produce clauses rather than turning it into a cnf string and then turning that into clauses, but this is not performance relevant for small formulas
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnf"></param>
        /// <returns></returns>
        public static (bool, Dictionary<string, bool>?) CheckSatisfiable(string cnf, CancellationToken? ctok = null)
        {
            List<string[]> cnfClauseList = cnf.Replace(" ", "").Split("&").Select(clause =>
            {
                //Assert(clause.StartsWith("(") && clause.EndsWith(")"));
                return clause.Trim('(', ')');
            }).Select(x => x.Split("|").ToArray()).ToList();

            var idToVariableMap = new List<string>() { "RESERVED" }; // index 0 must not be used as -0 == 0
            var variableToIdMap = new Dictionary<string, int>();

            foreach (var strClause in cnfClauseList)
            {
                foreach (var variable in strClause)
                {
                    if (!variableToIdMap.ContainsKey(variable.TrimStart('!')))
                    {
                        variableToIdMap.Add(variable.TrimStart('!'), idToVariableMap.Count);
                        idToVariableMap.Add(variable.TrimStart('!'));
                    }
                }
            }

            // eliminate clauses with >3 literals
            // TODO: it may be much better to have the solver support clauses of any size rather than converting it into 3cnf first
            // Further speedup could be achieved by converting 3cnf into forms with less clauses but larger ones.

            int lastCnt = 0;
            string CreateNewVariable()
            {
                string prefix = "UV_";
                while (true)
                {
                    var candidate = prefix + lastCnt;
                    lastCnt += 1;
                    if (!variableToIdMap.ContainsKey(candidate))
                    {
                        prefix = candidate;
                        break;
                    }
                }
                var newId = idToVariableMap.Count;
                variableToIdMap.Add(prefix, newId);
                idToVariableMap.Add(prefix);
                return prefix;
            }

            int i = 0;
            while (i < cnfClauseList.Count)
            {
                var currClause = cnfClauseList[i];
                if (currClause.Length > 3)
                {
                    var newName = CreateNewVariable();
                    var baseClause = currClause.Take(2).Append(newName).ToArray();
                    var restClause = currClause.Skip(2).Append("!" + newName).ToArray();

                    cnfClauseList[i] = baseClause;
                    cnfClauseList.Add(restClause);
                }
                i++;
            }

            List<Clause> initialClauseList = new();
            foreach (var x in cnfClauseList)
            {
                var res = Clause.FromArray(x.Select(y => (y.StartsWith('!') ? -1 : 1) * variableToIdMap[y.TrimStart('!')]).ToArray());
                initialClauseList.Add(res);
            }

            var (sat, modeldict) = CheckSatisfiable(initialClauseList, ctok);
            return (sat, modeldict?.ToDictionary(x => idToVariableMap[x.Key], x => x.Value));
        }

        /// <summary>
        /// Main function of the solver. Accepts a single formula (A list of clauses) and checks whether or not there is a model, and returns a model if one exists.
        /// </summary>
        /// <param name="initialClauseList">The Formula to solve.</param>
        /// <returns>Whether or not this formula is satisfiable, A model represented as a variable-value map</returns>
        private static (bool, Dictionary<int, bool>?) CheckSatisfiable(List<Clause> initialClauseList, CancellationToken? ctok = null)
        {
            // eliminate duplicate literals or duplicate inverted literals inside clauses
            var absSeen = new List<int>(3);
            for (int clauseIdx = initialClauseList.Count - 1; clauseIdx >= 0; clauseIdx--)
            {
                absSeen.Clear();
                var currClause = initialClauseList[clauseIdx];
                bool clauseIsTrue = false;
                for (int litIdx = currClause.varcnt - 1; litIdx >= 0; litIdx--)
                {
                    var currLit = currClause.Variables[litIdx];
                    if (absSeen.Contains(currLit))
                    {
                        currClause = currClause.RemoveNthVariable(litIdx);
                    }
                    else if (absSeen.Contains(-currLit))
                    {
                        clauseIsTrue = true;
                        break;
                    }
                    else
                        absSeen.Add(currLit);
                }

                if (clauseIsTrue)
                    initialClauseList.RemoveAt(clauseIdx);
                else
                    initialClauseList[clauseIdx] = currClause;
            }

            var clauseLists = new List<List<Clause>>() { initialClauseList };

            // count variables to figure out which variables to expand first
            var variableCountsRaw = clauseLists[0].SelectMany(x => x.Variables.Distinct()).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            var min = variableCountsRaw.Select(x => Math.Abs(x.Key)).Min();
            var max = variableCountsRaw.Select(x => Math.Abs(x.Key)).Max();
            var skewedness = new Dictionary<int, float>();
            for (int i = min; i <= max; i++)
            {
                if (variableCountsRaw.ContainsKey(i) && variableCountsRaw.ContainsKey(-i))
                {
                    float sum = variableCountsRaw[i] + variableCountsRaw[-i];
                    skewedness.Add(i, Math.Abs(variableCountsRaw[i] - variableCountsRaw[i]) / sum);
                }
                else if (variableCountsRaw.ContainsKey(i) || variableCountsRaw.ContainsKey(-i))
                {
                    skewedness.Add(i, 1);
                }
            }

            var variableCounts = clauseLists[0].SelectMany(x => x.Variables.Select(y => Math.Abs(y)).Distinct()).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            //var variableExpansionOrder = variableCounts.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();

            // expand most skewed variables first (Variables whichs occurrences are least uniform)
            var variableExpansionOrder = skewedness.OrderByDescending(x => x.Value).Select(x => x.Key).ToList();
            var allVariables = new List<int>(variableExpansionOrder);
            // three steps:
            // a) check if clauseLists is empty --> unsat, or any sublist of clauseLists is empty --> sat
            // b) expand variable with highest occurrence count
            // c) simplify
            // repeat until a comes up with either sat or unsat


            // these are returnvalues of the recrusive method below
            bool sat = false;
            List<int>? model = null;
            // recursive method that performs the solving by selecting a variable and then replacing the Current_Formula by
            // Current_Formula[selected_variable substituted with True] OR Current_Formula[selected_variable substituted with False],
            // while also applying some optimizations
            void UnpackRemainingVariables(List<(List<Clause>, List<int>)> currentFormulasAndStates, List<int> variablesLeftToExpand, List<Clause> untouchedClauses)
            {
#if GENERATE_DEBUG_CNF_STRING
                var intermediateDebugCnf = string.Join("|",
                    currentFormulasAndStates.Select(cf =>
                        string.Join("&", cf.Item1.Concat(untouchedClauses).Select(c => $"({string.Join("|", c.Variables)})"))
                        )
                    );
#endif

                // if there are no variables left to expand and we have not determined this formula is sat then it must be unsatisfiable
                if (variablesLeftToExpand.Count == 0)
                {
                    return;
                }

                // expansion selection is done in the previos iteration, so simply pick the first one
                var varToExpand = variablesLeftToExpand.First();

                // partition the previously unused clauses into a set of newlyUsedClauses and a set of clauses that are still unused
                List<Clause> newlyTouchedClauses = new();
                List<Clause> stillUntouchedClauses = new();
                for (int i = untouchedClauses.Count - 1; i >= 0; i--)
                {
                    if (untouchedClauses[i].ContainsLiteralOrInverted(varToExpand))
                        newlyTouchedClauses.Add(untouchedClauses[i]);
                    else
                        stillUntouchedClauses.Add(untouchedClauses[i]);
                }

                // keep track of unitclauses for more efficient expansion
                var cumulativeUnitClauseCountByVarLeftToExpand = new Dictionary<int, int>(); // variableId : count
                var newFormulas = new List<(List<Clause>, List<int>)>(); // formulas that go into the next solving iteration

                var newFormulaBuckets = new Dictionary<int, List<(nuint, List<Clause>)>>(); // formulasize:list of (hashcode,formula)
                // Adds a formula to the list of next formulas if it is not included yet. This helps keep the formula count rather small.
                void AddFormulaDistinct(List<Clause> formulaToAdd, List<int> trueLiterals)
                {
                    nuint newHashCode = 0;
                    foreach (var clause in formulaToAdd)
                    {
                        newHashCode ^= (nuint)clause.GetHashCode();
                        newHashCode = BitOperations.RotateLeft(newHashCode, 1);
                    }

                    if (newFormulaBuckets.TryGetValue(formulaToAdd.Count, out var bucket))
                    {
                        var ftacnt = formulaToAdd.Count;
                        for (int i = 0; i < bucket.Count; i++)
                        {
                            var (hashcode, cf) = bucket[i];
                            if (newHashCode != hashcode) // if the hashcode doesnt match then the formula definitely wont match, so check the next one
                                continue;

                            bool isEqual = true;
                            for (int j = 0; j < formulaToAdd.Count; j++)
                            {
                                if (!cf[j].Equals(formulaToAdd[j]))
                                {
                                    isEqual = false;
                                    break;
                                }
                            }

                            if (isEqual)
                            {
                                return;
                            }
                        }

                        newFormulas.Add((formulaToAdd, trueLiterals));
                        bucket.Add((newHashCode, formulaToAdd));
                    }
                    else
                    {
                        newFormulas.Add((formulaToAdd, trueLiterals));
                        newFormulaBuckets.Add(formulaToAdd.Count, new() { (newHashCode, formulaToAdd) });
                    }
                }

                var nltf_cap = currentFormulasAndStates.Count > 0 ? currentFormulasAndStates[0].Item1.Count * 2 : 5;
                var nlT_inner = new List<Clause>(nltf_cap); // This is the formula that is produced if the given variable is set to True
                var nlF_inner = new List<Clause>(nltf_cap); // This is the formula that is produced if the given variable is set to False

                // If there is no formula given in the list of formulas, which may happen during solving, then we need to add an empty formula which then gets extended by the newly-used clauses again.
                if (currentFormulasAndStates.Count == 0)
                    currentFormulasAndStates.Add((new List<Clause>(), new List<int>()));

                bool emptyFormulaAlreadyAdded = false; // keep track of whether or not the empty formula was added already, which would indicate sat (assuming there are no unused clauses left)
                bool anySat = false;
                for (int i1 = 0; i1 < currentFormulasAndStates.Count; i1++) // iterate through all current formulas
                {
                    if ((i1 & 0x1FFF) == 0)
                        if (ctok.HasValue && ctok.Value.IsCancellationRequested)
                            throw new TimeoutException("CNF solving has timed out. Aborting.");

                    List<Clause>? formula = currentFormulasAndStates[i1].Item1;
                    List<int>? trueLiterals = currentFormulasAndStates[i1].Item2;
                    var unitClauseVariables = new List<int>();

                    // if the current formula does not contain the variable we are currently eliminating
                    // then it can just be passed on to the next iteration
                    if (newlyTouchedClauses.Count == 0 && !formula.Any(x => x.ContainsLiteralOrInverted(varToExpand)))
                    {
                        AddFormulaDistinct(formula, trueLiterals);
                        continue;
                    }


                    nlT_inner.Clear(); // defined outside to allow reusing
                    nlF_inner.Clear(); // defined outside to allow reusing
                    bool nlT_isUnsat = false; // keep track of whether the formula nlT is unsat to avoid adding it later
                    bool nlF_isUnsat = false;

                    // iterate over all newly used clauses and over the clauses of the current formula
                    for (int i2 = -newlyTouchedClauses.Count; i2 < formula.Count; i2++)
                    {
                        Clause currClause = i2 < 0 ? newlyTouchedClauses[-i2 - 1] : formula[i2];
                        // if the variable we are expanding is contained in the clause then the clause gets eliminated in one resulting formula,
                        // and the clause gets shrunk in the other formula
                        // if the variable is not contained then the clasue gets duplicated


                        // set variable to true in nlT, and false in the new list 'nlF'
                        if (currClause.ContainsExactLiteral(varToExpand))
                        {
                            // the clause in the original formula nlT becomes satisfied --> move this clause to the new list 'nlF' (after shrinking it) but dont add to nlT
                            var cc2 = currClause.RemoveFirstVariable(varToExpand);
                            nlF_inner.Add(cc2);
                            if (cc2.varcnt == 0)
                                nlF_isUnsat = true;
                            else if (cc2.varcnt == 1)
                            {
                                if (!unitClauseVariables.Contains(cc2.variable1))
                                {
                                    unitClauseVariables.Add(cc2.variable1);
                                }
                            }
                        }
                        else if (currClause.ContainsExactLiteral(-varToExpand)) // similar to above, but negated
                        {
                            // the original clause is unsatisfied
                            var cc2 = currClause.RemoveFirstVariable(-varToExpand);
                            nlT_inner.Add(cc2);
                            if (cc2.varcnt == 0)
                                nlT_isUnsat = true;
                            else if (cc2.varcnt == 1)
                            {
                                if (!unitClauseVariables.Contains(cc2.variable1))
                                {
                                    unitClauseVariables.Add(cc2.variable1);
                                }
                            }
                        }
                        else // variable is not contained in the clause --> add it to both formulas
                        {
                            if (currClause.varcnt == 1)
                            {
                                if (!unitClauseVariables.Contains(currClause.variable1))
                                {
                                    unitClauseVariables.Add(currClause.variable1);
                                }
                            }

                            if (currClause.varcnt > 0) // skip empty clauses
                            {
                                var clonedClause = currClause.Clone();
                                nlT_inner.Add(clonedClause);
                                var clonedClause2 = currClause.Clone();
                                nlF_inner.Add(clonedClause2);
                            }
                        }
                    }

                    // count unit clauses to determine what to expand next
                    for (int i3 = 0; i3 < unitClauseVariables.Count; i3++)
                    {
                        int item = unitClauseVariables[i3];
                        if (cumulativeUnitClauseCountByVarLeftToExpand.ContainsKey(item))
                            cumulativeUnitClauseCountByVarLeftToExpand[item]++;
                        else
                            cumulativeUnitClauseCountByVarLeftToExpand.Add(item, 1);
                    }

                    // check whether T and F are identical
                    bool identical = true;
                    if (nlT_inner.Count != nlF_inner.Count)
                        identical = false;
                    else
                        for (int i = 0; i < nlT_inner.Count; i++)
                        {
                            if (!nlT_inner[i].Equals(nlF_inner[i]))
                            {
                                identical = false;
                                break;
                            }
                        }

                    // if the True formula is not unsatisfiable then add it to list of formulas for the next iteration
                    if (!nlT_isUnsat)
                    {
                        var newTrueLiterals = new List<int>(trueLiterals);
                        newTrueLiterals.Add(varToExpand);

                        if (nlT_inner.Count != 0)
                        {
                            AddFormulaDistinct(new(nlT_inner), newTrueLiterals);
                        }
                        else if (!emptyFormulaAlreadyAdded)
                        {
                            emptyFormulaAlreadyAdded = true;
                            AddFormulaDistinct(new(nlT_inner), newTrueLiterals);
                        }
                    }

                    // similarly for the False formula, but only if they are not identical
                    if (!nlF_isUnsat && !identical)
                    {
                        if (nlF_inner.Count != 0)
                            AddFormulaDistinct(new(nlF_inner), new(trueLiterals));
                        else if (!emptyFormulaAlreadyAdded)
                        {
                            emptyFormulaAlreadyAdded = true;
                            AddFormulaDistinct(new(nlF_inner), new(trueLiterals));
                        }
                    }
                }

                #region Experimental code, disabled for now
                // Idea: If the formula only contains unique literals then it must be sat. This could be refined further to cover more cases.
                //var hs = new HashSet<int>();
                //for (int nfi = 0; nfi < newFormulas.Count; nfi++)
                //{
                //    bool formulaIsSat = false;
                //    hs.Clear();
                //    for (int nfj = 0; nfj < newFormulas[nfi].Count; nfj++)
                //    {
                //        for (int nfv = 0; nfv < newFormulas[nfi][nfj].varcnt; nfv++)
                //        {
                //            if (!hs.Add(newFormulas[nfi][nfj].variables[nfv]))
                //            {
                //                formulaIsSat = false;
                //                break;
                //            }
                //        }
                //        if (!formulaIsSat)
                //            break;
                //    }
                //    if (formulaIsSat)
                //    {
                //        sat = true;
                //        return;
                //    }
                //}
                #endregion

                // if the next batch of formulas contains the empty formula (certainly sat) and there are
                // no more clauses to add then this formula is satisfiable
                if (emptyFormulaAlreadyAdded && stillUntouchedClauses.Count == 0)
                {
                    sat = true;
                    model = newFormulas.First(x => x.Item1.Count == 0).Item2;
                    return;
                }

                // if there are no more formulas then this problem is definitely unsatisfiable
                if (newFormulas.Count == 0)
                {
                    sat = false;
                    return;
                }

                currentFormulasAndStates.Clear();

                if (ctok.HasValue && ctok.Value.IsCancellationRequested)
                    throw new TimeoutException("CNF solving has timed out. Aborting.");

                // Compute new expansion order and start next iteration.
                // The current strategy is 
                // * Prioritize variables that do not appear in the list of untouched clauses to keep the growth as small as possible
                // * In case of draws prioritize variables in a way to ensure min(variable appears in unitclause; !variable appears in unitclause) is as large as possible
                // * In case of draws prioritize variables or their negation that appear in as many unit clauses as possible
                UnpackRemainingVariables(newFormulas /*new List<(List<Clause>, List<int>)>(newFormulas) { Capacity = newFormulas.Count }*/,
                    variablesLeftToExpand.Skip(1)
                    .OrderByDescending(x => stillUntouchedClauses.Any(cl => cl.ContainsLiteralOrInverted(x)) ? 0 : 1)
                    .ThenByDescending(x => Math.Min(cumulativeUnitClauseCountByVarLeftToExpand.TryGetValue(x, out int v) ? v : 0, cumulativeUnitClauseCountByVarLeftToExpand.TryGetValue(-x, out int v2) ? v2 : 0))
                    .ThenByDescending(x => (cumulativeUnitClauseCountByVarLeftToExpand.TryGetValue(x, out int v) ? v : 0) + (cumulativeUnitClauseCountByVarLeftToExpand.TryGetValue(-x, out int v2) ? v2 : 0))
                    .ToList(),
                    stillUntouchedClauses);
            }
            // grab an arbitrary first clause to start the algorithm and then do so
            var firstClause = initialClauseList.FindIndex(x => x.Variables.Any(y => Math.Abs(y) == variableExpansionOrder[0]));
            UnpackRemainingVariables(new List<(List<Clause>, List<int>)>() {
                (new List<Clause>() { initialClauseList[firstClause] }, new List<int>())
            }, variableExpansionOrder, initialClauseList.Except(new[] { initialClauseList[firstClause] }).ToList());

            // if the given formula is satisfiable, produce a model dictionary
            var modelDict = sat ? allVariables.ToDictionary(x => x, x => model!.Contains(x)) : null;

            return (sat, modelDict);
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Starting solver...");
            var cnfString = "(!k|i|l)&(!g|!q|!r)&(!m|!q|t)&(!d|!p|l)&(h|b|n)&(!i|!s|n)&(g|d|r)&(!s|!p|e)&(!a|!p|q)&(!b|t|!k)&(!t|i|!q)&(!n|!o|!q)&(s|!r|o)&(f|o|!b)&(l|h|!n)&(!a|!b|!c)&(k|!h|e)&(f|r|!a)&(g|!k|h)&(a|e|o)&(d|j|l)&(k|f|r)&(g|j|c)&(n|!p|!q)&(d|r|m)&(!k|!o|!m)&(!b|!i|t)&(b|!e|s)&(n|f|!s)&(!h|!m|t)&(!i|h|m)&(b|!n|!g)&(c|p|!o)&(!b|m|q)&(!r|!m|p)&(!r|a|!p)&(r|b|n)&(!t|f|!n)&(o|!s|!h)&(d|l|!k)&(s|c|!n)&(f|e|!g)&(j|m|!k)&(o|!a|!c)&(i|f|j)&(!k|!a|p)&(r|!a|l)&(r|!b|!d)&(e|m|!t)&(s|!l|!f)&(o|k|m)&(l|b|!g)&(c|e|!s)&(c|m|!j)&(a|h|!f)&(!b|r|!k)&(!c|f|!i)&(!r|!n|!c)&(!d|!s|!q)&(g|e|!n)&(m|s|!l)&(!l|!g|!c)&(i|g|!s)&(f|b|j)&(k|f|!l)&(o|a|!q)&(t|!a|!d)&(!r|a|e)&(i|r|n)&(o|!q|i)&(!c|k|i)&(n|l|i)&(e|n|b)&(q|!j|!h)&(n|!o|i)&(!f|!t|m)&(a|f|m)&(!p|o|!q)&(!h|s|g)&(!g|c|!a)&(!r|j|q)&(l|!d|n)&(g|j|s)&(t|o|s)&(!m|!q|!i)&(j|!i|c)&(o|!k|j)&(l|a|!m)&(k|c|o)&(p|!b|!a)&(!q|!e|!a)";

            // Example for solving a problem given in dimacs notation
            //var lines = File.ReadAllLines("path/to/dimacs.cnf");
            //var (sat, model) = CheckSatisfiableDimacs(lines);

            var (sat, model) = CheckSatisfiable(cnfString);

            // print satisfiability and a model if one exists:
            Console.WriteLine($"Sat? {sat}; Model: {(model == null ? "NONE" : string.Join(" ", model.Select(x => $"{x.Key}={(x.Value ? 1 : 0)}")))}");

            Console.ReadLine();
        }
    }
}