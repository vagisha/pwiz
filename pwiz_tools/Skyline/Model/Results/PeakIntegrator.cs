﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2020 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.PeakFinding;
using pwiz.Skyline.Util;

namespace pwiz.Skyline.Model.Results
{

    public class PeakIntegrator
    {
        public PeakIntegrator(TimeIntensities interpolatedTimeIntensities)
            : this(interpolatedTimeIntensities, null)
        {
        }

        public PeakIntegrator(TimeIntensities interpolatedTimeIntensities, IPeakFinder peakFinder)
        {
            InterpolatedTimeIntensities = interpolatedTimeIntensities;
            PeakFinder = peakFinder;
        }

        public IPeakFinder PeakFinder { get; private set; }
        public TimeIntensities InterpolatedTimeIntensities { get; private set; }
        public TimeIntensities RawTimeIntensities { get; set; }
        public TimeIntervals TimeIntervals { get; set; }

        /// <summary>
        /// Return the ChromPeak with the specified start and end times chosen by a user.
        /// </summary>
        public ChromPeak IntegratePeak(float startTime, float endTime, ChromPeak.FlagValues flags)
        {
            if (TimeIntervals != null)
            {
                // For a triggered acquisition, we just use the start and end time supplied by the
                // user and Crawdad is not involved with the peak integration.
                return IntegratePeakWithoutBackground(startTime, endTime, flags);
            }
            if (PeakFinder == null)
            {
                PeakFinder = CreatePeakFinder(InterpolatedTimeIntensities);
            }

            int startIndex = InterpolatedTimeIntensities.IndexOfNearestTime(startTime);
            int endIndex = InterpolatedTimeIntensities.IndexOfNearestTime(endTime);
            if (startIndex == endIndex)
            {
                return ChromPeak.EMPTY;
            }
            var foundPeak = PeakFinder.GetPeak(startIndex, endIndex);
            return new ChromPeak(PeakFinder, foundPeak, flags, InterpolatedTimeIntensities, RawTimeIntensities?.Times);
        }

        /// <summary>
        /// Returns a ChromPeak and IFoundPeak that match the start and end times a particular other IFoundPeak
        /// that was found by Crawdad.
        /// </summary>
        public Tuple<ChromPeak, IFoundPeak> IntegrateFoundPeak(IFoundPeak peakMax, ChromPeak.FlagValues flags)
        {
            Assume.IsNotNull(PeakFinder);
            var interpolatedPeak = PeakFinder.GetPeak(peakMax.StartIndex, peakMax.EndIndex);
            if ((flags & ChromPeak.FlagValues.forced_integration) != 0 && ChromData.AreCoeluting(peakMax, interpolatedPeak))
                flags &= ~ChromPeak.FlagValues.forced_integration;

            var chromPeak = new ChromPeak(PeakFinder, interpolatedPeak, flags, InterpolatedTimeIntensities, RawTimeIntensities?.Times);
            if (TimeIntervals != null)
            {
                chromPeak = IntegratePeakWithoutBackground(InterpolatedTimeIntensities.Times[peakMax.StartIndex], InterpolatedTimeIntensities.Times[peakMax.EndIndex], flags);
            }

            return Tuple.Create(chromPeak, interpolatedPeak);
        }

        /// <summary>
        /// Returns a ChromPeak with the specified start and end times and no background subtraction.
        /// </summary>
        private ChromPeak IntegratePeakWithoutBackground(float startTime, float endTime, ChromPeak.FlagValues flags)
        {
            if (TimeIntervals != null)
            {
                var intervalIndex = TimeIntervals.IndexOfIntervalEndingAfter(startTime);
                if (intervalIndex >= 0 && intervalIndex < TimeIntervals.Count)
                {
                    startTime = Math.Max(startTime, TimeIntervals.Starts[intervalIndex]);
                    endTime = Math.Min(endTime, TimeIntervals.Ends[intervalIndex]);
                }
            }
            return new ChromPeak(RawTimeIntensities ?? InterpolatedTimeIntensities, startTime, endTime, flags);
        }

        public static IPeakFinder CreatePeakFinder(TimeIntensities interpolatedTimeIntensities)
        {
            var peakFinder = PeakFinders.NewDefaultPeakFinder();
            peakFinder.SetChromatogram(interpolatedTimeIntensities.Times, interpolatedTimeIntensities.Intensities);
            return peakFinder;
        }

        public static IList<float> GetDdaIntensities(IList<TimeIntensities> chromatograms,
            IList<PeakBounds> peakBoundsList)
        {
            Assume.AreEqual(chromatograms.Count, peakBoundsList.Count);
            var nonNullPeakBounds = peakBoundsList.Where(bounds => null != bounds).ToList();
            if (nonNullPeakBounds.Count == 0)
            {
                return Enumerable.Repeat(0f, chromatograms.Count).ToArray();
            }
            var minStartTime = (float) nonNullPeakBounds.Min(bounds => bounds.StartTime);
            var maxEndTime = (float) nonNullPeakBounds.Max(bounds => bounds.EndTime);
            var intensitiesAtPoints = new Dictionary<float, double>();
            for (int iChromatogram = 0; iChromatogram < chromatograms.Count; iChromatogram++)
            {
                var timeIntensities = chromatograms[iChromatogram];
                var iPoint = CollectionUtil.BinarySearch(timeIntensities.Times, minStartTime);
                if (iPoint < 0)
                {
                    iPoint = ~iPoint;
                }

                for (; iPoint < timeIntensities.Times.Count; iPoint++)
                {
                    var time = timeIntensities.Times[iPoint];
                    if (time > maxEndTime)
                    {
                        break;
                    }

                    intensitiesAtPoints.TryGetValue(time, out var totalIntensity);
                    totalIntensity += timeIntensities.Intensities[iPoint];
                    intensitiesAtPoints[time] = totalIntensity;
                }
            }

            var ddaIntensities = new List<float>();
            var maxTotalIntensity = intensitiesAtPoints.Values.Max();
            var timeWithMaxIntensity = intensitiesAtPoints.OrderBy(kvp => kvp.Key)
                .FirstOrDefault(kvp => kvp.Value == maxTotalIntensity).Key;
            for (int iChromatogram = 0; iChromatogram < chromatograms.Count; iChromatogram++)
            {
                var peakBounds = peakBoundsList[iChromatogram];
                if (peakBounds == null || peakBounds.StartTime > timeWithMaxIntensity || peakBounds.EndTime < timeWithMaxIntensity)
                {
                    ddaIntensities.Add(0);
                    continue;
                }

                var timeIntensities = chromatograms[iChromatogram];
                int iPoint = CollectionUtil.BinarySearch(timeIntensities.Times, timeWithMaxIntensity);
                if (iPoint >= 0 && iPoint < timeIntensities.NumPoints)
                {
                    ddaIntensities.Add(timeIntensities.Intensities[iPoint]);
                }
                else
                {
                    ddaIntensities.Add(0);
                }
            }

            return ddaIntensities;
        }
    }
}
