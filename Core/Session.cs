﻿using Sempiler.AST;
using Sempiler.Core;
using Sempiler.Emission;
using Sempiler.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Sempiler
{
    ///<summary>
    /// Not serialized between runs of the compiler, so can contain machine specific
    /// information such as the base path
    ///</summary>
    public struct Session
    {
        // IOptions Options { get; set; }

        // [dho] DO NOT store the AST on the session, because we might
        // have multiple ASTs throughout a session - 23/08/18
        //RawAST AST { get; }
        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public DuplexSocketServer Server { get; set; }

        public IDirectoryLocation BaseDirectory { get; set; }

        public IEnumerable<string> InputPaths { get; set; }

        public Dictionary<string, Artifact> Artifacts { get; set; }

        public Dictionary<string, List<Ancillary>> Ancillaries { get; set; }

        // public Dictionary<string, string> GUIDs { get; set; }

        // public Dictionary<string, RawAST> ASTs { get; set; }

        // public Dictionary<string, List<ISource>> Resources { get; set; }

        // public Dictionary<string, List<Capability>> Capabilities { get; set; }
        // public Dictionary<string, List<Dependency>> Dependencies { get; set; }

        // public Dictionary<string, List<Entitlement>> Entitlements { get; set; }

        // public Dictionary<string, List<Permission>> Permissions { get; set; }

        // ///<summary>Key is *source* artifact name (ie. where the bridge intent was found), *NOT* the *target* artifact it wants to talk to</summary>
        // public Dictionary<string, List<Directive>> BridgeIntents { get; set; }

        public Dictionary<string, Dictionary<string, OutFile>> FilesWritten { get; set; }
    }

    public struct Capability
    {
        public string Name;
        public ConfigurationPrimitive Type;
        public string[] Values;
    }


    public struct Dependency 
    {
        public string Name;
        public string Version;
    }

    public enum ConfigurationPrimitive
    {
        String,
        StringArray
    }

    public struct Entitlement
    {
        public string Name;
        public ConfigurationPrimitive Type;
        public string[] Values;
    }

    public struct Permission
    {
        public string Name;
        public string Description;
    }

    public static class SessionHelpers
    {
        // [dho] Session is a struct (value type) so it will have been
        // implicitly copied when passed to this function. If we ever change
        // Session to a reference type, here we will have to do more work to
        // actually copy the properties on the object to a new instance explicitly - 21/04/19
        public static Session Clone(Session session)
        {
            return session;
        }

        // public static void AddSource(Session session, ISource source)
        // {
        //     // [dho] no de duping?
        //     session.Sources.Add(source);
        // }
    }

}
