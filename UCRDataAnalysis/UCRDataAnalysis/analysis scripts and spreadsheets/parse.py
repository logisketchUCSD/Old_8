"""
This creates detailed output of results and shows each of the 
9 pairwise combinations of training and testing data.

Use with the spreadsheets in this directory by replacing
the main sheet in the spreadsheet with the CSV output of this
script.
"""
import os

users = os.listdir("Dollar")
sets = ["GlobalSet","UserSet","ComboSet"]
acts = ["iso","copy","synth"]
labels = ["and","or","not"]

lc = 1

for set in sets:    
    fout = open(set+".csv",'w')
    lc = 1
    for train in acts:
        for test in acts:
            gates = [[],[],[]]
            for user in users:
                cwd = "Dollar/"+user+"/"+set+"/"
                lines = [ x.split(',')[1:-1] for x in open(cwd+train+"-"+test+".csv").readlines()[1:] ]
                for i in xrange(3):
                    sum = 0
                    for j in xrange(3):
                        sum += int(lines[i][j])
                    if sum > 0:
                        gates[i]+=[float(lines[i][i])/float(sum)]
                    else:
                        gates[i]+=['na']
            for i in xrange(3):
                fout.write(train+"-"+test+": "+labels[i])
                for percent in gates[i]:
                    fout.write(","+str(percent))
                fout.write("\n,=AVERAGE(B"+str(lc)+":E"+str(lc)+"),=STDEV(B"+str(lc)+":E"+str(lc)+")\n\n")
                lc += 3
    fout.close()

