﻿using DAQ.Environment;
using DAQ.HAL;
using DAQ.TransferCavityLock2012;
using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TransferCavityLock2012
{
    /// <summary>
    /// A base class to represent commonalities of Slave and Master lasers.
    /// </summary>
    public abstract class Laser
    {
        public double IntegralGain { get; set; }
        public double ProportionalGain { get; set; }
        public string Name;
        public string FeedbackChannel;
        public LorentzianFit Fit { get; set; }
        public double[] LatestScanData { get; set; }
        public String PhotoDiodeChannel;
        public Cavity ParentCavity;
        private TransferCavityLock2012LaserControllable laser;
        protected bool lockBlocked;
        public double PeakRampPosition{ get; set; }
        public string RampVoltageChannel;

        public enum LaserState
        {
            FREE, LOCKING, LOCKED
        };
        public enum PeakFindingMode
        {
            DIGITAL, ANALOG
        };

        public LaserState lState = LaserState.FREE;
        public PeakFindingMode lPeakFindingMode = PeakFindingMode.DIGITAL;

        public virtual double LaserSetPoint { get; set; }
        public abstract double VoltageError { get; }
        public abstract double VoltageErrorDifferenceFromLast { get; }

        public double UpperVoltageLimit
        {
            get
            {
                return ((AnalogOutputChannel)Environs.Hardware.AnalogOutputChannels[FeedbackChannel]).RangeHigh;
            }
        }

        public double LowerVoltageLimit
        {
            get
            {
                return ((AnalogOutputChannel)Environs.Hardware.AnalogOutputChannels[FeedbackChannel]).RangeLow;
            }
        }

        private double currentVoltage;
        public double CurrentVoltage
        {
            get
            {
                return currentVoltage;
            }

            set
            {
                if (value < LowerVoltageLimit) // Want to make sure we don't try to send voltage that is too high or low so TCL doesn't crash
                {
                    currentVoltage = LowerVoltageLimit;
                }
                else if (value > UpperVoltageLimit)
                {
                    currentVoltage = UpperVoltageLimit;
                }
                else
                {
                    currentVoltage = value;
                }
                laser.SetLaserVoltage(currentVoltage);
            }
        }

        public Laser(string feedbackChannel, string photoDiode, Cavity cavity)
        {
            laser = new DAQMxTCL2012LaserControlHelper(feedbackChannel);
            lState = LaserState.FREE;
            lPeakFindingMode = PeakFindingMode.DIGITAL;
            Name = feedbackChannel;
            FeedbackChannel = feedbackChannel;
            PhotoDiodeChannel = photoDiode;
            ParentCavity = cavity;
            laser.ConfigureSetLaserVoltage(0.0);
        }

        public void ArmLock()
        {
            lState = LaserState.LOCKING;
        }

        protected virtual void Lock()
        {
            lState = LaserState.LOCKED;
        }

        public void DisengageLock()
        {
            lState = LaserState.FREE;
        }

        public bool IsLocked
        {
            get { return lState == LaserState.LOCKED; }
        }

        public virtual void UpdateScan(double[] rampData, double[] scanData, bool shouldBlock)
        {
            LatestScanData = scanData;
            lockBlocked = shouldBlock;
            LorentzianFit newFit = new LorentzianFit(0, 0, 0, 0);
            if (!lockBlocked)
            {
                switch (lState)
                {

                    case LaserState.LOCKED:
                        newFit = FitWithPreviousAsBestGuess(rampData, scanData);
                        double dataPeakCentre = rampData[Array.IndexOf(scanData, scanData.Max())];
                        bool fitTooNarrow = newFit.Width < 0.001; // Sometimes fit seems to break and give a tiny width
                        bool fitTooFarFromMax = Math.Abs(newFit.Centre - dataPeakCentre)/newFit.Width > 1;
                        if (fitTooNarrow || fitTooFarFromMax) 
                        {
                            newFit = FitUsingDataForBestGuess(rampData, scanData);
                        }
                        Fit = newFit;
                        break;

                    case LaserState.LOCKING:
                        Fit = FitUsingDataForBestGuess(rampData, scanData);
                        Lock();
                        break;

                    case LaserState.FREE:
                        break;
                }
            }
        }

        public virtual void UpdateScanFast(double[] rampData, double[] scanData, bool shouldBlock, double pointsToConsiderEitherSideOfPeakInFWHMs, int maximumNLMFSteps)
        {
            LatestScanData = scanData;
            lockBlocked = shouldBlock;
            LorentzianFit newFit = new LorentzianFit(0, 0, 0, 0);
            if (!lockBlocked)
            {
                switch (lState)
                {

                    case LaserState.LOCKED:
                        newFit = FitWithPreviousAsBestGuess(rampData, scanData, pointsToConsiderEitherSideOfPeakInFWHMs, maximumNLMFSteps);
                        double dataPeakCentre = rampData[Array.IndexOf(scanData, scanData.Max())];
                        bool fitTooNarrow = newFit.Width < 0.001; // Sometimes fit seems to break and give a tiny width
                        bool fitTooFarFromMax = Math.Abs(newFit.Centre - dataPeakCentre) / newFit.Width > 1;
                        if (fitTooNarrow || fitTooFarFromMax)
                        {
                            newFit = FitUsingDataForBestGuess(rampData, scanData);
                        }
                        Fit = newFit;
                        break;

                    case LaserState.LOCKING:
                        Fit = FitUsingDataForBestGuess(rampData, scanData);
                        Lock();
                        break;

                    case LaserState.FREE:
                        break;
                }
            }
        }

        protected LorentzianFit FitWithPreviousAsBestGuess(double[] rampData, double[] scanData)
        {
            return CavityScanFitHelper.FitLorentzianToData(rampData, scanData, Fit);
        }

        protected LorentzianFit FitWithPreviousAsBestGuess(double[] rampData, double[] scanData, double pointsToConsiderEitherSideOfPeakInFWHMs, int maximumNLMFSteps)
        {
            return CavityScanFitHelper.FitLorentzianToData(rampData, scanData, Fit, pointsToConsiderEitherSideOfPeakInFWHMs, maximumNLMFSteps);
        }

        protected LorentzianFit FitUsingDataForBestGuess(double[] rampData, double[] scanData)
        {
            double background = scanData.Min();
            double maximum = scanData.Max();
            double amplitude = maximum - background;
            double centre = rampData[Array.IndexOf(scanData, maximum)];
            double width = (rampData.Max() - rampData.Min()) / 20;
            LorentzianFit bestGuessFit = new LorentzianFit(background, amplitude, centre, width);
            return CavityScanFitHelper.FitLorentzianToData(rampData, scanData, bestGuessFit);
        }

        public virtual void UpdateLock()
        {
            if (lState == LaserState.LOCKED)
            {
                CurrentVoltage = CurrentVoltage + IntegralGain * VoltageError + ProportionalGain * VoltageErrorDifferenceFromLast;
            }
        }

        public void DisposeLaserControl()
        {
            laser.DisposeLaserTask();
        }
    }
}
