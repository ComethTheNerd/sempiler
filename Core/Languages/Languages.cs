
namespace Sempiler.Languages
{
    public static class LanguageSemantics 
    {
        public static readonly BaseLanguageSemantics Java = new JavaLanguageSemantics();
        public static readonly BaseLanguageSemantics Swift = new SwiftLanguageSemantics();
        public static readonly BaseLanguageSemantics TypeScript = new TypeScriptLanguageSemantics();
        public static readonly BaseLanguageSemantics JavaScript = TypeScript;

        public static BaseLanguageSemantics Of(string langName)
        {
            switch(langName)
            {
                case ArtifactTargetLang.Java:
                    return Java;

                case ArtifactTargetLang.Swift:
                    return Swift;
                    
                case ArtifactTargetLang.TypeScript:
                    return TypeScript;

                case ArtifactTargetLang.JavaScript:
                    return JavaScript;

                default:
                    return null;
            }
        }
    }


}
//     using Sempiler.AST;
//     using Sempiler.AST.Diagnostics;
//     using System.Collections.Generic;
//     using System.Threading;

//     public class Scope 
//     {
//         public Node Start;
//         public Dictionary<string, Node> Declarations;
//         // public Dictionary<string, List<Node>> References;
//         // public List<Scope> Children;

//         // public Scope Parent { get => null; }
    
//         public Scope(Node start)
//         {
//             Start = start;
//             Declarations = new Dictionary<string, Node>();
//             // References = new Dictionary<string, List<Node>>();
//         }
//     }

//     public static class LanguageHelpers
//     {




//         /*
//             import { someFunction } from "./some-other-file.ts"

//         // need to rewrite import symbols as just qualified names where they are used.. qualified by the NODE ID we use for the inlining


//             function MyComponent(props : SomeType) : View
//             {   
//                 const a : number = props.a; 
//                 const b : boolean = props.b;


//             }

        
        
        
        

        
//          */

        

//         // public static void GetTypeOfExpression(Session session, RawAST ast, Node node, Dictionary<SemanticKind, object> scopeBoundaries, System.Threading.CancellationToken token)
//         // {
//         //     var scope = GetEnclosingScope(session, ast, node, scopeBoundaries, token);

//         //     if(scope.Declarations.ContainsKey("foo"))        
//         //     {
//         //         var decl = scope.Declarations["foo"];

//         //         if(decl.Kind == SemanticKind.DataValueDeclaration)
//         //         {
//         //             // use dv.Type ?? GetTypeOfExpression(dv.Initializer);
//         //         }
//         //         else if(decl.Kind == SemanticKind.ParameterDeclaration)
//         //         {
//         //             // use p.Type ?? GetTypeOfExpression(p.Initializer) ... or is this p.DefaultValue???
//         //         }
//         //         else if(decl.Kind == SemanticKind.TypeParameterDeclaration)
//         //         {
//         //             // ?
//         //         }
//         //         else if(decl.Kind == SemanticKind.PropertyDeclaration)
//         //         {
//         //             // use p.Type ?? GetTypeOfExpression(p.Initializer)
//         //         }
//         //         else if(decl.Kind == SemanticKind.FieldDeclaration)
//         //         {
//         //             // use p.Type ??? GetTypeOfExpression(p.Initializer)
//         //         }
//         //         else if(decl.Kind == SemanticKind.ImportDeclaration)
//         //         {

//         //         }
//         //     }
//         // }

//         // // this should just return a name in the scope, not a node // IntrinsicString
//         // public static string GetScopedTypeOfExpression(Node node) 
//         // {  
//         //     switch(node.Kind)
//         //     {
//         //         case SemanticKind.BooleanConstant:
//         //         break;

//         //         case SemanticKind.NumericConstant:
//         //         break;

//         //         case SemanticKind.Null:
//         //         break;

//         //         case SemanticKind.StringConstant:
//         //         case SemanticKind.InterpolatedString:
//         //         case SemanticKind.InterpolatedStringConstant:
//         //         case SemanticKind.Concatenation:
//         //         // [dho] SEMANTICS! some languages do not treat assignments as value producing expressions - 22/06/19
//         //         case SemanticKind.ConcatenationAssignment:
//         //         break;

//         //         case SemanticKind.IncidentContextReference:
//         //         break;

//         //         case SemanticKind.SuperContextReference:
//         //         break;

//         //         case SemanticKind.Identifier:
//         //             // [dho] need to resolve identifier in scope
                
//         //             /*
//         //                 if(decl.Kind == SemanticKind.DataValueDeclaration)
//         //         {
//         //             // use dv.Type ?? GetTypeOfExpression(dv.Initializer);
//         //         }
//         //         else if(decl.Kind == SemanticKind.ParameterDeclaration)
//         //         {
//         //             // use p.Type ?? GetTypeOfExpression(p.Initializer) ... or is this p.DefaultValue???
//         //         }
//         //         else if(decl.Kind == SemanticKind.TypeParameterDeclaration)
//         //         {
//         //             // ?
//         //         }
//         //         else if(decl.Kind == SemanticKind.PropertyDeclaration)
//         //         {
//         //             // use p.Type ?? GetTypeOfExpression(p.Initializer)
//         //         }
//         //         else if(decl.Kind == SemanticKind.FieldDeclaration)
//         //         {
//         //             // use p.Type ??? GetTypeOfExpression(p.Initializer)
//         //         }
//         //         else if(decl.Kind == SemanticKind.ImportDeclaration)
//         //         {
//         //             // get component from tree... unless its a fake import "sempiler" etc
//         //             // or we clone the AST and inject the global decls as components in order to have it work on its own
//         //         }

