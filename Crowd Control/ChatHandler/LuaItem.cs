using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrowdControl.ChatHandler
{
    public class LuaParser
    {
        internal static LuaItem ParseLuaFile(string content)
        {
            var fileItem = new LuaItem();

            // Match function signatures
            var functionMatches = Regex.Matches(content, @"(---@[\s\S]+?)?function (\w+\.\w+)\( (.+?) \)");
            foreach (Match functionMatch in functionMatches)
            {
                FunctionInfo function = new();
                // Extract annotations for this function
                var annotationMatches = Regex.Matches(functionMatch.Groups[1].Value, @"---@(\w+)\s+(.+)");
                foreach (Match annotationMatch in annotationMatches)
                {
                    string indexer = annotationMatch.Groups[1].Value;
                    string value = annotationMatch.Groups[2].Value;
                    switch (indexer)
                    {
                        case "class":
                            function.Class = value.Trim();
                            break;
                        case "type":
                            function.Type = Enum.Parse<EnvironemntType>(value.Trim());
                            break;
                        case "param":
                            
                            var param = value.Trim().Split(' ');
                            function.Params.Add(param[^1].Trim(), param[^2].Trim());
                            break;
                        case "field":
                            break;
                    }
                }

                var functionInfo = new FunctionItem()
                {
                    FName = new(functionMatch.Groups[2].Value),
                    Info = function
                };

                fileItem.Functions.Add(functionMatch.Groups[2].Value, functionInfo);
            }

            return fileItem;
        }
    }
    internal class LuaItem
    {
        public Dictionary<string, FunctionItem> Functions { get; set; } = new();
    }
    internal readonly record struct FunctionItem(FunctionName FName, FunctionInfo Info);
    internal record FunctionInfo
    {
        public string Class { get; set; } = string.Empty;
        public EnvironemntType Type { get; set; }
        public Dictionary<string, string> Params { get; set; } = new();
    }
    internal readonly record struct FunctionName
    {
        public FunctionName(string fullName)
        {
            var item = fullName.Split('.');
            this.Name = item[^1];
            this.Class = item[^2];
        }
        public string Class { get; init; }
        public string Name { get; init; }
    }
    internal enum EnvironemntType
    {
        Server = 0x00,
        Client = 0x01,
    }
}




















