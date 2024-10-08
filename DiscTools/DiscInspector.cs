﻿using DiscTools.Inspection;
using DiscTools.ISO;
using DiscTools.Objects;

namespace DiscTools
{
    public class DiscInspector
    {
        public string CuePath { get; set; }
        public DiscData Data { get; set; }
        public DiscStructure DiscStructure { get; set; }
        public DetectedDiscType DetectedDiscType { get; set; }
        public string DiscTypeString { get; set; }
        public string DiscViewString { get; set; }

        /// <summary>
        /// Return a DiscInspector Object
        /// IntensiveScan will return more matches but may take longer
        /// </summary>
        /// <param name="cuePath"></param>
        /// <param name="intensiveScan"></param>
        public static DiscInspector ScanDisc(string cuePath, bool intensiveScan)
        {
            var inter = new Interrogator(cuePath, intensiveScan);
            var res = inter.Start();

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, intensiveScan);

            return res;
        }

        public static DiscInspector ScanDiscNoCorrection(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start();

            return res;
        }

        public static DiscInspector ScanDiscQuickNoCorrection(string cuePath)
        {
            var inter = new Interrogator(cuePath, false);
            var res = inter.Start();

            return res;
        }

        /// <summary>
        /// Return a DiscInspector Object - quick scan that may miss detection on some non-iso based images
        /// </summary>
        /// <param name="cuePath"></param>
        public static DiscInspector ScanDiscQuick(string cuePath)
        {
            return ScanDisc(cuePath, false);
        }

        /// <summary>
        /// Return a DiscInspector Object - Intensive scan that has more chance of detection (but may take longer)
        /// </summary>
        /// <param name="cuePath"></param>
        public static DiscInspector ScanDisc(string cuePath)
        {
            return ScanDisc(cuePath, true);
        }

        /* Specific system targetted scans */

        public static DiscInspector ScanPSX(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.SonyPSX);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanPS2(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.SonyPSX);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanPSP(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.SonyPSP);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanSaturn(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.SegaSaturn);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanPCECD(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.PCEngineCD);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanPCFX(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.PCFX);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanSegaCD(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.SegaCD);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanCDi(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.PhilipsCDi);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanNeoGeoCD(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.NeoGeoCD);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanDreamcast(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.DreamCast);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector Scan3DO(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.Panasonic3DO);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanAmigaCDTV(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.AmigaCDTV);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanAmigaCD32(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.AmigaCD32);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanPlaydia(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.BandaiPlaydia);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanGamecube(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.Gamecube);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanWii(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.Wii);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }

        public static DiscInspector ScanTowns(string cuePath)
        {
            var inter = new Interrogator(cuePath, true);
            var res = inter.Start(DetectedDiscType.FMTowns);

            // run the cue routine
            res = CueHandler.CueRoutine(res, cuePath, true);

            return res;
        }
    }

    public enum DetectedDiscType
    {
        SonyPSX,
        SonyPSP,
        SegaSaturn,
        PCEngineCD,
        PCFX,
        SegaCD,
        PhilipsCDi,
        AudioCD,
        NeoGeoCD,
        DreamCast,
        UnknownCDFS,
        UnknownFormat,
        Panasonic3DO,
        AmigaCDTV,
        AmigaCD32,
        BandaiPlaydia,
        Gamecube,
        Wii,
        SonyPS2,
        FMTowns
    }
}