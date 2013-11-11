import os

recognizers = ["DollarHA", "DollarSA", "NDollar", "Img"]
usersets = ["GlobalSet", "UserSet", "ComboSet"]
activities = ["iso", "copy", "synth"]

runs = [ "Run"+str(x) for x in xrange(1, 26) ]
users = [ "USER"+str(x) for x in xrange(1,25) ]


def csv_to_array(filename):
    """read in a csv file into a 2d array"""
    return [
        [y.strip() for y in x.strip().split(",")]
        for x in open(filename).readlines() ]

def empty_csv():
    """return a nested list corresponding to one CSV"""
    return [['actual/observed', 'and', 'or', 'not', 'precision', 'recall', 'accuracy'],
            ['and', 0, 0, 0, '', '', ''],
            ['or', 0, 0, 0, '',''],
            ['not', 0, 0, 0, '', '']]

def accuracy(csv):
    """calculate the accuracy for a single run given the confusion
matrix read in from CSV"""
    correct = float(csv[1][1])+float(csv[2][2])+float(csv[3][3])
    total = ( sum([ int(x) for x in csv[1][1:4]]) +
              sum([ int(x) for x in csv[2][1:4]]) +
              sum([ int(x) for x in csv[3][1:4]]) )
    return correct/total

def write_csv(csv, output_folder, file_name):
    """dump a csv to file"""
    try:
        os.mkdir(output_folder)
    except:
        pass # directory exists
    writer = open("%s\\%s" % (output_folder, file_name), 'w')
    for line in csv:
        print >> writer, ','.join([str(cell) for cell in line])
    writer.close()

def stats_and_output(res, output_folder, file_name):
    """calculate precision, recall, and accuracy"""
    res[0].append("accuracy")
    # 6 lines of brute force!
    # precision
    res[1][4] = float(res[1][1]) / sum(res[1][1:4])
    res[2][4] = float(res[2][2]) / sum(res[2][1:4])
    res[3][4] = float(res[3][3]) / sum(res[3][1:4])
    # recall
    res[1][5] = float(res[1][1]) / (res[1][1]+res[2][1]+res[3][1])
    res[2][5] = float(res[2][2]) / (res[1][2]+res[2][2]+res[3][2])
    res[3][5] = float(res[3][3]) / (res[1][3]+res[2][3]+res[3][3])
    # accuracy
    res[1][6] = accuracy(res)
    write_csv(res, output_folder, file_name)

def summarize_run(run, recognizer, userset, training_activity, testing_activity):
    """summarize a run for one training-testing pair and one level of
user-specific training data"""
    res = empty_csv()
    output_folder = "%s\\%s\\%s" % (run, recognizer, userset)
    input_folder = "%s\\%s" % (run, recognizer)
    file_name = "%s-%s.csv" % (training_activity, testing_activity)
    users = os.listdir(input_folder)
    # make sure we're only getting USERX directories
    for x in usersets:
        if x in users: users.remove(x)
    # sum up the confusion matrices for all the users
    for userdir in ["%s\\%s\\%s" % (input_folder, user, userset) for user in users]:
        results = csv_to_array("%s\\%s" % (userdir, file_name))
        res[0] = results[0]
        for x in xrange(1,4):
            res[x][0] = results[x][0]
            for y in xrange(1,4):
                res[x][y] += int(results[x][y])
    stats_and_output(res,output_folder, file_name)

def anova_summary(recognizer, userset):
    """anova summarization"""
    lines = []
    lines.append(["Test Set", "Iso Train", "Copy Train", "Synth Train", " "]*3)
    for run in runs:
        line = []
        for test in activities:
            line.append("%s-%s" % (run,test))
            for train in activities:
                accuracy = csv_to_array("%s\\%s\\%s\\%s-%s.csv" %
                                    (run, recognizer, userset, train, test))
                line.append(accuracy[1][6])
            line.append(" ")
        lines.append(line)
    try:
        os.mkdir("anova_format")
    except:
        pass # directory exists
    writer = open("anova_format\\%s-%s.csv" % (recognizer, userset), 'w')
    for line in lines:
        print >> writer, ','.join(line)
    writer.close()

def user_anova_summary(recognizer):
    """anova summarization for users"""
    lines = []
    lines.append(["user","run","globalset","userset","comboset"])
    for user in users:
        for run in runs:
            if user in os.listdir("%s\\%s" % (run, recognizer)):
                line = [user,run]
                for s in usersets:
                    csv = csv_to_array("%s\\%s\\%s\\%s\\iso-synth.csv" %
                                       (run,recognizer,user,s))
                    line.append(str(accuracy(csv)))
                lines.append(line)
    try:
        os.mkdir("anova_user")
    except:
        pass #directory exists
    writer = open("anova_user\\%s.csv" % (recognizer,), 'w')
    for line in lines:
        print >> writer, ','.join(line)
    writer.close()
                        
def recognizer_anova_summary(userset, test_set):
    lines = []
    lines.append(["Run-Training", "DollarHA", "DollarSA", "NDollar", "Image", " "]*3)
    for run in runs:
        line = []
        for train_set in activities:
            line.append("%s-%s" % (run, train_set))
            for recognizer in recognizers:
                csv = csv_to_array("%s\\%s\\%s\\%s-%s.csv" %
                                   (run, recognizer, userset,train_set,test_set))
                line.append(csv[1][6])
            line.append(" ");
        lines.append(line)
    try:
        os.mkdir("anova_recs")
    except:
        pass # directory exists
    writer = open("anova_recs\\%s-%s-test.csv" % (userset,test_set), 'w')
    for line in lines:
        print >> writer, ','.join(line)
    writer.close()
            
    

"""
the following 3 are the ones you want to run
summarize needs to be run once, before any others
anova outputs files for testing variance between tasks
user_anova outputs files for testing variance between globalset/comboset/userset
rec_anova outputs files for comparing recognizers to each other
"""

def summarize():
    for run in runs:
        for recognizer in recognizers:
            for userset in usersets:
                for training in activities:
                    for testing in activities:
                        summarize_run(run, recognizer, userset, training, testing)

def anova():
    for recognizer in recognizers:
        for userset in usersets:
            anova_summary(recognizer, userset)

def user_anova():
    for recognizer in recognizers:
        user_anova_summary(recognizer)

def rec_anova():
    for userset in usersets:
        for test_set in activities:
            recognizer_anova_summary(userset, test_set)
