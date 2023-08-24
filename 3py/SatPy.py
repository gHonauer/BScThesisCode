import glob
import os

import numpy as np
from tqdm import tqdm
from mpl_toolkits import mplot3d
import matplotlib.pyplot as plt
from typing import List

np.set_printoptions(suppress=True)

# Gradient descent function that does the solving
def RunGradDesc(func, startPoint, minStepsize, minimize=True, out_trackStepList: list = None):
    assert type(startPoint) == np.ndarray

    currentP = startPoint  # abcde


    stepSizes = np.ones(shape=currentP.shape)
    lastStepDir = [None] * currentP.shape[0]


    def wrapperFun(X):
        #print(f"Probing X={str(X)}")
        return func(X) if minimize else -func(X)

    def onStepTaken(newX, newY):
        if out_trackStepList is not None:
            out_trackStepList.append((newX, newY if minimize else -newY))

    currentY = wrapperFun(currentP)
    onStepTaken(currentP, currentY)

    saddleCheckStepsize = 1e-3
    mulList = [-saddleCheckStepsize,saddleCheckStepsize]

    def saddlePointCheck(): # return false to stop execution if we conclude that this is not a saddlepoint but an extremum
        nonlocal currentP
        nonlocal currentY

        deltaVec = np.zeros_like(stepSizes)
        for i in range(currentP.shape[0]):
            for i_d in mulList:
                deltaVec[i] = i_d
                for k in range(i+1,currentP.shape[0]):
                    for k_d in mulList:
                        deltaVec[k] = k_d
                        candidateP = currentP + deltaVec
                        newPVal = wrapperFun(candidateP)
                        if newPVal < currentY:
                            currentP = candidateP
                            currentY = newPVal
                            onStepTaken(currentP, currentY)
                            return True  # we found a descent direction --> we can continue going


                    deltaVec[k] = 0

            deltaVec[i] = 0


        deltaVec = np.zeros_like(stepSizes)
        for i in range(currentP.shape[0]):
            for i_d in mulList:
                deltaVec[i] = i_d
                for j in range(i+1,currentP.shape[0]):
                    for j_d in mulList:
                        deltaVec[j] = j_d
                        for k in range(j+1,currentP.shape[0]):
                            for k_d in mulList:
                                deltaVec[k] = k_d
                                candidateP = currentP + deltaVec
                                newPVal = wrapperFun(candidateP)
                                if newPVal < currentY:
                                    currentP = candidateP
                                    currentY = newPVal
                                    onStepTaken(currentP, currentY)
                                    return True  # we found a descent direction --> we can continue going


                            deltaVec[k] = 0

                    deltaVec[j] = 0

            deltaVec[i] = 0

        return False


    keepRunning = True
    while keepRunning:
        for idx in range(currentP.shape[0]):
            delta = np.zeros_like(stepSizes)
            delta[idx] = stepSizes[idx]

            posP = np.array(currentP) + delta
            posPVal = wrapperFun(posP)
            if posPVal < currentY:
                currentP = posP
                currentY = posPVal

                onStepTaken(currentP, currentY)

                if lastStepDir[idx] == True: # true means stepped into positive dir last time
                    stepSizes[idx] = np.minimum(1,stepSizes[idx] * 2)
                else:
                    lastStepDir[idx] = True

            else:
                negP = np.array(currentP) - delta
                negPVal = wrapperFun(negP)
                if negPVal < currentY:
                    currentP = negP
                    currentY = negPVal

                    onStepTaken(currentP, currentY)

                    if lastStepDir[idx] == False: # false means stepped into negative dir last time
                        stepSizes[idx] = np.minimum(1,stepSizes[idx] * 2)
                    else:
                        lastStepDir[idx] = False

                else: # both steps didnt lead to an improvement --> reduce step size
                    if np.all(stepSizes[idx] < minStepsize):
                        keepRunning = saddlePointCheck()
                        break

                    stepSizes[idx] = stepSizes[idx] / 2

    return currentP, currentY * (1 if minimize else -1)


steepness = 5 # 2
def rect(x):
    return (np.tanh((x-0.5)*steepness)+1)/2
    #return np.clip(x,0,1) # only differentiable in interval (0,1), but should be fine

rect_vec = np.vectorize(rect)

