usersets = ["GlobalSet","ComboSet","UserSet"]
recs = ["Img","Dollar"]
sets = ["iso","copy","synth"]
files = []

def read_csv(filename):
    return [
        [y.strip() for y in x.strip().split(",")]
        for x in open(filename).readlines() ]

def accuracy(csv):
    correct = float(csv[1][1])+float(csv[2][2])+float(csv[3][3])
    total = ( sum([ int(x) for x in csv[1][1:4]]) +
              sum([ int(x) for x in csv[2][1:4]]) +
              sum([ int(x) for x in csv[3][1:4]]) )
    return correct/total

def anova(rec, user):
    """anova summarization for recognizer rec using user data from
        either GlobalSet, ComboSet, or UserSet"""
    output = open("%s-%s.csv" % (rec,user), 'w')
    print >> output, "Testing Set, Iso Train Results, Copy Train Results, Synth Trian Results"
    for test in sets:
        for n in xrange(1, 26):
            line = "TestSet%s%s," % (n, test)
            for train in sets:
                line += str(accuracy(read_csv("Run%s\\%s\\%s\\%s-%s.csv" %
                                              (n, rec, user, train, test))))
                line += ","
            print >> output, line
    output.close()
    
    


def main():
    global files
    for s in sets:
        for s2 in sets:
            files.append("%s-%s.csv" % (s,s2))

    for rec in recs:
        for user in usersets:
            anova(rec,user)
    anova("Rubine", "")
