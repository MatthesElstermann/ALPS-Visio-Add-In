
using alps.net.api.ALPS;
using alps.net.api.ALPS.ALPSModelElements.ALPSSIDComponents;
using alps.net.api.parsing;
using alps.net.api.StandardPASS;
using alps.net.api.util;
using Microsoft.Office.Interop.Visio;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Configuration;
using VDS.RDF.Shacl.Validation;
using Visio = Microsoft.Office.Interop.Visio;

namespace VisioAddIn.OwlShapes
{
    public class VisioModelLayer : ModelLayer, IVisioExportable
    {

        public VisioModelLayer(IPASSProcessModel model, string labelForID = null, string comment = null, string additionalLabel = null,
            IList<IIncompleteTriple> additionalAttribute = null)
            : base(model, labelForID, comment, additionalLabel, additionalAttribute) { setContainedBy(model); }

        protected VisioModelLayer() { }


        public void exportToVisio(Visio.Page currentPage, ISimple2DVisualizationBounds bounds = null)
        {
            Boolean simple2DVis = checkForSimple2DVisualizationAndSamePageRatio();
            if (simple2DVis)
            {
                determinAndSetSimple2DVisPagebounds(currentPage);
            }
            else
            {
                analyseContainedSubjectsForAutoArrange();
            }
            //Todo: determin if via auto arrange or not for this page here
            // find bounds / make sure same page ratio
            // wenn extending layer match with base layer to fit 

            IList<IPASSProcessModelElement> exportedElements = new List<IPASSProcessModelElement>();
            // Go through each model element on the layer/SID First place the subjects
            Debug.WriteLine("");

            //TODOS: auto place mechanism if no coordinates are available:

            //Todo: sort he subjects...for placing groups/system interfaces first and size them

            //Auto arrang only 
                // 
            foreach (ISubject modelElement in getElements().Select(x => x.Value).OfType<ISubject>())
            {

                if (!(modelElement is IVisioExportable exportableSubj)) continue;

                exportableSubj.exportToVisio(currentPage);
                ISubject mySub = (ISubject)exportableSubj;
                exportedElements.Add(modelElement);
            }

            // secondly do the exchange Lists (groups of message exchanges that should be modeled together
            // exchange List should also contain the routing coordinates if applicable
            foreach (IMessageExchangeList modelElement in getElements().Select(x => x.Value).OfType<IMessageExchangeList>())
            {
                if (!(modelElement is IVisioExportable exportable)) continue;


                exportable.exportToVisio(currentPage);
                exportedElements.Add(modelElement);
            }

            foreach (IMessageExchange modelElement in getElements().Select(x => x.Value).OfType<IMessageExchange>())
            {
                if (!(modelElement is IVisioExportable exportable)) continue;


                exportable.exportToVisio(currentPage);
                exportedElements.Add(modelElement);
            }
            foreach (var modelElement in getElements().Select(x => x.Value).Where(x => !exportedElements.Contains(x)))
            {
                if (modelElement is ISubjectBehavior || !(modelElement is IVisioExportable exportable))
                    continue;

                exportable.exportToVisio(currentPage);
                exportedElements.Add(modelElement);

            }
        }

       

        private bool checkForSimple2DVisualizationAndSamePageRatio()
        {
            bool result = true;
            // First, check if all get2DPageRatio values are identical
            double commonPageRatio = -1;
            bool isFirstSubject = true;

            foreach (ISubject modelElement in getElements().Select(x => x.Value).OfType<ISubject>())
            {

                double pageRatio = modelElement.get2DPageRatio();

                if (isFirstSubject)
                {
                    commonPageRatio = pageRatio;
                    isFirstSubject = false;
                    if (commonPageRatio <= 0) { result = false; break; }
                }
                else
                {
                    if (commonPageRatio != pageRatio)
                    {
                        Log.Warning("Mismatching pageRatio values among subjects in " + this.getModelComponentID());
                        result = false;
                        break;
                    }
                }
                //ISubject demo;
                //modelElement.getContainedBy(demo);
            }
            
            return result;
        }