def cnfToCont(string: str, clampInputValues:bool = True):
    trimmed = string.replace(" ","")
    clauses = trimmed.split("&")

    literalIdMap = dict()

    cfuncs = []
    for clause in clauses:
        cforms = []
        assert clause[0] == "("
        assert clause[-1] == ")"
        clause = clause[1:-1]
        for subformula in clause.split("|"):

            literal = subformula.replace("!","")
            if literal not in literalIdMap:
                literalIdMap[literal] = len(literalIdMap)

            if "!" in subformula: # replace !a with rect(1-a)
                assert subformula[0] == "!"
                assert "!" not in subformula[1:]

                def fun(literal):
                    idx = literalIdMap[literal]
                    print("Executing negated for idx: ", idx)
                    return lambda X: rect(1-rect(X[idx]))

                cforms.append(fun(literal))
            else:
                def fun(literal):
                    idx = literalIdMap[literal]
                    print("Executing for idx: ", idx)
                    return lambda X:rect(X[idx])

                cforms.append(fun(literal))

        def fun2(clauseArray):
            print("Using clause array with id: ",id(clauseArray))
            return lambda X:rect(np.sum([f(X) for f in clauseArray]))

        cfuncs.append(fun2(cforms))


    def fullFunc(X):
        X2 = np.clip(X,0,1) if clampInputValues else X
        clauseValues = [f(X2) for f in cfuncs]
        return rect(np.product(clauseValues))

    print("Final function id: ", id(fullFunc))

    return fullFunc, literalIdMap

DEBUG_DnfConversion = False

def cnfToContDnf(string: str, clampInputValues:bool = True):
    # turn the cnf formula which is a conjunction of clauses into
    # a sum of products of squared inverted literals

    trimmed = string.replace(" ","")
    clauses = trimmed.split("&")

    literalIdMap = dict()

    cfuncs = []
    for clause in clauses:
        cforms = []
        assert clause[0] == "("
        assert clause[-1] == ")"
        clause = clause[1:-1]
        for subformula in clause.split("|"):

            literal = subformula.replace("!","")
            if literal not in literalIdMap:
                literalIdMap[literal] = len(literalIdMap)

            """
            (a or b) & (a or !b) = 1 <=>
            ![(a or b) & (a or !b)] = !1 <=>
            [!(a or b) or !(a or !b)] = 0 <=>
            [(!a and !b) or (!a and b)] = 0 <=>
            (!a and !b) or (!a and b) = 0 <=>
            ((1-a)² * (1-b)²) + ((1-a)² * b²) = 0 <=>
            (1-a)²*(1-b)² + (1-a)²*b² = 0
            """

            if "!" in subformula: # replace !a with (a)²
                assert subformula[0] == "!"
                assert "!" not in subformula[1:]

                def fun(literal):
                    idx = literalIdMap[literal]
                    if DEBUG_DnfConversion:
                        print("Executing negated for idx: ", idx)
                    return lambda X: X[idx]**2

                cforms.append(fun(literal))
            else: # replace a with (1-a)²
                def fun(literal):
                    idx = literalIdMap[literal]
                    if DEBUG_DnfConversion:
                        print("Executing for idx: ", idx)
                    return lambda X:(1-X[idx])**2

                cforms.append(fun(literal))

        def fun2(clauseArray):
            if DEBUG_DnfConversion:
                print("Using clause array with id: ",id(clauseArray))
            return lambda X:np.product([f(X) for f in clauseArray])

        cfuncs.append(fun2(cforms))


    def fullFunc(X):
        X2 = np.clip(X,0,1) if clampInputValues else X
        clauseValues = [f(X2) for f in cfuncs]
        return np.sum(clauseValues)

    if DEBUG_DnfConversion:
        print("Final function id: ", id(fullFunc))

    return fullFunc, literalIdMap

def dimacsToPlainCnf(lines: List[str]):
    clauses = []
    for untrimmedLine in lines:
        line = untrimmedLine.strip()
        if not line.startswith("c") and not line.startswith("p") and len(line) > 1:
            line = line.rstrip("0").rstrip()
            clauses.append(line.replace(" ","|").replace("-","!"))

    return "&".join([f"({c})" for c in clauses])


ShowPlotIf2dInput = True

