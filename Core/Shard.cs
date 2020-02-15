using Sempiler.AST;
using Sempiler.Core;
using Sempiler.Emission;
using Sempiler.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Sempiler
{
    public enum ShardRole 
    {
        MainApp,
        ShareExtension,
        StaticSite
    }


   
    public class Shard
    {
        public readonly ShardRole Role;
       
        public Shard(ShardRole role, string absoluteEntryPointPath, RawAST ast)
        {
            Role = role;
            AbsoluteEntryPointPath = absoluteEntryPointPath;
            AST = ast;
            Name = role.ToString().ToLower();
            Version = "0.0.1";
            Orientation = Orientation.Unspecified;
            Capabilities = new List<Capability>();
            ManifestEntries = new List<ManifestEntry>();
            Dependencies = new List<Dependency>();
            Entitlements = new List<Entitlement>();
            Assets = new List<Asset>();
            Permissions = new List<Permission>();
            Resources = new List<Resource>();
            BridgeIntents = new List<Directive>();
        }

        public RawAST AST;
        public string Name;

        public string Version;

        public string AbsoluteEntryPointPath;

        public Orientation Orientation;

        public List<ManifestEntry> ManifestEntries;

        public List<Capability> Capabilities;
        public List<Dependency> Dependencies;
        
        public List<Entitlement> Entitlements;

        public List<Asset> Assets;

        public List<Permission> Permissions;

        public List<Resource> Resources;

        public List<Directive> BridgeIntents;
    }

    public static class ShardHelpers 
    {
        // public static ConfigValue StringConfigValue(string value) 
        // {
        //     return new ConfigValue { Type = ConfigurationPrimitive.String, Values = new [] { value } };
        // }

        // public static ConfigValue StringArrayConfigValue(params string[] values) 
        // {
        //     return new ConfigValue { Type = ConfigurationPrimitive.StringArray, Values = values };
        // }

        // public static Result<bool> AddConfigValue(Dictionary<string, object> dict, string path, object value, bool merge = true) => 
        //     AddConfigValue(dict, path.Split('/'), value, merge);

        public static Result<bool> AddConfigValue(Dictionary<string, object> container, string[] pathParts, object value/*, bool merge = true*/)
        {
            var result = new Result<bool>()
            {
                Value = false
            };
            
            if(pathParts.Length == 0){ 
                result.AddMessages(new Message(MessageKind.Warning, $"Cannot add config value because path is empty"));
                return result;
            }

            container = result.AddMessages(GetOrCreateConfigValueContainer(container, pathParts));
            
            if(container != null)
            {
                var lastPart = pathParts[pathParts.Length - 1];
 
                // if(merge && 
                //     container.ContainsKey(lastPart) && 
                //     value is Dictionary<string, object> source &&
                //     container[lastPart] is Dictionary<string, object> existingValue)
                // {
                //     DeepMerge(source, existingValue);   
                // }
                // else
                {
                    container[lastPart] = value;
                }

                result.Value = true;
            }

            return result;
        }

        public static void DeepMerge(Dictionary<string, object> source, Dictionary<string, object> dest)
        {
            foreach(var kv in source)
            {
                var key = kv.Key;
                var value = kv.Value;

                if(!dest.ContainsKey(key))
                {
                    dest[key] = value;
                }
                else
                {
                    var existingValue = dest[key];

                    if(existingValue is Dictionary<string, object> && value is Dictionary<string, object>)
                    {
                        DeepMerge((Dictionary<string, object>)value, (Dictionary<string, object>)existingValue);
                    }
                    else
                    {
                        dest[key] = value;
                    }
                }
            }
        }

        public static Result<Dictionary<string, object>> GetOrCreateConfigValueContainer(Dictionary<string, object> start, string[] pathParts)
        {
            var result = new Result<Dictionary<string, object>>();

            // if(pathParts.Length == 0)
            // {
            //     return result;
            // }

            var dict = start;

            for(int i = 0; i < pathParts.Length - 1; ++i)
            {
                var part = pathParts[i];

                if(dict.ContainsKey(part))
                {
                    var obj = dict[part];

                    if(obj is Dictionary<string, object> subDict)
                    {
                        // if(i == pathParts.Length - 1)
                        // {
                        //     result.AddMessages(new Message(MessageKind.Error, $"Cannot add config value because path '{string.Join(".", pathParts)}' has existing children"));
                        //     return result;
                        // }
                        // else
                        // {
                            dict = subDict;
                        // }
                    }
                    else
                    {
                        result.AddMessages(new Message(MessageKind.Error, $"Cannot add config value because path '{string.Join(".", pathParts)}' ends prematurely at existing value"));
                        return result;
                    }
                }
                // is last path component - 18/01/20
                // else if(i == pathParts.Length - 1)
                // {
                //     result.Value = dict;
                //     break;
                // }
                else
                {
                    dict = (Dictionary<string, object>)
                        (dict[part] = new Dictionary<string, object>());
                }
            }

            result.Value = dict;

            return result;
        
        }

        // public static ConfigValue? GetConfigValue(Dictionary<string, object> dict, string path) =>
        //     GetConfigValue(dict, path.Split('.'));

        // public static ConfigValue? GetConfigValue(Dictionary<string, object> dict, string[] pathParts)
        // {
        //     for(int i = 0; i < pathParts.Length; ++i)
        //     {
        //         var part = pathParts[i];

        //         if(dict.ContainsKey(part))
        //         {
        //             var obj = dict[part];

        //             if(obj is Dictionary<string, object> subDict)
        //             {
        //                 if(i == pathParts.Length - 1)
        //                 {
        //                     break;
        //                 }
        //                 else
        //                 {
        //                     dict = subDict;
        //                 }
        //             }
        //             else
        //             {
        //                 break;
        //             }
        //         }
        //         // is last path component - 18/01/20
        //         else if(i == pathParts.Length - 1)
        //         {
        //             if(dict.ContainsKey(part) && dict[part] is ConfigValue cv)
        //             {
        //                 return cv;
        //             }
        //             else
        //             {
        //                 break;
        //             }
        //         }
        //         else
        //         {
        //             break;
        //         }
        //     }

        //     return null;
        // }
    }
}