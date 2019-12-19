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
        ShareExtension
    }


   
    public class Shard
    {
        public readonly ShardRole Role;
       
        public Shard(ShardRole role, RawAST ast)
        {
            Role = role;
            AST = ast;
            Name = role.ToString().ToLower();
            Version = "0.0.1";
            Orientation = Orientation.Unspecified;
            Capabilities = new List<Capability>();
            Dependencies = new List<Dependency>();
            Entitlements = new List<Entitlement>();
            Assets = new List<Asset>();
            Permissions = new List<Permission>();
            Resources = new List<ISource>();
            BridgeIntents = new List<Directive>();
        }

        public RawAST AST;
        public string Name;

        public string Version;

        public Orientation Orientation;

        public List<Capability> Capabilities;
        public List<Dependency> Dependencies;
        
        public List<Entitlement> Entitlements;

        public List<Asset> Assets;

        public List<Permission> Permissions;

        public List<ISource> Resources;

        public List<Directive> BridgeIntents;
    }
}