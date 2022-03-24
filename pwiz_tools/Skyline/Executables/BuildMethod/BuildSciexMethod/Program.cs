﻿/*
 * Original author: Kaipo Tamura <kaipot .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2022 University of Washington - Seattle, WA
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using SCIEX.Apis.Control.v1;
using SCIEX.Apis.Control.v1.DeviceMethods;
using SCIEX.Apis.Control.v1.DeviceMethods.Properties;
using SCIEX.Apis.Control.v1.DeviceMethods.Requests;
using SCIEX.Apis.Control.v1.DeviceMethods.Responses;
using SCIEX.Apis.Control.v1.Security.Requests;

namespace BuildSciexMethod
{
    class Program
    {
        public class UsageException : Exception
        {
            public UsageException() { }
            public UsageException(string message) : base(message) { }
        }

        private static void Main(string[] args)
        {
            try
            {
                Environment.ExitCode = -1;  // Failure until success
                using (var builder = new Builder())
                {
                    builder.ParseCommandArgs(args);
                    builder.BuildMethod();
                }
                Environment.ExitCode = 0;
            }
            catch (UsageException x)
            {
                if (!string.IsNullOrEmpty(x.Message))
                {
                    Console.Error.WriteLine(x.Message);
                    Console.Error.WriteLine();
                }
                Usage();
            }
            catch (Exception x)
            {
                Console.Error.WriteLine("ERROR: {0}", x.Message);
            }
        }

        private static void Usage()
        {
            const string usage =
                "Usage: BuildSciexMethod [options] <template method> [list file]*\n" +
                "   Takes template method file and a Skyline generated \n" +
                "   transition list as inputs, to generate a new method file\n" +
                "   as output.\n" +
                "   -d               Standard (unscheduled) method\n" +
                "   -o <output file> New method is written to the specified output file\n" +
                "   -s               Transition list is read from stdin.\n" +
                "                    e.g. cat TranList.csv | BuildSciexMethod -s -o new.ext temp.ext\n" +
                "\n" +
                "   -m               Multiple lists concatenated in the format:\n" +
                "                    file1.ext\n" +
                "                    <transition list>\n" +
                "\n" +
                "                    file2.ext\n" +
                "                    <transition list>\n" +
                "                    ...";
            Console.Error.Write(usage);
        }
    }

    public class Builder : IDisposable
    {
        private const string ServiceUri = "net.tcp://localhost:63333/SciexControlApiService";
        private readonly ISciexControlApi _api = SciexControlApiFactory.Create();
        private readonly List<MethodTransitions> _methodTrans = new List<MethodTransitions>();

        private string TemplateMethod { get; set; }
        private bool StandardMethod { get; set; }

        public void ParseCommandArgs(string[] args)
        {
            // Default to stdin for transition list input
            string outputMethod = null;
            var readStdin = false;
            var multiFile = false;

            var i = 0;
            while (i < args.Length && args[i][0] == '-')
            {
                switch (args[i++][1])
                {
                    case 'd':
                        StandardMethod = true;
                        break;
                    case 'o':
                        if (i >= args.Length)
                            throw new Program.UsageException();
                        outputMethod = Path.GetFullPath(args[i++]);
                        break;
                    case 's':
                        readStdin = true;
                        break;
                    case 'm':
                        multiFile = true;
                        break;
                    default:
                        throw new Program.UsageException();
                }
            }

            if (multiFile && !string.IsNullOrEmpty(outputMethod))
                throw new Program.UsageException("Multi-file and specific output are not compatibile.");

            var argcLeft = args.Length - i;
            if (argcLeft < 1 || (!readStdin && argcLeft < 2))
                throw new Program.UsageException();

            TemplateMethod = Path.GetFullPath(args[i++]);

            // Read input into a list of lists of fields
            if (readStdin)
            {
                if (!multiFile && string.IsNullOrEmpty(outputMethod))
                    throw new Program.UsageException("Reading from standard in without multi-file format must specify an output file.");

                ReadTransitions(Console.In, outputMethod);
            }
            else
            {
                for (; i < args.Length; i++)
                {
                    var inputFile = Path.GetFullPath(args[i]);
                    string filter = null;
                    if (inputFile.Contains("*"))
                        filter = Path.GetFileName(inputFile);
                    else if (Directory.Exists(inputFile))
                        filter = "*.csv";

                    if (string.IsNullOrEmpty(filter))
                        ReadFile(inputFile, outputMethod, multiFile);
                    else
                    {
                        var dirName = Path.GetDirectoryName(filter) ?? ".";
                        foreach (var fileName in Directory.GetFiles(dirName, filter))
                            ReadFile(Path.Combine(dirName, fileName), null, multiFile);
                    }
                }
            }
        }

        private void ReadFile(string inputFile, string outputMethod, bool multiFile)
        {
            if (!multiFile && string.IsNullOrEmpty(outputMethod))
            {
                var methodFileName = Path.GetFileNameWithoutExtension(inputFile) + ".msm";
                var dirName = Path.GetDirectoryName(inputFile);
                outputMethod = (dirName != null ? Path.Combine(dirName, methodFileName) : inputFile);
            }

            using (var infile = new StreamReader(inputFile))
            {
                ReadTransitions(infile, outputMethod);
            }
        }

        private void ReadTransitions(TextReader instream, string outputMethod)
        {
            var outputMethodCurrent = outputMethod;
            var finalMethod = outputMethod;
            var sb = new StringBuilder();

            string line;
            while ((line = instream.ReadLine()) != null)
            {
                line = line.Trim();

                if (string.IsNullOrEmpty(outputMethodCurrent))
                {
                    if (!string.IsNullOrEmpty(outputMethod))
                    {
                        // Only one file, if outputMethod specified
                        throw new IOException($"Failure creating method file {outputMethod}. Transition lists may not contain blank lines.");
                    }

                    // Read output file path from a line in the file
                    outputMethodCurrent = line;
                    finalMethod = instream.ReadLine();
                    if (finalMethod == null)
                        throw new IOException("Empty transition list found.");

                    sb = new StringBuilder();
                }
                else if (string.IsNullOrEmpty(line))
                {
                    _methodTrans.Add(new MethodTransitions(outputMethodCurrent, finalMethod, sb.ToString()));
                    outputMethodCurrent = null;
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            // Add the last method, if there is one
            if (!string.IsNullOrEmpty(outputMethodCurrent))
            {
                _methodTrans.Add(new MethodTransitions(outputMethodCurrent, finalMethod, sb.ToString()));
            }
        }

        private TResponse ExecuteAndCheck<TResponse>(IControlRequest<TResponse> request) where TResponse : class, IControlResponse, new()
        {
            var response = _api.Execute(request);
            Check(response);
            return response;
        }

        private static void Check(IControlResponse response)
        {
            if (response.IsSuccessful)
                return;

            var msgs = new List<string> { $"{response.GetType().Name} failure." };

            if (!string.IsNullOrEmpty(response.ErrorCode))
                msgs.Add($"Error code: {response.ErrorCode}.");
            if (!string.IsNullOrEmpty(response.ErrorMessage))
                msgs.Add($"Error message: {response.ErrorMessage}.");
            if (response is MsMethodValidationResponse validationResponse && validationResponse.ValidationErrors.Length > 0)
                msgs.AddRange(validationResponse.ValidationErrors.Select(error => $"Validation error: {error}"));

            throw new Exception(string.Join(Environment.NewLine, msgs));
        }

        public void BuildMethod()
        {
            // Connect and login
            Check(_api.Connect(new ConnectByUriRequest(ServiceUri)));
            Check(_api.Login(new LoginCurrentUserRequest()));

            foreach (var methodTranList in _methodTrans)
            {
                Console.Error.WriteLine($"MESSAGE: Exporting method {Path.GetFileName(methodTranList.FinalMethod)}");

                if (string.IsNullOrEmpty(methodTranList.TransitionList))
                    throw new IOException($"Failure creating method file {methodTranList.FinalMethod}. The transition list is empty.");

                try
                {
                    WriteToTemplate(methodTranList);
                }
                catch (Exception x)
                {
                    throw new IOException($"Failure creating method file {methodTranList.FinalMethod}.  {x.Message}");
                }

                if (!File.Exists(methodTranList.OutputMethod))
                {
                    throw new IOException($"Failure creating method file {methodTranList.FinalMethod}.");
                }

                // Skyline uses a segmented progress status, which expects 100% for each
                // segment, with one segment per file.
                Console.Error.WriteLine("100%");
            }
        }

        private void WriteToTemplate(MethodTransitions transitions)
        {
            // Load template
            var loadResponse = ExecuteAndCheck(new MsMethodLoadRequest(TemplateMethod));
            var method = loadResponse.MsMethod;

            // Edit method
            if (method.Experiments.Count == 0)
                throw new Exception("Method does not contain any experiments.");
            var massTable = method.Experiments[0].MassTable;
            switch (massTable.Rows.Count)
            {
                case 0:
                    throw new Exception("Mass table does not contain any rows.");
                case 1:
                    break;
                default:
                    massTable.RemoveRows(massTable.Rows.Skip(1).ToArray());
                    break;
            }
            massTable.CloneAndAddRow(0, transitions.Transitions.Length - 1);

            const string rowGetterMethodName = "TryGet";
            const string rowPropertyValue = "Value";
            var rowGetter = typeof(PropertiesRow).GetMethod(rowGetterMethodName);
            if (rowGetter == null)
                throw new Exception($"PropertiesRow does not have method {rowGetterMethodName}.");

            var doNothingObj = new object();
            var allProps = new Dictionary<Type, Func<MethodTransition, object>>
            {
                [typeof(GroupIdProperty)] = t => t.Group,
                [typeof(CompoundIdProperty)] = t => t.Label,
                [typeof(Q1MassProperty)] = t => t.PrecursorMz,
                [typeof(Q3MassProperty)] = t => t.ProductMz,
                [typeof(PrecursorIonProperty)] = t => t.PrecursorMz,
                [typeof(FragmentIonProperty)] = t => t.ProductMz,
                [typeof(DwellTimeProperty)] = t => t.DwellOrRt,
                [typeof(RetentionTimeProperty)] = t => t.DwellOrRt,
                [typeof(DeclusteringPotentialProperty)] = t => t.DP,
                [typeof(EntrancePotentialProperty)] = t => doNothingObj,
                [typeof(CollisonEnergyProperty)] = t => t.CE,
                [typeof(CollisionCellExitPotentialProperty)] = t => doNothingObj,
            };

            allProps.Remove(StandardMethod ? typeof(RetentionTimeProperty) : typeof(DwellTimeProperty));

            // Remove missing properties from the dictionary
            var missingProps = allProps.Select(kvp => kvp.Key).Where(prop =>
                rowGetter.MakeGenericMethod(prop).Invoke(massTable.Rows[0], null) == null).ToArray();
            foreach (var prop in missingProps)
                allProps.Remove(prop);

            for (var i = 0; i < transitions.Transitions.Length; i++)
            {
                var row = massTable.Rows[i];
                var transition = transitions.Transitions[i];
                foreach (var kvp in allProps)
                {
                    var prop = kvp.Key;
                    var valueProp = prop.GetProperty(rowPropertyValue);
                    if (valueProp == null)
                        throw new Exception($"Property '{prop.Name}' does not have a property named '{rowPropertyValue}'.");

                    var rowProp = rowGetter.MakeGenericMethod(prop).Invoke(row, null);
                    var newValue = kvp.Value.Invoke(transition);
                    if (!ReferenceEquals(newValue, doNothingObj))
                        valueProp.SetValue(rowProp, newValue);
                }
            }

            // Validate and save method
            ExecuteAndCheck(new MsMethodValidationRequest(method));
            ExecuteAndCheck(new MsMethodSaveRequest(method, transitions.OutputMethod));
        }

        public void Dispose()
        {
            _api.Logout(new LogoutRequest());
            _api.Disconnect(new DisconnectRequest());
        }
    }

    public sealed class MethodTransitions
    {
        public MethodTransitions(string outputMethod, string finalMethod, string transitionList)
        {
            OutputMethod = outputMethod;
            FinalMethod = finalMethod;
            TransitionList = transitionList;

            var tmp = new List<MethodTransition>();
            var reader = new StringReader(TransitionList);
            string line;
            while ((line = reader.ReadLine()) != null)
                tmp.Add(new MethodTransition(line));
            Transitions = tmp.ToArray();

            // Sciex requires that compound IDs are unique, so fix if necessary
            var idSet = new HashSet<string>();
            var needRename = new Dictionary<string, int>();
            foreach (var transition in Transitions)
            {
                if (!needRename.ContainsKey(transition.Label) && !idSet.Add(transition.Label))
                    needRename.Add(transition.Label, 0);
            }
            foreach (var transition in Transitions)
            {
                if (!needRename.TryGetValue(transition.Label, out var i))
                    continue;
                needRename[transition.Label]++;
                transition.Label += "_" + i;
            }
        }

        public string OutputMethod { get; }
        public string FinalMethod { get; }
        public string TransitionList { get; }
        public MethodTransition[] Transitions { get; }
    }

    public sealed class MethodTransition
    {
        private static readonly Dictionary<int, Action<MethodTransition, string>> Columns = new Dictionary<int, Action<MethodTransition, string>>
        {
            [0] = (t, s) => { t.PrecursorMz = double.Parse(s, CultureInfo.InvariantCulture); },
            [1] = (t, s) => { t.ProductMz = double.Parse(s, CultureInfo.InvariantCulture); },
            [2] = (t, s) => { t.DwellOrRt = double.Parse(s, CultureInfo.InvariantCulture); },
            [3] = (t, s) => { t.Label = s; },
            [4] = (t, s) => { t.DP = double.Parse(s, CultureInfo.InvariantCulture); },
            [5] = (t, s) => { t.CE = double.Parse(s, CultureInfo.InvariantCulture); },
            [6] = (t, s) => { t.PrecursorWindow = string.IsNullOrEmpty(s) ? (double?)null : double.Parse(s, CultureInfo.InvariantCulture); },
            [7] = (t, s) => { t.ProductWindow = string.IsNullOrEmpty(s) ? (double?)null : double.Parse(s, CultureInfo.InvariantCulture); },
            [8] = (t, s) => { t.Group = s; },
            [9] = (t, s) => { t.AveragePeakArea = string.IsNullOrEmpty(s) ? (float?)null : float.Parse(s, CultureInfo.InvariantCulture); },
            [10] = (t, s) => { t.VariableRtWindow = string.IsNullOrEmpty(s) ? (double?)null : double.Parse(s, CultureInfo.InvariantCulture); },
            [11] = (t, s) => { t.Threshold = string.IsNullOrEmpty(s) ? (double?)null : double.Parse(s, CultureInfo.InvariantCulture); },
            [12] = (t, s) => { t.Primary = string.IsNullOrEmpty(s) ? (int?)null : int.Parse(s, CultureInfo.InvariantCulture); },
            [13] = (t, s) => { t.CoV = string.IsNullOrEmpty(s) ? (double?)null : double.Parse(s, CultureInfo.InvariantCulture); },
        };

        public MethodTransition(string transitionListLine)
        {
            var values = transitionListLine.Split(',');
            if (values.Length < 6)
                throw new IOException("Invalid transition list format. Each line must at least have 6 values.");
            for (var i = 0; i < values.Length; i++)
            {
                try
                {
                    if (Columns.TryGetValue(i, out var func))
                        func(this, values[i]);
                }
                catch (FormatException)
                {
                    throw new IOException($"Invalid transition list format. Error parsing value '{values[i]}' in column {i}.");
                }
            }
        }

        public double PrecursorMz { get; private set; }
        public double ProductMz { get; private set; }
        public double DwellOrRt { get; private set; }
        public string Label { get; set; }
        public double CE { get; private set; }
        public double DP { get; private set; }
        public double? PrecursorWindow { get; private set; }
        public double? ProductWindow { get; private set; }
        public double? Threshold { get; private set; }
        public int? Primary { get; private set; }
        public string Group { get; private set; }
        public float? AveragePeakArea { get; private set; }
        public double? VariableRtWindow { get; private set; }
        public double? CoV { get; private set; }

        public override string ToString()
        {
            // For debugging
            return $"{Label}; Q1={PrecursorMz}; Q3={ProductMz}; DwellOrRT={DwellOrRt}";
        }
    }
}
