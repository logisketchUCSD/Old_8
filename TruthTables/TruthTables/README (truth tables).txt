README for truth table recognition
Sara Sheehan, Sketchers 2007

OVERVIEW OF CLASSES

1) Debugging.cs: executable run by the command line that will print out the truth table in a 
form similar to:

00_1
01_0
10_0
11_0

will also print out shape data, and creates 

2) ShapeComparer.cs: (IComparer) used for comparing (sorting) shapes based on x-coordinate,
y-coordinate, height, and width

3) TruthTable.cs: this is the main recognition class. The main recognition method is 
assign(). Given the sketch, assign creates a matrix of 1,0,and -1 (representing X), 
which is then sent to createPins(), which creates a 2D list of pins representing the
table, which is what is ultmaitely used by the user interface. The function ouputLabels()
is what creates the pins representing the labels. These two functions (in region "Pins")
are all that should be necessary for the user interface. The function assign has an
overloaded method assign(int numCols)

4) TTGroup.cs (in Grouper)

5) TruthTableDomain.txt