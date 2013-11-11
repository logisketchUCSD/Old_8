This project runs the Dollar and Rubine gesture recognition engines on the Gate 
Study data.  It has been modified to also include the E85 data now.  All of the
paths to the data are hard coded.  Currently, there are also hard coded rules
that filter out the gates that we don't want (nand,nor,xor).  It also has the
ability to perform a pairwise Hausdorff distance comparison between all shapes
of the same user and class.

Output is separated by Run#/Recognizer/Testing User/Training Set(If applicable)
and is formatted as CSV files with some excel formulas.

the analysis scripts and spreadsheets exist to make aggregation and analysis of
data easier.  The main sheet of the spreadsheet can be replaced by the output 
of the parse.py script and the graphs should automatically update themselves.