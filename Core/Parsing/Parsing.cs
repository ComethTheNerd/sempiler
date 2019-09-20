using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sempiler.AST;

namespace Sempiler.Parsing
{
    public interface IParser
    { 
        // IEnumerable so it can support lazy collections and we can quit out early if needs be
        Task<Sempiler.Diagnostics.Result<AST.Component[]>> Parse(Session session, RawAST ast, ISource source, CancellationToken token);
    }
}