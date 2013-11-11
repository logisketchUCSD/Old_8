"""
Put this file in a directory like results/5,2 and run it to see how 
each of the 3 sets did over all users over all runs.
"""
import os

def go(files, name):
	confusion = [ [0,0,0],[0,0,0],[0,0,0] ]
	for file in files:
		matrix = [ x[:-1].split(',')[1:-1] for x in open(file).read().split()[2:] ]
		for i in xrange(3):
			for j in xrange(3):
				confusion[i][j] += int(matrix[i][j])

	file = open(name,'w')
	file.write(",and,or,not,%correct\n")
	file.write("and,"+str(confusion[0][0])+","+str(confusion[0][1])+","+str(confusion[0][2])+",=B2*100/SUM(B2:D2)\n")
	file.write("or,"+str(confusion[1][0])+","+str(confusion[1][1])+","+str(confusion[1][2])+",=C3*100/SUM(B3:D3)\n")
	file.write("not,"+str(confusion[2][0])+","+str(confusion[2][1])+","+str(confusion[2][2])+",=D4*100/SUM(B4:D4)\n")
	file.write(",,,,=(B2+C3+D4)*100/SUM(B2:D4)\n")
	file.close()


allfiles = filter(lambda x: "Dollar" in x, os.popen('find . -name *.csv').read().split())

userset = filter(lambda x: "UserSet" in x, allfiles)
globalset = filter(lambda x: "GlobalSet" in x, allfiles)
comboset = filter(lambda x: "ComboSet" in x, allfiles)
go(userset,"userset.csv")
go(globalset,"globalset.csv")
go(comboset,"comboset.csv")
