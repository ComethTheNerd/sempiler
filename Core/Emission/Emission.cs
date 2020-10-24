using Sempiler.AST;
using Sempiler.AST.Diagnostics;
using Sempiler.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices; 
using System.Threading;
using System.Threading.Tasks;

namespace Sempiler.Emission
{
    using static Sempiler.AST.Diagnostics.DiagnosticsHelpers;

    // [dho] NOTE intentional value type (struct) so we can quick shallow copies - 29/08/18
    public struct Context 
    {
        public Session Session;

        public Artifact Artifact;
        public Shard Shard;
        public RawAST AST;

        public Domain Domain;
        public Component Component;
        // public NodeWrapper Parent;

        public OutFileCollection OutFileCollection;

        public IEmission Emission;

        // [dho] public ILinterConfig Linter settings

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

    public interface IEmitter 
    {
        // IEnumerable so it can support lazy collections and we can quit out early if needs be
        Task<Diagnostics.Result<OutFileCollection>> Emit(Session session, Artifact artifact, Shard shard, RawAST ast, Node node, CancellationToken token);
    }

    public abstract class BaseEmitter : IEmitter
    {
        protected readonly string[] DiagnosticTags;

        public string FileExtension = string.Empty;

        protected BaseEmitter(string[] diagnosticTags = null)
        {
            DiagnosticTags = diagnosticTags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Result<object> IgnoreNode(AST.Node node)
        {
            return new Result<object>();
        }

        protected delegate Result<object> EmitDelegate<T>(T node, Context context, CancellationToken token);


        public virtual Task<Result<OutFileCollection>> Emit(Session session, Artifact artifact, Shard shard, RawAST ast, Node node, CancellationToken token)
        {
            var result = new Result<OutFileCollection>();

            var outFileCollection = new OutFileCollection();

            var context = new Context 
            {
                Session = session,
                Artifact = artifact,
                Shard = shard,
                AST = ast,
                OutFileCollection = outFileCollection
            };

            var root = ASTHelpers.GetRoot(ast);

            result.AddMessages(EmitNode(root, context, token));

            result.Value = outFileCollection;

            return Task.FromResult(result);
        }

        public virtual Result<object> EmitNode(Node node, Context context, CancellationToken token)
        {
            switch(node.Kind)
            {
                case SemanticKind.AccessorDeclaration:
                    return EmitAccessorDeclaration(ASTNodeFactory.AccessorDeclaration(context.AST, node), context, token);

                case SemanticKind.AccessorSignature:
                    return EmitAccessorSignature(ASTNodeFactory.AccessorSignature(context.AST, node), context, token);

                case SemanticKind.Addition:
                    return EmitAddition(ASTNodeFactory.Addition(context.AST, node), context, token);

                case SemanticKind.AdditionAssignment:
                    return EmitAdditionAssignment(ASTNodeFactory.AdditionAssignment(context.AST, node), context, token);

                case SemanticKind.AddressOf:
                    return EmitAddressOf(ASTNodeFactory.AddressOf(context.AST, node), context, token);

                case SemanticKind.Annotation:
                    return EmitAnnotation(ASTNodeFactory.Annotation(context.AST, node), context, token);

                case SemanticKind.ArithmeticNegation:
                    return EmitArithmeticNegation(ASTNodeFactory.ArithmeticNegation(context.AST, node), context, token);

                case SemanticKind.ArrayConstruction:
                    return EmitArrayConstruction(ASTNodeFactory.ArrayConstruction(context.AST, node), context, token);

                case SemanticKind.ArrayTypeReference:
                    return EmitArrayTypeReference(ASTNodeFactory.ArrayTypeReference(context.AST, node), context, token);

                case SemanticKind.Assignment:
                    return EmitAssignment(ASTNodeFactory.Assignment(context.AST, node), context, token);

                case SemanticKind.Association:
                    return EmitAssociation(ASTNodeFactory.Association(context.AST, node), context, token);

                case SemanticKind.BitwiseAnd:
                    return EmitBitwiseAnd(ASTNodeFactory.BitwiseAnd(context.AST, node), context, token);

                case SemanticKind.BitwiseAndAssignment:
                    return EmitBitwiseAndAssignment(ASTNodeFactory.BitwiseAndAssignment(context.AST, node), context, token);

                case SemanticKind.BitwiseExclusiveOr:
                    return EmitBitwiseExclusiveOr(ASTNodeFactory.BitwiseExclusiveOr(context.AST, node), context, token);

                case SemanticKind.BitwiseExclusiveOrAssignment:
                    return EmitBitwiseExclusiveOrAssignment(ASTNodeFactory.BitwiseExclusiveOrAssignment(context.AST, node), context, token);

                case SemanticKind.BitwiseLeftShift:
                    return EmitBitwiseLeftShift(ASTNodeFactory.BitwiseLeftShift(context.AST, node), context, token);

                case SemanticKind.BitwiseLeftShiftAssignment:
                    return EmitBitwiseLeftShiftAssignment(ASTNodeFactory.BitwiseLeftShiftAssignment(context.AST, node), context, token);

                case SemanticKind.BitwiseNegation:
                    return EmitBitwiseNegation(ASTNodeFactory.BitwiseNegation(context.AST, node), context, token);

                case SemanticKind.BitwiseOr:
                    return EmitBitwiseOr(ASTNodeFactory.BitwiseOr(context.AST, node), context, token);

                case SemanticKind.BitwiseOrAssignment:
                    return EmitBitwiseOrAssignment(ASTNodeFactory.BitwiseOrAssignment(context.AST, node), context, token);

                case SemanticKind.BitwiseRightShift:
                    return EmitBitwiseRightShift(ASTNodeFactory.BitwiseRightShift(context.AST, node), context, token);

                case SemanticKind.BitwiseRightShiftAssignment:
                    return EmitBitwiseRightShiftAssignment(ASTNodeFactory.BitwiseRightShiftAssignment(context.AST, node), context, token);

                case SemanticKind.BitwiseUnsignedRightShift:
                    return EmitBitwiseUnsignedRightShift(ASTNodeFactory.BitwiseUnsignedRightShift(context.AST, node), context, token);

                case SemanticKind.BitwiseUnsignedRightShiftAssignment:
                    return EmitBitwiseUnsignedRightShiftAssignment(ASTNodeFactory.BitwiseUnsignedRightShiftAssignment(context.AST, node), context, token);

                case SemanticKind.Block:
                    return EmitBlock(ASTNodeFactory.Block(context.AST, node), context, token);

                case SemanticKind.BooleanConstant:
                    return EmitBooleanConstant(ASTNodeFactory.BooleanConstant(context.AST, (DataNode<bool>)node), context, token);

                case SemanticKind.Breakpoint:
                    return EmitBreakpoint(ASTNodeFactory.Breakpoint(context.AST, node), context, token);

                case SemanticKind.ClauseBreak:
                    return EmitClauseBreak(ASTNodeFactory.ClauseBreak(context.AST, node), context, token);

                case SemanticKind.CodeConstant:
                    return EmitCodeConstant(ASTNodeFactory.CodeConstant(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.CollectionDestructuring:
                    return EmitCollectionDestructuring(ASTNodeFactory.CollectionDestructuring(context.AST, node), context, token);

                case SemanticKind.CompilerHint:
                    return EmitCompilerHint(ASTNodeFactory.CompilerHint(context.AST, node), context, token);

                case SemanticKind.Component:
                    return EmitComponent(ASTNodeFactory.Component(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.ComputedValue:
                    return EmitComputedValue(ASTNodeFactory.ComputedValue(context.AST, node), context, token);

                case SemanticKind.Concatenation:
                    return EmitConcatenation(ASTNodeFactory.Concatenation(context.AST, node), context, token);

                case SemanticKind.ConcatenationAssignment:
                    return EmitConcatenationAssignment(ASTNodeFactory.ConcatenationAssignment(context.AST, node), context, token);

                case SemanticKind.ConditionalTypeReference:
                    return EmitConditionalTypeReference(ASTNodeFactory.ConditionalTypeReference(context.AST, node), context, token);

                case SemanticKind.LiteralTypeReference:
                    return EmitLiteralTypeReference(ASTNodeFactory.LiteralTypeReference(context.AST, node), context, token);

                case SemanticKind.ConstructorDeclaration:
                    return EmitConstructorDeclaration(ASTNodeFactory.ConstructorDeclaration(context.AST, node), context, token);

                case SemanticKind.ConstructorSignature:
                    return EmitConstructorSignature(ASTNodeFactory.ConstructorSignature(context.AST, node), context, token);

                case SemanticKind.ConstructorTypeReference:
                    return EmitConstructorTypeReference(ASTNodeFactory.ConstructorTypeReference(context.AST, node), context, token);

                case SemanticKind.DataValueDeclaration:
                    return EmitDataValueDeclaration(ASTNodeFactory.DataValueDeclaration(context.AST, node), context, token);

                case SemanticKind.DefaultExportReference:
                    return EmitDefaultExportReference(ASTNodeFactory.DefaultExportReference(context.AST, node), context, token);

                case SemanticKind.Destruction:
                    return EmitDestruction(ASTNodeFactory.Destruction(context.AST, node), context, token);

                case SemanticKind.DestructorDeclaration:
                    return EmitDestructorDeclaration(ASTNodeFactory.DestructorDeclaration(context.AST, node), context, token);

                case SemanticKind.DestructorSignature:
                    return EmitDestructorSignature(ASTNodeFactory.DestructorSignature(context.AST, node), context, token);

                case SemanticKind.DestructuredMember:
                    return EmitDestructuredMember(ASTNodeFactory.DestructuredMember(context.AST, node), context, token);

                case SemanticKind.DictionaryConstruction:
                    return EmitDictionaryConstruction(ASTNodeFactory.DictionaryConstruction(context.AST, node), context, token);

                case SemanticKind.DictionaryTypeReference:
                    return EmitDictionaryTypeReference(ASTNodeFactory.DictionaryTypeReference(context.AST, node), context, token);
                
                case SemanticKind.Directive:
                    return EmitDirective(ASTNodeFactory.Directive(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.Division:
                    return EmitDivision(ASTNodeFactory.Division(context.AST, node), context, token);

                case SemanticKind.DivisionAssignment:
                    return EmitDivisionAssignment(ASTNodeFactory.DivisionAssignment(context.AST, node), context, token);

                case SemanticKind.DoOrDieErrorTrap:
                    return EmitDoOrDieErrorTrap(ASTNodeFactory.DoOrDieErrorTrap(context.AST, node), context, token);

                case SemanticKind.DoOrRecoverErrorTrap:
                    return EmitDoOrRecoverErrorTrap(ASTNodeFactory.DoOrRecoverErrorTrap(context.AST, node), context, token);

                case SemanticKind.DoWhilePredicateLoop:
                    return EmitDoWhilePredicateLoop(ASTNodeFactory.DoWhilePredicateLoop(context.AST, node), context, token);

                case SemanticKind.Domain:
                    return EmitDomain(ASTNodeFactory.Domain(context.AST, node), context, token);

                case SemanticKind.DynamicTypeConstruction:
                    return EmitDynamicTypeConstruction(ASTNodeFactory.DynamicTypeConstruction(context.AST, node), context, token);

                case SemanticKind.DynamicTypeReference:
                    return EmitDynamicTypeReference(ASTNodeFactory.DynamicTypeReference(context.AST, node), context, token);

                case SemanticKind.EntityDestructuring:
                    return EmitEntityDestructuring(ASTNodeFactory.EntityDestructuring(context.AST, node), context, token);

                case SemanticKind.EnumerationMemberDeclaration:
                    return EmitEnumerationMemberDeclaration(ASTNodeFactory.EnumerationMemberDeclaration(context.AST, node), context, token);

                case SemanticKind.EnumerationTypeDeclaration:
                    return EmitEnumerationTypeDeclaration(ASTNodeFactory.EnumerationTypeDeclaration(context.AST, node), context, token);

                case SemanticKind.ErrorFinallyClause:
                    return EmitErrorFinallyClause(ASTNodeFactory.ErrorFinallyClause(context.AST, node), context, token);

                case SemanticKind.ErrorHandlerClause:
                    return EmitErrorHandlerClause(ASTNodeFactory.ErrorHandlerClause(context.AST, node), context, token);

                case SemanticKind.ErrorTrapJunction:
                    return EmitErrorTrapJunction(ASTNodeFactory.ErrorTrapJunction(context.AST, node), context, token);

                case SemanticKind.EvalToVoid:
                    return EmitEvalToVoid(ASTNodeFactory.EvalToVoid(context.AST, node), context, token);

                case SemanticKind.Exponentiation:
                    return EmitExponentiation(ASTNodeFactory.Exponentiation(context.AST, node), context, token);

                case SemanticKind.ExponentiationAssignment:
                    return EmitExponentiationAssignment(ASTNodeFactory.ExponentiationAssignment(context.AST, node), context, token);

                case SemanticKind.ExportDeclaration:
                    return EmitExportDeclaration(ASTNodeFactory.ExportDeclaration(context.AST, node), context, token);

                case SemanticKind.FieldDeclaration:
                    return EmitFieldDeclaration(ASTNodeFactory.FieldDeclaration(context.AST, node), context, token);

                case SemanticKind.FieldSignature:
                    return EmitFieldSignature(ASTNodeFactory.FieldSignature(context.AST, node), context, token);

                case SemanticKind.ForKeysLoop:
                    return EmitForKeysLoop(ASTNodeFactory.ForKeysLoop(context.AST, node), context, token);

                case SemanticKind.ForMembersLoop:
                    return EmitForMembersLoop(ASTNodeFactory.ForMembersLoop(context.AST, node), context, token);

                case SemanticKind.ForPredicateLoop:
                    return EmitForPredicateLoop(ASTNodeFactory.ForPredicateLoop(context.AST, node), context, token);

                case SemanticKind.ForcedCast:
                    return EmitForcedCast(ASTNodeFactory.ForcedCast(context.AST, node), context, token);

                case SemanticKind.FunctionDeclaration:
                    return EmitFunctionDeclaration(ASTNodeFactory.FunctionDeclaration(context.AST, node), context, token);

                case SemanticKind.FunctionTermination:
                    return EmitFunctionTermination(ASTNodeFactory.FunctionTermination(context.AST, node), context, token);

                case SemanticKind.FunctionTypeReference:
                    return EmitFunctionTypeReference(ASTNodeFactory.FunctionTypeReference(context.AST, node), context, token);

                case SemanticKind.GeneratorSuspension:
                    return EmitGeneratorSuspension(ASTNodeFactory.GeneratorSuspension(context.AST, node), context, token);

                case SemanticKind.GlobalDeclaration:
                    return EmitGlobalDeclaration(ASTNodeFactory.GlobalDeclaration(context.AST, node), context, token);

                case SemanticKind.Identifier:
                    return EmitIdentifier(ASTNodeFactory.Identifier(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.Identity:
                    return EmitIdentity(ASTNodeFactory.Identity(context.AST, node), context, token);


                case SemanticKind.ImportDeclaration:
                    return EmitImportDeclaration(ASTNodeFactory.ImportDeclaration(context.AST, node), context, token);

                case SemanticKind.IncidentContextReference:
                    return EmitIncidentContextReference(ASTNodeFactory.IncidentContextReference(context.AST, node), context, token);

                case SemanticKind.IncidentTypeConstraint:
                    return EmitIncidentTypeConstraint(ASTNodeFactory.IncidentTypeConstraint(context.AST, node), context, token);

                case SemanticKind.IndexTypeQuery:
                    return EmitIndexTypeQuery(ASTNodeFactory.IndexTypeQuery(context.AST, node), context, token);

                case SemanticKind.InferredTypeQuery:
                    return EmitInferredTypeQuery(ASTNodeFactory.InferredTypeQuery(context.AST, node), context, token);

                case SemanticKind.IndexedAccess:
                    return EmitIndexedAccess(ASTNodeFactory.IndexedAccess(context.AST, node), context, token);

                case SemanticKind.IndexerSignature:
                    return EmitIndexerSignature(ASTNodeFactory.IndexerSignature(context.AST, node), context, token);

                case SemanticKind.InterfaceDeclaration:
                    return EmitInterfaceDeclaration(ASTNodeFactory.InterfaceDeclaration(context.AST, node), context, token);

                case SemanticKind.InterimSuspension:
                    return EmitInterimSuspension(ASTNodeFactory.InterimSuspension(context.AST, node), context, token);

                case SemanticKind.InterpolatedString:
                    return EmitInterpolatedString(ASTNodeFactory.InterpolatedString(context.AST, node), context, token);

                case SemanticKind.InterpolatedStringConstant:
                    return EmitInterpolatedStringConstant(ASTNodeFactory.InterpolatedStringConstant(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.IntersectionTypeReference:
                    return EmitIntersectionTypeReference(ASTNodeFactory.IntersectionTypeReference(context.AST, node), context, token);

                case SemanticKind.IntrinsicTypeReference:
                    return EmitIntrinsicTypeReference(ASTNodeFactory.IntrinsicTypeReference(context.AST, node), context, token);

                case SemanticKind.Invocation:
                    return EmitInvocation(ASTNodeFactory.Invocation(context.AST, node), context, token);

                case SemanticKind.InvocationArgument:
                    return EmitInvocationArgument(ASTNodeFactory.InvocationArgument(context.AST, node), context, token);

                case SemanticKind.JumpToNextIteration:
                    return EmitJumpToNextIteration(ASTNodeFactory.JumpToNextIteration(context.AST, node), context, token);

                case SemanticKind.KeyValuePair:
                    return EmitKeyValuePair(ASTNodeFactory.KeyValuePair(context.AST, node), context, token);

                case SemanticKind.Label:
                    return EmitLabel(ASTNodeFactory.Label(context.AST, node), context, token);

                case SemanticKind.LambdaDeclaration:
                    return EmitLambdaDeclaration(ASTNodeFactory.LambdaDeclaration(context.AST, node), context, token);

                case SemanticKind.LogicalAnd:
                    return EmitLogicalAnd(ASTNodeFactory.LogicalAnd(context.AST, node), context, token);

                case SemanticKind.LogicalNegation:
                    return EmitLogicalNegation(ASTNodeFactory.LogicalNegation(context.AST, node), context, token);

                case SemanticKind.LogicalOr:
                    return EmitLogicalOr(ASTNodeFactory.LogicalOr(context.AST, node), context, token);

                case SemanticKind.LoopBreak:
                    return EmitLoopBreak(ASTNodeFactory.LoopBreak(context.AST, node), context, token);

                case SemanticKind.LooseEquivalent:
                    return EmitLooseEquivalent(ASTNodeFactory.LooseEquivalent(context.AST, node), context, token);

                case SemanticKind.LooseNonEquivalent:
                    return EmitLooseNonEquivalent(ASTNodeFactory.LooseNonEquivalent(context.AST, node), context, token);

                case SemanticKind.LowerBoundedTypeConstraint:
                    return EmitLowerBoundedTypeConstraint(ASTNodeFactory.LowerBoundedTypeConstraint(context.AST, node), context, token);

                case SemanticKind.MappedDestructuring:
                    return EmitMappedDestructuring(ASTNodeFactory.MappedDestructuring(context.AST, node), context, token);

                case SemanticKind.MappedTypeReference:
                    return EmitMappedTypeReference(ASTNodeFactory.MappedTypeReference(context.AST, node), context, token);

                case SemanticKind.MatchClause:
                    return EmitMatchClause(ASTNodeFactory.MatchClause(context.AST, node), context, token);

                case SemanticKind.MatchJunction:
                    return EmitMatchJunction(ASTNodeFactory.MatchJunction(context.AST, node), context, token);

                case SemanticKind.MaybeNull:
                    return EmitMaybeNull(ASTNodeFactory.MaybeNull(context.AST, node), context, token);

                case SemanticKind.MemberNameReflection:
                    return EmitMemberNameReflection(ASTNodeFactory.MemberNameReflection(context.AST, node), context, token);

                case SemanticKind.MemberTypeConstraint:
                    return EmitMemberTypeConstraint(ASTNodeFactory.MemberTypeConstraint(context.AST, node), context, token);

                case SemanticKind.MembershipTest:
                    return EmitMembershipTest(ASTNodeFactory.MembershipTest(context.AST, node), context, token);

                case SemanticKind.Meta:
                    return EmitMeta(ASTNodeFactory.Meta(context.AST, (DataNode<MetaFlag>)node), context, token);

                case SemanticKind.MetaProperty:
                    return EmitMetaProperty(ASTNodeFactory.MetaProperty(context.AST, node), context, token);

                case SemanticKind.MethodDeclaration:
                    return EmitMethodDeclaration(ASTNodeFactory.MethodDeclaration(context.AST, node), context, token);

                case SemanticKind.MethodSignature:
                    return EmitMethodSignature(ASTNodeFactory.MethodSignature(context.AST, node), context, token);

                case SemanticKind.Modifier:
                    return EmitModifier(ASTNodeFactory.Modifier(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.Multiplication:
                    return EmitMultiplication(ASTNodeFactory.Multiplication(context.AST, node), context, token);

                case SemanticKind.MultiplicationAssignment:
                    return EmitMultiplicationAssignment(ASTNodeFactory.MultiplicationAssignment(context.AST, node), context, token);

                case SemanticKind.MutatorDeclaration:
                    return EmitMutatorDeclaration(ASTNodeFactory.MutatorDeclaration(context.AST, node), context, token);

                case SemanticKind.MutatorSignature:
                    return EmitMutatorSignature(ASTNodeFactory.MutatorSignature(context.AST, node), context, token);

                case SemanticKind.Mutex:
                    return EmitMutex(ASTNodeFactory.Mutex(context.AST, node), context, token);

                case SemanticKind.NamedTypeConstruction:
                    return EmitNamedTypeConstruction(ASTNodeFactory.NamedTypeConstruction(context.AST, node), context, token);

                case SemanticKind.NamedTypeQuery:
                    return EmitNamedTypeQuery(ASTNodeFactory.NamedTypeQuery(context.AST, node), context, token);

                case SemanticKind.NamedTypeReference:
                    return EmitNamedTypeReference(ASTNodeFactory.NamedTypeReference(context.AST, node), context, token);

                case SemanticKind.NamespaceDeclaration:
                    return EmitNamespaceDeclaration(ASTNodeFactory.NamespaceDeclaration(context.AST, node), context, token);

                case SemanticKind.NamespaceReference:
                    return EmitNamespaceReference(ASTNodeFactory.NamespaceReference(context.AST, node), context, token);

                case SemanticKind.NonMembershipTest:
                    return EmitNonMembershipTest(ASTNodeFactory.NonMembershipTest(context.AST, node), context, token);

                case SemanticKind.NotNull:
                    return EmitNotNull(ASTNodeFactory.NotNull(context.AST, node), context, token);

                case SemanticKind.Nop:
                    return EmitNop(ASTNodeFactory.Nop(context.AST, node), context, token);

                case SemanticKind.NotNumber:
                    return EmitNotNumber(ASTNodeFactory.NotNumber(context.AST, node), context, token);

                case SemanticKind.Null:
                    return EmitNull(ASTNodeFactory.Null(context.AST, node), context, token);

                case SemanticKind.NullCoalescence:
                    return EmitNullCoalescence(ASTNodeFactory.NullCoalescence(context.AST, node), context, token);

                case SemanticKind.NumericConstant:
                    return EmitNumericConstant(ASTNodeFactory.NumericConstant(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.ObjectTypeDeclaration:
                    return EmitObjectTypeDeclaration(ASTNodeFactory.ObjectTypeDeclaration(context.AST, node), context, token);

                // case SemanticKind.OrderedGroup:
                //     return EmitOrderedGroup(NodeInterrogation.OrderedGroup(context.AST, node), context, token);

                case SemanticKind.ParameterDeclaration:
                    return EmitParameterDeclaration(ASTNodeFactory.ParameterDeclaration(context.AST, node), context, token);

                case SemanticKind.ParenthesizedTypeReference:
                    return EmitParenthesizedTypeReference(ASTNodeFactory.ParenthesizedTypeReference(context.AST, node), context, token);

                case SemanticKind.PointerDereference:
                    return EmitPointerDereference(ASTNodeFactory.PointerDereference(context.AST, node), context, token);

                case SemanticKind.PointerTypeReference:
                    return EmitPointerTypeReference(ASTNodeFactory.PointerTypeReference(context.AST, node), context, token);

                case SemanticKind.PostDecrement:
                    return EmitPostDecrement(ASTNodeFactory.PostDecrement(context.AST, node), context, token);

                case SemanticKind.PostIncrement:
                    return EmitPostIncrement(ASTNodeFactory.PostIncrement(context.AST, node), context, token);

                case SemanticKind.PreDecrement:
                    return EmitPreDecrement(ASTNodeFactory.PreDecrement(context.AST, node), context, token);

                case SemanticKind.PreIncrement:
                    return EmitPreIncrement(ASTNodeFactory.PreIncrement(context.AST, node), context, token);

                case SemanticKind.PredicateFlat:
                    return EmitPredicateFlat(ASTNodeFactory.PredicateFlat(context.AST, node), context, token);

                case SemanticKind.PredicateJunction:
                    return EmitPredicateJunction(ASTNodeFactory.PredicateJunction(context.AST, node), context, token);

                case SemanticKind.PrioritySymbolResolutionContext:
                    return EmitPrioritySymbolResolutionContext(ASTNodeFactory.PrioritySymbolResolutionContext(context.AST, node), context, token);

                case SemanticKind.PropertyDeclaration:
                    return EmitPropertyDeclaration(ASTNodeFactory.PropertyDeclaration(context.AST, node), context, token);

                case SemanticKind.QualifiedAccess:
                    return EmitQualifiedAccess(ASTNodeFactory.QualifiedAccess(context.AST, node), context, token);

                case SemanticKind.RaiseError:
                    return EmitRaiseError(ASTNodeFactory.RaiseError(context.AST, node), context, token);

                case SemanticKind.ReferenceAliasDeclaration:
                    return EmitReferenceAliasDeclaration(ASTNodeFactory.ReferenceAliasDeclaration(context.AST, node), context, token);

                case SemanticKind.RegularExpressionConstant:
                    return EmitRegularExpressionConstant(ASTNodeFactory.RegularExpressionConstant(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.Remainder:
                    return EmitRemainder(ASTNodeFactory.Remainder(context.AST, node), context, token);

                case SemanticKind.RemainderAssignment:
                    return EmitRemainderAssignment(ASTNodeFactory.RemainderAssignment(context.AST, node), context, token);

                case SemanticKind.SafeCast:
                    return EmitSafeCast(ASTNodeFactory.SafeCast(context.AST, node), context, token);

                case SemanticKind.SmartCast:
                    return EmitSmartCast(ASTNodeFactory.SmartCast(context.AST, node), context, token);

                case SemanticKind.SpreadDestructuring:
                    return EmitSpreadDestructuring(ASTNodeFactory.SpreadDestructuring(context.AST, node), context, token);

                case SemanticKind.StrictEquivalent:
                    return EmitStrictEquivalent(ASTNodeFactory.StrictEquivalent(context.AST, node), context, token);

                case SemanticKind.StrictGreaterThan:
                    return EmitStrictGreaterThan(ASTNodeFactory.StrictGreaterThan(context.AST, node), context, token);

                case SemanticKind.StrictGreaterThanOrEquivalent:
                    return EmitStrictGreaterThanOrEquivalent(ASTNodeFactory.StrictGreaterThanOrEquivalent(context.AST, node), context, token);

                case SemanticKind.StrictLessThan:
                    return EmitStrictLessThan(ASTNodeFactory.StrictLessThan(context.AST, node), context, token);

                case SemanticKind.StrictLessThanOrEquivalent:
                    return EmitStrictLessThanOrEquivalent(ASTNodeFactory.StrictLessThanOrEquivalent(context.AST, node), context, token);

                case SemanticKind.StrictNonEquivalent:
                    return EmitStrictNonEquivalent(ASTNodeFactory.StrictNonEquivalent(context.AST, node), context, token);

                case SemanticKind.StringConstant:
                    return EmitStringConstant(ASTNodeFactory.StringConstant(context.AST, (DataNode<string>)node), context, token);

                case SemanticKind.Subtraction:
                    return EmitSubtraction(ASTNodeFactory.Subtraction(context.AST, node), context, token);

                case SemanticKind.SubtractionAssignment:
                    return EmitSubtractionAssignment(ASTNodeFactory.SubtractionAssignment(context.AST, node), context, token);

                case SemanticKind.SuperContextReference:
                    return EmitSuperContextReference(ASTNodeFactory.SuperContextReference(context.AST, node), context, token);

                case SemanticKind.TupleConstruction:
                    return EmitTupleConstruction(ASTNodeFactory.TupleConstruction(context.AST, node), context, token);

                case SemanticKind.TupleTypeReference:
                    return EmitTupleTypeReference(ASTNodeFactory.TupleTypeReference(context.AST, node), context, token);

                case SemanticKind.TypeAliasDeclaration:
                    return EmitTypeAliasDeclaration(ASTNodeFactory.TypeAliasDeclaration(context.AST, node), context, token);

                case SemanticKind.TypeInterrogation:
                    return EmitTypeInterrogation(ASTNodeFactory.TypeInterrogation(context.AST, node), context, token);

                case SemanticKind.TypeParameterDeclaration:
                    return EmitTypeParameterDeclaration(ASTNodeFactory.TypeParameterDeclaration(context.AST, node), context, token);

                case SemanticKind.TypeQuery:
                    return EmitTypeQuery(ASTNodeFactory.TypeQuery(context.AST, node), context, token);

                case SemanticKind.TypeTest:
                    return EmitTypeTest(ASTNodeFactory.TypeTest(context.AST, node), context, token);

                case SemanticKind.UpperBoundedTypeConstraint:
                    return EmitUpperBoundedTypeConstraint(ASTNodeFactory.UpperBoundedTypeConstraint(context.AST, node), context, token);

                case SemanticKind.UnionTypeReference:
                    return EmitUnionTypeReference(ASTNodeFactory.UnionTypeReference(context.AST, node), context, token);

                case SemanticKind.ViewConstruction:
                    return EmitViewConstruction(ASTNodeFactory.ViewConstruction(context.AST, node), context, token);

                case SemanticKind.ViewDeclaration:
                    return EmitViewDeclaration(ASTNodeFactory.ViewDeclaration(context.AST, node), context, token);

                case SemanticKind.WhilePredicateLoop:
                    return EmitWhilePredicateLoop(ASTNodeFactory.WhilePredicateLoop(context.AST, node), context, token);

                case SemanticKind.WildcardExportReference:
                    return EmitWildcardExportReference(ASTNodeFactory.WildcardExportReference(context.AST, node), context, token);
                    
                default:
                {
                    var result = new Result<object>();

                    result.AddMessages(
                        new NodeMessage(MessageKind.Error, $"Unsupported kind for node '{node.Kind}'", node)
                        {
                            Hint = GetHint(node.Origin),
                            Tags = DiagnosticTags
                        }
                    );

                    return result;
                }
            }
        }

        public virtual Result<object> EmitAccessorDeclaration(AccessorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAccessorSignature(AccessorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAddition(Addition node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAdditionAssignment(AdditionAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAddressOf(AddressOf node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAnnotation(Annotation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitArithmeticNegation(ArithmeticNegation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitArrayConstruction(ArrayConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitArrayTypeReference(ArrayTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAssignment(Assignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitAssociation(Association node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseAnd(BitwiseAnd node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseAndAssignment(BitwiseAndAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseExclusiveOr(BitwiseExclusiveOr node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseExclusiveOrAssignment(BitwiseExclusiveOrAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseLeftShift(BitwiseLeftShift node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseLeftShiftAssignment(BitwiseLeftShiftAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseNegation(BitwiseNegation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseOr(BitwiseOr node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseOrAssignment(BitwiseOrAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseRightShift(BitwiseRightShift node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseRightShiftAssignment(BitwiseRightShiftAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseUnsignedRightShift(BitwiseUnsignedRightShift node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBitwiseUnsignedRightShiftAssignment(BitwiseUnsignedRightShiftAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBlock(Block node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBooleanConstant(BooleanConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitBreakpoint(Breakpoint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitClauseBreak(ClauseBreak node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitCodeConstant(CodeConstant node, Context context, CancellationToken token)
        {
            // [dho] by default we just dump the code into the emission - 13/04/19
            context.Emission.Append(node, node.Value);

            return new Result<object>();
        }

        public virtual Result<object> EmitCollectionDestructuring(CollectionDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitCompilerHint(CompilerHint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitComponent(Component node, Context context, CancellationToken token)
        {
            // [dho] TODO move to a helper for other emitters that will spit out 1 file per component, eg. Java etc - 21/09/18 (ported 24/04/19)
            // [dho] TODO CLEANUP this is duplicated in CSEmitter (apart from FileEmission extension) - 21/09/18 (ported 24/04/19)
            var result = new Result<object>();

            // if (node.Origin.Kind != NodeOriginKind.Source)
            // {
            //     result.AddMessages(
            //         new NodeMessage(MessageKind.Error, $"Could not write emission in artifact due to unsupported origin kind '{node.Origin.Kind}'", node)
            //         {
            //             Hint = GetHint(node.Origin),
            //             Tags = DiagnosticTags
            //         }
            //     );

            //     return result;
            // }

            // var sourceWithLocation = ((SourceNodeOrigin)node.Origin).Source as ISourceWithLocation<IFileLocation>;

            // if (sourceWithLocation == null || sourceWithLocation.Location == null)
            // {
            //     result.AddMessages(
            //         new NodeMessage(MessageKind.Error, $"Could not write emission in artifact because output location cannot be determined", node)
            //         {
            //             Hint = GetHint(node.Origin),
            //             Tags = DiagnosticTags
            //         }
            //     );

            //     return result;
            // }

            // [dho] our emissions will be stored in a file with the same relative path and name
            // as the original source for this component, eg. hello/World.ts => hello/World.java - 26/04/19
            var location = FileSystem.ParseFileLocation(
                RelativeComponentOutFilePath(context.Session, context.Artifact, context.Shard, node)
            );
            
            var file = context.OutFileCollection.Contains(location) ? (
                (FileEmission)context.OutFileCollection[location]
            ) : (
                new FileEmission(location)
            );

            // if (context.OutFileCollection.Contains(file.Destination))
            // {
            //     result.AddMessages(
            //         new NodeMessage(MessageKind.Error, $"Could not write emission in artifact at '{file.Destination.ToPathString()}' because location already exists", node)
            //         {
            //             Hint = GetHint(node.Origin),
            //             Tags = DiagnosticTags
            //         }
            //     );
            // }

            var childContext = ContextHelpers.Clone(context);
            childContext.Component = node;
            // // childContext.Parent = node;
            childContext.Emission = file; // [dho] this is where children of this component should write to - 29/08/18

            foreach (var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(context.AST, node))
            {
                result.AddMessages(
                    EmitNode(child, childContext, token)
                );

                if(RequiresSemicolonSentinel(child))
                {
                    childContext.Emission.Append(child, ";");
                }

                childContext.Emission.AppendBelow(node, "");

                if (token.IsCancellationRequested)
                {
                    break;
                }
            }

            if (!Diagnostics.DiagnosticsHelpers.HasErrors(result))
            {
                context.OutFileCollection[file.Destination] = file;
            }

            return result;
        
        }

        public virtual Result<object> EmitConcatenation(Concatenation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitConcatenationAssignment(ConcatenationAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitConditionalTypeReference(ConditionalTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLiteralTypeReference(LiteralTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitComputedValue(ComputedValue node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitConstructorDeclaration(ConstructorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitConstructorSignature(ConstructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitConstructorTypeReference(ConstructorTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDataValueDeclaration(DataValueDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDefaultExportReference(DefaultExportReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDestruction(Destruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDestructorDeclaration(DestructorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDestructorSignature(DestructorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDestructuredMember(DestructuredMember node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDictionaryConstruction(DictionaryConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDictionaryTypeReference(DictionaryTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDirective(Directive node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDivision(Division node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDivisionAssignment(DivisionAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDoOrDieErrorTrap(DoOrDieErrorTrap node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDoOrRecoverErrorTrap(DoOrRecoverErrorTrap node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDoWhilePredicateLoop(DoWhilePredicateLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDomain(Domain node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            var childContext = ContextHelpers.Clone(context);
            childContext.Domain = node;
            // // childContext.Parent = node;

            foreach(var (child, hasNext) in ASTNodeHelpers.IterateLiveChildren(node.AST, node.ID))
            {
                result.AddMessages(
                    EmitNode(child, childContext, token).Messages
                );

                if(token.IsCancellationRequested)
                {
                    break;
                }
            }

            return result;
        }

        public virtual Result<object> EmitDynamicTypeConstruction(DynamicTypeConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitDynamicTypeReference(DynamicTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitEntityDestructuring(EntityDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitEnumerationMemberDeclaration(EnumerationMemberDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitEnumerationTypeDeclaration(EnumerationTypeDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitErrorFinallyClause(ErrorFinallyClause node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitErrorHandlerClause(ErrorHandlerClause node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitErrorTrapJunction(ErrorTrapJunction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitEvalToVoid(EvalToVoid node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitExponentiation(Exponentiation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitExponentiationAssignment(ExponentiationAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitExportDeclaration(ExportDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitFieldDeclaration(FieldDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitFieldSignature(FieldSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitForKeysLoop(ForKeysLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitForMembersLoop(ForMembersLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitForPredicateLoop(ForPredicateLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitForcedCast(ForcedCast node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitFunctionDeclaration(FunctionDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitFunctionTermination(FunctionTermination node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitFunctionTypeReference(FunctionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitGeneratorSuspension(GeneratorSuspension node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitGlobalDeclaration(GlobalDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIdentifier(Identifier node, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            context.Emission.Append(node, node.Lexeme);

            return result;
        }

        public virtual Result<object> EmitIdentity(Identity node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitImportDeclaration(ImportDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIncidentContextReference(IncidentContextReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIncidentTypeConstraint(IncidentTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIndexTypeQuery(IndexTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInferredTypeQuery(InferredTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIndexedAccess(IndexedAccess node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIndexerSignature(IndexerSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInterfaceDeclaration(InterfaceDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInterimSuspension(InterimSuspension node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInterpolatedString(InterpolatedString node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInterpolatedStringConstant(InterpolatedStringConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIntersectionTypeReference(IntersectionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitIntrinsicTypeReference(IntrinsicTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInvocation(Invocation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitInvocationArgument(InvocationArgument node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitJumpToNextIteration(JumpToNextIteration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitKeyValuePair(KeyValuePair node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLabel(Label node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLambdaDeclaration(LambdaDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLogicalAnd(LogicalAnd node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLogicalNegation(LogicalNegation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLogicalOr(LogicalOr node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLoopBreak(LoopBreak node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLooseEquivalent(LooseEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLooseNonEquivalent(LooseNonEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitLowerBoundedTypeConstraint(LowerBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMappedDestructuring(MappedDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMappedTypeReference(MappedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMatchClause(MatchClause node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMatchJunction(MatchJunction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMaybeNull(MaybeNull node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMemberNameReflection(MemberNameReflection node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMemberTypeConstraint(MemberTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMembershipTest(MembershipTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMetaProperty(MetaProperty node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMethodDeclaration(MethodDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMethodSignature(MethodSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitModifier(Modifier node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMultiplication(Multiplication node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMultiplicationAssignment(MultiplicationAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMutatorDeclaration(MutatorDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMutatorSignature(MutatorSignature node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitMutex(AST.Mutex node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNamedTypeConstruction(NamedTypeConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNamedTypeQuery(NamedTypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNamedTypeReference(NamedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNamespaceDeclaration(NamespaceDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNamespaceReference(NamespaceReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNonMembershipTest(NonMembershipTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }
        public virtual Result<object> EmitNop(Nop node, Context context, CancellationToken token)
        {
            // [dho] by default we emit nothing for a nop - 17/04/19
            return new Result<object>();
        }

        public virtual Result<object> EmitNotNull(NotNull node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNotNumber(NotNumber node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNull(Null node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNullCoalescence(NullCoalescence node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitNumericConstant(NumericConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitObjectTypeDeclaration(ObjectTypeDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        // public virtual Result<object> EmitOrderedGroup(OrderedGroup node, Context context, CancellationToken token)
        // {
        //     return CreateUnsupportedFeatureResult(node);
        // }

        public virtual Result<object> EmitMeta(Meta node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitParameterDeclaration(ParameterDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitParenthesizedTypeReference(ParenthesizedTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPointerDereference(PointerDereference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPointerTypeReference(PointerTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPostDecrement(PostDecrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPostIncrement(PostIncrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPreDecrement(PreDecrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPreIncrement(PreIncrement node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPredicateFlat(PredicateFlat node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPredicateJunction(PredicateJunction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPrioritySymbolResolutionContext(PrioritySymbolResolutionContext node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitPropertyDeclaration(PropertyDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitQualifiedAccess(QualifiedAccess node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitRaiseError(RaiseError node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitReferenceAliasDeclaration(ReferenceAliasDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitRegularExpressionConstant(RegularExpressionConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitRemainder(Remainder node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitRemainderAssignment(RemainderAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitRunDirective(Directive node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitSafeCast(SafeCast node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitSmartCast(SmartCast node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitSpreadDestructuring(SpreadDestructuring node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStrictEquivalent(StrictEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStrictGreaterThan(StrictGreaterThan node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStrictGreaterThanOrEquivalent(StrictGreaterThanOrEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStrictLessThan(StrictLessThan node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStrictLessThanOrEquivalent(StrictLessThanOrEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStrictNonEquivalent(StrictNonEquivalent node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitStringConstant(StringConstant node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitSubtraction(Subtraction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitSubtractionAssignment(SubtractionAssignment node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitSuperContextReference(SuperContextReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTupleConstruction(TupleConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTupleTypeReference(TupleTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTypeAliasDeclaration(TypeAliasDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTypeInterrogation(TypeInterrogation node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTypeParameterDeclaration(TypeParameterDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTypeQuery(TypeQuery node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitTypeTest(TypeTest node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitUpperBoundedTypeConstraint(UpperBoundedTypeConstraint node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitUnionTypeReference(UnionTypeReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitViewConstruction(ViewConstruction node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitViewDeclaration(ViewDeclaration node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitWhilePredicateLoop(WhilePredicateLoop node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }

        public virtual Result<object> EmitWildcardExportReference(WildcardExportReference node, Context context, CancellationToken token)
        {
            return CreateUnsupportedFeatureResult(node);
        }
        
        protected Result<object> EmitDelimited(Node[] nodes, string delimiter, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            foreach(var (member, hasNext) in ASTNodeHelpers.IterateMembers(nodes))
            {
                result.AddMessages(EmitNode(member, context, token));

                if(hasNext)
                {
                    context.Emission.Append(member, delimiter);
                }
            }

            return result;
        }

        protected Result<object> ReportUnsupportedMetaFlags(ASTNode node, MetaFlag flags, Context context, CancellationToken token)
        {
            var result = new Result<object>();

            // [dho] filter out any flags that have no bearing on emission - 29/11/19
            flags &= MetaFlag.EmittedFlagsMask;

            if(flags != 0x0)
            {
                foreach(MetaFlag f in System.Enum.GetValues(typeof(MetaFlag)))
                {
                    if((flags & f) == f)
                    {
                        var flagName = System.String.Join(" ", 
                            System.Text.RegularExpressions.Regex.Split(((MetaFlag)f).ToString(), SplitOnUpperCaseLettersRegexPattern)
                        ).ToLower();

                        result.AddMessages(
                            CreateUnsupportedFeatureResult(node, flagName)
                        );
                    }
                }
            }

            return result;
        }

        public virtual string RelativeComponentOutFilePath(Session session, Artifact artifact, Shard shard, Component node)
        {
            var name = node.Name;
            var sessionBaseDir = session.BaseDirectory.ToPathString() + '/';

            var path = name.Replace(sessionBaseDir, "");

            return System.IO.Path.ChangeExtension(path, FileExtension);
        }

        protected virtual bool RequiresSemicolonSentinel(Node node) => false;
    }
}