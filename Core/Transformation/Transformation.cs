using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using System.Runtime.CompilerServices; 
using System.Threading;
using System.Threading.Tasks;

namespace Sempiler.Transformation
{
    using static Sempiler.Diagnostics.DiagnosticsHelpers;
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;
    // [dho] NOTE intentional value type (struct) so we can quick shallow copies - 29/08/18
    public struct Context 
    {
        public Artifact Artifact;

        public Session Session;
        public RawAST AST;

        public Domain Domain;
        public Component Component;
        public Node Parent;
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
    }


    public interface ITransformer
    {
        Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token);
    }

    // public abstract class BaseTransformer : ITransformer
    // {
    //     protected readonly string[] DiagnosticTags;

    //     protected BaseTransformer(string[] diagnosticTags = null)
    //     {
    //         DiagnosticTags = diagnosticTags;
    //     }

    //     public virtual Task<Diagnostics.Result<RawAST>> Transform(Session session, Artifact artifact, RawAST ast, CancellationToken token)
    //     {
    //         var result = new Result<RawAST>();

    //         var clonedAST = ast.Clone();

    //         var context = new Context 
    //         {
    //             Artifact = artifact,
    //             Session = session,
    //             AST = clonedAST
    //         };

    //         var root = ASTHelpers.GetRoot(clonedAST);

    //         var transformedRoot = result.AddMessages(TransformNode(root, context, token));

    //         if(!HasErrors(result))
    //         {
    //             result.Value = clonedAST;
    //         }

    //         return Task.FromResult(result);
    //     }

    //     public virtual Result<Node> TransformNode(Node node, Context context, CancellationToken token)
    //     {
    //         switch(node.Kind)
    //         {
    //             case SemanticKind.Addition:
    //                 return TransformAddition(NodeInterrogation.Addition(context.AST, node), context, token);

    //             case SemanticKind.ArrayTypeReference:
    //                 return TransformArrayTypeReference(NodeInterrogation.ArrayTypeReference(context.AST, node), context, token);

    //             case SemanticKind.Association:
    //                 return TransformAssociation(NodeInterrogation.Association(context.AST, node), context, token);

    //             case SemanticKind.Block:
    //                 return TransformBlock(NodeInterrogation.Block(context.AST, node), context, token);

    //             case SemanticKind.BooleanConstant:
    //                 return TransformBooleanConstant(NodeInterrogation.BooleanConstant(context.AST, (DataNode<bool>)node), context, token);

    //             // case SemanticKind.BooleanTypeReference:
    //             //     return TransformBooleanTypeReference(NodeInterrogation.IBooleanTypeReference(context.AST, node), context, token);

    //             case SemanticKind.CollectionDestructuring:
    //                 return TransformCollectionDestructuring(NodeInterrogation.CollectionDestructuring(context.AST, node), context, token);

    //             case SemanticKind.Component:
    //                 return TransformComponent(NodeInterrogation.Component(context.AST, (DataNode<string>)node), context, token);

    //             case SemanticKind.ConstructorDeclaration:
    //                 return TransformConstructorDeclaration(NodeInterrogation.ConstructorDeclaration(context.AST, node), context, token);

    //             case SemanticKind.DefaultExportReference:
    //                 return TransformDefaultExportReference(NodeInterrogation.DefaultExportReference(context.AST, node), context, token);

    //             case SemanticKind.DestructorDeclaration:
    //                 return TransformDestructorDeclaration(NodeInterrogation.DestructorDeclaration(context.AST, node), context, token);

    //             case SemanticKind.DestructuredMember:
    //                 return TransformDestructuredMember(NodeInterrogation.DestructuredMember(context.AST, node), context, token);

    //             case SemanticKind.Division:
    //                 return TransformDivision(NodeInterrogation.Division(context.AST, node), context, token);

    //             case SemanticKind.Domain:
    //                 return TransformDomain(NodeInterrogation.Domain(context.AST, node), context, token);

    //             case SemanticKind.EntityDestructuring:
    //                 return TransformEntityDestructuring(NodeInterrogation.EntityDestructuring(context.AST, node), context, token);

    //             // case SemanticKind.DoublePrecisionNumberTypeReference:
    //             //     return TransformDoublePrecisionNumberTypeReference(NodeInterrogation.IDoublePrecisionNumberTypeReference(context.AST, node), context, token);

    //             case SemanticKind.EnumerationTypeDeclaration:
    //                 return TransformEnumerationTypeDeclaration(NodeInterrogation.EnumerationTypeDeclaration(context.AST, node), context, token);

    //             case SemanticKind.EvalToVoid:
    //                 return TransformEvalToVoid(NodeInterrogation.EvalToVoid(context.AST, node), context, token);

    //             case SemanticKind.Exponentiation:
    //                 return TransformExponentiation(NodeInterrogation.Exponentiation(context.AST, node), context, token);

    //             case SemanticKind.ExportDeclaration:
    //                 return TransformExportDeclaration(NodeInterrogation.ExportDeclaration(context.AST, node), context, token);

    //             // case SemanticKind.FloatingPointNumberTypeReference:
    //             //     return TransformFloatingPointNumberTypeReference(NodeInterrogation.IFloatingPointNumberTypeReference(context.AST, node), context, token);

    //             case SemanticKind.FunctionDeclaration:
    //                 return TransformFunctionDeclaration(NodeInterrogation.FunctionDeclaration(context.AST, node), context, token);

    //             case SemanticKind.FunctionTermination:
    //                 return TransformFunctionTermination(NodeInterrogation.FunctionTermination(context.AST, node), context, token);

    //             case SemanticKind.FunctionTypeReference:
    //                 return TransformFunctionTypeReference(NodeInterrogation.FunctionTypeReference(context.AST, node), context, token);

    //             case SemanticKind.Identifier:
    //                 return TransformIdentifier(NodeInterrogation.Identifier(context.AST, node), context, token);

    //                 // case SemanticKind.Identity:
    //                 //     return TransformIdentity(NodeInterrogation.Identity(context.AST, node), context, token);

    //             case SemanticKind.ImportDeclaration:
    //                 return TransformImportDeclaration(NodeInterrogation.ImportDeclaration(context.AST, node), context, token);

    //             // case SemanticKind.IntegralNumberTypeReference:
    //             //     return TransformIntegralNumberTypeReference(NodeInterrogation.IIntegralNumberTypeReference(context.AST, node), context, token);

    //             case SemanticKind.InterfaceDeclaration:
    //                 return TransformInterfaceDeclaration(NodeInterrogation.InterfaceDeclaration(context.AST, node), context, token);

    //             case SemanticKind.InterpolatedString:
    //                 return TransformInterpolatedString(NodeInterrogation.InterpolatedString(context.AST, node), context, token);

    //             case SemanticKind.InterpolatedStringConstant:
    //                 return TransformInterpolatedStringConstant(NodeInterrogation.InterpolatedStringConstant(context.AST, (DataNode<string>)node), context, token);

    //             case SemanticKind.Invocation:
    //                 return TransformInvocation(NodeInterrogation.Invocation(context.AST, node), context, token);

    //             case SemanticKind.Label:
    //                 return TransformLabel(NodeInterrogation.Label(context.AST, node), context, token);

    //             case SemanticKind.MappedDestructuring:
    //                 return TransformMappedDestructuring(NodeInterrogation.MappedDestructuring(context.AST, node), context, token);

    //             case SemanticKind.MethodDeclaration:
    //                 return TransformMethodDeclaration(NodeInterrogation.MethodDeclaration(context.AST, node), context, token);

    //             case SemanticKind.Multiplication:
    //                 return TransformMultiplication(NodeInterrogation.Multiplication(context.AST, node), context, token);

    //                 // case SemanticKind.Negation:
    //                 //     return TransformNegation(NodeInterrogation.INegation(context.AST, node), context, token);


    //             case SemanticKind.NamedTypeReference:
    //                 return TransformNamedTypeReference(NodeInterrogation.NamedTypeReference(context.AST, node), context, token);

    //             case SemanticKind.NamespaceDeclaration:
    //                 return TransformNamespaceDeclaration(NodeInterrogation.NamespaceDeclaration(context.AST, node), context, token);

    //             case SemanticKind.NumericConstant:
    //                 return TransformNumericConstant(NodeInterrogation.NumericConstant(context.AST, (DataNode<string>)node), context, token);

    //             case SemanticKind.ObjectTypeDeclaration:
    //                 return TransformObjectTypeDeclaration(NodeInterrogation.ObjectTypeDeclaration(context.AST, node), context, token);

    //                 // [dho] if a node has a group it should use TransformDelimited - 20/09/18
    //                 // case SemanticKind.OrderedGroup:
    //                 //     return TransformOrderedGroup(NodeInterrogation.OrderedGroup(context.AST, node), context, token);

    //             case SemanticKind.ParameterDeclaration:
    //                 return TransformParameterDeclaration(NodeInterrogation.ParameterDeclaration(context.AST, node), context, token);

    //             case SemanticKind.ParenthesizedTypeReference:
    //                 return TransformParenthesizedTypeReference(NodeInterrogation.ParenthesizedTypeReference(context.AST, node), context, token);

    //             case SemanticKind.PostDecrement:
    //                 return TransformPostDecrement(NodeInterrogation.PostDecrement(context.AST, node), context, token);

    //             case SemanticKind.PreDecrement:
    //                 return TransformPreDecrement(NodeInterrogation.PreDecrement(context.AST, node), context, token);

    //             case SemanticKind.PostIncrement:
    //                 return TransformPostIncrement(NodeInterrogation.PostIncrement(context.AST, node), context, token);

    //             case SemanticKind.PreIncrement:
    //                 return TransformPreIncrement(NodeInterrogation.PreIncrement(context.AST, node), context, token);


    //             case SemanticKind.QualifiedAccess:
    //                 return TransformQualifiedAccess(NodeInterrogation.QualifiedAccess(context.AST, node), context, token);

    //             case SemanticKind.ReferenceAliasDeclaration:
    //                 return TransformReferenceAliasDeclaration(NodeInterrogation.ReferenceAliasDeclaration(context.AST, node), context, token);

    //             case SemanticKind.Remainder:
    //                 return TransformRemainder(NodeInterrogation.Remainder(context.AST, node), context, token);

    //             case SemanticKind.SpreadDestructuring:
    //                 return TransformSpreadDestructuring(NodeInterrogation.SpreadDestructuring(context.AST, node), context, token);

    //             case SemanticKind.StringConstant:
    //                 return TransformStringConstant(NodeInterrogation.StringConstant(context.AST, (DataNode<string>)node), context, token);

    //             // case SemanticKind.StringTypeReference:
    //             //     return TransformStringTypeReference(NodeInterrogation.IStringTypeReference(context.AST, node), context, token);

    //             case SemanticKind.Subtraction:
    //                 return TransformSubtraction(NodeInterrogation.Subtraction(context.AST, node), context, token);

    //             case SemanticKind.TypeAliasDeclaration:
    //                 return TransformTypeAliasDeclaration(NodeInterrogation.TypeAliasDeclaration(context.AST, node), context, token);

    //             case SemanticKind.WildcardExportReference:
    //                 return TransformWildcardExportReference(NodeInterrogation.WildcardExportReference(context.AST, node), context, token);

    //             // case SemanticKind.ValueTypeDeclaration:
    //             //     return TransformValueTypeDeclaration(NodeInterrogation.ValueTypeDeclaration(context.AST, node), context, token);

    //             default:
    //                 {
    //                     var result = new Result<Node>();

    //                     result.AddMessages(
    //                         new NodeMessage(MessageKind.Error, $"Unsupported kind for node '{node.Kind}'", node)
    //                     {
    //                         Hint = GetHint(node.Origin),
    //                         Tags = DiagnosticTags
    //                     }
    //                     );

    //                     return result;
    //                 }

    //         }
    //     }

    //     public virtual Result<Node> TransformDomain(Domain nodeWrapper, Context context, CancellationToken token)
    //     {
    //         var childContext = ContextHelpers.Clone(context);
    //         childContext.Domain = nodeWrapper;
    //         // // childContext.Parent = nodeWrapper.Node;

    //         return TransformChildren(nodeWrapper, childContext, token);
    //     }

    //     public virtual Result<Node> TransformComponent(Component node, Context context, CancellationToken token)
    //     {
    //         var childContext = ContextHelpers.Clone(context);
    //         childContext.Component = node;
    //         // // childContext.Parent = node.Node;

    //         return TransformChildren(node, childContext, token);
    //     }

    //     public virtual Result<Node> TransformFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformInvocation(Invocation node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformIdentifier(Identifier node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }


    //     public virtual Result<Node> TransformDefaultExportReference(DefaultExportReference node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformWildcardExportReference(WildcardExportReference node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformBlock(Block node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformLabel(Label node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     #region constants

    //     public virtual Result<Node> TransformBooleanConstant(BooleanConstant node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformNumericConstant(NumericConstant node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformStringConstant(StringConstant node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformInterpolatedString(InterpolatedString node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformInterpolatedStringConstant(InterpolatedStringConstant node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     #endregion

    //     // #region intrinsics

    //     // public virtual Result<Node> TransformBooleanTypeReference(IBooleanTypeReference node, Context context, CancellationToken token)
    //     // {
    //     //     return TransformChildren(node, context, token);
    //     // }

    //     // public virtual Result<Node> TransformDoublePrecisionNumberTypeReference(IDoublePrecisionNumberTypeReference node, Context context, CancellationToken token)
    //     // {
    //     //     return TransformChildren(node, context, token);
    //     // }

    //     // public virtual Result<Node> TransformFloatingPointNumberTypeReference(IFloatingPointNumberTypeReference node, Context context, CancellationToken token)
    //     // {
    //     //     return TransformChildren(node, context, token);
    //     // }

    //     // public virtual Result<Node> TransformIntegralNumberTypeReference(IIntegralNumberTypeReference node, Context context, CancellationToken token)
    //     // {
    //     //    return TransformChildren(node, context, token);
    //     // }

    //     // public virtual Result<Node> TransformStringTypeReference(IStringTypeReference node, Context context, CancellationToken token)
    //     // {
    //     //    return TransformChildren(node, context, token);
    //     // }

    //     // #endregion

    //     #region arithmetic

    //     public virtual Result<Node> TransformPostDecrement(PostDecrement node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformPreDecrement(PreDecrement node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformPostIncrement(PostIncrement node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformPreIncrement(PreIncrement node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformAddition(Addition node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformDivision(Division node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformExponentiation(Exponentiation node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformMultiplication(Multiplication node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }


    //     public virtual Result<Node> TransformRemainder(Remainder node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformSubtraction(Subtraction node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     #endregion

    //     #region destructuring

    //     public virtual Result<Node> TransformCollectionDestructuring(CollectionDestructuring node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformEntityDestructuring(EntityDestructuring node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }  

    //     public virtual Result<Node> TransformDestructuredMember(DestructuredMember node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformMappedDestructuring(MappedDestructuring node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformReferenceAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformSpreadDestructuring(SpreadDestructuring node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     #endregion


    //     public virtual Result<Node> TransformFunctionTermination(FunctionTermination node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformAssociation(Association node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }
    
    //     public virtual Result<Node> TransformNamespaceDeclaration(NamespaceDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }



    //     // public virtual Result<Node> TransformValueTypeDeclaration(ValueTypeDeclaration node, Context context, CancellationToken token)
    //     // {
    //     //     return TransformChildren(node, context, token);
    //     // }

    //     public virtual Result<Node> TransformConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformFunctionTypeReference(FunctionTypeReference node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     public virtual Result<Node> TransformParenthesizedTypeReference(ParenthesizedTypeReference node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }
    
    //     public virtual Result<Node> TransformEvalToVoid(EvalToVoid node, Context context, CancellationToken token)
    //     {
    //         return TransformChildren(node, context, token);
    //     }

    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     protected Result<Node> TransformChildren(NodeWrapper nodeWrapper, Context context, CancellationToken token) => TransformChildren(nodeWrapper.Node, context, token);

    //     [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //     protected Result<Node> TransformChildren(Node node, Context context, CancellationToken token)
    //     {
    //         var result = new Result<Node>();

    //         var childContext = ContextHelpers.Clone(context);
    //         // // childContext.Parent = node;

    //         var newChildren = default(Node[]);

    //         if(node.Children != null)
    //         {
    //             ASTHelpers.ReplaceNode(ast, outgoingID, newNode);

    //             newChildren = new Node[node.Children.Length];

    //             for(int i = 0; i < node.Children?.Length; ++i)
    //             {
    //                 var currentChild = node.Children[i];

    //                 // [dho] child might be null if a type has a field that is optional,
    //                 // but stores the value for that field as an Node - 27/09/18
    //                 if(currentChild != null)
    //                 {
    //                     var r = TransformNode(currentChild, childContext, token);

    //                     result.AddMessages(r.Messages);

    //                     // [dho] I'm nervous about only doing this if we detect the child
    //                     // has actually transformed (been replaced) in case `Transform___` functions
    //                     // herein only return a property of the Node, and return a Node that ostensibly
    //                     // looks unchanged because the ID is the same - 24/09/18
    //                     newChildren[i] = r.Value;
    //                 }
    //                 else
    //                 {
    //                     newChildren[i] = null;
    //                 }


                    
    //                 if(token.IsCancellationRequested)
    //                 {
    //                     break;
    //                 }
    //             }

    //         }


    //         if(!HasErrors(result))
    //         {
    //             result.Value = new Node(newID, node.Kind, node.Origin, node.Meta.Clone(), newChildren);
    //         }

    //         return result;
    //     }
    // }
}