using MOTMaster;
using MOTMaster.SnippetLibrary;

using System;
using System.Collections.Generic;
using System.Collections;

using DAQ.Pattern;
using DAQ.Analog;

// This script is supposed to be the basic script for loading a molecule MOT.
// Note that times are all in units of the clock periods of the two pattern generator boards (at present, both are 10us).
// All times are relative to the Q switch, though note that this is not the first event in the pattern.
public class Patterns : MOTMasterScript
{
    public Patterns()
    {
        Parameters = new Dictionary<string, object>();
        Parameters["PatternLength"] = 50000;
        Parameters["TCLBlockStart"] = 2000; // This is a time before the Q switch
        Parameters["TCLBlockDuration"] = 8000;
        Parameters["FlashToQ"] = 16; // This is a time before the Q switch
        Parameters["QSwitchPulseDuration"] = 10;
        Parameters["FlashPulseDuration"] = 10;
        Parameters["HeliumShutterToQ"] = 100;
        Parameters["HeliumShutterDuration"] = 1550;

        Parameters["MOTSwitchOffTime"] = 6300;
        Parameters["ExpansionTime"] = 1;
        Parameters["MolassesDelay"] = 600;// 100;
        Parameters["MolassesHoldTime"] = 1;// 600;
        Parameters["MolassesRampDuration"] = 1;

        // Camera
        Parameters["Frame0TriggerDuration"] = 10;
        Parameters["WaitBeforeImage"] = 500;

        //PMT
        Parameters["PMTTrigger"] = 4000;
        Parameters["PMTTriggerDuration"] = 10;

        // Slowing
        Parameters["slowingAOMOnStart"] = 250;
        Parameters["slowingAOMOnDuration"] = 45000;
        Parameters["slowingAOMOffStart"] = 1500;
        Parameters["slowingAOMOffDuration"] = 35000;
        Parameters["slowingRepumpAOMOnStart"] = 250;
        Parameters["slowingRepumpAOMOnDuration"] = 45000;
        Parameters["slowingRepumpAOMOffStart"] = 1700;
        Parameters["slowingRepumpAOMOffDuration"] = 35000;

        // Slowing Chirp
        Parameters["SlowingChirpStartTime"] = 340;
        Parameters["SlowingChirpDuration"] = 1160;
        Parameters["SlowingChirpStartValue"] = 0.0;
        Parameters["SlowingChirpEndValue"] = -1.25;//-1.3

        // Slowing field
        Parameters["slowingCoilsValue"] = 5.0; //1.1;
        Parameters["slowingCoilsOffTime"] = 1500;

        // B Field
        Parameters["MOTCoilsSwitchOn"] = 0;
        Parameters["MOTCoilsCurrentRampStartValue"] = 0.65; // 0.65;
        Parameters["MOTCoilsCurrentRampStartTime"] = 4000;
        Parameters["MOTCoilsCurrentRampEndValue"] = 0.65;//1.5
        Parameters["MOTCoilsCurrentRampDuration"] = 1000;
        Parameters["MOTCoilsCurrentMolassesValue"] = 0.0; //0.06
        Parameters["CoilsSwitchOffTime"] = 20000;

        // Shim fields
        Parameters["xShimLoadCurrent"] = 3.6;// 2.7;
        Parameters["yShimLoadCurrent"] = -0.12;// -0.12;
        Parameters["zShimLoadCurrent"] = -5.35;// -5.35; 

        // v0 Light Intensity
        Parameters["v0IntensityRampStartTime"] = 4000;
        Parameters["v0IntensityRampDuration"] = 2000;
        Parameters["v0IntensityRampStartValue"] = 5.8;
        Parameters["v0IntensityRampEndValue"] = 9.11;// 8.465;
        Parameters["v0IntensityMolassesValue"] = 8.5;

        // v0 Light Frequency
        Parameters["v0FrequencyStartValue"] = 0.0; //set this to 0.0 for 114.1MHz 
        Parameters["v0FrequencyNewValue"] = 30.0; //set this to MHz detuning desired if doing frequency jump (positive for blue detuning)

        //v0aomCalibrationValues
        Parameters["lockAomFrequency"] = 114.1;
        Parameters["calibOffset"] = 64.2129;
        Parameters["calibGradient"] = 5.55075;

        // v0 F=1 (dodgy code using an analogue output to control a TTL)
        Parameters["v0F1AOMStartValue"] = 5.0;
        Parameters["v0F1AOMOffValue"] = 0.0;
    }

