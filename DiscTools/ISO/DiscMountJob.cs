using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DiscTools.ISO.Internal.Jobs;
using DiscTools.ISO.Internal;
using DiscTools.ISO.DiscFormats.CUE;
using DiscTools.ISO.DiscFormats;

namespace DiscTools.ISO
{
    /// <summary>
	/// A Job interface for mounting discs.
	/// This is publicly exposed because it's the main point of control for fine-tuning disc loading options.
	/// This would typically be used to load discs.
	/// </summary>
	public partial class DiscMountJob : DiscJob
    {
        /// <summary>
        /// The filename to be loaded
        /// </summary>
        public string? InFromPath;

        /// <summary>
        /// Slow-loading cues won't finish loading if this threshold is exceeded.
        /// Set to 10 to always load a cue
        /// </summary>
        private const int InSlowLoadAbortThreshold = 10;

        /// <summary>
        /// Cryptic policies to be used when mounting the disc.
        /// </summary>
        private readonly DiscMountPolicy _inDiscMountPolicy = new DiscMountPolicy();

        /// <summary>
        /// The interface to be used for loading the disc.
        /// Usually you'll want DiscInterface.BizHawk, but others can be used for A/B testing
        /// </summary>
        private DiscInterface _inDiscInterface = DiscInterface.BizHawk;

        /// <summary>
        /// The resulting disc
        /// </summary>
        public Disc? OutDisc;

        /// <summary>
        /// Whether a mount operation was aborted due to being too slow
        /// </summary>
        public bool OutSlowLoadAborted;

        public void Run()
        {
            switch (_inDiscInterface)
            {
                case DiscInterface.LibMirage:
                    throw new NotSupportedException("LibMirage not supported yet");
                case DiscInterface.BizHawk:
                    RunBizHawk();
                    break;
                case DiscInterface.MednaDisc:
                    RunMednaDisc();
                    break;
            }

            if (OutDisc != null)
            {
                OutDisc.Name = Path.GetFileName(InFromPath);

                //generate toc and structure:
                //1. TOCRaw from RawTOCEntries
                var tocSynth = new Synthesize_DiscTOC_From_RawTOCEntries_Job() { Entries = OutDisc.RawTOCEntries };
                tocSynth.Run();
                OutDisc.TOC = tocSynth.Result;
                //2. Structure from TOCRaw
                var structureSynth = new Synthesize_DiscStructure_From_DiscTOC_Job() { IN_Disc = OutDisc, TOCRaw = OutDisc.TOC };
                structureSynth.Run();
                OutDisc.Structure = structureSynth.Result;

                //insert a synth provider to take care of the leadout track
                //currently, we let mednafen take care of its own leadout track (we'll make that controllable later)
                if (_inDiscInterface != DiscInterface.MednaDisc)
                {
                    var ss_leadout = new SS_Leadout()
                    {
                        SessionNumber = 1,
                        Policy = _inDiscMountPolicy
                    };
                    var condition = (int lba) => lba >= OutDisc.Session1.LeadoutLBA;
                    new ConditionalSectorSynthProvider().Install(OutDisc, condition, ss_leadout);
                }

                //apply SBI if it exists
                /*
                var sbiPath = Path.ChangeExtension(IN_FromPath, ".sbi");
                if (File.Exists(sbiPath) && SBI.SBIFormat.QuickCheckISSBI(sbiPath))
                {
                    var loadSbiJob = new SBI.LoadSBIJob() { IN_Path = sbiPath };
                    loadSbiJob.Run();
                    var applySbiJob = new ApplySBIJob();
                    applySbiJob.Run(OUT_Disc, loadSbiJob.OUT_Data, IN_DiscMountPolicy.SBI_As_Mednafen);
                }
                */
            }

            FinishLog();
        }

        void RunBizHawk()
        {
            var infile = InFromPath;
            string cue_content = null;

            var cfr = new CueFileResolver();

        RERUN:
            var ext = Path.GetExtension(infile).ToLowerInvariant();

            if (ext == ".iso")
            {
                //make a fake cue file to represent this iso file and rerun it as a cue
                var filebase = Path.GetFileName(infile);
                cue_content = string.Format(@"
						FILE ""{0}"" BINARY
							TRACK 01 MODE1/2048
								INDEX 01 00:00:00",
                    filebase);
                infile = Path.ChangeExtension(infile, ".cue");
                goto RERUN;
            }
            if (ext == ".cue")
            {
                //TODO - major renovation of error handling needed

                //TODO - make sure code is designed so no matter what happens, a disc is disposed in case of errors.
                //perhaps the CUE_Format2 (once renamed to something like Context) can handle that
                var cuePath = InFromPath;
                var cueContext = new CUE_Context();
                cueContext.DiscMountPolicy = _inDiscMountPolicy;

                cueContext.Resolver = cfr;
                if (!cfr.IsHardcodedResolve) cfr.SetBaseDirectory(Path.GetDirectoryName(infile));

                //parse the cue file
                var parseJob = new ParseCueJob();
                if (cue_content == null)
                    cue_content = File.ReadAllText(cuePath);
                parseJob.IN_CueString = cue_content;
                var okParse = true;
                try { parseJob.Run(parseJob); }
                catch (DiscJobAbortException) { okParse = false; parseJob.FinishLog(); }
                if (!string.IsNullOrEmpty(parseJob.OUT_Log)) Console.WriteLine(parseJob.OUT_Log);
                ConcatenateJobLog(parseJob);
                if (!okParse)
                    goto DONE;

                //compile the cue file:
                //includes this work: resolve required bin files and find out what it's gonna take to load the cue
                var compileJob = new CompileCueJob();
                compileJob.IN_CueContext = cueContext;
                compileJob.IN_CueFile = parseJob.OUT_CueFile;
                var okCompile = true;
                try { compileJob.Run(); }
                catch (DiscJobAbortException) { okCompile = false; compileJob.FinishLog(); }
                if (!string.IsNullOrEmpty(compileJob.OUT_Log)) Console.WriteLine(compileJob.OUT_Log);
                ConcatenateJobLog(compileJob);
                if (!okCompile || compileJob.OUT_ErrorLevel)
                    goto DONE;

                //check slow loading threshold
                if (compileJob.OUT_LoadTime > InSlowLoadAbortThreshold)
                {
                    Warn("Loading terminated due to slow load threshold");
                    OutSlowLoadAborted = true;
                    goto DONE;
                }

                //actually load it all up
                var loadJob = new LoadCueJob();
                loadJob.IN_CompileJob = compileJob;
                loadJob.Run();
                //TODO - need better handling of log output
                if (!string.IsNullOrEmpty(loadJob.OUT_Log)) Console.WriteLine(loadJob.OUT_Log);
                ConcatenateJobLog(loadJob);

                OutDisc = loadJob.OUT_Disc;
                //OUT_Disc.DiscMountPolicy = IN_DiscMountPolicy; //NOT SURE WE NEED THIS (only makes sense for cue probably)
            }
            else if (ext == ".ccd")
            {
                var ccdLoader = new CCD_Format();
                OutDisc = ccdLoader.LoadCCDToDisc(InFromPath, _inDiscMountPolicy);
            }


        DONE:

            //setup the lowest level synth provider
            if (OutDisc != null)
            {
                var sssp = new ArraySectorSynthProvider()
                {
                    Sectors = OutDisc._Sectors,
                    FirstLBA = -150
                };
                OutDisc.SynthProvider = sssp;
            }
        }
    }
}