def solveCnf(cnfString: str, findModel: bool): # This is the main entry point; it can either try to find a model or a countermodel
    contFunc, literalIdMap = cnfToContDnf(cnfString)
    startpoint = np.zeros(shape=len(literalIdMap))

    showPlot = ShowPlotIf2dInput and len(literalIdMap) == 2

    trackStepList = [] if showPlot else None
    minX, minY = RunGradDesc(contFunc, startpoint, 0.000001, minimize=findModel, out_trackStepList=trackStepList) # perform the solving using gradient descent

    roundedXResult = np.round(minX)
    dnfResult = contFunc(roundedXResult)
    targetY = 0 if findModel else 1 # 0 in the math expression means we found a cnf model

    def wasSuccessful(y):
        return np.abs(y-targetY)<0.01

    def classify(y):
        search_success = wasSuccessful(y)
        resultName = None
        if findModel:
            resultName = "Satisfiable" if search_success else "Unsatisfiable"
        else:
            resultName = "Refutable" if search_success else "Valid"

        return resultName.upper()



    print(f"Best solution at: {minX} with function value: {minY} (target={targetY}) with boolean variables: \n"+
          f"{'; '.join([str(k)+':'+str(minX[v]) for k,v in literalIdMap.items()])}; \n"+
          f"Corresponding boolean inputs: {'; '.join([str(k)+':'+str(minX[v]>0.5) for k,v in literalIdMap.items()])}\n"+
          f"(Debug) Function value indicates {'success' if wasSuccessful(minY) else 'failure'}; CNF formula was thus classified as: {classify(minY)}\n"+
          f"Rounded Function value indicates {'success' if wasSuccessful(dnfResult) else 'failure'}; CNF function was classified as: {classify(dnfResult)}\n"+
          f"Input cnf: {cnfString}\n")

    if showPlot:
        density = 50

        x = np.linspace(0, 1, density)
        y = np.linspace(0, 1, density)

        X, Y = np.meshgrid(x, y)

        Z = np.zeros_like(X)
        for _i in range(X.shape[0]):
            for _j in range(X.shape[0]):
                Z[_i, _j] = contFunc(np.array([X[_i,_j], Y[_i,_j]]))

        fig = plt.figure()
        ax = plt.axes(projection='3d')
        # ax.contour3D(X, Y, Z, 80, cmap='viridis')
        ax.plot_wireframe(X, Y, Z)

        pathXArray = np.zeros(shape=len(trackStepList))
        pathYArray = np.zeros(shape=len(trackStepList))
        pathZArray = np.zeros(shape=len(trackStepList))
        for idx, steptuple in enumerate(trackStepList):
            pathXArray[idx] = steptuple[0][0]
            pathYArray[idx] = steptuple[0][1]
            pathZArray[idx] = steptuple[1]

        ax.plot(pathXArray, pathYArray, pathZArray, '--o', color="green")
        # ax.plot_surface(X,Y,Z, rstride=1, cstride=1,cmap='viridis', edgecolor='none')
        ax.set_xlabel('x')
        ax.set_ylabel('y')
        ax.set_zlabel('z')

        plt.show()
        print("Showing plot, press enter to continue")
        input()

    return wasSuccessful(dnfResult)

def solveDimacs(dimacsFileName: str, findModel: bool):
    lines = None
    with open(dimacsFileName) as f:
        lines = f.readlines()

    cnfRep = dimacsToPlainCnf(lines)
    return solveCnf(cnfRep, findModel)


#cnf = "(a | !b) & (!a | b)"
#cnf = "(!a | !b) & (a | b)"

#cnf = "(!a | b | !c) & (a|!b | c) & (b | !c) & (c) & (!b)" # unsat

