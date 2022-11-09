using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.FileUI;
using pwiz.Skyline.Model;
using pwiz.Skyline.Properties;
using pwiz.Skyline.ToolsUI;
using pwiz.Skyline.Util;
using pwiz.SkylineTestUtil;

namespace pwiz.SkylineTestFunctional
{
    [TestClass]
    public class SkypTest : AbstractFunctionalTest
    {
        [TestMethod]
        public void TestSkyp()
        {
            TestFilesZipPaths = new[] { @"TestFunctional\SkypTest.zip"};
            RunFunctionalTest();
        }

        protected override void DoTest()
        {
            TestSkypValid();

            TestSkypGetNonExistentPath();

            TestSkypOpen();  
        }

        private void TestSkypOpen()
        {
            var skyZipPath = TestContext.GetProjectDirectory(@"TestFunctional\LibraryShareTest.zip"); // Reusing ShareDocumentTest test file

            var userName = "no-name@no-name.org";
            var password = "password";
            var server = new Server("http://fakepanoramalabkeyserver.org", userName, password); // This is the server in test.skyp and test-extended.skyp
            var anotherServer = new Server("http://anotherserver.org", null, null);

            // ---------------------- .skyp file with only a URL -----------------------------
            TestOpenErrorsSimpleSkyp(server, anotherServer);

            // ---------------------- .skyp file with URL, FileSize and DownloadingUser ------
            TestOpenErrorsExtendedSkyp(server, anotherServer);



            var skypPath = TestFilesDir.GetTestPath("test.skyp");
            var skyp = SkypFile.Create(skypPath, new List<Server>());
            var skyZipName = Path.GetFileName(skyZipPath);
            Assert.AreEqual(TestFilesDir.GetTestPath(skyZipName), skyp.DownloadPath);
            var skypSupport = new SkypSupport(SkylineWindow)
            {
                DownloadClientCreator =
                    new TestDownloadClientCreator(skyZipPath, skyp, false, false)
            };
            RunUI(() => skypSupport.Open(skypPath, null));
            WaitForDocumentLoaded();
            var skyZipNoExt = Path.GetFileNameWithoutExtension(skyZipPath);
            var explodedDir = TestFilesDir.GetTestPath(skyZipNoExt);
            Assert.AreEqual(Path.Combine(explodedDir, skyZipNoExt + SrmDocument.EXT), SkylineWindow.DocumentFilePath);
        }

        private void TestOpenErrorsExtendedSkyp(Server server, Server anotherServer)
        {
            // Contents of test-extended.skyp
            // http://fakepanoramalabkeyserver.org/LibraryShareTest.zip
            // FileSize: 100
            // DownloadingUser: no - name@no - name.org
            var skypPath2 = TestFilesDir.GetTestPath("test-extended.skyp");
            var skyp = SkypFile.Create(skypPath2, new List<Server>());
            Assert.IsNull(skyp.Server);


            // 1. Server in the .skyp file does not match a saved Panorama server. Expect to see an error about adding a new Panorama 
            //    server in Skyline. Username in the EditServerDlg should be the username from the .skyp file
            TestSkypOpenWithError(skypPath2, null, new[] { anotherServer },
                TestDownloadClient.ERROR401,
                skyp.GetSkylineDocServer().URI,  skyp.User  /* Expect username from the .skyp file */, string.Empty,
                false);


            // 2. Server in the .skyp file matches a saved Panorama server. If we get a 401 (Unauthorized) error it means that the saved
            //    credentials are invalid. Expect a message about updating the credentials of the saved Panorama server.
            TestSkypOpenWithError(skypPath2, server, new[] { server, anotherServer },
                TestDownloadClient.ERROR401,
                server.URI, server.Username, server.Password,
                false); // Username in test-extended.skyp matches the saved username for server


            // 3. Server in the .skyp file matches a saved Panorama server. If we get a 403 (Forbidden) error it means that the saved
            //    credentials are invalid. The username in the skyp file is the same as the saved username for the server. 
            //    EditServerDlg should not be shown in this case
            TestSkypOpenWithError(skypPath2, server, new[] { server, anotherServer },
                TestDownloadClient.ERROR403,
                null, null, null,
                false,
                false); // Do not expect to see the EditServerDlg


            // 4. Server in the .skyp file matches a saved Panorama server. If we get a 403 (Forbidden) error it means that the saved
            //    credentials are invalid. Expect a message about updating the credentials of the saved Panorama server.
            //    Username in the .skyp file is not the same as the username saved for the server. Username displayed in the EditServerDlg
            //    should be the username from the .skyp file.
            var userName2 = "another-" + skyp.User;
            var server2 = new Server("http://fakepanoramalabkeyserver.org", userName2, server.Password);
            TestSkypOpenWithError(skypPath2, server2, new[] { server2, anotherServer },
                TestDownloadClient.ERROR403,
                server2.URI, skyp.User /* username from the .skyp file */, string.Empty,
                true); // Username in test2.skyp does not match the saved username for server2
        }

