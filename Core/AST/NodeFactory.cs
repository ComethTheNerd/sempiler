using System.Runtime.CompilerServices;

namespace Sempiler.AST
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NodeID = System.String;

    public static class NodeFactory
    {
        public delegate T NodeFactoryDelegate<T>(RawAST ast, INodeOrigin origin) where T : ASTNode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AccessorDeclaration AccessorDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.AccessorDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.AccessorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AccessorSignature AccessorSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.AccessorSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.AccessorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Addition Addition(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Addition, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Addition(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AdditionAssignment AdditionAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.AdditionAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.AdditionAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddressOf AddressOf(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.AddressOf, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.AddressOf(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Annotation Annotation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Annotation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Annotation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArithmeticNegation ArithmeticNegation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ArithmeticNegation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ArithmeticNegation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayConstruction ArrayConstruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ArrayConstruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ArrayConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayTypeReference ArrayTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ArrayTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ArrayTypeReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArtifactReference ArtifactReference(RawAST ast, INodeOrigin origin, string targetArtifactName)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.ArtifactReference, origin, targetArtifactName);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ArtifactReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Assignment Assignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Assignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Assignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Association Association(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Association, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Association(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseAnd BitwiseAnd(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseAnd, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseAnd(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseAndAssignment BitwiseAndAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseAndAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseAndAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseExclusiveOr BitwiseExclusiveOr(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseExclusiveOr, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseExclusiveOr(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseExclusiveOrAssignment BitwiseExclusiveOrAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseExclusiveOrAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseExclusiveOrAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseLeftShift BitwiseLeftShift(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseLeftShift, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseLeftShift(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseLeftShiftAssignment BitwiseLeftShiftAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseLeftShiftAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseLeftShiftAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseNegation BitwiseNegation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseNegation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseNegation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseOr BitwiseOr(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseOr, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseOr(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseOrAssignment BitwiseOrAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseOrAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseOrAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseRightShift BitwiseRightShift(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseRightShift, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseRightShift(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseRightShiftAssignment BitwiseRightShiftAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseRightShiftAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseRightShiftAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseUnsignedRightShift BitwiseUnsignedRightShift(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseUnsignedRightShift, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseUnsignedRightShift(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseUnsignedRightShiftAssignment BitwiseUnsignedRightShiftAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BitwiseUnsignedRightShiftAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BitwiseUnsignedRightShiftAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Block Block(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Block, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Block(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BooleanConstant BooleanConstant(RawAST ast, INodeOrigin origin, bool value)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<bool>(id, SemanticKind.BooleanConstant, origin, value);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BooleanConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Breakpoint Breakpoint(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Breakpoint, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Breakpoint(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BridgeFunctionDeclaration BridgeFunctionDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BridgeFunctionDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BridgeFunctionDeclaration(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BridgeInvocation BridgeInvocation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.BridgeInvocation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.BridgeInvocation(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClauseBreak ClauseBreak(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ClauseBreak, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ClauseBreak(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CodeConstant CodeConstant(RawAST ast, INodeOrigin origin, string value)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.CodeConstant, origin, value);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.CodeConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CollectionDestructuring CollectionDestructuring(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.CollectionDestructuring, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.CollectionDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CompilerHint CompilerHint(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.CompilerHint, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.CompilerHint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component Component(RawAST ast, INodeOrigin origin, string name)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.Component, origin, name);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Component(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Concatenation Concatenation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Concatenation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Concatenation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConcatenationAssignment ConcatenationAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ConcatenationAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ConcatenationAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConditionalTypeReference ConditionalTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ConditionalTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ConditionalTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LiteralTypeReference LiteralTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LiteralTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LiteralTypeReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComputedValue ComputedValue(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ComputedValue, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ComputedValue(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorDeclaration ConstructorDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ConstructorDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ConstructorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorSignature ConstructorSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ConstructorSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ConstructorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorTypeReference ConstructorTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ConstructorTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ConstructorTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataValueDeclaration DataValueDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DataValueDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DataValueDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DefaultExportReference DefaultExportReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DefaultExportReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DefaultExportReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Destruction Destruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Destruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Destruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DestructorDeclaration DestructorDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DestructorDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DestructorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DestructorSignature DestructorSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DestructorSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DestructorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DestructuredMember DestructuredMember(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DestructuredMember, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DestructuredMember(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DictionaryConstruction DictionaryConstruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DictionaryConstruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DictionaryConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DictionaryTypeReference DictionaryTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DictionaryTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DictionaryTypeReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Directive Directive(RawAST ast, INodeOrigin origin, string name)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.Directive, origin, name);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Directive(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Division Division(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Division, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Division(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DivisionAssignment DivisionAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DivisionAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DivisionAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoOrDieErrorTrap DoOrDieErrorTrap(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DoOrDieErrorTrap, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DoOrDieErrorTrap(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoOrRecoverErrorTrap DoOrRecoverErrorTrap(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DoOrRecoverErrorTrap, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DoOrRecoverErrorTrap(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoWhilePredicateLoop DoWhilePredicateLoop(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DoWhilePredicateLoop, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DoWhilePredicateLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Domain Domain(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Domain, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Domain(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicTypeConstruction DynamicTypeConstruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DynamicTypeConstruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DynamicTypeConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicTypeReference DynamicTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.DynamicTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.DynamicTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityDestructuring EntityDestructuring(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.EntityDestructuring, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.EntityDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnumerationMemberDeclaration EnumerationMemberDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.EnumerationMemberDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.EnumerationMemberDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnumerationTypeDeclaration EnumerationTypeDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.EnumerationTypeDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.EnumerationTypeDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ErrorFinallyClause ErrorFinallyClause(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ErrorFinallyClause, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ErrorFinallyClause(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ErrorHandlerClause ErrorHandlerClause(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ErrorHandlerClause, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ErrorHandlerClause(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ErrorTrapJunction ErrorTrapJunction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ErrorTrapJunction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ErrorTrapJunction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvalToVoid EvalToVoid(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.EvalToVoid, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.EvalToVoid(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exponentiation Exponentiation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Exponentiation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Exponentiation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExponentiationAssignment ExponentiationAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ExponentiationAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ExponentiationAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExportDeclaration ExportDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ExportDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ExportDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldDeclaration FieldDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.FieldDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.FieldDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldSignature FieldSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.FieldSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.FieldSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForKeysLoop ForKeysLoop(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ForKeysLoop, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ForKeysLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForMembersLoop ForMembersLoop(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ForMembersLoop, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ForMembersLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForPredicateLoop ForPredicateLoop(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ForPredicateLoop, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ForPredicateLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForcedCast ForcedCast(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ForcedCast, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ForcedCast(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FunctionDeclaration FunctionDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.FunctionDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.FunctionDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FunctionTermination FunctionTermination(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.FunctionTermination, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.FunctionTermination(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FunctionTypeReference FunctionTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.FunctionTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.FunctionTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeneratorSuspension GeneratorSuspension(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.GeneratorSuspension, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.GeneratorSuspension(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GlobalDeclaration GlobalDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.GlobalDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.GlobalDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Identifier Identifier(RawAST ast, INodeOrigin origin, string lexeme)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.Identifier, origin, lexeme);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Identifier(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Identity Identity(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Identity, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Identity(ast, node);
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static IfDirective IfDirective(RawAST ast, INodeOrigin origin)
        // {
        //     NodeID id = ASTHelpers.NextID();

        //     var node = new Node(id, SemanticKind.IfDirective, origin);

        //     ASTHelpers.RegisterNode(ast, node);

        //     return ASTNode.IfDirective(ast, node);
        // }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ImportDeclaration ImportDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ImportDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ImportDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IncidentContextReference IncidentContextReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IncidentContextReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IncidentContextReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IncidentTypeConstraint IncidentTypeConstraint(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IncidentTypeConstraint, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IncidentTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexTypeQuery IndexTypeQuery(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IndexTypeQuery, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IndexTypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InferredTypeQuery InferredTypeQuery(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.InferredTypeQuery, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.InferredTypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexedAccess IndexedAccess(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IndexedAccess, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IndexedAccess(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexerSignature IndexerSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IndexerSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IndexerSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterfaceDeclaration InterfaceDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.InterfaceDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.InterfaceDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterimSuspension InterimSuspension(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.InterimSuspension, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.InterimSuspension(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterpolatedString InterpolatedString(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.InterpolatedString, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.InterpolatedString(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterpolatedStringConstant InterpolatedStringConstant(RawAST ast, INodeOrigin origin, string value)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.InterpolatedStringConstant, origin, value);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.InterpolatedStringConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntersectionTypeReference IntersectionTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IntersectionTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IntersectionTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntrinsicTypeReference IntrinsicTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.IntrinsicTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.IntrinsicTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Invocation Invocation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Invocation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Invocation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InvocationArgument InvocationArgument(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.InvocationArgument, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.InvocationArgument(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JumpToNextIteration JumpToNextIteration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.JumpToNextIteration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.JumpToNextIteration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair KeyValuePair(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.KeyValuePair, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.KeyValuePair(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Label Label(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Label, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Label(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaDeclaration LambdaDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LambdaDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LambdaDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalAnd LogicalAnd(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LogicalAnd, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LogicalAnd(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalNegation LogicalNegation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LogicalNegation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LogicalNegation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalOr LogicalOr(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LogicalOr, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LogicalOr(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LoopBreak LoopBreak(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LoopBreak, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LoopBreak(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LooseEquivalent LooseEquivalent(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LooseEquivalent, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LooseEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LooseNonEquivalent LooseNonEquivalent(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LooseNonEquivalent, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LooseNonEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LowerBoundedTypeConstraint LowerBoundedTypeConstraint(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.LowerBoundedTypeConstraint, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.LowerBoundedTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MappedDestructuring MappedDestructuring(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MappedDestructuring, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MappedDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MappedTypeReference MappedTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MappedTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MappedTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MatchClause MatchClause(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MatchClause, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MatchClause(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MatchJunction MatchJunction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MatchJunction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MatchJunction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MaybeNull MaybeNull(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MaybeNull, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MaybeNull(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemberNameReflection MemberNameReflection(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MemberNameReflection, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MemberNameReflection(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemberTypeConstraint MemberTypeConstraint(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MemberTypeConstraint, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MemberTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MembershipTest MembershipTest(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MembershipTest, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MembershipTest(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Meta Meta(RawAST ast, INodeOrigin origin, MetaFlag flags)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<MetaFlag>(id, SemanticKind.Meta, origin, flags);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Meta(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MetaProperty MetaProperty(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MetaProperty, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MetaProperty(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodDeclaration MethodDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MethodDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MethodDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodSignature MethodSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MethodSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MethodSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Modifier Modifier(RawAST ast, INodeOrigin origin, string lexeme)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.Modifier, origin, lexeme);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Modifier(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Multiplication Multiplication(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Multiplication, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Multiplication(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MultiplicationAssignment MultiplicationAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MultiplicationAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MultiplicationAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MutatorDeclaration MutatorDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MutatorDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MutatorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MutatorSignature MutatorSignature(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.MutatorSignature, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.MutatorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mutex Mutex(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Mutex, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Mutex(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamedTypeConstruction NamedTypeConstruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NamedTypeConstruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NamedTypeConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamedTypeQuery NamedTypeQuery(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NamedTypeQuery, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NamedTypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamedTypeReference NamedTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NamedTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NamedTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamespaceDeclaration NamespaceDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NamespaceDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NamespaceDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamespaceReference NamespaceReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NamespaceReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NamespaceReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NonMembershipTest NonMembershipTest(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NonMembershipTest, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NonMembershipTest(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Nop Nop(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Nop, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Nop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NotNull NotNull(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NotNull, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NotNull(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NotNumber NotNumber(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NotNumber, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NotNumber(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Null Null(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Null, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Null(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NullCoalescence NullCoalescence(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.NullCoalescence, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NullCoalescence(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NumericConstant NumericConstant(RawAST ast, INodeOrigin origin, string value)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.NumericConstant, origin, value);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.NumericConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectTypeDeclaration ObjectTypeDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ObjectTypeDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ObjectTypeDeclaration(ast, node);
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static OrderedGroup OrderedGroup(RawAST ast, INodeOrigin origin)
        // {
        //     NodeID id = ASTHelpers.NextID();

        //     var node = new Node(id, SemanticKind.OrderedGroup, origin);

        //     ASTHelpers.RegisterNode(ast, node);

        //     return NodeInterrogation.OrderedGroup(ast, node);
        // }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParameterDeclaration ParameterDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ParameterDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ParameterDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParenthesizedTypeReference ParenthesizedTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ParenthesizedTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ParenthesizedTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointerDereference PointerDereference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PointerDereference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PointerDereference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointerTypeReference PointerTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PointerTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PointerTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PostDecrement PostDecrement(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PostDecrement, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PostDecrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PostIncrement PostIncrement(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PostIncrement, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PostIncrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PreDecrement PreDecrement(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PreDecrement, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PreDecrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PreIncrement PreIncrement(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PreIncrement, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PreIncrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PredicateFlat PredicateFlat(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PredicateFlat, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PredicateFlat(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PredicateJunction PredicateJunction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PredicateJunction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PredicateJunction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrioritySymbolResolutionContext PrioritySymbolResolutionContext(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PrioritySymbolResolutionContext, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PrioritySymbolResolutionContext(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyDeclaration PropertyDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.PropertyDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.PropertyDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QualifiedAccess QualifiedAccess(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.QualifiedAccess, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.QualifiedAccess(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RaiseError RaiseError(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.RaiseError, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.RaiseError(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReferenceAliasDeclaration ReferenceAliasDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ReferenceAliasDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ReferenceAliasDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegularExpressionConstant RegularExpressionConstant(RawAST ast, INodeOrigin origin, string value)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.RegularExpressionConstant, origin, value);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.RegularExpressionConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Remainder Remainder(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Remainder, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Remainder(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RemainderAssignment RemainderAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.RemainderAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.RemainderAssignment(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SafeCast SafeCast(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.SafeCast, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.SafeCast(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SmartCast SmartCast(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.SmartCast, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.SmartCast(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpreadDestructuring SpreadDestructuring(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.SpreadDestructuring, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.SpreadDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictEquivalent StrictEquivalent(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.StrictEquivalent, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StrictEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictGreaterThan StrictGreaterThan(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.StrictGreaterThan, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StrictGreaterThan(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictGreaterThanOrEquivalent StrictGreaterThanOrEquivalent(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.StrictGreaterThanOrEquivalent, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StrictGreaterThanOrEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictLessThan StrictLessThan(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.StrictLessThan, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StrictLessThan(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictLessThanOrEquivalent StrictLessThanOrEquivalent(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.StrictLessThanOrEquivalent, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StrictLessThanOrEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictNonEquivalent StrictNonEquivalent(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.StrictNonEquivalent, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StrictNonEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringConstant StringConstant(RawAST ast, INodeOrigin origin, string value)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new DataNode<string>(id, SemanticKind.StringConstant, origin, value);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.StringConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Subtraction Subtraction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.Subtraction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.Subtraction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SubtractionAssignment SubtractionAssignment(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.SubtractionAssignment, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.SubtractionAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SuperContextReference SuperContextReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.SuperContextReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.SuperContextReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TupleConstruction TupleConstruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TupleConstruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TupleConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TupleTypeReference TupleTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TupleTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TupleTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeAliasDeclaration TypeAliasDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TypeAliasDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TypeAliasDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeInterrogation TypeInterrogation(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TypeInterrogation, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TypeInterrogation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeParameterDeclaration TypeParameterDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TypeParameterDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TypeParameterDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeQuery TypeQuery(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TypeQuery, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeTest TypeTest(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.TypeTest, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.TypeTest(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UpperBoundedTypeConstraint UpperBoundedTypeConstraint(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.UpperBoundedTypeConstraint, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.UpperBoundedTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnionTypeReference UnionTypeReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.UnionTypeReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.UnionTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ViewConstruction ViewConstruction(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ViewConstruction, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ViewConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ViewDeclaration ViewDeclaration(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.ViewDeclaration, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.ViewDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WhilePredicateLoop WhilePredicateLoop(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.WhilePredicateLoop, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.WhilePredicateLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WildcardExportReference WildcardExportReference(RawAST ast, INodeOrigin origin)
        {
            NodeID id = ASTHelpers.NextID();

            var node = new Node(id, SemanticKind.WildcardExportReference, origin);

            ASTHelpers.RegisterNode(ast, node);

            return ASTNodeFactory.WildcardExportReference(ast, node);
        }

    }
}