#sat:
#cnf="(a | b | !c) & (a|!b | c) & (b | a| !c) & (c) & (!b) & (a2 | b2 | !c2) & (!a2|!b2 | !c2) & (b2 |a2| !c2) & (c2) & (!b2)&(a3 | b3 | !c3) & (a3|!b3 | c3) & (a3|b3 | !c3) & (c3) & (!b3) & (a23 | b23 | !c23) & (a23|!b23 | c23) & (a23|b23 | !c23) & (c23) & (!b23)"
cnf = "(!g | c | i) & (!d | !c) & (!i | !a | g | d | !d | a | !k | !b) & (a | !k | !c | i | h | !g | d | k | !l | !d) & (!i) & (a | !a | !j | g | !b | !l | !e | h | !c) & (!f | h | l | !k | !i | !c | j | a) & (!f | !i | d | f | j | !j | k) & (!b | !e | !j | g | j | !h | !d | !k | !a) & (b | !d | h | !a | !b | f | l | !f | c | !j) & (d | f | !e | !a | l | !d | a | !g | b) & (!k | !h | !g | !a | !i) & (f | !e | h) & (g) & (!f | h | !a | !e | l | !g | !h | !c | i) & (!h | a | !a | !e | e | !i | !f | g | !b) & (!d) & (!g | !f | g | !e | !b | !d) & (!b | l | a | !f | !i | k) & (!h | a | h | !i | f | !f | !l | c) & (i | b | !j | c | !i | a | !c | !g | !l) & (i | d | !h | f) & (j | c | f | !j | i | !e) & (!j | i | !i) & (!c | !k | k | !d | d) & (j) & (k | !b | g | i | d | !i | !e) & (!h | !b | a | h) & (!a) & (b | !l | f) & (!k | !e | !j | !b | !f | h | !l | !c) & (!e | !i | a | !a) & (k | !j | !i | !b | b | j | e | g | !h | !f) & (c | j) & (!l | f | !b | !g | i) & (a | !f | b) & (g | !g | !c | f | !h | !e | b | !a | !l | !i) & (g | !g) & (!g | g | !d | d | !a | j | !e) & (a | k | !h | !a | j | !l | f | g) & (!g | e | k | !b | g | i) & (!a | !f) & (!j | !f | !d | !k | h | c) & (c) & (f | !f | g) & (k | !h | !e | i | l) & (k | d | j | h | !h | !d) & (!g | !f | d) & (b | !b | c | !a | e | !h) & (d | !h | !g | !a | c | !j)"
#cnf = "(!11|9|12)&(!7|!17|!18)&(!13|!17|20)&(!4|!16|12)&(8|2|14)&(!9|!19|14)&(7|4|18)&(!19|!16|5)&(!1|!16|17)&(!2|20|!11)&(!20|9|!17)&(!14|!15|!17)&(19|!18|15)&(6|15|!2)&(12|8|!14)&(!1|!2|!3)&(11|!8|5)&(6|18|!1)&(7|!11|8)&(1|5|15)&(4|10|12)&(11|6|18)&(7|10|3)&(14|!16|!17)&(4|18|13)&(!11|!15|!13)&(!2|!9|20)&(2|!5|19)&(14|6|!19)&(!8|!13|20)&(!9|8|13)&(2|!14|!7)&(3|16|!15)&(!2|13|17)&(!18|!13|16)&(!18|1|!16)&(18|2|14)&(!20|6|!14)&(15|!19|!8)&(4|12|!11)&(19|3|!14)&(6|5|!7)&(10|13|!11)&(15|!1|!3)&(9|6|10)&(!11|!1|16)&(18|!1|12)&(18|!2|!4)&(5|13|!20)&(19|!12|!6)&(15|11|13)&(12|2|!7)&(3|5|!19)&(3|13|!10)&(1|8|!6)&(!2|18|!11)&(!3|6|!9)&(!18|!14|!3)&(!4|!19|!17)&(7|5|!14)&(13|19|!12)&(!12|!7|!3)&(9|7|!19)&(6|2|10)&(11|6|!12)&(15|1|!17)&(20|!1|!4)&(!18|1|5)&(9|18|14)&(15|!17|9)&(!3|11|9)&(14|12|9)&(5|14|2)&(17|!10|!8)&(14|!15|9)&(!6|!20|13)&(1|6|13)&(!16|15|!17)&(!8|19|7)&(!7|3|!1)&(!18|10|17)&(12|!4|14)&(7|10|19)&(20|15|19)&(!13|!17|!9)&(10|!9|3)&(15|!11|10)&(12|1|!13)&(11|3|15)&(16|!2|!1)&(!17|!5|!1)"

#unsat:
#cnf="(a | b | !c) & (a|!b | c) & (b | a| !c) & (c) & (!b) & (a2 | b2 | !c2) & (!a2|b2 | !c2) & (b2 |a2| !c2) & (c2) & (!b2)&(a3 | b3 | !c3) & (a3|!b3 | c3) & (a3|b3 | !c3) & (c3) & (!b3) & (a23 | b23 | !c23) & (a23|!b23 | c23) & (a23|b23 | !c23) & (c23) & (!b23)"

autotest = False

if not autotest:
    solveCnf(cnf, findModel=True)
else:
    rootFolder = "cnfTestFiles"
    for file in tqdm(glob.glob(f"{rootFolder}/**/*.cnf")):
        kind = file.split("\\")[1]
        assert kind in ["sat", "unsat"], "invalid folder structure"
        shouldBeTrue = kind == "sat"

        success = solveDimacs(file, True)
        assert success == shouldBeTrue

print("Done!")
#solveDimacs()