    public override PatternBuilder32 GetDigitalPattern()
    {
        PatternBuilder32 p = new PatternBuilder32();
        int patternStartBeforeQ = (int)Parameters["TCLBlockStart"];
        int releaseTime = (int)Parameters["MOTSwitchOffTime"];
        int cameraTriggerTime = releaseTime + (int)Parameters["MolassesHoldTime"]+ (int)Parameters["ExpansionTime"] ;

        MOTMasterScriptSnippet lm = new LoadMoleculeMOT(p, Parameters);  // This is how you load "preset" patterns. 

        p.Pulse(patternStartBeforeQ, releaseTime + (int)Parameters["MolassesHoldTime"], cameraTriggerTime - releaseTime - (int)Parameters["MolassesHoldTime"], "v00MOTAOM"); //pulse off the MOT light to release the cloud
        // p.Pulse(patternStartBeforeQ, 4000, (int)Parameters["Frame0TriggerDuration"], "cameraTrigger"); //camera trigger for first frame
        p.Pulse(patternStartBeforeQ, cameraTriggerTime, (int)Parameters["Frame0TriggerDuration"], "cameraTrigger"); //camera trigger for first frame

        return p;
    }

    public override AnalogPatternBuilder GetAnalogPattern()
    {
        AnalogPatternBuilder p = new AnalogPatternBuilder((int)Parameters["PatternLength"]);

        MOTMasterScriptSnippet lm = new LoadMoleculeMOT(p, Parameters);

        int releaseTime = (int)Parameters["MOTSwitchOffTime"];
        int cameraTriggerTime = releaseTime + (int)Parameters["MolassesHoldTime"] + (int)Parameters["ExpansionTime"];

        // Add Analog Channels
        p.AddChannel("v00Intensity");
        p.AddChannel("v00Frequency");
        p.AddChannel("xShimCoilCurrent");
        p.AddChannel("yShimCoilCurrent");
        p.AddChannel("zShimCoilCurrent");
        p.AddChannel("v00EOMAmp");


        // B Field
        p.AddAnalogValue("MOTCoilsCurrent", (int)Parameters["MOTCoilsSwitchOn"], (double)Parameters["MOTCoilsCurrentRampStartValue"]);
        //p.AddLinearRamp("MOTCoilsCurrent", (int)Parameters["MOTCoilsCurrentRampStartTime"], (int)Parameters["MOTCoilsCurrentRampDuration"], (double)Parameters["MOTCoilsCurrentRampEndValue"]);
        p.AddAnalogValue("MOTCoilsCurrent", releaseTime+2, 0.0);

        // Shim Fields
        p.AddAnalogValue("xShimCoilCurrent", 0, (double)Parameters["xShimLoadCurrent"]);
        p.AddAnalogValue("yShimCoilCurrent", 0, (double)Parameters["yShimLoadCurrent"]);
        p.AddAnalogValue("zShimCoilCurrent", 0, (double)Parameters["zShimLoadCurrent"]);

        // v0 Intensity Ramp
        p.AddAnalogValue("v00Intensity", 0, (double)Parameters["v0IntensityRampStartValue"]);
        p.AddLinearRamp("v00Intensity", (int)Parameters["v0IntensityRampStartTime"], (int)Parameters["v0IntensityRampDuration"], (double)Parameters["v0IntensityRampEndValue"]);

        p.AddAnalogValue("v00Intensity", (int)Parameters["MOTSwitchOffTime"] + 100, (double)Parameters["v0IntensityMolassesValue"]);

        // F=0
        p.AddAnalogValue("v00EOMAmp", 0, 5.2);

        // v0 Frequency Ramp
        p.AddAnalogValue("v00Frequency", 0, ((double)Parameters["lockAomFrequency"] - (double)Parameters["v0FrequencyStartValue"] / 2 - (double)Parameters["calibOffset"]) / (double)Parameters["calibGradient"]);
        p.AddAnalogValue("v00Frequency", (int)Parameters["MOTSwitchOffTime"] + 100, ((double)Parameters["lockAomFrequency"] - (double)Parameters["v0FrequencyNewValue"] / 2 - (double)Parameters["calibOffset"]) / (double)Parameters["calibGradient"]);//jump to blue detuning
        p.AddAnalogValue("v00Frequency", (int)Parameters["MOTSwitchOffTime"] + (int)Parameters["MolassesHoldTime"] + (int)Parameters["ExpansionTime"], ((double)Parameters["lockAomFrequency"] - (double)Parameters["v0FrequencyStartValue"] / 2 - (double)Parameters["calibOffset"]) / (double)Parameters["calibGradient"]); //jump aom frequency back to normal for imaging 

        return p;
    }

}
