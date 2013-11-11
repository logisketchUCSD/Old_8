Devin Smith
Summer 2007

SVM:

SVM is a project that provides a wrapper around libsvm (better than the one that they provide).

Contains two main classes. TrainSVM and ClassifySVM. 

TrainSVM is a class that takes in a training file and outputs a model file.

ClassifySVM is a class that takes in a model file and has a function that returns predictions.


Issues:

Sometimes training fails, or probability estimations do not converge.  Maybe a new version will fix this? 