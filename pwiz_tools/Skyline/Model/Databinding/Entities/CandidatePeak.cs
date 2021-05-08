﻿using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.DataBinding;
using pwiz.Common.DataBinding.Attributes;
using pwiz.Skyline.Model.Hibernate;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Databinding.Entities
{

    [InvariantDisplayName(nameof(CandidatePeakGroup))]
    public class CandidatePeakGroup : SkylineObject, ILinkValue
    {
        private ChromatogramGroup _chromatogramGroup;
        private int _peakIndex;
        public CandidatePeakGroup(ChromatogramGroup chromatogramGroup, int peakIndex, DefaultPeakScores defaultPeakScores) : base(chromatogramGroup.DataSchema)
        {
            _chromatogramGroup = chromatogramGroup;
            _peakIndex = peakIndex;
            DefaultPeakScores = defaultPeakScores;
        }

        private PrecursorResult GetPrecursorResult()
        {
            return _chromatogramGroup.PrecursorResult;
        }

        public double PeakGroupStartTime
        {
            get
            {
                return GetTransitionPeaks().Min(peak => peak.StartTime);
            }
        }

        public double PeakGroupEndTime
        {
            get
            {
                return GetTransitionPeaks().Max(peak => peak.EndTime);
            }
        }

        private IEnumerable<ChromPeak> GetTransitionPeaks()
        {
            return _chromatogramGroup.ChromatogramGroupInfo.GetPeakGroup(_peakIndex);
        }

        public override string ToString()
        {
            return string.Format(@"[{0}-{1}]", 
                PeakGroupStartTime.ToString(Formats.RETENTION_TIME),
                PeakGroupEndTime.ToString(Formats.RETENTION_TIME));
        }

        public void LinkValueOnClick(object sender, EventArgs args)
        {
            var skylineWindow = DataSchema.SkylineWindow;
            if (null == skylineWindow)
            {
                return;
            }

            var precursorResult = GetPrecursorResult();
            precursorResult.LinkValueOnClick(sender, args);
            var chromatogramGraph = skylineWindow.GetGraphChrom(precursorResult.GetResultFile().Replicate.Name);
            if (chromatogramGraph != null)
            {
                chromatogramGraph.ZoomToPeak(PeakGroupStartTime, PeakGroupEndTime);
            }
        }
        EventHandler ILinkValue.ClickEventHandler
        {
            get
            {
                return LinkValueOnClick;
            }
        }

        object ILinkValue.Value => this;



        public DefaultPeakScores DefaultPeakScores
        {
            get;
        }
    }
}
