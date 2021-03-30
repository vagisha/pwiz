﻿/*
 * Original author: Ali Marsh <alimarsh .at. uw.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
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
using System.IO;
using Microsoft.Win32;
using SkylineBatch.Properties;

namespace SkylineBatch
{

    public class RInstallations
    {
        // Finds and saves information about the computer's R installation locations
        private const string RegistryLocationR = @"SOFTWARE\R-core\R\";

        public static bool FindRDirectory()
        {
            if (Settings.Default.RDirs == null) Settings.Default.RDirs = new List<string>();
            if (Settings.Default.RDirs.Count == 0)
            {
                RegistryKey rKey = null;
                try
                {
                    rKey = Registry.LocalMachine.OpenSubKey(RegistryLocationR);
                }
                catch (Exception)
                {
                    // ignored
                }
                if (rKey == null)
                    return false;
                var latestRPath = rKey.GetValue(@"InstallPath") as string;
                Settings.Default.RDirs.Add(Path.GetDirectoryName(latestRPath));
            }

            Settings.Default.RVersions = GetRInstallationDict(Settings.Default.RDirs);
            return Settings.Default.RVersions.Count > 0;
        }

        public static void AddRDirectory(string newRDir)
        {
            if (!Directory.Exists(newRDir))
                throw new ArgumentException(string.Format(Resources.RInstallations_AddRDirectory_R_installation_directory_not_found___0_, newRDir) + Environment.NewLine +
                                            Resources.RInstallations_AddRDirectory_Please_enter_a_valid_directory_);
            if (Settings.Default.RDirs.Contains(newRDir))
                throw new ArgumentException(string.Format(Resources.RInstallations_AddRDirectory_The_R_directory_is_already_used_to_find_R_installations___0_, newRDir) + Environment.NewLine +
                                                          Resources.RInstallations_AddRDirectory_Please_enter_a_different_directory_);
            var newRVersions = GetRInstallationDict(new List<string> {newRDir}).Count;
            if (newRVersions == 0)
                throw new ArgumentException(string.Format(Resources.RInstallations_AddRDirectory_No_R_installations_were_found_in___0_, newRDir) + Environment.NewLine +
                                                          Resources.RInstallations_AddRDirectory_Please_choose_a_directory_with_R_installations_);
            Settings.Default.RDirs.Add(newRDir);
            Settings.Default.RVersions = GetRInstallationDict(Settings.Default.RDirs);
            Settings.Default.Save();
        }

        private static Dictionary<string, string> GetRInstallationDict(List<string> RDirs)
        {
            var rPaths = new Dictionary<string, string>();
            foreach (var RDir in RDirs)
            {
                string[] rVersions;
                try
                {
                    rVersions = Directory.GetDirectories(RDir);
                }
                catch (Exception)
                {
                    continue; // path was deleted/changed
                }

                foreach (var rVersion in rVersions)
                {
                    var folderName = Path.GetFileName(rVersion);
                    if (folderName.StartsWith("R-"))
                    {
                        var rScriptPath = rVersion + "\\bin\\Rscript.exe";
                        if (File.Exists(rScriptPath))
                        {
                            if (rPaths.ContainsKey(folderName.Substring(2)))
                                rPaths[folderName.Substring(2)] = rScriptPath;
                            else
                                rPaths.Add(folderName.Substring(2), rScriptPath);
                        }
                    }
                }
            }
            return rPaths;
        }
    }
}
