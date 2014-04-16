﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Microsoft.WindowsAzure.Utilities.HttpRecorder
{
    public static class Utilities
    {
        public static string FormatString(string content)
        {
            if (IsXml(content))
            {
                return TryFormatXml(content);
            }
            else if (IsJson(content))
            {
                return TryFormatJson(content);
            }
            else
            {
                return content;
            }
        }

        /// <summary>
        /// Formats the given XML into indented way.
        /// </summary>
        /// <param name="content">The input xml string</param>
        /// <returns>The formatted xml string</returns>
        public static string TryFormatXml(string content)
        {
            try
            {
                XDocument doc = XDocument.Parse(content);
                return doc.ToString();
            }
            catch (Exception)
            {
                return content;
            }
        }

        /// <summary>
        /// Checks if the content is valid XML or not.
        /// </summary>
        /// <param name="content">The text to check</param>
        /// <returns>True if XML, false otherwise</returns>
        public static bool IsXml(string content)
        {
            try
            {
                XDocument doc = XDocument.Parse(content);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Handling the failure by returning the original string.")]
        public static string TryFormatJson(string str)
        {
            try
            {
                object parsedJson = JsonConvert.DeserializeObject(str);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch
            {
                // can't parse JSON, return the original string
                return str;
            }
        }

        public static bool IsJson(string content)
        {
            content = content.Trim();
            return content.StartsWith("{") && content.EndsWith("}")
                   || content.StartsWith("[") && content.EndsWith("]");
        }

        /// <summary>
        /// Perform an action on each element of a sequence.
        /// </summary>
        /// <typeparam name="T">Type of elements in the sequence.</typeparam>
        /// <param name="sequence">The sequence.</param>
        /// <param name="action">The action to perform.</param>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            Debug.Assert(sequence != null, "sequence cannot be null!");
            Debug.Assert(action != null, "action cannot be null!");

            foreach (T element in sequence)
            {
                action(element);
            }
        }

        public static void SerializeJson<T>(T data, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.None, new JsonSerializerSettings
                {
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                    TypeNameHandling = TypeNameHandling.None
                }));
        }

        public static T DeserializeJson<T>(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
                {
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                    TypeNameHandling = TypeNameHandling.None
                });
        }

        public static void CleanDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) File.Delete(file);
                foreach (string subDirectory in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories)) Directory.Delete(subDirectory, true);
            }
        }

        public static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
