using Microsoft.Office.Interop.Visio;
using System;

namespace VisioAddIn.SiSi
{
    class SiSi_Distribution
    {
        //Values in Fraction of Days (7.0 = 7 Days = 7*24*60 Minutes)
        private Double meanValue;
        private Double standardDeviation;
        private Double maxValue;
        private Double minValue;
        private Boolean wellKnownDuration;

        public SiSi_Distribution()
        {
            meanValue = 0;
            standardDeviation = 0;
            maxValue = 0;
            minValue = 0;
        }

        public void addDistribution(SiSi_Distribution otherDistribution)
        {
            addDistributionWeighted(otherDistribution, 1.0);
        }

        public void addDistributionWeighted(SiSi_Distribution otherDistribution, Double otherDistributionWeight)
        {
            meanValue += (otherDistribution.getMeanValue() * otherDistributionWeight);
            standardDeviation = Math.Sqrt(Math.Pow(standardDeviation, 2) + Math.Pow(otherDistribution.getStandardDeviation() * otherDistributionWeight, 2));
            maxValue += (otherDistribution.getMaxValue() * otherDistributionWeight);
            minValue += (otherDistribution.getMinValue() * otherDistributionWeight);

            if (wellKnownDuration)
            {
                wellKnownDuration = otherDistribution.getWellKnownDuration();
            }
        }

        public void substractDuration(SiSi_Distribution otherDistribution)
        {
            meanValue -= otherDistribution.getMeanValue();
            checkMeanValue();

            minValue -= otherDistribution.getMinValue();
            checkMinValue();

            maxValue -= otherDistribution.getMaxValue();
            checkMaxValue();
        }

        public SiSi_Distribution substractDurationAndGiveResult(SiSi_Distribution otherDistribution)
        {
            SiSi_Distribution result = new SiSi_Distribution();
            result.setMeanValue(meanValue - otherDistribution.getMeanValue());
            checkMeanValue();

            result.setMinValue(minValue - otherDistribution.getMinValue());
            checkMinValue();

            result.setMaxValue(maxValue - otherDistribution.getMaxValue());
            checkMaxValue();

            return result;
        }

        //calls combineDicombineDistributionAndGiveResultWeighted with Weight = 1
        public SiSi_Distribution combineDistributionAndGiveResult(SiSi_Distribution otherDistribution)
        {
            return combineDistributionAndGiveResultWeighted(otherDistribution, 1.0);
        }

        public SiSi_Distribution combineDistributionAndGiveResultWeighted(SiSi_Distribution otherDistribution, Double otherDistributionWeight)
        {
            SiSi_Distribution result = new SiSi_Distribution();
            result.setMeanValue(meanValue + (otherDistribution.getMeanValue() * otherDistributionWeight));
            result.setStandardDeviation(Math.Sqrt(Math.Pow(standardDeviation, 2) + Math.Pow(otherDistribution.getStandardDeviation() * otherDistributionWeight, 2)));
            result.setMinValue(minValue + (otherDistribution.getMinValue() * otherDistributionWeight));
            result.setMaxValue(maxValue + (otherDistribution.getMaxValue() * otherDistributionWeight));
            result.setWellKnownDuration(wellKnownDuration & otherDistribution.getWellKnownDuration());

            return result;
        }

        public void parseStateOrTransition(Shape inputShape)
        {
            if (inputShape.CellExistsU["Prop." + ALPSConstants.simpleSimDurationMeanValue, 1] != 0)
            {
                setMeanValue(ALPSGlobalFunctions.convertFormulaToFractionsOfDay(inputShape.CellsU["Prop." + ALPSConstants.simpleSimDurationMeanValue].ResultStr["none"]));
            }

            if (inputShape.CellExistsU["Prop." + ALPSConstants.simpleSimDurationStandardDeviation, 1] != 0)
            {
                setStandardDeviation(ALPSGlobalFunctions.convertFormulaToFractionsOfDay(inputShape.CellsU["Prop." + ALPSConstants.simpleSimDurationStandardDeviation].ResultStr["none"]));
            }

            if (inputShape.CellExistsU["Prop." + ALPSConstants.simpleSimDurationMaxValue, 1] != 0)
            {
                setMaxValue(ALPSGlobalFunctions.convertFormulaToFractionsOfDay(inputShape.CellsU["Prop." + ALPSConstants.simpleSimDurationMaxValue].ResultStr["none"]));
            }

            if (inputShape.CellExistsU["Prop." + ALPSConstants.simpleSimDurationMinValue, 1] != 0)
            {
                setMinValue(ALPSGlobalFunctions.convertFormulaToFractionsOfDay(inputShape.CellsU["Prop." + ALPSConstants.simpleSimDurationMinValue].ResultStr["none"]));
            }

            if (getMaxValue() <= 0)
            {
                setMaxValue(getMeanValue() + (getStandardDeviation() * SiSi_SimpleSim.getNumberOfSigmasForMinMax()));
            }

            if (getMinValue() <= 0)
            {
                setMinValue(getMeanValue() - (getStandardDeviation() * SiSi_SimpleSim.getNumberOfSigmasForMinMax()));
                checkMinValue();
            }

            setWellKnownDuration(true);
        }

