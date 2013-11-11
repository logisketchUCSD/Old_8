# -------------------------------------------------------------------------- #
################################ RECOGNITION #################################
# -------------------------------------------------------------------------- #

This folder contains classes used for symbol and sketch recognition. Each
subfolder is summarized below, and may contain additional README.txt files,
if necessary.

These files are no longer used, but may be useful if we want to integrate
them again, or as a useful reference for other algorithms.

# -------------------------------------------------------------------------- #


CircuitRec: parses circuit for Verilog writer. Has been rendered obsolete by
     CircuitParser.

Cluster: An experimental grouper designed by Eric Peterson at UCR. Not used.

Clusterer: Old code to group strokes into "clusters." No longer used.

Clusters: Used by Clusterer. No longer used.

Congeal: An image-based recognizer based on work done by Eric Miller at
     UMass Amherst. See readme in the directory for more information

CRF: contains CRF data structure, just library. No longer used.

DecisionTrees: Code from 2010 to replace Weka. Much faster than the original
     Java implementation of Weka, but less accurate.

FeatureSpace: Old version of Featurefy. No longer used.

Flow: Executes CRF and Grouper in sequence. Sort of.

Fragmenter: splits the stroke at critical corner points (be careful, can be
     compiled as class library or console application... JntToXml has -f flag
     that uses Fragmenter as library)

graph_based: C++ code to do graph-based recognition, based of work from
     Prof. Stahovich's group's code. See the ReadMe.txt file in the directory
     for more information

Grouper: Various clustering algorithms, implementing the IGrouper interface

hhreco: symbol recognizer in Java (good symbol recognition, but doesn't work
     with C#)

HierarchicalCluster: An extension of Cluster. No longer used.

ImageAligner: Unused version of Aligner.

Image-Based Recognizer: This recognizer compares a BitmapSymbol to a list of
     template BitmapSymbols. Similar code exists as part of Combination
     Recognizer.

InferFromJnt: Old code to perform CRF inference from a Windows Journal (Jnt)
     file.

InkForm: The code that used to run the project. Has been rendered obsolete
     by Recognition Manager.

IOTrain: trains multilayered perceptron for CircuitRec

JntToXml: Wrapper for converting JNT files to XML files. Not really needed,
     since the Labeler can read Jnt files and write XML files, and you probably
     want to label these, anyway

LoopyBP: contains Matlab code and C# code interfaced together to get LoopyBP
     from Kevin Murphy's code working in C#

MIT_LBP: Code by Michael Ross from MIT to do belief propagation.

NeuralNet: Somebody's Neural Nets homework, in the tree for some reason.

Old Recognizers: At some point a new Recognizers folder was created and this
     one was renamed. No longer used or up-to-date, but may be a good place to
     pull code from in the future.

PrimitiveClassifier: A very simple bit of code to classify substrokes as
     circles, lines and arcs.

PrimitiveGrouper: An unused version of StrokeGrouper. Groups by temporal
     order of strokes and spacial nearness.

PrimitiveRecognizer: Runs a very simple version of LogiSketch. Probably
     somebody's first try at creating this project. Definitely not used.

RecognitionTemplate: An unused template class for recognizers written by Eric
     Peterson at UCR.

RunCRF: executable that interfaces with CRF and TrainCRF that allows
     training or inference

ScaleFamilyTreeSketches: Unused program to scale strokes. Assumingly designed
     to be used with a family tree sketch.

Segment: partitions a labeled Sketch, just library

Settings: Was used by InkForm. InkForm is no longer used, so this is also
     obsolete.

ShapeTemplates: Old code for implementing recognizers using a template class.
     Also some versions of recognizers that are no longer used.

StrokeScanner: Uses sliding scanboxes to detect intersections between
     strokes, and to further detect features at those intersections. Also
     includes a Scanboxes-based grouper which uses K-means.

Svm: Support vector machine library. Autoconverted from Java to C#. Works
     okay, but don't try to read the code or your brain will implode.

SvmWGL: More support vector machine code. See the wiki for details:
     https://www.cs.hmc.edu/twiki/bin/view/Sketchers/SvmWGL

Test_DecisionTree: Tests for the DecisionTree class.

TestHierarchicalClusters: Old tests for the HierarchicalClusters class.
     Rendered useless when we stopped using Clusters.

TrainCRF: all the training code for the CRF, implements several gradients,
     log likelihood, and helper functions

TruthTables: all truth table recognition functions

UCRDataAnalysis: Contains code for (and results from) a UC Riverside Gate
     Study. The project runs the Dollar and Rubine gesture recognition engines
     on the data. Also contains analysis scripts and spreadsheets to make
     it easier to interpret the data.

WireRecognizer: A very simple class that returns the classification "wire"
     with 100% certainty.

ZernikeMomentRecognizer: Code for the Zernike Moment Recognizer. Most of this
     code exists in ComboRecognizer as well.


---Last Updated 13 June 2011---
