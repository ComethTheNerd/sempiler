using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sempiler.Distribution.CLI.Protocol
{
    public interface IProtocol_Serializable {}

    public class Packet : IProtocol_Serializable
    {
        protected Packet()
        {
            // ID = ProtocolHelpers.NextID();
        }

        // [JsonProperty("id", Required = Required.Always)]
        // public string ID;

        [JsonProperty("type", Required = Required.Always)]
        public string Type;

        [JsonProperty("data")]
        public object Data; // [dho] NOTE do NOT set this to anything other than `object` or it hangs for some reason! - 02/06/19
    }

    public class Request : Packet
    {
        public Request()
        {
            Type = "request";
        }

        [JsonProperty("id", Required = Required.Always)]
        public string ID;

        [JsonProperty("command", Required = Required.Always)]
        public string Command;
    }

    public class Response : Packet
    {
        public Response()
        {
            Type = "response";
        }

        // [JsonProperty("requestID", Required = Required.Always)]
        public string RequestID;

        [JsonProperty("ok", Required = Required.Always)]
        public bool OK;
    }

    public class Event : Packet
    {
        public Event()
        {
            Type = "event";
        }

        [JsonProperty("event", Required = Required.Always)]
        public string Name;
    }

    // public class Error : Packet
    // {
    //     public Error()
    //     {
    //         Type = "error";
    //     }

    //     [JsonProperty("description", Required = Required.Always)]
    //     public string Description;
    // }

    public struct Protocol_Session : IProtocol_Serializable
    {
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public string BaseDirectory { get; set; }

        public Dictionary<string, Protocol_Artifact> Artifacts { get; set; }

        public Dictionary<string, string[]> FilesWritten { get; set; }
    }

    public struct Protocol_Artifact : IProtocol_Serializable
    {
        public string Role;
        public string Name;
        public string TargetLang;
        public string TargetPlatform;
    }

    public struct Protocol_MessageCollection : IProtocol_Serializable
    {
        public IEnumerable<Protocol_Message> Infos;
        public IEnumerable<Protocol_Message> Warnings;
        public IEnumerable<Protocol_Message> Errors;
    }

    public class Protocol_Message : IProtocol_Serializable
    {
        [JsonProperty("kind", Required = Required.Always)]
        public string Kind;

        [JsonProperty("description", Required = Required.Always)]
        public string Description;

        public IEnumerable<string> Tags;

        public string Path;

        public Range LineNumber;

        public Range ColumnIndex;

        public Range Pos;
    }


    public struct Protocol_Result : IProtocol_Serializable
    {
        [JsonProperty("messages", Required = Required.Always)]
        public Protocol_MessageCollection Messages;
    
        public IProtocol_Serializable Value;
    }


    public class SetConfigData
    {
        [JsonProperty("path", Required = Required.Always)]
        public string Path; 
    }

    public static class ProtocolHelpers
    {
        // static int ID = 0;
        // static object l = new object{};

        static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings 
        { 
            // TypeNameHandling = TypeNameHandling.All,
            // TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter(true /* camelCase */) }
            
        };

        // public static string NextID()
        // {
        //     lock(l)
        //     {
        //         return ++ID + "";
        //     }
        // }

        // public static Protocol.Session Convert(Sempiler.Session session)
        // {
        //     return new Protocol.Session
        //     {
        //         BaseDirectory = session.BaseDirectory.ToPathString(),
        //         DurationMS = (session.End - session.Start).Milliseconds,
        //         Messages = Convert(session.Messages),
        //         FilesWritten = session.FilesWritten.Keys
        //     };
        // }

        public static Protocol_MessageCollection Convert(Diagnostics.MessageCollection input)
        {
            return new Protocol_MessageCollection
            {
                Infos = input?.Infos != null ? Convert(input.Infos) : null,
                Warnings = input?.Warnings != null ? Convert(input.Warnings) : null,
                Errors = input?.Errors != null ? Convert(input.Errors) : null
            };
        }

        public static IEnumerable<Protocol_Message> Convert(IEnumerable<Diagnostics.Message> input)
        {
            var output = new List<Protocol_Message>();

            foreach(var message in input)
            {
                string path = null;
                Range lineNumber = default(Range);
                Range columnIndex = default(Range);
                Range pos = default(Range);

                var hint = message.Hint;

                if(hint != null)
                {
                    path = hint.File.ToPathString();                
                    lineNumber = RangeHelpers.Clone(hint.LineNumber);
                    columnIndex = RangeHelpers.Clone(hint.ColumnIndex);
                    pos = RangeHelpers.Clone(hint.Pos);
                }

                output.Add(new Protocol_Message {
                    Kind = message.Kind.ToString("g").ToLower(),
                    Tags = message.Tags,
                    Description = message.Description,
                    Path = path,
                    LineNumber = lineNumber,
                    ColumnIndex = columnIndex,
                    Pos = pos
                });
            }

            return output;
        }

        public static Protocol_Session Convert(Session session)
        {
            var artifacts = new Dictionary<string, Protocol_Artifact>();

            if(session.Artifacts != null)
            {
                foreach(var entry in session.Artifacts)
                {
                    var artifactName = entry.Key;
                    var artifact = entry.Value;

                    artifacts[artifactName] = new Protocol_Artifact
                    {
                        Role = artifact.Role.ToString().ToLower(),
                        Name = artifact.Name,
                        TargetLang = artifact.TargetLang,
                        TargetPlatform = artifact.TargetPlatform
                    };
                }
            }

            var filesWritten = new Dictionary<string, string[]>();

            if(session.FilesWritten != null)
            {
                foreach(var entry in session.FilesWritten)
                {
                    var artifactName = entry.Key;
                    
                    var artifactFilesWritten = new string[entry.Value.Keys.Count];
                    entry.Value.Keys.CopyTo(artifactFilesWritten, 0);

                    filesWritten[artifactName] = artifactFilesWritten;
                }
            }

            return new Protocol_Session
            {
                Start = session.Start,
                End = session.End,
                BaseDirectory = session.BaseDirectory.ToPathString(),
                Artifacts = artifacts,
                FilesWritten = filesWritten
            };
        }

        // private static object Convert(object input) => input;

        public static Protocol_Result Convert(Diagnostics.Result<IProtocol_Serializable> result)
        {
            return new Protocol_Result
            {
                Messages = Convert(result.Messages),
                Value = result.Value
            };
        }

    

        public static void Print(Packet packet)
        {
            Console.WriteLine(JsonConvert.SerializeObject(packet, serializerSettings));
        }
    }
}