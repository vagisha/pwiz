﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoQC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SharedBatch;


namespace AutoQCTest
{
    [TestClass]
    public class PanoramaTest
    {
        public const string SERVER_URL = "https://panoramaweb.org/";
        public const string PANORAMA_PARENT_PATH = "SkylineTest";
        public const string PANORAMA_FOLDER_PREFIX = "AutoQcTest";
        public const string PANORAMA_USER_NAME = "skyline_tester@proteinms.net";
        public const string PANORAMA_PASSWORD = "lclcmsms";
        private const int WAIT_3SEC = 3000;
        private const int TIMEOUT_80SEC = 80000;

        private string _testPanoramaFolder;
        private WebPanoramaClient _panoramaClient;

        /// <summary>
        /// Called by the unit test framework when a test begins.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            // Create a Panorama folder for the test
            var panoramaServerUri = new Uri(SERVER_URL);
            _panoramaClient = new WebPanoramaClient(panoramaServerUri);

            var random = new Random();
            FolderOperationStatus status;
            string uniqueFolderName;
            do
            {
                uniqueFolderName = PANORAMA_FOLDER_PREFIX + random.Next(1000, 9999);
                status = _panoramaClient.CreateFolder(PANORAMA_PARENT_PATH, uniqueFolderName, PANORAMA_USER_NAME, PANORAMA_PASSWORD);
            }
            while (FolderOperationStatus.alreadyexists == status);
            
            Assert.AreEqual(FolderOperationStatus.OK, status, "Expected folder to be successfully created");
            _testPanoramaFolder = uniqueFolderName;
        }

        /// <summary>
        /// Called by the unit test framework when a test is finished.
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            // Delete the Panorama test folder
            Assert.AreEqual(FolderOperationStatus.OK,
                _panoramaClient.DeleteFolder($"{PANORAMA_PARENT_PATH}/{_testPanoramaFolder}/", PANORAMA_USER_NAME,
                    PANORAMA_PASSWORD));
        }

        [TestMethod]
        public async Task TestPublishToPanorama()
        {
            Assert.IsTrue(File.Exists(TestUtils.GetTestFilePath("QEP_2015_0424_RJ_2015_04\\QEP_2015_0424_RJ.sky")),
                "Could not find Skyline file, nothing to import data into.");
            Assert.IsTrue(File.Exists(TestUtils.GetTestFilePath("PanoramaTestConfig\\QEP_2015_0424_RJ_05_prtc.raw")),
                "Data file is not in configuration folder, nothing to upload.");

            File.Copy(TestUtils.GetTestFilePath("QEP_2015_0424_RJ_2015_04\\QEP_2015_0424_RJ.sky"), TestUtils.GetTestFilePath("QEP_2015_0424_RJ.sky"), true);
            File.Copy(TestUtils.GetTestFilePath("QEP_2015_0424_RJ_2015_04\\QEP_2015_0424_RJ.skyd"), TestUtils.GetTestFilePath("QEP_2015_0424_RJ.skyd"), true);

            
            var skylineSettings = TestUtils.GetTestSkylineSettings();
            Assert.IsNotNull(skylineSettings, "Test cannot run without an installation of Skyline or Skyline-daily.");

            var config = new AutoQcConfig("PanoramaTestConfig", false, DateTime.MinValue, DateTime.MinValue,
                TestUtils.GetTestMainSettings("folderToWatch", "PanoramaTestConfig"),
                new PanoramaSettings(true, SERVER_URL, PANORAMA_USER_NAME, PANORAMA_PASSWORD, $"{PANORAMA_PARENT_PATH}/{_testPanoramaFolder}"), 
                TestUtils.GetTestSkylineSettings());

            // Validate the configuration
            try
            {
                config.Validate(true);
            }
            catch (Exception e)
            {
                Assert.Fail($"Expected configuration to be valid. Validation failed with error '{e.Message}'");
            }

            var runner = new ConfigRunner(config, TestUtils.GetTestLogger(config));
            Assert.IsTrue(runner.CanStart());
            runner.Start();
            Assert.IsTrue(WaitForConfigRunning(runner), $"Expected configuration to be running. Status was {runner.GetStatus()}.");
            
            var success = await SuccessfulPanoramaUpload(_testPanoramaFolder);
            Assert.IsTrue(success, "File was not uploaded to panorama.");

            runner.Stop();
        }

        private bool WaitForConfigRunning(ConfigRunner runner)
        {
            var start = DateTime.Now;
            while (!RunnerStatus.Running.Equals(runner.GetStatus()))
            {
                Thread.Sleep(1000);
                if (DateTime.Now > start.AddSeconds(60))
                {
                    return false;
                }
            }

            return true;
        }


        private async Task<bool> SuccessfulPanoramaUpload(string uniqueFolder)
        {
            var panoramaServerUri = new Uri(PanoramaUtil.ServerNameToUrl(SERVER_URL));
            var labKeyQuery = PanoramaUtil.CallNewInterface(panoramaServerUri, "query", $"{PANORAMA_PARENT_PATH}/{uniqueFolder}",
                "selectRows", "schemaName=targetedms&queryName=runs", true);
            var webClient = new WebPanoramaClient(panoramaServerUri);
            var startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var x = startTime;
            while (x < startTime + TIMEOUT_80SEC)
            {
                var jsonAsString = webClient.DownloadString(labKeyQuery, PANORAMA_USER_NAME, PANORAMA_PASSWORD);
                var json = JsonConvert.DeserializeObject<RootObject>(jsonAsString);
                if (json.rowCount > 0) return true;
                await Task.Delay(WAIT_3SEC);
                x = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }

            return false;
        }
    }
}
