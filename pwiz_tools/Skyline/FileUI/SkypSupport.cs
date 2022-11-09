using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using pwiz.Common.SystemUtil;
using pwiz.Skyline.Alerts;
using pwiz.Skyline.Controls;
using pwiz.Skyline.Model;
using pwiz.Skyline.Properties;
using pwiz.Skyline.Util;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.FileUI
{
    public class SkypSupport
    {
        private SkylineWindow _skyline;

        public DownloadClientCreator DownloadClientCreator { private get; set; }

        public SkypSupport(SkylineWindow skyline)
        {
            _skyline = skyline;
            DownloadClientCreator = new DownloadClientCreator();
        }

        public bool Open(string skypPath, IEnumerable<Server> servers, FormEx parentWindow = null)
        {
            if (string.IsNullOrEmpty(skypPath))
            {
                MessageDlg.Show(parentWindow ?? _skyline, Resources.SkypSupport_Open_Path_to__skyp_file_cannot_be_empty_);
                return false;
            }

            SkypFile skyp;

            try
            {
                skyp = SkypFile.Create(skypPath, servers); // Read the .skyp file
            }
            catch (Exception e)
            {
                var message = TextUtil.LineSeparate(Resources.SkypSupport_Open_Failure_opening_skyp_file_, e.Message);
                MessageDlg.ShowWithException(parentWindow ?? _skyline, message, e);
                return false;
            }

            try
            {
                using (var longWaitDlg = new LongWaitDlg
                {
                    Text = Resources.SkypSupport_Open_Downloading_Skyline_Document_Archive,
                })
                {
                    var progressStaus = longWaitDlg.PerformWork(parentWindow ?? _skyline, 1000, progressMonitor => Download(skyp, progressMonitor, parentWindow));
                    if (longWaitDlg.IsCanceled)
                        return false;

                    if (progressStaus.IsError)
                    {
                        var exception = progressStaus.ErrorException;
                        if (exception is SkypDownloadException)
                        {
                            var skypEx = exception as SkypDownloadException;
                            if (skypEx.Unauthorized())
                            {
                                return skyp.Server == null ? AddServerAndOpen(skyp, exception.Message, parentWindow) : EditServerAndOpen(skyp, exception.Message, parentWindow);
                            }
                            else if (skypEx.Forbidden() && skyp.UsernameMismatch())
                            {
                                return EditServerAndOpen(skyp, exception.Message, parentWindow);
                            }
                            
                            MessageDlg.ShowWithException(parentWindow ?? _skyline, exception.Message, exception);
                            return false;
                        }
                        else
                        {
                            ShowDownloadError(parentWindow, skyp, exception);
                            return false;
                        }
                    }
                }
                
                return _skyline.OpenSharedFile(skyp.DownloadPath);
            }
            catch (Exception e)
            {
                ShowDownloadError(parentWindow, skyp, e);
                return false;
            }
        }

        private void ShowDownloadError(FormEx parentWindow, SkypFile skyp, Exception exception)
        {
            var message = TextUtil.LineSeparate(string.Format(
                Resources
                    .SkypSupport_Download_There_was_an_error_downloading_the_Skyline_document_specified_in_the_skyp_file___0__,
                skyp.SkylineDocUri), exception.Message);
            MessageDlg.ShowWithException(parentWindow ?? _skyline, message, exception);
        }

        private bool AddServerAndOpen(SkypFile skypFile, string message, FormEx parentWindow)
        {
            using (var alertDlg = new AlertDlg(message, MessageBoxButtons.OKCancel))
            {
                alertDlg.ShowDialog(parentWindow ?? _skyline);
                if (alertDlg.DialogResult == DialogResult.OK)
                {
                    var allServers = Settings.Default.ServerList;
                    var newServer = allServers.EditItem(parentWindow, skypFile.GetSkylineDocServer(), allServers, false);
                    if (newServer == null)
                        return false;
                    allServers.Add(newServer); // server should not have the same Uri as an existing server in the list. EditServerDlg takes care of that.

                    return Open(skypFile.SkypPath, allServers, parentWindow);
                }

                return false;
            }
        }

        private bool EditServerAndOpen(SkypFile skyp, string message, FormEx parentWindow)
        {
            using (var alertDlg = new AlertDlg(message, MessageBoxButtons.OKCancel))
            {
                alertDlg.ShowDialog(parentWindow ?? _skyline);
                if (alertDlg.DialogResult == DialogResult.OK)
                {
                    var allServers = Settings.Default.ServerList;
                    var serverInSkyp = skyp.Server;
                    var servers = allServers.Where(s => !Equals(serverInSkyp.URI.Host, s.URI.Host)).ToList();

                    var serverToEdit = skyp.UsernameMismatch()
                        ? new Server(serverInSkyp.URI, skyp.User, null) // Use the username from the .skyp
                        : serverInSkyp;

                    var editedServer = allServers.EditItem(parentWindow, serverToEdit, servers, false);
                    if (editedServer == null)
                        return false;

                    if (!Equals(serverToEdit.URI, editedServer.URI))
                    {
                        allServers.Add(editedServer); // User may have changed the server Uri in the form
                    }
                    else
                    {
                        allServers.Remove(serverInSkyp);
                        allServers.Add(editedServer);
                    }

                    return Open(skyp.SkypPath, allServers, parentWindow);
                }

                return false;
            }
        }


        private void Download(SkypFile skyp, IProgressMonitor progressMonitor, FormEx parentWindow = null)
        {
            var progressStatus =
                new ProgressStatus(string.Format(Resources.SkypSupport_Download_Downloading__0_, skyp.SkylineDocUri));
            progressMonitor.UpdateProgress(progressStatus);

            var downloadClient = DownloadClientCreator.Create(progressMonitor, progressStatus);

            downloadClient.Download(skyp);

            if (progressMonitor.IsCanceled || downloadClient.IsError)
            {
                FileEx.SafeDelete(skyp.DownloadPath, true);
            }
        }
    }

    public class SkypDownloadException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public SkypDownloadException(string message, HttpStatusCode statusCode, Exception e) : base(message, e)
        {
            StatusCode = statusCode;
        }

        public bool Unauthorized()
        {
            return Unauthorized(StatusCode);
        }

        public static bool Unauthorized(HttpStatusCode statusCode)
        {
            return HttpStatusCode.Unauthorized.Equals(statusCode); // 401 -  No credentials provided or invalid credentials
        }

        public bool Forbidden()
        {
            return Forbidden(StatusCode);
        }

        public static bool Forbidden(HttpStatusCode statusCode)
        {
            return HttpStatusCode.Forbidden.Equals(statusCode); // 403 - Valid credentials but not enough permissions
        }

        public static SkypDownloadException Create(SkypFile skyp, Exception e)
        {
            var statusCode = GetStatusCode(e);
            var message = GetMessage(skyp, e, statusCode);
            return new SkypDownloadException(message, statusCode, e);
        }

        private static HttpStatusCode GetStatusCode(Exception e)
        {
            var webException = e as WebException;
            if (webException != null)
            {
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = webException.Response as HttpWebResponse;
                    if (response != null)
                    {
                        return response.StatusCode;
                    }
                }
            }

            return 0;
        }

        public static string GetMessage(SkypFile skyp, Exception ex, HttpStatusCode statusCode)
        {
            var message =
                string.Format(
                    Resources
                        .SkypSupport_Download_There_was_an_error_downloading_the_Skyline_document_specified_in_the_skyp_file___0__,
                    skyp.SkylineDocUri);

            if (ex != null)
            {
                var exceptionMsg = ex.Message;
                message = TextUtil.LineSeparate(message, exceptionMsg);
            }
           
            var serverName = skyp.GetServerName();

            if (Unauthorized(statusCode)) // 401 -  No credentials provided or invalid credentials
            {
                if (skyp.Server == null)
                {
                    message = TextUtil.LineSeparate(message, @"", string.Format("Would you like to add {0} as a Panorama server in Skyline?", serverName));
                }
                else if (skyp.UsernameMismatch())
                {
                    message = TextUtil.LineSeparate(message, @"",
                        string.Format(
                            "Credentials saved in Skyline for the Panorama server {0} are invalid. The .skyp file was downloaded by {1}. " +
                            "Credentials saved in Skyline for this server are for the user {2}. Would you like to update the credentials?",
                            serverName, skyp.User, skyp.Server.Username));
                }
                else
                {
                    message = TextUtil.LineSeparate(message, @"",
                        string.Format("Credentials saved in Skyline for the Panorama server {0} are invalid. Would you like to update the credentials?", serverName));
                }
            }
            else if (Forbidden(statusCode)) // 403 - Valid credentials but not enough permissions
            {
                if (skyp.UsernameMismatch() && skyp.Server != null)
                {
                    message = TextUtil.LineSeparate(message, @"",
                        string.Format(
                            "Credentials saved in Skyline for the Panorama server {0} are for the user {1}. This user does not have permissions to download the file." +
                            " The .skyp file was downloaded by {2}. Would you like to update the credentials saved in Skyline?",
                            serverName, skyp.Server.Username, skyp.User));
                }
                else
                {
                    message = TextUtil.LineSeparate(message, @"",
                        string.Format(
                            Resources.SkypSupport_Download_You_do_not_have_permissions_to_download_this_file_from__0__,
                            serverName));
                }
            }

            return message;
        }
    }

    public class WebDownloadClient : IDownloadClient
    {
        private IProgressMonitor ProgressMonitor { get; }
        private IProgressStatus ProgressStatus { get; set; }
        private bool DownloadComplete { get; set; }

        public bool IsCancelled => ProgressMonitor != null && ProgressMonitor.IsCanceled;
        public bool IsError => ProgressStatus != null && ProgressStatus.IsError;
        public Exception Error => ProgressStatus?.ErrorException;

        public WebDownloadClient(IProgressMonitor progressMonitor, IProgressStatus progressStatus)
        {
            ProgressMonitor = progressMonitor;
            ProgressStatus = progressStatus;
        }

        public void Download(SkypFile skyp)
        {
            using (var wc = new UTF8WebClient())
            {
                if (skyp.HasCredentials())
                {
                    wc.Headers.Add(HttpRequestHeader.Authorization, Server.GetBasicAuthHeader(skyp.Server.Username, skyp.Server.Password));
                }

                wc.DownloadProgressChanged += (s,e) =>
                {
                    // The Content-Length header is not set in the response from PanoramaWeb, so the ProgressPercentage remains 0
                    // during the download. If the .skyp includes the file size, use that to calculate the progress.
                    int progressPercent = e.ProgressPercentage > 0 ? e.ProgressPercentage : -1;
                    var fileSize = skyp.Size;
                    if (progressPercent == -1 && fileSize.HasValue && fileSize > 0)
                    {
                        progressPercent = Math.Max(0, Math.Min(100, (int)(e.BytesReceived * 100 / fileSize)));
                    }
                    ProgressMonitor.UpdateProgress(ProgressStatus = ProgressStatus.ChangePercentComplete(progressPercent));
                };

                wc.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null && !ProgressMonitor.IsCanceled)
                    {
                        ProgressMonitor.UpdateProgress(ProgressStatus = ProgressStatus.ChangeErrorException(SkypDownloadException.Create(skyp, e.Error)));
                    }

                    DownloadComplete = true;
                };

                wc.DownloadFileAsync(skyp.SkylineDocUri, skyp.DownloadPath);

                while (!DownloadComplete)
                {
                    if (ProgressMonitor.IsCanceled)
                    {
                        wc.CancelAsync();
                    }
                }
            }
        }
    }

    public interface IDownloadClient
    {
        void Download(SkypFile skyp);

        bool IsCancelled { get; }
        bool IsError { get; }
        Exception Error { get; }
    }

    public class DownloadClientCreator
    {
        public virtual IDownloadClient Create(IProgressMonitor progressMonitor, IProgressStatus progressStatus)
        {
            return new WebDownloadClient(progressMonitor, progressStatus);
        }
    }
}