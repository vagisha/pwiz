﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.PeakFinding;
using pwiz.Skyline.Model.Databinding.Entities;
using pwiz.Skyline.Model.DocSettings;
using pwiz.Skyline.Model.Results.Scoring;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{
    public class OnDemandFeatureCalculator
    {
        public delegate IList<ChromatogramGroupInfo> ChromatogramLoader(int replicateIndex, TransitionGroupDocNode transitionGroupDocNode);
        private Dictionary<TransitionGroup, ChromatogramGroupInfo> _chromatogramGroupInfos =
            new Dictionary<TransitionGroup, ChromatogramGroupInfo>(new IdentityEqualityComparer<TransitionGroup>());

        private ChromatogramLoader _chromatogramLoader;

        private ScoreQValueMap _scoreQValueMap;

        public OnDemandFeatureCalculator(FeatureCalculators calculators, SrmSettings settings,
            PeptideDocNode peptideDocNode, int replicateIndex, ChromFileInfo chromFileInfo,
            ChromatogramLoader chromatogramLoader)
        {
            Calculators = calculators;
            Settings = settings;
            PeptideDocNode = peptideDocNode;
            ChromFileInfo = chromFileInfo;
            ReplicateIndex = replicateIndex;
            _scoreQValueMap = settings.PeptideSettings.Integration.ScoreQValueMap;
            _chromatogramLoader = chromatogramLoader;
        }

        public FeatureCalculators Calculators { get; }
        public SrmSettings Settings { get; }
        public PeptideDocNode PeptideDocNode { get; }
        public int ReplicateIndex { get; }
        public ChromFileInfo ChromFileInfo { get; }
        public ChromatogramSet ChromatogramSet
        {
            get { return Settings.MeasuredResults.Chromatograms[ReplicateIndex]; }
        }

        public float MzMatchTolerance
        {
            get
            {
                return (float) Settings.TransitionSettings.Instrument.MzMatchTolerance;
            }
        }

        public CandidatePeakGroupData GetChosenPeakGroupData(TransitionGroup transitionGroup)
        {
            foreach (var groupScores in GetChosenPeakGroupDataForAllComparableGroups())
            {
                if (groupScores.Item1.Any(tg => ReferenceEquals(tg.TransitionGroup, transitionGroup)))
                {
                    return groupScores.Item2;
                }
            }

            return null;
        }

        public List<Tuple<ImmutableList<TransitionGroupDocNode>, CandidatePeakGroupData>>
            GetChosenPeakGroupDataForAllComparableGroups()
        {
            var list = new List<Tuple<ImmutableList<TransitionGroupDocNode>, CandidatePeakGroupData>>();
            var peptideChromDataSets = MakePeptideChromDataSets();
            peptideChromDataSets.PickChromatogramPeaks(GetTransitionPeakBounds);
            foreach (var comparableSet in peptideChromDataSets.ComparableDataSets.Select(ImmutableList.ValueOf))
            {
                var groupNodes = ImmutableList.ValueOf(comparableSet.Select(dataSet => dataSet.NodeGroup));
                if (groupNodes.Contains(null))
                {
                    continue;
                }

                var scores = CalculateScoresForComparableGroup(peptideChromDataSets, comparableSet).FirstOrDefault();
                if (scores == null)
                {
                    continue;
                }

                double minStartTime = double.MaxValue;
                double maxEndTime = double.MinValue;
                foreach (var groupNode in groupNodes)
                {
                    var transitionGroupChromInfo = groupNode.GetSafeChromInfo(ReplicateIndex)
                        .FirstOrDefault(chromInfo =>
                            0 == chromInfo.OptimizationStep && ReferenceEquals(chromInfo.FileId, ChromFileInfo.FileId));
                    if (transitionGroupChromInfo?.StartRetentionTime != null)
                    {
                        minStartTime = Math.Min(minStartTime, transitionGroupChromInfo.StartRetentionTime.Value);
                    }

                    if (transitionGroupChromInfo?.EndRetentionTime != null)
                    {
                        maxEndTime = Math.Max(maxEndTime, transitionGroupChromInfo.EndRetentionTime.Value);
                    }
                }

                var candidatePeakData =
                    new CandidatePeakGroupData(null, minStartTime, maxEndTime, true, MakePeakScore(scores));
                list.Add(Tuple.Create(groupNodes, candidatePeakData));
            }

            return list;
        }

        internal IEnumerable<FeatureValues> CalculateScoresForComparableGroup(PeptideChromDataSets peptideChromDataSets,
            IList<ChromDataSet> comparableSet)
        {
            var transitionGroups = comparableSet.Select(dataSet => dataSet.NodeGroup).ToList();
            var chromatogramGroupInfos = peptideChromDataSets.MakeChromatogramGroupInfos(comparableSet).ToList();
            if (chromatogramGroupInfos.Count == 0)
            {
                return Array.Empty<FeatureValues>();
            }
            return CalculateChromatogramGroupScores(transitionGroups, chromatogramGroupInfos);
        }

        public IEnumerable<CandidatePeakGroupData> GetCandidatePeakGroups(TransitionGroup transitionGroup)
        {
            var transitionGroupDocNode = (TransitionGroupDocNode) PeptideDocNode.FindNode(transitionGroup);
            var chromatogramGroupInfo = GetChromatogramGroupInfo(transitionGroup);
            if (transitionGroupDocNode == null || chromatogramGroupInfo == null)
            {
                return Array.Empty<CandidatePeakGroupData>();
            }

            return CalculateCandidatePeakScores(transitionGroupDocNode, chromatogramGroupInfo);
        }

        public IList<CandidatePeakGroupData> CalculateCandidatePeakScores(TransitionGroupDocNode transitionGroup, ChromatogramGroupInfo chromatogramGroupInfo)
        {
            var transitionGroups = new List<TransitionGroupDocNode> {transitionGroup};
            var chromatogramGroupInfos = new List<ChromatogramGroupInfo> {chromatogramGroupInfo};
            foreach (var otherTransitionGroup in PeptideDocNode.TransitionGroups)
            {
                if (ReferenceEquals(otherTransitionGroup.TransitionGroup, transitionGroup.TransitionGroup))
                {
                    continue;
                }

                if (transitionGroup.RelativeRT == RelativeRT.Unknown)
                {
                    if (!Equals(otherTransitionGroup.LabelType, transitionGroup.LabelType))
                    {
                        continue;
                    }
                }
                else
                {
                    if (otherTransitionGroup.RelativeRT == RelativeRT.Unknown)
                    {
                        continue;
                    }
                }

                var otherChromatogramGroupInfo = Settings.LoadChromatogramGroup(
                    ChromatogramSet, ChromFileInfo.FilePath, PeptideDocNode, otherTransitionGroup);
                if (otherChromatogramGroupInfo != null)
                {
                    transitionGroups.Add(otherTransitionGroup);
                    chromatogramGroupInfos.Add(otherChromatogramGroupInfo);
                }
            }

            return MakePeakGroupData(transitionGroups, chromatogramGroupInfos, CalculateChromatogramGroupScores(transitionGroups, chromatogramGroupInfos).ToList());
        }

        public IList<CandidatePeakGroupData> MakePeakGroupData(IList<TransitionGroupDocNode> transitionGroups,
            IList<ChromatogramGroupInfo> chromatogramGroupInfos, IList<FeatureValues> peakGroupFeatureValues)
        {
            var chromatogramInfos = new List<ChromatogramInfo>();
            
            var transitionChromInfos = new List<TransitionChromInfo>();
            for (int iTransitionGroup = 0; iTransitionGroup < transitionGroups.Count; iTransitionGroup++)
            {
                var transitionGroupDocNode = transitionGroups[iTransitionGroup];
                var chromatogramGroupInfo = chromatogramGroupInfos[iTransitionGroup];
                foreach (var transition in transitionGroupDocNode.Transitions)
                {
                    var chromatogramInfo = chromatogramGroupInfo.GetTransitionInfo(transition, MzMatchTolerance,
                        TransformChrom.raw, ChromatogramSet.OptimizationFunction);
                    if (chromatogramInfo == null)
                    {
                        continue;
                    }

                    chromatogramInfos.Add(chromatogramInfo);
                    transitionChromInfos.Add(FindTransitionChromInfo(transition));
                }
            }

            var peakGroupDatas = new List<CandidatePeakGroupData>();
            for (int peakIndex = 0; peakIndex < peakGroupFeatureValues.Count; peakIndex++)
            {
                peakGroupDatas.Add(MakeCandidatePeakGroupData(peakIndex, chromatogramInfos, transitionChromInfos, peakGroupFeatureValues[peakIndex]));
            }

            return peakGroupDatas;
        }

        private CandidatePeakGroupData MakeCandidatePeakGroupData(int peakIndex,
            IList<ChromatogramInfo> chromatogramInfos, IList<TransitionChromInfo> transitionChromInfos,
            FeatureValues featureValues)
        {
            Assume.AreEqual(chromatogramInfos.Count, transitionChromInfos.Count);
            bool isChosen = true;
            double minStartTime = double.MaxValue;
            double maxEndTime = double.MinValue;
            for (int iTransition = 0; iTransition < transitionChromInfos.Count; iTransition++)
            {
                var transitionChromInfo = transitionChromInfos[iTransition];
                var chromatogramInfo = chromatogramInfos[iTransition];
                var chromPeak = chromatogramInfo.GetPeak(peakIndex);
                if (chromPeak.IsEmpty)
                {
                    if (transitionChromInfo != null && !transitionChromInfo.IsEmpty)
                    {
                        isChosen = false;
                    }
                }
                else
                {
                    if (transitionChromInfo == null || transitionChromInfo.IsEmpty || transitionChromInfo.StartRetentionTime != chromPeak.StartTime ||
                        transitionChromInfo.EndRetentionTime != chromPeak.EndTime)
                    {
                        isChosen = false;
                    }

                    minStartTime = Math.Min(minStartTime, chromPeak.StartTime);
                    maxEndTime = Math.Max(maxEndTime, chromPeak.EndTime);
                }
            }
            var model = Settings.PeptideSettings.Integration.PeakScoringModel;
            if (model == null || !model.IsTrained)
            {
                model = LegacyScoringModel.DEFAULT_MODEL;
            }
            return new CandidatePeakGroupData(peakIndex, minStartTime, maxEndTime, isChosen, MakePeakScore(featureValues));
        }

        private PeakGroupScore MakePeakScore(FeatureValues featureValues)
        {
            var model = Settings.PeptideSettings.Integration.PeakScoringModel;
            if (model == null || !model.IsTrained)
            {
                model = LegacyScoringModel.DEFAULT_MODEL;
            }
            return PeakGroupScore.MakePeakScores(featureValues, model, _scoreQValueMap);
        }

        internal IEnumerable<FeatureValues> CalculateChromatogramGroupScores(
            IList<TransitionGroupDocNode> transitionGroups, IList<ChromatogramGroupInfo> chromatogramGroupInfos)
        {
            var context = new PeakScoringContext(Settings);
            var summaryData = new PeakFeatureEnumerator.SummaryPeptidePeakData(
                Settings, PeptideDocNode, transitionGroups, Settings.MeasuredResults.Chromatograms[ReplicateIndex],
                ChromFileInfo, chromatogramGroupInfos);
            while (summaryData.NextPeakIndex())
            {
                var scores = new List<float>();
                foreach (var calculator in Calculators)
                {
                    if (calculator is SummaryPeakFeatureCalculator)
                    {
                        scores.Add(calculator.Calculate(context, summaryData));
                    }
                    else if (calculator is DetailedPeakFeatureCalculator)
                    {
                        scores.Add(chromatogramGroupInfos[0].GetScore(calculator.GetType(), summaryData.UsedBestPeakIndex ? summaryData.BestPeakIndex : summaryData.PeakIndex));
                    }
                }

                yield return new FeatureValues(Calculators, ImmutableList.ValueOf(scores));
            }
        }

        public virtual PeakBounds GetTransitionPeakBounds(TransitionGroup transitionGroup, Transition transition)
        {
            var transitionChromInfo = FindTransitionChromInfo((TransitionDocNode) ((TransitionGroupDocNode) PeptideDocNode
                .FindNode(transitionGroup))?.FindNode(transition));
            if (transitionChromInfo == null || transitionChromInfo.IsEmpty)
            {
                return null;
            }
            return new PeakBounds(transitionChromInfo.StartRetentionTime, transitionChromInfo.EndRetentionTime);
        }

        private TransitionChromInfo FindTransitionChromInfo(TransitionDocNode transitionDocNode)
        {
            foreach (var transitionChromInfo in transitionDocNode.GetSafeChromInfo(ReplicateIndex))
            {
                if (transitionChromInfo.OptimizationStep == 0 &&
                    ReferenceEquals(transitionChromInfo.FileId, ChromFileInfo.FileId))
                {
                    return transitionChromInfo;
                }
            }

            return null;
        }
        public IEnumerable<float> ScorePeak(double startTime, double endTime, IEnumerable<DetailedPeakFeatureCalculator> calculators)
        {
            var peptideChromDataSets = MakePeptideChromDataSets();
            var explicitPeakBounds = new PeakBounds(startTime, endTime);
            peptideChromDataSets.PickChromatogramPeaks(explicitPeakBounds);
            return peptideChromDataSets.DataSets[0].PeakSets.First().DetailScores;
        }

        internal PeptideChromDataSets MakePeptideChromDataSets()
        {
            var peptideChromDataSets = new PeptideChromDataSets(PeptideDocNode, Settings, ChromFileInfo,
                Calculators.Detailed, false);
            foreach (var transitionGroup in PeptideDocNode.TransitionGroups)
            {
                var chromDatas = new List<ChromData>();
                var chromatogramGroupInfo = GetChromatogramGroupInfo(transitionGroup.TransitionGroup);
                if (chromatogramGroupInfo == null)
                {
                    continue;
                }
                foreach (var transition in transitionGroup.Transitions)
                {
                    var chromatogramInfo =
                        chromatogramGroupInfo.GetTransitionInfo(transition, MzMatchTolerance, TransformChrom.raw, null);
                    if (chromatogramInfo == null)
                    {
                        continue;
                    }
                    var rawTimeIntensities = chromatogramInfo.TimeIntensities;
                    chromatogramInfo.Transform(TransformChrom.interpolated);
                    var interpolatedTimeIntensities = chromatogramInfo.TimeIntensities;
                    var chromKey = new ChromKey(PeptideDocNode.ModifiedTarget, transitionGroup.PrecursorMz, null,
                        transition.Mz, 0, 0, transition.IsMs1 ? ChromSource.ms1 : ChromSource.fragment,
                        ChromExtractor.summed, true, false);
                    chromDatas.Add(new ChromData(chromKey, transition, rawTimeIntensities, interpolatedTimeIntensities));
                }

                if (!chromDatas.Any())
                {
                    continue;
                }

                var chromDataSet = new ChromDataSet(true, PeptideDocNode, transitionGroup,
                    Settings.TransitionSettings.FullScan.AcquisitionMethod, chromDatas.ToArray());
                peptideChromDataSets.Add(PeptideDocNode, chromDataSet);
            }

            return peptideChromDataSets;
        }

        public ChromatogramGroupInfo GetChromatogramGroupInfo(TransitionGroup transitionGroup)
        {
            if (_chromatogramGroupInfos.TryGetValue(transitionGroup, out var chromatogramGroupInfo))
            {
                return chromatogramGroupInfo;
            }

            var transitionGroupDocNode = (TransitionGroupDocNode) PeptideDocNode.FindNode(transitionGroup);
            return LoadChromatogramGroupInfo(transitionGroupDocNode);
        }

        private IList<ChromatogramGroupInfo> LoadChromatogramGroupInfos(TransitionGroupDocNode transitionGroup)
        {
            if (_chromatogramLoader != null)
            {
                return _chromatogramLoader(ReplicateIndex, transitionGroup);
            }
            var measuredResults = Settings.MeasuredResults;
            ChromatogramGroupInfo[] infoSet;
            if (!measuredResults.TryLoadChromatogram(measuredResults.Chromatograms[ReplicateIndex], PeptideDocNode,
                    transitionGroup,
                    MzMatchTolerance, out infoSet))
            {
                return ImmutableList.Empty<ChromatogramGroupInfo>();
            }

            return infoSet;
        }

        private ChromatogramGroupInfo LoadChromatogramGroupInfo(TransitionGroupDocNode transitionGroup)
        {
            var infos = LoadChromatogramGroupInfos(transitionGroup);
            foreach (var chromatogramInfo in infos)
            {
                if (Equals(chromatogramInfo.FilePath, ChromFileInfo.FilePath))
                {
                    _chromatogramGroupInfos.Add(transitionGroup.TransitionGroup, chromatogramInfo);
                    return chromatogramInfo;
                }
            }

            return null;
        }

        public static OnDemandFeatureCalculator GetFeatureCalculator(SrmDocument document, IdentityPath peptideIdentityPath, int replicateIndex, ChromFileInfoId chromFileInfoId)
        {
            var peptideDocNode = document.FindNode(peptideIdentityPath) as PeptideDocNode;
            if (peptideDocNode == null)
            {
                return null;
            }

            if (!document.Settings.HasResults || replicateIndex < 0 ||
                replicateIndex >= document.Settings.MeasuredResults.Chromatograms.Count)
            {
                return null;
            }

            var chromatogramSet = document.Settings.MeasuredResults.Chromatograms[replicateIndex];
            ChromFileInfo chromFileInfo;
            if (chromFileInfoId != null)
            {
                chromFileInfo = chromatogramSet.GetFileInfo(chromFileInfoId);
            }
            else
            {
                chromFileInfo = chromatogramSet.MSDataFileInfos.FirstOrDefault();
            }

            if (chromFileInfo == null)
            {
                return null;
            }

            return new OnDemandFeatureCalculator(FeatureCalculators.ALL, document.Settings, peptideDocNode, replicateIndex,
                chromFileInfo, null);
        }
    }
}
