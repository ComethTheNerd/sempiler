using Sempiler.AST;
using Sempiler.Core;
using System.Collections.Generic;

namespace Sempiler.CTExec
{
    public class CTExecInfo
    {
        public string ID;
        
        // public System.Threading.CancellationTokenRegistration CancellationTokenRegistration;

        // [dho] the paths that all CT Exec JS files will `require(..)`, eg. server/client code - 25/11/19
        public string[] AbsoluteLibPaths;
        public IDirectoryLocation OutDirectory;
        public DuplexSocketServer.OnMessageDelegate MessageHandler;
        public System.Diagnostics.Process Process;

        // public Dictionary<string, RawAST> ASTs;

        public Dictionary<string, Emission.OutFile> FilesWritten;

        public Dictionary<string, bool> ComponentIDsEmitted;
    }
}