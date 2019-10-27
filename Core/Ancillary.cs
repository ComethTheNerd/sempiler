using Sempiler.AST;
using Sempiler.Core;
using Sempiler.Emission;
using Sempiler.Diagnostics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Sempiler
{
    public enum AncillaryRole 
    {
        MainApp,
        ShareExtension
    }


   
    public struct Ancillary
    {
        public readonly AncillaryRole Role;
       
        public Ancillary(AncillaryRole role, RawAST ast)
        {
            Role = role;
            AST = ast;
            
            Capabilities = new List<Capability>();
            Dependencies = new List<Dependency>();
            Entitlements = new List<Entitlement>();
            Permissions = new List<Permission>();
            Resources = new List<ISource>();
            BridgeIntents = new List<Directive>();
        }

        public RawAST AST { get; set; }

        public List<Capability> Capabilities { get; set; }
        public List<Dependency> Dependencies { get; set; }
        
        public List<Entitlement> Entitlements { get; set; }

        public List<Permission> Permissions { get; set; }

        public List<ISource> Resources { get; set; }

        public List<Directive> BridgeIntents { get; set; }
    }
}