        private void analyseContainedSubjectsForAutoArrange()
        {
            //Circular
            //Step
            //sorting
            IEnumerable<ISubject> mySubjects = this.getElements().Select(x => x.Value).OfType<ISubject>();
            List<ISubject> subjectsToBeAutoArranged = mySubjects.ToList();
            Debug.WriteLine("Subjects: " + mySubjects.Count());
            int numberOfSubjectToBePlaced = mySubjects.Count();

            //Remove subjects from the list that contained within groups (group subjects or system interfaces)
            foreach (ISubject modelElement in mySubjects)
            {
                if (modelElement is ISubjectGroup groupSubject)
                {
                    IDictionary<string, ISubject> containedSubjects = groupSubject.getContainedSubjects();

                    foreach (ISubject containedSubject in containedSubjects.Values)
                    {
                        subjectsToBeAutoArranged.Remove(containedSubject);

                    }
                }
            }

            //Debug.WriteLine("Before Sorting: ");
            //subjectsToBeAutoArranged.ForEach(subject => Debug.WriteLine(subject.getModelComponentID()+";"));
            //Debug.WriteLine("Special");
            //subjectsToBeAutoArranged.OrderByDescending(subject => getMessageExchangeSum(subject)).ToList().ForEach(subject => Debug.WriteLine(subject.getModelComponentID() + ";"));
            
            subjectsToBeAutoArranged = subjectsToBeAutoArranged.OrderByDescending(subject => getMessageExchangeSum(subject)).ToList();
            
            //Debug.WriteLine("After Sorting: ");
            //subjectsToBeAutoArranged.ForEach(subject => Debug.WriteLine(subject.getModelComponentID() + ";"));
            //Debug.WriteLine("");



            Debug.WriteLine("Now there are : " + subjectsToBeAutoArranged.Count() + " - contained to be auto arranged");

            Dictionary<Tuple<ISubject, ISubject>, int> scores = determineClusteringScores(subjectsToBeAutoArranged);

            Debug.WriteLine("scores: " + scores.Count);
            foreach (Tuple<ISubject, ISubject> a in scores.Keys)
            {
                Debug.WriteLine("clustering score for: < " + a.Item1.getModelComponentID() + "," + a.Item2.getModelComponentID() + "> = " + scores[a]);
            }

            List<List<ISubject>> clusters = clusterWithHirachicalClustering(subjectsToBeAutoArranged, scores, threshold: 5); // Adjust the threshold as needed
        }

        static int getMessageExchangeSum(ISubject subject)
        {
            var incomingExchanges = subject.getIncomingMessageExchanges();
            var outgoingExchanges = subject.getOutgoingMessageExchanges();
            return incomingExchanges.Count + outgoingExchanges.Count;
        }

        private static List<List<ISubject>> clusterWithHirachicalClustering(List<ISubject> subjects, Dictionary<Tuple<ISubject, ISubject>, int> scores, int threshold)
        {
            List<List<ISubject>> clusters = subjects.Select(subject => new List<ISubject> { subject }).ToList();

            while (clusters.Count > 1)
            {
                var closestPair = findClosestPair(clusters, scores);
                if (closestPair.Item2 <= threshold)
                {
                    // Merge the two closest clusters
                    var cluster1 = clusters[closestPair.Item1];
                    var cluster2 = clusters[closestPair.Item3];
                    cluster1.AddRange(cluster2);
                    clusters.RemoveAt(closestPair.Item3);
                }
                else {// Stop clustering if no clusters meet the threshold
                    break;
                }
            }

            return clusters;
        }

        // Helper method to find the closest pair of clusters
        private static Tuple<int, int, int> findClosestPair(List<List<ISubject>> clusters, Dictionary<Tuple<ISubject, ISubject>, int> scores)
        {
            int minDistance = int.MaxValue;
            int cluster1Index = -1;
            int cluster2Index = -1;

            for (int i = 0; i < clusters.Count; i++)
            {
                for (int j = i + 1; j < clusters.Count; j++)
                {
                    int distance = calculateClusterDistance(clusters[i], clusters[j], scores);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        cluster1Index = i;
                        cluster2Index = j;
                    }
                }
            }

            return Tuple.Create(cluster1Index, minDistance, cluster2Index);
        }

        // Helper method to calculate the distance between two clusters
        private static int calculateClusterDistance(List<ISubject> cluster1, List<ISubject> cluster2, Dictionary<Tuple<ISubject, ISubject>, int> scores)
        {
            int totalScore = 0;
            int pairCount = 0;

            foreach (var subject1 in cluster1)
            {
                foreach (var subject2 in cluster2)
                {
                    var pair = Tuple.Create(subject1, subject2);
                    if (scores.TryGetValue(pair, out int score))
                    {
                        totalScore += score;
                        pairCount++;
                    }
                }
            }

            return pairCount > 0 ? totalScore / pairCount : int.MaxValue;
        }

        private static Dictionary<Tuple<ISubject, ISubject>, int> determineClusteringScores(List<ISubject> subjectsToBeAutoArranged)
        {
            // Create a dictionary to store scores
            Dictionary<Tuple<ISubject, ISubject>, int> scores = new Dictionary<Tuple<ISubject, ISubject>, int>();

            // Iterate through each pair of ISubjects
            for (int i = 0; i < subjectsToBeAutoArranged.Count; i = i + 1)
            {
                ISubject subjectA = subjectsToBeAutoArranged[i];

                for (int j = i + 1; j < subjectsToBeAutoArranged.Count; j = j + 1)
                {
                    ISubject subjectB = subjectsToBeAutoArranged[j];

                    // Calculate the score based on the number of common connected subjects
                    int score = calculateCommonConnections(subjectA, subjectB);

                    // Create a tuple representing the pair of ISubjects
                    Tuple<ISubject, ISubject> subjectPair = Tuple.Create(subjectA, subjectB);

                    // Add or update the score in the dictionary
                    if (scores.ContainsKey(subjectPair))
                    {
                        scores[subjectPair] = scores[subjectPair] + score;
                    }
                    else
                    {
                        scores[subjectPair] = score;
                    }
                }
            }

            return scores;
        }

