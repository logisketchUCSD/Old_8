This is the set of data that the UC Riverside team sent us.
The format they use is as follows (from the email send by a UCR student):

"... first line contains the number of strokes, the next line contains the 
number of points in the first stroke followed by the group id. The rest of 
the lines corresponding to this stroke contain information about each point
but you don't really need it ..."

The CRF files corresponding to their set of data resides in 
Code\Recognition\RunCRF\TESTS\INPUT-MULTIPASS

This data is used in the function doGroupCorrection() in CreateTrainingModel.cs 
in SvmWGL. The function is implemented but there was no time to do tests.