        private void TestOpenErrorsSimpleSkyp(Server server, Server anotherServer)
        {
            // Contents of test.skyp:
            // http://fakepanoramalabkeyserver.org/LibraryShareTest.zip
            var skypPath = TestFilesDir.GetTestPath("test.skyp");

            var skyp = SkypFile.Create(skypPath, new List<Server>());
            Assert.IsNull(skyp.Server);


            // 1. Server in the .skyp file does not match a saved Panorama server. Expect to see an error about adding a new Panorama 
            //    server in Skyline.
            TestSkypOpenWithError(skypPath, null, new[] { anotherServer },
                TestDownloadClient.ERROR401,
                server.URI, string.Empty, string.Empty,
                false); // No matching server, don't expect a username mismatch


            // 2. Server in the .skyp file matches a saved Panorama server. If we get a 401 (Unauthorized) error it means that the saved
            //    credentials are invalid. Expect a message about updating the credentials of a saved Panorama server.
            TestSkypOpenWithError(skypPath, server, new[] { server, anotherServer },
                TestDownloadClient.ERROR401,
                server.URI, server.Username, server.Password,
                false); // test.skyp does not have the "DownloadingUser"


            // 3. Server in the .skyp file matches a saved Panorama server. If we get a 403 (Forbidden) error it means that the saved user does
            //    not have adequate permissions for the requested resource on the Panorama server. EditServerDlg should not be shown.
            TestSkypOpenWithError(skypPath, server, new[] { server, anotherServer },
                TestDownloadClient.ERROR403,
                null, null, null,
                false, // test.skyp does not have the "DownloadingUser"
                false); // Don't expect to see the EditServerDlg
        }

        private void TestSkypOpenWithError (string skypPath, Server matchingServer, Server[] savedServers, string errorCode,
            Uri expectedUrlInEditServerDlg, string expectedUserNameInEditServerDlg, string expectedPasswordInEditServerDlg,
            bool usernameMismatch,
            bool expectEditServerDlg = true)
        {
            Settings.Default.ServerList.Clear();
            Settings.Default.ServerList.AddRange(savedServers);

            var skyp = SkypFile.Create(skypPath, savedServers);
            if (matchingServer == null) Assert.IsNull(skyp.Server); // No matching saved server found
            else Assert.AreEqual(skyp.Server, matchingServer); // Matching saved server found
            Assert.AreEqual(usernameMismatch, skyp.UsernameMismatch());


            bool err401 = TestDownloadClient.ERROR401.Equals(errorCode);
            bool err403 = TestDownloadClient.ERROR403.Equals(errorCode);

            var skypSupport = new SkypSupport(SkylineWindow)
            {
                DownloadClientCreator =
                    new TestDownloadClientCreator(null, skyp, err401, err403)
            };
            var errDlg = ShowDialog<AlertDlg>(() => skypSupport.Open(skypPath, savedServers));
            string expectedErr = null;
            if (err401)
            {
                expectedErr = matchingServer == null
                    ? string.Format(
                        "You may have to add {0} as a Panorama server in Skyline.",
                        skyp.GetServerName())
                    : string.Format(
                        "You may have to update the credentials saved in Skyline for the Panorama server {0}.",
                        skyp.GetServerName());
            }
            else if (err403)
            {
                expectedErr = usernameMismatch?
                    string.Format(
                        "Username {0} saved with the server {1} in Skyline does not have permissions to download this file. " +
                        "The .skyp file was downloaded by {2}. Would you like to update the credentials saved in Skyline for {3}?",
                        skyp.Server.Username, skyp.GetServerName(), skyp.User, skyp.GetServerName())
                    : 
                    string.Format(
                        Resources.SkypSupport_Download_You_do_not_have_permissions_to_download_this_file_from__0__,
                        skyp.GetServerName());
            }

            Assert.IsNotNull(expectedErr);
            Assert.IsTrue(errDlg.Message.Contains(expectedErr));
            Assert.IsTrue(errDlg.Message.Contains(errorCode));

            if (expectEditServerDlg)
            {
                var editServerDlg = ShowDialog<EditServerDlg>(errDlg.ClickOk);
                RunUI(() =>
                {
                    Assert.AreEqual(editServerDlg.URL, expectedUrlInEditServerDlg.ToString());
                    Assert.AreEqual(editServerDlg.Username, expectedUserNameInEditServerDlg);
                    Assert.AreEqual(editServerDlg.Password , expectedPasswordInEditServerDlg);
                    editServerDlg.CancelButton.PerformClick();
                });
                WaitForClosedForm(editServerDlg);
            }
            else
            {
                RunUI(() => { errDlg.ClickOk(); });
                WaitForClosedForm(errDlg);
            }

            Settings.Default.ServerList.Clear();
        }

