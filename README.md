This repository contains source files for the associated thesis.

`/3py/` contains the Python sources for Chapter 3
`/4net/` contains the C# sources for Chapter 4

### Chapter 3 Readme:
This script was created using Python 3.8 and using the packages listed in environment.yml.
In order to reduce repository size (and to not violate copyright), cnf test instances are not included in this repository.
The python script however can automatically parse dimacs cnf files, such as the ones hosted by cs.ubc.ca: https://www.cs.ubc.ca/~hoos/SATLIB/benchm.html
These problems can manually be put into the folders `/3py/cnfTestFiles/sat/` and `/3py/cnfTestFiles/unsat/`.

### Chapter 4 Readme:
These projects use .NET 7, so just opening up the project in visual studio (Windows/Mac only), or installing the SDK and then running `dotnet run` / `dotnet build` (Pretty much any 64bit platform) should suffice to get started.
The main project contains the solver and the test project contains unittests, some of which also perform the benchmarks.
As before, the benchmark instances are not included, but they would go into `/4net/SatTests/CnfTestFiles/sat/` and `/4net/SatTests/CnfTestFiles/unsat/` as before.