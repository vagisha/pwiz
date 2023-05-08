﻿/*
 * Original author: Shannon Joyner <saj9191 .at. gmail.com>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2012 University of Washington - Seattle, WA
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
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using pwiz.Common.SystemUtil;
using pwiz.PanoramaClient.Properties;

namespace pwiz.PanoramaClient
{
    public class PanoramaUtil
    {
        public const string PANORAMA_WEB = "https://panoramaweb.org/";
        public const string FORM_POST = "POST";
        public const string LABKEY_CTX = "/labkey/";
        public const string ENSURE_LOGIN_PATH = "security/home/ensureLogin.view";

        public static Uri ServerNameToUri(string serverName)
        {
            try
            {
                return new Uri(ServerNameToUrl(serverName));
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        private static string ServerNameToUrl(string serverName)
        {
            const string https = "https://";
            const string http = "http://";

            var httpsIndex = serverName.IndexOf(https, StringComparison.Ordinal);
            var httpIndex = serverName.IndexOf(http, StringComparison.Ordinal);

            if (httpsIndex == -1 && httpIndex == -1)
            {
                serverName = serverName.Insert(0, https);
            }

            return serverName;
        }

        public static bool TryGetJsonResponse(HttpWebResponse response, ref JObject jsonResponse)
        {
            using (var stream = response.GetResponseStream())
            {
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        var responseText = reader.ReadToEnd();
                        try
                        {
                            jsonResponse = JObject.Parse(responseText);
                            return true;
                        }
                        catch (JsonReaderException) {}
                    }
                }
            }

            return false;
        }

        public static bool IsValidEnsureLoginResponse(JObject jsonResponse, string expectedEmail)
        {
            // Example JSON response:
            /*
             * {
                  "currentUser" : {
                    "canUpdateOwn" : "false",
                    "canUpdate" : "false",
                    "canDeleteOwn" : "false",
                    "canInsert" : "false",
                    "displayName" : "test_user",
                    "canDelete" : "false",
                    "id" : 1166,
                    "isAdmin" : "false",
                    "email" : "test_user@uw.edu"
                  }
                }
             */
            jsonResponse.TryGetValue(@"currentUser", out JToken currentUser);
            if (currentUser != null)
            {
                var email = currentUser.Value<string>(@"email");
                return email != null && email.Equals(expectedEmail);
            }

            return false;
        }

        public static Uri GetEnsureLoginUri(PanoramaServer pServer)
        {
            return new Uri(pServer.URI, ENSURE_LOGIN_PATH);
        }

        public static void VerifyFolder(IPanoramaClient panoramaClient, PanoramaServer server, string panoramaFolder) // MOVE
        {
            switch (panoramaClient.IsValidFolder(panoramaFolder))
            {
                case FolderState.notfound:
                    throw new PanoramaServerException(
                        string.Format(
                            Resources.PanoramaUtil_VerifyFolder_Folder__0__does_not_exist_on_the_Panorama_server__1_,
                            panoramaFolder, panoramaClient.ServerUri));
                case FolderState.nopermission:
                    throw new PanoramaServerException(string.Format(
                        Resources
                            .PanoramaUtil_VerifyFolder_User__0__does_not_have_permissions_to_upload_to_the_Panorama_folder__1_,
                        server.Username, panoramaFolder));
                case FolderState.notpanorama:
                    throw new PanoramaServerException(string.Format(
                        Resources.PanoramaUtil_VerifyFolder__0__is_not_a_Panorama_folder,
                        panoramaFolder));
            }
        }

        /// <summary>
        /// Parses the JSON returned from the getContainers LabKey API to look for the folder type and active modules in a container.
        /// </summary>
        /// <param name="folderJson"></param>
        /// <returns>True if the folder is a Targeted MS folder.</returns>
        public static bool CheckFolderType(JToken folderJson)
        {
            if (folderJson != null)
            {

                var folderType = (string)folderJson[@"folderType"];
                var modules = folderJson[@"activeModules"];
                return modules != null && ContainsTargetedMSModule(modules) &&
                       Equals(@"Targeted MS", folderType);
            }

            return false;
        }

        /// <summary>
        /// Parses the JSON returned from the getContainers LabKey API to look for user permissions in the container.
        /// </summary>
        /// <param name="folderJson"></param>
        /// <returns>True if the user has insert permissions.</returns>
        public static bool CheckFolderPermissions(JToken folderJson)
        {
            if (folderJson != null)
            {
                var userPermissions = folderJson.Value<int?>(@"userPermissions");
                return userPermissions != null && Equals(userPermissions & 2, 2);
            }

            return false;
        }

        private static bool ContainsTargetedMSModule(IEnumerable<JToken> modules)
        {
            foreach (var module in modules)
            {
                if (string.Equals(module.ToString(), @"TargetedMS"))
                    return true;
            }

            return false;
        }

        public static Uri Call(Uri serverUri, string controller, string folderPath, string method, bool isApi = false)
        {
            return Call(serverUri, controller, folderPath, method, null, isApi);
        }

        public static Uri Call(Uri serverUri, string controller, string folderPath, string method, string query,
            bool isApi = false)
        {
            string path = controller + @"/" + (folderPath ?? string.Empty) + @"/" +
                          method + (isApi ? @".api" : @".view");

            if (!string.IsNullOrEmpty(query))
            {
                path = path + @"?" + query;
            }

            return new Uri(serverUri, path);
        }

        public static Uri CallNewInterface(Uri serverUri, string controller, string folderPath, string method,
            string query,
            bool isApi = false)
        {
            string apiString = isApi ? @"api" : @"view";
            string queryString = string.IsNullOrEmpty(query) ? "" : @"?" + query;
            string path = $@"{folderPath}/{controller}-{method}.{apiString}{queryString}";

            return new Uri(serverUri, path);
        }

        public static Uri GetContainersUri(Uri serverUri, string folder, bool includeSubfolders)
        {
            var queryString = string.Format(@"includeSubfolders={0}&moduleProperties=TargetedMS",
                includeSubfolders ? @"true" : @"false");
            return Call(serverUri, @"project", folder, @"getContainers", queryString);
        }

        public static IPanoramaClient CreatePanoramaClient(Uri serverUri, string userName, string password)
        {
            return new WebPanoramaClient(new PanoramaServer(serverUri, userName, password));
        }
    }

    public abstract class GenericState<T>
    {
        public T State { get; }
        public string Error { get; }
        public Uri Uri { get; }

        public abstract bool IsValid();

        protected string AppendErrorAndUri(string stateErrorMessage)
        {
            var message = stateErrorMessage;

            if (Error != null || Uri != null)
            {
                var sb = new StringBuilder();

                if (Error != null)
                {
                    sb.AppendLine(string.Format(Resources.GenericState_AppendErrorAndUri_Error___0_, Error));
                }

                if (Uri != null)
                {
                    sb.AppendLine(string.Format(Resources.GenericState_AppendErrorAndUri_URL___0_, Uri));
                }

                message = TextUtil.LineSeparate(message, string.Empty, sb.ToString());
            }


            return message;
        }

        public GenericState(T state, string error, Uri uri)
        {
            State = state;
            Error = error;
            Uri = uri;
        }
    }

    public class ServerState : GenericState<ServerStateEnum>
    {
        public static readonly ServerState VALID = new ServerState(ServerStateEnum.available, null, null);

        public ServerState(ServerStateEnum state, string error, Uri uri) : base(state, error, uri)
        {
        }

        public override bool IsValid()
        {
            return State == ServerStateEnum.available;
        }

        public string GetErrorMessage(Uri serverUri)
        {
            var stateError = string.Empty;
            switch (State)
            {
                case ServerStateEnum.missing:
                    stateError = string.Format(
                        Resources.ServerState_GetErrorMessage_The_server__0__does_not_exist_,
                        serverUri.AbsoluteUri);
                    break;
                case ServerStateEnum.unknown:
                    stateError = string.Format(
                        Resources.ServerState_GetErrorMessage_Unable_to_connect_to_the_server__0__,
                        serverUri.AbsoluteUri);
                    break;
            }

            return AppendErrorAndUri(stateError);
        }
    }

    public class UserState : GenericState<UserStateEnum>
    {
        public static readonly UserState VALID = new UserState(UserStateEnum.valid, null, null);

        public UserState(UserStateEnum state, string error, Uri uri) : base(state, error, uri)
        {
        }

        public override bool IsValid()
        {
            return State == UserStateEnum.valid;
        }

        public string GetErrorMessage(Uri serverUri)
        {
            var stateError = string.Empty;
            switch (State)
            {
                case UserStateEnum.nonvalid:
                    stateError = Resources.UserState_GetErrorMessage_The_username_and_password_could_not_be_authenticated_with_the_panorama_server_;
                    break;
                case UserStateEnum.unknown:
                    stateError = string.Format(
                        Resources.UserState_GetErrorMessage_There_was_an_error_authenticating_user_credentials_on_the_server__0__,
                        serverUri.AbsoluteUri);
                    break;
            }

            return AppendErrorAndUri(stateError);
        }
    }

    public enum ServerStateEnum { unknown, missing, available }
    public enum UserStateEnum { valid, nonvalid, unknown }
    public enum FolderState { valid, notpanorama, nopermission, notfound }
    public enum FolderOperationStatus { OK, notpanorama, nopermission, notfound, alreadyexists, error }

    public interface IPanoramaClient
    {
        PanoramaServer PanoramaServer { get; }
        Uri ServerUri { get; }

        void ValidateServer();
        ServerState GetServerState();
        UserState IsValidUser();
        FolderState IsValidFolder(string folderPath);
    
        /**
         * Returns FolderOperationStatus.OK if created successfully, otherwise returns the reason
         * why the folder was not created.
         */
        FolderOperationStatus CreateFolder(string parentPath, string folderName);
        /**
         * Returns FolderOperationStatus.OK if the folder was successfully deleted, otherwise returns the reason
         * why the folder was not deleted.
         */
        FolderOperationStatus DeleteFolder(string folderPath);
    
        JToken GetInfoForFolders(string folder);
    }

    public abstract class AbstractPanoramaClient : IPanoramaClient
    {
        public abstract PanoramaServer PanoramaServer { get; }
        public abstract Uri ServerUri { get; }

        public virtual void ValidateServer()
        {
            throw new NotImplementedException();
        }

        public virtual ServerState GetServerState()
        {
            throw new NotImplementedException();
        }

        public virtual UserState IsValidUser()
        {
            throw new NotImplementedException();
        }

        public virtual FolderState IsValidFolder(string folderPath)
        {
            throw new NotImplementedException();
        }

        public virtual FolderOperationStatus CreateFolder(string parentPath, string folderName)
        {
            throw new NotImplementedException();
        }

        public virtual FolderOperationStatus DeleteFolder(string folderPath)
        {
            throw new NotImplementedException();
        }

        public virtual JToken GetInfoForFolders(string folder)
        {
            throw new NotImplementedException();
        }
    }
    
    public class WebPanoramaClient : IPanoramaClient
    {
        public PanoramaServer PanoramaServer { get; set; }
        public Uri ServerUri => PanoramaServer?.URI;
        // private IProgressMonitor ProgressMonitor { get; set; }
        // private IProgressStatus ProgressStatus { get; set; }
        // public bool Success { get; private set; } = true;

        public WebPanoramaClient(Uri serverUri, string userName, string password) : this(new PanoramaServer(serverUri, userName, password))
        {
        }

        public WebPanoramaClient(PanoramaServer server)
        {
            PanoramaServer = server;
        }
    
        public ServerState GetServerState()
        {
            return TryGetServerState();
        }
    
        private ServerState TryGetServerState(bool tryNewProtocol = true)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    webClient.DownloadString(ServerUri);
                    return ServerState.VALID;
                }
            }
            catch (WebException ex)
            {
                // Invalid URL
                if (ex.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    return new ServerState(ServerStateEnum.missing, ex.Message, ServerUri);
                }
                else if (tryNewProtocol)
                {
                    if (TryNewProtocol(() => TryGetServerState(false).IsValid()))
                        return ServerState.VALID;
    
                    return new ServerState(ServerStateEnum.unknown, ex.Message, ServerUri);
                }
            }
            return new ServerState(ServerStateEnum.unknown, null, ServerUri);
        }
    
        // This function must be true/false returning; no exceptions can be thrown
        private bool TryNewProtocol(Func<bool> testFunc)
        {
            var currentServer = PanoramaServer;
    
            // try again using https
            if (ServerUri.Scheme.Equals(@"http"))
            {
                PanoramaServer = PanoramaServer.ChangeUri(new Uri(currentServer.URI.AbsoluteUri.Replace(@"http", @"https")));
                // ServerUri = new Uri(currentUri.AbsoluteUri.Replace(@"http", @"https"));
                return testFunc();
            }
            // We assume "https" (PanoramaUtil.ServerNameToUrl) if there is no scheme in the user provided URL.
            // Try http. LabKey Server may not be running under SSL. 
            else if (ServerUri.Scheme.Equals(@"https"))
            {
                PanoramaServer = PanoramaServer.ChangeUri(new Uri(currentServer.URI.AbsoluteUri.Replace(@"https", @"http")));
                return testFunc();
            }
    
            PanoramaServer = currentServer;
            return false;
        }
    
        public UserState IsValidUser()
        {
            var refServerUri = ServerUri;
            var userState = ValidateServerAndUser(ref refServerUri);
            if (userState.IsValid())
            {
                PanoramaServer = PanoramaServer.ChangeUri(refServerUri);
            }
            return userState;
        }

        public FolderState IsValidFolder(string folderPath)
        {
            try
            {
                var uri = PanoramaUtil.GetContainersUri(ServerUri, folderPath, false);
    
                using (var webClient = new WebClientWithCredentials(ServerUri, PanoramaServer.Username, PanoramaServer.Password))
                {
                    JToken response = webClient.Get(uri);
    
                    // User needs write permissions to publish to the folder
                    if (!PanoramaUtil.CheckFolderPermissions(response))
                    {
                        return FolderState.nopermission;
                    }
    
                    // User can only upload to a TargetedMS folder type.
                    if (!PanoramaUtil.CheckFolderType(response))
                    {
                        return FolderState.notpanorama;
                    }
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return FolderState.notfound;
                }
                else throw;
            }
            return FolderState.valid;
        }
    
        public FolderOperationStatus CreateFolder(string folderPath, string folderName)
        {
    
            if (IsValidFolder($@"{folderPath}/{folderName}") == FolderState.valid)
                return FolderOperationStatus.alreadyexists;        //cannot create a folder with the same name
            var parentFolderStatus = IsValidFolder(folderPath);
            switch (parentFolderStatus)
            {
                case FolderState.nopermission:
                    return FolderOperationStatus.nopermission;
                case FolderState.notfound:
                    return FolderOperationStatus.notfound;
                case FolderState.notpanorama:
                    return FolderOperationStatus.notpanorama;
            }
    
            //Create JSON body for the request
            Dictionary<string, string> requestData = new Dictionary<string, string>();
            requestData[@"name"] = folderName;
            requestData[@"title"] = folderName;
            requestData[@"description"] = folderName;
            requestData[@"type"] = @"normal";
            requestData[@"folderType"] = @"Targeted MS";
            string createRequest = JsonConvert.SerializeObject(requestData);
    
            try
            {
                using (var webClient = new WebClientWithCredentials(ServerUri, PanoramaServer.Username, PanoramaServer.Password))
                {
                    Uri requestUri = PanoramaUtil.CallNewInterface(ServerUri, @"core", folderPath, @"createContainer", "", true);
                    JObject result = webClient.Post(requestUri, createRequest);
                    return FolderOperationStatus.OK;
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode != HttpStatusCode.OK)
                {
                    return FolderOperationStatus.error;
                }
                else throw;
            }
        }
    
        public FolderOperationStatus DeleteFolder(string folderPath)
        {
            var parentFolderStatus = IsValidFolder(folderPath);
            switch (parentFolderStatus)
            {
                case FolderState.nopermission:
                    return FolderOperationStatus.nopermission;
                case FolderState.notfound:
                    return FolderOperationStatus.notfound;
                case FolderState.notpanorama:
                    return FolderOperationStatus.notpanorama;
            }
    
            try
            {
                using (var webClient = new WebClientWithCredentials(ServerUri, PanoramaServer.Username, PanoramaServer.Password))
                {
                    Uri requestUri = PanoramaUtil.CallNewInterface(ServerUri, @"core", folderPath, @"deleteContainer", "", true);
                    JObject result = webClient.Post(requestUri, "");
                    return FolderOperationStatus.OK;
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode != HttpStatusCode.OK)
                {
                    return FolderOperationStatus.error;
                }
                else throw;
            }
        }

        private UserState EnsureLogin() // MOVE
        {
            var requestUri = PanoramaUtil.GetEnsureLoginUri(PanoramaServer);
            var request = (HttpWebRequest)WebRequest.Create(requestUri);
            request.Headers.Add(HttpRequestHeader.Authorization,
                PanoramaServer.GetBasicAuthHeader(PanoramaServer.Username, PanoramaServer.Password));
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        return new UserState(UserStateEnum.nonvalid,
                            string.Format(Resources.PanoramaUtil_EnsureLogin_Could_not_authenticate_user__Response_received_from_server___0___1_,
                                response.StatusCode, response.StatusDescription),
                            requestUri);
                    }

                    JObject jsonResponse = null;
                    if (PanoramaUtil.TryGetJsonResponse(response, ref jsonResponse) && PanoramaUtil.IsValidEnsureLoginResponse(jsonResponse, PanoramaServer.Username))
                    {
                        return UserState.VALID;
                    }
                    else if (jsonResponse == null)
                    {
                        return new UserState(UserStateEnum.unknown,
                            string.Format(Resources.PanoramaUtil_EnsureLogin_Server_did_not_return_a_valid_JSON_response___0__is_not_a_Panorama_server_, PanoramaServer.URI),
                            requestUri);
                    }
                    else
                    {
                        var jsonText = jsonResponse.ToString(Formatting.None);
                        jsonText = jsonText.Replace(@"{", @"{{"); // escape curly braces
                        return new UserState(UserStateEnum.unknown,
                            string.Format(Resources.PanoramaUtil_EnsureLogin_Unexpected_JSON_response_from_the_server___0_, jsonText),
                            requestUri);
                    }
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;

                if (response != null && response.StatusCode == HttpStatusCode.Unauthorized) // 401
                {
                    var responseUri = response.ResponseUri;
                    if (!requestUri.Equals(responseUri))
                    {
                        // This means we were redirected.  Authorization headers are not persisted across redirects. Try again
                        // with the responseUri.
                        if (PanoramaServer.Redirect(responseUri.AbsoluteUri, PanoramaUtil.ENSURE_LOGIN_PATH))
                        {
                            return EnsureLogin();
                        }
                    }

                    return new UserState(UserStateEnum.nonvalid, ex.Message, requestUri); // User cannot be authenticated
                }

                throw;
            }
        }

        public PanoramaServer EnsureLoginReturnServer()
        {
            var refServerUri = PanoramaServer.URI;
            UserState userState = ValidateServerAndUser(ref refServerUri);
            if (userState.IsValid())
            {
                return PanoramaServer.ChangeUri(refServerUri);
            }
            else
            {
                throw new PanoramaServerException(userState.GetErrorMessage(refServerUri));
            }
        }

        private UserState ValidateServerAndUser(ref Uri serverUri) // MOVE
        {
            var pServer = PanoramaServer;

            try
            {
                var userState = EnsureLogin();
                serverUri = pServer.URI;
                return userState;
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;

                if (response != null && response.StatusCode == HttpStatusCode.NotFound) // 404
                {
                    if (pServer.AddLabKeyContextPath())
                    {
                        // e.g. Given server URL is https://panoramaweb.org but LabKey Server is not deployed as the root webapp.
                        // Try again with '/labkey' context path
                        return TryEnsureLogin(pServer, ref serverUri);
                    }
                    else if (pServer.RemoveContextPath())
                    {
                        // e.g. User entered the home page of the LabKey Server, running as the root webapp: 
                        // https://panoramaweb.org/project/home/begin.view OR https://panoramaweb.org/home/project-begin.view
                        // We will first try https://panoramaweb.org/project/ OR https://panoramaweb.org/home/ as the server URL. 
                        // And that will fail.  Remove the assumed context path and try again.
                        return TryEnsureLogin(pServer, ref serverUri);
                    }
                }

                return new UserState(UserStateEnum.unknown, ex.Message, PanoramaUtil.GetEnsureLoginUri(pServer));
            }
        }

        private UserState TryEnsureLogin(PanoramaServer pServer, ref Uri serverUri) // MOVE
        {
            try
            {
                var userState = EnsureLogin();
                serverUri = pServer.URI;
                return userState;
            }
            catch (WebException e)
            {
                // Due to anything other than 401 (Unauthorized), which is handled in EnsureLogin.
                return new UserState(UserStateEnum.unknown, e.Message, PanoramaUtil.GetEnsureLoginUri(pServer));
            }
        }

        public void ValidateServer() // MOVE
        {
            var uriServer = ServerUri;

            var serverState = GetServerState();
            if (!serverState.IsValid())
            {
                throw new PanoramaServerException(serverState.GetErrorMessage(uriServer));
            }

            var userState = IsValidUser();
            if (!userState.IsValid())
            {
                throw new PanoramaServerException(userState.GetErrorMessage(uriServer));
            }
        }

        public JToken GetInfoForFolders(string folder)
        {
            if (PanoramaServer.HasUserCredentials())
            {
                PanoramaServer = EnsureLoginReturnServer();
            }
           
    
            // Retrieve folders from server.
            Uri uri = PanoramaUtil.GetContainersUri(PanoramaServer.URI, folder, true);
    
            using (var webClient = new WebClientWithCredentials(PanoramaServer.URI, PanoramaServer.Username, PanoramaServer.Password))
            {
                return webClient.Get(uri);
            }
        }

        /// <summary>
        /// Downloads a given file to a given folder path and shows the progress
        /// of the download during downloading
        /// </summary>
        /// <param name="path"></param>
        /// <param name="server"></param>
        /// <param name="downloadName"></param>
        /// <param name="pm"></param>
        /// <param name="size"></param>
        /// <param name="fileName"></param>
        public void DownloadFile(string path, PanoramaServer server, string downloadName, IProgressMonitor pm, IProgressStatus  progressStatus, long size, string fileName)
        {
            path = GetDownloadName(path);
            // DownloadPath = path;
            
            using var wc = new WebClientWithCredentials(server.URI, server.Username, server.Password);
            wc.DownloadProgressChanged += (s, e) =>
            {
                var progressPercent = e.ProgressPercentage > 0 ? e.ProgressPercentage : -1;
                if (progressPercent == -1 && size > 0)
                {
                    progressPercent = (int)(e.BytesReceived * 100 / size);
                }
                var downloaded = e.BytesReceived;
                var message = TextUtil.LineSeparate(
                    string.Format("Downloading {0}", fileName),
                    string.Empty,
                    GetDownloadedSize(downloaded, size > 0 ? (long)size : 0));
                progressStatus = progressStatus.ChangeMessage(message);
                pm.UpdateProgress(progressStatus = progressStatus.ChangePercentComplete(progressPercent));
            };
            var downloadComplete = false;
            wc.DownloadFileCompleted += (s, e) =>
            {
                if (e.Error != null && !pm.IsCanceled)
                {
                    pm.UpdateProgress(progressStatus = progressStatus.ChangeErrorException(e.Error));
                }
                downloadComplete = true;
            };
            wc.DownloadFileAsync(

                // Param1 = Link of file
                new Uri(downloadName),
                // Param2 = Path to save
                path
            );
            while (!downloadComplete)
            {
                if (pm.IsCanceled)
                {
                    wc.CancelAsync();
                }
                Thread.Sleep(100);
            }
        }

        private string GetDownloadName(string fullPath)
        {
            var count = 1;
            var fileName = fullPath;
            var extension = Path.GetExtension(fullPath);
            if (fullPath.EndsWith(".sky.zip"))
            {
                extension = ".sky.zip";
                fileName = fileName.Replace(".sky.zip", string.Empty);
            }
            fileName = Path.GetFileNameWithoutExtension(fileName);
            
            var newName = fullPath;
            var path = Path.GetDirectoryName(fullPath);
            while (File.Exists(newName))
            {
                var formattedName = string.Format("{0}({1})", fileName, count++);
                newName = Path.Combine(path, formattedName + extension);
            }
            return newName;
        }

        /// <summary>
        /// Borrowed from SkypSupport.cs, displays download progress
        /// </summary>
        /// <param name="downloaded"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public static string GetDownloadedSize(long downloaded, long fileSize)
        {
            var formatProvider = new FileSizeFormatProvider();
            if (fileSize > 0)
            {
                return string.Format(@"{0} / {1}", string.Format(formatProvider, @"{0:fs1}", downloaded), string.Format(formatProvider, @"{0:fs1}", fileSize));
            }
            else
            {
                return string.Format(formatProvider, @"{0:fs1}", downloaded);
            }
        }

    }

    public class PanoramaServerException : Exception
    {
        public PanoramaServerException(string message) : base(message)
        {
        }
    }

    public class FolderInformation
    {
        private readonly PanoramaServer _server;
        private readonly bool _hasWritePermission;

        public FolderInformation(PanoramaServer server, bool hasWritePermission)
        {
            _server = server;
            _hasWritePermission = hasWritePermission;
        }

        public PanoramaServer Server
        {
            get { return _server; }
        }

        public bool HasWritePermission
        {
            get { return _hasWritePermission; }
        }
    }

    public class UTF8WebClient : WebClient
    {
        public UTF8WebClient()
        {
            Encoding = Encoding.UTF8;
        }

        public JObject Get(Uri uri)
        {
            var response = DownloadString(uri);
            return JObject.Parse(response);
        }
    }

    public class WebClientWithCredentials : UTF8WebClient
    {
        private CookieContainer _cookies = new CookieContainer();
        private string _csrfToken;
        private Uri _serverUri;
    
        private static string LABKEY_CSRF = @"X-LABKEY-CSRF";
    
        public WebClientWithCredentials(Uri serverUri, string username, string password)
        {
            // Add the Authorization header
            Headers.Add(HttpRequestHeader.Authorization, PanoramaServer.GetBasicAuthHeader(username, password));
            _serverUri = serverUri;
        }
    
        public JObject Post(Uri uri, NameValueCollection postData)
        {
            if (string.IsNullOrEmpty(_csrfToken))
            {
                // After this the client should have the X-LABKEY-CSRF token 
                DownloadString(new Uri(_serverUri, PanoramaUtil.ENSURE_LOGIN_PATH));
            }
            if (postData == null)
            {
                postData = new NameValueCollection();
            }
            var responseBytes = UploadValues(uri, PanoramaUtil.FORM_POST, postData);
            var response = Encoding.UTF8.GetString(responseBytes);
            return JObject.Parse(response);
        }
    
        public JObject Post(Uri uri, string postData)
        {
            if (string.IsNullOrEmpty(_csrfToken))
            {
                // After this the client should have the X-LABKEY-CSRF token 
                DownloadString(new Uri(_serverUri, PanoramaUtil.ENSURE_LOGIN_PATH));
            }
            Headers.Add(HttpRequestHeader.ContentType, "application/json");
            var response = UploadString(uri, PanoramaUtil.FORM_POST, postData);
            return JObject.Parse(response);
        }
    
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
    
            var httpWebRequest = request as HttpWebRequest;
            if (httpWebRequest != null)
            {
                httpWebRequest.CookieContainer = _cookies;
    
                if (request.Method == PanoramaUtil.FORM_POST)
                {
                    if (!string.IsNullOrEmpty(_csrfToken))
                    {
                        // All POST requests to LabKey Server will be checked for a CSRF token
                        request.Headers.Add(LABKEY_CSRF, _csrfToken);
                    }
                }
            }
            return request;
        }
    
        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var response = base.GetWebResponse(request);
            var httpResponse = response as HttpWebResponse;
            if (httpResponse != null)
            {
                GetCsrfToken(httpResponse);
            }
            return response;
        }
    
        private void GetCsrfToken(HttpWebResponse response)
        {
            if (!string.IsNullOrEmpty(_csrfToken))
            {
                return;
            }
    
            var csrf = response.Cookies[LABKEY_CSRF];
            if (csrf != null)
            {
                // The server set a cookie called X-LABKEY-CSRF, get its value
                _csrfToken = csrf.Value;
            }
        }
    }

    public class PanoramaServer : Immutable
    {
        public Uri URI { get; protected set; }
        public string Username { get; protected set; }
        public string Password { get; protected set; }

        protected PanoramaServer()
        {
        }

        public PanoramaServer(Uri serverUri) : this(serverUri, null, null)
        {
        }

        public PanoramaServer(Uri serverUri, string username, string password)
        {
            Username = username;
            Password = password;

            var path = serverUri.AbsolutePath;

            if (path.Length > 1)
            {
                // Get the context path (e.g. /labkey) from the path
                var idx = path.IndexOf(@"/", 1, StringComparison.Ordinal);
                if (idx != -1 && path.Length > idx + 1)
                {
                    path = path.Substring(0, idx + 1);
                }
            }

            // Need trailing '/' for correct URIs with new Uri(baseUri, relativeUri) method
            // With no trailing '/', new Uri("https://panoramaweb.org/labkey", "project/getContainers.view") will
            // return https://panoramaweb.org/project/getContainers.view (no labkey)
            // ReSharper disable LocalizableElement
            path = path + (path.EndsWith("/") ? "" : "/");
            // ReSharper restore LocalizableElement

            URI = new UriBuilder(serverUri) { Path = path, Query = string.Empty, Fragment = string.Empty }.Uri;
        }

        public string AuthHeader => GetBasicAuthHeader(Username, Password);

        public PanoramaServer ChangeUri(Uri uri)
        {
            return ChangeProp(ImClone(this), im => im.URI = uri);
        }

        public bool HasUserCredentials()
        {
            return Username != null;
        }

        public bool RemoveContextPath()
        {
            if (!URI.AbsolutePath.Equals(@"/"))
            {
                URI = new UriBuilder(URI) { Path = @"/" }.Uri;
                return true;
            }
            return false;
        }

        public bool AddLabKeyContextPath()
        {
            if (URI.AbsolutePath.Equals(@"/"))
            {
                URI = new UriBuilder(URI) { Path = PanoramaUtil.LABKEY_CTX }.Uri;
                return true;
            }
            return false;
        }

        public bool Redirect(string redirectUri, string panoramaActionPath)
        {
            if (!Uri.IsWellFormedUriString(redirectUri, UriKind.Absolute))
            {
                return false;
            }

            var idx = redirectUri.IndexOf(panoramaActionPath, StringComparison.Ordinal);
            if (idx != -1)
            {
                var newUri = new Uri(redirectUri.Remove(idx));
                if (!URI.Host.Equals(newUri.Host))
                {
                    return false;
                }

                URI = newUri;
                return true;
            }
            return false;
        }

        public static string getFolderPath(PanoramaServer server, Uri serverPlusPath)
        {
            var path = serverPlusPath.AbsolutePath;
            var contextPath = server.URI.AbsolutePath;
            return path.StartsWith(contextPath) ? path.Remove(0, contextPath.Length) : path;
        }

        public static string GetBasicAuthHeader(string username, string password)
        {
            byte[] authBytes = Encoding.UTF8.GetBytes(String.Format(@"{0}:{1}", username, password));
            var authHeader = @"Basic " + Convert.ToBase64String(authBytes);
            return authHeader;
        }
    }
}
