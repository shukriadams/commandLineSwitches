
////////////////////////////////////////////////////////////////
// CommandLineSwitches - Parses command line args to switches //
// Shukri Adams (mail@shukriadams.com)                      //
// https://github.com/shukriadams/commandLineSwitches         //
// MIT License (MIT) Copyright (c) 2018 Shukri Adams          //
////////////////////////////////////////////////////////////////

using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Madscience_CommandLineSwitches
{
    //
    public class Argument
    {
        public string Id { get; set;}
        
        public string LongName { get; set;}
        
        public string ShortName { get; set;}
        
        public bool IsRequired { get; set;}
        
        public bool IsSet { get; set;}

        // note, must be in same format as command line would be, will be parsed layer
        public string DefaultValue { get; set;}
        
        public object Value { get; set;}

        public Type Type  { get; set;}

        public Argument(string id, Type type)
        {
            this.Id = id;
            this.Type = type;
        }

        public void SetValue(string rawValue)
        {
            if(!string.IsNullOrEmpty(rawValue))
            {
                if (this.Type == typeof(int) ||  this.Type == typeof(Int32))
                    this.Value = int.Parse(rawValue);
                else if (this.Type == typeof(int?))
                    this.Value = int.Parse(rawValue);
                else if (this.Type == typeof(Boolean) || this.Type == typeof(bool))
                    this.Value =  bool.Parse(rawValue);
                else if (this.Type == typeof(bool?))
                    this.Value = bool.Parse(rawValue);
                else if (this.Type == typeof(string))
                    this.Value = rawValue;
                else
                    throw new Exception($"arg type {this.Type.Name} is not supported.");
            }

            this.IsSet = true;
        }

        public override string ToString()
        {
            return $"Id:{Id}\n" +
                $"LongName:{LongName}\n" +
                $"ShortName:{ShortName}\n" +
                $"Type:{Type}\n" +
                $"IsRequired:{IsRequired}\n" +
                $"Value:{Value}\n"
            ;
        }
    }

    public class BindResponse 
    {
        public bool Succeeded {get;set;}
        public string Description {get;set;}
    }

    /// <summary>
    /// Parses command line arguments into key-value pairs. For example, if an app is started "app.exe -i foo -j bar"
    /// this.Arguments will contain two items, {i,foo} and {j, bar}.
    /// </summary>
    public class CommandLineSwitches 
    {
        #region PROPERTIES

        /// <summary>
        /// Parsed key-values for arguments.
        /// </summary>
        public IList<KeyValuePair<string, Argument>> Arguments { get; private set; } = new List<KeyValuePair<string, Argument>>();

        /// <summary>
        /// Invalid switches
        /// </summary>
        public IList<string> InvalidArguments { get; set; } = new List<string>();

        /// <summary>
        /// Unregistered switches
        /// </summary>
        public IList<string> UnregisteredArguments { get; set; } = new List<string>();

        public bool AllowUnregisteredArguments {get; set;}

        public bool AllowInvalidArguments {get; set;}

        #endregion

        #region METHODS
        
        public void Add(Argument argument)
        {
            this.Arguments.Add(new KeyValuePair<string, Argument>(argument.Id, argument));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">Raw command line args, such as those passed into your command line app.</param>
        public BindResponse Bind(string[] args, bool validate)
        {
            BindResponse response = new BindResponse();

            Regex switchLeadRegex = new Regex("(-+)(.*)");
            StringBuilder description = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                Match match = switchLeadRegex.Match(arg);
                if (!match.Success || match.Groups.Count < 2)
                    continue;

                // lead must be a dash, and following unix convention must be a single for a single char switch
                // and two for a multichar switch. Anythingmore than 2 is considered invalid
                string lead = match.Groups[1].ToString();
                string key = match.Groups[2].ToString();

                // ignore empty switches "- " and "-- "
                if (key.Length == 0)
                    continue;

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

                // if this arg is a valid switch, and the next item in list does not start with a dish, 
                // ergo is not a swtich, then value is the next item in list
                if (args.Length > i + 1 && !args[i + 1].ToLower().StartsWith("-"))
                    value = args[i + 1];

                bool argumentRegistered = false;


                foreach(KeyValuePair<string, Argument> bound in this.Arguments)
                {
                    Argument argument = bound.Value;
                    if (argument.ShortName == key || argument.LongName == key)
                    {
                        argument.SetValue(value);
                        argumentRegistered = true;
                        break;
                    }
                }

                if (!argumentRegistered)
                    UnregisteredArguments.Add($"{key} {value}".Trim());
            }

            response.Succeeded = true;


            // set default values, these are always applied even is switch is not set
            foreach(KeyValuePair<string, Argument> arg in this.Arguments)
            {
                Argument argument = arg.Value;
                if (argument.DefaultValue != null && argument.IsRequired)
                    throw new Exception($"\"{argument.Id}\" cannot be both required and have a default value.");

                if (argument.DefaultValue != null)
                    argument.SetValue(argument.DefaultValue);
            }


            if (validate)
            {
                // enforce mandatory args
                foreach(KeyValuePair<string, Argument> arg in this.Arguments)
                {
                    Argument argument = arg.Value;

                    if (argument.IsRequired && argument.Value == null)
                    {
                        response.Succeeded = false;
                        description.AppendLine($"{GenerateUseText(argument)}");
                    }
                }

                // fail on unregistered
                if (!this.AllowUnregisteredArguments && UnregisteredArguments.Any())
                {
                    response.Succeeded = false;
                    foreach (string arg in UnregisteredArguments)
                        description.AppendLine($"Unrecognized argument \"{arg}\"");
                }

                // fail on invalid
                if (!this.AllowInvalidArguments && InvalidArguments.Any())
                {
                    response.Succeeded = false;
                    foreach (string arg in InvalidArguments)
                        description.AppendLine($"Invalid argument \"{arg}\"");
                }
            }

            response.Description = description.ToString().Trim();
            return response;
        }

        public string GenerateUseText(Argument argument)
        {
            string text =  $"Required argument not set, ";

            if (string.IsNullOrEmpty(argument.LongName) && !string.IsNullOrEmpty(argument.ShortName))
                text += $"use \"-{argument.ShortName} <VALUE>\"";
            else if (!string.IsNullOrEmpty(argument.LongName) && string.IsNullOrEmpty(argument.ShortName))
                text += $"use \"--{argument.LongName} <VALUE>\"";
            else
                text += $"use \"--{argument.LongName} <VALUE>\" or \"-{argument.ShortName} <VALUE>\"";

            return text;
        }

        /// <summary>
        /// Returns true if the collection has an item of the given name
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsSet(string id)
        {
            if (!this.Arguments.Any(r => r.Key == id))
                throw new Exception($"Argument {id} is not registered");

            KeyValuePair<string,Argument> argument = this.Arguments.FirstOrDefault(r => r.Key == id);

            return argument.Value.IsSet;
        }

        /// <summary>
        /// Returns the value for a given key ; returns emptry string if key is not set.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string id)
        {
            KeyValuePair<string,Argument> argument = default(KeyValuePair<string,Argument>);

            try
            {
                if (!this.Arguments.Any(r => r.Key == id))
                    throw new Exception($"Argument {id} is not registered");

                argument = this.Arguments.FirstOrDefault(r => r.Key == id);
                //Console.WriteLine("!!"+argument.Value);

                return (T)argument.Value.Value;
            } 
            catch (Exception e)
            {
                Console.WriteLine(argument);
                Console.WriteLine($"Error getting value for switch {id}");
                throw;
            }
        }

        #endregion
    }
}
