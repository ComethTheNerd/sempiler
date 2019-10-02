using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Sempiler;
using Sempiler.AST;
using Sempiler.Parsing;
using Sempiler.Diagnostics;
using static Sempiler.Diagnostics.DiagnosticsHelpers;

namespace Sempiler.Parsing
{
    using Token = Lexer.Token;

    using NodeID = System.String;

    public struct Context // [dho] TODO name? - 11/09/18
    {
        public ContextKind TotalKind;
        public ContextKind CurrentKind;

        public ContextFlags Flags;

        // public bool ErrorBeforeNextFinishedNode;

        public Session Session;
        public RawAST AST;

        public ISource Source;

        public bool ErrorRecoveryMode;
    }


    public static class ContextHelpers
    {
        public static Context Clone(Context context)
        {
            // [dho] Context is a struct (value type) so it will have been
            // implicitly copied when passed to this function. If we ever change
            // Context to a reference type, here we will have to do more work to
            // actually copy the properties on the object to a new instance explicitly - 29/08/18
            return context;
        }


        public static Context CloneInKind(Context source, ContextKind kind)
        {
            var context = ContextHelpers.Clone(source);

            context.CurrentKind = kind;

            context.TotalKind |= kind;

            return context;
        }
    }


    public class RelaxedParser : IParser
    {
        const string ConstLexeme = "const";
        const string LetLexeme = "let";
        const string VarLexeme = "var";


        static string[] DiagnosticTags = new string[] { "relaxed", PhaseKind.Parsing.ToString("g").ToLower() };

        private object l = new object();

        public RelaxedParser()
        {
        }

        // private bool IsScratch = false;
        // private bool DidClearScratch = false;

        public async Task<Result<Component[]>> Parse(Session session, RawAST ast, ISource source, CancellationToken token)
        {
            // [dho] @TODO just directly create our AST, rather than creating a TS
            // one and converting it! - 19/07/18

            var result = new Result<Component[]>();

            // var tsAST = new TypeScriptAST();
            /*
                var parser = new Parser();
                var sourceFile = parser.ParseSourceFile(fileName, source, ScriptTarget, null, false, ScriptKind.Ts);
             */

            string path = null;
            string text;

            switch (source.Kind)
            {
                case SourceKind.File:
                    {
                        path = ((ISourceFile)source).GetPathString();

                        // IsScratch = path.EndsWith("scratch.ts");

                        // lock (l)
                        // {
                        //     if (!IsScratch && !DidClearScratch)
                        //     {
                        //         System.IO.File.WriteAllText("../Samples/scratch/scratch.ts", "");
                        //         DidClearScratch = true;
                        //     }
                        // }

                        // // [dho] if the relative path starts with a '/' then Path.Combine
                        // // will just give us the relative path resolved to the root of the file system,
                        // // which is not what we want so we have to remove a trailing slash - 18/03/18
                        // if (relPath.StartsWith("/"))
                        // {
                        //     relPath = relPath.Substring(1);
                        // }

                        // path = Path.Combine(session.BaseDirectory.ToPathString(), relPath);

                        try
                        {
                            // [dho] TODO non blocking I/O - 13/08/18
                            text = File.ReadAllText(path);
                        }
                        catch (Exception e)
                        {
                            result.AddMessages(CreateErrorFromException(e));

                            return result;
                        }
                    }
                    break;

                case SourceKind.Literal:
                    {
                        text = ((ISourceLiteral)source).Text;
                    }
                    break;

                default:
                    {
                        result.AddMessages(
                            new Message(MessageKind.Error, $"Could not parse component from unsupported source kind : '{source.Kind}'")
                            {
                                Tags = DiagnosticTags
                            }
                        );

                        return result;
                    }
            }

            if (!HasErrors(result))
            {
                var context = new Context
                {
                    Session = session,
                    AST = ast,
                    Source = source
                };

                var conversionResult = ParseSourceFile(path, text, context, token);

                result.AddMessages(conversionResult.Messages);

                result.Value = new Component[] { ASTNodeFactory.Component(ast, (DataNode<string>)conversionResult.Value) };
            }

            return result;
        }


        private void PrintSnippet(Token token, Lexer lexer)
        {
            var lexeme = Lexeme(token, lexer);

            int tokenLinePos = lexer.SourceText.Length - lexeme.Length;
            int lineAfterPos = lexer.SourceText.Length;

            foreach (var ls in lexer.LineStarts)
            {
                if (ls <= token.StartPos)
                {
                    tokenLinePos = ls;
                }
                else if (ls < lineAfterPos)
                {
                    lineAfterPos = ls - 1; // remove new line
                }
            }


            var sourceLine = lexer.SourceText.Substring(tokenLinePos, lineAfterPos - tokenLinePos);

            var markerLine = "";

            for (int i = 0; i < token.StartPos - tokenLinePos; ++i)
            {
                markerLine += " ";
            }

            for (int i = 0; i < lexeme.Length; ++i)
            {
                markerLine += "^";
            }


            Console.WriteLine(sourceLine + "\n" + markerLine);
        }

        private string Lexeme(Token token, Lexer lexer)
        {
            if (token.Lexeme != null)
            {
                return token.Lexeme;
            }

            foreach (var kv in lexer.TokenMap)
            {
                if (kv.Value == token.Kind)
                {
                    return kv.Key;
                }
            }

            return string.Empty;
        }

        private Result<Token> NextToken(Lexer lexer, Context context, CancellationToken ct)
        {
            Result<Token> r;

            var s = lexer.Pos;

            while (true)
            {
                r = lexer.NextToken();

                if (!HasErrors(r))
                {
                    var token = r.Value;


                    if (token == null)
                    {
                        Console.WriteLine("TOKEN IS NULL. SOURCE TEXT:");
                        Console.WriteLine(lexer.SourceText.Substring(s, lexer.Pos - s));
                    }

                    if (token.Kind == SyntaxKind.SingleLineCommentTrivia || token.Kind == SyntaxKind.MultiLineCommentTrivia)
                    {
                        // Console.WriteLine("Here is a comment");
                        // Console.WriteLine(token.Lexeme);
                        continue;
                    }

                    if (token.Kind == SyntaxKind.ConflictMarkerTrivia)
                    {
                        continue;
                    }

                    if (token.Kind == SyntaxKind.EndOfFileToken)
                    {
                        int i = 0;
                    }

                    // if(token.Kind == SyntaxKind.Directive)
                    // {
                    //     int i = 0;
                    //     continue;
                    // }

                    // Console.WriteLine($"{token.StartPos} TOKEN : {token.Kind.ToString()} : {GetSymbolRole(token, lexer, context, ct)} : '{Lexeme(token, lexer)}'");
                }
                else
                {
                    int i = 0;
                }

                break;

            }


            return r;
        }

        private Result<Node> FinishNode(ASTNode nodeWrapper, /* Node meta, NodeID[] children,*/ Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var node = nodeWrapper.Node;

           

            // ASTHelpers.Commit(context.AST, node, meta, children);

            result.Value = node;

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SourceNodeOrigin CreateOrigin(Range range, Lexer lexer, Context context)
        {
            var (lineStart, columnStart) = lexer.GetLineAndCharacterOfPosition(range.Start);
            var (lineEnd, columnEnd) = lexer.GetLineAndCharacterOfPosition(range.End);

            var line = new Range(lineStart, lineEnd);
            var column = new Range(columnStart, columnEnd);

            // [dho] added lexeme to `SourceNodeOrigin` latterly, and I'm not about to refactor a
            // tonne of code just to more efficiently pass the lexeme through.. so we'll just do this
            // for now, and optimize if it becomes a problem - 05/04/19
            var lexeme = lexer.SourceText.Substring(range.Start, range.End - range.Start);

            return new SourceNodeOrigin(context.Source, line, column, range, lexeme);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public INodeOrigin CreateOrigin(Token token, Lexer lexer, Context context)
        {
            var endPos = token.StartPos + Lexeme(token, lexer).Length;

            var range = new Range(token.StartPos, endPos);

            return CreateOrigin(range, lexer, context);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FileMarker GetHint(Range range, Lexer lexer, Context context)
        {
            return Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(
                CreateOrigin(range, lexer, context)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FileMarker GetHint(Token token, Lexer lexer, Context context)
        {
            return Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(
                CreateOrigin(token, lexer, context)
            );
        }

        
        static int badfilecount = 0;
        private static void PrintMessages(string fileName, Lexer lexer, MessageCollection messages)
        {
            if (messages != null)
            {

                if (messages.Infos?.Count > 0)
                {
                    foreach (var info in messages.Infos)
                    {
                        Console.Out.WriteLine($"{fileName} ({info.Hint.LineNumber.Start + 1}, {info.Hint.ColumnIndex.Start + 1}) INFO : {info.Description}");
                        Console.Out.WriteLine(lexer.SourceText.Substring(info.Hint.Pos.Start));
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }

                if (messages.Warnings?.Count > 0)
                {
                    Console.WriteLine($"{fileName} {messages.Warnings.Count} warnings encountered:");

                    foreach (var warning in messages.Warnings)
                    {
                        Console.Out.WriteLine($"{fileName} ({warning.Hint.LineNumber.Start + 1}, {warning.Hint.ColumnIndex.Start + 1}) WARN : {warning.Description}");
                        Console.Out.WriteLine(lexer.SourceText.Substring(warning.Hint.Pos.Start));
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }

                if (messages.Errors?.Count > 0)
                {
                    Console.WriteLine($"{fileName} {messages.Errors.Count} errors encountered:");

                    foreach (var error in messages.Errors)
                    {
                        Console.Error.WriteLine($"{fileName} ({error.Hint.LineNumber.Start + 1}, {error.Hint.ColumnIndex.Start + 1}) ERROR : {error.Description}");
                        Console.Out.WriteLine(lexer.SourceText.Substring(error.Hint.Pos.Start));
                    }

                    // Console.WriteLine($"BAD FILE: " + fileName);

                    Console.WriteLine();
                    Console.WriteLine();

                }
            }
        }


        public Result<AST.Node> ParseSourceFile(string fileName, string sourceText, Context context, CancellationToken ct)
        {
            // Console.WriteLine("PARSING " + fileName);

            var result = new Result<AST.Node>();

            var childContext = ContextHelpers.CloneInKind(context, ContextKind.SourceElements);

            childContext.Flags = ContextFlags.JavaScriptFile;
            childContext.ErrorRecoveryMode = false;

            // childContext.ErrorBeforeNextFinishedNode = false;

            var lexer = new Lexer(sourceText);

            var token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] NOTE this may be > 0 if the file starts with comments - 17/03/19
            var startPos = token.StartPos;

            var children = result.AddMessages(
                ParseList(ParseStatement, token, lexer, childContext, ct)
            );

            if (!HasErrors(result))
            {
                EatIfNext(IsListTerminator, lexer, childContext, ct);

                if (lexer.Pos != sourceText.Length)
                {

                    throw new Exception("Excuse me, WTF?");
                }
            }


            var name = fileName;

            var range = new Range(startPos, lexer.Pos);

            var component = NodeFactory.Component(
                context.AST,
                // [dho] using childContext because the source file handle will be set correctly - 13/09/18
                CreateOrigin(range, lexer, childContext),
                name
            );

            result.AddMessages(AddOutgoingEdges(component, children, SemanticRole.None));

            result.Value = result.AddMessages(FinishNode(component, lexer, context, ct));


            // // [dho] TODO CLEANUP HACK quick and dirty way to print grouped
            // // diagnostic messages when chewing through a large number of files
            // // across multiple threads - 24/03/19
            // if (!IsScratch && HasErrors(result))
            // {
            //     lock (l)
            //     {
            //         System.IO.File.AppendAllText("../Samples/scratch/scratch.ts",
            //             $"\n\n// BAD FILE {fileName} :\n\n{lexer.SourceText}");
            //     }
            // }


            return result;
        }


        private delegate Result<Node> ParseDelegate(Token token, Lexer lexer, Context context, CancellationToken ct);


        private Result<Node[]> ParseList(ParseDelegate del, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node[]>();

            var list = new List<Node>();

            var lookAhead = lexer.Clone();

            while (true)
            {
                // [dho] `...)` 
                //           ^   - 10/02/19
                if (IsListTerminator(token, lookAhead, context, ct))
                {
                    // [dho] Do not eat the terminator - 10/02/19
                    break;
                }
                else if (IsListElement(token, lookAhead, context, ct))
                {
                    list.Add(
                        result.AddMessages(
                            del(token, lookAhead, context, ct)
                        )
                    );

                    if (HasErrors(result))
                    {
                        // [dho] attempt error recovery - 24/03/19
                        if (EatToBeforeNext(IsListElementOrListTerminator, lookAhead, context, ct))
                        {
                            if (!context.ErrorRecoveryMode)
                            {
                                context = ContextHelpers.Clone(context);
                                context.ErrorRecoveryMode = true;
                            }
                        }
                        else
                        {
                            // [dho] if we could not recover we bail out - 24/03/19
                            break;
                        }
                    }

                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                else
                {
                    // [dho] record the error - 24/03/19
                    result.AddMessages(
                        CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                    );

                    // [dho] attempt error recovery - 24/03/19
                    if (EatToBeforeNext(IsListElementOrListTerminator, lookAhead, context, ct))
                    {
                        lexer.Pos = lookAhead.Pos;

                        if (!context.ErrorRecoveryMode)
                        {
                            context = ContextHelpers.Clone(context);
                            context.ErrorRecoveryMode = true;
                        }

                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }
                    else
                    {
                        // [dho] if we could not recover we bail out - 24/03/19
                        break;
                    }
                }
            }

            result.Value = list.ToArray();

            return result;

        }

        Result<Node[]> ParseBracketedCommaDelimitedList(ParseDelegate del, SyntaxKind open, SyntaxKind close, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == open)
            {
                var result = new Result<Node[]>();

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (token.Kind == close)
                {
                    result.Value = null;
                    return result;
                }

                var elements = result.AddMessages(
                    ParseCommaDelimitedList(del, token, lexer, context, ct)
                );

                if (HasErrors(result))
                {
                    // [dho] attempt error recovery - 24/03/19
                    if (EatToBeforeNext((t, _, __, ___) => t.Kind == close, lexer, context, ct))
                    {
                        if (!context.ErrorRecoveryMode)
                        {
                            context = ContextHelpers.Clone(context);
                            context.ErrorRecoveryMode = true;
                        }
                    }
                    else
                    {
                        // [dho] if we could not jump to just before the close token we bail out - 24/03/19
                        return result;
                    }
                }


                var lookAhead = lexer.Clone();

                token = result.AddMessages(NextToken(lookAhead, context, ct));

                if (token.Kind == close)
                {
                    lexer.Pos = lookAhead.Pos;

                    result.Value = elements;
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                    );
                }


                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node[]>(token, lexer, context);
            }
        }

        private Result<Node[]> ParseCommaDelimitedList(ParseDelegate del, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node[]>();

            var list = new List<Node>();

            var lookAhead = lexer.Clone();

            while (true)
            {
                if (IsListElement(token, lookAhead, context, ct))
                {
                    list.Add(
                        result.AddMessages(
                            del(token, lookAhead, context, ct)
                        )
                    );

                    if (HasErrors(result))
                    {
                        // [dho] attempt error recovery - 24/03/19
                        if (EatToBeforeNext(IsCommaOrListTerminator, lookAhead, context, ct))
                        {
                            if (!context.ErrorRecoveryMode)
                            {
                                context = ContextHelpers.Clone(context);
                                context.ErrorRecoveryMode = true;
                            }
                        }
                        else
                        {
                            // [dho] if we could not recover we bail out - 24/03/19
                            break;
                        }
                    }

                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));

                    // [dho] `...,` 
                    //           ^   - 09/02/19
                    if (token.Kind == SyntaxKind.CommaToken)
                    {
                        lexer.Pos = lookAhead.Pos;
                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }
                    // [dho] `...)` 
                    //           ^   - 09/02/19
                    else if (IsListTerminator(token, lookAhead, context, ct))
                    {
                        // [dho] Do not eat the terminator - 09/02/19
                        break;
                    }
                    else
                    {
                        // [dho] record the error - 24/03/19
                        result.AddMessages(
                            CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                        );

                        // [dho] attempt error recovery - 24/03/19
                        if (EatToBeforeNext(IsListElementOrListTerminator, lookAhead, context, ct))
                        {
                            lexer.Pos = lookAhead.Pos;

                            if (!context.ErrorRecoveryMode)
                            {
                                context = ContextHelpers.Clone(context);
                                context.ErrorRecoveryMode = true;
                            }

                            token = result.AddMessages(NextToken(lookAhead, context, ct));
                        }
                        else
                        {
                            // [dho] if we could not recover we bail out - 24/03/19
                            break;
                        }
                    }
                }
                // [dho] `...)` 
                //           ^   - 09/02/19
                else if (IsListTerminator(token, lookAhead, context, ct))
                {
                    // [dho] Do not eat the terminator - 09/02/19
                    break;
                }
                else
                {
                    // [dho] record the error - 24/03/19
                    result.AddMessages(
                        CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                    );

                    // [dho] attempt error recovery - 24/03/19
                    if (EatToBeforeNext(IsListElementOrCommaOrListTerminator, lookAhead, context, ct))
                    {
                        lexer.Pos = lookAhead.Pos;

                        if (!context.ErrorRecoveryMode)
                        {
                            context = ContextHelpers.Clone(context);
                            context.ErrorRecoveryMode = true;
                        }

                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }
                    else
                    {
                        // [dho] if we could not recover we bail out - 24/03/19
                        break;
                    }
                }
            }

            result.Value = list.ToArray();

            return result;
        }

        // private OrderedGroup PrepareOrderedGroup(Token token, Lexer lexer, Context context)
        // {
        //     var og = NodeFactory.OrderedGroup(context.AST, CreateOrigin(token, lexer, context));

        //     og.Members = new NodeChildren(og);

        //     return og;
        // }

        // private void AddToOrderedGroup(OrderedGroup og, Node node)
        // {
        //     foreach (var (member, hasNext) in NodeInterrogation.IterateMembers(node))
        //     {
        //         og.Members.Add(member);
        //     }
        // }

        // private Node SimplifyOrderedGroup(OrderedGroup og)
        // {
        //     if (og?.Members != null)
        //     {
        //         var count = og.Members.Count;

        //         if (count == 1)
        //         {
        //             return og.Members[0];
        //         }
        //         else if (count > 1)
        //         {
        //             return og;
        //         }
        //     }

        //     return null;
        // }

        // public Result<Node> ParseToken(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     switch (token.Kind)
        //     {
        //         case SyntaxKind.SemicolonToken:
        //             return ParseEmptyStatement(token, lexer, context, ct);

        //         case SyntaxKind.OpenBraceToken:
        //             return ParseBlock(token, lexer, context, ct);

        //         case SyntaxKind.AtToken:
        //             return ParseDecorator(token, lexer, context, ct);

        //         case SyntaxKind.Identifier:
        //             return ParseIdentifierByRole(token, lexer, context, ct);

        //         default:
        //             return CreateUnsupportedTokenResult<Node>(token, lexer, context);
        //     }
        // }

        private Result<Node> ParseDirective(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var name = Lexeme(token, lexer);

            System.Diagnostics.Debug.Assert(name.StartsWith("#"));

            // [dho] remove the hash from the name - 18/05/19
            name = name.Substring(1);

            // [dho] `#run ...` 
            //             ^^^      - 17/04/19
            token = result.AddMessages(NextToken(lexer, context, ct));
            
            var expContext = ContextHelpers.Clone(context);
            expContext.Flags &= ~ContextFlags.DisallowInContext;

            var operand = result.AddMessages(
                name == Sempiler.Core.Directives.CTDirective.CodeGen ? 
                ParseExpression(token, lexer, expContext, ct)
                : ParseStatement(token, lexer, expContext, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            Node directive = result.AddMessages(FinishNode(
                NodeFactory.Directive(context.AST, CreateOrigin(range, lexer, context), name),
                lexer, context, ct)
            );

            result.AddMessages(AddOutgoingEdges(context.AST, directive, operand, SemanticRole.Operand));

            result.Value = directive;

            return result;
        }


        private Result<Node> ParseSuperExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            /**
                [dho] I think we need to refactor this so it does not impose the constraints on `super`
                that is a TypeScript restriction.. other language semantics may not restrict the user of super
                in the same way - 05/02/19
             */


            var result = new Result<Node>();

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `super` 
            //        ^^^^^   - 05/02/19
            if (role == SymbolRole.SuperContextReference)
            {
                var superContextRef = NodeFactory.SuperContextReference(context.AST, CreateOrigin(token, lexer, context));

                // [dho] `super(` 
                //             ^   - 05/02/19
                // [dho] `super.` 
                //             ^   - 05/02/19
                // [dho] `super[` 
                //             ^   - 05/02/19
                if (LookAhead(IsOpenParenOrDotOrOpenBracket, lexer, context, ct).Item1)
                {
                    result.Value = result.AddMessages(FinishNode(superContextRef, lexer, context, ct));
                }
                // [dho] TODO INVESTIGATE I'm a bit confused by the original source here, because we already look
                // for a dot in the predicate above, so this predicate will never be hit?? - 23/03/19
                else if (LookAhead(IsDot, lexer, context, ct).Item1)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    if (HasErrors(result)) return result;

                    Node member = result.AddMessages(
                        ParseRightSideOfDot(/*allowIdentifierNames true*/ token, lexer, context, ct)
                    );

                    // [dho] The qualified access creates the incident object on 
                    // the left hand side of the expression, eg. (((A.B).C).D) - 29/08/18
                    var qualifiedAccess = NodeFactory.QualifiedAccess(
                        context.AST,
                        CreateOrigin(token, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(qualifiedAccess, 
                        result.AddMessages(FinishNode(superContextRef, lexer, context, ct)), 
                        SemanticRole.Incident
                    ));

                    result.AddMessages(AddOutgoingEdges(qualifiedAccess, member, SemanticRole.Member));

                    result.Value = result.AddMessages(FinishNode(qualifiedAccess, lexer, context, ct));
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedTokenResult<Node>(token, lexer, context)
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );
            }

            return result;
        }



        public Result<Node> ParseRightSideOfDot(/* bool allowIdentifierNames,*/ Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `.` 
            //        ^   - 08/02/19
            if (token.Kind == SyntaxKind.DotToken)
            {
                var result = new Result<Node>();

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (token.PrecedingLineBreak && IsKnownSymbolRole(token, lexer, context, ct))
                {
                    var lookAhead = lexer.Clone();

                    // [dho] token is identifier or keyword on same line - 10/02/19
                    var (matchesPattern, _) = LookAhead(IsKnownSymbolRoleOnSameLine, lexer, context, ct);

                    if (matchesPattern)
                    {
                        result.AddMessages(CreateErrorResult<Node>(token, "Identifier expected", lexer, context));

                        return result;
                    }
                }

                result.Value = result.AddMessages(
                    // [dho] allow 'keywords' for names - 23/03/19
                    /* ParseIdentifier */ParseKnownSymbolRoleAsIdentifier(token, lexer, context, ct)
                );

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }

        }


        private bool IsKnownSymbolRole(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return GetSymbolRole(token, lexer, context, ct) != SymbolRole.None;
        }

        private bool IsSmartCast(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak && GetSymbolRole(token, lexer, context, ct) == SymbolRole.SmartCast;
        }

        private bool IsAsteriskAsterisk(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.AsteriskAsteriskToken;
        }

        private bool IsOpenParenOrDotOrOpenBracket(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.OpenParenToken || token.Kind == SyntaxKind.DotToken || token.Kind == SyntaxKind.OpenBracketToken;
        }

        private bool IsDot(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.DotToken;
        }

        private bool IsOpenBracketOrOpenBrace(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] check for `[` or `(` - 01/02/19
            return token.Kind == SyntaxKind.OpenBracketToken ||
                        token.Kind == SyntaxKind.OpenBraceToken;
        }

        private bool IsOpenParenOrLessThan(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] check for `(` or `<` - 01/02/19
            return token.Kind == SyntaxKind.OpenParenToken ||
                        token.Kind == SyntaxKind.LessThanToken;
        }

        private bool IsEqualsGreaterThan(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] check for `=>` - 26/02/19
            return token.Kind == SyntaxKind.EqualsGreaterThanToken;
        }

        private bool IsKnownSymbolRoleOnSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak && IsKnownSymbolRole(token, lexer, context, ct);
        }

        private bool IsIdentifierOnSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak && IsIdentifier(token, lexer, context, ct);
        }

        private bool IsIdentifierOrStringLiteralOnSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak &&
                (token.Kind == SyntaxKind.StringLiteral || IsIdentifier(token, lexer, context, ct));
        }

        private bool IsLessThanOnSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak && token.Kind == SyntaxKind.LessThanToken;
        }

        private bool IsDotOnSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak && token.Kind == SyntaxKind.DotToken;
        }

        private bool IsIdentifierOrOpenBraceOrExport(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                return true;
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            return role == SymbolRole.Identifier || role == SymbolRole.Export;
        }

        private bool IsStringLiteralOrAsteriskOrOpenBraceOrKnownSymbolRole(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.StringLiteral || token.Kind == SyntaxKind.AsteriskToken || token.Kind == SyntaxKind.OpenBraceToken)
            {
                return true;
            }

            return IsKnownSymbolRole(token, lexer, context, ct);
        }

        private bool IsEqualsOrAsteriskOrOpenBraceOrDefaultOrReferenceAliasOrIsDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.EqualsToken || token.Kind == SyntaxKind.AsteriskToken || token.Kind == SyntaxKind.OpenBraceToken)
            {
                return true;
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.Default || role == SymbolRole.ReferenceAlias)
            {
                return true;
            }

            return IsDeclaration(token, lexer, context, ct);
        }

        private bool IsSameLineButNotSemicolon(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind != SyntaxKind.SemicolonToken && IsSameLine(token, lexer, context, ct);
        }

        private bool IsSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return !token.PrecedingLineBreak;
        }

        private bool IsQuestion(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.QuestionToken;
        }

        private bool IsColon(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.ColonToken;
        }

        private bool IsCommaOrCloseBraceOrEquals(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.CommaToken || token.Kind == SyntaxKind.CloseBraceToken || token.Kind == SyntaxKind.EqualsToken;
        }

        private Result<Node> ParseLeftHandSideExpressionOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var role = GetSymbolRole(token, lexer, context, ct);

            var expression = result.AddMessages(
                role == SymbolRole.SuperContextReference ?
                ParseSuperExpression(token, lexer, context, ct) :
                ParseMemberExpressionOrHigher(token, lexer, context, ct)
            );

            result.Value = result.AddMessages(
                ParseCallExpressionRest(expression, lexer, context, token.StartPos, ct)
            );

            return result;
        }

        private Result<Node> ParseBinaryExpressionRest(int precedence, Node leftOperand, int startPos, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var lookAhead = lexer.Clone();

            while (true)
            {
                var token = result.AddMessages(NextToken(lookAhead, context, ct));

                if (HasErrors(result))
                {
                    break;
                }

                if (token.Kind == SyntaxKind.GreaterThanToken)
                {
                    // We either have a binary operator here, or we're finished.  We call
                    // reScanGreaterToken so that we merge token sequences like > and = into >=
                    token = result.AddMessages(TokenUtils.ReScanGreaterThanToken(token, lookAhead));
                }

                var newPrecedence = GetBinaryOperatorPrecedence(token, lookAhead, context, ct);

                var consumeCurrentOperator = token.Kind == SyntaxKind.AsteriskAsteriskToken ?
                                                                            newPrecedence >= precedence :
                                                                            newPrecedence > precedence;

                if (!consumeCurrentOperator)
                {
                    break;
                }

                var role = GetSymbolRole(token, lookAhead, context, ct);

                // [dho] `in` 
                //        ^^   - 17/03/19
                if (role == SymbolRole.KeyIn && ((context.Flags & ContextFlags.DisallowInContext) == ContextFlags.DisallowInContext))
                {
                    break;
                }

                // [dho] `as` 
                //        ^^   - 17/03/19
                if (role == SymbolRole.ReferenceAlias)
                {
                    if (token.PrecedingLineBreak)
                    {
                        break;
                    }
                    else
                    {
                        token = result.AddMessages(NextToken(lookAhead, context, ct));

                        var type = result.AddMessages(ParseType(token, lookAhead, context, ct));

                        lexer.Pos = lookAhead.Pos;

                        var range = new Range(startPos, lookAhead.Pos);

                        var subject = leftOperand;
                        var targetType = type;

                        leftOperand = result.AddMessages(FinishNode(
                            NodeFactory.SafeCast(context.AST, CreateOrigin(range, lookAhead, context)),
                            lookAhead, context, ct)
                        );

                        result.AddMessages(AddOutgoingEdges(context.AST, leftOperand, subject, SemanticRole.Subject));
                        result.AddMessages(AddOutgoingEdges(context.AST, leftOperand, targetType, SemanticRole.TargetType));
                    }
                }
                else
                {
                    var op = token;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));

                    var rightOperand = result.AddMessages(
                        ParseBinaryExpressionOrHigher(newPrecedence, token, lookAhead, context, ct)
                    );

                    lexer.Pos = lookAhead.Pos;

                    var range = new Range(startPos, lookAhead.Pos);

                    leftOperand = result.AddMessages(
                        MakeBinaryExpression(leftOperand, op, rightOperand, range, lookAhead, context, ct)
                    );
                }

            }

            result.Value = leftOperand;

            return result;
        }

        private Result<Node> ParseCallExpressionRest(Node expression, Lexer lexer, Context context, int startPos, CancellationToken ct)
        {
            var result = new Result<Node>();

            var lookAhead = lexer.Clone();
            var template = default(Node[]);
            var args = default(Node[]);

            while (true)
            {
                if (HasErrors(result))
                {
                    break;
                }

                expression = result.AddMessages(
                    ParseMemberExpressionRest(expression, lookAhead, context, ct)
                );


                if (HasErrors(result))
                {
                    break;
                }

                lexer.Pos = lookAhead.Pos;

                var token = result.AddMessages(NextToken(lookAhead, context, ct));


                // [dho] `exp<` 
                //           ^   - 08/02/19
                if (token.Kind == SyntaxKind.LessThanToken)
                {
                    // [dho] the `<` may or may not be type arguments
                    // for a call expression, so we try to parse them
                    // and if we cannot, we just break out of the loop 
                    // because we are done - 09/02/19
                    var r = ParseTypeArgumentsInExpression(token, lookAhead, context, ct);

                    // [dho] no type arguments found - 09/02/19
                    if (HasErrors(r) || r.Value == null)
                    {
                        break;
                    }

                    template = r.Value;

                    lexer.Pos = lookAhead.Pos;

                    // token = result.AddMessages(NextToken(lookAhead, context, ct));
                    continue;
                }

                // [dho] `exp(` 
                //           ^   - 08/02/19
                if (token.Kind == SyntaxKind.OpenParenToken)
                {
                    args = result.AddMessages(
                        // [dho] `exp(...` 
                        //            ^^^   - 08/02/19
                        ParseArguments(token, lookAhead, context, ct)
                    );

                    var range = new Range(startPos, lookAhead.Pos);

                    var invocation = NodeFactory.Invocation(
                        context.AST,
                        CreateOrigin(range, lookAhead, context)
                    );

                    // [dho] TODO discard outer Association if its an IIFE, and set expression as the FE - 30/05/19
                    // if(expression.Kind == SemanticKind.Association)
                    // {
                    //     var assoc = ASTNodeFactory.Association(context.AST, expression);

                    //     if(assoc.Subject is FunctionLikeDeclaration)
                    //     {

                    //     }
                    // }

                    result.AddMessages(AddOutgoingEdges(invocation, expression, SemanticRole.Subject));
                    result.AddMessages(AddOutgoingEdges(invocation, template, SemanticRole.Template));
                    result.AddMessages(AddOutgoingEdges(invocation, args, SemanticRole.Argument));


                    expression = result.AddMessages(FinishNode(invocation, lookAhead, context, ct));

                    lexer.Pos = lookAhead.Pos;

                    // token = result.AddMessages(NextToken(lookAhead, context, ct));

                    continue;
                }
                else
                {
                    break;
                }
            }

            result.Value = expression;

            return result;
        }

        private Result<Node[]> ParseTypeArgumentsInExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `<` 
            //        ^      - 09/02/19
            if (token.Kind == SyntaxKind.LessThanToken)
            {
                var result = new Result<Node[]>();

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result))
                {
                    return result;
                }

                var argsContext = ContextHelpers.CloneInKind(context, ContextKind.TypeArguments);

                var args = result.AddMessages(
                    // [dho] `<...` 
                    //         ^^^   - 09/02/19
                    ParseCommaDelimitedList(ParseType, token, lexer, argsContext, ct)
                );

                if (HasErrors(result))
                {
                    return result;
                }


                var lookAhead = lexer.Clone();

                token = result.AddMessages(NextToken(lookAhead, context, ct));

                // [dho] `<...>` 
                //            ^   - 09/02/19
                if (token.Kind == SyntaxKind.GreaterThanToken)
                {
                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                    );
                }

                if (HasErrors(result))
                {
                    return result;
                }

                // [dho] `<...>(` 
                //             ^   - 09/02/19
                if (CanFollowTypeArgumentsInExpression(token))
                {
                    // [dho] NOTE we do not advance the lexer
                    // because we are just checking that the next token
                    // implies that the sequence we parsed is a type arguments
                    // list, we don't actually want to eat that token - 09/02/19
                    result.Value = args;
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                    );
                }

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node[]>(token, lexer, context);
            }
        }

        private bool CanFollowTypeArgumentsInExpression(Token token)
        {
            switch (token.Kind)
            {
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.DotToken:
                case SyntaxKind.CloseParenToken:
                case SyntaxKind.CloseBracketToken:
                case SyntaxKind.ColonToken:
                case SyntaxKind.SemicolonToken:
                case SyntaxKind.QuestionToken:
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.EqualsEqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsToken:
                case SyntaxKind.ExclamationEqualsEqualsToken:
                case SyntaxKind.AmpersandAmpersandToken:
                case SyntaxKind.BarBarToken:
                case SyntaxKind.CaretToken:
                case SyntaxKind.AmpersandToken:
                case SyntaxKind.BarToken:
                case SyntaxKind.CloseBraceToken:
                case SyntaxKind.EndOfFileToken:
                    // foo<x>
                    // these cases can't legally follow a type arg list.  However, they're not legal
                    // expressions either.  The user is probably in the middle of a generic type. So
                    // treat it as such.
                    return true;

                case SyntaxKind.CommaToken:
                case SyntaxKind.OpenBraceToken:
                default:

                    // Anything else treat as an expression.
                    return false;
            }
        }



        private Result<Node[]> ParseArguments(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `(` 
            //        ^   - 08/02/19
            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                var argsContext = ContextHelpers.CloneInKind(context, ContextKind.ArgumentExpressions);

                return ParseBracketedCommaDelimitedList(ParseArgumentExpression, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken, token, lexer, argsContext, ct);
            }
            else
            {
                return CreateUnsupportedTokenResult<Node[]>(token, lexer, context);
            }
        }

        private Result<Node> ParseArgumentExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var expContext = ContextHelpers.Clone(context);

            expContext.Flags &= ~(ContextFlags.DisallowInContext | ContextFlags.DecoratorContext);

            return ParseArgumentOrArrayLiteralElement(token, lexer, expContext, ct);
        }

        private Result<Node> ParseMemberExpressionRest(Node expression, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var lookAhead = lexer.Clone();

            var token = result.AddMessages(NextToken(lookAhead, context, ct));

            while (true)
            {
                if (HasErrors(result))
                {
                    break;
                }

                // [dho] `exp.` 
                //           ^   - 05/02/19
                if (token.Kind == SyntaxKind.DotToken)
                {
                    var right = result.AddMessages(
                        ParseRightSideOfDot(/*allowIdentifierNames true,*/ token, lookAhead, context, ct)
                    );

                    var incident = expression;
                    var member = right;

                    expression = result.AddMessages(FinishNode(
                        NodeFactory.QualifiedAccess(
                        context.AST,
                        CreateOrigin(token, lookAhead, context)
                    ), lookAhead, context, ct));

                    result.AddMessages(AddOutgoingEdges(context.AST, expression, incident, SemanticRole.Incident));
                    result.AddMessages(AddOutgoingEdges(context.AST, expression, member, SemanticRole.Member));

                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                // [dho] `exp!` 
                //           ^   - 05/02/19
                else if (token.Kind == SyntaxKind.ExclamationToken && !token.PrecedingLineBreak)
                {
                    if(LookAhead(IsDotOnSameLine, lookAhead, context, ct).Item1)
                    {
                        lexer.Pos = lookAhead.Pos;

                        var subject = expression;

                        expression = result.AddMessages(FinishNode(NodeFactory.NotNull(
                            context.AST,
                            CreateOrigin(token, lookAhead, context)
                        ), lookAhead, context, ct));

                        result.AddMessages(AddOutgoingEdges(context.AST, expression, subject, SemanticRole.Subject));

                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }
                    else
                    {
                        result.Value = expression;
                        break;
                    }
                }
                // [dho] `exp?` 
                //           ^   - 01/10/19
                else if (token.Kind == SyntaxKind.QuestionToken && !token.PrecedingLineBreak)
                {
                    if(LookAhead(IsDotOnSameLine, lookAhead, context, ct).Item1)
                    {
                        lexer.Pos = lookAhead.Pos;

                        var subject = expression;

                        expression = result.AddMessages(FinishNode(NodeFactory.MaybeNull(
                            context.AST,
                            CreateOrigin(token, lookAhead, context)
                        ), lookAhead, context, ct));

                        result.AddMessages(AddOutgoingEdges(context.AST, expression, subject, SemanticRole.Subject));

                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }
                    else
                    {
                        result.Value = expression;
                        break;
                    }
                }
                // [dho] if NOT in decorator context - 09/02/19
                else if (((context.Flags & ContextFlags.DecoratorContext) == 0) &&
                        // [dho] `exp[` 
                        //           ^   - 05/02/19
                        token.Kind == SyntaxKind.OpenBracketToken)
                {
                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));

                    Node member = default(Node);

                    if (token.Kind != SyntaxKind.CloseBracketToken)
                    {
                        var expContext = ContextHelpers.Clone(context);
                        // [dho] allow `in expressions` when parsing the expression - 09/02/19
                        expContext.Flags &= ~ContextFlags.DisallowInContext;

                        // [dho] `exp[...]` 
                        //            ^^^   - 05/02/19
                        member = result.AddMessages(
                            ParseExpression(token, lookAhead, expContext, ct)
                        );

                        lexer.Pos = lookAhead.Pos;

                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }

                    // [dho] `exp[...]` 
                    //               ^   - 05/02/19
                    if (token.Kind != SyntaxKind.CloseBracketToken)
                    {
                        result.AddMessages(
                            CreateUnsupportedTokenResult<Node>(token, lookAhead, context)
                        );
                    }
                    else
                    {
                        var incident = expression;
                    
                        expression = result.AddMessages(FinishNode(NodeFactory.IndexedAccess(
                            context.AST,
                            CreateOrigin(token, lookAhead, context)
                        ), lookAhead, context, ct));

                        
                        result.AddMessages(AddOutgoingEdges(context.AST, expression, incident, SemanticRole.Incident));
                        result.AddMessages(AddOutgoingEdges(context.AST, expression, member, SemanticRole.Member));


                        lexer.Pos = lookAhead.Pos;

                        token = result.AddMessages(NextToken(lookAhead, context, ct));
                    }
                }
                else if (token.Kind == SyntaxKind.TemplateHead)
                {
                    var members = result.AddMessages(
                        ParseTemplateExpressionMembers(token, lookAhead, context, ct)
                    );

                    // [dho] Tagged Template Literal...??
                    var interp = NodeFactory.InterpolatedString(
                        context.AST,
                        CreateOrigin(token, lookAhead, context)
                    );

                    result.AddMessages(AddOutgoingEdges(interp, expression, SemanticRole.ParserName));
                    result.AddMessages(AddOutgoingEdges(interp, members, SemanticRole.Member));

                    expression = result.AddMessages(FinishNode(interp, lookAhead, context, ct));

                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                // [dho] `exp`foo`` 
                //           ^   - 05/02/19
                else if (token.Kind == SyntaxKind.NoSubstitutionTemplateLiteral)
                {
                    var members = result.AddMessages(
                        ParseNoSubstitutionTemplateLiteral(token, lookAhead, context, ct)
                    );

                    // [dho] Tagged Template Literal...??
                    var interp = NodeFactory.InterpolatedString(
                        context.AST,
                        CreateOrigin(token, lookAhead, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(interp, expression, SemanticRole.ParserName));
                    result.AddMessages(AddOutgoingEdges(interp, members, SemanticRole.Member));

                    expression = result.AddMessages(FinishNode(interp, lookAhead, context, ct));

                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                else
                {
                    result.Value = expression;
                    break;
                }
            }

            return result;
        }

        private Result<Node[]> ParseTemplateExpressionMembers(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node[]>();

            var list = new List<Node>();

            // var startPos = token.StartPos;

            if (token.Kind == SyntaxKind.TemplateHead)
            {
                var lexeme = Lexeme(token, lexer);

                var range = new Range(token.StartPos, token.StartPos + lexeme.Length);

                var interpString = NodeFactory.InterpolatedStringConstant(context.AST, CreateOrigin(range, lexer, context), lexeme);

                list.Add(
                    result.AddMessages(
                        FinishNode(interpString, lexer, context, ct)
                    )
                );

                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "Expected template head", lexer, context)
                );

                return result;
            }

            var lookAhead = lexer.Clone();

            while (token != null)
            {
                if (token.Kind == SyntaxKind.CloseBraceToken)
                {
                    token = result.AddMessages(TokenUtils.ReScanTemplateToken(token, lookAhead));
                }

                if (HasErrors(result))
                {
                    return result;
                }


                if (token.Kind == SyntaxKind.TemplateMiddle)
                {
                    lexer.Pos = lookAhead.Pos;

                    var lexeme = Lexeme(token, lexer);

                    var range = new Range(token.StartPos, token.StartPos + lexeme.Length);

                    var interpString = NodeFactory.InterpolatedStringConstant(context.AST, CreateOrigin(range, lexer, context), lexeme);

                    list.Add(
                        result.AddMessages(
                            FinishNode(interpString, lexer, context, ct)
                        )
                    );

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                else if (token.Kind == SyntaxKind.TemplateTail)
                {
                    lexer.Pos = lookAhead.Pos;

                    var lexeme = Lexeme(token, lexer);

                    var range = new Range(token.StartPos, token.StartPos + lexeme.Length);

                    var interpString = NodeFactory.InterpolatedStringConstant(context.AST, CreateOrigin(range, lexer, context), lexeme);

                    list.Add(
                        result.AddMessages(
                            FinishNode(interpString, lexer, context, ct)
                        )
                    );

                    break; // end the while loop
                }
                else
                {
                    var expContext = ContextHelpers.Clone(context);

                    expContext.Flags &= ~ContextFlags.DisallowInContext;

                    list.Add(
                        result.AddMessages(
                            ParseExpression(token, lookAhead, expContext, ct)
                        )
                    );

                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
            }


            // var interp = NodeFactoryHelpers.CreateInterpolatedString(
            //     context.AST, 
            //     CreateOrigin(new Range(startPos, lexer.Pos), lexer, context), 
            //     SimplifyOrderedGroup(og)
            // );

            result.Value = list.ToArray();//result.AddMessages(FinishNode(interp, lexer, context, ct));

            return result;

        }



        private Result<Node> ParseNoSubstitutionTemplateLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            if (token.Kind == SyntaxKind.NoSubstitutionTemplateLiteral)
            {
                var lexeme = Lexeme(token, lexer);

                var range = new Range(token.StartPos, token.StartPos + lexeme.Length);

                var interpString = NodeFactory.InterpolatedStringConstant(context.AST, CreateOrigin(range, lexer, context), lexeme);

                result.Value = result.AddMessages(
                    FinishNode(interpString, lexer, context, ct)
                );
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "Expected no substitution template literal", lexer, context)
                );
            }

            return result;
        }

        private Result<Node> ParseRegularExpressionLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] informed by https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_Expressions - 22/03/19

            Result<Node> result = new Result<Node>();

            var lexeme = Lexeme(token, lexer);

            string pattern = string.Empty;

            if (!String.IsNullOrEmpty(lexeme))
            {
                if (lexeme[0] == '/')
                {
                    var lastSlashIndex = lexeme.LastIndexOf('/');

                    if (lastSlashIndex >= 1)
                    {
                        var meta = new List<Node>();

                        pattern = lexeme.Substring(1, lastSlashIndex - 1);

                        var flagString = lexeme.Substring(lastSlashIndex + 1);

                        for (var i = 0; i < flagString.Length; ++i)
                        {
                            var character = flagString[i];

                            MetaFlag flags = 0x0;

                            switch (character)
                            {
                                case 'g': // global search
                                    flags |= MetaFlag.GlobalSearch;
                                    break;

                                case 'i': // case-insensitive search
                                    flags |= MetaFlag.CaseInsensitive;
                                    break;

                                case 'm': // multi-line search
                                    flags |= MetaFlag.MultiLineSearch;
                                    break;

                                case 's': // Allows '.' to match newline characters
                                    flags |= MetaFlag.DotsAsNewLines;
                                    break;

                                case 'u': // Treat pattern as sequence of unicode code points
                                    flags |= MetaFlag.UnicodeCodePoints;
                                    break;

                                case 'y': // perform a sticky search that matches starting at the current position in the target string
                                    flags |= MetaFlag.StickySearch;
                                    break;

                                default: // unsupported flag
                                    {
                                        result.AddMessages(
                                            CreateErrorResult<Node>(token, $"Unsupported regex flag '{character}'", lexer, context)
                                        );
                                    }
                                    continue;
                            }

                            var s = token.StartPos + lastSlashIndex + i;

                            var e = s + 1 /* character length */;

                            var m = result.AddMessages(FinishNode(NodeFactory.Meta(
                                context.AST,
                                CreateOrigin(new Range(s, e), lexer, context),
                                flags
                            ), lexer, context, ct));

                            meta.Add(m);
                        }

                        if (!HasErrors(result))
                        {
                            var range = new Range(token.StartPos, token.StartPos + lexeme.Length);

                            var regex = NodeFactory.RegularExpressionConstant(context.AST, CreateOrigin(range, lexer, context), pattern);

                            result.AddMessages(AddOutgoingEdges(regex, meta, SemanticRole.Meta));

                            result.Value = result.AddMessages(FinishNode(regex, lexer, context, ct));

                            // [dho] TODO pattern codes are not x-platform...
                            // we need to properly parse regular expressions and create 
                            // a tree from them - 22/03/19
                            result.AddMessages(
                                new Message(MessageKind.Warning, "Regular expression patterns are currently treated as literal values that may be incompatible with the target platform")
                                {
                                    Hint = GetHint(token, lexer, context),
                                    Tags = DiagnosticTags
                                }
                            );
                        }
                    }
                    else
                    {
                        result.AddMessages(
                            CreateErrorResult<Node>(token, $"Regular expression has invalid format '{lexeme}'", lexer, context)
                        );
                    }
                }
                else
                {
                    result.AddMessages(
                        CreateErrorResult<Node>(token, $"Regular expression has invalid format '{lexeme}'", lexer, context)
                    );
                }
            }


            return result;
        }

        private Result<Node> ParseMemberExpressionOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var exp = result.AddMessages(
                ParsePrimaryExpression(token, lexer, context, ct)
            );

            result.Value = result.AddMessages(
                ParseMemberExpressionRest(exp, lexer, context, ct)
            );

            return result;
        }

        private Result<Node> ParseParenthesizedExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                var result = new Result<Node>();

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result))
                {
                    return result;
                }

                var incidentContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 09/02/19
                incidentContext.Flags &= ~ContextFlags.DisallowInContext;

                var incident = result.AddMessages(
                    ParseExpression(token, lexer, incidentContext, ct)
                );

                var lookAhead = lexer.Clone();

                token = result.AddMessages(NextToken(lookAhead, context, ct));

                if (token.Kind == SyntaxKind.CloseParenToken)
                {
                    lexer.Pos = lookAhead.Pos;

                    var assoc = NodeFactory.Association(context.AST, CreateOrigin(token, lexer, context));

                    result.AddMessages(AddOutgoingEdges(assoc, incident, SemanticRole.Subject));

                    result.Value = result.AddMessages(FinishNode(assoc, lexer, context, ct));
                }
                else
                {
                    result.AddMessages(CreateUnsupportedTokenResult<Node>(token, lexer, context));
                }

                return result;

            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        private Result<Node> ParseExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            // [dho] come out of decorator context - 09/02/19
            var expContext = ContextHelpers.Clone(context);
            expContext.Flags &= ~ContextFlags.DecoratorContext;

            var exp = result.AddMessages(
                ParseAssignmentExpressionOrHigher(token, lexer, expContext, ct)
            );

            var lookAhead = lexer.Clone();

            token = result.AddMessages(NextToken(lookAhead, context, ct));

            while (!HasErrors(result))
            {
                // [dho] TODO CHECK why is this a comma token as
                // a binary operator?! - 09/02/19
                if (token.Kind == SyntaxKind.CommaToken)
                {
                    var operatorToken = token;

                    token = result.AddMessages(NextToken(lookAhead, context, ct));

                    var left = exp;

                    var right = result.AddMessages(
                        ParseAssignmentExpressionOrHigher(token, lookAhead, expContext, ct)
                    );

                    lexer.Pos = lookAhead.Pos;

                    var range = new Range(startPos, lexer.Pos); // [dho] is this startPos always the same? - 20/02/19

                    exp = result.AddMessages(
                        MakeBinaryExpression(left, token, right, range, lexer, expContext, ct)
                    );


                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
                else
                {
                    break;
                }
            }

            result.Value = exp;

            return result;
        }

        private Result<Node> MakeBinaryExpression(Node leftOperand, Token op, Node rightOperand, Range range, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var binExp = default(ASTNode);

            switch (op.Kind)
            {
                case SyntaxKind.AmpersandToken: // x & y

                    binExp = NodeFactory.BitwiseAnd(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.AmpersandEqualsToken: // x &= y
                {
                    binExp = NodeFactory.BitwiseAndAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.AmpersandAmpersandToken: // x && y
                    binExp = NodeFactory.LogicalAnd(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.AsteriskToken: // x * y
                    binExp = NodeFactory.Multiplication(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.AsteriskEqualsToken: // x *= y
                {
                    binExp = NodeFactory.MultiplicationAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.AsteriskAsteriskToken: // x ** y
                    binExp = NodeFactory.Exponentiation(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;


                case SyntaxKind.AsteriskAsteriskEqualsToken: // x **= y
                {
                    binExp = NodeFactory.ExponentiationAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.BarToken: // x | y
                    binExp = NodeFactory.BitwiseOr(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.BarEqualsToken: // x |= y
                {
                    binExp = NodeFactory.BitwiseOrAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.BarBarToken: // x || y
                    // [dho] whilst this may actually be a null coalesce
                    // rather than logical or, we should deal with that in a transformer
                    // rather than make the parser have to care about those semantics - 05/10/18
                    binExp = NodeFactory.LogicalOr(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.CaretToken: // x ^ y
                    binExp = NodeFactory.BitwiseExclusiveOr(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.CaretEqualsToken: // x ^= y
                {
                    binExp = NodeFactory.BitwiseExclusiveOrAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.EqualsToken: // x = y
                {
                    binExp = NodeFactory.Assignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }    
                    break;

                case SyntaxKind.EqualsEqualsToken: // x == y
                    binExp = NodeFactory.LooseEquivalent(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.EqualsEqualsEqualsToken: // x === y
                    binExp = NodeFactory.StrictEquivalent(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.ExclamationEqualsToken: // x != y
                    binExp = NodeFactory.LooseNonEquivalent(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.ExclamationEqualsEqualsToken: // x !== y
                    binExp = NodeFactory.StrictNonEquivalent(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.GreaterThanToken: // x > y
                    binExp = NodeFactory.StrictGreaterThan(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.GreaterThanEqualsToken: // x >= y
                    binExp = NodeFactory.StrictGreaterThanOrEquivalent(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;


                case SyntaxKind.GreaterThanGreaterThanToken: // x >> y
                    binExp = NodeFactory.BitwiseRightShift(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.GreaterThanGreaterThanEqualsToken: // x >>= y
                {
                    binExp = NodeFactory.BitwiseRightShiftAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken: // x >>> y
                    binExp = NodeFactory.BitwiseUnsignedRightShift(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken: // x >>>= y
                {
                    binExp = NodeFactory.BitwiseUnsignedRightShiftAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.LessThanToken: // x < y
                    binExp = NodeFactory.StrictLessThan(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.LessThanEqualsToken: // x <= y
                    binExp = NodeFactory.StrictLessThanOrEquivalent(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.LessThanLessThanToken: // x << y
                    binExp = NodeFactory.BitwiseLeftShift(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.LessThanLessThanEqualsToken: // x <<= y
                {
                    binExp = NodeFactory.BitwiseLeftShiftAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                    
                }    
                    break;

                case SyntaxKind.MinusToken: // x - y
                    binExp = NodeFactory.Subtraction(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.MinusEqualsToken: // x -= y
                {
                    binExp = NodeFactory.SubtractionAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.PercentToken: // x % y
                    binExp = NodeFactory.Remainder(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.PercentEqualsToken:// x %= y
                {   
                    binExp = NodeFactory.RemainderAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }    
                break;

                case SyntaxKind.PlusToken: // x + y
                    // [dho] whilst this may actually be a concatenation
                    // rather than addition, we should deal with that in a transformer
                    // rather than make the parser have to care about those semantics - 05/10/18
                    binExp = NodeFactory.Addition(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;
                case SyntaxKind.PlusEqualsToken: // x += y
                {
                    // [dho] whilst this may actually be a concatenation assignment
                    // rather than addition assignment, we should deal with that in a transformer
                    // rather than make the parser have to care about those semantics - 05/10/18
                    binExp = NodeFactory.AdditionAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );
                    
                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                case SyntaxKind.SlashToken: // x / y
                    binExp = NodeFactory.Division(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    result.AddMessages(AddOutgoingEdges(binExp, leftOperand, SemanticRole.Operand));
                    result.AddMessages(AddOutgoingEdges(binExp, rightOperand, SemanticRole.Operand));
                    break;

                case SyntaxKind.SlashEqualsToken: // x /= y
                {    
                    binExp = NodeFactory.DivisionAssignment(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    
                    var storage = leftOperand;
                    var value = rightOperand;

                    result.AddMessages(AddOutgoingEdges(binExp, storage, SemanticRole.Storage));
                    result.AddMessages(AddOutgoingEdges(binExp, value, SemanticRole.Value));
                }
                    break;

                default:
                    {
                        var role = GetSymbolRole(op, lexer, context, ct);

                        // [dho] `x in y` 
                        //          ^^  - 10/02/19
                        if (role == SymbolRole.KeyIn)
                        {
                            binExp = NodeFactory.MembershipTest(
                                context.AST,
                                CreateOrigin(range, lexer, context)
                            );

                            var subject = leftOperand;
                            var criteria = rightOperand;

                            result.AddMessages(AddOutgoingEdges(binExp, subject, SemanticRole.Subject));
                            result.AddMessages(AddOutgoingEdges(binExp, criteria, SemanticRole.Criteria));
                        }
                        // [dho] `x instanceof y` 
                        //          ^^^^^^^^^^  - 10/02/19
                        else if (role == SymbolRole.TypeTest)
                        {
                            binExp = NodeFactory.TypeTest(
                                context.AST,
                                CreateOrigin(range, lexer, context)
                            );

                            var subject = leftOperand;
                            var criteria = rightOperand;

                            result.AddMessages(AddOutgoingEdges(binExp, subject, SemanticRole.Subject));
                            result.AddMessages(AddOutgoingEdges(binExp, criteria, SemanticRole.Criteria));
                        }
                        else
                        {
                            result.AddMessages(
                                CreateUnsupportedTokenResult<Node>(op, lexer, context)
                            );
                        }
                    }
                    break;
            }



            result.Value = result.AddMessages(FinishNode(binExp, lexer, context, ct));

            return result;
        }

        private Result<Node> ParseArrayLiteralExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var startPos = token.StartPos;

            // [dho] `[a, b] = c` - 11/02/19

            var result = new Result<Node>();

            // [dho] `[` 
            //        ^  - 11/02/19
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                Node[] members = default(Node[]);

                // [dho] guard against the case where it's an empty array - 25/03/19
                if (!EatIfNext(SyntaxKind.CloseBracketToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var membersContext = ContextHelpers.CloneInKind(context, ContextKind.ArrayLiteralMembers);

                    // [dho] `[...` 
                    //         ^^^  - 25/03/19
                    members = result.AddMessages(
                        ParseCommaDelimitedList(ParseArgumentOrArrayLiteralElement, token, lexer, membersContext, ct)
                    );

                    // [dho] `[...]` 
                    //            ^  - 25/03/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBracketToken, lexer, context, ct)
                    );
                }

                if (!HasErrors(result))
                {
                    var range = new Range(startPos, token.StartPos + Lexeme(token, lexer).Length);

                    var array = NodeFactory.ArrayConstruction(context.AST, CreateOrigin(range, lexer, context));

                    result.AddMessages(AddOutgoingEdges(array, members, SemanticRole.Member));

                    result.Value = result.AddMessages(FinishNode(array, lexer, context, ct));
                }
            }
            else
            {
                return CreateErrorResult<Node>(token, "'[' expected", lexer, context);
            }

            return result;
        }

        private Result<Node> ParseObjectLiteralExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {

            // [dho] `{a, b} = c` - 11/02/19

            var result = new Result<Node>();

            // [dho] `{` 
            //        ^  - 11/02/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var startPos = token.StartPos;

                var membersContext = ContextHelpers.CloneInKind(context, ContextKind.ObjectLiteralMembers);

                var members = result.AddMessages(
                    ParseBracketedCommaDelimitedList(ParseObjectLiteralElement, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken, token, lexer, membersContext, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var dynamicType = NodeFactory.DynamicTypeConstruction(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(dynamicType, members, SemanticRole.Member));

                result.Value = result.AddMessages(FinishNode(dynamicType, lexer, context, ct));
            }
            else
            {
                return CreateErrorResult<Node>(token, "'{' expected", lexer, context);
            }


            return result;
        }

        private Result<Node> ParsePrimaryExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.Directive:
                    return ParseDirective(token, lexer, context, ct);

                case SyntaxKind.NumericLiteral:
                    return ParseNumericLiteral(token, lexer, context, ct);

                case SyntaxKind.StringLiteral:
                    return ParseStringLiteral(token, lexer, context, ct);

                case SyntaxKind.NoSubstitutionTemplateLiteral:{
                    var result = new Result<Node>();
                    
                    var members = result.AddMessages(
                        ParseNoSubstitutionTemplateLiteral(token, lexer, context, ct)
                    );

                    var interp = NodeFactory.InterpolatedString(
                        context.AST,
                        CreateOrigin(token, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(interp, members, SemanticRole.Member));
                    result.Value = result.AddMessages(FinishNode(interp, lexer, context, ct));;
                    
                    return result;
                }

                case SyntaxKind.OpenParenToken:
                    return ParseParenthesizedExpression(token, lexer, context, ct);

                case SyntaxKind.OpenBracketToken:
                    return ParseArrayLiteralExpression(token, lexer, context, ct);

                case SyntaxKind.OpenBraceToken:
                    return ParseObjectLiteralExpression(token, lexer, context, ct);

                case SyntaxKind.SlashToken:
                case SyntaxKind.SlashEqualsToken:
                    {
                        var result = new Result<Node>();

                        token = result.AddMessages(TokenUtils.ReScanSlashToken(token, lexer));

                        if (token.Kind == SyntaxKind.RegularExpressionLiteral)
                        {
                            // lexer.Pos = token.StartPos + Lexeme(token, lexer).Length;

                            result.Value = result.AddMessages(ParseRegularExpressionLiteral(token, lexer, context, ct));
                        }
                        else
                        {
                            result.Value = result.AddMessages(ParseIdentifier(token, lexer, context, ct));
                        }

                        return result;
                    }

                case SyntaxKind.TemplateHead:
                    {
                        var result = new Result<Node>();

                        var startPos = token.StartPos;

                        var members = result.AddMessages(
                            ParseTemplateExpressionMembers(token, lexer, context, ct)
                        );

                        var interp = NodeFactory.InterpolatedString(
                            context.AST,
                            CreateOrigin(new Range(startPos, lexer.Pos), lexer, context)
                        );

                        result.AddMessages(AddOutgoingEdges(interp, members, SemanticRole.Member));

                        result.Value = result.AddMessages(FinishNode(interp, lexer, context, ct));

                        return result;
                    }

                default:
                    {
                        var result = new Result<Node>();

                        var role = GetSymbolRole(token, lexer, context, ct);

                        switch (role)
                        {
                            case SymbolRole.IncidentContextReference:
                                return ParseIncidentContextReference(token, lexer, context, ct);

                            case SymbolRole.SuperContextReference:
                                return ParseSuperContextReference(token, lexer, context, ct);

                            case SymbolRole.Null:
                                return ParseNull(token, lexer, context, ct);

                            case SymbolRole.TrueBooleanConstant:
                                return ParseTrueBooleanConstant(token, lexer, context, ct);

                            case SymbolRole.FalseBooleanConstant:
                                return ParseFalseBooleanConstant(token, lexer, context, ct);

                            case SymbolRole.Modifier:
                                {
                                    // [dho] `await function` 
                                    //              ^^^^^^^^   - 21/02/19
                                    if (IsFunctionExpressionFollowingModifiersOnSameLine(token, lexer, context, ct))
                                    {
                                        return ParseFunctionExpression(token, lexer, context, ct);
                                    }
                                }
                                break;

                            case SymbolRole.ObjectType:
                                return ParseClassExpression(token, lexer, context, ct);

                            // case SymbolRole.ValueTypeDeclaration:
                            //     return ParseStructExpression(token, lexer, context, ct);

                            case SymbolRole.Function:
                                return ParseFunctionExpression(token, lexer, context, ct);

                            case SymbolRole.Construction:
                                return ParseNewExpression(token, lexer, context, ct);
                        }

                        // [dho] fallthrough to here - 21/02/19
                        return ParseKnownSymbolRoleAsIdentifier(token, lexer, context, ct);
                        // return ParseIdentifier(token, lexer, context, ct);

                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<T> CreateErrorResult<T>(Token token, string description, Lexer lexer, Context context)
        {
            var result = new Result<T>();

            // PrintSnippet(token, lexer);

            result.AddMessages(new Message(MessageKind.Error, description)
            {
                Hint = token != null ? GetHint(token, lexer, context) : null,
                Tags = DiagnosticTags
            });

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<T> CreateUnsupportedFeatureResult<T>(Token token, string featureName, Lexer lexer, Context context)
        {
            var result = new Result<T>();

            // PrintSnippet(token, lexer);

            result.AddMessages(new Message(MessageKind.Error, $"The use of {featureName} is not currently supported")
            {
                Hint = GetHint(token, lexer, context),
                Tags = DiagnosticTags
            });

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<T> CreateUnsupportedTokenResult<T>(Token token, Lexer lexer, Context context)
        {
            var result = new Result<T>();

            result.AddMessages(new Message(MessageKind.Error, $"'{Lexeme(token, lexer)}' has unexpected kind : '{token.Kind.ToString()}'")
            {
                Hint = GetHint(token, lexer, context),
                Tags = DiagnosticTags
            });

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<T> CreateUnsupportedSymbolRoleResult<T>(SymbolRole role, Token token, Lexer lexer, Context context)
        {
            var result = new Result<T>();

            // PrintSnippet(token, lexer);

            result.AddMessages(new Message(MessageKind.Error, $"'{Lexeme(token, lexer)}' has unsupported role : '{role.ToString()}'")
            {
                Hint = GetHint(token, lexer, context),
                Tags = DiagnosticTags
            });

            return result;
        }

        // private Result<Node> ParsePrimaryExpressionAsRole(SymbolRole role, Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     switch(role)
        //     {
        //         case SymbolRole.IncidentContextReference:
        //             return ParseIncidentContextReference(token, lexer, context, ct);

        //         case SymbolRole.SuperContextReference:
        //             return ParseSuperContextReference(token, lexer, context, ct);

        //         case SymbolRole.NullConstant: // [dho] do we want this?
        //             x;
        //             break;

        //         case SymbolRole.FalseBooleanConstant:
        //             return ParseFalseBooleanConstant(token, lexer, context, ct);

        //         case SymbolRole.TrueBooleanConstant:
        //             return ParseTrueBooleanConstant(token, lexer, context, ct);

        //         case SymbolRole.ObjectTypeDeclaration:
        //             return ParseClassLike(token, lexer, context, ct);

        //         case SymbolRole.FunctionLike:
        //             return ParseFunctionLike(token, lexer, context, ct);

        //         case SymbolRole.Construction:
        //             return ParseNewExpression(token, lexer, context, ct);

        //         case SymbolRole.Identifier:
        //             return ParseIdentifier(token, lexer, context, ct);

        //         default:
        //             return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
        //     }
        // }
        public Result<Node> ParseIncidentContextReference(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var n = NodeFactory.IncidentContextReference(context.AST, CreateOrigin(token, lexer, context));

            result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseSuperContextReference(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var n = NodeFactory.SuperContextReference(context.AST, CreateOrigin(token, lexer, context));

            result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseNull(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var n = NodeFactory.Null(context.AST, CreateOrigin(token, lexer, context));

            result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

            return result;
        }


        public bool IsFunctionExpressionFollowingModifiersOnSameLine(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            var role = GetSymbolRole(token, lookAhead, context, ct);

            while (role == SymbolRole.Modifier)
            {
                if (token.PrecedingLineBreak)
                {
                    return false;
                }

                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;

                role = GetSymbolRole(token, lookAhead, context, ct);
            }

            return role == SymbolRole.Function && !token.PrecedingLineBreak;
        }

        public Result<Node> ParseNewExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.Construction)
            {
                if (EatIfNext(IsDot, lexer, context, ct))
                {
                    var result = new Result<Node>();

                    var startPos = token.StartPos;

                    // [dho] `new.target` 
                    //            ^^^^^^   - 04/03/19
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    if (HasErrors(result)) return result;

                    Node name = result.AddMessages(
                        ParseKnownSymbolRoleAsIdentifier(token, lexer, context, ct)
                    );

                    if (HasErrors(result)) return result;

                    var range = new Range(startPos, lexer.Pos);

                    var metaProperty = NodeFactory.MetaProperty(context.AST, CreateOrigin(range, lexer, context));

                    result.AddMessages(AddOutgoingEdges(metaProperty, name, SemanticRole.Name));

                    result.Value = result.AddMessages(FinishNode(metaProperty, lexer, context, ct));

                    return result;
                }
                else
                {
                    var result = new Result<Node>();

                    var startPos = token.StartPos;

                    // [dho] `new ...` 
                    //            ^^^   - 04/03/19
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    if (HasErrors(result)) return result;

                    var exp = result.AddMessages(
                        ParseMemberExpressionOrHigher(token, lexer, context, ct)
                    );

                    if (HasErrors(result)) return result;

                    var template = default(Node[]);

                    // [dho] `new Foo<...` 
                    //               ^     - 04/03/19
                    if (LookAhead(IsLessThan, lexer, context, ct).Item1)
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        template = result.AddMessages(
                            // [dho] `new Foo(...` 
                            //               ^       - 04/03/19
                            ParseTypeArgumentsInExpression(token, lexer, context, ct)
                        );

                        if (HasErrors(result)) return result;
                    }


                    var arguments = default(Node[]);

                    if (LookAhead(IsOpenParen, lexer, context, ct).Item1)
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        if (HasErrors(result)) return result;

                        arguments = result.AddMessages(
                            // [dho] `new Foo(...` 
                            //               ^       - 04/03/19
                            ParseArguments(token, lexer, context, ct)
                        );

                        if (HasErrors(result)) return result;
                    }



                    var range = new Range(startPos, lexer.Pos);

                    var construction = NodeFactory.NamedTypeConstruction(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(construction, exp, SemanticRole.Name));
                    result.AddMessages(AddOutgoingEdges(construction, template, SemanticRole.Template));
                    result.AddMessages(AddOutgoingEdges(construction, arguments, SemanticRole.Argument));

                    result.Value = result.AddMessages(FinishNode(construction, lexer, context, ct));

                    return result;
                }
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        public Result<Node> ParseTrueBooleanConstant(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.TrueBooleanConstant)
            {
                var result = new Result<Node>();

                var n = NodeFactory.BooleanConstant(context.AST, CreateOrigin(token, lexer, context), true);

                result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseFalseBooleanConstant(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.FalseBooleanConstant)
            {
                var result = new Result<Node>();

                var n = NodeFactory.BooleanConstant(context.AST, CreateOrigin(token, lexer, context), false);

                result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        #region extensibility
        public enum SymbolRole
        {
            None,
            Accessor,
            Breakpoint,
            GeneratorValue, // yield
            TrueBooleanConstant,
            FalseBooleanConstant,
            ForMembersLoop,
            ForKeysLoop,
            ForPredicateLoop,
            CompilerHint,
            Constant,
            Construction,
            Constructor,
            // DataValue,
            Default,
            Destruction,
            Enumeration,
            ErrorHandlerClause,
            ErrorFinallyClause,
            ErrorTrapJunction,
            EvalToVoid,
            Export,
            Function,
            FunctionTermination,
            GlobalContextReference,
            Identifier,
            Import,
            IncidentContextReference,
            Inheritance,
            Conformity,
            Interface,
            InterimSuspension,
            JumpToNextIteration,
            LoopBreak,
            ClauseBreak,
            MatchClause,
            MatchJunction,
            Modifier,
            KeyIn,
            // Method,
            Mutator,
            Namespace,
            Null,
            PackageReference,
            DoWhilePredicateLoop,
            PrioritySymbolResolutionContext,
            WhilePredicateLoop,
            PredicateJunction,
            PredicateJunctionFalseBranch,
            MemberOf,
            IndexTypeQuery,
            InferredTypeQuery,
            RaiseError,
            ObjectType,
            ReferenceAlias,
            SuperContextReference,
            TypeAlias,
            // [dho] this used to be `TypeInterrogation` but that's only for expressions, it's a `TypeQuery` in types - 29/03/19
            TypeOf,
            SmartCast,
            TypeTest,
            Variable
            // ValueTypeDeclaration
        }

        // [dho] NOTE this function can be overridden to provide a different lexicon
        // within the same syntactic structures - 02/02/19
        public SymbolRole GetSymbolRole(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.Identifier)
            {
                switch (token.Lexeme)
                {
                    case "abstract":
                    case "async":
                    case "private":
                    case "protected":
                    case "public":
                    case "readonly":
                    case "ref": // [dho] TODO CLEANUP ?? added this for now for expressing parameters by reference - 04/08/19
                    case "static":
                        return SymbolRole.Modifier;

                    // case "any":
                    //     return SymbolRole.AnyTypeReference;

                    case "as":
                        return SymbolRole.ReferenceAlias;

                    case "await":
                        return SymbolRole.InterimSuspension;

                    // case "boolean":
                    //     return SymbolRole.BooleanTypeReference;

                    case "case":
                        return SymbolRole.MatchClause;

                    case "catch":
                        return SymbolRole.ErrorHandlerClause;

                    case "class":
                        return SymbolRole.ObjectType;

                    case ConstLexeme:
                        return SymbolRole.Constant;

                    case "constructor":
                        return SymbolRole.Constructor;

                    case "continue":
                        return SymbolRole.JumpToNextIteration;

                    case "debugger":
                        return SymbolRole.Breakpoint;

                    case "declare":
                        return SymbolRole.CompilerHint;

                    case "default":
                        return SymbolRole.Default;

                    // case "delegate":
                    //     return SymbolRole.Delegate; 

                    case "delete":
                        return SymbolRole.Destruction;

                    case "do":
                        return SymbolRole.DoWhilePredicateLoop;

                    case "else":
                        return SymbolRole.PredicateJunctionFalseBranch;

                    case "enum":
                        return SymbolRole.Enumeration;

                    // case "event":
                    //     return SymbolRole.Event;

                    case "export":
                        return SymbolRole.Export;

                    case "extends":
                        return SymbolRole.Inheritance;

                    // case "extension":
                    //     return SymbolRole.Augmentation;

                    case "false":
                        return SymbolRole.FalseBooleanConstant;

                    case "finally":
                        return SymbolRole.ErrorFinallyClause;

                    case "for":
                        // [dho] CLEANUP HACK? In TS the "for" keyword could be a members, keys or predicate loop.
                        // Whilst it's not ideal to just return `ForPredicateLoop`, the alternative would be expensive
                        // considering we would need to partially parse the expression, then throw that result away and reparse
                        // it again properly straight after. In the current implementation we resolve the ambiguity in `ParseForLoop`.
                        // 
                        // NOTE if we just had `SymbolRole.ForLoop` it would mean other languages that do not use `for` for all these
                        // loop types would be stuck, eg. C# that has `for` and `foreach` - 26/02/19
                        return SymbolRole.ForPredicateLoop;

                    case "from":
                        // [dho] this is a fix for an issue where `from` is used as an identifier
                        // for an argument like `console.log(from)` - 30/03/19
                        if (context.CurrentKind == ContextKind.SourceElements)
                        {
                            return SymbolRole.PackageReference;
                        }
                        else
                        {
                            return SymbolRole.Identifier;
                        }

                    case "function":
                        return SymbolRole.Function;

                    case "get":
                        if (context.CurrentKind == ContextKind.ClassMembers ||
                            context.CurrentKind == ContextKind.ObjectLiteralMembers ||
                            context.CurrentKind == ContextKind.TypeMembers)
                        {
                            return SymbolRole.Accessor;
                        }
                        else
                        {
                            return SymbolRole.Identifier;
                        }

                    case "set":
                        if (context.CurrentKind == ContextKind.ClassMembers ||
                            context.CurrentKind == ContextKind.ObjectLiteralMembers ||
                            context.CurrentKind == ContextKind.TypeMembers)
                        {
                            return SymbolRole.Mutator;
                        }
                        else
                        {
                            return SymbolRole.Identifier;
                        }


                    case "global":
                        return SymbolRole.GlobalContextReference;

                    case "if":
                        return SymbolRole.PredicateJunction;

                    case "implements":
                        return SymbolRole.Conformity;

                    case "import":
                        return SymbolRole.Import;

                    case "in":
                        return SymbolRole.KeyIn;

                    case "infer":
                        return SymbolRole.InferredTypeQuery;

                    case "instanceof":
                        return SymbolRole.TypeTest;

                    case "interface":
                        return SymbolRole.Interface;

                    case "is": // [dho] eg `foo(...) : Bar is Charlie` - 24/02/19
                        return SymbolRole.SmartCast;

                    case "keyof":
                        return SymbolRole.IndexTypeQuery;

                    case LetLexeme:
                    case VarLexeme:
                        return SymbolRole.Variable;

                    case "module":
                    case "namespace":
                        return SymbolRole.Namespace;

                    // case "never":
                    //     return SymbolRole.NeverTypeReference;

                    case "new":
                        return SymbolRole.Construction;

                    case "null":
                        return SymbolRole.Null;

                    // case "number":
                    //     return SymbolRole.DoublePrecisionNumberTypeReference;

                    case "of":
                        return SymbolRole.MemberOf;

                    case "return":
                        return SymbolRole.FunctionTermination;

                    case "type":
                        return SymbolRole.TypeAlias;

                    // case "string":
                    //     return SymbolRole.StringTypeReference;

                    // case "struct":
                    //     return SymbolRole.ValueTypeDeclaration;

                    case "super":
                        return SymbolRole.SuperContextReference;

                    case "switch":
                        return SymbolRole.MatchJunction;

                    case "this":
                        return SymbolRole.IncidentContextReference;

                    case "throw":
                        return SymbolRole.RaiseError;

                    case "true":
                        return SymbolRole.TrueBooleanConstant;

                    case "try":
                        return SymbolRole.ErrorTrapJunction;

                    case "typeof":
                        return SymbolRole.TypeOf;


                    case "void":
                        {
                            switch (context.CurrentKind)
                            {
                                // [dho] when we are using 'void' as a type name
                                // treat it just as an identifier - 04/02/19
                                case ContextKind.ClassMembers:
                                case ContextKind.EnumMembers:
                                case ContextKind.TupleElementTypes:
                                case ContextKind.TypeArguments:
                                case ContextKind.TypeMembers:
                                case ContextKind.TypeParameters:
                                    return SymbolRole.Identifier;

                                // [dho] otherwise treat it as a void expression - 04/02/19
                                default:
                                    return SymbolRole.EvalToVoid;
                            }
                        }

                    case "while":
                        return SymbolRole.WhilePredicateLoop;

                    case "with":
                        return SymbolRole.PrioritySymbolResolutionContext;

                    case "break":
                        return context.CurrentKind == ContextKind.SwitchClauseStatements ?
                                SymbolRole.ClauseBreak : SymbolRole.LoopBreak;

                    case "yield":
                        return SymbolRole.GeneratorValue;

                    // case "object":
                    // case "package":
                    // case "require":
                    // case "symbol":
                    // case "undefined":

                    default:
                        return SymbolRole.Identifier;
                }
            }
            else
            {
                return SymbolRole.None;
            }

        }

        // public enum ModifierRole
        // {
        //     None,
        //     Abstract,
        //     Static
        // }

        #endregion

        // ///<summary>
        // /// Eats decorators and modifiers in order to find the identifier
        // // role they are affixed to - 04/02/19
        // ///</summary> 
        // Result<SymbolRole> LookAheadToSymbolRole(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<SymbolRole>();

        //     var lookAhead = lexer.Clone();

        //     // [dho] skip any decorators - 04/02/19
        //     if(token.Kind == SyntaxKind.AtToken)
        //     {
        //         if(EatDecorators(token, lookAhead, context, ct))
        //         {
        //             token = result.AddMessages(NextToken(lookAhead, context, ct));
        //         }
        //         else
        //         {
        //             result.AddMessages(
        //                 CreateErrorResult<Node>(token, "Expected to have parsed decorators", lexer, context)
        //             );

        //             return result;
        //         }
        //     }

        //     if(token.Kind == SyntaxKind.AsteriskToken)
        //     {
        //         token = result.AddMessages(NextToken(lookAhead, context, ct));

        //         // [dho] `*foo` 
        //         //        ^      - 11/02/19
        //         if(GetSymbolRole(token, lookAhead, context, ct) == SymbolRole.Identifier)
        //         {
        //             result.Value = SymbolRole.Method; // or do we say GeneratorMethod?
        //         }
        //         else
        //         {
        //             result.Value = SymbolRole.None;
        //         }
        //     }
        //     else
        //     {
        //         var role = GetSymbolRole(token, lookAhead, context, ct);

        //         // [dho] skip any modifiers - 04/02/19
        //         if(role == SymbolRole.Modifier)
        //         {
        //             if(EatModifiers(token, lookAhead, context, ct))
        //             {
        //                 token = result.AddMessages(NextToken(lookAhead, context, ct));
        //             }
        //             else
        //             {
        //                 result.AddMessages(
        //                     CreateErrorResult<Node>(token, "Expected to have parsed modifiers", lexer, context)
        //                 );

        //                 return result;
        //             }
        //         }

        //         result.Value = GetSymbolRole(token, lookAhead, context, ct);

        //         return result;
        //     }
        // }


        // Result<Node> ParseDecorator(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<Node>();

        //     var role = result.AddMessages(
        //         LookAheadToSymbolRole(token, lexer, context, ct)
        //     );

        //     result.Value = result.AddMessages(
        //         ParseAsSymbolRole(role, token, lexer, context, ct)
        //     );

        //     return result;
        // }

        // Result<Node> ParseIdentifierByRole(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<Node>();

        //     var role = GetSymbolRole(token, lexer, context, ct);

        //     // [dho] if we encounter any modifiers up front then we will
        //     // skip over them to uncover the type of construct beyond them 
        //     // that they apply to - 03/02/19
        //     if(role == SymbolRole.Modifier)
        //     {
        //         var lookAhead = lexer.Clone();

        //         if(EatModifiers(token, lookAhead, context, ct))
        //         {
        //             var t = result.AddMessages(NextToken(lookAhead, context, ct));

        //             // [dho] store the role of the token immediately 
        //             // after the modifiers have been skipped - 03/02/19
        //             role = GetSymbolRole(t, lookAhead, context, ct);
        //         }
        //         else
        //         {
        //             x; // thats a problem because we expected to have skipped modifiers, return error
        //         }
        //     }

        //     // [dho] at this point, `role` either tells us the role of the 
        //     // original token we were passed OR the token that sits after any 
        //     // modifiers we encountered. NOTE, in the latter case we are still 
        //     // passing through the untouched original token, meaning that if the receiving
        //     // parse delegate does not expect modifiers it will return errors as expected,
        //     // and if it does support modifiers it will parse them out - 03/02/19
        //     return ParseAsSymbolRole(role, token, lexer, context, ct);
        // }

        // private Result<Node> ParseAsSymbolRole(SymbolRole role, Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     // [dho] there are probably loads of cases we need to add below here to be complete.. - 14/02/19
        //     xxx;
        //     switch(role)
        //     {
        //         /*
        //                                 case SyntaxKind.VarKeyword:
        //                 return ParseVariableStatement();

        //             case SyntaxKind.LetKeyword:
        //                 if (IsLetDeclaration())
        //                 {

        //                     return ParseVariableStatement();
        //                 }

        //                 break;


        //             case SyntaxKind.WithKeyword:
        //                 return ParseWithStatement();

        //             case SyntaxKind.TryKeyword:
        //             case SyntaxKind.CatchKeyword:
        //             case SyntaxKind.FinallyKeyword:
        //                 return ParseTryStatement();

        //             case SyntaxKind.DebuggerKeyword:
        //                 return ParseDebuggerStatement();
        //                 */

        //         case SymbolRole.BoundedLoop: // [dho] for limit, for members, for properties - 03/02/19
        //             return ParseBoundedLoop(token, lexer, context, ct);

        //         case SymbolRole.ClauseTermination:
        //             return ParseClauseTermination(token, lexer, context, ct);

        //         case SymbolRole.ConstructorLike:
        //             return ParseConstructorLike(token, lexer, context, ct);

        //         case SymbolRole.Destruction:
        //             return ParseDeleteExpression(token, lexer, context, ct);

        //         case SymbolRole.EnumerationTypeDeclaration:
        //             return ParseEnumDeclaration(token, lexer, context, ct);

        //         case SymbolRole.FunctionLike:
        //             return ParseFunctionLike(token, lexer, context, ct);

        //         case SymbolRole.FunctionTermination:
        //             return ParseFunctionTermination(token, lexer, context, ct);

        //         case SymbolRole.Identifier:
        //             return ParseIdentifier(token, lexer, context, ct);

        //         case SymbolRole.IncidentContextReference:
        //             return ParseIncidentContextReference(token, lexer, context, ct);

        //         case SymbolRole.InterfaceDeclaration:
        //             return ParseInterfaceDeclaration(token, lexer, context, ct);

        //         case SymbolRole.JumpToNextIteration:
        //             return ParseJumpToNextIteration(token, lexer, context, ct);

        //         case SymbolRole.LoopTermination:
        //             return ParseLoopTermination(token, lexer, context, ct);

        //         case SymbolRole.MatchClause:
        //             return ParseMatchClause(token, lexer, context,ct);

        //         case SymbolRole.MatchJunction:
        //             return ParseMatchJunction(token, lexer, context, ct);

        //         case SymbolRole.NamespaceDeclaration:
        //             return ParseNamespaceDeclaration(token, lexer, context, ct);

        //         case SymbolRole.PostPredicateLoop:
        //             return ParsePostPredicateLoop(token, lexer, context, ct);

        //         case SymbolRole.PrePredicateLoop:
        //             return ParsePrePredicateLoop(token, lexer, context, ct);

        //         case SymbolRole.PredicateJunction:
        //             return ParsePredicateJunction(token, lexer, context, ct);

        //         case SymbolRole.RaiseError:
        //             return ParseRaiseError(token, lexer, context, ct);

        //         case SymbolRole.ObjectTypeDeclaration:
        //             return ParseClassDeclaration(token, lexer, context, ct);

        //         case SymbolRole.SafeCast:
        //             return ParseSafeCast(token, lexer, context, ct);

        //         case SymbolRole.TypeAliasDeclaration:
        //             return ParseTypeAlias(token, lexer, context, ct);

        //         case SymbolRole.TypeQuery:
        //             return ParseTypeOfExpression(token, lexer, context, ct);

        //         case SymbolRole.TypeTest:
        //             return ParseTypeTest(token, lexer, context, ct);

        //         case SymbolRole.ValueTypeDeclaration:
        //             return ParseValueTypeDeclaration(token, lexer, context, ct);


        //         // [dho] this case handles the cases where either we were passed
        //         // a non identifier originally, or the token sat after modifiers
        //         // is not an identifier - 03/02/19
        //         case SymbolRole.None:
        //         // [dho] Modifier case should be impossible but future proofing in case of 
        //         // regressions from refactoring - 03/02/19
        //         case SymbolRole.Modifier:
        //         default:
        //             return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
        //     }
        // }

        public Result<Node> ParseCaseClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `case` 
            //        ^^^^    - 23/02/19
            if (role == SymbolRole.MatchClause)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                var expContext = ContextHelpers.Clone(context);
                expContext.Flags &= ~ContextFlags.DisallowInContext;

                // [dho] `case ...` 
                //             ^^^    - 23/02/19
                var expression = result.AddMessages(
                    ParseExpression(token, lexer, expContext, ct)
                );

                if (HasErrors(result)) return result;

                result.Value = result.AddMessages(ParseSwitchClauseRest(startPos, expression, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseDefaultMatchClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `default` 
            //        ^^^^^^^    - 23/02/19
            if (role == SymbolRole.Default)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                // [dho] expression not set because this is the default case - 30/03/19
                Node expression = default(Node);

                result.Value = result.AddMessages(ParseSwitchClauseRest(startPos, expression, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        private Result<Node> ParseSwitchClauseRest(int startPos, Node expression, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] `case ...:` 
            //                ^   - 23/02/19
            // [dho] `default:` 
            //                ^   - 23/02/19
            if (result.AddMessages(EatIfNextOrError(SyntaxKind.ColonToken, lexer, context, ct)))
            {
                var body = default(Node[]);

                // [dho] guard against eating too many tokens when
                // cases are stacked on top of each other without a body
                // between them:
                //
                // ```
                // case Foo:
                // case Bar:
                // default:
                // ```
                // - 29/03/19
                if (!LookAhead(IsMatchClauseOrDefault, lexer, context, ct).Item1)
                {
                    var token = result.AddMessages(NextToken(lexer, context, ct));

                    var statementContext = ContextHelpers.CloneInKind(context, ContextKind.SwitchClauseStatements);

                    // [dho] `case ...: ...` 
                    //                  ^^^   - 23/02/19
                    // [dho] `default: ...` 
                    //                 ^^^   - 30/03/19
                    body = result.AddMessages(
                        ParseList(ParseStatement, token, lexer, statementContext, ct)
                    );
                }

                var range = new Range(startPos, lexer.Pos);

                var clause = NodeFactory.MatchClause(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(clause, expression, SemanticRole.Pattern));
                result.AddMessages(AddOutgoingEdges(clause, body, SemanticRole.Body));

                result.Value = result.AddMessages(FinishNode(clause, lexer, context, ct));
            }

            return result;
        }

        public Result<Node> ParseTypeOfExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            Result<Node> result = new Result<Node>();

            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.TypeOf)
            {
                var startPos = token.StartPos;

                token = result.AddMessages(
                    NextToken(lexer, context, ct)
                );

                Node subject = result.AddMessages(
                    ParseSimpleUnaryExpression(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var typeInterrogation = NodeFactory.TypeInterrogation(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(typeInterrogation, subject, SemanticRole.Subject));

                result.Value = result.AddMessages(FinishNode(typeInterrogation, lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );
            }

            return result;
        }

        public Result<Node> ParseVoidExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.EvalToVoid)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                var exp = result.AddMessages(
                    ParseSimpleUnaryExpression(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var evalToVoid = NodeFactory.EvalToVoid(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(evalToVoid, exp, SemanticRole.Operand));

                result.Value = result.AddMessages(FinishNode(evalToVoid, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseAwaitExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.InterimSuspension)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                var future = result.AddMessages(
                    ParseSimpleUnaryExpression(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var n = NodeFactory.InterimSuspension(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(n, future, SemanticRole.Operand));

                result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseUpdateExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var unary = default(ASTNode);
            Node operand = default(Node);

            // [dho] prefix - 14/02/19
            if (token.Kind == SyntaxKind.PlusPlusToken || token.Kind == SyntaxKind.MinusMinusToken)
            {
                var prefix = token;

                token = result.AddMessages(NextToken(lexer, context, ct));

                operand = result.AddMessages(
                    ParseLeftHandSideExpressionOrHigher(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                // [dho] `++x` 
                //        ^^    - 14/02/19
                if (prefix.Kind == SyntaxKind.PlusPlusToken)
                {
                    unary = NodeFactory.PreIncrement(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                }
                // [dho] `--x` 
                //        ^^    - 14/02/19
                else
                {
                    unary = NodeFactory.PreDecrement(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                }
            }
            else if (token.Kind == SyntaxKind.LessThanToken && LookAhead(IsKnownSymbolRole, lexer, context, ct).Item1)
            {
                return ParseJsxElementOrSelfClosingElement(true /* in expression */, token, lexer, context, ct);
            }
            else // [dho] postfix - 14/02/19
            {
                operand = result.AddMessages(
                    ParseLeftHandSideExpressionOrHigher(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                // [dho] `x++` 
                //         ^^    - 14/02/19
                if (EatIfNext(SyntaxKind.PlusPlusToken, lexer, context, ct))
                {
                    unary = NodeFactory.PostIncrement(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                }
                // [dho] `x--` 
                //         ^^    - 14/02/19
                else if (EatIfNext(SyntaxKind.MinusMinusToken, lexer, context, ct))
                {
                    unary = NodeFactory.PostDecrement(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                }
                else
                {
                    // [dho] added this for the case where it is neither 
                    // a prefix nor postfix update expression - 17/03/19
                    result.Value = operand;
                    return result;
                }
            }

            result.Value = result.AddMessages(FinishNode(unary, lexer, context, ct));

            return result;
        }

#region jsx
        struct JSXComponents
        {
            public Node Name;
            public Node[] Properties;

            public bool SelfClosing;


            public bool HasEatenTokens;

            public void Deconstruct(out Node name, out Node[] properties, out bool selfClosing, out bool hasEatenTokens)
            {
                name = Name;
                properties = Properties;
                selfClosing = SelfClosing;

                hasEatenTokens = HasEatenTokens;
            }
        }


        private Result<Node> ParseJsxElementOrSelfClosingElement(bool inExpression, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startToken = token;

            var (name, properties, selfClosing, hasEatenTokens) = result.AddMessages(
                ParseJsxOpeningOrSelfClosingElement(token, lexer, context, ct)
            );

            if(HasErrors(result) || ct.IsCancellationRequested) return result;

            var children = new List<Node>();

            // [dho] `<Foo>` - 10/06/19
            if(!selfClosing)
            {
                if(hasEatenTokens) // [dho] should always be true.. but just in case we synthesized something!! - 12/06/19
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    if(HasErrors(result) || ct.IsCancellationRequested) return result;
                }


                {
                    // var lookAhead = lexer.Clone();

                    var childContext = ContextHelpers.CloneInKind(context, ContextKind.JsxChildren);
                    // System.Console.WriteLine($"STARTING LOOP is {token.Kind} with lexeme {Lexeme(token, lexer)}\n{lexer.SourceText.Substring(token.StartPos, 50)}");
                    while(true)
                    {
                        token = result.AddMessages(TokenUtils.ReScanJSXToken(token, lexer));

                        // System.Console.WriteLine($"The token is {token.Kind} with lexeme {Lexeme(token, lexer)}\n{lexer.SourceText.Substring(token.StartPos, 50)}");
                        
                        if(HasErrors(result) || ct.IsCancellationRequested) return result;

                        if(token.Kind == SyntaxKind.LessThanSlashToken)
                        {
                            var closingJSX = result.AddMessages(ParseJsxClosingElement(inExpression, token, lexer, context, ct));

                            // [dho] check that if we parsed the closing JSX tag ok, that it actually matches the expected
                            // name from the corresponding opening JSX tag - 12/06/19
                            if(closingJSX.Name == null || !TagNamesAreEquivalent(name, closingJSX.Name, context))
                            {
                                result.AddMessages(new Message(MessageKind.Error, "No closing tag found for JSX element")
                                {
                                    Hint = GetHint(startToken, lexer, context),
                                    Tags = DiagnosticTags
                                });

                                return result;
                            }

                            // [dho] closing tag done - 13/06/19
                            break;
                        }
                        else if(token.Kind == SyntaxKind.EndOfFileToken)
                        {
                            // If we hit EOF, issue the error at the tag that lacks the closing element
                            // rather than at the end of the file (which is useless)
                            result.AddMessages(new Message(MessageKind.Error, $"No closing tag found for JSX element")
                            {
                                Hint = GetHint(startToken, lexer, context),
                                Tags = DiagnosticTags
                            });

                            return result;
                        }
                        else if(token.Kind == SyntaxKind.ConflictMarkerTrivia)
                        {
                            break;
                        }
                        else if(token.Kind == SyntaxKind.JsxText)
                        {            
                            children.Add(result.AddMessages(
                                ParseJsxText(token, lexer, childContext, ct)
                            ));

                            if(HasErrors(result) || ct.IsCancellationRequested) return result;

                            token = result.AddMessages(NextToken(lexer, context, ct));
                        }
                        else if(token.Kind == SyntaxKind.JsxText || token.Kind == SyntaxKind.OpenBraceToken)
                        {
                            children.Add(result.AddMessages(
                                ParseJsxExpression(false /* in expression */, token, lexer, childContext, ct)
                            ));

                            if(HasErrors(result) || ct.IsCancellationRequested) return result;

                            token = result.AddMessages(NextToken(lexer, context, ct));
                        }
                        else if(token.Kind == SyntaxKind.LessThanToken)
                        {
                            children.Add(result.AddMessages(
                                ParseJsxElementOrSelfClosingElement(false /* in expression */, token, lexer, childContext, ct)
                            ));

                            if(HasErrors(result) || ct.IsCancellationRequested) return result;

                            token = result.AddMessages(NextToken(lexer, context, ct));
                        }
                        else
                        {
                            result.AddMessages(new Message(MessageKind.Error, $"Unexpected JSX child token '{token.Kind}'")
                            {
                                Hint = GetHint(token, lexer, context),
                                Tags = DiagnosticTags
                            });

                            return result;
                        }

                        // System.Console.WriteLine($"WE'RE GOING ROUND AGAIN WITH {token.Kind} with lexeme {Lexeme(token, lexer)}\n{lexer.SourceText.Substring(token.StartPos, 50)}");
                    }
                }


                // token = result.AddMessages(NextToken(lexer, context, ct));

                if(HasErrors(result) || ct.IsCancellationRequested) return result;

                // var closingJSX = result.AddMessages(ParseJsxClosingElement(inExpression, token, lexer, context, ct));

                // // [dho] check that if we parsed the closing JSX tag ok, that it actually matches the expected
                // // name from the corresponding opening JSX tag - 12/06/19
                // if(closingJSX.Name == null || !TagNamesAreEquivalent(name, closingJSX.Name, context))
                // {
                //     result.AddMessages(new Message(MessageKind.Error, "No closing tag found for JSX element")
                //     {
                //         Hint = GetHint(startToken, lexer, context),
                //         Tags = DiagnosticTags
                //     });

                //     return result;
                // }
            }


            // [dho] this next block checks for a sibling JSX element, which would be a violation
            // if we are inside an expression context - 12/06/19
            if(inExpression)
            {
                var lookAhead = lexer.Clone();

                var r = NextToken(lookAhead, context, ct);

                if(!HasErrors(r) && r.Value.Kind == SyntaxKind.LessThanToken)
                {
                    var siblingJSX = r.AddMessages(
                        ParseJsxElementOrSelfClosingElement(true /* in expression */, r.Value, lookAhead, context, ct)
                    );

                    if(!HasErrors(r))
                    {
                        result.AddMessages(new Sempiler.AST.Diagnostics.NodeMessage(MessageKind.Error, "JSX expressions must have one parent element", siblingJSX)
                        {
                            Hint = Sempiler.AST.Diagnostics.DiagnosticsHelpers.GetHint(siblingJSX.Origin),
                            Tags = DiagnosticTags
                        });

                        lexer.Pos = lookAhead.Pos;

                        return result;
                    }
                }
            }



            {
                var range = new Range(startToken.StartPos, lexer.Pos);

                var origin = CreateOrigin(range, lexer, context);

                var viewConstruction = NodeFactory.ViewConstruction(context.AST, origin);

                result.AddMessages(AddOutgoingEdges(viewConstruction, name, SemanticRole.Name));

                if(properties?.Length > 0)
                {
                    result.AddMessages(AddOutgoingEdges(viewConstruction, properties, SemanticRole.Property));
                }

                if(children.Count > 0)
                {
                    result.AddMessages(AddOutgoingEdges(viewConstruction, children, SemanticRole.Child));
                }

                result.Value = result.AddMessages(FinishNode(viewConstruction, lexer, context, ct));
            }



            return result; 
        }
         
        private Result<JSXComponents> ParseJsxOpeningOrSelfClosingElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<JSXComponents>();

            var name = default(Node);
            var properties = default(Node[]);
            var selfClosing = default(bool);
            var hasEatenTokens = default(bool);

            if (token.Kind == SyntaxKind.LessThanToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                hasEatenTokens = true;

                {
                    name = result.AddMessages(ParseJsxElementName(token, lexer, context, ct));

                    token = result.AddMessages(NextToken(lexer, context, ct));
                }


                {
                    properties = result.AddMessages(ParseJsxAttributes(token, lexer, context, ct));

                    if(properties?.Length > 0)
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));
                    }
                }

                if (token.Kind != SyntaxKind.GreaterThanToken)
                {
                    selfClosing = true;

                    lexer.Pos = token.StartPos;

                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.SlashToken, lexer, context, ct)
                    );

                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.GreaterThanToken, lexer, context, ct)
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'<' expected", lexer, context)
                );
            }

            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 12/06/19
            result.Value = new JSXComponents
            {
                Name = name,
                Properties = properties,
                SelfClosing = selfClosing,
                HasEatenTokens = hasEatenTokens
            };

            return result;
        }

        private Result<JSXComponents> ParseJsxClosingElement(bool inExpression, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<JSXComponents>();

            var name = default(Node);
            var properties = default(Node[]);
            var selfClosing = default(bool);
            var hasEatenTokens = default(bool);

            // token = result.AddMessages(lexer.ScanJSXToken());

            if (token.Kind == SyntaxKind.LessThanSlashToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                hasEatenTokens = true;

                {
                    name = result.AddMessages(ParseJsxElementName(token, lexer, context, ct));

                    // token = result.AddMessages(NextToken(lexer, context, ct));
                }

                result.AddMessages(
                    EatIfNextOrError(SyntaxKind.GreaterThanToken, lexer, context, ct)
                );
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'</' expected", lexer, context)
                );
            }

            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 12/06/19
            result.Value = new JSXComponents
            {
                Name = name,
                Properties = properties,
                SelfClosing = selfClosing,
                HasEatenTokens = hasEatenTokens
            };

            return result;
        }

        private Result<Node> ParseJsxElementName(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] decided to stray from original source material and do this for now - 12/06/19
            return ParseEntityName(token, lexer, context, ct);
        }

        private Result<Node[]> ParseJsxAttributes(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var memberContext = ContextHelpers.CloneInKind(context, ContextKind.JsxAttributes);

            return ParseList(ParseJsxAttribute, token, lexer, memberContext, ct);
        }

        private Result<Node> ParseJsxAttribute(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] `{...x}` - 12/06/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                if(HasErrors(result) || ct.IsCancellationRequested) return result;

                result.Value = result.AddMessages(ParseSpreadElement(token, lexer, context, ct));

                result.AddMessages(
                    EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                );
            }
            else
            {
                // [dho] rescan token as JSX Identifier - 13/06/19
                lexer.Pos = token.StartPos; 

                token = result.AddMessages(lexer.ScanJSXIdentifier());

                var startPos = token.StartPos;

                var name = result.AddMessages(
                    FinishNode(
                        NodeFactory.Identifier(context.AST, CreateOrigin(token, lexer, context), Lexeme(token, lexer)),
                        lexer, context, ct
                    )
                );

                // [dho] attribute has a value `x=...` - 12/06/19
                if(EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
                {
                    var valueToken = result.AddMessages(lexer.ScanJSXAttributeValue());

                    if(HasErrors(result))
                    {
                        return result;
                    }

                    var value = default(Node);

                    if(valueToken.Kind == SyntaxKind.StringLiteral)
                    {
                        value = result.AddMessages(ParseStringLiteral(valueToken, lexer, context, ct));
                    }
                    else
                    {
                        value = result.AddMessages(ParseJsxExpression(true /* in expression */, valueToken, lexer, context, ct));
                    }


                    {
                        var range = new Range(startPos, lexer.Pos);

                        var kv = NodeFactory.KeyValuePair(context.AST,  CreateOrigin(range, lexer, context));

                        result.AddMessages(AddOutgoingEdges(kv, name, SemanticRole.Key));
                        result.AddMessages(AddOutgoingEdges(kv, value, SemanticRole.Value));

                        result.Value = result.AddMessages(FinishNode(kv, lexer, context, ct));
                    }
                }
                else
                {
                    result.Value = name;
                }
            }
            // System.Console.WriteLine("POS IS " + lexer.Pos);
            return result;
        }

        private Result<Node> ParseJsxText(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            token = result.AddMessages(lexer.ScanJSXToken());

            var startPos = token.StartPos;

            var lexeme = Lexeme(token, lexer);

            // var m = result.AddMessages(ParseJsxTextOrJsxExpressionSequenceRest(SyntaxKind.JsxExpression, token, lexer, context, ct));

            // if(HasErrors(result) || ct.IsCancellationRequested) return result;

            // // [dho] if we found a sequence that warrants treated the nodes as an interpolated string
            // // then we do that - 12/06/19
            // if(m.Count > 0)
            // {
            //     var interp = NodeFactory.InterpolatedString(
            //         context.AST,
            //         CreateOrigin(new Range(startPos, lexer.Pos), lexer, context)
            //     );

            //     var members = new Node[m.Count + 1];

            //     members[0] = result.AddMessages(
            //         FinishNode(
            //             NodeFactory.InterpolatedStringConstant(context.AST, CreateOrigin(new Range(startPos, startPos + lexeme.Length), lexer, context), lexeme),
            //             lexer, context, ct
            //         )
            //     );

            //     System.Array.Copy(m.ToArray(), 0, members, 1, m.Count);

            //     result.AddMessages(AddOutgoingEdges(interp, members, SemanticRole.Member));

            //     result.Value = result.AddMessages(FinishNode(interp, lexer, context, ct));
            // }
            // else
            // {
                var n = NodeFactory.StringConstant(context.AST, CreateOrigin(new Range(startPos, startPos + lexeme.Length), lexer, context), lexeme);

                result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));
            // }

            return result;
        }

                // private Result<List<Node>> ParseJsxTextOrJsxExpressionSequenceRest(SyntaxKind first, Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<List<Node>>();

        //     var sequence = new List<Node>();

        //     // [dho] if there is a JSX expression ahead then we are going to join that, and any subsequent
        //     // JSX texts or expressions into one long interpolated string constant - 12/06/19
        //     {
        //         var lookAhead = lexer.Clone();

        //         while(true)
        //         {
        //             if(HasErrors(result) || ct.IsCancellationRequested) return result;


        //             token = result.AddMessages(NextToken(lookAhead, context, ct));

        //             // [dho] sequence must start with right type of token, eg. we care if the sequence is `JsxText JsxExpression`,
        //             // but we do not want to treat a sequence of `JsxExpression JsxExpression` as an interpolated string - 12/06/19
        //             if(sequence.Count == 0 && token.Kind != first) break;

        //             if(token.Kind == SyntaxKind.JsxText)
        //             {
        //                 lexer.Pos = lookAhead.Pos;

        //                 sequence.Add(
        //                     result.AddMessages(
        //                         XXXXXXXXX(token, lexer, context, ct)
        //                     )
        //                 );
        //             }
        //             else if(token.Kind == SyntaxKind.OpenBraceToken)
        //             {
        //                 lexer.Pos = lookAhead.Pos;

        //                 sequence.Add(
        //                     result.AddMessages(
        //                         XXXXXXWQADADDS(false /* in expression */, token, lexer, context, ct)
        //                     )
        //                 );
        //             }
        //             else
        //             {
        //                 break;
        //             }
        //         }
        //     }

        //     result.Value = sequence;

        //     return result;
        // }

        private Result<Node> ParseJsxExpression(bool inExpression, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    if(token.Kind == SyntaxKind.DotDotDotToken)
                    {
                        result.Value = result.AddMessages(ParseSpreadElement(token, lexer, context, ct));
                    }
                    else
                    {
                        result.Value = result.AddMessages(ParseAssignmentExpressionOrHigher(token, lexer, context, ct));
                    }

                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );
                }



            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'{' expected", lexer, context)
                );
            }

            return result;
        }

        private bool TagNamesAreEquivalent(Node lhs, Node rhs, Context context)
        {
            if(lhs.Kind != rhs.Kind)
            {
                return false;
            }

            if(lhs.Kind == SemanticKind.Identifier)
            {
                return rhs.Kind == SemanticKind.Identifier && 
                    (ASTNodeFactory.Identifier(context.AST, (DataNode<string>)lhs).Lexeme == 
                        ASTNodeFactory.Identifier(context.AST, (DataNode<string>)rhs).Lexeme);
            }

            if(lhs.Kind == SemanticKind.IncidentContextReference)
            {
                return true;
            }


            // [dho] NOTES from original source (10/06/19) :
            // If we are at this statement then we must have PropertyAccessExpression and because tag name in Jsx element can only
            // take forms of JsxTagNameExpression which includes an identifier, "this" expression, or another propertyAccessExpression
            // it is safe to case the expression property as such. See parseJsxElementName for how we parse tag name in Jsx element
            return true;
            //todo
            //((PropertyAccessExpression)lhs).name.text == ((PropertyAccessExpression)rhs).name.text &&
            //tagNamesAreEquivalent(((PropertyAccessExpression)lhs).expression as JsxTagNameExpression, ((PropertyAccessExpression)rhs).expression as JsxTagNameExpression);
        }

#endregion

        private Result<Node> ParseArgumentOrArrayLiteralElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.DotDotDotToken)
            {
                return ParseSpreadElement(token, lexer, context, ct);
            }
            else if (token.Kind == SyntaxKind.CommaToken)
            {
                var r = ParseOmittedExpression(token, lexer, context, ct);

                if (!HasErrors(r))
                {
                    // [dho] this fixes the issues where you have `[, foo]` and because 
                    // we have already taken the `,` token, the list parser will choke because
                    // it will not see a separator so we rewind the lexer after successfully dealing
                    // with the omitted expression - 30/03/19
                    lexer.Pos = token.StartPos;
                }

                return r;
            }
            else
            {
                return ParseAssignmentExpressionOrHigher(token, lexer, context, ct);
            }
        }



        public Result<Node> ParseObjectLiteralElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `...x` 
            //        ^^^    - 11/02/19
            if (token.Kind == SyntaxKind.DotDotDotToken)
            {
                return ParseSpreadElement(token, lexer, context, ct);
            }

            var result = new Result<Node>();

            var startPos = token.StartPos;

            // [dho] NOTE we use a look ahead because we want
            // to pass the original token to the actual parsing
            // delegate, in case it has prefixes like annotations
            // or modifiers - 23/03/19  
            var lookAhead = lexer.Clone();

            Token lookAheadToken = token;

            if (EatOrnamentation(token, lookAhead, context, ct))
            {
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    result.AddMessages(r);

                    return result;
                }

                lookAheadToken = r.Value;
            }

            // [dho] `*foo()` 
            //        ^        - 23/03/19
            if (lookAheadToken.Kind == SyntaxKind.AsteriskToken)
            {
                result.Value = result.AddMessages(
                    // [dho] generator method - 11/02/19
                    ParseMethodDeclaration(token, lexer, context, ct)
                );
            }
            else
            {
                var role = GetSymbolRole(lookAheadToken, lookAhead, context, ct);

                // [dho] `get ...` 
                //        ^^^       - 11/02/19
                if (role == SymbolRole.Accessor)
                {
                    result.Value = result.AddMessages(
                        ParseAccessorDeclaration(token, lexer, context, ct)
                    );
                }
                // [dho] `set ...` 
                //        ^^^       - 11/02/19
                else if (role == SymbolRole.Mutator)
                {
                    result.Value = result.AddMessages(
                        ParseMutatorDeclaration(token, lexer, context, ct)
                    );
                }
                else
                {
                    // [dho] could be Identifier or ComputedPropertyName etc - 23/03/19
                    var r = ParsePropertyName(lookAheadToken, lookAhead, context, ct);

                    if (HasErrors(r))
                    {
                        result.AddMessages(r);

                        return result;
                    }

                    // [dho] `foo?` 
                    //           ^       - 23/03/19
                    EatIfNext(SyntaxKind.QuestionToken, lookAhead, context, ct);

                    // [dho] `foo(` 
                    //           ^       - 23/03/19
                    // [dho] `foo<` 
                    //           ^       - 23/03/19
                    if (LookAhead(IsOpenParenOrLessThan, lookAhead, context, ct).Item1)
                    {
                        result.Value = result.AddMessages(
                            // [dho] NOTE original token and lexer, NOT look ahead - 11/02/19
                            ParseMethodDeclaration(token, lexer, context, ct)
                        );
                    }
                    // [dho] `foo:` 
                    //           ^       - 23/03/19
                    else if (LookAhead(IsColon, lookAhead, context, ct).Item1)
                    {
                        // [dho] NOTE original token and lexer, NOT look ahead - 11/02/19
                        result.Value = result.AddMessages(
                            ParsePropertyAssignment(token, lexer, context, ct)
                        );
                    }
                    // [dho] `foo,` 
                    //           ^       - 23/03/19
                    // [dho] `foo}` 
                    //           ^       - 23/03/19
                    // [dho] `foo=` 
                    //           ^       - 23/03/19
                    else if (LookAhead(IsCommaOrCloseBraceOrEquals, lookAhead, context, ct).Item1)
                    {
                        // [dho] NOTE original token and lexer, NOT look ahead - 11/02/19 
                        result.Value = result.AddMessages(
                            ParseShorthandPropertyAssignment(token, lexer, context, ct)
                        );
                    }
                    else
                    {
                        result.AddMessages(
                            CreateErrorResult<Node>(lookAheadToken, "Could not infer element", lookAhead, context)
                        );
                    }
                }
            }

            return result;
        }

        public Result<Node> ParsePropertyAssignment(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 22/03/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            // [dho] `foo` 
            //        ^^^  - 12/02/19
            var name = result.AddMessages(
                // [dho] changing to `ParsePropertyName` to support computed property names etc - 22/03/19
                /* ParseIdentifier */ParsePropertyName(token, lexer, context, ct)
            );

            Node meta = default(Node);
            // [dho] `foo?` 
            //           ^  - 12/02/19
            if (EatIfNext(SyntaxKind.QuestionToken, lexer, context, ct))
            {
                meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Optional // [dho] the question mark signifies optional - 21/02/19
                ), lexer, context, ct));

                // token = result.AddMessages(NextToken(lexer, context, ct));
            }


            // [dho] `foo:` 
            //           ^  - 12/02/19
            result.AddMessages(
                EatIfNextOrError(SyntaxKind.ColonToken, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }


            token = result.AddMessages(
                NextToken(lexer, context, ct)
            );

            var initializerContext = ContextHelpers.Clone(context);
            // [dho] allow `in expressions` when parsing the initializer - 12/02/19
            initializerContext.Flags &= ~ContextFlags.DisallowInContext;

            var initializer = result.AddMessages(
                ParseAssignmentExpressionOrHigher(token, lexer, initializerContext, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var fieldDecl = NodeFactory.FieldDeclaration(context.AST, CreateOrigin(range, lexer, context));

            result.AddMessages(AddOutgoingEdges(fieldDecl, name, SemanticRole.Name));
            result.AddMessages(AddOutgoingEdges(fieldDecl, initializer, SemanticRole.Initializer));
            result.AddMessages(AddOutgoingEdges(fieldDecl, annotations, SemanticRole.Annotation));
            result.AddMessages(AddOutgoingEdges(fieldDecl, modifiers, SemanticRole.Modifier));
            result.AddMessages(AddOutgoingEdges(fieldDecl, meta, SemanticRole.Meta));

            result.Value = result.AddMessages(FinishNode(fieldDecl, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseShorthandPropertyAssignment(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] TODO decorators and modifiers - 12/02/19


            // [dho] `foo` 
            //        ^^^  - 12/02/19
            var name = result.AddMessages(
                ParseIdentifier(token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }

            var fieldDecl = NodeFactory.FieldDeclaration(context.AST, CreateOrigin(token, lexer, context));

            result.AddMessages(AddOutgoingEdges(fieldDecl, name, SemanticRole.Name));

            // [dho] TODO CLEANUP a shorthand property assignment `{ xx }` is semantically
            // equivalent to writing `{ xx : xx }`
            // One of the fundamental points about Sempiler is we should not care about syntax (sugar), hence
            // we will just set the initializer to be the same as the name.
            // 
            // Rather than use the already parsed `name` in two places (the `Name` and `Initializer`), currently 
            // duplicating the parsing work and just discarding the messages, because these will be stored on the 
            // result when parsing the `name` above - 04/10/18 (ported from old TypeScriptParser - 12/02/19)
            result.AddMessages(AddOutgoingEdges(fieldDecl, ParseIdentifier(token, lexer, context, ct).Value, SemanticRole.Initializer));

            if (EatIfNext(SyntaxKind.QuestionToken, lexer, context, ct))
            {
                // [dho] mark this as optional - 21/02/19
                var meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Optional // [dho] the question mark signifies optional - 21/02/19
                ), lexer, context, ct));

                result.AddMessages(AddOutgoingEdges(fieldDecl, meta, SemanticRole.Meta));
            }

            result.Value = result.AddMessages(FinishNode(fieldDecl, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseOmittedExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return CreateNop(token, lexer, context, ct);
        }

        public Result<Node> ParseSpreadElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.DotDotDotToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                var subject = result.AddMessages(
                    ParseAssignmentExpressionOrHigher(token, lexer, context, ct)
                );

                if (!HasErrors(result))
                {
                    var range = new Range(startPos, lexer.Pos);

                    var spread = NodeFactory.SpreadDestructuring(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(spread, subject, SemanticRole.Subject));

                    result.Value = result.AddMessages(FinishNode(spread, lexer, context, ct));
                }

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        public Result<Node> ParseWithStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var exp = result.AddMessages(
                ParseParenthesizedExpressionAfterSymbolRole(SymbolRole.PrioritySymbolResolutionContext, token, lexer, context, ct)
            );


            if (HasErrors(result))
            {
                return result;
            }


            // [dho] get next token after close paren - 28/03/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `while(...) ...` 
            //                   ^^^  - 28/03/19
            var scope = result.AddMessages(
                ParseStatement(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var psrc = NodeFactory.PrioritySymbolResolutionContext(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(psrc, exp, SemanticRole.SymbolProvider));
            result.AddMessages(AddOutgoingEdges(psrc, scope, SemanticRole.Scope));

            result.Value = result.AddMessages(FinishNode(psrc, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseTryStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `try` 
            //        ^^^    - 14/02/19
            if (role == SymbolRole.ErrorTrapJunction)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }

            // [dho] `try { ... }` 
            //            ^^^^^^^    - 14/02/19
            Node body = result.AddMessages(
                ParseBlock(token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }


            token = result.AddMessages(NextToken(lexer, context, ct));

            var clauses = result.AddMessages(
                ParseErrorClauses(token, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);
                var errorTrapJunction = NodeFactory.ErrorTrapJunction(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(errorTrapJunction, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(errorTrapJunction, clauses, SemanticRole.Clause));

                result.Value = result.AddMessages(FinishNode(errorTrapJunction, lexer, context, ct));
            }

            return result;
        }


        // public struct ErrorClauses
        // {
        //     public Node Handlers;
        //     public Node Finally;

        //     // public bool HasEatenTokens;

        //     public void Deconstruct(out Node handlers, out Node @finally/*, out bool hasEatenTokens*/)
        //     {
        //         handlers = Handlers;
        //         @finally = Finally;
        //         // hasEatenTokens = HasEatenTokens;
        //     }
        // }

        private Result<Node[]> ParseErrorClauses(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node[]>();

            // Node handlers = default(Node);
            // Node @finally = default(Node);
            // bool hasEatenTokens = default(bool);

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `try { ... } catch` 
            //                    ^^^^^    - 14/02/19
            if (role == SymbolRole.ErrorHandlerClause)
            {
                var startPos = token.StartPos;

                var clauses = new List<Node>();

                var lookAhead = lexer.Clone();

                // [dho] NOTE allowing multiple catch clauses,
                // deviating from TypeScript semantics - 14/02/19
                while (role == SymbolRole.ErrorHandlerClause)
                {
                    var handler = result.AddMessages(
                        ParseCatchClause(token, lookAhead, context, ct)
                    );

                    lexer.Pos = lookAhead.Pos;

                    clauses.Add(handler);

                    token = result.AddMessages(NextToken(lookAhead, context, ct));

                    role = GetSymbolRole(token, lookAhead, context, ct);
                }



                // [dho] `try { ... } catch ... finally` 
                //                              ^^^^^^^    - 14/02/19
                if (role == SymbolRole.ErrorFinallyClause)
                {
                    var @finally = result.AddMessages(
                        ParseFinallyClause(token, lookAhead, context, ct)
                    );

                    lexer.Pos = lookAhead.Pos;

                    clauses.Add(@finally);
                }

                result.Value = clauses.ToArray();
                // hasEatenTokens = true;
            }
            // [dho] `try { ... } finally` 
            //                    ^^^^^^^    - 14/02/19
            else if (role == SymbolRole.ErrorFinallyClause)
            {
                var @finally = result.AddMessages(
                    ParseFinallyClause(token, lexer, context, ct)
                );

                result.Value = new [] { @finally };

                // hasEatenTokens = true;
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "Expected error clauses", lexer, context)
                );
            }

            // // [dho] NOTE this should set the `result.Value` regardless
            // // of errors, because it is used with a deconstruct pattern at
            // // callsites that do not check for null - 14/02/19
            // result.Value = new ErrorClauses
            // {
            //     Handlers = handlers,
            //     // Finally = @finally,
            //     // HasEatenTokens = hasEatenTokens
            // };

            return result;
        }

        public Result<Node> ParseCatchClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.ErrorHandlerClause)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var pattern = default(Node[]);

                // [dho] making the pattern optional, deviating from TS spec
                // in case target semantics do not require it - 14/02/19
                if (LookAhead(IsOpenParen, lexer, context, ct).Item1)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    if (HasErrors(result)) return result;

                    // [dho] using `ParseParameters` for now,
                    // because we want to support the same kind of
                    // syntax - 23/03/19
                    pattern = result.AddMessages(
                        // [dho] `catch(...)` 
                        //              ^^^    - 14/02/19
                        ParseParameters(token, lexer, context, ct)
                    );

                    if (HasErrors(result)) return result;
                }


                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                Node body = result.AddMessages(
                    ParseBlock(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var clause = NodeFactory.ErrorHandlerClause(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(clause, pattern, SemanticRole.Pattern));

                result.AddMessages(AddOutgoingEdges(clause, body, SemanticRole.Body));

                result.Value = result.AddMessages(FinishNode(clause, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }


        public Result<Node> ParseFinallyClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.ErrorFinallyClause)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                Node body = result.AddMessages(
                    ParseBlock(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var clause = NodeFactory.ErrorFinallyClause(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(clause, body, SemanticRole.Body));

                result.Value = result.AddMessages(FinishNode(clause, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }


        public Result<Node> ParseMethodDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;
            }

            List<Node> meta = new List<Node>();

            // [dho] `*bar` 
            //        ^       - 12/02/19
            if (token.Kind == SyntaxKind.AsteriskToken)
            {
                meta.Add(
                    result.AddMessages(
                        FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Generator // [dho] the asterisk signifies generator - 23/03/19
                ), lexer, context, ct)
                    )
                );

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;
            }


            Node name = result.AddMessages(
                ParsePropertyName(token, lexer, context, ct)
            );

            // [dho] `foo?` 
            //           ^  - 21/02/19
            if (EatIfNext(SyntaxKind.QuestionToken, lexer, context, ct))
            {
                meta.Add(result.AddMessages(FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Optional // [dho] the question mark signifies optional - 21/02/19
                ), lexer, context, ct)));
            }

            token = result.AddMessages(NextToken(lexer, context, ct));

            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );


            token = result.AddMessages(NextToken(lexer, context, ct));

            Node body = result.AddMessages(ParseFunctionBlockOrSemicolon(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.MethodDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(decl, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(decl, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(decl, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        public Result<Node> ParseDebuggerStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `debugger` 
            //        ^^^^^^^^    - 12/02/19
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Breakpoint)
            {
                var result = new Result<Node>();
                var startPos = token.StartPos;
                var endPos = startPos + Lexeme(token, lexer).Length;

                // [dho] `debugger;` 
                //                ^    - 12/02/19
                if (result.AddMessages(EatSemicolon(lexer, context, ct)))
                {
                    endPos += 1;
                }

                var range = new Range(startPos, endPos);

                var debug = NodeFactory.Breakpoint(context.AST, CreateOrigin(range, lexer, context));

                result.Value = result.AddMessages(FinishNode(debug, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        public Result<Node> ParseExpressionOrLabeledStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var expContext = ContextHelpers.Clone(context);
            expContext.Flags &= ~ContextFlags.DisallowInContext;

            var exp = result.AddMessages(ParseExpression(token, lexer, expContext, ct));



            if (EatIfNext(SyntaxKind.ColonToken, lexer, context, ct))
            {
                var range = new Range(startPos, lexer.Pos);

                var label = NodeFactory.Label(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(label, exp, SemanticRole.Name));

                result.Value = result.AddMessages(FinishNode(label, lexer, context, ct));
            }
            else
            {
                result.Value = exp;

                result.AddMessages(EatSemicolon(lexer, context, ct));
            }

            return result;
        }

        public Result<Node> ParseVariableStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            result.Value = result.AddMessages(ParseVariableDeclarationList(token, lexer, context, ct));

            result.AddMessages(EatSemicolon(lexer, context, ct));

            return result;
        }

        public Result<Node> ParseFunctionExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return ParseFunctionLike(token, lexer, context, ct);
        }

        public Result<Node> ParseFunctionDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var decl = result.AddMessages(ParseFunctionLike(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var hasName = ASTHelpers.GetSingleMatch(context.AST, decl.ID, SemanticRole.Name) != null;

                if (hasName)
                {
                    result.Value = decl;
                }
                else
                {
                    result.AddMessages(
                        CreateErrorResult<Node>(token, "function name expected", lexer, context)
                    );
                }
            }

            return result;
        }

        private Result<Node> ParseFunctionLike(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            Node name = default(Node);
            Node body = default(Node);

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] did we find decorators or modifier tokens to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `function` 
            //        ^^^^^^^^  - 30/01/19
            if (role != SymbolRole.Function)
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }


            var meta = default(Node);
            // [dho] `function *` 
            //                 ^   - 30/01/19
            if (EatIfNext(SyntaxKind.AsteriskToken, lexer, context, ct))
            {
                meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Generator // [dho] the asterisk signifies generator - 23/03/19
                ), lexer, context, ct));
            }


            // [dho] advance to next token - 30/01/19
            token = result.AddMessages(NextToken(lexer, context, ct));


            // [dho] `function foo` 
            //                 ^^^   - 30/01/19
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Identifier)
            {
                name = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                // [dho] advance to next token - 30/01/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            // [dho] parse template, parameters and return type - 10/02/19
            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );

            // [dho] we would have eaten tokens because at the very least there should be
            // a set of `()` - 18/03/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            body = result.AddMessages(ParseFunctionBlockOrSemicolon(token, lexer, context, ct));

            // // [dho] `function foo(...) {` 
            // //                          ^   - 03/02/19
            // if(token.Kind == SyntaxKind.OpenBraceToken)
            // {
            //     // parse the block
            //     body = result.AddMessages(
            //         ParseFunctionBlock(token, lexer, context, ct)
            //     );
            // }
            // // [dho] `function foo(...) ;` 
            // //                          ^   - 03/02/19
            // else if(token.Kind != SyntaxKind.SemicolonToken)
            // {
            //     result.AddMessages(
            //         CreateUnsupportedTokenResult<Node>(token, lexer, context)
            //     );
            // }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var fn = NodeFactory.FunctionDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(fn, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(fn, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(fn, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(fn, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(fn, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(fn, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(fn, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(fn, meta, SemanticRole.Meta));


                result.Value = result.AddMessages(FinishNode(fn, lexer, context, ct));
            }


            return result;
        }

        private Result<Node> ParseFunctionBlock(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var bodyContext = ContextHelpers.Clone(context);
            bodyContext.Flags &= ~ContextFlags.DecoratorContext;

            return ParseBlock(token, lexer, bodyContext, ct);
        }

        public Result<Node> ParseConstructorDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `constructor` 
            //        ^^^^^^^^^^^    - 13/02/19
            if (role == SymbolRole.Constructor)
            {
                // [dho] skip past the keyword - 13/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }


            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );


            token = result.AddMessages(NextToken(lexer, context, ct));

            Node body = result.AddMessages(ParseFunctionBlockOrSemicolon(token, lexer, context, ct));


            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.ConstructorDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(decl, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(decl, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        public Result<Node> ParsePropertyDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            Node name = result.AddMessages(
                ParsePropertyName(token, lexer, context, ct)
            );

            Node meta = default(Node);

            if (EatIfNext(SyntaxKind.QuestionToken, lexer, context, ct))
            {
                meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Optional // [dho] the question mark signifies optional - 21/02/19
                ), lexer, context, ct));
            }

            Node type = default(Node);


            if (EatIfNext(SyntaxKind.ColonToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                type = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );
            }

            Node initializer = default(Node);

            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                initializer = result.AddMessages(
                    ParseNonParameterInitializer(token, lexer, context, ct)
                );
            }

            var range = new Range(startPos, lexer.Pos);

            var decl = NodeFactory.FieldDeclaration(context.AST, CreateOrigin(range, lexer, context));

            result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
            result.AddMessages(AddOutgoingEdges(decl, initializer, SemanticRole.Initializer));
            result.AddMessages(AddOutgoingEdges(decl, type, SemanticRole.Type));
            result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
            result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));
            result.AddMessages(AddOutgoingEdges(decl, meta, SemanticRole.Meta));

            result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));

            result.AddMessages(EatSemicolon(lexer, context, ct));

            return result;
        }


        public Result<Node> ParseAccessorDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            {
                var role = GetSymbolRole(token, lexer, context, ct);

                // [dho] `get ...` 
                //        ^^^       - 13/02/19
                if (role == SymbolRole.Accessor)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                    );

                    return result;
                }
            }

            Node name = result.AddMessages(
                ParsePropertyName(token, lexer, context, ct)
            );

            token = result.AddMessages(NextToken(lexer, context, ct));

            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );


            token = result.AddMessages(NextToken(lexer, context, ct));

            Node body = result.AddMessages(ParseFunctionBlockOrSemicolon(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.AccessorDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(decl, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(decl, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseFunctionBlockOrSemicolon(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind != SyntaxKind.OpenBraceToken && CanParseSemicolon(token, lexer, context, ct))
            {
                var result = new Result<Node>();

                // [dho] if we already have the semicolon then we need to step to before
                // it, so that `EatSemicolon` finds it and eats it, otherwise it would
                // get the next token - 29/03/19
                if (token.Kind == SyntaxKind.SemicolonToken)
                {
                    lexer.Pos = token.StartPos;
                }

                result.AddMessages(EatSemicolon(lexer, context, ct));

                return result;
            }
            else
            {
                return ParseFunctionBlock(token, lexer, context, ct);
            }
        }

        public Result<Node> ParseMutatorDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            {
                var role = GetSymbolRole(token, lexer, context, ct);

                // [dho] `set ...` 
                //        ^^^       - 13/02/19
                if (role == SymbolRole.Mutator)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));
                }
                else
                {
                    result.AddMessages(
                        CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                    );

                    return result;
                }
            }


            Node name = result.AddMessages(
                ParsePropertyName(token, lexer, context, ct)
            );

            token = result.AddMessages(NextToken(lexer, context, ct));

            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );


            token = result.AddMessages(NextToken(lexer, context, ct));

            Node body = result.AddMessages(ParseFunctionBlockOrSemicolon(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.MutatorDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(decl, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(decl, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }


        public Result<Node> ParseEnumDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            Node meta = default(Node);

            {
                var pos = lexer.Pos;

                // [dho] `const enum` 
                //        ^^^^^          - 29/03/19
                if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Constant)
                {
                    meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                        context.AST,
                        CreateOrigin(new Range(pos, lexer.Pos), lexer, context),
                        MetaFlag.Constant
                    ), lexer, context, ct));

                    token = result.AddMessages(NextToken(lexer, context, ct));
                }
            }


            {
                var role = GetSymbolRole(token, lexer, context, ct);

                if (role != SymbolRole.Enumeration)
                {
                    result.AddMessages(CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context));
                }
            }

            if (HasErrors(result)) return result;


            token = result.AddMessages(NextToken(lexer, context, ct));

            Node name = result.AddMessages(
                ParseIdentifier(token, lexer, context, ct)
            );

            if (HasErrors(result)) return result;

            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] deviating from TypeScript spec in order to support
            // type parameters on enum - 17/02/19
            var template = default(Node[]);

            // [dho] `enum Foo<` 
            //                 ^   - 17/02/19
            if (token.Kind == SyntaxKind.LessThanToken)
            {
                template = result.AddMessages(
                    ParseTypeParameters(token, lexer, context, ct)
                );

                // [dho] advance to next token - 17/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;
            }

            // [dho] deviating from TypeScript spec to support heritage on
            // enum, especially in Swift where you can say an enum is Codable etc - 29/08/19
            var (supers, interfaces, hasEatenHeritageClauses) = result.AddMessages(
                ParseTypeHeritageComponents(token, lexer, context, ct)
            );

            if (hasEatenHeritageClauses)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            if (HasErrors(result))
            {
                return result;
            }

            var members = default(Node[]);

            // [dho] `enum Foo {` 
            //                  ^   - 17/02/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // [dho] guards against the case where there are no enum members - 23/03/19
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var memberContext = ContextHelpers.CloneInKind(context, ContextKind.EnumMembers);

                    members = result.AddMessages(
                        // [dho] `enum Foo {...` 
                        //                   ^^^   - 17/02/19
                        ParseCommaDelimitedList(ParseEnumMember, token, lexer, memberContext, ct)
                    );

                    // [dho] `class Foo {...}` 
                    //                      ^   - 23/03/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'{' expected", lexer, context)
                );
            }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.EnumerationTypeDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, supers, SemanticRole.Super));
                result.AddMessages(AddOutgoingEdges(decl, interfaces, SemanticRole.Interface));
                result.AddMessages(AddOutgoingEdges(decl, members, SemanticRole.Member));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(decl, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        public Result<Node> ParseEnumMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node name = result.AddMessages(
                ParsePropertyName(token, lexer, context, ct)
            );

            Node initializer = default(Node);

            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                var initContext = ContextHelpers.Clone(context);

                initContext.Flags &= ~ContextFlags.DisallowInContext;

                initializer = result.AddMessages(
                    ParseAssignmentExpressionOrHigher(token, lexer, initContext, ct)
                );
            }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var member = NodeFactory.EnumerationMemberDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(member, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(member, initializer, SemanticRole.Initializer));

                result.Value = result.AddMessages(FinishNode(member, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParsePropertyName(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.StringLiteral:
                    return ParseStringLiteral(token, lexer, context, ct);

                case SyntaxKind.NumericLiteral:
                    return ParseNumericLiteral(token, lexer, context, ct);

                // [dho] computed property - 17/02/19
                case SyntaxKind.OpenBracketToken:
                    return ParseComputedPropertyName(token, lexer, context, ct);

                default:
                    // [dho] we cannot just use `ParseIdentifier` in case the property
                    // name is also a keyword, like `set` - 30/03/19
                    return ParseKnownSymbolRoleAsIdentifier(token, lexer, context, ct);
            }
        }

        public Result<Node> ParseComputedPropertyName(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `[...]` 
            //        ^       - 22/03/19
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result))
                {
                    return result;
                }

                var expContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 09/02/19
                expContext.Flags &= ~ContextFlags.DisallowInContext;

                // [dho] `[...]` 
                //         ^^^     - 22/03/19
                var exp = result.AddMessages(
                    ParseExpression(token, lexer, expContext, ct)
                );

                result.AddMessages(
                    // [dho] `[...]` 
                    //            ^    - 22/03/19
                    EatIfNextOrError(SyntaxKind.CloseBracketToken, lexer, context, ct)
                );

                result.Value = exp;

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        public Result<Node> ParseInterfaceDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 18/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            if (HasErrors(result))
            {
                return result;
            }

            Node name = default(Node);

            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Interface)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "Expected interface type declaration keyword", lexer, context)
                );
            }


            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Identifier)
            {
                name = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "Expected identifier", lexer, context)
                );
            }

            if (HasErrors(result))
            {
                return result;
            }


            var template = default(Node[]);

            // [dho] `interface Foo<` 
            //                     ^   - 18/02/19
            if (token.Kind == SyntaxKind.LessThanToken)
            {
                template = result.AddMessages(
                    ParseTypeParameters(token, lexer, context, ct)
                );

                // [dho] advance to next token - 18/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            if (HasErrors(result))
            {
                return result;
            }


            var (supers, interfaces, hasEatenHeritageClauses) = result.AddMessages(
                ParseTypeHeritageComponents(token, lexer, context, ct)
            );

            if (hasEatenHeritageClauses)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            if (HasErrors(result))
            {
                return result;
            }

            var members = default(Node[]);

            // [dho] `interface Foo {` 
            //                      ^   - 18/02/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // [dho] guard against the case where it's an empty interface - 19/03/19
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    members = result.AddMessages(
                        // [dho] `interface Foo {...` 
                        //                       ^^^   - 18/02/19
                        ParseObjectTypeMembers(token, lexer, context, ct)
                    );

                    // [dho] `interface Foo {...}` 
                    //                          ^   - 18/02/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'{' expected", lexer, context)
                );
            }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.InterfaceDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, supers, SemanticRole.Super));
                result.AddMessages(AddOutgoingEdges(decl, interfaces, SemanticRole.Interface));
                result.AddMessages(AddOutgoingEdges(decl, members, SemanticRole.Member));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        Result<Node> ParseTypeLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `{` 
            //        ^   - 18/02/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var members = new Node[]{};

                // [dho] guard against eating too many tokens when the block is empty - 29/03/19
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    members = result.AddMessages(
                        // [dho] `{...` 
                        //         ^^^   - 29/03/19
                        ParseObjectTypeMembers(token, lexer, context, ct)
                    );

                    // [dho] `{...}` 
                    //            ^   - 29/03/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );
                }

                if (HasErrors(result))
                {
                    return result;
                }

                var range = new Range(startPos, lexer.Pos);

                var origin = CreateOrigin(range, lexer, context);

                if(members.Length == 1)
                {
                    var member = members[0];

                    // [dho] single member with indexer signature indicates a dictionary
                    // type, rather than anonymous type with indexer - 03/10/18 (ported 18/02/19)
                    if (member?.Kind == SemanticKind.IndexerSignature)
                    {
                        var indexer = ASTNodeFactory.IndexerSignature(context.AST, member);

                        var parameters = indexer.Parameters;

                        if(parameters.Length == 1)
                        {
                            var parameter = parameters[0];

                            if (parameter?.Kind == SemanticKind.ParameterDeclaration)
                            {
                                var parameterDecl = ASTNodeFactory.ParameterDeclaration(indexer.AST, parameter);

                                if (parameterDecl.Type != null && indexer.Type != null)
                                {
                                    var dictionary = NodeFactory.DictionaryTypeReference(
                                        context.AST,
                                        origin
                                    );

                                    result.AddMessages(AddOutgoingEdges(dictionary, parameterDecl.Type, SemanticRole.KeyType));
                                    result.AddMessages(AddOutgoingEdges(dictionary, indexer.Type, SemanticRole.StoredType));

                                    result.Value = result.AddMessages(FinishNode(dictionary, lexer, context, ct));

                                    return result;
                                }
                            }
                        }

                    }
                }


                // [dho] otherwise we treat the type literal as an anonymous type reference - 03/10/18 (ported 18/02/19)
                var anonTypeRef = NodeFactory.DynamicTypeReference(context.AST, origin);

                result.AddMessages(AddOutgoingEdges(anonTypeRef, members, SemanticRole.Member));

                result.Value = result.AddMessages(FinishNode(anonTypeRef, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateErrorResult<Node>(token, "'{' expected", lexer, context);
            }
        }

        Result<Node[]> ParseObjectTypeMembers(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var memberContext = ContextHelpers.CloneInKind(context, ContextKind.TypeMembers);

            return ParseList(ParseTypeMember, token, lexer, memberContext, ct);
        }

        Result<Node> ParseTypeMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenParenToken || token.Kind == SyntaxKind.LessThanToken)
            {
                return ParseCallSignatureMember(token, lexer, context, ct);
            }
            else if (IsConstructSignatureMember(token, lexer, context, ct))
            {
                return ParseConstructSignatureMember(token, lexer, context, ct);
            }
            else
            {
                var lookAhead = lexer.Clone();

                Token t = token;

                if (EatOrnamentation(token, lookAhead, context, ct))
                {
                    var r = NextToken(lookAhead, context, ct);

                    if (HasErrors(r))
                    {
                        return ParsePropertyOrMethodSignature(token, lexer, context, ct);
                    }

                    t = r.Value;
                }

                if (IsIndexSignature(t, lookAhead, context, ct))
                {
                    return ParseIndexSignatureDeclaration(token, lexer, context, ct);
                }
                else
                {
                    return ParsePropertyOrMethodSignature(token, lexer, context, ct);
                }
            }
        }

        bool IsConstructSignatureMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `new (...` 
            //        ^^^        - 18/02/19
            // [dho] `constructor (...` 
            //        ^^^        - 29/03/19
            if (role == SymbolRole.Construction || role == SymbolRole.Constructor)
            {
                var lookAhead = lexer.Clone();

                var r = NextToken(lookAhead, context, ct);

                if (!HasErrors(r))
                {
                    token = r.Value;

                    return token.Kind == SyntaxKind.OpenParenToken || token.Kind == SyntaxKind.LessThanToken;
                }
            }

            return false;
        }

        Result<Node> ParseConstructSignatureMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `new (...` 
            //        ^^^        - 18/02/19
            // [dho] `constructor (...` 
            //        ^^^        - 29/03/19
            if (role == SymbolRole.Construction || role == SymbolRole.Constructor)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );
            }

            if (HasErrors(result))
            {
                return result;
            }


            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var signature = NodeFactory.ConstructorSignature(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(signature, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(signature, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }

            result.AddMessages(EatTypeMemberDelimiter(lexer, context, ct));

            return result;
        }

        Result<Node> ParseCallSignatureMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                // [dho] NOTE original parser treated this as a 'CallSignature' but since we treat the 
                // `new(...)` case as a 'ConstructorSignature', we might as well be consistent - 23/02/19
                var signature = NodeFactory.MethodSignature(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(signature, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(signature, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }

            result.AddMessages(EatTypeMemberDelimiter(lexer, context, ct));

            return result;
        }

        bool IsIndexSignature(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `[` 
            //        ^   - 18/02/19
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                var lookAhead = lexer.Clone();

                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;

                // [dho] `[...` 
                //         ^^^   - 18/02/19
                if (token.Kind == SyntaxKind.DotDotDotToken ||
                    // [dho] `[]` 
                    //         ^   - 18/02/19
                    token.Kind == SyntaxKind.CloseBracketToken)
                {
                    return true;
                }


                var role = GetSymbolRole(token, lookAhead, context, ct);

                if (role == SymbolRole.Modifier)
                {
                    r = NextToken(lookAhead, context, ct);

                    return !HasErrors(r) && IsIdentifier(r.Value, lookAhead, context, ct);
                }

                if (!IsIdentifier(token, lookAhead, context, ct))
                {
                    return false;
                }



                r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;

                // [dho] `[foo:` 
                //            ^   - 18/02/19
                if (token.Kind == SyntaxKind.ColonToken ||
                    // [dho] `[foo,` 
                    //            ^   - 18/02/19
                    token.Kind == SyntaxKind.CommaToken)
                {
                    return true;
                }

                if (token.Kind != SyntaxKind.QuestionToken)
                {
                    return false;
                }


                r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;

                // [dho] `[foo?:` 
                //             ^   - 18/02/19
                return token.Kind == SyntaxKind.ColonToken ||
                        // [dho] `[foo?,` 
                        //             ^   - 18/02/19
                        token.Kind == SyntaxKind.CommaToken ||
                            // [dho] `[foo?]` 
                            //             ^   - 18/02/19
                            token.Kind == SyntaxKind.CloseBracketToken;
            }

            return false;
        }

        Result<Node> ParseIndexSignatureDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            var parameterContext = ContextHelpers.CloneInKind(context, ContextKind.Parameters);

            var parameters = result.AddMessages(
                ParseBracketedCommaDelimitedList(ParseParameter, SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken, token, lexer, parameterContext, ct)
            );


            Node type = default(Node);

            if (EatIfNext(SyntaxKind.ColonToken, lexer, context, ct))
            {
                // [dho] advance to next token - 30/01/19
                token = result.AddMessages(NextToken(lexer, context, ct));

                type = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );
            }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var signature = NodeFactory.IndexerSignature(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(signature, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(signature, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(signature, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }

            result.AddMessages(EatTypeMemberDelimiter(lexer, context, ct));

            return result;
        }

        Result<Node> ParsePropertyOrMethodSignature(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            var role = GetSymbolRole(token, lexer, context, ct);

            Node name = default(Node);

            if (role != SymbolRole.Accessor && role != SymbolRole.Mutator)
            {
                name = result.AddMessages(
                    ParsePropertyName(token, lexer, context, ct)
                );
            }

            Node meta = default(Node);

            if (EatIfNext(SyntaxKind.QuestionToken, lexer, context, ct))
            {
                meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                    context.AST,
                    CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                    MetaFlag.Optional // [dho] the question mark signifies optional - 21/02/19
                ), lexer, context, ct));
            }


            if (HasErrors(result)) return result;

            // [dho] TODO CLEANUP repeated code below - 29/03/19
            if (role == SymbolRole.Accessor)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                var (template, parameters, type) = result.AddMessages(
                    ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var signature = NodeFactory.AccessorSignature(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(signature, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(signature, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(signature, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(signature, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(signature, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(signature, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }
            else if (role == SymbolRole.Mutator)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                var (template, parameters, type) = result.AddMessages(
                    ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var signature = NodeFactory.MutatorSignature(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(signature, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(signature, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(signature, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(signature, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(signature, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(signature, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }
            else if (LookAhead(IsOpenParenOrLessThan, lexer, context, ct).Item1)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                var (template, parameters, type) = result.AddMessages(
                    ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var signature = NodeFactory.MethodSignature(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(signature, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(signature, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(signature, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(signature, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(signature, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(signature, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }
            else
            {
                Node type = default(Node);
                Node initializer = default(Node);

                if (EatIfNext(IsColon, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    type = result.AddMessages(
                        ParseType(token, lexer, context, ct)
                    );
                }

                if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    // [dho] deviating from TS here to allow for initializers on property
                    // signatures - 29/03/19
                    initializer = result.AddMessages(
                        ParseNonParameterInitializer(token, lexer, context, ct)
                    );
                }

                var range = new Range(startPos, lexer.Pos);

                var signature = NodeFactory.FieldSignature(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(signature, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(signature, initializer, SemanticRole.Initializer));
                result.AddMessages(AddOutgoingEdges(signature, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(signature, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(signature, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(signature, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(signature, lexer, context, ct));
            }

            result.AddMessages(EatTypeMemberDelimiter(lexer, context, ct));

            return result;
        }

        public Result<Node> ParseGlobalDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 19/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `global ...` 
            //        ^^^^^^       - 19/02/19
            if (role == SymbolRole.GlobalContextReference)
            {
                // [dho] `global {` 
                //               ^    - 19/02/19
                if (EatIfNext(SyntaxKind.OpenBraceToken, lexer, context, ct))
                {
                    Node[] members = default(Node[]);

                    // [dho] guard against eating too many tokens when the block is empty - 19/03/19
                    if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        var membersContext = ContextHelpers.CloneInKind(context, ContextKind.BlockStatements);

                        members = result.AddMessages(
                            ParseList(ParseStatement, token, lexer, membersContext, ct)
                        );

                        result.AddMessages(
                            EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                        );
                    }

                    if (!HasErrors(result))
                    {
                        var range = new Range(startPos, lexer.Pos);

                        var decl = NodeFactory.GlobalDeclaration(
                            context.AST,
                            CreateOrigin(new Range(startPos, lexer.Pos), lexer, context)
                        );

                        result.AddMessages(
                            AddOutgoingEdges(decl, members, SemanticRole.Member)
                        );

                        result.AddMessages(
                            AddOutgoingEdges(decl, annotations, SemanticRole.Annotation)
                        );

                        result.AddMessages(
                            AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier)
                        );
                        
                        result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
                    }
                }
                else
                {
                    result.AddMessages(CreateUnsupportedTokenResult<Node>(token, lexer, context));
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> AddOutgoingEdges(ASTNode parent, IEnumerable<Node> children, SemanticRole childRole) => AddOutgoingEdges(parent.AST, parent.Node, children, childRole);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> AddOutgoingEdges(RawAST ast, Node parent, IEnumerable<Node> children, SemanticRole childRole)
        {
            var result = new Result<object>();
            
            if(children != null)
            {
                List<Node> newNodes = new List<Node>();

                foreach(var child in children)
                {
                    if(child == null) continue;

                    if(child is LegacyOrderedGroupHack)
                    {
                        newNodes.AddRange(((LegacyOrderedGroupHack)child).Nodes);
                    }
                    else
                    {
                        newNodes.Add(child);
                    }
                }


                ASTHelpers.Connect(ast, parent.ID, newNodes.ToArray(), childRole);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> AddOutgoingEdges(ASTNode parent, Node child, SemanticRole childRole) => AddOutgoingEdges(parent.AST, parent.Node, child, childRole);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Result<object> AddOutgoingEdges(RawAST ast, Node parent, Node child, SemanticRole childRole)
        {
            var result = new Result<object>();

            if(child != null)
            {
                if(child is LegacyOrderedGroupHack)
                {
                    result.AddMessages(AddOutgoingEdges(ast, parent, ((LegacyOrderedGroupHack)child).Nodes, childRole));
                }
                else
                {
                    ASTHelpers.Connect(ast, parent.ID, new Node[] { child }, childRole);
                }
            }
        
            return result;
        }

        public Result<Node> ParseNamespaceDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 19/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `namespace ...` 
            //        ^^^^^^^^^       - 19/02/19
            if (role == SymbolRole.Namespace)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                var (name, members) = result.AddMessages(
                    ParseNamespaceDeclarationComponents(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                var decl = NodeFactory.NamespaceDeclaration(
                    context.AST,
                    CreateOrigin(new Range(startPos, lexer.Pos), lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));

                result.AddMessages(
                    AddOutgoingEdges(decl, members, SemanticRole.Member)
                );

                result.AddMessages(
                    AddOutgoingEdges(decl, annotations, SemanticRole.Annotation)
                );

                result.AddMessages(
                    AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier)
                );

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );
            }

            return result;
        }

        public Result<Node> ParseImportDeclarationOrImportEqualsDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 19/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            // [dho] `import ...` 
            //        ^^^^^^       - 19/02/19
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Import)
            {
                Node specifier = default(Node);
                var clauses = default(List<Node>);

                token = result.AddMessages(NextToken(lexer, context, ct));


                // [dho] deviating from TS - we want to allow ambient imports:
                // `import System` - 01/03/19
                if (token.Kind == SyntaxKind.StringLiteral)
                {
                    specifier = result.AddMessages(
                        ParseStringLiteral(token, lexer, context, ct)
                    );

                    // but if the next token is on the same line and is NOT a semicolon,
                    // then it's an error - 01/03/19
                    if (LookAhead(IsSameLineButNotSemicolon, lexer, context, ct).Item1)
                    {
                        result.AddMessages(
                            CreateTokenAheadErrorResult("Unexpected end of import", lexer, context, ct)
                        );

                        return result;
                    }
                }
                else
                {
                    clauses = new List<Node>();

                    clauses.AddRange(
                        result.AddMessages(
                            ParseImportClause(token, lexer, context, ct)
                        )
                    );

                    // [dho] comma separated clauses - 19/02/19
                    while (EatIfNext(SyntaxKind.CommaToken, lexer, context, ct))
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        // [dho] flatten the clauses so we are just dealing with one
                        // dimension at most - 27/04/19
                        foreach(var (c, _) in ASTNodeHelpers.IterateMembers(result.AddMessages(
                                ParseImportClause(token, lexer, context, ct)
                            )))
                            {
                                clauses.Add(c);
                            }
                    }

                    if (HasErrors(result))
                    {
                        return result;
                    }

                    // [dho] `import ... =` 
                    //                   ^    - 19/02/19
                    if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        // [dho] deviate from TS here, where it is only
                        // valid if `require(...)` is used, but that is a context
                        // specific constraint - 19/02/19
                        specifier = result.AddMessages(
                            ParseModuleSpecifier(token, lexer, context, ct)
                        );
                    }
                    // [dho] `import ... from` 
                    //                   ^^^^    - 19/02/19
                    else if (EatIfNext(SymbolRole.PackageReference, lexer, context, ct))
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        specifier = result.AddMessages(
                            ParseModuleSpecifier(token, lexer, context, ct)
                        );
                    }
                    else
                    {
                        result.AddMessages(
                            CreateTokenAheadErrorResult("Unexpected end of import", lexer, context, ct)
                        );
                    }
                }


                if (!HasErrors(result))
                {
                    var range = new Range(startPos, lexer.Pos);

                    var import = NodeFactory.ImportDeclaration(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(import, specifier, SemanticRole.Specifier));
                    result.AddMessages(AddOutgoingEdges(import, clauses, SemanticRole.Clause));
                    result.AddMessages(AddOutgoingEdges(import, annotations, SemanticRole.Annotation));
                    result.AddMessages(AddOutgoingEdges(import, modifiers, SemanticRole.Modifier));

                    result.Value = result.AddMessages(FinishNode(import, lexer, context, ct));

                    result.AddMessages(EatSemicolon(lexer, context, ct));
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }



            return result;
        }

        private Result<Node[]> ParseImportClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node[]>();

            // [dho] `import Foo` 
            //               ^^^   - 19/02/19
            if (IsIdentifier(token, lexer, context, ct))
            {
                var name = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                var range = new Range(token.StartPos, lexer.Pos);

                Node defaultExport = result.AddMessages(FinishNode(
                    NodeFactory.DefaultExportReference(context.AST, CreateOrigin(range, lexer, context)),
                    lexer, context, ct
                ));

                var refAliasDecl = result.AddMessages(FinishNode(
                    NodeFactory.ReferenceAliasDeclaration(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    ),
                    lexer, context, ct
                ));

                result.AddMessages(AddOutgoingEdges(context.AST, refAliasDecl, defaultExport, SemanticRole.From));
                result.AddMessages(AddOutgoingEdges(context.AST, refAliasDecl, name, SemanticRole.Name));

                result.Value = new [] { refAliasDecl };
            }
            // [dho] `import *` 
            //               ^   - 19/02/19
            else if (token.Kind == SyntaxKind.AsteriskToken)
            {
                var (isRefAlias, la) = LookAhead(IsReferenceAlias, lexer, context, ct);
                // [dho] `import * as` 
                //                 ^^   - 19/02/19
                if (isRefAlias)
                {
                    lexer.Pos = la.Pos;

                    token = result.AddMessages(NextToken(lexer, context, ct));

                    // [dho] `import * as Foo` 
                    //                    ^^^   - 19/02/19
                    var name = result.AddMessages(
                        ParseIdentifier(token, lexer, context, ct)
                    );

                    var range = new Range(token.StartPos, lexer.Pos);

                    Node wildcardExport = result.AddMessages(FinishNode(
                        NodeFactory.WildcardExportReference(context.AST, CreateOrigin(range, lexer, context)),
                        lexer, context, ct
                    ));

                    var refAliasDecl = result.AddMessages(FinishNode(
                        NodeFactory.ReferenceAliasDeclaration(
                            context.AST,
                            CreateOrigin(range, lexer, context)
                        ),
                        lexer, context, ct
                    ));

                    result.AddMessages(AddOutgoingEdges(context.AST, refAliasDecl, wildcardExport, SemanticRole.From));
                    result.AddMessages(AddOutgoingEdges(context.AST, refAliasDecl, name, SemanticRole.Name));

                    result.Value = new [] { refAliasDecl };
                }
                else
                {
                    var range = new Range(token.StartPos, token.StartPos + Lexeme(token, lexer).Length);
                    
                    Node wildcardExport = result.AddMessages(FinishNode(
                        NodeFactory.WildcardExportReference(context.AST, CreateOrigin(range, lexer, context)),
                        lexer, context, ct
                    ));

                    result.Value = new [] { wildcardExport };
                }
            }
            // [dho] `import {` 
            //               ^   - 19/02/19
            else if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var clauseContext = ContextHelpers.CloneInKind(context, ContextKind.ImportOrExportSpecifiers);

                result.Value = result.AddMessages(
                    ParseBracketedCommaDelimitedList(ParseImportSpecifier, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken, token, lexer, clauseContext, ct)
                );
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }

            return result;
        }

        public Result<Node> ParseImportSpecifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            // [dho] `import { ...` 
            //                 ^^^     - 19/03/19
            Node from = result.AddMessages(
                ParseIdentifier(token, lexer, context, ct)
            );

            if (HasErrors(result)) return result;

            // [dho] `import { ... as` 
            //                     ^^     - 19/03/19
            if (EatIfNext(SymbolRole.ReferenceAlias, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                Node name = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var alias = NodeFactory.ReferenceAliasDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(alias, from, SemanticRole.From));

                result.AddMessages(AddOutgoingEdges(alias, name, SemanticRole.Name));

                result.Value = result.AddMessages(FinishNode(alias, lexer, context, ct));
            }
            else
            {
                result.Value = from;
            }

            return result;
        }

        public Result<Node> ParseTypeAliasDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            if (role != SymbolRole.TypeAlias)
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }

            token = result.AddMessages(NextToken(lexer, context, ct));

            Node name = result.AddMessages(
                ParseIdentifier(token, lexer, context, ct)
            );

            token = result.AddMessages(NextToken(lexer, context, ct));


            var template = default(Node[]);

            if (token.Kind == SyntaxKind.LessThanToken)
            {
                template = result.AddMessages(
                    ParseTypeParameters(token, lexer, context, ct)
                );

                // [dho] advance to next token - 15/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            Node from = default(Node);

            if (token.Kind == SyntaxKind.EqualsToken)
            {
                // [dho] advance to next token - 15/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));

                from = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );

                result.AddMessages(EatSemicolon(lexer, context, ct));

                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.TypeAliasDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, from, SemanticRole.From));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'=' expected", lexer, context)
                );
            }

            return result;
        }

        public Result<Node> ParseStringLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var n = NodeFactory.StringConstant(context.AST, CreateOrigin(token, lexer, context), token.Lexeme);

            result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseNumericLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var n = NodeFactory.NumericConstant(context.AST, CreateOrigin(token, lexer, context), token.Lexeme);

            result.Value = result.AddMessages(FinishNode(n, lexer, context, ct));

            return result;
        }

        public Result<Node> ParseClassExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return ParseClassLike(token, lexer, context, ct);
        }

        public Result<Node> ParseClassDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var decl = result.AddMessages(ParseClassLike(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var hasName = ASTHelpers.GetSingleMatch(context.AST, decl.ID, SemanticRole.Name) != null;

                if (hasName)
                {
                    result.Value = decl;
                }
                else
                {
                    result.AddMessages(
                        CreateErrorResult<Node>(token, "class name expected", lexer, context)
                    );
                }
            }

            return result;
        }


        private Result<Node> ParseClassLike(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            if (HasErrors(result))
            {
                return result;
            }

            Node name = default(Node);

            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.ObjectType)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                // [dho] optional class name - 14/02/19
                if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Identifier)
                {
                    name = result.AddMessages(
                        ParseNameOfClassDeclarationOrExpression(token, lexer, context, ct)
                    );

                    token = result.AddMessages(NextToken(lexer, context, ct));
                }
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "Expected reference type declaration keyword", lexer, context)
                );
            }

            if (HasErrors(result))
            {
                return result;
            }


            var template = default(Node[]);

            // [dho] `class Foo<` 
            //                 ^   - 10/02/19
            if (token.Kind == SyntaxKind.LessThanToken)
            {
                template = result.AddMessages(
                    ParseTypeParameters(token, lexer, context, ct)
                );

                // [dho] advance to next token - 10/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            if (HasErrors(result))
            {
                return result;
            }


            var (supers, interfaces, hasEatenHeritageClauses) = result.AddMessages(
                ParseTypeHeritageComponents(token, lexer, context, ct)
            );

            if (hasEatenHeritageClauses)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            if (HasErrors(result))
            {
                return result;
            }

            var members = default(Node[]);

            // [dho] `class Foo {` 
            //                  ^   - 10/02/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // [dho] guards against the case where there are no class members - 23/03/19
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    members = result.AddMessages(
                        // [dho] `class Foo {...` 
                        //                   ^^^   - 10/02/19
                        ParseClassMembers(token, lexer, context, ct)
                    );

                    // [dho] `class Foo {...}` 
                    //                      ^   - 23/03/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );
                }
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'{' expected", lexer, context)
                );
            }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.ObjectTypeDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(decl, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(decl, supers, SemanticRole.Super));
                result.AddMessages(AddOutgoingEdges(decl, interfaces, SemanticRole.Interface));
                result.AddMessages(AddOutgoingEdges(decl, members, SemanticRole.Member));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        private Result<Node[]> ParseTypeParameters(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var templateContext = ContextHelpers.CloneInKind(context, ContextKind.TypeParameters);

            return ParseBracketedCommaDelimitedList(ParseTypeParameter, SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken, token, lexer, templateContext, ct);
        }

        private Result<Node> ParseTypeParameter(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node name = result.AddMessages(
                ParseIdentifier(token, lexer, context, ct)
            );

            if (HasErrors(result)) return result;

            var (constraints, hasEatenConstraintTokens) = result.AddMessages(
                ParseTypeConstraintComponents(token, lexer, context, ct)
            );

            Node defaultType = default(Node);

            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                defaultType = result.AddMessages(ParseType(token, lexer, context, ct));

                if (HasErrors(result)) return result;
            }

            var range = new Range(startPos, lexer.Pos);

            var typeParam = NodeFactory.TypeParameterDeclaration(context.AST, CreateOrigin(range, lexer, context));

            result.AddMessages(AddOutgoingEdges(typeParam, name, SemanticRole.Name));
            result.AddMessages(AddOutgoingEdges(typeParam, constraints, SemanticRole.Constraint));
            result.AddMessages(AddOutgoingEdges(typeParam, defaultType, SemanticRole.Default));

            result.Value = result.AddMessages(FinishNode(typeParam, lexer, context, ct));

            return result;
        }

        private Result<Node> ParseNameOfClassDeclarationOrExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return ParseIdentifier(token, lexer, context, ct);
        }

        private bool IsReferenceAlias(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            return role == SymbolRole.ReferenceAlias;
        }

        private bool IsHeritageClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `class Foo extends Bar` 
            //                  ^^^^^^^       - 10/02/19
            return role == SymbolRole.Inheritance || role == SymbolRole.Conformity;
        }

        private bool IsIndexTypeQuery(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `class Foo keyof Bar` 
            //                  ^^^^^       - 24/03/19
            return role == SymbolRole.IndexTypeQuery;
        }


        struct TypeHeritageComponents
        {
            public Node[] Supers;
            public Node[] Interfaces;

            public bool HasEatenTokens;

            public void Deconstruct(out Node[] supers, out Node[] interfaces, out bool hasEatenTokens)
            {
                supers = Supers;
                interfaces = Interfaces;
                hasEatenTokens = HasEatenTokens;
            }
        }

        private Result<TypeHeritageComponents> ParseTypeHeritageComponents(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<TypeHeritageComponents>();

            bool hasEatenTokens = default(bool);

            var lookAhead = lexer.Clone();

            var heritageClauseContext = ContextHelpers.CloneInKind(context, ContextKind.HeritageClauses);
            var heritageElementContext = ContextHelpers.CloneInKind(context, ContextKind.HeritageClauseElement);

            Node[] supers = default(Node[]);
            Node[] interfaces = default(Node[]);

            while (true)
            {
                var role = GetSymbolRole(token, lexer, context, ct);

                // [dho] `extends` 
                //        ^^^^^^^    - 19/03/19
                if (role == SymbolRole.Inheritance)
                {
                    lexer.Pos = lookAhead.Pos;

                    // [dho] `extends ...` 
                    //                ^^^    - 19/03/19
                    token = result.AddMessages(NextToken(lexer, heritageClauseContext, ct));

                    supers = result.AddMessages(
                        ParseCommaDelimitedList(ParseExpressionWithTypeArguments, token, lexer, heritageElementContext, ct)
                    );

                    hasEatenTokens = true;
                }
                // [dho] `implements` 
                //        ^^^^^^^^^^    - 19/03/19
                else if (role == SymbolRole.Conformity)
                {
                    lexer.Pos = lookAhead.Pos;

                    // [dho] `implements ...` 
                    //                   ^^^    - 19/03/19
                    token = result.AddMessages(NextToken(lexer, heritageClauseContext, ct));

                    interfaces = result.AddMessages(
                        ParseCommaDelimitedList(ParseExpressionWithTypeArguments, token, lexer, heritageElementContext, ct)
                    );

                    hasEatenTokens = true;
                }
                else
                {
                    break;
                }

                if (HasErrors(result))
                {
                    break;
                }
                else
                {
                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
            }

            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 23/02/19
            result.Value = new TypeHeritageComponents
            {
                Supers = supers,
                Interfaces = interfaces,
                HasEatenTokens = hasEatenTokens
            };

            return result;
        }


        struct TypeConstraintComponents
        {
            public Node[] Constraints;

            public bool HasEatenTokens;

            public void Deconstruct(out Node[] constraints, out bool hasEatenTokens)
            {
                constraints = Constraints;
                hasEatenTokens = HasEatenTokens;
            }
        }

        private Result<TypeConstraintComponents> ParseTypeConstraintComponents(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<TypeConstraintComponents>();

            bool hasEatenTokens = default(bool);

            var constraints = new List<Node>();

            // [dho] deviating from TypeScript by allowing for any heritage clause,
            // not just inheritance constraints - 23/03/19

            while (true)
            {
                // [dho] `class Foo implements Bar` 
                //                  ^^^^^^^^^^       - 24/03/19
                if (LookAhead(IsConformity, lexer, context, ct).Item1)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    constraints.AddRange(result.AddMessages(
                        ParseIncidentTypeConstraints(token, lexer, context, ct)
                    ));

                    hasEatenTokens = true;

                    if (HasErrors(result)) break;
                }
                // [dho] `class Foo extends Bar` 
                //                  ^^^^^^^       - 24/03/19
                else if (LookAhead(IsInheritance, lexer, context, ct).Item1)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    constraints.AddRange(result.AddMessages(
                        ParseUpperBoundedTypeConstraints(token, lexer, context, ct)
                    ));

                    hasEatenTokens = true;

                    if (HasErrors(result)) break;
                }
                else
                {
                    break;
                }
            }

            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 24/03/19
            result.Value = new TypeConstraintComponents
            {
                Constraints = constraints.ToArray(),
                HasEatenTokens = hasEatenTokens
            };

            return result;
        }

        private Result<Node[]> ParseIncidentTypeConstraints(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.Conformity)
            {
                var result = new Result<Node[]>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                var type = result.AddMessages(
                    ParseTypeConstraintType(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var constraint = NodeFactory.IncidentTypeConstraint(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(constraint, type, SemanticRole.Type));

                result.Value = new [] { result.AddMessages(FinishNode(constraint, lexer, context, ct)) };

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node[]>(role, token, lexer, context);
            }
        }



        private Result<Node[]> ParseUpperBoundedTypeConstraints(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.Inheritance)
            {
                var result = new Result<Node[]>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                var type = result.AddMessages(
                    ParseTypeConstraintType(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var constraint = NodeFactory.UpperBoundedTypeConstraint(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(constraint, type, SemanticRole.Type));

                result.Value = new [] { result.AddMessages(FinishNode(constraint, lexer, context, ct)) };

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node[]>(role, token, lexer, context);
            }
        }


        private Result<Node> ParseTypeConstraintType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (IsStartOfType(token, lexer, context, ct) || !IsStartOfExpression(token, lexer, context, ct))
            {
                return ParseType(token, lexer, context, ct);
            }
            else
            {
                // [dho] deviating from TypeScript and original source here, because having
                // an expression here would be an error, but as long as we can parse it OK
                // we will let later stages of the pipeline (eg. target platform) determine
                // if having an expression here is actually illegal - 23/03/19
                return ParseUnaryExpressionOrHigher(token, lexer, context, ct);
            }
        }


        private Result<Node> ParseMemberTypeConstraint(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `in ...` 
            //        ^^       - 24/03/19
            if (role == SymbolRole.KeyIn)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                // [dho] `in ...` 
                //           ^^^    - 24/03/19
                var type = result.AddMessages(
                    ParseTypeOperatorOrHigher(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var constraint = NodeFactory.MemberTypeConstraint(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(constraint, type, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(constraint, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        private Result<Node> ParseExpressionWithTypeArguments(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] I don't know what to do here, because what other expressions
            // could we have other than type references? Because this delegate is 
            // only used to parse type heritage clauses - 19/03/19
            return ParseTypeReference(token, lexer, context, ct);

            // var result = new Result<Node>();

            // Node exp = result.AddMessages(
            //     ParseLeftHandSideExpressionOrHigher(token, lexer, context, ct)
            // );

            // Node template = default(Node);

            // if(!IsTerminal(result) && LookAhead(IsLessThan, lexer, context, ct).Item1)
            // {
            //     token = result.AddMessages(NextToken(lexer, context, ct));

            //     var templateContext = ContextHelpers.CloneInKind(context, ContextKind.TypeArguments);

            //     template = result.AddMessages(
            //         ParseBracketedCommaDelimitedList(ParseType, SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken, token, lexer, templateContext, ct)
            //     );
            // }

        }

        private Result<Node[]> ParseClassMembers(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var membersContext = ContextHelpers.CloneInKind(context, ContextKind.ClassMembers);

            return ParseList(ParseClassElement, token, lexer, membersContext, ct);
        }

        private Result<Node> ParseClassElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if(token.Kind == SyntaxKind.Directive)
            {
                return ParseDirective(token, lexer, context, ct);
            }


            var result = new Result<Node>();

            var startPos = token.StartPos;

            // [dho] NOTE we use a look ahead because we want
            // to pass the original token to the actual parsing
            // delegate, in case it has prefixes like annotations
            // or modifiers - 23/03/19  
            var lookAhead = lexer.Clone();

            Token lookAheadToken = token;

            if (EatOrnamentation(token, lookAhead, context, ct))
            {
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    result.AddMessages(r);

                    return result;
                }

                lookAheadToken = r.Value;
            }


            // [dho] `*foo()` 
            //        ^        - 23/03/19
            if (lookAheadToken.Kind == SyntaxKind.AsteriskToken)
            {
                result.Value = result.AddMessages(
                    // [dho] generator method - 11/02/19
                    ParseMethodDeclaration(token, lexer, context, ct)
                );
            }
            // [dho] `[index:string]`
            //        ^^^^^^^^^^^^^^    - 23/03/19
            else if (IsIndexSignature(lookAheadToken, lookAhead, context, ct))
            {
                result.Value = result.AddMessages(
                    ParseIndexSignatureDeclaration(token, lexer, context, ct)
                );
            }
            else
            {
                var role = GetSymbolRole(lookAheadToken, lookAhead, context, ct);

                // [dho] `get ...` 
                //        ^^^       - 11/02/19
                if (role == SymbolRole.Accessor &&
                    // [dho] guard against the case where `get` is actually the
                    // property name `{ get: ... }` or `{ get(){ ... } }` - 30/03/19
                    LookAhead(IsIdentifier, lookAhead, context, ct).Item1)
                {
                    result.Value = result.AddMessages(
                        ParseAccessorDeclaration(token, lexer, context, ct)
                    );
                }
                // [dho] `set ...` 
                //        ^^^       - 11/02/19
                else if (role == SymbolRole.Mutator &&
                    // [dho] guard against the case where `set` is actually the
                    // property name `{ set: ... } or `{ set(){ ... } }` - 30/03/19
                    LookAhead(IsIdentifier, lookAhead, context, ct).Item1)
                {
                    result.Value = result.AddMessages(
                        ParseMutatorDeclaration(token, lexer, context, ct)
                    );
                }
                else if (role == SymbolRole.Constructor)
                {
                    result.Value = result.AddMessages(
                        ParseConstructorDeclaration(token, lexer, context, ct)
                    );
                }
                else
                {
                    // [dho] could be Identifier or ComputedPropertyName etc - 23/03/19
                    var r = ParsePropertyName(lookAheadToken, lookAhead, context, ct);

                    if (HasErrors(r))
                    {
                        result.AddMessages(r);

                        return result;
                    }

                    // [dho] `foo?` 
                    //           ^       - 23/03/19
                    EatIfNext(SyntaxKind.QuestionToken, lookAhead, context, ct);

                    // [dho] `foo(` 
                    //           ^       - 23/03/19
                    // [dho] `foo<` 
                    //           ^       - 23/03/19
                    if (LookAhead(IsOpenParenOrLessThan, lookAhead, context, ct).Item1)
                    {
                        result.Value = result.AddMessages(
                            // [dho] NOTE original token and lexer, NOT look ahead - 11/02/19
                            ParseMethodDeclaration(token, lexer, context, ct)
                        );
                    }
                    else
                    {
                        // [dho] NOTE original token and lexer, NOT look ahead - 11/02/19
                        result.Value = result.AddMessages(
                            ParsePropertyDeclaration(token, lexer, context, ct)
                        );
                    }
                }
            }

            return result;
        }

        struct OrnamentationComponents
        {
            public Node[] Annotations;
            public Node[] Modifiers;

            // public Node Meta;

            public bool HasEatenTokens;

            public void Deconstruct(out Node[] annotations, out Node[] modifiers, /* out Node meta,*/ out bool hasEatenTokens)
            {
                annotations = Annotations;
                modifiers = Modifiers;
                // meta = Meta;
                hasEatenTokens = HasEatenTokens;
            }
        }

        private Result<OrnamentationComponents> ParseOrnamentationComponents(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<OrnamentationComponents>();

            bool hasEatenTokens = default(bool);

            var lookAhead = lexer.Clone();

            

            var annotations = new List<Node>();
            var modifiers = new List<Node>();

            while (true)
            {
                if (token.Kind == SyntaxKind.AtToken)
                {
                    var decorator = result.AddMessages(
                        ParseDecorator(token, lookAhead, context, ct)
                    );

                    lexer.Pos = lookAhead.Pos;

                    annotations.Add(decorator);

                    hasEatenTokens = true;
                }
                else if (GetSymbolRole(token, lookAhead, context, ct) == SymbolRole.Modifier)
                {
                    var modifier = result.AddMessages(
                        ParseModifier(token, lookAhead, context, ct)
                    );

                    lexer.Pos = lookAhead.Pos;

                    modifiers.Add(modifier);

                    hasEatenTokens = true;
                }
                else
                {
                    break;
                }


                if (HasErrors(result))
                {
                    break;
                }
                else
                {
                    token = result.AddMessages(NextToken(lookAhead, context, ct));
                }
            }


            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 04/04/19
            result.Value = new OrnamentationComponents
            {
                Annotations = annotations.ToArray(),
                Modifiers = modifiers.ToArray(),
                HasEatenTokens = hasEatenTokens
            };

            return result;
        }

        public Result<Node> ParseDecorator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.AtToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                var decoratorContext = ContextHelpers.Clone(context);
                decoratorContext.Flags |= ContextFlags.DecoratorContext;

                Node expression = result.AddMessages(
                    ParseLeftHandSideExpressionOrHigher(token, lexer, decoratorContext, ct)
                );

                if (HasErrors(result)) return result;

                var range = new Range(startPos, lexer.Pos);

                var decorator = NodeFactory.Annotation(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decorator, expression, SemanticRole.Operand));

                result.Value = result.AddMessages(FinishNode(decorator, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        public Result<Node> ParseModifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Modifier)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var range = new Range(startPos, lexer.Pos);

                var modifier = NodeFactory.Modifier(
                    context.AST,
                    CreateOrigin(range, lexer, context),
                    token.Lexeme
                );

                result.Value = result.AddMessages(FinishNode(modifier, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }


        // private Result<DeclarationPrefixes> XXXXXXXParseOptionalDeclarationPrefixes(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<DeclarationPrefixes>();

        //     Node decorators = default(Node);
        //     Node modifiers = default(Node);
        //     bool hasEatenTokens = default(bool);

        //     if(token.Kind == SyntaxKind.AtToken)
        //     {
        //         decorators = result.AddMessages(
        //             ParseDecorators(token, lexer, context, ct)
        //         );

        //         var lookAhead = lexer.Clone();

        //         token = result.AddMessages(NextToken(lookAhead, context, ct));

        //         if(GetSymbolRole(token, lookAhead, context, ct) == SymbolRole.Modifier)
        //         {
        //             lexer.Pos = lookAhead.Pos;

        //             modifiers = result.AddMessages(
        //                 ParseModifiers(token, lexer, context, ct)
        //             );
        //         }

        //         hasEatenTokens = true;
        //     }
        //     else if(GetSymbolRole(token, lexer, context, ct) == SymbolRole.Modifier)
        //     {
        //         modifiers = result.AddMessages(
        //             ParseModifiers(token, lexer, context, ct)
        //         );

        //         hasEatenTokens = true;
        //     }

        //     // [dho] NOTE this should set the `result.Value` regardless
        //     // of errors, because it is used with a deconstruct pattern at
        //     // callsites that do not check for null - 14/02/19
        //     result.Value = new DeclarationPrefixes
        //     {
        //         Decorators = decorators,
        //         Modifiers = modifiers,
        //         HasEatenTokens = hasEatenTokens
        //     };

        //     return result;
        // }

        public struct NamespaceDeclarationComponents
        {
            public Node Name;
            public Node[] Members;

            public void Deconstruct(out Node name, out Node[] members)
            {
                name = Name;
                members = Members;
            }
        }


        private Result<NamespaceDeclarationComponents> ParseNamespaceDeclarationComponents(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<NamespaceDeclarationComponents>();

            var name = default(Node);
            var members = default(Node[]);

            if (token.Kind == SyntaxKind.StringLiteral)
            {
                name = result.AddMessages(
                    ParseStringLiteral(token, lexer, context, ct)
                );

                token = result.AddMessages(NextToken(lexer, context, ct));

                members = result.AddMessages(
                    ParseBlockLikeContent(token, lexer, context, ct)
                );
            }
            else
            {
                name = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                // [dho] nested namespace of the form `A.B.C {...}` - 23/02/19
                if (EatIfNext(SyntaxKind.DotToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var (childNSName, childNSMembers) = result.AddMessages(
                        ParseNamespaceDeclarationComponents(token, lexer, context, ct)
                    );

                    var childNS = NodeFactory.NamespaceDeclaration(
                        context.AST,
                        CreateOrigin(new Range(token.StartPos, lexer.Pos), lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(childNS, childNSName, SemanticRole.Name));

                    result.AddMessages(AddOutgoingEdges(childNS, childNSMembers, SemanticRole.Member));

                    members = new [] { result.AddMessages(FinishNode(childNS, lexer, context, ct)) };
                }
                else
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    members = result.AddMessages(
                        ParseBlockLikeContent(token, lexer, context, ct)
                    );
                }
            }

            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 23/02/19
            result.Value = new NamespaceDeclarationComponents
            {
                Name = name,
                Members = members
            };

            return result;
        }

        public struct DataValueDeclarationComponents
        {
            public Node Name;
            public Node Type;
            public Node Initializer;

            public void Deconstruct(out Node name, out Node type, out Node initializer)
            {
                name = Name;
                type = Type;
                initializer = Initializer;
            }
        }
        private Result<DataValueDeclarationComponents> ParseDataValueDeclarationComponents(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] NOTE this is a variable declaration inside a variable
            // declaration list hence we do not deal with the storage token, 
            // eg. `var`, `const`, `let` - 15/02/19
            var result = new Result<DataValueDeclarationComponents>();

            Node name = result.AddMessages(
                ParseKnownSymbolRoleAsIdentifierOrPattern(token, lexer, context, ct)
            );

            Node type = default(Node);

            if (EatIfNext(SyntaxKind.ColonToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                type = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );
            }

            Node initializer = default(Node);

            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                initializer = result.AddMessages(
                    ParseAssignmentExpressionOrHigher(token, lexer, context, ct)
                );
            }

            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 14/02/19
            result.Value = new DataValueDeclarationComponents
            {
                Name = name,
                Type = type,
                Initializer = initializer
            };

            return result;

        }


        public struct FunctionSignatureComponents
        {
            public Node[] Template;
            public Node[] Parameters;
            public Node Type;

            public void Deconstruct(out Node[] template, out Node[] parameters, out Node type)
            {
                template = Template;
                parameters = Parameters;
                type = Type;
            }
        }
        private Result<FunctionSignatureComponents> ParseFunctionSignatureComponents(Token token, SyntaxKind returnTypePrefix, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<FunctionSignatureComponents>();

            var template = default(Node[]);
            var parameters = default(Node[]);
            Node returnType = default(Node);

            // [dho] `function foo<` 
            //                    ^   - 30/01/19
            if (token.Kind == SyntaxKind.LessThanToken)
            {
                template = result.AddMessages(
                    ParseTypeParameters(token, lexer, context, ct)
                );

                // [dho] advance to next token - 01/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            // [dho] `function foo(` 
            //                    ^   - 30/01/19
            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                parameters = result.AddMessages(
                    ParseParameters(token, lexer, context, ct)
                );

                // token = result.AddMessages(NextToken(lookAhead, context, ct));

                // [dho] `function foo(...) :` 
                //                          ^   - 31/01/19
                if (EatIfNext(returnTypePrefix, lexer, context, ct))
                {
                    // [dho] advance to next token - 30/01/19
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    returnType = result.AddMessages(
                        ParseTypeOrSmartCast(token, lexer, context, ct)
                    );

                    // // [dho] advance to next token - 31/01/19
                    // token = result.AddMessages(NextToken(lexer, context, ct));
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }


            // [dho] NOTE this should set the `result.Value` regardless
            // of errors, because it is used with a deconstruct pattern at
            // callsites that do not check for null - 14/02/19
            result.Value = new FunctionSignatureComponents
            {
                Template = template,
                Parameters = parameters,
                Type = returnType
            };

            return result;
        }


        public Result<Node[]> ParseParameters(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var paramsContext = ContextHelpers.CloneInKind(context, ContextKind.Parameters);
            paramsContext.Flags |= ContextFlags.AwaitContext | ContextFlags.YieldContext;

            // [dho] `(...)` 
            //        ^        - 18/03/19
            return ParseBracketedCommaDelimitedList(ParseParameter, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken, token, lexer, paramsContext, ct);
        }


        public Result<Node> ParseParameter(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node name = default(Node);
            Node type = default(Node);
            Node defaultValue = default(Node);

            // [dho] deviating from TS spec here, because we want to be able to adorn
            // parameters with ornaments like `ref foo : Bar` or `const foo : Bar` etc. - 23/02/19
            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            var isSpread = token.Kind == SyntaxKind.DotDotDotToken;

            var spreadStartPos = -1;

            if (isSpread)
            {
                spreadStartPos = token.StartPos;
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            // [dho] `(foo)` 
            //         ^^^     - 03/02/19
            // [dho] `({ foo })` 
            //         ^     - 03/02/19
            name = result.AddMessages(
                // [dho] NOTE deviating from TS here.. you can specify (this : XXX) as a first param in TypeScript,
                // telling the system what context the function should be invoked with (using `.call(..)` etc).
                // For now we will not support this as it is hard to articulate, and especially hard to emit in other languages
                // given we do not do type checking and/or do not know the callsites for this function (which may even be beyond
                // all the source code if it is a library used elsewhere) - 23/02/19
                ParseIdentifierOrPattern(token, lexer, context, ct)
            );

            var meta = new List<Node>();

            // [dho] `(foo?)` 
            //            ^     - 03/02/19
            if (EatIfNext(SyntaxKind.QuestionToken, lexer, context, ct))
            {
                meta.Add(
                    result.AddMessages(FinishNode(NodeFactory.Meta(
                        context.AST,
                        CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                        MetaFlag.Optional // [dho] the question mark signifies optional - 21/02/19
                    ), lexer, context, ct))
                );
            }

            // [dho] `(foo:)`
            //            ^     - 03/02/19
            if (EatIfNext(SyntaxKind.ColonToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                type = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );
            }

            // [dho] `(foo=)`
            //            ^     - 03/02/19
            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                defaultValue = result.AddMessages(
                    ParseParameterInitializer(token, lexer, context, ct)
                );
            }

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var param = NodeFactory.ParameterDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(param, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(param, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(param, defaultValue, SemanticRole.Default));
                result.AddMessages(AddOutgoingEdges(param, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(param, modifiers, SemanticRole.Modifier));
                result.AddMessages(AddOutgoingEdges(param, meta, SemanticRole.Meta));

                if (isSpread)
                {
                    var spreadRange = new Range(spreadStartPos, lexer.Pos);

                    var subject = result.AddMessages(FinishNode(param, lexer, context, ct));

                    // [dho] FIX this should be a Variadic param.. maybe set meta for it?? - 05/04/19
                    var spread = NodeFactory.SpreadDestructuring(
                        context.AST,
                        CreateOrigin(spreadRange, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(spread, subject, SemanticRole.Subject));

                    result.Value = result.AddMessages(FinishNode(spread, lexer, context, ct));
                }
                else
                {
                    result.Value = result.AddMessages(FinishNode(param, lexer, context, ct));
                }
            }

            return result;
        }

        private Result<Node> ParseParameterInitializer(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var initializerContext = ContextHelpers.CloneInKind(context, ContextKind.Parameters);

            initializerContext.Flags &= ~(ContextFlags.YieldContext | ContextFlags.DisallowInContext);

            return ParseAssignmentExpressionOrHigher(token, lexer, initializerContext, ct);
        }

        public Result<Node> ParseNonParameterInitializer(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var initializerContext = ContextHelpers.Clone(context);

            initializerContext.Flags &= ~(ContextFlags.YieldContext | ContextFlags.DisallowInContext);

            return ParseAssignmentExpressionOrHigher(token, lexer, initializerContext, ct);
        }

        public Result<Node> ParseEmptyStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return CreateNop(token, lexer, context, ct);
        }

        public Result<Node> CreateNop(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var range = new Range(token.StartPos, token.StartPos);

            var nop = NodeFactory.Nop(context.AST, CreateOrigin(range, lexer, context));

            result.Value = result.AddMessages(FinishNode(nop, lexer, context, ct));

            return result;
        }

        Result<Node[]> ReduceBodyLike(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node[]>();

            var block = result.AddMessages(ParseBlock(token, lexer, context, ct));

            if(block?.Kind == SemanticKind.Block)
            {
                result.Value = ASTNodeFactory.Block(context.AST, block).Content;
            }
            else
            {
                result.Value = new [] { block };
            }

            return result;
        }

        public Result<Node> ParseBlock(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var content = result.AddMessages(
                ParseBlockLikeContent(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var block = NodeFactory.Block(context.AST, CreateOrigin(range, lexer, context));

            result.AddMessages(AddOutgoingEdges(block, content, SemanticRole.Content));

            result.Value = result.AddMessages(FinishNode(block, lexer, context, ct));

            return result;
        }

        public Result<Node[]> ParseBlockLikeContent(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var result = new Result<Node[]>();

                var startPos = token.StartPos;

                var content = default(Node[]);

                // [dho] guard against eating too many tokens when the block is empty - 19/03/19
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var contentContext = ContextHelpers.CloneInKind(context, ContextKind.BlockStatements);

                    content = result.AddMessages(
                        ParseList(ParseStatement, token, lexer, contentContext, ct)
                    );

                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );
                }

                if(HasErrors(result))
                {
                    var lookAhead = lexer.Clone();

                    // [dho] attempt to synchronize - 13/05/19
                    if (EatToBeforeNext(IsBlockTerminator, lookAhead, context, ct) && 
                        EatIfNext(IsBlockTerminator, lookAhead, context, ct))
                    {
                        lexer.Pos = lookAhead.Pos; // [dho] skip the block terminator - 13/05/19
                    }
                }

                result.Value = content;

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node[]>(token, lexer, context);
            }
        }

        public Result<Node> ParseIfStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var exp = result.AddMessages(
                ParseParenthesizedExpressionAfterSymbolRole(SymbolRole.PredicateJunction, token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }


            // [dho] get next token after close paren - 29/01/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            Node trueBranch = default(Node);
            Node falseBranch = default(Node);

            {
                trueBranch = result.AddMessages(
                    ParseStatement(token, lexer, context, ct)
                );

                if (HasErrors(result))
                {
                    return result;
                }
            }

            // [dho] check if there is an `else` keyword ahead - 03/02/19
            {
                var lookAhead = lexer.Clone();

                var t = result.AddMessages(NextToken(lookAhead, context, ct));

                // [dho] `if(...) ... else` 
                //                    ^^^^  - 03/02/19
                if (GetSymbolRole(t, lookAhead, context, ct) == SymbolRole.PredicateJunctionFalseBranch)
                {
                    // [dho] eat the `else` keyword - 27/01/19
                    lexer.Pos = lookAhead.Pos;

                    token = result.AddMessages(NextToken(lexer, context, ct));

                    falseBranch = result.AddMessages(
                        ParseStatement(token, lexer, context, ct)
                    );
                }
            }

            var range = new Range(startPos, lexer.Pos);

            Node fork = result.AddMessages(FinishNode(
                NodeFactory.PredicateJunction(context.AST, CreateOrigin(range, lexer, context)),
                lexer, context, ct)
            );

            result.AddMessages(AddOutgoingEdges(context.AST, fork, exp, SemanticRole.Predicate));
            result.AddMessages(AddOutgoingEdges(context.AST, fork, trueBranch, SemanticRole.TrueBranch));
            result.AddMessages(AddOutgoingEdges(context.AST, fork, falseBranch, SemanticRole.FalseBranch));


            result.Value = fork;

            return result;
        }

        public Result<Node> ParseDoStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var role = GetSymbolRole(token, lexer, context, ct);
            // [dho] `do ...` 
            //        ^^      - 27/01/19
            if (role == SymbolRole.DoWhilePredicateLoop)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }


            // [dho] `do ...` 
            //           ^^^  - 27/01/19
            var body = result.AddMessages(
                ParseStatement(token, lexer, context, ct)
            );


            if (HasErrors(result))
            {
                return result;
            }

            token = result.AddMessages(NextToken(lexer, context, ct));


            // [dho] `do ... while` 
            //               ^^^^^  - 03/02/19
            var exp = result.AddMessages(
                ParseParenthesizedExpressionAfterSymbolRole(SymbolRole.WhilePredicateLoop, token, lexer, context, ct)
            );


            if (HasErrors(result))
            {
                return result;
            }

            var range = new Range(startPos, lexer.Pos);

            var loop = NodeFactory.DoWhilePredicateLoop(
                context.AST,
                CreateOrigin(range, lexer, context)
            );


            result.AddMessages(AddOutgoingEdges(loop, exp, SemanticRole.Condition));
            result.AddMessages(AddOutgoingEdges(loop, body, SemanticRole.Body));


            // From: https://mail.mozilla.org/pipermail/es-discuss/2011-August/016188.html
            // 157 min --- All allen at wirfs-brock.com CONF --- "do{;}while(false)false" prohibited in
            // spec but allowed in consensus reality. Approved -- this is the de-facto standard whereby
            //  do;while(0)x will have a semicolon inserted before x.

            // [dho] check if there is an `;` keyword ahead - 27/01/19
            // [dho] NOTE original source had this as explicitly optional so not using `EatSemicolon` - 24/03/19
            EatIfNext(SyntaxKind.SemicolonToken, lexer, context, ct);

            result.Value = result.AddMessages(FinishNode(loop, lexer, context, ct));

            return result;
        }


        private bool EatIfNext(SyntaxKind kind, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            var r = NextToken(lookAhead, context, ct);

            if (!HasErrors(r) && r.Value.Kind == kind)
            {
                // [dho] eat the token - 23/02/19
                lexer.Pos = lookAhead.Pos;

                return true;
            }

            return false;
        }

        private Result<Node> CreateUnsupportedTokenAheadResult(Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var l = lexer.Clone();

            // [dho] look ahead to the next token for helpful error
            // message generation - 01/03/19
            var token = result.AddMessages(NextToken(l, context, ct));

            result.AddMessages(
                CreateUnsupportedTokenResult<Node>(token, l, context)
            );

            return result;
        }

        private Result<Node> CreateTokenAheadErrorResult(string description, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var l = lexer.Clone();

            // [dho] look ahead to the next token for helpful error
            // message generation - 01/03/19
            var token = result.AddMessages(NextToken(l, context, ct));

            result.AddMessages(
                CreateErrorResult<Node>(token, description, l, context)
            );

            return result;
        }

        private Result<bool> EatIfNextOrError(SyntaxKind kind, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<bool>();

            if (EatIfNext(kind, lexer, context, ct))
            {
                result.Value = true;
            }
            else
            {
                var s = lexer.Pos - 20;
                s = s < 0 ? 0 : s;
                var hint = lexer.SourceText.Substring(s, lexer.Pos - s).Trim();
                
                result.AddMessages(
                    CreateTokenAheadErrorResult($"Expected {kind.ToString()} at or near '{hint}'", lexer, context, ct)
                );

                result.Value = false;
            }

            return result;
        }

        private Result<bool> EatIfNextOrError(SymbolRole role, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<bool>();

            if (EatIfNext(role, lexer, context, ct))
            {
                result.Value = true;
            }
            else
            {
                result.AddMessages(
                    CreateTokenAheadErrorResult($"Expected {role.ToString()}", lexer, context, ct)
                );

                result.Value = false;
            }

            return result;
        }

        private bool EatIfNext(SymbolRole role, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            var r = NextToken(lookAhead, context, ct);

            if (!HasErrors(r) && GetSymbolRole(r.Value, lookAhead, context, ct) == role)
            {
                // [dho] eat the token - 23/02/19
                lexer.Pos = lookAhead.Pos;

                return true;
            }

            return false;
        }

        private bool EatIfNext(LookAheadPredicateDelegate predicate, Lexer lexer, Context context, CancellationToken ct)
        {
            var (isNext, lookAheadLexer) = LookAhead(predicate, lexer, context, ct);

            if (isNext)
            {
                // [dho] eat the token - 27/01/19
                lexer.Pos = lookAheadLexer.Pos;
            }

            return isNext;
        }

        private bool EatToBeforeNext(LookAheadPredicateDelegate predicate, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            while (true)
            {
                if (LookAhead(predicate, lookAhead, context, ct).Item1)
                {
                    lexer.Pos = lookAhead.Pos;

                    return true;
                }

                // [dho] advance the lookAhead on another token - 24/03/19
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r)) break;

                var token = r.Value;

                if (token == null || token.Kind == SyntaxKind.EndOfFileToken)
                {
                    break;
                }
            }

            return false;
        }

        private bool EatParameterStart(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            if (EatOrnamentation(token, lookAhead, context, ct))
            {
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r)) return false;

                token = r.Value;
            }

            // [dho] NOTE deviating from TS here.. you can specify (this : XXX) as a first param in TypeScript,
            // telling the system what context the function should be invoked with (using `.call(..)` etc).
            // For now we will not support this as it is hard to articulate, and especially hard to emit in other languages
            // given we do not do type checking and/or do not know the callsites for this function (which may even be beyond
            // all the source code if it is a library used elsewhere) - 23/02/19
            if (IsIdentifier(token, lookAhead, context, ct))
            {
                // [dho] we only manipulate the passed in lexer
                // if we have successfully skipped the parameter
                // start, otherwise if we fail to do so, we want
                // the lexer's position not corrupted - 01/02/19
                lexer.Pos = lookAhead.Pos;

                return true;
            }


            if (IsOpenBracketOrOpenBrace(token, lookAhead, context, ct) &&
                EatIdentifierOrPattern(token, lookAhead, context, ct))
            {
                // [dho] we only manipulate the passed in lexer
                // if we have successfully skipped the parameter
                // start, otherwise if we fail to do so, we want
                // the lexer's position not corrupted - 01/02/19
                lexer.Pos = lookAhead.Pos;

                return true;
            }

            return false;
        }

        // private bool EatDecorators(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     // [dho] if we encounter any decorators up front then we will
        //     // skip over them to uncover the type of construct beyond them 
        //     // that they apply to - 03/02/19
        //     if(token.Kind == SyntaxKind.AtToken)
        //     {
        //         var lookAhead = lexer.Clone();

        //         var result = new Result<object>();

        //         var newPos = lookAhead.Pos;

        //         // [dho] Eat passed the modifiers - 02/02/19
        //         while(token.Kind == SyntaxKind.AtToken)
        //         {
        //             token = result.AddMessages(NextToken(lookAhead, context, ct));

        //             // [dho] `@foo()` 
        //             //         ^^^^^  - 03/02/19
        //             result.AddMessages(ParseToken(token, lookAhead, context, ct));

        //             if(IsTerminal(result))
        //             {
        //                 return false;
        //             }

        //             newPos = lookAhead.Pos;

        //             token = result.AddMessages(NextToken(lookAhead, context, ct));
        //         }

        //         // [dho] only mutate the original lexer
        //         // position if we were successful in skipping - 03/02/19
        //         lexer.Pos = newPos;

        //         return true;
        //     }
        //     else
        //     {
        //         // [dho] we return false because the caller should only invoke
        //         // this method if there is a Modifier. The return value indicates
        //         // whether any modifiers were skipped, not the absence of any errors - 03/02/19
        //         return false;
        //     }
        // }

        private bool EatOrnamentation(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            var r = ParseOrnamentationComponents(token, lookAhead, context, ct);

            if (!HasErrors(r) && r.Value.HasEatenTokens)
            {
                // [dho] we only manipulate the passed in lexer
                // if we have successfully skipped meta, 
                // otherwise if we fail to do so, we want
                // the lexer's position not corrupted - 23/02/19
                lexer.Pos = lookAhead.Pos;

                return true;
            }

            return false;
        }

        private bool EatIdentifierOrPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            if (!HasErrors(ParseIdentifierOrPattern(token, lookAhead, context, ct)))
            {
                // [dho] we only manipulate the passed in lexer
                // if we have successfully skipped the identifier
                // or pattern, otherwise if we fail to do so, we want
                // the lexer's position not corrupted - 01/02/19
                lexer.Pos = lookAhead.Pos;

                return true;
            }
            else
            {
                return false;
            }
        }

        private Result<Node> ParseIdentifierOrPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `[` 
            //        ^  - 15/02/19
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                // [dho] eg. collection destructuring - 15/02/19
                return ParseArrayBindingPattern(token, lexer, context, ct);
            }
            // [dho] `{` 
            //        ^  - 15/02/19
            else if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // [dho] eg. entity destructuring - 15/02/19
                return ParseObjectBindingPattern(token, lexer, context, ct);
            }
            else
            {
                return ParseIdentifier(token, lexer, context, ct);
            }
        }

        public Result<Node> ParseIdentifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Identifier)
            {
                var result = new Result<Node>();

                var identifier = NodeFactory.Identifier(context.AST, CreateOrigin(token, lexer, context), Lexeme(token, lexer));

                result.Value = result.AddMessages(FinishNode(identifier, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateErrorResult<Node>(token, "Identifier expected", lexer, context);
            }
        }

        private Result<Node> ParseKnownSymbolRoleAsIdentifierOrPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `[` 
            //        ^  - 29/03/19
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                // [dho] eg. collection destructuring - 29/03/19
                return ParseArrayBindingPattern(token, lexer, context, ct);
            }
            // [dho] `{` 
            //        ^  - 29/03/19
            else if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // [dho] eg. entity destructuring - 29/03/19
                return ParseObjectBindingPattern(token, lexer, context, ct);
            }
            else
            {
                return ParseKnownSymbolRoleAsIdentifier(token, lexer, context, ct);
            }
        }

        // [dho] this will create an `Identifier` node for any known symbol, which
        // means lexemes that have a role that is not `SymbolRole.Identifier` (keywords)
        // are legal, when they would fail if we just used `ParseIdentifier` - 23/03/19
        public Result<Node> ParseKnownSymbolRoleAsIdentifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (IsKnownSymbolRole(token, lexer, context, ct))
            {
                var result = new Result<Node>();

                var identifier = NodeFactory.Identifier(context.AST, CreateOrigin(token, lexer, context), Lexeme(token, lexer));

                result.Value = result.AddMessages(FinishNode(identifier, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateErrorResult<Node>(token, "Identifier expected", lexer, context);
            }
        }

        public Result<Node> ParseArrayBindingPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `[a, b] = c` - 01/02/19

            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node[] members = default(Node[]);

            // [dho] `[` 
            //        ^   - 23/03/19
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                // [dho] guard against the case where it's an empty destructuring - 23/03/19
                if (!EatIfNext(SyntaxKind.CloseBracketToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var membersContext = ContextHelpers.CloneInKind(context, ContextKind.ArrayBindingElements);

                    // [dho] `[...` 
                    //         ^^^  - 23/03/19
                    members = result.AddMessages(
                        ParseCommaDelimitedList(ParseArrayBindingElement, token, lexer, membersContext, ct)
                    );

                    if (HasErrors(result)) return result;

                    // [dho] `[...]` 
                    //            ^   - 23/03/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBracketToken, lexer, context, ct)
                    );

                    if (HasErrors(result)) return result;
                }

                var range = new Range(startPos, lexer.Pos);

                var dest = NodeFactory.CollectionDestructuring(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(dest, members, SemanticRole.Member));

                result.Value = result.AddMessages(FinishNode(dest, lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'[' expected", lexer, context)
                );
            }

            return result;
        }

        private Result<Node> ParseArrayBindingElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.CommaToken)
            {
                var r = ParseOmittedExpression(token, lexer, context, ct);

                if (!HasErrors(r))
                {
                    // [dho] this fixes the issues where you have `[, foo]` and because 
                    // we have already taken the `,` token, the list parser will choke because
                    // it will not see a separator so we rewind the lexer after successfully dealing
                    // with the omitted expression - 30/03/19
                    lexer.Pos = token.StartPos;
                }

                return r;
            }

            var result = new Result<Node>();

            var isSpread = token.Kind == SyntaxKind.DotDotDotToken;

            var spreadStartPos = -1;

            if (isSpread)
            {
                spreadStartPos = token.StartPos;
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var startPos = token.StartPos;

            Node name = result.AddMessages(
                ParseIdentifierOrPattern(token, lexer, context, ct)
            );

            Node defaultValue = default(Node);

            // [dho] `... =` 
            //            ^  - 22/03/19
            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                defaultValue = result.AddMessages(
                    ParseNonParameterInitializer(token, lexer, context, ct)
                );
            }


            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var member = NodeFactory.DestructuredMember(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(member, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(member, defaultValue, SemanticRole.Default));

                if (isSpread)
                {
                    var spreadRange = new Range(spreadStartPos, lexer.Pos);

                    var subject = result.AddMessages(FinishNode(member, lexer, context, ct));

                    var spread = NodeFactory.SpreadDestructuring(
                        context.AST,
                        CreateOrigin(spreadRange, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(spread, subject, SemanticRole.Subject));

                    result.Value = result.AddMessages(FinishNode(spread, lexer, context, ct));
                }
                else
                {
                    result.Value = result.AddMessages(FinishNode(member, lexer, context, ct));
                }
            }

            return result;

        }

        public Result<Node> ParseObjectBindingPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `{a, b} = c` - 01/02/19

            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node[] members = default(Node[]);

            // [dho] `{` 
            //        ^   - 23/03/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // [dho] guard against the case where it's an empty destructuring - 23/03/19
                if (!EatIfNext(SyntaxKind.CloseBraceToken, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var membersContext = ContextHelpers.CloneInKind(context, ContextKind.ObjectBindingElements);

                    // [dho] `{...` 
                    //         ^^^  - 23/03/19
                    members = result.AddMessages(
                        ParseCommaDelimitedList(ParseObjectBindingElement, token, lexer, membersContext, ct)
                    );

                    if (HasErrors(result)) return result;

                    // [dho] `{...}` 
                    //            ^   - 23/03/19
                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );

                    if (HasErrors(result)) return result;
                }

                var range = new Range(startPos, lexer.Pos);

                var dest = NodeFactory.EntityDestructuring(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(dest, members, SemanticRole.Member));

                result.Value = result.AddMessages(FinishNode(dest, lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "'{' expected", lexer, context)
                );
            }

            return result;
        }

        private Result<Node> ParseObjectBindingElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            Result<Node> result = new Result<Node>();

            var isSpread = token.Kind == SyntaxKind.DotDotDotToken;

            var spreadStartPos = -1;

            if (isSpread)
            {
                spreadStartPos = token.StartPos;
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var startPos = token.StartPos;

            // [dho] `foo` 
            //        ^^^  - 22/03/19
            Node propertyName = result.AddMessages(
                ParsePropertyName(token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }

            var skippedColon = EatIfNext(SyntaxKind.ColonToken, lexer, context, ct);

            Node name = default(Node);

            // [dho] `{ foo }` 
            //          ^^^     - 22/03/19
            // [dho] the property name *IS NOT* aliased to something - 22/03/19
            if (propertyName?.Kind == SemanticKind.Identifier && !skippedColon)
            {
                name = propertyName;
            }
            // [dho] `{ foo : ... }` 
            //                ^^^      - 22/03/19
            // [dho] the property name *IS* aliased to something - 22/03/19
            else if (skippedColon)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                Node to = result.AddMessages(
                    ParseIdentifierOrPattern(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var alias = NodeFactory.ReferenceAliasDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(alias, propertyName, SemanticRole.From));
                result.AddMessages(AddOutgoingEdges(alias, to, SemanticRole.Name));

                name = result.AddMessages(FinishNode(alias, lexer, context, ct));
            }

            Node defaultValue = default(Node);

            // [dho] `... =` 
            //            ^  - 22/03/19
            if (EatIfNext(SyntaxKind.EqualsToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                defaultValue = result.AddMessages(
                    ParseNonParameterInitializer(token, lexer, context, ct)
                );
            }


            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var member = NodeFactory.DestructuredMember(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(member, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(member, defaultValue, SemanticRole.Default));

                if (isSpread)
                {
                    var spreadRange = new Range(spreadStartPos, lexer.Pos);

                    var subject = result.AddMessages(
                        FinishNode(member, lexer, context, ct)
                    );

                    var spread = NodeFactory.SpreadDestructuring(
                        context.AST,
                        CreateOrigin(spreadRange, lexer, context)
                    );

                    result.AddMessages(AddOutgoingEdges(spread, subject, SemanticRole.Subject));

                    result.Value = result.AddMessages(FinishNode(spread, lexer, context, ct));
                }
                else
                {
                    result.Value = result.AddMessages(FinishNode(member, lexer, context, ct));
                }
            }

            return result;
        }

        private Result<Node> ParseStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if(token.Kind == SyntaxKind.Directive)
            {
                return ParseDirective(token, lexer, context, ct);
            }
            if (token.Kind == SyntaxKind.SemicolonToken)
            {
                return ParseEmptyStatement(token, lexer, context, ct);
            }
            else if (token.Kind == SyntaxKind.AtToken)
            {
                return ParseDeclaration(token, lexer, context, ct);
            }
            else if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                // // [dho] original source did not seem to have any provision for object literals
                // // at file scope level, which is seemingly legal - 29/03/19
                // return ParseObjectLiteralExpressionOrBlock(token, lexer, context, ct);
                return ParseBlock(token, lexer, context, ct); // [dho] reverted back to just block because I'm not sure object literals are legal! - 20/07/19
            }
            else
            {
                var role = GetSymbolRole(token, lexer, context, ct);

                switch (role)
                {
                    case SymbolRole.ForKeysLoop:
                    case SymbolRole.ForMembersLoop:
                    case SymbolRole.ForPredicateLoop:
                        return ParseForLoop(token, lexer, context, ct);

                    case SymbolRole.Function:
                        return ParseFunctionDeclaration(token, lexer, context, ct);

                    case SymbolRole.ObjectType:
                        return ParseClassDeclaration(token, lexer, context, ct);

                    case SymbolRole.PredicateJunction:
                        return ParseIfStatement(token, lexer, context, ct);

                    case SymbolRole.DoWhilePredicateLoop:
                        return ParseDoStatement(token, lexer, context, ct);

                    case SymbolRole.WhilePredicateLoop:
                        return ParseWhileStatement(token, lexer, context, ct);

                    case SymbolRole.JumpToNextIteration:
                        return ParseContinueKeyword(token, lexer, context, ct);

                    case SymbolRole.ClauseBreak:
                    case SymbolRole.LoopBreak:
                        return ParseBreakKeyword(token, lexer, context, ct);

                    case SymbolRole.FunctionTermination:
                        return ParseReturnStatement(token, lexer, context, ct);

                    case SymbolRole.PrioritySymbolResolutionContext:
                        return ParseWithStatement(token, lexer, context, ct);

                    case SymbolRole.MatchJunction:
                        return ParseSwitchStatement(token, lexer, context, ct);

                    case SymbolRole.RaiseError:
                        return ParseThrowStatement(token, lexer, context, ct);

                    case SymbolRole.ErrorTrapJunction:
                        return ParseTryStatement(token, lexer, context, ct);

                    case SymbolRole.Breakpoint:
                        return ParseDebuggerStatement(token, lexer, context, ct);

                    // case SyntaxKind.DeclareKeyword:
                    // case SyntaxKind.ConstKeyword:
                    case SymbolRole.CompilerHint:
                    case SymbolRole.Constant:
                    case SymbolRole.Enumeration:
                    case SymbolRole.Export:
                    case SymbolRole.GlobalContextReference:
                    case SymbolRole.Import:
                    case SymbolRole.Interface:
                    case SymbolRole.Modifier:
                    case SymbolRole.Namespace:
                    case SymbolRole.TypeAlias:
                    case SymbolRole.Variable:
                        if (IsStartOfDeclaration(token, lexer, context, ct))
                        {
                            return ParseDeclaration(token, lexer, context, ct);
                        }
                        break;
                }

                return ParseExpressionOrLabeledStatement(token, lexer, context, ct);
            }
        }

        // private Result<Node> ParseObjectLiteralExpressionOrBlock(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var lookAhead = lexer.Clone();

        //     if(context.CurrentKind == ContextKind.SourceElements)
        //     {
        //         var r = ParseObjectLiteralExpression(token, lookAhead, context, ct);

        //         if (!HasErrors(r))
        //         {
        //             lexer.Pos = lookAhead.Pos;

        //             return r;
        //         }
        //     }

        //     return ParseBlock(token, lexer, context, ct);
        // }

        private Result<(SymbolRole, bool)> LookAheadToSymbolRoleAfterOrnaments(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<(SymbolRole, bool)>();

            // [dho] look ahead to the symbol role after any ornaments - 21/02/19 
            SymbolRole role = SymbolRole.None;

            var lookAhead = lexer.Clone();

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lookAhead, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 21/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lookAhead, context, ct));

                if (!HasErrors(result))
                {
                    role = GetSymbolRole(token, lookAhead, context, ct);
                }
            }
            else
            {
                role = GetSymbolRole(token, lookAhead, context, ct);
            }

            result.Value = (role, hasEatenTokens);

            return result;
        }

        private Result<Node> ParseDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var (role, didFindOrnaments) = result.AddMessages(
                LookAheadToSymbolRoleAfterOrnaments(token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }

            switch (role)
            {
                case SymbolRole.CompilerHint:
                    return ParseDeclareStatement(token, lexer, context, ct);

                case SymbolRole.Constant:
                    {
                        // [dho] `const enum` 
                        //              ^^^^   - 29/03/19
                        if (LookAhead(IsEnumeration, lexer, context, ct).Item1)
                        {
                            return ParseEnumDeclaration(token, lexer, context, ct);
                        }
                        else
                        {
                            return ParseVariableStatement(token, lexer, context, ct);
                        }
                    }

                case SymbolRole.Function:
                    return ParseFunctionDeclaration(token, lexer, context, ct);

                case SymbolRole.ObjectType:
                    return ParseClassDeclaration(token, lexer, context, ct);

                case SymbolRole.Interface:
                    return ParseInterfaceDeclaration(token, lexer, context, ct);

                case SymbolRole.TypeAlias:
                    return ParseTypeAliasDeclaration(token, lexer, context, ct);

                case SymbolRole.Enumeration:
                    return ParseEnumDeclaration(token, lexer, context, ct);

                // [dho] original source grouped parsing `global` with parsing 
                // `namespace` and `module` declarations - 29/03/19
                case SymbolRole.GlobalContextReference:
                    return ParseGlobalDeclaration(token, lexer, context, ct);

                case SymbolRole.Import:
                    return ParseImportDeclarationOrImportEqualsDeclaration(token, lexer, context, ct);

                case SymbolRole.Export:
                    {
                        var lookAhead = lexer.Clone();
                        var r = NextToken(lookAhead, context, ct);

                        if (!HasErrors(r))
                        {
                            var tokenAhead = r.Value;

                            if (tokenAhead.Kind == SyntaxKind.EqualsToken)
                            {
                                return ParseExportAssignment(token, lexer, context, ct);
                            }

                            switch (GetSymbolRole(r.Value, lookAhead, context, ct))
                            {
                                case SymbolRole.Default:
                                    return ParseExportAssignment(token, lexer, context, ct);

                                case SymbolRole.ReferenceAlias:
                                    return ParseNamespaceExportDeclaration(token, lexer, context, ct);

                                default:
                                    return ParseExportDeclaration(token, lexer, context, ct);
                            }
                        }
                    }
                    break;

                case SymbolRole.Namespace:
                    return ParseNamespaceDeclaration(token, lexer, context, ct);

                case SymbolRole.Variable:
                    return ParseVariableStatement(token, lexer, context, ct);

            }

            result.AddMessages(
                CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
            );

            return result;
        }

        public Result<Node> ParseNamespaceExportDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] `export as` 
            //               ^^      - 29/03/19
            result.AddMessages(
                EatIfNextOrError(SymbolRole.ReferenceAlias, lexer, context, ct)
            );

            // [dho] `export as namespace` 
            //                  ^^^^^^^^^      - 29/03/19
            result.AddMessages(
                EatIfNextOrError(SymbolRole.Namespace, lexer, context, ct)
            );

            token = result.AddMessages(
                NextToken(lexer, context, ct)
            );

            Node clauses = default(Node);

            {
                var nsRefStartPos = lexer.Pos;

                // [dho] `export as namespace ...` 
                //                            ^^^      - 29/03/19
                Node name = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                var range = new Range(nsRefStartPos, lexer.Pos);

                var nsRef = NodeFactory.NamespaceReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(nsRef, name, SemanticRole.Name));

                clauses = result.AddMessages(FinishNode(nsRef, lexer, context, ct));
            }

            // [dho] `export as namespace ...;` 
            //                               ^      - 29/03/19
            result.AddMessages(
                EatSemicolon(lexer, context, ct)
            );

            if (HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.ExportDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, clauses, SemanticRole.Clause));
                result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        public Result<Node> ParseExportAssignment(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] `export =` 
            //               ^   - 29/03/19
            // [dho] `export default` 
            //               ^   - 29/03/19
            var (isNext, lookAhead) = LookAhead(IsEqualsOrDefault, lexer, context, ct);

            if (isNext)
            {
                // [dho] FIXUP this range may be a little wider than necessary if there is whitespace etc. - 29/03/19
                var defaultExportRange = new Range(lexer.Pos, lookAhead.Pos);

                lexer.Pos = lookAhead.Pos;

                Node defaultExport = result.AddMessages(FinishNode(
                    NodeFactory.DefaultExportReference(context.AST, CreateOrigin(defaultExportRange, lexer, context)),
                    lexer, context, ct
                ));

                token = result.AddMessages(NextToken(lexer, context, ct));

                // [dho] `export = ...` 
                //                 ^^^   - 29/03/19
                Node expression = result.AddMessages(ParseAssignmentExpressionOrHigher(token, lexer, context, ct));

                result.AddMessages(EatSemicolon(lexer, context, ct));

                if (!HasErrors(result))
                {
                    var referenceAliasRange = new Range(token.StartPos, lexer.Pos);

                    var referenceAliasDecl = NodeFactory.ReferenceAliasDeclaration(
                        context.AST,
                        CreateOrigin(referenceAliasRange, lexer, context)                    
                    );

                    result.AddMessages(AddOutgoingEdges(referenceAliasDecl, defaultExport, SemanticRole.From));
                    result.AddMessages(AddOutgoingEdges(referenceAliasDecl, expression, SemanticRole.Name));

                    // [dho] my eyes... - 29/03/19
                    Node clauses = result.AddMessages(FinishNode(
                        referenceAliasDecl,
                        lexer, context, ct
                    ));

                    var range = new Range(startPos, lexer.Pos);

                    var decl = NodeFactory.ExportDeclaration(context.AST, CreateOrigin(range, lexer, context));

                    result.AddMessages(AddOutgoingEdges(decl, clauses, SemanticRole.Clause));
                    result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                    result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                    result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
                }
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenAheadResult(lexer, context, ct)
                );
            }

            return result;
        }

        public Result<Node> ParseExportDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var role = GetSymbolRole(token, lexer, context, ct);

            if (role != SymbolRole.Export)
            {
                result.AddMessages(CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context));
            }

            token = result.AddMessages(NextToken(lexer, context, ct));

            if (HasErrors(result)) return result;

            // [dho] NOTE removing parsing ornamentation because this should be attached to the declaration
            // being exported, not eating by the export itself - 23/09/19
            // var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
            //     ParseOrnamentationComponents(token, lexer, context, ct)
            // );

            // if (hasEatenTokens)
            // {
            //     token = result.AddMessages(NextToken(lexer, context, ct));
            // }

            var clauses = default(Node[]);
            Node specifier = default(Node);

            // [dho] `export *` 
            //               ^   - 28/03/19
            if (token.Kind == SyntaxKind.AsteriskToken)
            {
                var range = new Range(lexer.Pos - Lexeme(token, lexer).Length, lexer.Pos);

                var wildcard = NodeFactory.WildcardExportReference(context.AST, CreateOrigin(range, lexer, context));

                clauses = new [] { result.AddMessages(FinishNode(wildcard, lexer, context, ct)) };

                // [dho] `export * from` 
                //                 ^^^^   - 28/03/19
                result.AddMessages(EatIfNextOrError(SymbolRole.PackageReference, lexer, context, ct));

                if (HasErrors(result)) return result;

                token = result.AddMessages(NextToken(lexer, context, ct));

                specifier = result.AddMessages(ParseModuleSpecifier(token, lexer, context, ct));
            }
            // [dho] `export { ... }` 
            //               ^          - 29/03/19
            else if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var clauseContext = ContextHelpers.CloneInKind(context, ContextKind.ImportOrExportSpecifiers);

                clauses = result.AddMessages(
                    ParseBracketedCommaDelimitedList(ParseExportSpecifier, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken, token, lexer, clauseContext, ct)
                );

                // [dho] `export { ... } from` 
                //                       ^^^^   - 29/03/19
                if (EatIfNext(SymbolRole.PackageReference, lexer, context, ct))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    specifier = result.AddMessages(ParseModuleSpecifier(token, lexer, context, ct));
                }
            }
            else
            {
                // [dho] Original source treated `export` as a modifier on a declaration,
                // but this is a bit annoying because it means statements like `export = foo` and 
                // `export { x, y }` are treated differently to `export class` or `export function` etc.
                // and I like the idea of it being consistent, but time will tell whether this is a good strategy - 29/03/19
                clauses = new [] {
                    result.AddMessages(
                        // [dho] NOTE not using the `clauseContext` because the original source treated this as a normal
                        // declaration, so we will parse it the same way - 29/03/19
                        ParseDeclaration(token, lexer, context, ct)
                    )
                };
            }

            result.AddMessages(EatSemicolon(lexer, context, ct));

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var decl = NodeFactory.ExportDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(decl, clauses, SemanticRole.Clause));
                result.AddMessages(AddOutgoingEdges(decl, specifier, SemanticRole.Specifier));
                // [dho] NOTE removing parsing ornamentation because this should be attached to the declaration
                // being exported, not eating by the export itself - 23/09/19
                // result.AddMessages(AddOutgoingEdges(decl, annotations, SemanticRole.Annotation));
                // result.AddMessages(AddOutgoingEdges(decl, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(decl, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseExportSpecifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node name = result.AddMessages(ParseIdentifier(token, lexer, context, ct));

            if (HasErrors(result)) return result;

            // [dho] `Foo as` 
            //            ^^     - 28/03/19
            if (EatIfNext(SymbolRole.ReferenceAlias, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                Node to = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                var range = new Range(startPos, lexer.Pos);

                var alias = NodeFactory.ReferenceAliasDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(alias, name, SemanticRole.From));
                result.AddMessages(AddOutgoingEdges(alias, to, SemanticRole.Name));

                result.Value = result.AddMessages(FinishNode(alias, lexer, context, ct));
            }
            else
            {
                result.Value = name;
            }

            return result;
        }




        // [dho] original source did not have this function because it treated `declare`
        // as a modifier, but we want to distinguish use of `declare` semantically as a compiler hint - 27/03/19 
        public Result<Node> ParseDeclareStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.CompilerHint)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                Node decl = result.AddMessages(ParseDeclaration(token, lexer, context, ct));

                if (!HasErrors(result))
                {
                    var range = new Range(startPos, lexer.Pos);

                    var compilerHint = NodeFactory.CompilerHint(context.AST, CreateOrigin(range, lexer, context));

                    result.AddMessages(AddOutgoingEdges(compilerHint, decl, SemanticRole.Subject));

                    result.Value = result.AddMessages(FinishNode(compilerHint, lexer, context, ct));
                }

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseModuleSpecifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.StringLiteral)
            {
                return ParseStringLiteral(token, lexer, context, ct);
            }
            else
            {
                return ParseExpression(token, lexer, context, ct);
            }
        }

        public Result<Node> ParseWhileStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var exp = result.AddMessages(
                ParseParenthesizedExpressionAfterSymbolRole(SymbolRole.WhilePredicateLoop, token, lexer, context, ct)
            );


            if (HasErrors(result))
            {
                return result;
            }


            // [dho] get next token after close paren - 29/01/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `while(...) ...` 
            //                   ^^^  - 29/01/19
            var body = result.AddMessages(
                ParseStatement(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var loop = NodeFactory.WhilePredicateLoop(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(loop, exp, SemanticRole.Condition));
            result.AddMessages(AddOutgoingEdges(loop, body, SemanticRole.Body));

            result.Value = result.AddMessages(FinishNode(loop, lexer, context, ct));

            return result;
        }


        public Result<Node> ParseSwitchStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var subject = result.AddMessages(
                ParseParenthesizedExpressionAfterSymbolRole(SymbolRole.MatchJunction, token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }


            // [dho] get next token after close paren - 29/01/19
            token = result.AddMessages(NextToken(lexer, context, ct));


            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            if (HasErrors(result))
            {
                return result;
            }


            var caseContext = ContextHelpers.CloneInKind(context, ContextKind.SwitchClauses);

            var clauses = result.AddMessages(
                ParseList(ParseCaseOrDefaultClause, token, lexer, caseContext, ct)
            );

            token = result.AddMessages(NextToken(lexer, context, ct));


            if (token.Kind != SyntaxKind.CloseBraceToken)
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }


            if (HasErrors(result))
            {
                return result;
            }


            var range = new Range(startPos, lexer.Pos);

            var multi = NodeFactory.MatchJunction(context.AST, CreateOrigin(range, lexer, context));

            result.AddMessages(AddOutgoingEdges(multi, subject, SemanticRole.Subject));
            result.AddMessages(AddOutgoingEdges(multi, clauses, SemanticRole.Clause));

            result.Value = result.AddMessages(FinishNode(multi, lexer, context, ct));

            return result;

        }

        private Result<Node> ParseCaseOrDefaultClause(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.MatchClause)
            {
                return ParseCaseClause(token, lexer, context, ct);
            }
            else if (role == SymbolRole.Default)
            {
                return ParseDefaultMatchClause(token, lexer, context, ct);
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }




        public Result<Node> ParseForLoop(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `for ...` 
            //        ^^^      - 30/01/19
            // [dho] NOTE all of these cases are synonymous for the `for` Symbol Role when it comes to TS.
            // We resolve the ambiguity below to determine the actual type of for loop we are parsing - 26/02/19
            if (role == SymbolRole.ForPredicateLoop || role == SymbolRole.ForMembersLoop || role == SymbolRole.ForKeysLoop)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }

            // [dho] `for await(...) ...` 
            //            ^^^^^           - 30/01/19
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.InterimSuspension)
            {
                result.AddMessages(
                    CreateErrorResult<Node>(token, "asynchronous loop subjects not supported", lexer, context)
                );

                // [dho] carry on parsing the loop, now the error has been recorded - 23/02/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            // [dho] `for(` 
            //           ^   - 30/01/19
            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }

            var initializerOrHandle = result.AddMessages(
                ParseForInitializer(token, lexer, context, ct)
            );

            var lookAhead = lexer.Clone();
            var tokenAhead = result.AddMessages(NextToken(lookAhead, context, ct));

            if (HasErrors(result)) return result;

            var roleAhead = GetSymbolRole(tokenAhead, lookAhead, context, ct);

            if (roleAhead == SymbolRole.KeyIn)
            {
                result.Value = result.AddMessages(
                    ParseForKeysLoopRest(startPos, initializerOrHandle, lexer, context, ct)
                );
            }
            else if (roleAhead == SymbolRole.MemberOf)
            {
                result.Value = result.AddMessages(
                    ParseForMembersLoopRest(startPos, initializerOrHandle, lexer, context, ct)
                );
            }
            else
            {
                result.Value = result.AddMessages(
                    ParseForPredicateLoopRest(startPos, initializerOrHandle, lexer, context, ct)
                );
            }


            return result;
        }



        private Result<Node> ParseForInitializer(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            if (token.Kind != SyntaxKind.SemicolonToken)
            {
                var role = GetSymbolRole(token, lexer, context, ct);

                var expContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 17/02/19
                expContext.Flags &= ~ContextFlags.DisallowInContext;

                switch (role)
                {
                    case SymbolRole.Constant:
                    case SymbolRole.Variable:
                        {
                            result.Value = result.AddMessages(
                                ParseVariableDeclarationList(token, lexer, expContext, ct)
                            );
                        }
                        break;

                    default:
                        {
                            result.Value = result.AddMessages(
                                ParseExpression(token, lexer, expContext, ct)
                            );
                        }
                        break;
                }
            }

            return result;
        }

        private Result<Node> ParseForKeysLoopRest(int startPos, Node handle, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] `for (... in ...` 
            //                 ^^       - 25/03/19
            result.AddMessages(EatIfNextOrError(SymbolRole.KeyIn, lexer, context, ct));

            if (HasErrors(result)) return result;

            var token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `for (... in ...` 
            //                    ^^^   - 30/01/19
            var subjectContext = ContextHelpers.Clone(context);

            // [dho] allow `in expressions` when parsing the expression - 29/01/19
            subjectContext.Flags &= ~ContextFlags.DisallowInContext;

            Node subject = result.AddMessages(
                ParseExpression(token, lexer, subjectContext, ct)
            );

            // [dho] advance to next token after expression - 30/01/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `for(... in ...)` 
            //                      ^   - 30/01/19
            if (token.Kind == SyntaxKind.CloseParenToken)
            {
                // [dho] eat the ')' - 30/01/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }


            Node body = result.AddMessages(
                ParseStatement(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var loop = NodeFactory.ForKeysLoop(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(loop, handle, SemanticRole.Handle));
            result.AddMessages(AddOutgoingEdges(loop, subject, SemanticRole.Subject));
            result.AddMessages(AddOutgoingEdges(loop, body, SemanticRole.Body));

            result.Value = result.AddMessages(FinishNode(loop, lexer, context, ct));

            return result;
        }

        private Result<Node> ParseForPredicateLoopRest(int startPos, Node initializer, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            Node condition = default(Node);
            Node iterator = default(Node);

            // [dho] `for(...;` 
            //               ^        - 30/01/19
            result.AddMessages(EatIfNextOrError(SyntaxKind.SemicolonToken, lexer, context, ct));

            if (HasErrors(result)) return result;

            var token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `for(...; ...` 
            //                 ^^^       - 30/01/19
            if (!IsSemicolonOrCloseParen(token, lexer, context, ct))
            {
                var conditionContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 29/01/19
                conditionContext.Flags &= ~ContextFlags.DisallowInContext;

                condition = result.AddMessages(
                    ParseExpression(token, lexer, conditionContext, ct)
                );

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;
            }

            // [dho] `for(...; ...;` 
            //                    ^    - 30/01/19
            if (token.Kind == SyntaxKind.SemicolonToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }


            if (HasErrors(result)) return result;

            // [dho] `for(...; ...; ...` 
            //                      ^^^   - 30/01/19
            if (token.Kind != SyntaxKind.CloseParenToken)
            {
                var iteratorContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 29/01/19
                iteratorContext.Flags &= ~ContextFlags.DisallowInContext;

                iterator = result.AddMessages(
                    ParseExpression(token, lexer, iteratorContext, ct)
                );

                // [dho] advance to next token - 30/01/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }


            // [dho] `for(...; ...; ...)` 
            //                         ^   - 30/01/19
            if (token.Kind == SyntaxKind.CloseParenToken)
            {
                // [dho] advance to next token - 30/01/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }


            Node body = result.AddMessages(
                ParseStatement(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var loop = NodeFactory.ForPredicateLoop(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(loop, initializer, SemanticRole.Initializer));
            result.AddMessages(AddOutgoingEdges(loop, condition, SemanticRole.Condition));
            result.AddMessages(AddOutgoingEdges(loop, iterator, SemanticRole.Iterator));
            result.AddMessages(AddOutgoingEdges(loop, body, SemanticRole.Body));

            result.Value = result.AddMessages(FinishNode(loop, lexer, context, ct));

            return result;
        }

        private Result<Node> ParseForMembersLoopRest(int startPos, Node handle, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] `for (... in ...` 
            //                 ^^       - 25/03/19
            result.AddMessages(EatIfNextOrError(SymbolRole.MemberOf, lexer, context, ct));

            if (HasErrors(result)) return result;

            var token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `for (... of ...` 
            //                    ^^^   - 30/01/19
            var subjectContext = ContextHelpers.Clone(context);

            // [dho] allow `in expressions` when parsing the expression - 29/01/19
            subjectContext.Flags &= ~ContextFlags.DisallowInContext;

            var subject = result.AddMessages(
                ParseAssignmentExpressionOrHigher(token, lexer, subjectContext, ct)
            );

            // [dho] advance to next token after expression - 30/01/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `for(... of ...)` 
            //                      ^   - 30/01/19
            //
            if (token.Kind == SyntaxKind.CloseParenToken)
            {
                // [dho] advance to next token - 30/01/19
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );
            }

            Node body = result.AddMessages(
                ParseStatement(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var loop = NodeFactory.ForMembersLoop(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(loop, handle, SemanticRole.Handle));
            result.AddMessages(AddOutgoingEdges(loop, subject, SemanticRole.Subject));

            result.Value = result.AddMessages(FinishNode(loop, lexer, context, ct));

            return result;
        }



        private Result<Node> ParseVariableDeclarationList(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] we are currently treating variable declaration lists, eg `let x = 2, y = 3, z;`
            // as a group of separate data value declarations with equivalent meta.
            //
            // This means the above example will actually be treated as if it were written:
            // ```
            // let x = 2;
            // let y = 3;
            // let z;
            // ```
            //
            // Hence the following code is pretty ugly in order to untangle the declarations - 22/02/19

            var result = new Result<Node>();

            // [dho] we need to remember the original start position because we will reparse
            // the meta N times, as a (less than ideal) way to clone the meta
            // so that we can apply it separately to each of the N declarations - 22/02/19
            var startPos = token.StartPos;
            var startToken = token;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            // [dho] we will use this flag to tell us below whether we need
            // to rebother reparsing the meta for each of the N declarations - 22/02/19
            var didFindOrnaments = annotations.Length > 0 || modifiers.Length > 0;

            // [dho] we have a singular type for all data value declarations, regardless
            // of whether they were defined with `let`, `var` or `const`. The way we distinguish
            // between those different meanings is by setting an `OrnamentRole` indicating the semantics - 22/02/19
            MetaFlag storageRole = default(MetaFlag);
            Range storageRolePos = default(Range);

            var role = GetSymbolRole(token, lexer, context, ct);

            var dvContext = ContextHelpers.CloneInKind(context, ContextKind.DataValueDeclarations);

            if (role == SymbolRole.Constant || role == SymbolRole.Variable)
            {
                storageRolePos = new Range(token.StartPos, token.StartPos + Lexeme(token, lexer).Length);

                // [dho] here we set the flag indicating the semantic meaning of the 
                // 'storage lexeme', which we will attach to the meta for each
                // of the N declarations subsequently - 22/02/19
                switch (token.Lexeme)
                {
                    case VarLexeme:
                        storageRole = MetaFlag.FunctionScope;
                        break;

                    case LetLexeme:
                        storageRole = MetaFlag.BlockScope;
                        break;

                    case ConstLexeme:
                        storageRole = MetaFlag.Constant | MetaFlag.BlockScope;
                        break;

                    default:
                        {
                            result.AddMessages(
                                CreateErrorResult<Node>(token, $"Unsupported data value storage '{Lexeme(token, lexer)}'", lexer, dvContext)
                            );

                            // [dho] carry on parsing as if it were `var`, with the appropriate error recorded - 22/02/19
                            storageRole = MetaFlag.FunctionScope;
                        }
                        break;
                }

                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }

            var childIndex = 0;

            var decls = result.AddMessages(
                // [dho] now we will go through the constituent N declarations - 22/02/19
                ParseCommaDelimitedList((childToken, childLexer, childContext, childCT) =>
                {

                    Result<Node> r = new Result<Node>();

                    var childStartPos = childToken.StartPos;

                    // [dho] parse the following construct, `foo : Bar = baz()` - 22/02/19
                    var (name, type, initializer) = r.AddMessages(
                        ParseDataValueDeclarationComponents(childToken, childLexer, childContext, childCT)
                    );

                    // [dho] this is the ornament that will record the semantic meaning of the 'storage lexeme' - 22/02/19
                    var meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                        childContext.AST,
                        CreateOrigin(new Range(storageRolePos.Start, storageRolePos.End), childLexer, childContext),
                        storageRole
                    ), childLexer, childContext, ct));

                    // [dho] here we create the data value declaration for this child - 22/02/19
                    var dvDecl = result.AddMessages(FinishNode(NodeFactory.DataValueDeclaration(
                        childContext.AST,
                        CreateOrigin(new Range(childStartPos, childLexer.Pos), childLexer, childContext)
                    ), childLexer, childContext, ct));

                    result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, name, SemanticRole.Name));
                    result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, meta, SemanticRole.Meta));
                    result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, type, SemanticRole.Type));
                    result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, initializer, SemanticRole.Initializer));

                    // [dho] if we are dealing with the first child,
                    // just use the meta object we parsed originally - 22/02/19
                    if (childIndex == 0)
                    {
                        result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, annotations, SemanticRole.Annotation));
                        result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, modifiers, SemanticRole.Modifier));
                    }
                    // [dho] subsequent children - 22/02/19
                    else if (didFindOrnaments)
                    {
                        // [dho] if ornaments were provided (before the 'storage lexeme'), then we 
                        // will basically reparse them in order to clone them (TODO more performant approach!) - 22/02/19
                        var ornamentLexer = lexer.Clone();

                        ornamentLexer.Pos = startPos;

                        // [dho] NOTE we ignore any messages that are returned because we would have recorded them
                        // when parsing the ornaments originally above, and we do not want to duplicate them to the caller - 22/02/19
                        var ornamentationComponents = result.AddMessages(
                            ParseOrnamentationComponents(startToken, ornamentLexer, childContext, childCT)
                        );

                        result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, ornamentationComponents.Annotations, SemanticRole.Annotation));
                        result.AddMessages(AddOutgoingEdges(childContext.AST, dvDecl, ornamentationComponents.Modifiers, SemanticRole.Modifier));
                    }

                    ++childIndex;

                    r.Value = dvDecl;

                    return r;

                }, token, lexer, dvContext, ct)
            );

            result.Value = new LegacyOrderedGroupHack
            {
                Nodes = decls
            };

            // result.AddMessages(EatSemicolon(lexer, context, ct));

            return result;
        }

        
        // [dho] TODO CLEANUP HACK this is very unfortunate but the way we currently parse a variable statement
        // is to split up the constituent declarations, but most of these delegates expect to deal with a single Node
        // being returned, so we wrap them up in this ugly wrapper and then unpack them in `AddOutgoingEdges` - 13/05/19
        private class LegacyOrderedGroupHack : Node 
        {
            public LegacyOrderedGroupHack() : base(null, SemanticKind.DataValueDeclaration, null)
            {

            }

            public IEnumerable<Node> Nodes { get; set;  }
        }





        private Result<Node> ParseForOfExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var expContext = ContextHelpers.Clone(context);

            // [dho] allow `in expressions` when parsing the expression - 17/02/19
            expContext.Flags &= ~ContextFlags.DisallowInContext;

            return ParseAssignmentExpressionOrHigher(token, lexer, expContext, ct);
        }

        public Result<Node> ParseBreakKeyword(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.ClauseBreak)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var exp = result.AddMessages(
                    ParseOptionalExpressionAfterSymbolRole(SymbolRole.ClauseBreak, token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var clauseBreak = NodeFactory.ClauseBreak(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(clauseBreak, exp, SemanticRole.Label));

                result.Value = result.AddMessages(FinishNode(clauseBreak, lexer, context, ct));

                return result;
            }
            else if (role == SymbolRole.LoopBreak)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var exp = result.AddMessages(
                    ParseOptionalExpressionAfterSymbolRole(SymbolRole.LoopBreak, token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var loopBreak = NodeFactory.LoopBreak(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(loopBreak, exp, SemanticRole.Label));

                result.Value = result.AddMessages(FinishNode(loopBreak, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }


        // public Result<Node> ParseClauseTermination(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var role = GetSymbolRole(token, lexer, context, ct);

        //     if(role == SymbolRole.ClauseTermination)
        //     {
        //         var result = new Result<Node>();

        //         var range = new Range(token.StartPos, token.StartPos + token.Lexeme.Length);

        //         var termination =FinishNode NodeFactoryHelpers.CreateClauseTermination(context.AST, CreateOrigin(range, lexer, context));

        //         result.Value = termination;

        //         return result;
        //     }
        //     else
        //     {
        //         return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
        //     }
        // }

        // public Result<Node> ParseLoopTermination(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var role = GetSymbolRole(token, lexer, context, ct);

        //     if(role == SymbolRole.LoopTermination)
        //     {
        //         var result = new Result<Node>();

        //         var startPos = token.StartPos;

        //         var label = result.AddMessages(
        //             ParseOptionalExpressionAfterSymbolRole(SymbolRole.JumpToNextIteration, token, lexer, context, ct)
        //         );

        //         var range = new Range(startPos, lexer.Pos);

        //         var termination = FinishNode NodeFactoryHelpers.CreateLoopTermination(context.AST, CreateOrigin(range, lexer, context));

        //         termination.Label = label;

        //         result.Value = termination;

        //         return result;
        //     }
        //     else
        //     {
        //         return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
        //     }
        // }

        public Result<Node> ParseContinueKeyword(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.JumpToNextIteration)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var label = result.AddMessages(
                    ParseOptionalExpressionAfterSymbolRole(SymbolRole.JumpToNextIteration, token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var jump = NodeFactory.JumpToNextIteration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(jump, label, SemanticRole.Label));

                result.Value = result.AddMessages(FinishNode(jump, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseReturnStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.FunctionTermination)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var exp = result.AddMessages(
                    ParseOptionalExpressionAfterSymbolRole(SymbolRole.FunctionTermination, token, lexer, context, ct)
                );

                // [dho] unwrap `return (x)` to `return x` - 20/06/19
                if(exp?.Kind == SemanticKind.Association)
                {
                    exp = ASTNodeFactory.Association(context.AST, exp).Subject;
                }


                var range = new Range(startPos, lexer.Pos);

                var termination = NodeFactory.FunctionTermination(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(termination, exp, SemanticRole.Value));

                result.Value = result.AddMessages(FinishNode(termination, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseThrowStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.RaiseError)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var operand = result.AddMessages(
                    // [dho] `throw  ...` 
                    //        ^^^^^^^^^^   - 10/02/19
                    ParseOptionalExpressionAfterSymbolRole(SymbolRole.RaiseError, token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var raiseError = NodeFactory.RaiseError(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(raiseError, operand, SemanticRole.Operand));

                result.Value = result.AddMessages(FinishNode(raiseError, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        public Result<Node> ParseDeleteExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `delete  ...` 
            //        ^^^^^^        - 10/02/19
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.Destruction)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                var subject = result.AddMessages(
                    ParseSimpleUnaryExpression(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var destruction = NodeFactory.Destruction(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(destruction, subject, SemanticRole.Subject));

                result.Value = result.AddMessages(FinishNode(destruction, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        private Result<Node> ParseSimpleUnaryExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.TildeToken:
                case SyntaxKind.ExclamationToken:
                    return ParsePrefixUnaryExpression(token, lexer, context, ct);

                case SyntaxKind.LessThanToken:
                    return ParseTypeAssertion(token, lexer, context, ct);

                default:
                    {
                        var role = GetSymbolRole(token, lexer, context, ct);

                        switch (role)
                        {
                            case SymbolRole.Destruction:
                                return ParseDeleteExpression(token, lexer, context, ct);

                            case SymbolRole.TypeOf:
                                return ParseTypeOfExpression(token, lexer, context, ct);

                            case SymbolRole.EvalToVoid: // [dho] thats like `void 0` - 22/02/19
                                return ParseVoidExpression(token, lexer, context, ct);

                            case SymbolRole.InterimSuspension:
                                return ParseAwaitExpression(token, lexer, context, ct);

                            default:
                                return ParseUpdateExpression(token, lexer, context, ct);
                        }
                    }
            }
        }

        private Result<Node> ParsePrefixUnaryExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var @operator = token;

            token = result.AddMessages(NextToken(lexer, context, ct));

            var operand = result.AddMessages(
                ParseSimpleUnaryExpression(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var origin = CreateOrigin(range, lexer, context);

            var unary = default(ASTNode);

            switch (@operator.Kind)
            {
                // [dho] `+x` 
                //        ^     - 15/02/19
                case SyntaxKind.PlusToken:
                    {
                        unary = NodeFactory.Identity(
                            context.AST,
                            origin
                        );

                        result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                    }
                    break;

                // [dho] `-x` 
                //        ^     - 15/02/19
                case SyntaxKind.MinusToken:
                    {
                        unary = NodeFactory.ArithmeticNegation(
                            context.AST,
                            origin
                        );

                        result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                    }
                    break;


                // [dho] `~x` 
                //        ^     - 15/02/19
                case SyntaxKind.TildeToken:
                    {
                        unary = NodeFactory.BitwiseNegation(
                            context.AST,
                            origin
                        );

                        result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                    }
                    break;

                // [dho] `!x` 
                //        ^     - 15/02/19
                case SyntaxKind.ExclamationToken:
                    {
                        // [dho] am I going to come back to this in the future and think
                        // "well, that was overkill?" - 08/10/18 (ported 15/02/19)
                        if (operand?.Kind == SemanticKind.Association) // !(x in y)
                        {
                            var subject = ASTNodeFactory.Association(context.AST, operand).Subject;

                            if (subject.Kind == SemanticKind.MembershipTest)
                            {
                                var memTest = ASTNodeFactory.MembershipTest(context.AST, subject);

                                unary = NodeFactory.NonMembershipTest(
                                    context.AST,
                                    origin
                                );

                                result.AddMessages(AddOutgoingEdges(unary, memTest.Subject, SemanticRole.Subject));
                                result.AddMessages(AddOutgoingEdges(unary, memTest.Criteria, SemanticRole.Criteria));

                                break;
                            }
                        }

                        unary = NodeFactory.LogicalNegation(
                            context.AST,
                            origin
                        );

                        result.AddMessages(AddOutgoingEdges(unary, operand, SemanticRole.Operand));
                    }
                    break;

                default:
                    {
                        result.AddMessages(
                            CreateUnsupportedTokenResult<Node>(@operator, lexer, context)
                        );
                    }
                    break;
            }

            if (unary != null)
            {
                result.Value = result.AddMessages(FinishNode(unary, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseUnaryExpressionOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            if (IsUpdateExpression(token, lexer, context, ct))
            {
                var startPos = token.StartPos;

                var updateExp = result.AddMessages(
                    ParseUpdateExpression(token, lexer, context, ct)
                );

                if (HasErrors(result))
                {
                    return result;
                }

                if (LookAhead(IsAsteriskAsterisk, lexer, context, ct).Item1)
                {
                    var precedence = GetBinaryOperatorPrecedence(token, lexer, context, ct);

                    var binaryExp = result.AddMessages(
                        ParseBinaryExpressionRest(precedence, updateExp, startPos, lexer, context, ct)
                    );

                    result.Value = binaryExp;
                }
                else
                {
                    result.Value = updateExp;
                }
            }
            else
            {
                var unaryExp = result.AddMessages(
                    ParseSimpleUnaryExpression(token, lexer, context, ct)
                );


                if (LookAhead(IsAsteriskAsterisk, lexer, context, ct).Item1)
                {
                    if (unaryExp.Kind == SemanticKind.ForcedCast)
                    {
                        result.AddMessages(
                            CreateErrorResult<Node>(token, "A type assertion expression is not allowed in the left hand side of an exponentiation expression Consider enclosing the expression in parentheses", lexer, context)
                        );
                    }
                    else
                    {
                        result.AddMessages(
                            CreateErrorResult<Node>(token, "An unary expression with the 0 operator is not allowed in the left hand side of an exponentiation expression Consider enclosing the expression in parentheses", lexer, context)
                        );
                    }
                }
                else
                {
                    result.Value = unaryExp;
                }
            }


            return result;
        }


        public Result<Node> ParseTypeAssertion(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `<foo> bar` 
            //        ^          - 23/03/19
            if (token.Kind == SyntaxKind.LessThanToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                // [dho] `<foo> bar` 
                //         ^^^        - 23/03/19
                Node targetType = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                result.AddMessages(
                    // [dho] `<foo> bar` 
                    //            ^       - 23/03/19
                    EatIfNextOrError(SyntaxKind.GreaterThanToken, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result)) return result;

                // [dho] `<foo> bar` 
                //              ^^^       - 23/03/19
                Node subject = result.AddMessages(
                    ParseSimpleUnaryExpression(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                var range = new Range(startPos, lexer.Pos);

                var forcedCast = NodeFactory.ForcedCast(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(forcedCast, subject, SemanticRole.Subject));
                result.AddMessages(AddOutgoingEdges(forcedCast, targetType, SemanticRole.TargetType));

                result.Value = result.AddMessages(FinishNode(forcedCast, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        // public enum Tristate
        // {
        //     False,
        //     True,
        //     Unknown
        // }
        // Tristate IsParenthesizedArrowFunctionExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var role = GetSymbolRole(token, lexer, context, ct);
        //     var lookAhead = lexer.Clone();

        //     if(EatOrnamentation(token, lookAhead, context, ct))
        //     {
        //         var r = NextToken(lookAhead, context, ct);

        //         if(IsTerminal(r))
        //         {
        //             return Tristate.False;
        //         }

        //         token = r.Value;

        //         if(token.PrecedingLineBreak)
        //         {
        //             return Tristate.False;
        //         }
        //         else if(token.Kind != SyntaxKind.OpenParenToken && 
        //                 token.Kind != SyntaxKind.LessThanToken)
        //         {
        //             return Tristate.False;
        //         }
        //     }

        //     if(token.Kind == SyntaxKind.OpenParenToken)
        //     {
        //         var r = NextToken(lookAhead, context, ct);

        //         if(IsTerminal(r))
        //         {
        //             return Tristate.False;
        //         }

        //         token = r.Value;


        //         if(token.Kind == SyntaxKind.CloseParenToken)
        //         {
        //             r = NextToken(lookAhead, context, ct);

        //             if(IsTerminal(r))
        //             {
        //                 return Tristate.False;
        //             }

        //             token = r.Value;

        //             switch(token.Kind)
        //             {
        //                 case SyntaxKind.EqualsGreaterThanToken:
        //                 case SyntaxKind.ColonToken:
        //                 case SyntaxKind.OpenBraceToken:
        //                     return Tristate.True;

        //                 default:
        //                     return Tristate.False;
        //             }
        //         }
        //         else if(token.Kind == SyntaxKind.OpenBracketToken || 
        //                 token.Kind == SyntaxKind.OpenBraceToken)
        //         {
        //             return Tristate.Unknown;
        //         }
        //         else if(token.Kind == SyntaxKind.DotDotDotToken)
        //         {
        //             return Tristate.True;
        //         }
        //         else if(!IsIdentifier(token, lookAhead, context, ct))
        //         {
        //             return Tristate.False;
        //         }
        //         else
        //         {
        //             r = NextToken(lookAhead, context, ct);

        //             if(IsTerminal(r))
        //             {
        //                 return Tristate.False;
        //             }

        //             token = r.Value;

        //             if(token.Kind == SyntaxKind.ColonToken)
        //             {
        //                 return Tristate.True;
        //             }
        //             else
        //             {
        //                 // This *could* be a parenthesized arrow function.
        //                 // Return Unknown to let the caller know.
        //                 return Tristate.Unknown;
        //             }
        //         }
        //     }
        //     else
        //     {
        //         if(!IsIdentifier(token, lookAhead, context, ct))
        //         {
        //             return Tristate.False;
        //         }
        //         else
        //         {
        //             // This *could* be a parenthesized arrow function.
        //             // Return Unknown to let the caller know.
        //             return Tristate.Unknown;
        //         }
        //     }
        // }


        public Result<Node> ParseAssignmentExpressionOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.GeneratorValue)
            {
                return ParseYieldExpression(token, lexer, context, ct);
            }

            // [dho] let us see if we are dealing with a parenthesized lambda - 20/02/19
            {
                var lookAhead = lexer.Clone();

                // [dho] try to parse a lambda - 20/02/19
                var r = ParseArrowFunctionExpression(token, lookAhead, context, ct);

                if (!HasErrors(r))
                {
                    lexer.Pos = lookAhead.Pos;

                    return r;
                }
            }

            // // [dho] let us see if we are dealing with a simple lambda - 20/02/19
            // {
            //     var lookAhead = lexer.Clone();

            //     // [dho] try to parse a lambda - 20/02/19
            //     var r = ParseSimpleArrowFunctionExpression(token, lookAhead, context, ct);

            //     if(!IsTerminal(r))
            //     {
            //         lexer.Pos = lookAhead.Pos;

            //         return r;
            //     }
            // }


            var result = new Result<Node>();

            var startPos = token.StartPos;

            var expr = result.AddMessages(
                ParseBinaryExpressionOrHigher(/*precedence*/ 0, token, lexer, context, ct)
            );

            if (HasErrors(result))
            {
                return result;
            }


            if (IsLeftHandSideExpression(expr))
            {
                // [dho] now we have to painfully check whether it is an
                // assignment binary expression.. man I'm so tired - 20/02/19

                var lookAhead = lexer.Clone();

                var r = NextToken(lookAhead, context, ct);

                if (!HasErrors(r))
                {
                    // [dho] we could just try and parse the assignment operator and ignore
                    // if we fail, but that makes debugging so hard because this path is hit A LOT
                    // and makes detecting real errors difficult. So now using a boolean check instead - 30/03/19
                    if (IsAssignmentOperator(r.Value, lookAhead, context, ct))
                    {
                        var @operator = result.AddMessages(
                            ParseAssignmentOperator(r.Value, lookAhead, context, ct)
                        );

                        // [dho] the assignment operator we parsed may be wider than the original
                        // token, eg `>` to `>=` - 20/02/19
                        lexer.Pos = lookAhead.Pos;

                        result.AddMessages(r);

                        Node leftOperand = default(Node);

                        // [dho] adding this chunk here because otherwise we will end up with an assignment
                        // expression where the left hand side is a `DynamicTypeConstruction` (when it should therefore
                        // be an `EntityDestructuring`) or an `ArrayConstruction` (when it should therefore be a 
                        // `CollectionDestructuring`). This is a bit annoying having to reparse the same text again
                        // but otherwise we end up with something not semantically correct - 22/03/19
                        if (expr.Kind == SemanticKind.DynamicTypeConstruction)
                        {
                            var rewindLexer = lexer.Clone();
                            rewindLexer.Pos = startPos + Lexeme(token, lexer).Length;

                            leftOperand = result.AddMessages(
                                ParseObjectBindingPattern(token, rewindLexer, context, ct)
                            );
                        }
                        else if (expr.Kind == SemanticKind.ArrayConstruction)
                        {
                            var rewindLexer = lexer.Clone();
                            rewindLexer.Pos = startPos + Lexeme(token, lexer).Length;

                            leftOperand = result.AddMessages(
                                ParseArrayBindingPattern(token, rewindLexer, context, ct)
                            );
                        }
                        else
                        {
                            leftOperand = expr;
                        }

                        token = result.AddMessages(NextToken(lexer, context, ct));

                        var rightOperand = result.AddMessages(
                            ParseAssignmentExpressionOrHigher(token, lexer, context, ct)
                        );

                        var range = new Range(startPos, lexer.Pos);

                        result.Value = result.AddMessages(
                            MakeBinaryExpression(leftOperand, @operator, rightOperand, range, lexer, context, ct)
                        );

                        return result;
                    }
                }
            }

            // It wasn't an assignment or a lambda.  This is a conditional expression:
            result.Value = result.AddMessages(
                ParseConditionalExpressionRest(expr, startPos, lexer, context, ct)
            );

            return result;
        }


        private Result<Token> ParseAssignmentOperator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.GreaterThanToken)
            {
                var r = TokenUtils.ReScanGreaterThanToken(token, lexer);

                if (HasErrors(r))
                {
                    return r;
                }
                else
                {
                    token = r.Value;
                }
            }

            var result = new Result<Token>();

            switch (token.Kind)
            {
                case SyntaxKind.EqualsToken:
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.AsteriskEqualsToken:
                case SyntaxKind.AsteriskAsteriskEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.PercentEqualsToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.BarEqualsToken:
                case SyntaxKind.CaretEqualsToken:
                    {
                        // [dho] the assignment operator we parsed may be wider than the original
                        // token, eg `>` to `>=` - 20/02/19
                        // lexer.Pos = token.StartPos + Lexeme(token, lexer).Length;
                        result.Value = token;
                    }
                    break;

                default:
                    {
                        result.AddMessages(
                            CreateUnsupportedTokenResult<Node>(token, lexer, context)
                        );
                    }
                    break;
            }

            return result;
        }

        private bool IsAssignmentOperator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.GreaterThanToken)
            {
                var r = TokenUtils.ReScanGreaterThanToken(token, lexer.Clone());

                if (HasErrors(r))
                {
                    return false;
                }
                else
                {
                    token = r.Value;
                }
            }

            switch (token.Kind)
            {
                case SyntaxKind.EqualsToken:
                case SyntaxKind.PlusEqualsToken:
                case SyntaxKind.MinusEqualsToken:
                case SyntaxKind.AsteriskEqualsToken:
                case SyntaxKind.AsteriskAsteriskEqualsToken:
                case SyntaxKind.SlashEqualsToken:
                case SyntaxKind.PercentEqualsToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                case SyntaxKind.BarEqualsToken:
                case SyntaxKind.CaretEqualsToken:
                    return true;

                default:
                    return false;
            }
        }

        private Result<Node> ParseBinaryExpressionOrHigher(int precedence, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var leftOperand = result.AddMessages(
                ParseUnaryExpressionOrHigher(token, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                result.Value = result.AddMessages(
                    ParseBinaryExpressionRest(precedence, leftOperand, startPos, lexer, context, ct)
                );
            }

            return result;
        }

        private Result<Node> ParseYieldExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.GeneratorValue)
            {
                // YieldExpression[In] :
                //      yield
                //      yield [no LineTerminator here] [Lexical goal InputElementRegExp]AssignmentExpression[?In, Yield]
                //      yield [no LineTerminator here] * [Lexical goal InputElementRegExp]AssignmentExpression[?In, Yield]
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var lookAhead = lexer.Clone();

                // [dho] we use a lookahead because we may not eat this token - 23/03/19
                token = result.AddMessages(NextToken(lookAhead, context, ct));

                if (HasErrors(result))
                {
                    // [dho] eat the problem token so it isn't
                    // confusing when the caller examines the result - 23/03/19
                    lexer.Pos = lookAhead.Pos;
                    return result;
                }

                // [dho] NOTE not bothering with a group because the only ornament we
                // may apply at the moment is for delegate generators, eg `yield* foo` - 24/03/19
                Node meta = default(Node);

                Node expression = default(Node);

                // if the next token is not on the same line as yield.  or we don't have an '*' or
                // the start of an expression, then this is just a simple "yield" expression.
                if (!token.PrecedingLineBreak)
                {
                    // [dho] signifies that the yielded value is actually another
                    // generator, so all subsequent yield values will be taken from the
                    // delegate generator until it is exhausted, before returning to the yields
                    // in this scope - 24/03/19
                    // [dho] https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/yield* - 24/03/19
                    if (token.Kind == SyntaxKind.AsteriskToken)
                    {
                        lexer.Pos = lookAhead.Pos;

                        meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                            context.AST,
                            CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                            MetaFlag.Generator // [dho] the asterisk signifies generator - 24/03/19
                        ), lexer, context, ct));

                        token = result.AddMessages(NextToken(lookAhead, context, ct));

                        if (HasErrors(result)) return result;
                    }

                    if (IsStartOfExpression(token, lookAhead, context, ct))
                    {
                        lexer.Pos = lookAhead.Pos;

                        expression = result.AddMessages(
                            ParseAssignmentExpressionOrHigher(token, lexer, context, ct)
                        );

                        if (HasErrors(result)) return result;
                    }
                }

                var range = new Range(startPos, lexer.Pos);

                var suspension = NodeFactory.GeneratorSuspension(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(suspension, expression, SemanticRole.Value));
                result.AddMessages(AddOutgoingEdges(suspension, meta, SemanticRole.Meta));

                result.Value = result.AddMessages(FinishNode(suspension, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        private Result<Node> ParseArrowFunctionExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                ParseOrnamentationComponents(token, lexer, context, ct)
            );

            // [dho] we did find optional prefixes to eat - 13/02/19
            if (hasEatenTokens)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }

            var parameters = default(Node[]);

            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                // [dho] `() => ...` 
                //        ^            - 21/03/19
                parameters = result.AddMessages(
                    ParseParameters(token, lexer, context, ct)
                );
            }
            else
            {
               

                // [dho] `foo => ...` 
                //        ^^^          - 21/03/19
                parameters = new [] {
                    result.AddMessages(
                        ParseParameter(token, lexer, context, ct)
                    )
                };
            }

            if (HasErrors(result)) return result;

            Node type = default(Node);

            // [dho] `() : ... => ...` 
            //           ^               - 23/03/19
            if (EatIfNext(SyntaxKind.ColonToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                type = result.AddMessages(
                    // [dho] `() : ... => ...` 
                    //             ^^^           - 23/03/19
                    ParseType(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;
            }

            // [dho] `() => ...` 
            //           ^^        - 21/03/19
            result.AddMessages(
                EatIfNextOrError(SyntaxKind.EqualsGreaterThanToken, lexer, context, ct)
            );

            if (HasErrors(result)) return result;

            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `() => ...` 
            //              ^^^   - 21/03/19
            Node body = result.AddMessages(
                ParseArrowFunctionExpressionBody(token, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var lambda = NodeFactory.LambdaDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(lambda, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(lambda, type, SemanticRole.Type));
                result.AddMessages(AddOutgoingEdges(lambda, body, SemanticRole.Body));
                result.AddMessages(AddOutgoingEdges(lambda, annotations, SemanticRole.Annotation));
                result.AddMessages(AddOutgoingEdges(lambda, modifiers, SemanticRole.Modifier));

                result.Value = result.AddMessages(FinishNode(lambda, lexer, context, ct));
            }

            return result;
        }

        // private Result<Node> ParseSimpleArrowFunctionExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<Node>();

        //     var startPos = token.StartPos;

        //     OrderedGroup meta = PrepareOrderedGroup(token, lexer, context);

        //     var hasEatenTokens = result.AddMessages(
        //         ParseMeta(meta, token, lexer, context, ct)
        //     );

        //     // [dho] we did find optional prefixes to eat - 13/02/19
        //     if(hasEatenTokens)
        //     {
        //         token = result.AddMessages(NextToken(lexer, context, ct));
        //     }


        //     // async keyword possibly

        //     return CreateUnsupportedFeatureResult<Node>(token, "simple arrow function expressions", lexer, context);
        // }

        private Result<Node> ParseArrowFunctionExpressionBody(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                return ParseFunctionBlock(token, lexer, context, ct);
            }
            else
            {
                var role = GetSymbolRole(token, lexer, context, ct);

                if (token.Kind != SyntaxKind.SemicolonToken &&
                    role != SymbolRole.Function &&
                    role != SymbolRole.ObjectType &&
                    IsStartOfStatement(token, lexer, context, ct) &&
                    !IsStartOfExpressionStatement(token, lexer, context, ct))
                {
                    // Check if we got a plain statement (i.e. no expression-statements, no function/class expressions/declarations)
                    //
                    // Here we try to recover from a potential error situation in the case where the
                    // user meant to supply a block. For example, if the user wrote:
                    //
                    //  a =>
                    //      let v = 0;
                    //  }
                    //
                    // they may be missing an open brace.  Check to see if that's the case so we can
                    // try to recover better.  If we don't do this, then the next close curly we see may end
                    // up preemptively closing the containing construct.
                    var result = new Result<Node>();

                    result.AddMessages(
                        CreateErrorResult<Node>(token, "Expected '{'", lexer, context)
                    );

                    var contentContext = ContextHelpers.CloneInKind(context, ContextKind.BlockStatements);

                    var content = result.AddMessages(
                        ParseList(ParseStatement, token, lexer, contentContext, ct)
                    );

                    result.AddMessages(
                        EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct)
                    );

                    return result;
                }


                return ParseAssignmentExpressionOrHigher(token, lexer, context, ct);
            }
        }


        private Result<Node> ParseConditionalExpressionRest(Node leftOperand, int startPos, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] `... ?` 
            //            ^   - 17/03/19
            if (EatIfNext(IsQuestion, lexer, context, ct))
            {
                Node condition = leftOperand;
                Node trueValue = default(Node);
                Node falseValue = default(Node);

                var token = result.AddMessages(NextToken(lexer, context, ct));

                var expContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 17/03/19
                expContext.Flags &= ~ContextFlags.DisallowInContext;

                // [dho] `... ? ...` 
                //              ^^^   - 17/03/19
                trueValue = result.AddMessages(
                    ParseAssignmentExpressionOrHigher(token, lexer, expContext, ct)
                );

                // [dho] `... ? ... :` 
                //                  ^   - 17/03/19
                if (result.AddMessages(EatIfNextOrError(SyntaxKind.ColonToken, lexer, context, ct)))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    // [dho] `... ? ... : ...` 
                    //                    ^^^   - 17/03/19
                    falseValue = result.AddMessages(
                        // [dho] NOTE original source did not specify different context - 17/03/19
                        ParseAssignmentExpressionOrHigher(token, lexer, context, ct)
                    );
                }

                var range = new Range(startPos, lexer.Pos);

                var predicate = NodeFactory.PredicateFlat(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(predicate, condition, SemanticRole.Predicate));
                result.AddMessages(AddOutgoingEdges(predicate, trueValue, SemanticRole.TrueValue));
                result.AddMessages(AddOutgoingEdges(predicate, falseValue, SemanticRole.FalseValue));

                result.Value = result.AddMessages(FinishNode(predicate, lexer, context, ct));
            }
            else
            {
                result.Value = leftOperand;
            }

            return result;
        }

        bool IsLeftHandSideExpression(Node expr)
        {
            switch (expr.Kind)
            {
                case SemanticKind.ArrayConstruction:
                case SemanticKind.Association:
                case SemanticKind.BooleanConstant:
                case SemanticKind.DynamicTypeConstruction:
                case SemanticKind.FunctionDeclaration:
                case SemanticKind.Identifier:
                case SemanticKind.IndexedAccess:
                case SemanticKind.IncidentContextReference:
                case SemanticKind.Invocation:
                case SemanticKind.MetaProperty:
                case SemanticKind.NamedTypeConstruction:
                case SemanticKind.InterpolatedString:
                case SemanticKind.NotNull:
                case SemanticKind.Null:
                case SemanticKind.NumericConstant:
                case SemanticKind.ObjectTypeDeclaration:
                case SemanticKind.QualifiedAccess:
                case SemanticKind.RegularExpressionConstant:
                case SemanticKind.StringConstant:
                case SemanticKind.SuperContextReference:
                    return true;

                default:
                    return false;
            }
        }



        bool IsUpdateExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.TildeToken:
                case SyntaxKind.ExclamationToken:
                    return false;

                case SyntaxKind.LessThanToken:
                    return true; // [dho] JSX support - 13/06/19
                //     // if (SourceFile.LanguageVariant != LanguageVariant.Jsx)
                //     // {
                //     return false;
                // // }
                // // break;

                default:
                    {
                        switch (GetSymbolRole(token, lexer, context, ct))
                        {
                            case SymbolRole.Destruction:
                            case SymbolRole.TypeOf:
                            case SymbolRole.EvalToVoid:
                            case SymbolRole.InterimSuspension:
                                return false;
                        }

                        return true;
                    }
            }
        }



        private Result<Node> ParseTypeOrSmartCast(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `Foo is` 
            //            ^^   - 31/01/19
            if (LookAhead(IsSmartCast, lexer, context, ct).Item1)
            {
                return ParseSmartCast(token, lexer, context, ct);
            }
            else
            {
                return ParseType(token, lexer, context, ct);
            }
        }

        private Result<Node> ParseSmartCast(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node subject = default(Node);

            // [dho] `Foo` 
            //        ^^^   - 04/03/19
            if (role == SymbolRole.Identifier)
            {
                subject = result.AddMessages(
                    ParseIdentifier(token, lexer, context, ct)
                );
            }
            // [dho] `this` 
            //        ^^^^   - 04/03/19
            else if (role == SymbolRole.IncidentContextReference)
            {
                subject = result.AddMessages(
                    ParseIncidentContextReference(token, lexer, context, ct)
                );
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );

                return result;
            }


            Node type = default(Node);

            var isSmartCast = result.AddMessages(
                EatIfNextOrError(SymbolRole.SmartCast, lexer, context, ct)
            );

            if (isSmartCast)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                type = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );
            }

            var range = new Range(startPos, lexer.Pos);

            var smartCast = NodeFactory.SmartCast(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(smartCast, subject, SemanticRole.Subject));
            result.AddMessages(AddOutgoingEdges(smartCast, type, SemanticRole.TargetType));

            result.Value = result.AddMessages(FinishNode(smartCast, lexer, context, ct));


            return result;
        }

        private Result<Node> ParseType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var typeContext = ContextHelpers.Clone(context);
            typeContext.Flags &= ~ContextFlags.TypeExcludesFlags;

            if (IsStartOfFunctionType(token, lexer, typeContext, ct))
            {
                return ParseFunctionType(token, lexer, typeContext, ct);
            }
            // [dho] `new` keyword etc. - 15/02/19
            else if (GetSymbolRole(token, lexer, typeContext, ct) == SymbolRole.Construction)
            {
                return ParseConstructorType(token, lexer, typeContext, ct);
            }
            else
            {
                return ParseUnionTypeOrHigher(token, lexer, typeContext, ct);
            }
        }

        private Result<Node> ParseTypeReference(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node name = result.AddMessages(
                ParseEntityName(token, lexer, context, ct)
            );

            if (HasErrors(result)) return result;

            // [dho] `T extends { attributes: infer A }`
            //          ^^^^^^^                            - 23/03/19
            if (LookAhead(IsHeritageClause, lexer, context, ct).Item1)
            {
                var (constraints, hasEatenConstraintTokens) = result.AddMessages(
                    ParseTypeConstraintComponents(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                var range = new Range(startPos, lexer.Pos);

                var query = NodeFactory.NamedTypeQuery(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(query, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(query, constraints, SemanticRole.Constraint));

                result.Value = result.AddMessages(
                    ParseConditionalTypeReferenceRest(
                        result.AddMessages(FinishNode(query, lexer, context, ct)),
                        startPos,
                        lexer,
                        context,
                        ct
                    )
                );
            }
            else
            {
                var template = default(Node[]);

                if (LookAhead(IsLessThanOnSameLine, lexer, context, ct).Item1)
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var templateContext = ContextHelpers.CloneInKind(context, ContextKind.TypeArguments);

                    template = result.AddMessages(
                        ParseBracketedCommaDelimitedList(ParseType, SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken, token, lexer, templateContext, ct)
                    );

                    if (HasErrors(result)) return result;
                }

                var range = new Range(startPos, lexer.Pos);

                var typeRef = NodeFactory.NamedTypeReference(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(typeRef, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(typeRef, template, SemanticRole.Template));

                result.Value = result.AddMessages(FinishNode(typeRef, lexer, context, ct));
            }




            return result;
        }

        // [dho] https://www.typescriptlang.org/docs/handbook/release-notes/typescript-2-8.html
        private Result<Node> ParseConditionalTypeReferenceRest(Node typeRef, int startPos, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            // [dho] `... ?` 
            //            ^   - 23/03/19
            if (EatIfNext(IsQuestion, lexer, context, ct))
            {
                Node condition = typeRef;
                Node trueTypeRef = default(Node);
                Node falseTypeRef = default(Node);

                var token = result.AddMessages(NextToken(lexer, context, ct));

                // [dho] `... ? ...` 
                //              ^^^   - 23/03/19
                trueTypeRef = result.AddMessages(
                    ParseTypeReference(token, lexer, context, ct)
                );

                if (HasErrors(result)) return result;

                // [dho] `... ? ... :` 
                //                  ^   - 17/03/19
                if (result.AddMessages(EatIfNextOrError(SyntaxKind.ColonToken, lexer, context, ct)))
                {
                    token = result.AddMessages(NextToken(lexer, context, ct));

                    // [dho] `... ? ... : ...` 
                    //                    ^^^   - 17/03/19
                    falseTypeRef = result.AddMessages(
                        ParseTypeReference(token, lexer, context, ct)
                    );

                    if (HasErrors(result)) return result;
                }

                var range = new Range(startPos, lexer.Pos);

                var conditionalTypeRef = NodeFactory.ConditionalTypeReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(conditionalTypeRef, condition, SemanticRole.Predicate));
                result.AddMessages(AddOutgoingEdges(conditionalTypeRef, trueTypeRef, SemanticRole.TrueType));
                result.AddMessages(AddOutgoingEdges(conditionalTypeRef, falseTypeRef, SemanticRole.FalseType));

                result.Value = result.AddMessages(FinishNode(conditionalTypeRef, lexer, context, ct));
            }
            else
            {
                result.Value = typeRef;
            }

            return result;
        }

        private Result<Node> ParseEntityName(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node subject = result.AddMessages(
                // [dho] allow 'keywords' for names - 23/03/19
                /* ParseIdentifier */ParseKnownSymbolRoleAsIdentifier(token, lexer, context, ct)
            );

            while (LookAhead(IsDot, lexer, context, ct).Item1)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result))
                {
                    return result;
                }

                var member = result.AddMessages(
                    ParseRightSideOfDot(token, lexer, context, ct)
                );

                var incident = subject;

                var range = new Range(startPos, lexer.Pos);

                subject = result.AddMessages(FinishNode(NodeFactory.QualifiedAccess(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                ), lexer, context, ct));

                result.AddMessages(AddOutgoingEdges(context.AST, subject, incident, SemanticRole.Incident));
                result.AddMessages(AddOutgoingEdges(context.AST, subject, member, SemanticRole.Member));
            }

            result.Value = subject;

            return result;
        }

        private Result<Node> ParseParenthesizedType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result))
                {
                    return result;
                }

                var type = result.AddMessages(
                    ParseType(token, lexer, context, ct)
                );

                result.AddMessages(
                    EatIfNextOrError(SyntaxKind.CloseParenToken, lexer, context, ct)
                );

                if (!HasErrors(result))
                {
                    var range = new Range(startPos, lexer.Pos);

                    // var parenthesizedTypeRef = NodeFactory.ParenthesizedTypeReference(context.AST, CreateOrigin(range, lexer, context));

                    // result.AddMessages(AddOutgoingEdges(parenthesizedTypeRef, type, SemanticRole.Type));

                    // result.Value = result.AddMessages(FinishNode(parenthesizedTypeRef, lexer, context, ct));
                    
                    // [dho] TODO CHECK removing parenthesized type for now and just unwrapping the type
                    // we parsed from it.. is that OK in all cases?? - 19/07/19
                    result.Value = type;
                }

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        private Result<Node> ParseTupleType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                var typesContext = ContextHelpers.CloneInKind(context, ContextKind.TupleElementTypes);
                
                var types = result.AddMessages(
                    ParseBracketedCommaDelimitedList(ParseType, SyntaxKind.OpenBracketToken, SyntaxKind.CloseBracketToken, token, lexer, typesContext, ct)
                );

                if (types.Length == 1)
                {
                    result.Value = types[0];
                }
                else
                {
                    var range = new Range(startPos, lexer.Pos);

                    var tuple = NodeFactory.UnionTypeReference(context.AST, CreateOrigin(range, lexer, context));

                    result.AddMessages(AddOutgoingEdges(tuple, types, SemanticRole.Type));

                    result.Value = result.AddMessages(FinishNode(tuple, lexer, context, ct));
                }

                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        private Result<Node> ParseMappedType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `{ readonly [P in keyof T]: T[P]; }` 
            //        ^                                     - 24/03/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var result = new Result<Node>();

                token = result.AddMessages(NextToken(lexer, context, ct));

                var startPos = token.StartPos;

                // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                //          ^^^^^^^^                           - 24/03/19
                var (annotations, modifiers, hasEatenTokens) = result.AddMessages(
                    ParseOrnamentationComponents(token, lexer, context, ct)
                );

                // [dho] we did find optional prefixes to eat - 22/03/19
                if (hasEatenTokens)
                {
                    // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                    //                   ^                           - 24/03/19
                    result.AddMessages(EatIfNextOrError(SyntaxKind.OpenBracketToken, lexer, context, ct));
                }
                else if (token.Kind != SyntaxKind.OpenBracketToken)
                {
                    // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                    //                   ^                           - 24/03/19
                    result.AddMessages(CreateErrorResult<Node>(token, "Expected '['", lexer, context));
                }

                if (HasErrors(result)) return result;

                token = result.AddMessages(NextToken(lexer, context, ct));

                // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                //                    ^^^^^^^^^^^^            - 24/03/19
                Node typeParameter = result.AddMessages(
                    ParseMappedTypeParameter(token, lexer, context, ct)
                );

                // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                //                                ^            - 24/03/19
                if (result.AddMessages(EatIfNextOrError(SyntaxKind.CloseBracketToken, lexer, context, ct)))
                {
                    Node meta = default(Node);

                    // [dho] `{ readonly [P in keyof T]?: T[P]; }` 
                    //                                 ^            - 24/03/19
                    if (EatIfNext(IsQuestion, lexer, context, ct))
                    {
                        meta = result.AddMessages(FinishNode(NodeFactory.Meta(
                            context.AST,
                            CreateOrigin(new Range(lexer.Pos - 1, lexer.Pos), lexer, context),
                            MetaFlag.Optional // [dho] the question mark signifies optional - 24/03/19
                        ), lexer, context, ct));
                    }

                    Node type = default(Node);

                    // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                    //                                 ^                - 24/03/19
                    if (EatIfNext(IsColon, lexer, context, ct))
                    {
                        token = result.AddMessages(NextToken(lexer, context, ct));

                        // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                        //                                   ^^^^       - 24/03/19
                        type = result.AddMessages(
                            ParseType(token, lexer, context, ct)
                        );
                    }

                    // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                    //                                       ^           - 24/03/19
                    result.AddMessages(EatSemicolon(lexer, context, ct));

                    // [dho] `{ readonly [P in keyof T]: T[P]; }` 
                    //                                         ^         - 24/03/19
                    result.AddMessages(EatIfNextOrError(SyntaxKind.CloseBraceToken, lexer, context, ct));

                    if (!HasErrors(result))
                    {
                        var range = new Range(startPos, lexer.Pos);

                        var mappedType = NodeFactory.MappedTypeReference(context.AST, CreateOrigin(range, lexer, context));

                        result.AddMessages(AddOutgoingEdges(mappedType, typeParameter, SemanticRole.TypeParameter));
                        result.AddMessages(AddOutgoingEdges(mappedType, type, SemanticRole.Type));
                        result.AddMessages(AddOutgoingEdges(mappedType, annotations, SemanticRole.Annotation));
                        result.AddMessages(AddOutgoingEdges(mappedType, modifiers, SemanticRole.Modifier));
                        result.AddMessages(AddOutgoingEdges(mappedType, meta, SemanticRole.Meta));

                        result.Value = result.AddMessages(FinishNode(mappedType, lexer, context, ct));
                    }
                }


                return result;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }
        }

        private Result<Node> ParseMappedTypeParameter(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            // [dho] `P in keyof T` 
            //        ^              - 24/03/19
            Node name = result.AddMessages(ParseIdentifier(token, lexer, context, ct));

            token = result.AddMessages(NextToken(lexer, context, ct));

            // [dho] `P in keyof T` 
            //          ^^            - 24/03/19
            Node constraints = result.AddMessages(ParseMemberTypeConstraint(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var typeParameter = NodeFactory.TypeParameterDeclaration(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(typeParameter, name, SemanticRole.Name));
                result.AddMessages(AddOutgoingEdges(typeParameter, constraints, SemanticRole.Constraint));

                result.Value = result.AddMessages(FinishNode(typeParameter, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseTypeQuery(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            Result<Node> result = new Result<Node>();

            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.TypeOf)
            {
                var startPos = token.StartPos;

                token = result.AddMessages(
                    NextToken(lexer, context, ct)
                );

                Node subject = result.AddMessages(
                    ParseEntityName(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var typeQuery = NodeFactory.TypeQuery(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(typeQuery, subject, SemanticRole.Subject));

                result.Value = result.AddMessages(FinishNode(typeQuery, lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context)
                );
            }

            return result;
        }

        private Result<Node> ParseFunctionType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.EqualsGreaterThanToken, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var fnType = NodeFactory.FunctionTypeReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(fnType, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(fnType, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(fnType, type, SemanticRole.Type));
                
                result.Value = result.AddMessages(FinishNode(fnType, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseConstructorType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role != SymbolRole.Construction)
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }

            var result = new Result<Node>();

            var startPos = token.StartPos;

            // [dho] skip over the `new` token - 17/02/19
            token = result.AddMessages(NextToken(lexer, context, ct));

            var (template, parameters, type) = result.AddMessages(
                ParseFunctionSignatureComponents(token, SyntaxKind.ColonToken, lexer, context, ct)
            );

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var ctorType = NodeFactory.ConstructorTypeReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(ctorType, template, SemanticRole.Template));
                result.AddMessages(AddOutgoingEdges(ctorType, parameters, SemanticRole.Parameter));
                result.AddMessages(AddOutgoingEdges(ctorType, type, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(ctorType, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseUnionTypeOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return ParseUnionType(token, lexer, context, ct);
        }

        private Result<Node> ParseUnionType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var types = new List<Node>();

            types.Add(
                result.AddMessages(
                    ParseIntersectionTypeOrHigher(token, lexer, context, ct)
                )
            );

            while (EatIfNext(SyntaxKind.BarToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                types.Add(
                    result.AddMessages(
                        ParseIntersectionTypeOrHigher(token, lexer, context, ct)
                    )
                );
            }

            if (types.Count == 1)
            {
                result.Value = types[0];
            }
            else
            {
                var range = new Range(startPos, lexer.Pos);

                var union = NodeFactory.UnionTypeReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(union, types, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(union, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseIntersectionTypeOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return ParseIntersectionType(token, lexer, context, ct);
        }

        private Result<Node> ParseIntersectionType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var types = new List<Node>();

            types.Add(
                result.AddMessages(
                    ParseTypeOperatorOrHigher(token, lexer, context, ct)
                )
            );

            while (EatIfNext(SyntaxKind.AmpersandToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                types.Add(
                    result.AddMessages(
                        ParseTypeOperatorOrHigher(token, lexer, context, ct)
                    )
                );
            }


            if (types.Count == 1)
            {
                result.Value = types[0];
            }
            else
            {
                var range = new Range(startPos, lexer.Pos);

                var intersection = NodeFactory.IntersectionTypeReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(intersection, types, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(intersection, lexer, context, ct));
            }

            return result;
        }

        private Result<Node> ParseTypeOperatorOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (IsTypeOperator(token, lexer, context, ct))
            {
                return ParseTypeOperator(token, lexer, context, ct);
            }
            else
            {
                return ParseArrayTypeOrHigher(token, lexer, context, ct);
            }
        }

        private Result<Node> ParseIndexTypeQuery(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            token = result.AddMessages(NextToken(lexer, context, ct));

            var type = result.AddMessages(
                ParseTypeOperatorOrHigher(token, lexer, context, ct)
            );

            var range = new Range(startPos, lexer.Pos);

            var indexTypeQuery = NodeFactory.IndexTypeQuery(
                context.AST,
                CreateOrigin(range, lexer, context)
            );

            result.AddMessages(AddOutgoingEdges(indexTypeQuery, type, SemanticRole.Type));

            result.Value = result.AddMessages(FinishNode(indexTypeQuery, lexer, context, ct));

            return result;
        }

        private Result<Node> ParseTypeOperator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            // [dho] `keyof T` 
            //        ^^^^^      - 23/03/19
            if (role == SymbolRole.IndexTypeQuery)
            {
                return ParseIndexTypeQuery(token, lexer, context, ct);
            }
            // [dho] `infer T` 
            //        ^^^^^      - 23/03/19
            else if (role == SymbolRole.InferredTypeQuery)
            {
                var result = new Result<Node>();

                var startPos = token.StartPos;

                token = result.AddMessages(NextToken(lexer, context, ct));

                var type = result.AddMessages(
                    ParseTypeOperatorOrHigher(token, lexer, context, ct)
                );

                var range = new Range(startPos, lexer.Pos);

                var inferredTypeQuery = NodeFactory.InferredTypeQuery(
                    context.AST,
                    CreateOrigin(range, lexer, context)
                );

                result.AddMessages(AddOutgoingEdges(inferredTypeQuery, type, SemanticRole.Type));

                result.Value = result.AddMessages(FinishNode(inferredTypeQuery, lexer, context, ct));

                return result;
            }
            else
            {
                return CreateUnsupportedSymbolRoleResult<Node>(role, token, lexer, context);
            }
        }

        private Result<Node> ParseArrayTypeOrHigher(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var startPos = token.StartPos;

            var type = result.AddMessages(
                ParseNonArrayType(token, lexer, context, ct)
            );

            while (!token.PrecedingLineBreak && EatIfNext(SyntaxKind.OpenBracketToken, lexer, context, ct))
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                if (IsStartOfType(token, lexer, context, ct))
                {
                    var incident = type;

                    var member = result.AddMessages(
                        ParseType(token, lexer, context, ct)
                    );

                    token = result.AddMessages(NextToken(lexer, context, ct));

                    var range = new Range(startPos, lexer.Pos);

                    type = result.AddMessages(FinishNode(NodeFactory.IndexedAccess(
                        context.AST,
                        CreateOrigin(range, lexer, context)
                    ), lexer, context, ct));

                    result.AddMessages(AddOutgoingEdges(context.AST, type, incident, SemanticRole.Incident));
                    result.AddMessages(AddOutgoingEdges(context.AST, type, member, SemanticRole.Member));
                }
                else
                {
                    var range = new Range(startPos, lexer.Pos);

                    var arrayType = type;

                    type = result.AddMessages(FinishNode(
                        NodeFactory.ArrayTypeReference(context.AST, CreateOrigin(range, lexer, context)),
                        lexer, context, ct));

                    result.AddMessages(AddOutgoingEdges(context.AST, type, arrayType, SemanticRole.Type));
                }

                if (token.Kind != SyntaxKind.CloseBracketToken)
                {
                    result.AddMessages(
                        CreateErrorResult<Node>(token, "']' expected", lexer, context)
                    );
                }
            }

            result.Value = type;

            return result;
        }

        private Result<Node> ParseNonArrayType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (GetSymbolRole(token, lexer, context, ct))
            {
                case SymbolRole.FalseBooleanConstant:
                    return ParseLiteralTypeReference(token, lexer, context, ct);

                // case SymbolRole.Identifier:
                //     return ParseTypeReference(token, lexer, context, ct);

                case SymbolRole.IncidentContextReference:
                    {
                        if (LookAhead(IsSmartCast, lexer, context, ct).Item1)
                        {
                            return ParseSmartCast(token, lexer, context, ct);
                        }
                        else
                        {
                            return ParseIncidentContextReference(token, lexer, context, ct);
                        }
                    }

                case SymbolRole.Null:
                    return ParseNull(token, lexer, context, ct);

                case SymbolRole.TrueBooleanConstant:
                    return ParseLiteralTypeReference(token, lexer, context, ct);

                case SymbolRole.TypeOf:
                    return ParseTypeOfExpression(token, lexer, context, ct);
            }

            ////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////////

            switch (token.Kind)
            {
                case SyntaxKind.StringLiteral:
                    return ParseLiteralTypeReference(token, lexer, context, ct);

                case SyntaxKind.NumericLiteral:
                    return ParseLiteralTypeReference(token, lexer, context, ct);

                case SyntaxKind.MinusToken:
                    {
                        if (LookAhead(IsNumericLiteral, lexer, context, ct).Item1)
                        {
                            return ParseLiteralTypeReference(token, lexer, context, ct);
                        }
                        else
                        {
                            return ParseTypeReference(token, lexer, context, ct);
                        }
                    }

                case SyntaxKind.OpenBraceToken:
                    {
                        if (LookAhead(IsStartOfMappedType, lexer, context, ct).Item1)
                        {
                            return ParseMappedType(token, lexer, context, ct);
                        }
                        else
                        {
                            return ParseTypeLiteral(token, lexer, context, ct);
                        }
                    }

                case SyntaxKind.OpenBracketToken:
                    return ParseTupleType(token, lexer, context, ct);


                case SyntaxKind.OpenParenToken:
                    return ParseParenthesizedType(token, lexer, context, ct);

                default:
                    return ParseTypeReference(token, lexer, context, ct);
            }
        }

        private Result<Node> ParseLiteralTypeReference(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            ParseDelegate del;

            if (role == SymbolRole.FalseBooleanConstant)
            {
                del = ParseFalseBooleanConstant;
            }
            else if (role == SymbolRole.TrueBooleanConstant)
            {
                del = ParseTrueBooleanConstant;
            }
            else if (token.Kind == SyntaxKind.StringLiteral)
            {
                del = ParseStringLiteral;
            }
            else if (token.Kind == SyntaxKind.NumericLiteral)
            {
                del = ParseNumericLiteral;
            }
            else if (token.Kind == SyntaxKind.MinusToken && LookAhead(IsNumericLiteral, lexer, context, ct).Item1)
            {
                del = ParseSimpleUnaryExpression;
            }
            else
            {
                return CreateUnsupportedTokenResult<Node>(token, lexer, context);
            }

            var result = new Result<Node>();

            var startPos = token.StartPos;

            Node constant = result.AddMessages(del(token, lexer, context, ct));

            if (!HasErrors(result))
            {
                var range = new Range(startPos, lexer.Pos);

                var literalTypeRef = NodeFactory.LiteralTypeReference(context.AST, CreateOrigin(range, lexer, context));

                result.AddMessages(AddOutgoingEdges(literalTypeRef, constant, SemanticRole.Literal));

                result.Value = result.AddMessages(FinishNode(literalTypeRef, lexer, context, ct));
            }

            return result;
        }

        // private Result<Node> ParseIdentifierAndNoDot(Token token, Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var result = new Result<Node>();

        //     var identifier = result.AddMessages(
        //         ParseIdentifier(token, lexer, context, ct)
        //     );

        //     if(LookAhead(IsDot, lexer, context, ct).Item1)
        //     {
        //         result.AddMessages(
        //             CreateErrorResult<Node>(token, "Dot token cannot follow identifier here", lexer, context)
        //         );
        //     }
        //     else
        //     {
        //         result.Value = identifier;
        //     }

        //     return result;
        // }

        private Result<Node> ParseOptionalExpressionAfterSymbolRole(SymbolRole role, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var actualRole = GetSymbolRole(token, lexer, context, ct);

            // [dho] `return` 
            //        ^^^^^^      - 29/01/19
            if (actualRole == role)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(actualRole, token, lexer, context)
                );

                return result;
            }

            if (!CanParseSemicolon(token, lexer, context, ct))
            {
                var expContext = ContextHelpers.Clone(context);

                // [dho] allow `in expressions` when parsing the expression - 24/03/19
                expContext.Flags &= ~ContextFlags.DisallowInContext;

                var exp = result.AddMessages(
                    ParseExpression(token, lexer, expContext, ct)
                );

                result.Value = exp;
            }

            result.AddMessages(EatSemicolon(lexer, context, ct));

            return result;
        }


        private Result<Node> ParseParenthesizedExpressionAfterSymbolRole(SymbolRole role, Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var result = new Result<Node>();

            var actualRole = GetSymbolRole(token, lexer, context, ct);

            // [dho] `while ...` 
            //        ^^^^^      - 29/01/19
            if (actualRole == role)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedSymbolRoleResult<Node>(actualRole, token, lexer, context)
                );

                return result;
            }

            // [dho] `while(...` 
            //             ^      - 29/01/19
            if (token.Kind == SyntaxKind.OpenParenToken)
            {
                token = result.AddMessages(NextToken(lexer, context, ct));
            }
            else
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );

                return result;
            }


            var expContext = ContextHelpers.Clone(context);

            // [dho] allow `in expressions` when parsing the expression - 29/01/19
            expContext.Flags &= ~ContextFlags.DisallowInContext;

            var exp = result.AddMessages(
                ParseExpression(token, lexer, expContext, ct)
            );



            // [dho] get the next token after the statement - 29/01/19
            {
                token = result.AddMessages(NextToken(lexer, context, ct));

                if (HasErrors(result))
                {
                    return result;
                }
            }


            // [dho] `while(...)` 
            //                 ^   - 29/01/19
            if (token.Kind != SyntaxKind.CloseParenToken)
            {
                result.AddMessages(
                    CreateUnsupportedTokenResult<Node>(token, lexer, context)
                );

                return result;
            }


            result.Value = exp;

            return result;
        }


        private bool IsStartOfFunctionType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.LessThanEqualsToken)
            {
                return true;
            }
            else if (token.Kind == SyntaxKind.OpenParenToken)
            {
                return LookAhead(IsUnambiguouslyStartOfFunctionType, lexer, context, ct).Item1;
            }
            else
            {
                return false;
            }
        }

        private bool IsUnambiguouslyStartOfFunctionType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.CloseParenToken || token.Kind == SyntaxKind.DotDotDotToken)
            {
                // ( )
                // ( ...
                return true;
            }

            // [dho] skip start of parameter
            if (EatParameterStart(token, lexer, context, ct))
            {
                var r = NextToken(lexer, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;

                if (token.Kind == SyntaxKind.ColonToken || token.Kind == SyntaxKind.CommaToken ||
                    token.Kind == SyntaxKind.QuestionToken || token.Kind == SyntaxKind.EqualsToken)
                {
                    // ( xxx :
                    // ( xxx ,
                    // ( xxx ?
                    // ( xxx =
                    return true;
                }


                // ( xxx ) =>
                if (token.Kind == SyntaxKind.CloseParenToken &&
                    LookAhead(IsEqualsGreaterThan, lexer, context, ct).Item1)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsStartOfMappedType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAheadLexer = lexer.Clone();

            Token lookAheadToken = token;

            if (EatOrnamentation(lookAheadToken, lookAheadLexer, context, ct))
            {
                var r = NextToken(lookAheadLexer, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                lookAheadToken = r.Value;
            }

            // [dho] `{ [` 
            //          ^   - 20/03/19
            return lookAheadToken.Kind == SyntaxKind.OpenBracketToken &&
                    // [dho] `{ [ foo` 
                    //            ^^^   - 20/03/19
                    EatIfNext(IsIdentifier, lookAheadLexer, context, ct) &&
                        // [dho] `{ [ foo in` 
                        //                ^^   - 20/03/19
                        EatIfNext(IsKeyIn, lookAheadLexer, context, ct);
        }

        // public IEnumerable<Token> EnumerateTokenList(Lexer lexer, Context context, CancellationToken ct)
        // {
        //     var token = new Token(lexer.Scan(), lexer.GetStartPos());

        //     while (!IsListTerminator(token, lexer, context, ct))
        //     {
        //         if (IsListElement(token, lexer, /*inErrorRecovery*/ false, context, ct))
        //         {
        //             yield return token;

        //         }
        //         // [dho] TODO I feel like we should always break
        //         // if it's not a list element.. otherwise is there a
        //         // danger of swallowing tokens...?? - 20/01/19
        //         else if (AbortParsingListOrMoveToNextToken(kind))
        //         {
        //             break;
        //         }

        //         // [dho] move to next token - 20/01/19
        //         token = new Token(lexer.Scan(), lexer.GetStartPos());
        //     }
        // }

        private bool IsBlockTerminator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.CloseBraceToken;
        }

        private bool IsCommaOrListTerminator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.CommaToken || IsListTerminator(token, lexer, context, ct);
        }

        private bool IsListElementOrCommaOrListTerminator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return IsCommaOrListTerminator(token, lexer, context, ct) || IsListElement(token, lexer, context, ct);
        }

        private bool IsListElementOrListTerminator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return IsListElement(token, lexer, context, ct) || IsListTerminator(token, lexer, context, ct);
        }

        public bool IsListTerminator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.EndOfFileToken)
            {
                // Being at the end of the file ends all lists.
                return true;
            }
            switch (context.CurrentKind)
            {
                case ContextKind.BlockStatements:
                case ContextKind.SwitchClauses:
                case ContextKind.TypeMembers:
                case ContextKind.ClassMembers:
                case ContextKind.EnumMembers:
                case ContextKind.ObjectLiteralMembers:
                case ContextKind.ObjectBindingElements:
                case ContextKind.ImportOrExportSpecifiers:
                    {
                        return token.Kind == SyntaxKind.CloseBraceToken;
                    }

                case ContextKind.SwitchClauseStatements:
                    {
                        if (token.Kind == SyntaxKind.CloseBraceToken)
                        {
                            return true;
                        }
                        else
                        {
                            var role = GetSymbolRole(token, lexer, context, ct);

                            return role == SymbolRole.MatchClause || role == SymbolRole.Default;
                        }
                    }

                case ContextKind.HeritageClauseElement:
                    {
                        if (token.Kind == SyntaxKind.OpenBraceToken)
                        {
                            return true;
                        }
                        else
                        {
                            var role = GetSymbolRole(token, lexer, context, ct);

                            return role == SymbolRole.Conformity || role == SymbolRole.Inheritance;
                        }
                    }

                case ContextKind.DataValueDeclarations:
                    {
                        return IsVariableDeclaratorListTerminator(token, lexer, context, ct);
                    }

                case ContextKind.TypeParameters:
                    {
                        // Tokens other than '>' are here for better error recovery
                        if (token.Kind == SyntaxKind.GreaterThanToken ||
                            token.Kind == SyntaxKind.OpenParenToken ||
                            token.Kind == SyntaxKind.OpenBraceToken)
                        {
                            return true;
                        }
                        else
                        {
                            var role = GetSymbolRole(token, lexer, context, ct);

                            return role == SymbolRole.Conformity || role == SymbolRole.Inheritance;
                        }
                    }

                case ContextKind.ArgumentExpressions:
                    {
                        // Tokens other than ')' are here for better error recovery
                        return token.Kind == SyntaxKind.CloseParenToken ||
                                token.Kind == SyntaxKind.SemicolonToken;
                    }

                case ContextKind.ArrayLiteralMembers:
                case ContextKind.TupleElementTypes:
                case ContextKind.ArrayBindingElements:

                    return token.Kind == SyntaxKind.CloseBracketToken;
                case ContextKind.Parameters:
                case ContextKind.RestProperties:

                    // Tokens other than ')' and ']' (the latter for index signatures) are here for better error recovery
                    return token.Kind == SyntaxKind.CloseParenToken || token.Kind == SyntaxKind.CloseBracketToken /*|| token == SyntaxKind.OpenBraceToken*/;
                case ContextKind.TypeArguments:

                    // All other tokens should cause the type-argument to terminate except comma token
                    return token.Kind != SyntaxKind.CommaToken;
                case ContextKind.HeritageClauses:

                    return token.Kind == SyntaxKind.OpenBraceToken || token.Kind == SyntaxKind.CloseBraceToken;
                case ContextKind.JsxAttributes:

                    return token.Kind == SyntaxKind.GreaterThanToken || token.Kind == SyntaxKind.SlashToken;
                case ContextKind.JsxChildren:

                    return token.Kind == SyntaxKind.LessThanToken && LookAhead(IsSlash, lexer, context, ct).Item1;
                case ContextKind.JSDocFunctionParameters:

                    return token.Kind == SyntaxKind.CloseParenToken || token.Kind == SyntaxKind.ColonToken || token.Kind == SyntaxKind.CloseBraceToken;
                case ContextKind.JSDocTypeArguments:

                    return token.Kind == SyntaxKind.GreaterThanToken || token.Kind == SyntaxKind.CloseBraceToken;
                case ContextKind.JSDocTupleTypes:

                    return token.Kind == SyntaxKind.CloseBracketToken || token.Kind == SyntaxKind.CloseBraceToken;
                case ContextKind.JSDocRecordMembers:

                    return token.Kind == SyntaxKind.CloseBraceToken;
            }

            return false; // ?
        }

        public bool IsListElement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] TODO CHECK is this OK?! - 17/04/19
            if(token.Kind == SyntaxKind.Directive)
            {
                return true;
            }

            // var node = CurrentNode(parsingContext);
            // if (node != null)
            // {

            //     return true;
            // }
            switch (context.CurrentKind)
            {
                case ContextKind.SourceElements:
                case ContextKind.BlockStatements:
                case ContextKind.SwitchClauseStatements:
                    // If we're in error recovery, then we don't want to treat ';' as an empty statement.
                    // The problem is that ';' can show up in far too many contexts, and if we see one
                    // and assume it's a statement, then we may bail out inappropriately from whatever
                    // we're parsing.  For example, if we have a semicolon in the middle of a class, then
                    // we really don't want to assume the class is over and we're on a statement in the
                    // outer module.  We just want to consume and move on.
                    return !(token.Kind == SyntaxKind.SemicolonToken && context.ErrorRecoveryMode) && IsStartOfStatement(token, lexer, context, ct);

                case ContextKind.SwitchClauses:
                    {
                        var role = GetSymbolRole(token, lexer, context, ct);

                        return role == SymbolRole.MatchClause || role == SymbolRole.Default;
                    }

                case ContextKind.TypeMembers:
                    {
                        // [dho] original source seemed to use a look ahead here, but we 
                        // already have the first token of the type member, so if we look ahead
                        // to the next token then we get the wrong result (false negatives) - 18/03/19
                        return IsStartOfTypeMember(token, lexer, context, ct);
                    }

                case ContextKind.ClassMembers:
                    {
                        // We allow semicolons as class elements (as specified by ES6) as long as we're
                        // not in error recovery.  If we're in error recovery, we don't want an errant
                        // semicolon to be treated as a class member (since they're almost always used
                        // for statements.
                        if (token.Kind == SyntaxKind.SemicolonToken && !context.ErrorRecoveryMode)
                        {
                            return true;
                        }
                        else
                        {
                            // [dho] original source seemed to use a look ahead here, but we 
                            // already have the first token of the class member, so if we look ahead
                            // to the next token then we get the wrong result (false negatives) - 18/03/19
                            return IsStartOfClassMember(token, lexer, context, ct);
                        }
                    }

                case ContextKind.EnumMembers:
                    // Include open bracket computed properties. This technically also lets in indexers,
                    // which would be a candidate for improved error reporting.
                    return token.Kind == SyntaxKind.OpenBracketToken ||
                                            IsLiteralPropertyName(token, lexer, context, ct);

                case ContextKind.ObjectLiteralMembers:
                    return token.Kind == SyntaxKind.OpenBracketToken || token.Kind == SyntaxKind.AsteriskToken || token.Kind == SyntaxKind.DotDotDotToken || IsLiteralPropertyName(token, lexer, context, ct);

                case ContextKind.RestProperties:
                    return IsLiteralPropertyName(token, lexer, context, ct);

                case ContextKind.ObjectBindingElements:
                    return token.Kind == SyntaxKind.OpenBracketToken ||
                            token.Kind == SyntaxKind.DotDotDotToken ||
                            IsLiteralPropertyName(token, lexer, context, ct);

                case ContextKind.HeritageClauseElement:
                    if (token.Kind == SyntaxKind.OpenBraceToken)
                    {
                        var lookAhead = lexer.Clone();

                        var r = NextToken(lookAhead, context, ct);

                        return !HasErrors(r) && IsValidHeritageClauseObjectLiteral(r.Value, lookAhead, context, ct);
                    }
                    else if (!context.ErrorRecoveryMode)
                    {
                        return IsStartOfLeftHandSideExpression(token, lexer, context, ct) &&
                                !IsHeritageClauseExtendsOrImplementsKeyword(token, lexer, context, ct);
                    }
                    else
                    {
                        // If we're in error recovery we tighten up what we're willing to match.
                        // That way we don't treat something like "this" as a valid heritage clause
                        // element during recovery.
                        return IsIdentifier(token, lexer, context, ct) &&
                                !IsHeritageClauseExtendsOrImplementsKeyword(token, lexer, context, ct);
                    }
                //goto caseLabel12;
                case ContextKind.DataValueDeclarations:
                    //caseLabel12:

                    // [dho] allowing known symbol roles here, so you can have things like
                    // `var module : any` instead of purely identifiers (ie. lexemes with `SymbolRole.Identifier`) - 29/03/19
                    return IsKnownSymbolRoleOrPattern(token, lexer, context, ct);

                case ContextKind.ArrayBindingElements:
                    return token.Kind == SyntaxKind.CommaToken || token.Kind == SyntaxKind.DotDotDotToken || IsIdentifierOrPattern(token, lexer, context, ct);

                case ContextKind.TypeParameters:
                    return IsIdentifier(token, lexer, context, ct);

                case ContextKind.ArgumentExpressions:
                case ContextKind.ArrayLiteralMembers:
                    return token.Kind == SyntaxKind.CommaToken || token.Kind == SyntaxKind.DotDotDotToken || IsStartOfExpression(token, lexer, context, ct);

                case ContextKind.Parameters:
                    return IsStartOfParameter(token, lexer, context, ct);

                case ContextKind.TypeArguments:
                case ContextKind.TupleElementTypes:
                    return token.Kind == SyntaxKind.CommaToken || IsStartOfType(token, lexer, context, ct);

                case ContextKind.HeritageClauses:
                    return IsHeritageClause(token, lexer, context, ct);

                case ContextKind.ImportOrExportSpecifiers:
                    return IsKnownSymbolRole(token, lexer, context, ct);

                case ContextKind.JsxAttributes:
                    return IsKnownSymbolRole(token, lexer, context, ct) ||
                            token.Kind == SyntaxKind.OpenBraceToken;

                case ContextKind.JsxChildren:
                    return true;

                // case ContextKind.JSDocFunctionParameters:
                // case ContextKind.JSDocTypeArguments:
                // case ContextKind.JSDocTupleTypes:
                //     return JsDocParser.IsJsDocType();

                case ContextKind.JSDocRecordMembers:
                    return IsLiteralPropertyName(token, lexer, context, ct);
            }


            // Debug.Fail("Non-exhaustive case in 'isListElement'.");
            return false;
        }


        public bool IsVariableDeclaratorListTerminator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (CanParseSemicolon(token, lexer, context, ct))
            {
                return true;
            }
            else if (token.Kind == SyntaxKind.EqualsGreaterThanToken)
            {
                return true;
            }
            else
            {
                var role = GetSymbolRole(token, lexer, context, ct);

                return role == SymbolRole.KeyIn || role == SymbolRole.MemberOf;
            }
        }

        Result<bool> EatSemicolon(Lexer lexer, Context context, CancellationToken ct)
        {
            if (LookAhead(CanParseSemicolon, lexer, context, ct).Item1) // [dho] semicolon is optional - 24/03/19
            {
                // [dho] so eat it if we can, but no big deal - 24/03/19
                var didEat = EatIfNext(SyntaxKind.SemicolonToken, lexer, context, ct);

                return new Result<bool>() { Value = didEat };
            }
            else
            {
                // [dho] semicolon is mandatory - 24/03/19
                return EatIfNextOrError(SyntaxKind.SemicolonToken, lexer, context, ct);
            }
        }

        Result<bool> EatTypeMemberDelimiter(Lexer lexer, Context context, CancellationToken ct)
        {
            if (EatIfNext(SyntaxKind.CommaToken, lexer, context, ct))
            {
                return new Result<bool>() { Value = true };
            }

            // [dho] no comma ahead, so look for semicolon - 24/03/19
            return EatSemicolon(lexer, context, ct);
        }

        public bool CanParseSemicolon(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.SemicolonToken)
            {
                return true;
            }

            // We can parse out an optional semicolon in ASI cases in the following cases.
            return token.Kind == SyntaxKind.CloseBraceToken || token.Kind == SyntaxKind.EndOfFileToken || token.PrecedingLineBreak;
        }



        public bool IsStartOfStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.AtToken ||
                token.Kind == SyntaxKind.SemicolonToken ||
                token.Kind == SyntaxKind.OpenBraceToken)
            {
                return true;
            }


            var role = GetSymbolRole(token, lexer, context, ct);

            switch (role)
            {
                case SymbolRole.Constant:
                case SymbolRole.Variable:
                case SymbolRole.ObjectType:
                case SymbolRole.Enumeration:
                case SymbolRole.PredicateJunction:
                case SymbolRole.DoWhilePredicateLoop:
                case SymbolRole.Function:
                case SymbolRole.WhilePredicateLoop:

                case SymbolRole.ForKeysLoop:
                case SymbolRole.ForMembersLoop:
                case SymbolRole.ForPredicateLoop:

                case SymbolRole.JumpToNextIteration:
                case SymbolRole.ClauseBreak:
                case SymbolRole.LoopBreak:
                case SymbolRole.FunctionTermination:
                case SymbolRole.MatchJunction:
                case SymbolRole.RaiseError:
                case SymbolRole.ErrorTrapJunction:
                case SymbolRole.Breakpoint:
                case SymbolRole.ErrorHandlerClause:
                case SymbolRole.ErrorFinallyClause:
                case SymbolRole.PrioritySymbolResolutionContext:
                    return true;

                case SymbolRole.Import:
                case SymbolRole.Export:
                    return IsStartOfDeclaration(token, lexer, context, ct);

                // case SyntaxKind.AsyncKeyword:
                // case SyntaxKind.DeclareKeyword:
                case SymbolRole.CompilerHint:
                case SymbolRole.Interface:
                // case SyntaxKind.ModuleKeyword:
                case SymbolRole.Namespace:
                case SymbolRole.TypeAlias:
                case SymbolRole.GlobalContextReference:
                    // When these don't start a declaration, they're an identifier in an expression statement
                    return true;

                case SymbolRole.Modifier:
                    {
                        // When these don't start a declaration, they may be the start of a class member if an identifier
                        // immediately follows. Otherwise they're an identifier in an expression statement.
                        return IsStartOfDeclaration(token, lexer, context, ct) ||
                                !LookAhead(IsKnownSymbolRoleOnSameLine, lexer, context, ct).Item1;
                    }

                default:
                    return IsStartOfExpression(token, lexer, context, ct);
            }
        }

        public bool IsStartOfClassMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            if (EatOrnamentation(token, lookAhead, context, ct))
            {
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;
            }


            if (token.Kind == SyntaxKind.AsteriskToken)
            {
                return true;
            }

            SyntaxKind idTokenKind = SyntaxKind.Unknown;
            SymbolRole idTokenRole = SymbolRole.None;

            if (IsLiteralPropertyName(token, lookAhead, context, ct))
            {
                idTokenKind = token.Kind;
                idTokenRole = GetSymbolRole(token, lookAhead, context, ct);

                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                token = r.Value;
            }


            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                return true;
            }

            if (idTokenKind != SyntaxKind.Unknown)  // null)
            {
                if (idTokenRole == SymbolRole.Identifier || idTokenRole == SymbolRole.Mutator || idTokenRole == SymbolRole.Accessor)
                {
                    return true;
                }

                switch (token.Kind)
                {
                    case SyntaxKind.OpenParenToken:
                    case SyntaxKind.LessThanToken:
                    case SyntaxKind.ColonToken:
                    case SyntaxKind.EqualsToken:
                    case SyntaxKind.QuestionToken:
                        // Not valid, but permitted so that it gets caught later on.
                        return true;
                    default:
                        // Covers
                        //  - Semicolons     (declaration termination)
                        //  - Closing braces (end-of-class, must be declaration)
                        //  - End-of-files   (not valid, but permitted so that it gets caught later on)
                        //  - Line-breaks    (enabling *automatic semicolon insertion*)
                        return CanParseSemicolon(token, lookAhead, context, ct);
                }
            }


            return false;
        }

        public bool IsStartOfDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] going to try not using `LookAhead` here because it will skip the current
            // token, eg `import` and that is the token we want to see is the start of a declaration
            // or not - 01/03/19
            return IsDeclaration(token, lexer, context, ct);
            // return LookAhead(IsDeclaration, lexer, context, ct).Item1;
        }

        public bool IsStartOfExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (IsStartOfLeftHandSideExpression(token, lexer, context, ct))
            {
                return true;
            }

            switch (token.Kind)
            {
                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                case SyntaxKind.TildeToken:
                case SyntaxKind.ExclamationToken:
                case SyntaxKind.PlusPlusToken:
                case SyntaxKind.MinusMinusToken:
                case SyntaxKind.LessThanToken:
                    // Yield/await always starts an expression.  Either it is an identifier (in which case
                    // it is definitely an expression).  Or it's a keyword (either because we're in
                    // a generator or async function, or in strict mode (or both)) and it started a yield or await expression.
                    return true;

                default:
                    {
                        switch (GetSymbolRole(token, lexer, context, ct))
                        {
                            case SymbolRole.Destruction:
                            case SymbolRole.GeneratorValue: // yield
                            case SymbolRole.InterimSuspension: // await
                            case SymbolRole.Identifier:
                            case SymbolRole.Modifier:
                            case SymbolRole.TypeOf:
                            case SymbolRole.EvalToVoid: // void 0
                                return true;

                            default:
                                break;
                        }

                        // Error tolerance.  If we see the start of some binary operator, we consider
                        // that the start of an expression.  That way we'll parse out a missing identifier,
                        // give a good message about an identifier being missing, and then consume the
                        // rest of the binary expression.
                        if (IsBinaryOperator(token, lexer, context, ct))
                        {
                            return true;
                        }


                        return false;
                    }
            }
        }


        public bool IsStartOfLeftHandSideExpression(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.NumericLiteral:
                case SyntaxKind.StringLiteral:
                case SyntaxKind.NoSubstitutionTemplateLiteral:
                case SyntaxKind.TemplateHead:
                case SyntaxKind.OpenParenToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.SlashEqualsToken:
                    return true;

                default:
                    {
                        switch (GetSymbolRole(token, lexer, context, ct))
                        {
                            case SymbolRole.IncidentContextReference:
                            case SymbolRole.SuperContextReference:
                            case SymbolRole.Null:
                            case SymbolRole.TrueBooleanConstant:
                            case SymbolRole.FalseBooleanConstant:
                            case SymbolRole.Function:
                            case SymbolRole.ObjectType:
                            case SymbolRole.Construction:
                            case SymbolRole.Identifier:
                                return true;

                            default:
                                return false;
                        }
                    }
            }
        }


        public bool IsStartOfExpressionStatement(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // As per the grammar, none of '{' or 'function' or 'class' can start an expression statement.
            if (token.Kind != SyntaxKind.OpenBraceToken && token.Kind != SyntaxKind.AtToken)
            {
                var role = GetSymbolRole(token, lexer, context, ct);

                if (role != SymbolRole.ObjectType && role != SymbolRole.Function)
                {
                    return IsStartOfExpression(token, lexer, context, ct);
                }
            }

            return false;
        }

        public bool IsStartOfParameter(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.DotDotDotToken || token.Kind == SyntaxKind.AtToken || IsIdentifierOrPattern(token, lexer, context, ct))
            {
                return true;
            }

            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.IncidentContextReference || role == SymbolRole.Modifier)
            {
                return true;
            }

            return false;
        }

        public bool IsStartOfParenthesizedOrFunctionType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.CloseParenToken ||
                    IsStartOfParameter(token, lexer, context, ct) ||
                    IsStartOfType(token, lexer, context, ct);
        }

        public bool IsStartOfType(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.OpenBraceToken:
                case SyntaxKind.OpenBracketToken:
                case SyntaxKind.LessThanToken:
                case SyntaxKind.BarToken:
                case SyntaxKind.AmpersandToken:
                case SyntaxKind.StringLiteral:
                case SyntaxKind.NumericLiteral:
                    return true;

                case SyntaxKind.MinusToken:
                    {
                        var lookAhead = lexer.Clone();

                        return EatIfNext(SyntaxKind.NumericLiteral, lookAhead, context, ct);
                    }

                case SyntaxKind.OpenParenToken:
                    {
                        var lookAhead = lexer.Clone();

                        var r = NextToken(lookAhead, context, ct);

                        // Only consider '(' the start of a type if followed by ')', '...', an identifier, a modifier,
                        // or something that starts a type. We don't want to consider things like '(1)' a type.
                        return !HasErrors(r) && IsStartOfParenthesizedOrFunctionType(r.Value, lookAhead, context, ct);
                    }

                default:
                    {
                        var role = GetSymbolRole(token, lexer, context, ct);

                        switch (role)
                        {
                            case SymbolRole.Construction:
                            case SymbolRole.EvalToVoid:
                            case SymbolRole.Null:
                            case SymbolRole.IncidentContextReference:
                            case SymbolRole.TypeOf:
                            case SymbolRole.TrueBooleanConstant:
                            case SymbolRole.FalseBooleanConstant:
                            case SymbolRole.Identifier:
                            case SymbolRole.IndexTypeQuery:
                            case SymbolRole.InferredTypeQuery:
                                return true;

                            default:
                                return false;
                        }
                    }
            }
        }

        public bool IsStartOfTypeMember(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            if (token.Kind == SyntaxKind.OpenParenToken || token.Kind == SyntaxKind.LessThanToken)
            {
                return true;
            }

            var lookAhead = lexer.Clone();

            bool idToken = false;

            while (GetSymbolRole(token, lookAhead, context, ct) == SymbolRole.Modifier)
            {
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                idToken = true;

                token = r.Value;
            }

            if (token.Kind == SyntaxKind.OpenBracketToken)
            {
                return true;
            }

            if (IsLiteralPropertyName(token, lookAhead, context, ct))
            {
                var r = NextToken(lookAhead, context, ct);

                if (HasErrors(r))
                {
                    return false;
                }

                idToken = true;

                token = r.Value;
            }


            if (idToken)
            {

                return token.Kind == SyntaxKind.OpenParenToken ||
                    token.Kind == SyntaxKind.LessThanToken ||
                    token.Kind == SyntaxKind.QuestionToken ||
                    token.Kind == SyntaxKind.ColonToken ||
                    token.Kind == SyntaxKind.CommaToken ||
                    CanParseSemicolon(token, lookAhead, context, ct);
            }

            return false;
        }


















        public bool IsBinaryOperator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] in a disallow `in` context - 26/02/19
            if ((context.Flags & ContextFlags.DisallowInContext) == ContextFlags.DisallowInContext)
            {
                // [dho] and the symbol is for `in`- 26/02/19
                if (GetSymbolRole(token, lexer, context, ct) == SymbolRole.KeyIn)
                {
                    return false;
                }
            }

            return GetBinaryOperatorPrecedence(token, lexer, context, ct) > 0;
        }

        public int GetBinaryOperatorPrecedence(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            switch (token.Kind)
            {
                case SyntaxKind.BarBarToken:

                    return 1;
                case SyntaxKind.AmpersandAmpersandToken:

                    return 2;
                case SyntaxKind.BarToken:

                    return 3;
                case SyntaxKind.CaretToken:

                    return 4;
                case SyntaxKind.AmpersandToken:

                    return 5;
                case SyntaxKind.EqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsToken:
                case SyntaxKind.EqualsEqualsEqualsToken:
                case SyntaxKind.ExclamationEqualsEqualsToken:

                    return 6;
                case SyntaxKind.LessThanToken:
                case SyntaxKind.GreaterThanToken:
                case SyntaxKind.LessThanEqualsToken:
                case SyntaxKind.GreaterThanEqualsToken:
                    return 7;

                case SyntaxKind.LessThanLessThanToken:
                case SyntaxKind.GreaterThanGreaterThanToken:
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
                    return 8;

                case SyntaxKind.PlusToken:
                case SyntaxKind.MinusToken:
                    return 9;

                case SyntaxKind.AsteriskToken:
                case SyntaxKind.SlashToken:
                case SyntaxKind.PercentToken:
                    return 10;

                case SyntaxKind.AsteriskAsteriskToken:
                    return 11;

                default:
                    {
                        var role = GetSymbolRole(token, lexer, context, ct);

                        if (role == SymbolRole.TypeTest ||
                            role == SymbolRole.KeyIn ||
                            role == SymbolRole.ReferenceAlias)
                        {
                            return 7;
                        }
                    }
                    break;
            }


            // -1 is lower than all other precedences.  Returning it will cause binary expression
            // parsing to stop.
            return -1;
        }

        public bool IsDeclaration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            while (true)
            {
                var role = GetSymbolRole(token, lookAhead, context, ct);

                switch (role)
                {
                    case SymbolRole.CompilerHint:
                        {
                            var la = LookAhead(IsSameLine, lookAhead, context, ct);

                            if (!la.Item1) // [dho] not on same line - 23/02/19
                            {
                                return false;
                            }

                            var r = NextToken(lookAhead, context, ct);

                            if (HasErrors(r)) return false;

                            token = r.Value;

                            continue;
                        }

                    case SymbolRole.Constant:
                    case SymbolRole.Variable:
                    case SymbolRole.Function:
                    case SymbolRole.ObjectType:
                    case SymbolRole.Enumeration:
                        return true;

                    case SymbolRole.Export:
                        {
                            var la = LookAhead(IsEqualsOrAsteriskOrOpenBraceOrDefaultOrReferenceAliasOrIsDeclaration, lookAhead, context, ct);

                            if (la.Item1)
                            {
                                return true;
                            }

                            lookAhead.Pos = la.Item2.Pos;

                            var r = NextToken(lookAhead, context, ct);

                            if (HasErrors(r)) return false;

                            token = r.Value;

                            continue;
                        }

                    case SymbolRole.GlobalContextReference:
                        return LookAhead(IsIdentifierOrOpenBraceOrExport, lookAhead, context, ct).Item1;

                    case SymbolRole.Import:
                        return LookAhead(IsStringLiteralOrAsteriskOrOpenBraceOrKnownSymbolRole, lookAhead, context, ct).Item1;

                    case SymbolRole.Interface:
                    case SymbolRole.TypeAlias:
                        return LookAhead(IsIdentifierOnSameLine, lookAhead, context, ct).Item1;

                    case SymbolRole.Namespace:
                        return LookAhead(IsIdentifierOrStringLiteralOnSameLine, lookAhead, context, ct).Item1;

                    case SymbolRole.Modifier:
                        {
                            lookAhead.Pos = token.StartPos;

                            EatOrnamentation(token, lookAhead, context, ct);

                            var la = LookAhead(IsSameLine, lookAhead, context, ct);

                            if (!la.Item1) // [dho] not on same line - 23/02/19
                            {
                                return false;
                            }

                            var r = NextToken(lookAhead, context, ct);

                            if (HasErrors(r)) return false;

                            token = r.Value;

                            continue;
                        }

                    // [dho] NOTE deviate here, we put `static` in as a modifier,
                    // is this going to be an issue? - 23/02/19
                    // case SyntaxKind.StaticKeyword:

                    //     NextToken();

                    //     continue;

                    default:
                        return false;
                }
            }
        }


        public bool IsIdentifier(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return GetSymbolRole(token, lexer, context, ct) == SymbolRole.Identifier;
        }

        public bool IsEqualsOrDefault(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.EqualsToken ||
                    GetSymbolRole(token, lexer, context, ct) == SymbolRole.Default;
        }

        public bool IsKeyIn(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return GetSymbolRole(token, lexer, context, ct) == SymbolRole.KeyIn;
        }

        public bool IsIdentifierOrPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.OpenBraceToken ||
                    token.Kind == SyntaxKind.OpenBracketToken ||
                    IsIdentifier(token, lexer, context, ct);
        }

        public bool IsKnownSymbolRoleOrPattern(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.OpenBraceToken ||
                    token.Kind == SyntaxKind.OpenBracketToken ||
                    IsKnownSymbolRole(token, lexer, context, ct);
        }


        public bool IsInheritance(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return GetSymbolRole(token, lexer, context, ct) == SymbolRole.Inheritance;
        }

        public bool IsConformity(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return GetSymbolRole(token, lexer, context, ct) == SymbolRole.Conformity;
        }

        public bool IsEnumeration(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return GetSymbolRole(token, lexer, context, ct) == SymbolRole.Enumeration;
        }

        public bool IsTypeOperator(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            return role == SymbolRole.IndexTypeQuery || role == SymbolRole.InferredTypeQuery;
        }


        public bool IsSemicolonOrCloseParen(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.SemicolonToken || token.Kind == SyntaxKind.CloseParenToken;
        }

        public bool IsMatchClauseOrDefault(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            return role == SymbolRole.MatchClause || role == SymbolRole.Default;
        }

        public bool IsHeritageClauseExtendsOrImplementsKeyword(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            var role = GetSymbolRole(token, lexer, context, ct);

            if (role == SymbolRole.Conformity || role == SymbolRole.Inheritance)
            {
                return LookAhead(IsStartOfExpression, lexer, context, ct).Item1;
            }

            return false;
        }

        public bool IsValidHeritageClauseObjectLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            // [dho] `{` 
            //        ^   - 23/02/19
            if (token.Kind == SyntaxKind.OpenBraceToken)
            {
                var lookAhead = lexer.Clone();

                // [dho] `{}` 
                //         ^   - 23/02/19
                if (EatIfNext(SyntaxKind.CloseBraceToken, lookAhead, context, ct))
                {
                    return LookAhead((laToken, laLexer, laContext, laCT) =>
                    {

                        // [dho] `{},` 
                        //          ^   - 23/02/19
                        if (laToken.Kind == SyntaxKind.CommaToken ||
                            // [dho] `{}{` 
                            //          ^   - 23/02/19
                            laToken.Kind == SyntaxKind.OpenBraceToken)
                        {
                            return true;
                        }

                        var role = GetSymbolRole(laToken, laLexer, laContext, laCT);

                        // [dho] `{} implements` 
                        //           ^^^^^^^^^^   - 23/02/19
                        return role == SymbolRole.Conformity ||
                                // [dho] `{} extends` 
                                //           ^^^^^^^   - 23/02/19
                                role == SymbolRole.Inheritance;

                    }, lookAhead, context, ct).Item1;
                }
            }

            return true;
        }

        public bool IsLiteralPropertyName(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.StringLiteral ||
                    token.Kind == SyntaxKind.NumericLiteral ||
                    IsKnownSymbolRole(token, lexer, context, ct);
        }

        public bool IsNumericLiteral(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.NumericLiteral;
        }

        public bool IsOpenParen(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.OpenParenToken;
        }

        public bool IsLessThan(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.LessThanToken;
        }

        public bool IsSlash(Token token, Lexer lexer, Context context, CancellationToken ct)
        {
            return token.Kind == SyntaxKind.SlashToken;
        }

        public delegate bool LookAheadPredicateDelegate(Token token, Lexer lexer, Context context, CancellationToken ct);

        public (bool, Lexer) LookAhead(LookAheadPredicateDelegate predicate, Lexer lexer, Context context, CancellationToken ct)
        {
            var lookAhead = lexer.Clone();

            var r = NextToken(lookAhead, context, ct);

            var isMatch = !HasErrors(r) && predicate(r.Value, lookAhead, context, ct);

            // [dho] we return the cloned scanner too in case
            // the caller wants to continue where the lookahead left off
            return (isMatch, lookAhead);
        }

    }
}