/*
 * Original author: Vagisha Sharma <vsharma .at. uw.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 * Copyright 2015 University of Washington - Seattle, WA
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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using AutoQC.Properties;
using SharedBatch;
using pwiz.Common.SystemUtil;

namespace AutoQC
{
    [XmlRoot("main_settings")]
    public class MainSettings : Immutable
    {
        public const string XML_EL = "main_settings";

        public const int ACCUM_TIME_WINDOW = 31;
        public const int ACQUISITION_TIME = 75;
        public const string THERMO = "Thermo";
        public const string WATERS = "Waters";
        public const string SCIEX = "SCIEX";
        public const string AGILENT = "Agilent";
        public const string BRUKER = "Bruker";
        public const string SHIMADZU = "Shimadzu";

        // Default getters
        public static FileFilter GetDefaultQcFileFilter() { return FileFilter.GetFileFilter(AllFileFilter.FilterName, string.Empty); }
        public static bool GetDefaultRemoveResults() { return true; }
        public static string GetDefaultResultsWindow() { return ACCUM_TIME_WINDOW.ToString(); }
        public static string GetDefaultInstrumentType() { return THERMO; }
        public static string GetDefaultAcquisitionTime() { return ACQUISITION_TIME.ToString(); }


        public readonly string SkylineFilePath;
        public readonly string FolderToWatch;
        public readonly bool IncludeSubfolders;
        public readonly FileFilter QcFileFilter;
        public readonly bool RemoveResults;
        public readonly int ResultsWindow;
        public string InstrumentType { get; private set; }
        public readonly int AcquisitionTime;
        public LockMassParameters LockMassParameters { get; private set; }


        public MainSettings(string skylineFilePath, string folderToWatch, bool includeSubFolders, FileFilter qcFileFilter, 
            bool removeResults, string resultsWindowString, string instrumentType, string acquisitionTimeString,
            LockMassParameters lockMassParameters = null)
        {
            SkylineFilePath = skylineFilePath;
            FolderToWatch = folderToWatch;
            IncludeSubfolders = includeSubFolders;
            QcFileFilter = qcFileFilter;
            RemoveResults = removeResults;
            ResultsWindow = ValidateIntTextField(resultsWindowString, Resources.MainSettings_MainSettings_results_window);
            InstrumentType = instrumentType;
            AcquisitionTime = ValidateIntTextField(acquisitionTimeString, Resources.MainSettings_MainSettings_acquisition_time);
            LockMassParameters = lockMassParameters;
        }

        public virtual bool IsSelected()
        {
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Skyline file: ").AppendLine(SkylineFilePath);
            sb.Append("Folder to watch: ").AppendLine(FolderToWatch);
            sb.Append("Include subfolders: ").AppendLine(IncludeSubfolders.ToString());
            sb.AppendLine(QcFileFilter.ToString());
            sb.Append("Instrument: ").AppendLine(InstrumentType);
            if (RemoveResults)
            {
                sb.Append("Remove results older than: ").Append(ResultsWindow.ToString()).AppendLine(" days");
            }
            else
            {
                sb.AppendLine("Remove older results: No");
            }
            sb.Append("Acquisition time: ").Append(AcquisitionTime.ToString()).AppendLine(" minutes");
            if (LockMassParameters != null)
            {
                sb.Append(LockMassParameters);
            }
            return sb.ToString();
        }

        private int ValidateIntTextField(string textToParse, string fieldName)
        {
            int parsedInt;
            if (!Int32.TryParse(textToParse, out parsedInt))
            {
                throw new ArgumentException(string.Format(
                    Resources.MainSettings_ValidateIntTextField_Invalid_value_for__0____1_, fieldName,
                    textToParse));
            }
            return parsedInt;
        }

        public void ValidateSettings()
        {
            // Path to the Skyline file.
            ValidateSkylineFile(SkylineFilePath);

            // Path to the folder to monitor for mass spec. results files
            ValidateFolderToWatch(FolderToWatch);

            // File filter
            if (!(QcFileFilter is AllFileFilter))
            {
                var pattern = QcFileFilter.Pattern;
                if (string.IsNullOrEmpty(pattern))
                {
                    var err = string.Format(Resources.MainSettings_ValidateSettings_The_file_filter___0___cannot_have_an_empty_pattern__Please_enter_a_pattern_, QcFileFilter.Name());
                    throw new ArgumentException(err);  
                }
            }

            // Results time window.
            if (ResultsWindow < ACCUM_TIME_WINDOW)
            {
                throw new ArgumentException(string.Format(Resources.MainSettings_ValidateSettings__Results_time_window__cannot_be_less_than__0__days_,
                    ACCUM_TIME_WINDOW) + Environment.NewLine + 
                    string.Format(Resources.MainSettings_ValidateSettings_Please_enter_a_value_greater_than_or_equal_to__0__, ACCUM_TIME_WINDOW));
            }
            try
            {
                var unused = DateTime.Now.AddDays(-(ResultsWindow - 1));
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException(Resources.MainSettings_ValidateSettings_The_results_time_window_is_too_big__Please_enter_a_smaller_number_);
            }

            // Expected acquisition time
            if (AcquisitionTime < 0)
            {
                throw new ArgumentException(Resources.MainSettings_ValidateSettings__Expected_acquisition_time__cannot_be_less_than_0_minutes_ +Environment.NewLine +
                      string.Format(Resources.MainSettings_ValidateSettings_Please_enter_a_value_greater_than_or_equal_to__0__, 0));
            }
        }

        public static void ValidateSkylineFile(string skylineFile)
        {
            if (string.IsNullOrWhiteSpace(skylineFile))
            {
                throw new ArgumentException(Resources.MainSettings_ValidateSkylineFile_Skyline_file_name_cannot_be_empty__Please_specify_path_to_a_Skyline_file_);
            }
            if (!File.Exists(skylineFile))
            {
                throw new ArgumentException(string.Format(Resources.MainSettings_ValidateSkylineFile_The_Skyline_file__0__does_not_exist_, skylineFile) + Environment.NewLine +
                                            Resources.MainSettings_ValidateSkylineFile_Please_enter_a_path_to_an_existing_file_);
            }
        }

        public static void ValidateFolderToWatch(string folderToWatch)
        {
            if(string.IsNullOrWhiteSpace(folderToWatch))
            {
                throw new ArgumentException(Resources.MainSettings_ValidateFolderToWatch_The_folder_to_watch_cannot_be_empty__Please_specify_path_to_a_folder_where_mass_spec__files_will_be_written_);
            }
            if (!Directory.Exists(folderToWatch))
            {
                throw new ArgumentException(string.Format(Resources.MainSettings_ValidateFolderToWatch_The_folder_to_watch___0__does_not_exist_, folderToWatch) + Environment.NewLine +
                                            Resources.MainSettings_ValidateFolderToWatch_Please_enter_a_path_to_an_existing_folder_);
            }
        }

        public void Validate()
        {
            ValidateSettings();
        }

        public MainSettings ChangeInstrument(string instrumentType)
        {
            return ChangeProp(ImClone(this), im => im.InstrumentType = instrumentType);
        }

        public MainSettings ChangeWatersLockmassParams(LockMassParameters lockmassParams)
        {
            return ChangeProp(ImClone(this), im => im.LockMassParameters = lockmassParams);
        }


        #region Implementation of IXmlSerializable interface

        private enum Attr
        {
            skyline_file_path,
            folder_to_watch,
            include_subfolders,
            file_filter_type,
            qc_file_pattern,
            remove_results,
            results_window,
            instrument_type,
            acquisition_time,
            lockmass_positive,
            lockmass_negative,
            lockmass_tolerance
        };

        public XmlSchema GetSchema()
        {
            return null;
        }

        public static MainSettings ReadXml(XmlReader reader)
        {
            var skylineFilePath = reader.GetAttribute(Attr.skyline_file_path);
            var folderToWatch = reader.GetAttribute(Attr.folder_to_watch);
            var includeSubfolders = reader.GetBoolAttribute(Attr.include_subfolders);
            var pattern = reader.GetAttribute(Attr.qc_file_pattern);
            var filterType = reader.GetAttribute(Attr.file_filter_type);
            if (string.IsNullOrEmpty(filterType) && !string.IsNullOrEmpty(pattern))
            {
                // Support for older version where filter type was not written to XML; only regex filters were allowed
                filterType = RegexFilter.FilterName;
            }
            var qcFileFilter = FileFilter.GetFileFilter(filterType, pattern);
            var removeResults = reader.GetBoolAttribute(Attr.remove_results, true);
            var resultsWindow = reader.GetAttribute(Attr.results_window);
            var instrumentType = reader.GetAttribute(Attr.instrument_type);
            var acquisitionTime = reader.GetAttribute(Attr.acquisition_time);

            // Waters lockmass correction parameters
            var lockmassPositive = reader.GetDoubleAttribute(Attr.lockmass_positive);
            var lockmassNegative = reader.GetDoubleAttribute(Attr.lockmass_negative);
            var lockmassTolerance = reader.GetDoubleAttribute(Attr.lockmass_tolerance);
            var lockmassParams = new LockMassParameters(lockmassPositive, lockmassNegative, lockmassTolerance);

            // Return unvalidated settings. Validation can throw an exception that will cause the config to not get read fully and it will not be added to the config list
            // We want the user to be able to fix invalid configs.
            return new MainSettings(skylineFilePath, folderToWatch, includeSubfolders, 
                qcFileFilter, removeResults, resultsWindow, instrumentType, 
                acquisitionTime, lockmassParams.IsEmpty ? null : lockmassParams);
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(XML_EL);
            writer.WriteAttributeIfString(Attr.skyline_file_path, SkylineFilePath);
            writer.WriteAttributeIfString(Attr.folder_to_watch, FolderToWatch);
            writer.WriteAttribute(Attr.include_subfolders, IncludeSubfolders);
            writer.WriteAttributeIfString(Attr.qc_file_pattern, QcFileFilter.Pattern);
            writer.WriteAttributeString(Attr.file_filter_type, QcFileFilter.Name());   
            writer.WriteAttribute(Attr.remove_results, RemoveResults, true);
            writer.WriteAttributeNullable(Attr.results_window, ResultsWindow);
            writer.WriteAttributeIfString(Attr.instrument_type, InstrumentType);
            writer.WriteAttributeNullable(Attr.acquisition_time, AcquisitionTime);
            if (LockMassParameters != null && !LockMassParameters.IsEmpty)
            {
                writer.WriteAttributeNullable(Attr.lockmass_positive, LockMassParameters.LockmassPositive);
                writer.WriteAttributeNullable(Attr.lockmass_negative, LockMassParameters.LockmassNegative);
                writer.WriteAttributeNullable(Attr.lockmass_tolerance, LockMassParameters.LockmassTolerance);
            }
            writer.WriteEndElement();
        }
        #endregion

        #region Equality members

        protected bool Equals(MainSettings other)
        {
            return SkylineFilePath == other.SkylineFilePath && FolderToWatch == other.FolderToWatch &&
                   IncludeSubfolders == other.IncludeSubfolders && Equals(QcFileFilter, other.QcFileFilter) &&
                   RemoveResults == other.RemoveResults && ResultsWindow == other.ResultsWindow &&
                   InstrumentType == other.InstrumentType && AcquisitionTime == other.AcquisitionTime &&
                   Equals(LockMassParameters, other.LockMassParameters);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MainSettings) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (SkylineFilePath != null ? SkylineFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FolderToWatch != null ? FolderToWatch.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ IncludeSubfolders.GetHashCode();
                hashCode = (hashCode * 397) ^ (QcFileFilter != null ? QcFileFilter.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ RemoveResults.GetHashCode();
                hashCode = (hashCode * 397) ^ ResultsWindow;
                hashCode = (hashCode * 397) ^ AcquisitionTime;
                hashCode = (hashCode * 397) ^ (InstrumentType != null ? InstrumentType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LockMassParameters != null ? LockMassParameters.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }

    public class AccumulationWindow
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public static AccumulationWindow Get(DateTime endWindow, int windowSize)
        {
            if (windowSize < 1)
            {
                throw new ArgumentException(Resources.AccumulationWindow_Get_Results_time_window_size_has_be_greater_than_0_);
            }

            DateTime startDate;
            try
            {
                startDate = endWindow.AddDays(-(windowSize - 1));
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException(Resources.AccumulationWindow_Get_Results_time_window_is_too_big_);   
            }
            
            var window = new AccumulationWindow
            {
                EndDate = endWindow,
                StartDate = startDate
            };
            return window;
        }
    }

    public abstract class FileFilter
    {
        public abstract bool Matches(string path);
        public abstract string Name();
        public string Pattern { get; }

        protected FileFilter(string pattern)
        {
            Pattern = pattern ?? string.Empty;
        }

        public static FileFilter GetFileFilter(string filterType, string pattern)
        {
            if (filterType.Equals(StartsWithFilter.FilterName))
                return new StartsWithFilter(pattern);
            if(filterType.Equals(EndsWithFilter.FilterName))
                return new EndsWithFilter(pattern);
            if(filterType.Equals(ContainsFilter.FilterName))
                return new ContainsFilter(pattern);
            if(filterType.Equals(RegexFilter.FilterName))
                return new RegexFilter(pattern);

            return new AllFileFilter(string.Empty);
        }

        protected static string GetLastPathPartWithoutExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            // We need the last part of the path; it could be a directory or a file
            path = path.TrimEnd(Path.DirectorySeparatorChar);
            return Path.GetFileNameWithoutExtension(path); 
        }

        #region Overrides of Object

        public override string ToString()
        {
            var toStr = string.Format("Filter Type: {0}{1}", Name(),
                (!(string.IsNullOrEmpty(Pattern))) ? string.Format("; Pattern: {0}", Pattern) : string.Empty);
            return toStr;
        }

        #endregion

        #region Equality members

        protected bool Equals(FileFilter other)
        {
            return string.Equals(Pattern, other.Pattern);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileFilter) obj);
        }

        public override int GetHashCode()
        {
            return Pattern != null ? Pattern.GetHashCode() : 0;
        }

        #endregion
    }

    public class AllFileFilter : FileFilter
    {
        public static readonly string FilterName = Resources.AllFileFilter_FilterName_All;

        public AllFileFilter(string pattern)
            : base(pattern)
        {
        }

        #region Overrides of FileFilter
        public override bool Matches(string path)
        {
            return !string.IsNullOrEmpty(path);
        }

        public override string Name()
        {
            return FilterName;
        }

        #endregion
    }

    public class StartsWithFilter: FileFilter
    {
        public static readonly string FilterName = Resources.StartsWithFilter_FilterName_Starts_with;

        public StartsWithFilter(string pattern)
            : base(pattern)
        {
        }

        #region Overrides of FileFilter

        public override bool Matches(string path)
        {
            return GetLastPathPartWithoutExtension(path).StartsWith(Pattern);
        }

        public override string Name()
        {
            return FilterName;
        }

        #endregion
    }

    public class EndsWithFilter : FileFilter
    {
        public static readonly string FilterName = Resources.EndsWithFilter_FilterName_Ends_with;

        public EndsWithFilter(string pattern)
            : base(pattern)
        {
        }

        #region Overrides of FileFilter

        public override bool Matches(string path)
        {
            return GetLastPathPartWithoutExtension(path).EndsWith(Pattern);
        }

        public override string Name()
        {
            return FilterName;
        }
        #endregion
    }

    public class ContainsFilter : FileFilter
    {
        public static readonly string FilterName = Resources.ContainsFilter_FilterName_Contains;

        public ContainsFilter(string pattern)
            : base(pattern)
        {
        }

        #region Overrides of FileFilter

        public override bool Matches(string path)
        {
            return GetLastPathPartWithoutExtension(path).Contains(Pattern);
        }

        public override string Name()
        {
            return FilterName;
        }
        #endregion
    }

    public class RegexFilter : FileFilter
    {
        public static readonly string FilterName = Resources.RegexFilter_FilterName_Regular_expression;

        public readonly Regex Regex;

        public RegexFilter(string pattern)
            : base(pattern)
        {
            // Validate the regular expression
            try
            {
                Regex = new Regex(pattern);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(Resources.RegexFilter_RegexFilter_Invalid_regular_expression_for_QC_file_names, e);
            }  
        }

        #region Overrides of FileFilter

        public override bool Matches(string path)
        {
            return Regex.IsMatch(GetLastPathPartWithoutExtension(path));
        }

        public override string Name()
        {
            return FilterName;
        }
        #endregion
    }

    public interface IFileSystemUtil
    {
        IEnumerable<string> GetSkyZipFiles(string dirPath);
        DateTime LastWriteTime(string filePath);
    }

    public class FileSystemUtil : IFileSystemUtil
    {
        public IEnumerable<string> GetSkyZipFiles(string dirPath)
        {
            return Directory.GetFiles(dirPath, $"*{TextUtil.EXT_SKY_ZIP}");
        }

        public DateTime LastWriteTime(string filePath)
        {
            return File.GetLastWriteTime(filePath);
        }
    }

    /// <summary>
    /// For Waters lockmass correction
    /// </summary>
    public sealed class LockMassParameters : IComparable
    {
        public LockMassParameters(double? lockmassPositve, double? lockmassNegative, double? lockmassTolerance)
        {
            LockmassPositive = lockmassPositve;
            LockmassNegative = lockmassNegative;
            if (LockmassPositive.HasValue || LockmassNegative.HasValue)
            {
                LockmassTolerance = lockmassTolerance ?? LOCKMASS_TOLERANCE_DEFAULT;
            }
            else
            {
                LockmassTolerance = null;  // Means nothing when no mz is given
            }
        }

        public double? LockmassPositive { get; private set; }
        public double? LockmassNegative { get; private set; }
        public double? LockmassTolerance { get; private set; }

        public static readonly double LOCKMASS_TOLERANCE_DEFAULT = 0.1; // Per Will T
        public static readonly double LOCKMASS_TOLERANCE_MAX = 10.0;
        public static readonly double LOCKMASS_TOLERANCE_MIN = 0;

        public static readonly string POSITIVE = "ESI+";
        public static readonly string NEGATIVE = "ESI-";
        public static readonly string TOLERANCE = "Tolerance";

        public static readonly string POSITIVE_CMD_ARG = "import-lockmass-positive";
        public static readonly string NEGATIVE_CMD_ARG = "import-lockmass-negative";
        public static readonly string TOLERANCE_CMD_ARG = "import-lockmass-tolerance";

        public static readonly LockMassParameters EMPTY = new LockMassParameters(null, null, null);

        public bool IsEmpty
        {
            get
            {
                return (0 == (LockmassNegative ?? 0)) &&
                       (0 == (LockmassPositive ?? 0));
                // Ignoring tolerance here, which means nothing when no mz is given
            }
        }

        private bool Equals(LockMassParameters other)
        {
            return LockmassPositive.Equals(other.LockmassPositive) &&
                   LockmassNegative.Equals(other.LockmassNegative) &&
                   LockmassTolerance.Equals(other.LockmassTolerance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LockMassParameters && Equals((LockMassParameters)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = LockmassPositive.GetHashCode();
                result = (result * 397) ^ LockmassNegative.GetHashCode();
                result = (result * 397) ^ LockmassTolerance.GetHashCode();
                return result;
            }
        }

        public int CompareTo(LockMassParameters other)
        {
            if (ReferenceEquals(null, other))
                return -1;
            var result = Nullable.Compare(LockmassPositive, other.LockmassPositive);
            if (result != 0)
                return result;
            result = Nullable.Compare(LockmassNegative, other.LockmassNegative);
            if (result != 0)
                return result;
            return Nullable.Compare(LockmassTolerance, other.LockmassTolerance);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return -1;
            if (ReferenceEquals(this, obj)) return 0;
            if (obj.GetType() != GetType()) return -1;
            return CompareTo((LockMassParameters)obj);
        }

        public override string ToString()
        {
            if (IsEmpty) return "";
            var sb = new StringBuilder();
            sb.AppendLine("Waters lockmass correction parameters:");
            if (LockmassPositive.HasValue)
            {
                sb.Append(POSITIVE).Append(": ").Append(LockmassPositive);
            }
            if (LockmassNegative.HasValue)
            {
                sb.Append(NEGATIVE).Append(": ").Append(LockmassNegative);
            }
            if (LockmassTolerance.HasValue)
            {
                sb.Append(TOLERANCE).Append(": ").Append(LockmassTolerance);
            }
            
            return sb.ToString();
        }

        public string GetCommandLineParams()
        {
            var args = new StringBuilder();
            if (!IsEmpty)
            {
                if (LockmassPositive.HasValue)
                {
                    args.Append(string.Format(" --{0}={1}", POSITIVE_CMD_ARG, LockmassPositive));
                }
                if (LockmassNegative.HasValue)
                {
                    args.Append(string.Format(" --{0}={1}", NEGATIVE_CMD_ARG, LockmassNegative));
                }
                if (LockmassTolerance.HasValue)
                {
                    args.Append(string.Format(" --{0}={1}", TOLERANCE_CMD_ARG, LockmassTolerance));
                }
            }

            return args.ToString();
        }
    }
}