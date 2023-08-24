using SumSat;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace SatTests.SumSat
{
    [TestClass]
    public class SumSatTests
    {
        [TestMethod]
        public void SatTests()
        {
            var cnfs = """                
                (!g | c | i) & (!d | !c) & (!i | !a | g | d | !d | a | !k | !b) & (a | !k | !c | i | h | !g | d | k | !l | !d) & (!i) & (a | !a | !j | g | !b | !l | !e | h | !c) & (!f | h | l | !k | !i | !c | j | a) & (!f | !i | d | f | j | !j | k) & (!b | !e | !j | g | j | !h | !d | !k | !a) & (b | !d | h | !a | !b | f | l | !f | c | !j) & (d | f | !e | !a | l | !d | a | !g | b) & (!k | !h | !g | !a | !i) & (f | !e | h) & (g) & (!f | h | !a | !e | l | !g | !h | !c | i) & (!h | a | !a | !e | e | !i | !f | g | !b) & (!d) & (!g | !f | g | !e | !b | !d) & (!b | l | a | !f | !i | k) & (!h | a | h | !i | f | !f | !l | c) & (i | b | !j | c | !i | a | !c | !g | !l) & (i | d | !h | f) & (j | c | f | !j | i | !e) & (!j | i | !i) & (!c | !k | k | !d | d) & (j) & (k | !b | g | i | d | !i | !e) & (!h | !b | a | h) & (!a) & (b | !l | f) & (!k | !e | !j | !b | !f | h | !l | !c) & (!e | !i | a | !a) & (k | !j | !i | !b | b | j | e | g | !h | !f) & (c | j) & (!l | f | !b | !g | i) & (a | !f | b) & (g | !g | !c | f | !h | !e | b | !a | !l | !i) & (g | !g) & (!g | g | !d | d | !a | j | !e) & (a | k | !h | !a | j | !l | f | g) & (!g | e | k | !b | g | i) & (!a | !f) & (!j | !f | !d | !k | h | c) & (c) & (f | !f | g) & (k | !h | !e | i | l) & (k | d | j | h | !h | !d) & (!g | !f | d) & (b | !b | c | !a | e | !h) & (d | !h | !g | !a | c | !j)
                (a | !b) & (!a | b)
                (a) & (a)
                (!a | !b) & (a | b)
                (a | b | !c) & (a|!b | c) & (b | a| !c) & (c) & (!b) & (a2 | b2 | !c2) & (!a2|!b2 | !c2) & (b2 |a2| !c2) & (c2) & (!b2)&(a3 | b3 | !c3) & (a3|!b3 | c3) & (a3|b3 | !c3) & (c3) & (!b3) & (a23 | b23 | !c23) & (a23|!b23 | c23) & (a23|b23 | !c23) & (c23) & (!b23)
                (!k|i|l)&(!g|!q|!r)&(!m|!q|t)&(!d|!p|l)&(h|b|n)&(!i|!s|n)&(g|d|r)&(!s|!p|e)&(!a|!p|q)&(!b|t|!k)&(!t|i|!q)&(!n|!o|!q)&(s|!r|o)&(f|o|!b)&(l|h|!n)&(!a|!b|!c)&(k|!h|e)&(f|r|!a)&(g|!k|h)&(a|e|o)&(d|j|l)&(k|f|r)&(g|j|c)&(n|!p|!q)&(d|r|m)&(!k|!o|!m)&(!b|!i|t)&(b|!e|s)&(n|f|!s)&(!h|!m|t)&(!i|h|m)&(b|!n|!g)&(c|p|!o)&(!b|m|q)&(!r|!m|p)&(!r|a|!p)&(r|b|n)&(!t|f|!n)&(o|!s|!h)&(d|l|!k)&(s|c|!n)&(f|e|!g)&(j|m|!k)&(o|!a|!c)&(i|f|j)&(!k|!a|p)&(r|!a|l)&(r|!b|!d)&(e|m|!t)&(s|!l|!f)&(o|k|m)&(l|b|!g)&(c|e|!s)&(c|m|!j)&(a|h|!f)&(!b|r|!k)&(!c|f|!i)&(!r|!n|!c)&(!d|!s|!q)&(g|e|!n)&(m|s|!l)&(!l|!g|!c)&(i|g|!s)&(f|b|j)&(k|f|!l)&(o|a|!q)&(t|!a|!d)&(!r|a|e)&(i|r|n)&(o|!q|i)&(!c|k|i)&(n|l|i)&(e|n|b)&(q|!j|!h)&(n|!o|i)&(!f|!t|m)&(a|f|m)&(!p|o|!q)&(!h|s|g)&(!g|c|!a)&(!r|j|q)&(l|!d|n)&(g|j|s)&(t|o|s)&(!m|!q|!i)&(j|!i|c)&(o|!k|j)&(l|a|!m)&(k|c|o)&(p|!b|!a)&(!q|!e|!a)                
                """.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim('\r', '\n', ' ')).ToArray();

            for (int i = 0; i < cnfs.Length; i++)
            {
                string? cnf = cnfs[i];
                var (sat, model) = Program.CheckSatisfiable(cnf);
                Assert.IsTrue(sat, $"This cnf is satisfiable, but solver returned unsat! Idx: {i}");
            }
        }

        [TestMethod]
        public void UnsatTests()
        {
            var cnfs = """
                (38|42|22)&(!49|!34)&(!11|!39)&(!44|!35)&(!38|46|!39)&(!45)&(!11|!35)&(!42|!15|45)&(!48|31|!11)&(47|42)&(42|!34)&(!40|39|!20)&(45|46)&(39|!44)&(!20)&(49|35)&(39|35|!11)&(!20)&(!45|15)&(38)&(!22|11)&(40|!48)&(!14|46)&(!34|20|!46)&(!15|45)&(!42|31)&(!45)&(!48|49)&(31|!48)&(11|!47|38)&(39|46)&(!14|!38)&(40|34)&(47|!31)&(!39|!14)&(42|!44)&(!44|!39)&(46|44|!42)&(14)&(20|!11)&(!22|39)&(!49|44|!42)&(!45|!44|31)&(!31|!11)&(39|20)&(!22|38)&(!22|49)&(46|47|31)&(!18|45)&(!22|44)&(!31|38)
                (a | b | !c) & (a|!b | c) & (b | a| !c) & (c) & (!b) & (a2 | b2 | !c2) & (!a2|b2 | !c2) & (b2 |a2| !c2) & (c2) & (!b2)&(a3 | b3 | !c3) & (a3|!b3 | c3) & (a3|b3 | !c3) & (c3) & (!b3) & (a23 | b23 | !c23) & (a23|!b23 | c23) & (a23|b23 | !c23) & (c23) & (!b23)
                (a | b | c) & (!a | !b) & (!a | b) & (a | !b) & (!c)
                (a | b) & (!a | !b) & (!a | b) & (a | !b)
                """.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim('\r', '\n', ' ')).ToArray();

            for (int i = 0; i < cnfs.Length; i++)
            {
                string? cnf = cnfs[i];
                var (sat, model) = Program.CheckSatisfiable(cnf);
                Assert.IsFalse(sat, $"This cnf is unsatisfiable, but solver returned sat! Idx: {i}");
            }
        }

        public string[] GetDimacsGroupFolders()
        {
            var dimacsPath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName, "CnfTestFiles");
            return "unsat;sat".Split(";").Select(x => Path.Combine(dimacsPath, x))
                .SelectMany(currentPath => Directory.GetDirectories(currentPath, "*", SearchOption.TopDirectoryOnly)).ToArray();
        }

        internal static void RunDimacsOfCategory(bool satisfiable, string subfoldername, int? maxDimacsToRunForBenchmark = null)
        {
            bool stopMemtesterThread = false;
            bool resetMemtestMem = false;
            Thread memoryTesterThread;
            try
            {
                long maxMemUsed = 0;
                memoryTesterThread = new Thread(() =>
                {
                    var cp = Process.GetCurrentProcess();
                    while (!stopMemtesterThread)
                    {
                        if (resetMemtestMem)
                        {
                            maxMemUsed = 0;
                            resetMemtestMem = false;
                        }
                        else
                        {
                            cp.Refresh();
                            maxMemUsed = Math.Max(maxMemUsed, cp.PrivateMemorySize64);
                        }

                        Thread.Sleep(100);
                    }
                });

                memoryTesterThread.Start();

                void RestartMemTracking()
                {
                    resetMemtestMem = true;
                }

                long GetCurrentPeakMemuse() => maxMemUsed;

                var dimacsPath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName, "CnfTestFiles");
                var groupFolder = Path.Combine(dimacsPath, satisfiable ? "sat" : "unsat", subfoldername);
                Assert.IsTrue(Directory.Exists(groupFolder));


                Console.WriteLine($"Beginning group section '{groupFolder.Split(Path.DirectorySeparatorChar).Last()}'");
                var sw = new Stopwatch();
                RestartMemTracking();
                var timeout = TimeSpan.FromMinutes(10);
                List<double> timesTakenMs = new();
                int totalCnt = 0;
                foreach (var testFile in Directory.GetFiles(groupFolder, "*.cnf", SearchOption.AllDirectories))
                {
                    var timeoutCtok = new CancellationTokenSource(timeout);
                    if (maxDimacsToRunForBenchmark.HasValue && int.Parse(testFile.Split('-').Last().Split(".cnf").First()) > maxDimacsToRunForBenchmark.Value)
                        continue;

                    sw.Restart();
                    var lines = File.ReadAllLines(testFile);
                    try
                    {
                        var (sat, model) = Program.CheckSatisfiableDimacs(lines, timeoutCtok.Token);
                        Assert.IsTrue(sat == satisfiable, $"Wrong solution. Group: {groupFolder}; idx: {totalCnt}");
                        timesTakenMs.Add(sw.ElapsedMilliseconds);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine($"Solving CNF of Group: {groupFolder}; idx: {totalCnt} has timed out.");
                        timesTakenMs.Add(-timeout.TotalMilliseconds);
                    }
                    totalCnt++;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }

                var peakMemUse = GetCurrentPeakMemuse();
                stopMemtesterThread = true;
                string gn = groupFolder.Split(Path.DirectorySeparatorChar).Last();
                Console.WriteLine($"Ending group section '{gn}'. {totalCnt} Problems solved. Time taken total/avg_perProblem {timesTakenMs.Where(x => x >= 0).DefaultIfEmpty(0).Sum()}ms/{timesTakenMs.Where(x => x >= 0).DefaultIfEmpty(0).Average():N3}ms");
                Console.WriteLine($"Raw ms: [{string.Join(",", timesTakenMs.Select(x => ((long)x).ToString()))}]");
                Console.WriteLine($"PARAMS: timeout={timeout.TotalMinutes}min, maxCnt={(maxDimacsToRunForBenchmark?.ToString() ?? "null")}");

                var orderedPositiveTimes = timesTakenMs.Where(x => x >= 0).Order().ToList();
                double computeMedian()
                {
                    var centerIndex = orderedPositiveTimes.Count / 2;
                    var medianValue = orderedPositiveTimes.Count % 2 == 1 ?
                        orderedPositiveTimes[centerIndex] : (orderedPositiveTimes[centerIndex - 1] + orderedPositiveTimes[centerIndex]) / 2;
                    return medianValue;
                }

                var anyPositive = orderedPositiveTimes.Count > 0;

                var fmt = new NumberFormatInfo() { NumberDecimalSeparator = ".", NumberGroupSeparator = " " };

                string msTimeToText(double ms)
                {
                    if (ms == 0)
                        return "0s";

                    var units = "m;;k;M".Split(';').ToArray();
                    var prefixIdx = (int)Math.Log(ms, 1000);
                    return $"{Math.Round(ms / Math.Pow(1000, prefixIdx), 3).ToString(fmt)}{units[prefixIdx]}s";
                }

                var tmean = anyPositive ? msTimeToText(orderedPositiveTimes.Average()) : "-";
                var tmedian = anyPositive ? msTimeToText(computeMedian()) : "-";
                var tmin = anyPositive ? msTimeToText(orderedPositiveTimes.Min()) : "-";
                var tmax = anyPositive ? msTimeToText(orderedPositiveTimes.Max()) : "-";
                var pctTimeout = @$"{Math.Ceiling(100d * timesTakenMs.Count(x => x < 0) / timesTakenMs.Count):N0} \%";


                var units = "B;kiB;MiB;GiB".Split(';').ToArray();
                var prefixIdx = (int)Math.Log(peakMemUse, 1024);
                var peakmem = $"{Math.Round(peakMemUse / Math.Pow(1024, prefixIdx), 3).ToString(fmt)} {units[prefixIdx]}";

                string tstart = @"\begin{tabular}{c}";
                string tend = @"\end{tabular}";

                string ltr = $"{gn} & {(satisfiable ? "Y" : "N")} & {totalCnt} & {string.Join('/', gn.Split('-').Select(x => x.TrimStart('u', 'f')))} & {tstart}{tmean} / {tmedian} \\\\ {tmin} / {tmax}{tend} & {pctTimeout} & {peakmem} " + @"\\\hline";

                Console.WriteLine($"Latex table row:\n{ltr}");
            }
            finally
            {
                stopMemtesterThread = true;
            }
            memoryTesterThread.Join();
        }

        internal static void RunDimacsOfCategory(string subfoldername, int? maxDimacsToRunForBenchmark) =>
            RunDimacsOfCategory(!subfoldername.Contains("uuf"), subfoldername, maxDimacsToRunForBenchmark);

        [TestClass]
        public class DimacsBenchmarkTests
        {
            [TestMethod] public void RunDimacsOfCategoryUF20() => RunDimacsOfCategory("uf20-91", null);
            [TestMethod] public void RunDimacsOfCategoryUF50() => RunDimacsOfCategory("uf50-218", 100);
            [TestMethod] public void RunDimacsOfCategoryUF75() => RunDimacsOfCategory("uf75-325", 5);

            [TestMethod] public void RunDimacsOfCategoryUUF50() => RunDimacsOfCategory("uuf50-218", 100);
            [TestMethod] public void RunDimacsOfCategoryUUF75() => RunDimacsOfCategory("uuf75-325", 5);

        }


        [TestMethod]
        public void zDimacsTests()
        {
            bool fullRun = false;
            var dimacsPath = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName, "CnfTestFiles");
            var sw = new Stopwatch();
            foreach (var subname in "unsat;sat".Split(";"))
            {
                Console.WriteLine($"Beginning section '{subname}'");

                var currentPath = Path.Combine(dimacsPath, subname);

                foreach (var groupFolder in Directory.GetDirectories(currentPath, "*", SearchOption.TopDirectoryOnly))
                {

                    Console.WriteLine($"Beginning group section '{groupFolder.Split(Path.DirectorySeparatorChar).Last()}'");
                    sw.Restart();

                    int totalCnt = 0;
                    foreach (var testFile in Directory.GetFiles(currentPath, "*.cnf", SearchOption.AllDirectories))
                    {
                        if (!fullRun && int.Parse(testFile.Split('-').Last().Split(".cnf").First()) > 25)
                            continue;

                        var lines = File.ReadAllLines(testFile);
                        var (sat, model) = Program.CheckSatisfiableDimacs(lines);
                        Assert.IsTrue(sat == (subname == "sat"), $"Wrong solution. Section: {subname}; idx: {totalCnt}");
                        totalCnt++;

                        //if (subname == "unsat" && totalCnt >= 1) // only run 1 test
                        //    break;
                        GC.Collect();
                    }
                    var timeTaken = sw.ElapsedMilliseconds;
                    Console.WriteLine($"Ending group section '{groupFolder.Split(Path.DirectorySeparatorChar).Last()}'. {totalCnt} Problems solved. Time taken total/perProblem {timeTaken}ms/{(float)timeTaken / totalCnt:N3}ms");
                }

                Console.WriteLine($"Ending section '{subname}'");
            }
        }
    }
}
