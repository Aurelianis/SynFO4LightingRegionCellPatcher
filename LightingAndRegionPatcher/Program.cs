using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Fallout4;
using LightingAndRegionPatcher.Extensions;
using Noggog;

namespace LightingAndRegionPatcher
{
    public class Program
    {
        public static async Task<int> Main(string[] args) => await SynthesisPipeline.Instance
            .AddPatch<IFallout4Mod, IFallout4ModGetter>(RunPatch)
            .SetTypicalOpen(GameRelease.Fallout4, "Synthesis - Cell.esp")
            .Run(args);

        public static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
        {
            Console.WriteLine("CellPatcher - RunPatch - START");

            bool clarityActive = state.LoadOrder.ContainsKey(Clarity.ModKey);
            bool uilActive = state.LoadOrder.ContainsKey(UltraInteriorLighting.ModKey);
            bool regionsActive = state.LoadOrder.ContainsKey(RegionNames.ModKey);
            bool jsrsRegionsActive = state.LoadOrder.ContainsKey(JSRSRegions.ModKey);
            bool dcaRegionsActive = state.LoadOrder.ContainsKey(DiamondCityAmbience.ModKey);
            bool bosStoryActive = state.LoadOrder.ContainsKey(BOSStory.ModKey);

            //If Ultra Interior Lighting is Enabled, forward cell lighting records
            if (uilActive)
            {
                var uil = state.LoadOrder.GetIfEnabled(UltraInteriorLighting.ModKey);
                if (uil.Mod != null)
                {
                    Console.WriteLine($"Processing {UltraInteriorLighting.ModKey}");
                    foreach (var uilContext in uil.Mod.EnumerateMajorRecordContexts<ICell, ICellGetter>(state.LinkCache))
                    {
                        var uilCell = uilContext.Record;
                        Console.WriteLine($"Processing UIL Cell: {uilCell.EditorID}");
                        //Make sure there is lighting data
                        if (uilCell.Lighting != null)
                        {
                            // Get winning record.
                            if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(uilContext.Record.FormKey, out var uilWinner))
                            {
                                var uilWinnerCell = uilWinner.Record;
                                if (uilWinnerCell.Lighting != null)
                                {
                                    // Check if UIL is already the winning record.
                                    if (uilWinnerCell.Lighting == uilCell.Lighting)
                                    {
                                        Console.WriteLine($"Skipping, UIL is already winning.");
                                        continue;
                                    }
                                }

                                //Copy record as override for patching
                                var uilCellOverride = uilWinner.GetOrAddAsOverride(state.PatchMod);

                                //Make sure lighting is not null before continuing
                                if (uilCellOverride.Lighting != null)
                                {
                                    //Set up the mask so that only the records that UIL modifies are copied
                                    var onlyUILMask = new CellLighting.TranslationMask(defaultOn: false)
                                    {
                                        FogNearColor = true,
                                        FogClipDistance = true,
                                        FogFarColor = true,
                                        FogMax = true,
                                        LightFadeBegin = true,
                                        LightFadeEnd = true,
                                        Inherits = true,
                                        ForColorHighNear = true,
                                        ForColorHighFar = true,
                                        FogNearScale = true,
                                        FogFarScale = true,
                                        FogHighNearScale = true,
                                        FogHighFarScale = true
                                    };

                                    //Create CellLighting Object with data from UIL
                                    CellLighting lightingCopy = uilCellOverride.Lighting;

                                    //Deep copy 
                                    lightingCopy.DeepCopyIn(uilCell.Lighting, onlyUILMask);

                                    //Compressed flag is removed, so need to make sure it is added back
                                    //Note Synthesis does not currently support this so commenting out for now
                                    //uilCellOverride.Fallout4MajorRecordFlags = uilCellOverride.Fallout4MajorRecordFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);                             
                                }
                            }
                        }
                    }
                }
            }
            //If Clarity is Enabled, forward cell lighting records
            if (clarityActive)
            {
                var clarity = state.LoadOrder.GetIfEnabled(Clarity.ModKey);
                if (clarity.Mod != null)
                {
                    Console.WriteLine($"Processing {Clarity.ModKey}");
                    foreach (var clarityContext in clarity.Mod.EnumerateMajorRecordContexts<ICell, ICellGetter>(state.LinkCache))
                    {
                        var clarityCell = clarityContext.Record;
                        if (clarityCell.Lighting != null)
                        {
                            Console.WriteLine($"Patching Clarity Cell: {clarityCell.EditorID}");

                            // Get winning record.
                            if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(clarityContext.Record.FormKey, out var clarityWinner))
                            {
                                var clarityWinnerCell = clarityWinner.Record;
                                if (clarityWinnerCell.Lighting != null)
                                {
                                    // Check if clarity is already the winning record.
                                    if (clarityWinnerCell.Lighting == clarityCell.Lighting)
                                    {
                                        Console.WriteLine($"Skipping, Clarity is already winning.");
                                        continue;
                                    }
                                }

                                var clarityCellOverride = clarityWinner.GetOrAddAsOverride(state.PatchMod);
                                if (clarityCellOverride.Lighting != null)
                                {
                                    var onlyClarityMask = new CellLighting.TranslationMask(defaultOn: false)
                                    {
                                        FogNear = true,
                                        FogFar = true,
                                        FogPower = true
                                    };
                                    CellLighting lightingCopy = clarityCellOverride.Lighting;
                                    lightingCopy.DeepCopyIn(clarityCell.Lighting, onlyClarityMask);

                                    //Compressed flag is removed, so need to make sure it is added back
                                    //Note Synthesis does not currently support this so commenting out for now
                                    //clarityCellOverride.Fallout4MajorRecordFlags = clarityCellOverride.Fallout4MajorRecordFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);
                                }

                            }
                        }
                    }
                }
            }
            //If See Region Names on Save Files is Enabled, forward Unique region records
            if (regionsActive)
            {
                var regions = state.LoadOrder.GetIfEnabled(RegionNames.ModKey);
                if (regions.Mod != null)
                {
                    Console.WriteLine($"Processing {RegionNames.ModKey}");
                    foreach (var regionContext in regions.Mod.EnumerateMajorRecordContexts<ICell, ICellGetter>(state.LinkCache))
                    {
                        var regionCell = regionContext.Record;
                        if (regionContext.Record.Regions != null)
                        {
                            var regionRecords = regionContext.Record.Regions;
                            if (regionRecords.Count > 0)
                            {
                                if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(regionContext.Record.FormKey, out var regionWinner))
                                {
                                    var regionWinnerCell = regionWinner.Record;
                                    if (regionWinnerCell.Regions != null)
                                    {
                                        // Check if Regions is already the winning record.
                                        if (regionWinnerCell.Regions == regionCell.Regions)
                                        {
                                            Console.WriteLine($"Skipping, Regions is already winning.");
                                            continue;
                                        }
                                    }
                                    var regionCellOverride = regionWinner.GetOrAddAsOverride(state.PatchMod);
                                    if (regionCellOverride.Regions != null)
                                    {
                                        //Add only region records that are unique so that there are no duplicates
                                        regionCellOverride.Regions.AddRangeIfUnique(regionContext.Record.Regions);

                                        //Compressed flag is removed, so need to make sure it is added back
                                        //Note Synthesis does not currently support this so commenting out for now
                                        // regionCellOverride.MajorFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //If JSRS Regions is Enabled, forward Unique region records
            if (jsrsRegionsActive)
            {
                var jsrsregions = state.LoadOrder.GetIfEnabled(JSRSRegions.ModKey);
                if (jsrsregions.Mod != null)
                {
                    Console.WriteLine($"Processing {JSRSRegions.ModKey}");
                    foreach (var jsrsRegionContext in jsrsregions.Mod.EnumerateMajorRecordContexts<ICell, ICellGetter>(state.LinkCache))
                    {
                        var jsrsCell = jsrsRegionContext.Record;

                        if (jsrsRegionContext.Record.Regions != null)
                        {
                            var jsrsRegionRecords = jsrsRegionContext.Record.Regions;
                            if (jsrsRegionRecords.Count > 0)
                            {
                                if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(jsrsRegionContext.Record.FormKey, out var jsrsRegionWinner))
                                {
                                    var jsrsRegionWinnerCell = jsrsRegionWinner.Record;
                                    if (jsrsRegionWinnerCell.Regions != null)
                                    {
                                        // Check if Regions is already the winning record.
                                        if (jsrsRegionWinnerCell.Regions == jsrsCell.Regions)
                                        {
                                            Console.WriteLine($"Skipping, JSRS Regions is already winning.");
                                            continue;
                                        }
                                    }
                                    var jsrsCellOverride = jsrsRegionWinner.GetOrAddAsOverride(state.PatchMod);
                                    if (jsrsCellOverride.Regions != null)
                                    {
                                        jsrsCellOverride.Regions.AddRangeIfUnique(jsrsRegionContext.Record.Regions);
                                        //Compressed flag is removed, so need to make sure it is added back
                                        //Note Synthesis does not currently support this so commenting out for now
                                        //jsrsCellOverride.Fallout4MajorRecordFlags = jsrsCellOverride.Fallout4MajorRecordFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //If Diamond City Ambiance is Enabled, forward Unique region records
            if (dcaRegionsActive)
            {
                var dca = state.LoadOrder.GetIfEnabled(DiamondCityAmbience.ModKey);
                if (dca.Mod != null)
                {
                    Console.WriteLine($"Processing {DiamondCityAmbience.ModKey}");
                    foreach (var dcaContext in dca.Mod.EnumerateMajorRecordContexts<ICell, ICellGetter>(state.LinkCache))
                    {
                        var dcaCell = dcaContext.Record;
                        if (dcaContext.Record.Regions != null)
                        {
                            var regionRecords = dcaContext.Record.Regions;
                            if (regionRecords.Count > 0)
                            {
                                if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(dcaContext.Record.FormKey, out var regionWinner))
                                {
                                    var dcaWinnerCell = regionWinner.Record;
                                    if (dcaWinnerCell.Regions != null)
                                    {
                                        // Check if Regions is already the winning record.
                                        if (dcaWinnerCell.Regions == dcaCell.Regions)
                                        {
                                            Console.WriteLine($"Skipping, Diamond City Ambiance is already winning.");
                                            continue;
                                        }
                                    }
                                    var dcaCellOverride = regionWinner.GetOrAddAsOverride(state.PatchMod);
                                    if (dcaCellOverride.Regions != null)
                                    {
                                        //Add only region records that are unique so that there are no duplicates
                                        dcaCellOverride.Regions.AddRangeIfUnique(dcaContext.Record.Regions);

                                        //Compressed flag is removed, so need to make sure it is added back
                                        //Note Synthesis does not currently support this so commenting out for now
                                        // dcaCellOverride.MajorFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //If BoS Story is Enabled, forward Lighting & Encounter Zone records
            if (bosStoryActive)
            {
                var bosstory = state.LoadOrder.GetIfEnabled(BOSStory.ModKey);
                if (bosstory.Mod != null)
                {
                    Console.WriteLine($"Processing {BOSStory.ModKey}");
                    foreach (var cellContext in bosstory.Mod.EnumerateMajorRecordContexts<ICell, ICellGetter>(state.LinkCache))
                    {
                        var cell = cellContext.Record;
                        Console.WriteLine($"Processing BoS Story Cell: {cell.EditorID}");
                        //Make sure there is lighting data
                        if (cell.Lighting != null)
                        {
                            // Get winning record.
                            if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(cellContext.Record.FormKey, out var winner))
                            {
                                var winnerCell = winner.Record;
                                if (winnerCell.Lighting != null)
                                {
                                    // Check if UIL is already the winning record.
                                    if (winnerCell.Lighting == cell.Lighting)
                                    {
                                        Console.WriteLine($"Skipping, BoS Story is already winning.");
                                        continue;
                                    }
                                }

                                //Copy record as override for patching
                                var cellOverride = winner.GetOrAddAsOverride(state.PatchMod);

                                //Make sure lighting is not null before continuing
                                if (cellOverride.Lighting != null)
                                {
                                    //Set up the mask so that only the records that UIL modifies are copied
                                    var recordMask = new CellLighting.TranslationMask(defaultOn: false)
                                    {
                                        AmbientColors = true
                                    };

                                    //Create CellLighting Object with data from UIL
                                    CellLighting lightingCopy = cellOverride.Lighting;

                                    //Deep copy 
                                    lightingCopy.DeepCopyIn(cell.Lighting, recordMask);

                                    //Compressed flag is removed, so need to make sure it is added back
                                    //Note Synthesis does not currently support this so commenting out for now
                                    //uilCellOverride.Fallout4MajorRecordFlags = uilCellOverride.Fallout4MajorRecordFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);                             
                                }
                            }
                        }

                        //Make sure there is Encounter Zone Data
                        if (cell.EncounterZone != null)
                        {
                            // Get winning record.
                            if (state.LinkCache.TryResolveContext<ICell, ICellGetter>(cellContext.Record.FormKey, out var winner))
                            {
                                var winnerCell = winner.Record;
                                if (winnerCell.EncounterZone != null)
                                {
                                    // Check if UIL is already the winning record.
                                    if (winnerCell.EncounterZone == cell.EncounterZone)
                                    {
                                        Console.WriteLine($"Skipping, BoS Story is already winning.");
                                        continue;
                                    }
                                }

                                //Copy record as override for patching
                                var cellOverride = winner.GetOrAddAsOverride(state.PatchMod);

                                //Make sure lighting is not null before continuing

                                if (cellOverride.EncounterZone != null && cellOverride.EditorID != "GeneralAtomicsFactory01")
                                {
                                    cellOverride.EncounterZone.SetTo(cellContext.Record.EncounterZone);
                                    //Compressed flag is removed, so need to make sure it is added back
                                    //Note Synthesis does not currently support this so commenting out for now
                                    //cellOverride.Fallout4MajorRecordFlags = cellOverride.Fallout4MajorRecordFlags.SetFlag(Cell.Fallout4MajorRecordFlag.Compressed, true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
