////////////////////////////////////////////////////////////////
// CommandLineSwitches - Parses command line args to switches //
// Shukri Adams (shukri.adams@gmail.com)                      //
// https://github.com/shukriadams/commandLineSwitches         //
// MIT License (MIT) Copyright (c) 2018 Shukri Adams          //
////////////////////////////////////////////////////////////////

using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Madscience_CommandLineSwitches
{
    /// <summary>
    /// Parses command line arguments into key-value pairs. For example, if an app is started "app.exe -i foo -j bar"
    /// this.Arguments will contain two items, {i,foo} and {j, bar}.
    /// </summary>
    public class CommandLineSwitches 
    {
        /// <summary>
        /// Parsed key-values for arguments.
        /// </summary>
        public List<KeyValuePair<string, string>> Arguments { get; set; }

        /// <summary>
        /// Invalid switches
        /// </summary>
        public IList<string> InvalidArguments { get; set; } = new List<string>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Raw command line args, such as those passed into your command line app.</param>
        public CommandLineSwitches(string[] args)
        {
            this.Arguments = new List<KeyValuePair<string, string>>();

            if (args == null || !args.Any())
                return;

            Regex switchLeadRegex = new Regex("(-+)(.*)");
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                Match match = switchLeadRegex.Match(arg);
                if (!match.Success || match.Groups.Count < 2)
                    continue;

                string lead = match.Groups[1].ToString();
                string key = match.Groups[2].ToString();

                if (lead.Length > 2)
                {
                    this.InvalidArguments.Add(lead+key);
                    continue;
                }

                if (lead.Length == 1 && key.Length > 1)
                {
                    this.InvalidArguments.Add(lead+key);
                    continue;
                }

                string value = null;

                // if this arg is a valid switch, and the next item in list is not a swtich, value is the next item in list
                if (args.Length > i + 1 && !args[i + 1].ToLower().StartsWith("-"))
                    value = args[i + 1];


                this.Arguments.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        /// <summary>
        /// Returns true if the collection has an item of the given name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            return this.Arguments.Any(r => r.Key == key);
        }

        /// <summary>
        /// Returns the value for a given key ; returns emptry string if key is not set.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string longName, string shortName)
        {
            if (this.Contains(shortName))
                return this.Arguments.Single(r => r.Key == shortName).Value;

            if (this.Contains(longName))
                return this.Arguments.Single(r => r.Key == longName).Value;

            return string.Empty;
        }
    }
}
