namespace Sempiler
{
    using Sempiler.Emission;
    using Sempiler.Diagnostics;
    using System.Collections.Generic;

    public class Artifact
    {
        public readonly ArtifactRole Role;
        public readonly string Name;
        public readonly string TargetLang;
        public readonly string TargetPlatform;

        public string TeamName;

        // public readonly Dictionary<string, OutFile> FilesWritten;
    
        // public Artifact(ArtifactRole role, string name, string targetLang, string targetPlatform) 
        //     : this(role, name, targetLang, targetPlatform/* */, new Dictionary<string, OutFile>()){}

        public Artifact(ArtifactRole role, string name, string targetLang, string targetPlatform/* , Dictionary<string, OutFile> filesWritten*/)
        {
            Role = role;
            Name = name;
            TargetLang = targetLang;
            TargetPlatform = targetPlatform;

            TeamName = "sempiler";
        }
        

        // public Artifact Clone()
        // {
        //     return new Artifact(Role, Name, TargetLang, TargetPlatform/* , new Dictionary<string, OutFile>(FilesWritten)*/);
        // }
    }


    public enum ArtifactRole 
    {
        Compiler,

        Client,
        Server,
        Database
    }

    public static class ArtifactTargetLang
    {
        // public const string Compiler = "compiler";
        
        public const string JavaScript = "javascript";
        public const string Java = "java";
        public const string JSON = "json";
        public const string SQL = "sql";
        public const string Swift = "swift";
        public const string TypeScript = "typescript";
    }

    public static class ArtifactTargetPlatform
    {
        // public const string Compiler = "compiler";

        public const string Android = "android";
        public const string AWSLambda = "aws/lambda";
        public const string FirebaseFunctions = "firebase/functions";
        public const string IOS = "ios";
        public const string Node = "node";
        public const string SwiftUI = "ios/swiftui";
        public const string WebBrowser = "webbrowser";
        public const string ZeitNow = "zeit/now";
    }

    public static class ArtifactHelpers
    {
        public static Result<object> AddAll(Dictionary<string, Artifact> source, Dictionary<string, Artifact> destination)
        {
            var result = new Result<object>();

            foreach(var kv in source)
            {
                if(destination.ContainsKey(kv.Key))
                {
                    var art = destination[kv.Key];

                    if(art.TargetLang != kv.Value.TargetLang)
                    {
                        result.AddMessages(
                            new Message(MessageKind.Error, $"Artifact '{art.Name}' redeclared with incompatible target languages ('{art.TargetLang}' vs '{kv.Value.TargetLang}'")
                        );
                    }

                    if(art.TargetPlatform != kv.Value.TargetPlatform)
                    {
                        result.AddMessages(
                            new Message(MessageKind.Error, $"Artifact '{art.Name}' redeclared with incompatible target platforms ('{art.TargetPlatform}' vs '{kv.Value.TargetPlatform}'")
                        );
                    }
                }
                else
                {
                    destination[kv.Key] = kv.Value;
                }
            }

            return result;
        }
    }
}