//         //         member of Entity Destructuring ..
//         //              */
//         //         break;

//         //         case SemanticKind.Invocation:
//         //             // resolve the Subject to a declaration

//         //             // if the Subject is a qualified access, then you need to resolve the left most Incident,
//         //             // get all members of its type, then go down the chain extracting the next member in the sequence
//         //             // from each object


//         //             // eventually you get to a FunctionLikeDeclaration
//         //             // now we have to then see if it has an explicit type,
//         //             // if not, we have to get the encapsulating type (most specific type that covers all cases)
//         //             // for all ExplicitExits
//         //         break;


//         //         case SemanticKind.InvocationArgument:
//         //         break;


//         //         case SemanticKind.BridgeInvocation:
//         //         break;

//         //         case SemanticKind.NamedTypeConstruction:
//         //         break;

//         //         case SemanticKind.ParameterDeclaration:
//         //         break;

//         //         case SemanticKind.ViewConstruction:
//         //         break;

//         //         case SemanticKind.TypeInterrogation:
//         //         break;

//         //         case SemanticKind.PredicateFlat:
//         //         break;

//         //         default:{
//         //             if(IsAssignmentLike(node))
//         //             {

//         //             }
//         //             else if(IsArithmeticExpressionLike(node))
//         //             {

//         //             }
//         //             else if(IsLogicExpressionLike(node))
//         //             {
//         //                 // boolean;
//         //             }
//         //         }
//         //     }
//         // }


//         // public static bool JavaTreatsAsSymbolReference(RawAST ast, Node node)
//         // {

//         //     if(node.Kind == SemanticKind.Identifier)
//         //     {
//         //         if(JavaTreatsAsValue(ast, node))
//         //         {
//         //             return true;
//         //         }
//         //         else
//         //         {
//         //             var pos = ASTHelpers.GetPosition(ast, node.ID);

//         //             var parent = pos.Parent;

//         //             if(parent == null) return false;

//         //             // [dho] if its a qualified access, check it is the leftmost symbol - 22/06/19
//         //             if(parent.Kind == SemanticKind.QualifiedAccess && pos.Role == SemanticRole.Incident)
//         //             {
//         //                 arghgghhghghgghghg();

//         //                 var grandparent = ASTHelpers.GetPosition(ast, parent.ID).Parent;

//         //                 return grandparent?.Kind != SemanticKind.QualifiedAccess;
//         //             }

//         //             if(pos.Role == SemanticRole.Name || pos.Role == SemanticRole.Storage)
//         //             {
//         //                 // [dho] filter out cases where it is just the name used for an
//         //                 // a declaration
//         //                 return !JavaTreatsAsDeclaration(ast, parent);
//         //             }

//         //             if(pos.Role == SemanticRole.Subject)
//         //             {
//         //                 return true;
//         //             }

//         //             if(pos.Role == SemanticRole.Member)
//         //             {
//         //                 return parent.Kind == SemanticKind.EntityDestructuring;
//         //             }
//         //         }
//         //     }


//         //     // if(node.Kind == SemanticKind.Identifier)
//         //     // {
//         //     //     var pos = ASTHelpers.GetPosition(ast, node.ID);

//         //     //     var parent = pos.Parent;

//         //     //     if(parent == null) return false;

//         //     //     // [dho] `x` in `y.x` - 22/06/19
//         //     //     if(parent.Kind == SemanticKind.QualifiedAccess)
//         //     //     {
//         //     //         return true;
//         //     //     }
//         //     //     // [dho] `x` in `x()` - 22/06/19
//         //     //     else if(pos.Role == SemanticRole.Subject && parent.Kind == SemanticKind.Invocation)
//         //     //     {
//         //     //         return true;
//         //     //     }
//         //     //     // [dho] eg. `x` in `y(x)` - 22/06/19
//         //     //     else if(JavaTreatsAsValue(ast, node))
//         //     //     {
//         //     //         return true;
//         //     //     }
//         //     //     // [dho] `x` in `x = y`- 22/06/19
//         //     //     else if(pos.Role == SemanticRole.Storage && IsAssignmentLike(parent))
//         //     //     {
//         //     //         return true;
//         //     //     }
//         //     //     // [dho] `x` in `new x()` - 22/06/19
//         //     //     else if(pos.Role == SemanticRole.Name && parent.Kind == SemanticKind.NamedTypeConstruction)
//         //     //     {
//         //     //         return true;
//         //     //     }
//         //     //     // [dho] `x` in  `const y : x` - 22/06/19
//         //     //     else if(pos.Role == SemanticRole.Name && parent.Kind == SemanticKind.NamedTypeReference)
//         //     //     {   
//         //     //         return true;
//         //     //     }
//         //     // }

//         //     return false;
//         // }

        


       
//     }
// }