        private static int calculateCommonConnections(ISubject subjectA, ISubject subjectB)
        {
            // Get the incoming and outgoing IMessageExchange connections of subjectA and subjectB
            IDictionary<String,IMessageExchange> incomingA = subjectA.getIncomingMessageExchanges();
            IDictionary<String, IMessageExchange> outgoingA = subjectA.getOutgoingMessageExchanges();
            IDictionary<String, IMessageExchange> incomingB = subjectB.getIncomingMessageExchanges();
            IDictionary<String, IMessageExchange> outgoingB = subjectB.getOutgoingMessageExchanges();

            int commonConnections = incomingA.Values.Intersect(outgoingB.Values).Count() +
                                     incomingB.Values.Intersect(outgoingA.Values).Count();

            return commonConnections;
        }

        
        private void determinAndSetSimple2DVisPagebounds(Visio.Page currentPage)
        {
            // Now, calculate the average of getRelative2DWidth and getRelative2DHeight values
            double sumWidth = 0;
            double sumHeight = 0;
            int subjectCount = 0;
            double pageRatio = 1;

            foreach (ISubject modelElement in getElements().Select(x => x.Value).OfType<ISubject>())
            {
                if ((modelElement is IFullySpecifiedSubject || modelElement is IInterfaceSubject) && !(modelElement is ISystemInterfaceSubject) ) //no groups
                {
                    double width = modelElement.getRelative2DWidth();
                    double height = modelElement.getRelative2DHeight();
                    pageRatio = modelElement.get2DPageRatio();

                    
                    if (width > 0)
                    {
                        sumWidth += width;
                        subjectCount++;
                    }

                    if (height > 0)
                    {
                        sumHeight += height;
                    }
                }
            }

            double averageWidth = sumWidth / subjectCount;
            double averageHeight = sumHeight / subjectCount;

            double heightMod = 1 / pageRatio;
            double averageSubjectRatio = averageWidth / (averageHeight * heightMod);

            double expectedDefaultValue = 0.638297872;

            double ratioMod = averageSubjectRatio - expectedDefaultValue; //>0 if wider subjects <0 if 

            double newPageWidth =  (32.00 + (32.00*ratioMod)) / averageWidth + 1;
            double newPageHeight = newPageWidth * heightMod;

            string oldFormula = currentPage.PageSheet.CellsU["PageWidth"].FormulaU;
            string newFormula = $"{newPageWidth.ToString(CultureInfo.InvariantCulture)} mm";

            //string widthFormula = $" in"; // Convert mm to inches (Visio unit)
            //targetPage.PageSheet.CellsU["PageWidth"].FormulaU = widthFormula;

            int id = currentPage.ID;

            currentPage.PageSheet.CellsU["PageWidth"].FormulaU = newFormula;
            currentPage.PageSheet.CellsU["PageHeight"].FormulaU = newPageHeight.ToString(CultureInfo.InvariantCulture) + " mm";

            currentPage.AutoSize = false;
            if (currentPage.PageSheet.CellExistsU["User.OWLIMPORTINFORATIOMOD",0] == 0)
            {
                currentPage.PageSheet.AddNamedRow((short)VisSectionIndices.visSectionUser, "OWLIMPORTINFORATIOMOD", 0);
            }

            currentPage.PageSheet.CellsU["User.OWLIMPORTINFORATIOMOD"].FormulaU = "=" + ratioMod.ToString(CultureInfo.InvariantCulture);
            
            Debug.WriteLine(" New Formula: " + currentPage.PageSheet.CellsU["User.OWLIMPORTINFORATIOMOD"].FormulaU);
            Debug.WriteLine(" New Result: " + currentPage.PageSheet.CellsU["User.OWLIMPORTINFORATIOMOD"].Result[""]);
            Debug.WriteLine(" New ResultSTR: " + currentPage.PageSheet.CellsU["User.OWLIMPORTINFORATIOMOD"].ResultStr[""]);


            //Debug.WriteLine("Changed paged size of page " + currentPage.ID  + " (name: " + currentPage.NameU + ") to width: "
            //               + newFormula + " - height: " + (newPageHeight.ToString(CultureInfo.InvariantCulture)) + " mm");



            //Console.WriteLine("Changed the size of page: " + currentPage.NameU + " - ID: " + currentPage.ID);

            //currentPage.PageSheet.AddNamedRow(243, "EditedBYModelLayer", 0);
            //Call activeDocument.DocumentSheet.addNamedRow(visSectionProp, ALPSConstants.alpsPropertieTypeSimpleSimEnabled, visTagDefault)
            //myShape.addNamedRow(visSectionProp, ALPSConstants.sisiSubjectExecutionCostPerHour, 0)

        }



        public override IParseablePASSProcessModelElement getParsedInstance()
        {
            return new VisioModelLayer();
        }

        
    }
}
