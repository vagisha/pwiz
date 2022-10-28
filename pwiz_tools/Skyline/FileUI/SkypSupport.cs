﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public const string ERROR401 = "(401) Unauthorized";
        public const string ERROR403 = "(403) Forbidden";
        private int _retryRemaining = 3;

        public SkypSupport(SkylineWindow skyline)
        {
            _skyline = skyline;
            DownloadClientCreator = new DownloadClientCreator();
        }

        public bool Open(string skypPath, IEnumerable<Server> servers, FormEx parentWindow = null)
        {
            SkypFile skyp = null;
            var existingServers = servers.ToList();
            try
            {
                skyp = SkypFile.Create(skypPath, existingServers);
                
                using (var longWaitDlg = new LongWaitDlg
                {
                    Text = Resources.SkypSupport_Open_Downloading_Skyline_Document_Archive,
                })
                {
                    longWaitDlg.PerformWork(parentWindow ?? _skyline, 1000, progressMonitor => Download(skyp, progressMonitor, parentWindow));
                    if (longWaitDlg.IsCanceled)
                        return false;
                }
                return _skyline.OpenSharedFile(skyp.DownloadPath);
            }
            catch (Exception e)
            {
                if (_retryRemaining-- > 0 && skyp != null && e.Message.Contains(AddPanoramaServerMessage(skyp)))
                {
                    return AddServerAndOpen(skyp, existingServers, e.Message, parentWindow);
                }
                else
                {
                    var message = TextUtil.LineSeparate(Resources.SkypSupport_Open_Failure_opening_skyp_file_, e.Message);
                    MessageDlg.ShowWithException(parentWindow ?? _skyline, message, e);
                    return false;
                }
            }
        }

        private bool AddServerAndOpen(SkypFile skypFile, IEnumerable<Server> existingServers, string message, FormEx parentWindow)
        {
            using (var alertDlg = new AlertDlg(message, MessageBoxButtons.OKCancel))
            {
                alertDlg.ShowDialog(parentWindow ?? _skyline);
                if (alertDlg.DialogResult == DialogResult.OK)
                {
                    var servers = Settings.Default.ServerList;
                    Server server = new Server(skypFile.SkylineDocUri.GetLeftPart(UriPartial.Authority), 
                        skypFile.User != null ? skypFile.User : string.Empty, string.Empty);
                    var newServer = servers.EditItem(parentWindow, server, existingServers, true);
                    if (newServer == null)
                        return false;

                    servers.Add(newServer);

                    return Open(skypFile.SkypPath, Settings.Default.ServerList /*get the updated server list*/, parentWindow);
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

            downloadClient.Download(skyp.SkylineDocUri, skyp.DownloadPath, skyp.Server?.Username, skyp.Server?.Password, skyp.Size);

            if (progressMonitor.IsCanceled || downloadClient.IsError)
            {
                FileEx.SafeDelete(skyp.DownloadPath, true);
            }
            if (downloadClient.IsError)
            {
                var message =
                    string.Format(
                        Resources
                            .SkypSupport_Download_There_was_an_error_downloading_the_Skyline_document_specified_in_the_skyp_file___0__,
                        skyp.SkylineDocUri);

                if (downloadClient.Error != null)
                {
                    var exceptionMsg = downloadClient.Error.Message;
                    message = TextUtil.LineSeparate(message, exceptionMsg);

                    if (exceptionMsg.Contains(ERROR401))
                    {
                        message = TextUtil.LineSeparate(message, AddPanoramaServerMessage(skyp));
                    }
                    else if (exceptionMsg.Contains(ERROR403))
                    {
                        message = TextUtil.LineSeparate(message,
                            string.Format(
                                Resources.SkypSupport_Download_You_do_not_have_permissions_to_download_this_file_from__0__,
                                skyp.SkylineDocUri.Host));
                    }
                }

                throw new Exception(message, downloadClient.Error);
            }
        }

        private static string AddPanoramaServerMessage(SkypFile skyp)
        {
            return string.Format(
                Resources
                    .SkypSupport_Download_You_may_have_to_add__0__as_a_Panorama_server_from_the_Tools___Options_menu_in_Skyline_,
                skyp.SkylineDocUri.Authority);
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

        public void Download(Uri remoteUri, string downloadPath, string username, string password, long? fileSize)
        {
            using (var wc = new UTF8WebClient())
            {
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    wc.Headers.Add(HttpRequestHeader.Authorization, Server.GetBasicAuthHeader(username, password));
                }

                wc.DownloadProgressChanged += (s,e) =>
                {
                    int progressPercent = e.ProgressPercentage > 0 ? e.ProgressPercentage : -1;
                    if (progressPercent == -1 && fileSize.HasValue && fileSize > 0)
                    {
                        progressPercent = Math.Min((int)(e.BytesReceived * 100 / fileSize), 100);
                    }
                    ProgressMonitor.UpdateProgress(ProgressStatus = ProgressStatus.ChangePercentComplete(progressPercent));
                };
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;

                wc.DownloadFileAsync(remoteUri, downloadPath);

                while (!DownloadComplete)
                {
                    if (ProgressMonitor.IsCanceled)
                    {
                        wc.CancelAsync();
                    }
                }
            }
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null && !ProgressMonitor.IsCanceled)
            {
                ProgressMonitor.UpdateProgress(ProgressStatus = ProgressStatus.ChangeErrorException(e.Error));
            }

            DownloadComplete = true;
        }

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressMonitor.UpdateProgress(ProgressStatus = ProgressStatus.ChangePercentComplete(e.ProgressPercentage > 0 ? e.ProgressPercentage : -1));
        }
    }

    public interface IDownloadClient
    {
        void Download(Uri remoteUri, string downloadPath, string username, string password, long? fileSize);

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