        private void TestSkypGetNonExistentPath()
        {
            const string skyZip = "empty.sky.zip";

            var skyZipPath = TestFilesDir.GetTestPath(skyZip);
            
            Assert.AreEqual(skyZipPath, SkypFile.GetNonExistentPath(TestFilesDir.FullPath, skyZip));

            // Create the file so that GetNonExistentPath appends a (1) suffix to the file name
            using (File.Create(skyZipPath)) { }
            Assert.IsTrue(File.Exists(skyZipPath));

            skyZipPath = TestFilesDir.GetTestPath("empty(1).sky.zip");
            Assert.AreEqual(skyZipPath, SkypFile.GetNonExistentPath(TestFilesDir.FullPath, skyZip));

            // Create a empty(1) directory.
            // Now empty.sky.zip AND empty(1) directory exist in the folder.
            // empty(1).sky.zip does not exist, but opening a file by this name will extract the zip
            // in an empty(1)(1) since empty(1) exists. So we append a (2) suffix to fhe filename so 
            // that the zip is extracted in an empty(2) folder. 
            Directory.CreateDirectory(TestFilesDir.GetTestPath("empty(1)"));
            skyZipPath = TestFilesDir.GetTestPath("empty(2).sky.zip");
            Assert.AreEqual(skyZipPath, SkypFile.GetNonExistentPath(TestFilesDir.FullPath, skyZip));
        }

        private void TestSkypValid()
        {
            AssertEx.ThrowsException<InvalidDataException>(
                () => SkypFile.ReadSkyp(new SkypFile(), new StringReader(STR_EMPTY_SKYP)),
                string.Format(
                    Resources.SkypFile_GetSkyFileUrl_File_does_not_contain_the_URL_of_a_shared_Skyline_archive_file___0___on_a_Panorama_server_,
                    SrmDocumentSharing.EXT_SKY_ZIP));

            var err =
                string.Format(
                    Resources
                        .SkypFile_GetSkyFileUrl_Expected_the_URL_of_a_shared_Skyline_document_archive___0___in_the_skyp_file__Found__1__instead_,
                    SrmDocumentSharing.EXT_SKY_ZIP,
                    STR_INVALID_SKYP1);
            AssertEx.ThrowsException<InvalidDataException>(() => SkypFile.ReadSkyp(new SkypFile(), new StringReader(STR_INVALID_SKYP1)), err);


            err = string.Format(Resources.SkypFile_GetSkyFileUrl__0__is_not_a_valid_URL_on_a_Panorama_server_, STR_INVALID_SKYP2);
            AssertEx.ThrowsException<InvalidDataException>(() => SkypFile.ReadSkyp(new SkypFile(), new StringReader(STR_INVALID_SKYP2)), err);

            var skyp1 = new SkypFile();
            AssertEx.NoExceptionThrown<Exception>(() => SkypFile.ReadSkyp(skyp1, new StringReader(STR_VALID_SKYP)));
            Assert.AreEqual(new Uri(STR_VALID_SKYP), skyp1.SkylineDocUri);
            Assert.IsNull(skyp1.Size);
            Assert.IsNull(skyp1.User);


            var skyp2 = new SkypFile();
            SkypFile.ReadSkyp(skyp2, new StringReader(STR_VALID_SKYP_EXTENDED));
            Assert.AreEqual(new Uri(STR_VALID_SKYP_LOCALHOST), skyp2.SkylineDocUri);
            Assert.AreEqual(LOCALHOST, skyp2.GetServerName());
            Assert.AreEqual(skyp2.Size, 100);
            Assert.AreEqual(skyp2.User, "no-name@no-name.edu");

            var skyp3 = new SkypFile();
            SkypFile.ReadSkyp(skyp3, new StringReader(STR_INVALID_SIZE_SKYP3));
            Assert.AreEqual(new Uri(STR_VALID_SKYP_LOCALHOST), skyp3.SkylineDocUri);
            Assert.IsFalse(skyp3.Size.HasValue);
            Assert.AreEqual(skyp3.User, "no-name@no-name.edu");

        }

