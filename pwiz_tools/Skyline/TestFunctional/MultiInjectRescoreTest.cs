﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2024 University of Washington - Seattle, WA
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
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Skyline.Controls.Graphs;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    /// <summary>
    /// Tests doing a "Rescore" on a document with a multi-injection replicate where
    /// one of the injections fails to be reimported because that injection is missing
    /// chromatograms for the iRT standards.
    /// </summary>
    [TestClass]
    public class MultiInjectRescoreTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestMultiInjectRescore()
        {
            TestFilesZip = @"TestFunctional\MultiInjectRescoreTest.zip";
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            RunUI(()=>SkylineWindow.OpenFile(TestFilesDir.GetTestPath("RescoreTest.sky")));
            WaitForDocumentLoaded();

            // The document has one replicate, and that replicate has two files
            var measuredResults = SkylineWindow.Document.Settings.MeasuredResults;
            Assert.AreEqual(1, measuredResults.Chromatograms.Count);
            Assert.AreEqual(2, measuredResults.Chromatograms[0].FileCount);

            foreach (var peptideDocNode in SkylineWindow.Document.Molecules)
            {
                if (peptideDocNode.GlobalStandardType == null)
                {
                    // The ordinary peptides are expected to have chromatograms for both files
                    Assert.AreEqual(2, peptideDocNode.Results[0].Count);
                }
                else
                {
                    // The iRT peptides are expected to be missing chromatograms for one of the files
                    Assert.AreEqual(1, peptideDocNode.Results[0].Count);
                }
            }

            // Do a "Rescore" which is expected to fail for one of the files because the iRT peptides do
            // not have any chromatograms
            var manageResultsDlg = ShowDialog<ManageResultsDlg>(SkylineWindow.ManageResults);
            RunDlg<RescoreResultsDlg>(manageResultsDlg.Rescore, rescoreResultsDlg =>
            {
                rescoreResultsDlg.Rescore(false);
            });
            try
            {
                WaitForCondition(() => SkylineWindow.Document.MeasuredResults?.Chromatograms[0]?.FileCount == 1);
            }
            catch (Exception ex)
            {
                throw AnnotateTestFailure(ex);
            }


            var allChromatogramsGraph = WaitForOpenForm<AllChromatogramsGraph>();
            RunUI(() =>
            {
                var fileStatuses = allChromatogramsGraph.Files.ToList();
                Assert.AreEqual(2, fileStatuses.Count);
                Assert.IsNotNull(fileStatuses[0].Error);
                Assert.IsNull(fileStatuses[1].Error);
            });
            measuredResults = SkylineWindow.Document.Settings.MeasuredResults;
            Assert.AreEqual(1, measuredResults.Chromatograms.Count);
            Assert.AreEqual(1, measuredResults.Chromatograms[0].FileCount);

            // Verify that the peptides still have their results for the one file that did not fail
            foreach (var peptideDocNode in SkylineWindow.Document.Molecules)
            {
                Assert.AreEqual(1, peptideDocNode.Results[0].Count);
            }
            OkDialog(allChromatogramsGraph, allChromatogramsGraph.Close);
        }

        /// <summary>
        /// Add extra information to a test failure exception in hopes of tracking down intermittent failure.
        /// </summary>
        private Exception AnnotateTestFailure(Exception testFailure)
        {
            // Write out the current document's XML in hopes of figuring out what went wrong
            try
            {
                var documentStringWriter = new StringWriter();
                new XmlSerializer(typeof(SrmDocument)).Serialize(documentStringWriter, SkylineWindow.Document);
                return new AssertFailedException("Unexpected test failure. Document XML: " + documentStringWriter,
                    testFailure);
            }
            catch (Exception exception2)
            {
                throw new AggregateException(testFailure, exception2);
            }
        }
    }
}
