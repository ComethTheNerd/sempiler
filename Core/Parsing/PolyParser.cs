using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sempiler.AST;
using Sempiler.Diagnostics;

namespace Sempiler.Parsing
{
    public class PolyParser : IParser
    {
        private Dictionary<string, IParser> ParserCache = new Dictionary<string, IParser>();

        private object l = new object();

        public PolyParser()
        {
        }

        public Task<Result<AST.Component[]>> Parse(Session session, RawAST ast, ISource source, CancellationToken token)
        {
            string extension = (source as ISourceWithLocation<IFileLocation>)?.Location.Extension;

            if(IsSupportedSourceExtension(extension))
            {
                IParser parser = default(IParser);

                lock(l)
                {
                    var cacheKey = ParserKey(extension);

                    if(ParserCache.ContainsKey(cacheKey))
                    {
                        parser = ParserCache[cacheKey];
                    }
                    else
                    {
                        parser = ParserCache[cacheKey] = CreateParserForSourceExtension(extension);
                    }
                }
            
                return parser.Parse(session, ast, source, token);
            }
            else
            {
                var result = new Result<AST.Component[]>();

                result.AddMessages(
                    new Message(MessageKind.Error, $"No parser configured for source file extension '{extension}'")
                );

                return Task.FromResult(result);
            }
        }

        private bool IsSupportedSourceExtension(string extension)
        {
            switch(extension)
            {
                case "ts":
                case "tsx":
                    return true;

                default:
                    return false;
            }
        }

        private string ParserKey(string extension)
        {
            if(extension == "tsx") return "ts";

            return extension;
        }


        private IParser CreateParserForSourceExtension(string extension)
        {
            switch(extension)
            {
                case "ts":
                case "tsx":
                    return new RelaxedParser();

                default:
                    return null;
            }
        }
    }
}