        private const string STR_EMPTY_SKYP = "";
        private const string STR_INVALID_SKYP1 = @"http://panoramaweb.org/_webdav/Project/not_a_shared_zip.sky";
        private const string STR_INVALID_SKYP2 = @"C:\Project\not_a_shared_zip.sky.zip";
        private const string STR_VALID_SKYP = @"https://panoramaweb.org/_webdav/Project/shared_zip.sky.zip";

        private const string LOCALHOST = "http://localhost:8080";
        private const string STR_VALID_SKYP_LOCALHOST = LOCALHOST + "/labkey/_webdav/Project/shared_zip.sky.zip";
        private const string STR_VALID_SKYP_EXTENDED =
            STR_VALID_SKYP_LOCALHOST + "\n\rFileSize:100\n\rDownloadingUser:no-name@no-name.edu";

        private const string STR_INVALID_SIZE_SKYP3 =
            STR_VALID_SKYP_LOCALHOST + "\n\rFileSize:invalid\n\rDownloadingUser:no-name@no-name.edu";
    }

    public abstract class TestDownloadClient : IDownloadClient
    {
        private readonly string _srcPath;
        protected readonly SkypFile _skyp;
        private IProgressMonitor ProgressMonitor { get; }
        private IProgressStatus ProgressStatus { get; set; }

        public const string ERROR401 = "(401) Unauthorized";
        public const string ERROR403 = "(403) Forbidden";

        public TestDownloadClient(string srcFile, SkypFile skyp, IProgressMonitor progressMonitor, IProgressStatus progressStatus)
        {
            _srcPath = srcFile;
            _skyp = skyp;
            IsCancelled = false;
            ProgressMonitor = progressMonitor;
            ProgressStatus = progressStatus;
        }
    
        public void Download(SkypFile skyp /*Uri remoteFile, string downloadPath, string username, string password*/)
        {
            var downloadException = GetDownloadException();
            if (downloadException != null)
            {
                ProgressMonitor.UpdateProgress(ProgressStatus = ProgressStatus.ChangeErrorException(downloadException));
                return;
            }
            Assert.AreEqual(_skyp.DownloadPath, skyp.DownloadPath);
            File.Copy(_srcPath, skyp.DownloadPath);
        }

        public bool IsCancelled { get; }
        public bool IsError => Error != null;
        public Exception Error { get; set; }

        public abstract SkypDownloadException GetDownloadException();
    }

    public class TestDownloadClientError401 : TestDownloadClient
    {
        public TestDownloadClientError401(SkypFile skyp, IProgressMonitor progressMonitor, IProgressStatus progressStatus) : 
            base(null, skyp, progressMonitor, progressStatus)
        {
        }

        public override SkypDownloadException GetDownloadException()
        {
            return new SkypDownloadException(SkypDownloadException.GetMessage(_skyp, new Exception(ERROR401), HttpStatusCode.Unauthorized), HttpStatusCode.Unauthorized, null);
        }
    }

    public class TestDownloadClientError403 : TestDownloadClient
    {
        public TestDownloadClientError403(SkypFile skyp, IProgressMonitor progressMonitor, IProgressStatus progressStatus) :
            base(null, skyp, progressMonitor, progressStatus)
        {
        }

        public override SkypDownloadException GetDownloadException()
        {
            return new SkypDownloadException(SkypDownloadException.GetMessage(_skyp, new Exception(ERROR403), HttpStatusCode.Forbidden), HttpStatusCode.Forbidden, null);
        }
    }

    public class TestDownloadClientNoError : TestDownloadClient
    {
        public TestDownloadClientNoError(string srcFile, SkypFile skyp, IProgressMonitor progressMonitor, IProgressStatus progressStatus) :
            base(srcFile, skyp, progressMonitor, progressStatus)
        {
        }

        public override SkypDownloadException GetDownloadException()
        {
            return null;
        }
    }

    internal class TestDownloadClientCreator : DownloadClientCreator
    {
        private string _skyZipPath;
        private SkypFile _skyp;
        private bool _401Error;
        private bool _403Error;

        public TestDownloadClientCreator(string skyZipPath, SkypFile skyp, bool error401, bool error403)
        {
            _skyZipPath = skyZipPath;
            _skyp = skyp;
            _401Error = error401;
            _403Error = error403;
        }

        public override IDownloadClient Create(IProgressMonitor progressMonitor, IProgressStatus progressStatus)
        {
            if (_401Error)
            {
                return new TestDownloadClientError401(_skyp, progressMonitor, progressStatus);
            }

            else if (_403Error)
            {
                return new TestDownloadClientError403(_skyp, progressMonitor, progressStatus);
            }
            else
            {
                return new TestDownloadClientNoError(_skyZipPath, _skyp, progressMonitor, progressStatus);
            }
        }
    }
}