        public string toString(bool withIndividualLines)
        {
            string result = "";


            //If values are not read correctly, the min value will be greater then the max value
            if (minValue > maxValue)
            {
                result = "ERROR - Invalid Times are given";
            }
            else
            {
                result = " - Mean Value: " + ALPSGlobalFunctions.convertFractionOfDayToHourFormat(getMeanValue());

                if (getStandardDeviation() != 0)
                {
                    if (withIndividualLines) { result += System.Environment.NewLine; }
                    result += " - Standard Deviation: " + ALPSGlobalFunctions.convertFractionOfDayToHourFormat(getStandardDeviation());
                }
                /*


                if (getMinValue() >= 0)
                {
                */
                if (withIndividualLines) { result += System.Environment.NewLine; }
                result += " - Minimum time: " + ALPSGlobalFunctions.convertFractionOfDayToHourFormat(getMinValue());
                /*
                }


                if (getMaxValue() >= 0)
                {
                */
                if (withIndividualLines) { result += System.Environment.NewLine; }
                result += " - Maximum time: " + ALPSGlobalFunctions.convertFractionOfDayToHourFormat(getMaxValue());
                //}
            }
            return result;
        }


        public void averageOutWith(SiSi_Distribution otherDuration)
        {
            meanValue = (meanValue + otherDuration.meanValue) / 2;
            standardDeviation = (standardDeviation + otherDuration.standardDeviation) / 2;

            if (otherDuration.maxValue >= maxValue)
            {
                maxValue = otherDuration.maxValue;
            }
            if (otherDuration.minValue <= minValue)
            {
                minValue = otherDuration.minValue;
            }
        }


        public void copyValuesOf(SiSi_Distribution otherDistribution)
        {
            setMeanValue(otherDistribution.getMeanValue());
            setStandardDeviation(otherDistribution.getStandardDeviation());
            setMaxValue(otherDistribution.getMaxValue());
            setMinValue(otherDistribution.getMinValue());
        }

        public SiSi_Distribution getCopy()
        {
            SiSi_Distribution result = new SiSi_Distribution();
            setMeanValue(getMeanValue());
            setStandardDeviation(getStandardDeviation());
            setMaxValue(getMaxValue());
            setMinValue(getMinValue());

            return result;
        }




        //Checker-Methods checks if Value is < 0, if so it corrects it to 0
        public void checkMeanValue() { if (meanValue < 0) { meanValue = 0; } }
        public void checkMaxValue() { if (maxValue < 0) { maxValue = 0; } }
        public void checkMinValue() { if (minValue < 0) { minValue = 0; } }

        //getter-Methods
        public Double getMeanValue() { return meanValue; }
        public Double getStandardDeviation() { return standardDeviation; }
        public Double getMaxValue() { return maxValue; }
        public Double getMinValue() { return minValue; }
        public Boolean getWellKnownDuration() { return wellKnownDuration; }

        //setter-Methoden
        public void setMeanValue(Double newMeanValue) { meanValue = newMeanValue; }
        public void setStandardDeviation(Double newStandardDeviation) { standardDeviation = newStandardDeviation; }
        public void setMaxValue(Double newMaxValue) { maxValue = newMaxValue; }
        public void setMinValue(Double newMinValue) { minValue = newMinValue; }
        public void setWellKnownDuration(Boolean newBool) { wellKnownDuration = newBool; }

    }
}
