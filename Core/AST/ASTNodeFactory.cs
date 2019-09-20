namespace Sempiler.AST
{
    using System.Runtime.CompilerServices;

    using NodeID = System.String;
    // using System.Runtime.CompilerServices;
    using System.Collections.Generic;

    /// <summary>Node itself is agnostic of any AST, but these wrappers allow traversal of a Node's edges
    /// in the context of a particular AST</summary>
    public static class ASTNodeFactory
    {        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AccessorDeclaration AccessorDeclaration(RawAST ast, Node node)
        {
            return new AccessorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AccessorSignature AccessorSignature(RawAST ast, Node node)
        {
            return new AccessorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Addition Addition(RawAST ast, Node node)
        {
            return new Addition(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AdditionAssignment AdditionAssignment(RawAST ast, Node node)
        {
            return new AdditionAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AddressOf AddressOf(RawAST ast, Node node)
        {
            return new AddressOf(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Annotation Annotation(RawAST ast, Node node)
        {
            return new Annotation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArithmeticNegation ArithmeticNegation(RawAST ast, Node node)
        {
            return new ArithmeticNegation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayConstruction ArrayConstruction(RawAST ast, Node node)
        {
            return new ArrayConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayTypeReference ArrayTypeReference(RawAST ast, Node node)
        {
            return new ArrayTypeReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArtifactReference ArtifactReference(RawAST ast, DataNode<string> node)
        {
            return new ArtifactReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Assignment Assignment(RawAST ast, Node node)
        {
            return new Assignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Association Association(RawAST ast, Node node)
        {
            return new Association(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseAnd BitwiseAnd(RawAST ast, Node node)
        {
            return new BitwiseAnd(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseAndAssignment BitwiseAndAssignment(RawAST ast, Node node)
        {
            return new BitwiseAndAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseExclusiveOr BitwiseExclusiveOr(RawAST ast, Node node)
        {
            return new BitwiseExclusiveOr(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseExclusiveOrAssignment BitwiseExclusiveOrAssignment(RawAST ast, Node node)
        {
            return new BitwiseExclusiveOrAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseLeftShift BitwiseLeftShift(RawAST ast, Node node)
        {
            return new BitwiseLeftShift(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseLeftShiftAssignment BitwiseLeftShiftAssignment(RawAST ast, Node node)
        {
            return new BitwiseLeftShiftAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseNegation BitwiseNegation(RawAST ast, Node node)
        {
            return new BitwiseNegation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseOr BitwiseOr(RawAST ast, Node node)
        {
            return new BitwiseOr(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseOrAssignment BitwiseOrAssignment(RawAST ast, Node node)
        {
            return new BitwiseOrAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseRightShift BitwiseRightShift(RawAST ast, Node node)
        {
            return new BitwiseRightShift(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseRightShiftAssignment BitwiseRightShiftAssignment(RawAST ast, Node node)
        {
            return new BitwiseRightShiftAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseUnsignedRightShift BitwiseUnsignedRightShift(RawAST ast, Node node)
        {
            return new BitwiseUnsignedRightShift(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitwiseUnsignedRightShiftAssignment BitwiseUnsignedRightShiftAssignment(RawAST ast, Node node)
        {
            return new BitwiseUnsignedRightShiftAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Block Block(RawAST ast, Node node)
        {
            return new Block(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BooleanConstant BooleanConstant(RawAST ast, DataNode<bool> node)
        {
            return new BooleanConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Breakpoint Breakpoint(RawAST ast, Node node)
        {
            return new Breakpoint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BridgeFunctionDeclaration BridgeFunctionDeclaration(RawAST ast, Node node)
        {
            return new BridgeFunctionDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BridgeInvocation BridgeInvocation(RawAST ast, Node node)
        {
            return new BridgeInvocation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClauseBreak ClauseBreak(RawAST ast, Node node)
        {
            return new ClauseBreak(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CodeConstant CodeConstant(RawAST ast, DataNode<string> node)
        {
            return new CodeConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CollectionDestructuring CollectionDestructuring(RawAST ast, Node node)
        {
            return new CollectionDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CompilerHint CompilerHint(RawAST ast, Node node)
        {
            return new CompilerHint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Component Component(RawAST ast, DataNode<string> node)
        {
            return new Component(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Concatenation Concatenation(RawAST ast, Node node)
        {
            return new Concatenation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConcatenationAssignment ConcatenationAssignment(RawAST ast, Node node)
        {
            return new ConcatenationAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConditionalTypeReference ConditionalTypeReference(RawAST ast, Node node)
        {
            return new ConditionalTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LiteralTypeReference LiteralTypeReference(RawAST ast, Node node)
        {
            return new LiteralTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorDeclaration ConstructorDeclaration(RawAST ast, Node node)
        {
            return new ConstructorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorSignature ConstructorSignature(RawAST ast, Node node)
        {
            return new ConstructorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConstructorTypeReference ConstructorTypeReference(RawAST ast, Node node)
        {
            return new ConstructorTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DataValueDeclaration DataValueDeclaration(RawAST ast, Node node)
        {
            return new DataValueDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DefaultExportReference DefaultExportReference(RawAST ast, Node node)
        {
            return new DefaultExportReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Destruction Destruction(RawAST ast, Node node)
        {
            return new Destruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DestructorDeclaration DestructorDeclaration(RawAST ast, Node node)
        {
            return new DestructorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DestructorSignature DestructorSignature(RawAST ast, Node node)
        {
            return new DestructorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DestructuredMember DestructuredMember(RawAST ast, Node node)
        {
            return new DestructuredMember(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DictionaryConstruction DictionaryConstruction(RawAST ast, Node node)
        {
            return new DictionaryConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DictionaryTypeReference DictionaryTypeReference(RawAST ast, Node node)
        {
            return new DictionaryTypeReference(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Directive Directive(RawAST ast, DataNode<string> node)
        {
            return new Directive(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Division Division(RawAST ast, Node node)
        {
            return new Division(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DivisionAssignment DivisionAssignment(RawAST ast, Node node)
        {
            return new DivisionAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoOrDieErrorTrap DoOrDieErrorTrap(RawAST ast, Node node)
        {
            return new DoOrDieErrorTrap(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoOrRecoverErrorTrap DoOrRecoverErrorTrap(RawAST ast, Node node)
        {
            return new DoOrRecoverErrorTrap(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DoWhilePredicateLoop DoWhilePredicateLoop(RawAST ast, Node node)
        {
            return new DoWhilePredicateLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Domain Domain(RawAST ast, Node node)
        {
            return new Domain(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicTypeConstruction DynamicTypeConstruction(RawAST ast, Node node)
        {
            return new DynamicTypeConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DynamicTypeReference DynamicTypeReference(RawAST ast, Node node)
        {
            return new DynamicTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityDestructuring EntityDestructuring(RawAST ast, Node node)
        {
            return new EntityDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnumerationMemberDeclaration EnumerationMemberDeclaration(RawAST ast, Node node)
        {
            return new EnumerationMemberDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EnumerationTypeDeclaration EnumerationTypeDeclaration(RawAST ast, Node node)
        {
            return new EnumerationTypeDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ErrorFinallyClause ErrorFinallyClause(RawAST ast, Node node)
        {
            return new ErrorFinallyClause(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ErrorHandlerClause ErrorHandlerClause(RawAST ast, Node node)
        {
            return new ErrorHandlerClause(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ErrorTrapJunction ErrorTrapJunction(RawAST ast, Node node)
        {
            return new ErrorTrapJunction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EvalToVoid EvalToVoid(RawAST ast, Node node)
        {
            return new EvalToVoid(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exponentiation Exponentiation(RawAST ast, Node node)
        {
            return new Exponentiation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExponentiationAssignment ExponentiationAssignment(RawAST ast, Node node)
        {
            return new ExponentiationAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExportDeclaration ExportDeclaration(RawAST ast, Node node)
        {
            return new ExportDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldDeclaration FieldDeclaration(RawAST ast, Node node)
        {
            return new FieldDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FieldSignature FieldSignature(RawAST ast, Node node)
        {
            return new FieldSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForKeysLoop ForKeysLoop(RawAST ast, Node node)
        {
            return new ForKeysLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForMembersLoop ForMembersLoop(RawAST ast, Node node)
        {
            return new ForMembersLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForPredicateLoop ForPredicateLoop(RawAST ast, Node node)
        {
            return new ForPredicateLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ForcedCast ForcedCast(RawAST ast, Node node)
        {
            return new ForcedCast(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FunctionDeclaration FunctionDeclaration(RawAST ast, Node node)
        {
            return new FunctionDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FunctionTermination FunctionTermination(RawAST ast, Node node)
        {
            return new FunctionTermination(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FunctionTypeReference FunctionTypeReference(RawAST ast, Node node)
        {
            return new FunctionTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GeneratorSuspension GeneratorSuspension(RawAST ast, Node node)
        {
            return new GeneratorSuspension(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GlobalDeclaration GlobalDeclaration(RawAST ast, Node node)
        {
            return new GlobalDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Identifier Identifier(RawAST ast, DataNode<string> node)
        {
            return new Identifier(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Identity Identity(RawAST ast, Node node)
        {
            return new Identity(ast, node);
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static IfDirective IfDirective(RawAST ast, Node node)
        // {
        //     return new IfDirective(ast, node);
        // }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ImportDeclaration ImportDeclaration(RawAST ast, Node node)
        {
            return new ImportDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IncidentContextReference IncidentContextReference(RawAST ast, Node node)
        {
            return new IncidentContextReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IncidentTypeConstraint IncidentTypeConstraint(RawAST ast, Node node)
        {
            return new IncidentTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexTypeQuery IndexTypeQuery(RawAST ast, Node node)
        {
            return new IndexTypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InferredTypeQuery InferredTypeQuery(RawAST ast, Node node)
        {
            return new InferredTypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexedAccess IndexedAccess(RawAST ast, Node node)
        {
            return new IndexedAccess(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IndexerSignature IndexerSignature(RawAST ast, Node node)
        {
            return new IndexerSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterfaceDeclaration InterfaceDeclaration(RawAST ast, Node node)
        {
            return new InterfaceDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterimSuspension InterimSuspension(RawAST ast, Node node)
        {
            return new InterimSuspension(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterpolatedString InterpolatedString(RawAST ast, Node node)
        {
            return new InterpolatedString(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InterpolatedStringConstant InterpolatedStringConstant(RawAST ast, DataNode<string> node)
        {
            return new InterpolatedStringConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntersectionTypeReference IntersectionTypeReference(RawAST ast, Node node)
        {
            return new IntersectionTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntrinsicTypeReference IntrinsicTypeReference(RawAST ast, Node node)
        {
            return new IntrinsicTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Invocation Invocation(RawAST ast, Node node)
        {
            return new Invocation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InvocationArgument InvocationArgument(RawAST ast, Node node)
        {
            return new InvocationArgument(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JumpToNextIteration JumpToNextIteration(RawAST ast, Node node)
        {
            return new JumpToNextIteration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair KeyValuePair(RawAST ast, Node node)
        {
            return new KeyValuePair(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Label Label(RawAST ast, Node node)
        {
            return new Label(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LambdaDeclaration LambdaDeclaration(RawAST ast, Node node)
        {
            return new LambdaDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalAnd LogicalAnd(RawAST ast, Node node)
        {
            return new LogicalAnd(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalNegation LogicalNegation(RawAST ast, Node node)
        {
            return new LogicalNegation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LogicalOr LogicalOr(RawAST ast, Node node)
        {
            return new LogicalOr(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LoopBreak LoopBreak(RawAST ast, Node node)
        {
            return new LoopBreak(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LooseEquivalent LooseEquivalent(RawAST ast, Node node)
        {
            return new LooseEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LooseNonEquivalent LooseNonEquivalent(RawAST ast, Node node)
        {
            return new LooseNonEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LowerBoundedTypeConstraint LowerBoundedTypeConstraint(RawAST ast, Node node)
        {
            return new LowerBoundedTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MappedDestructuring MappedDestructuring(RawAST ast, Node node)
        {
            return new MappedDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MappedTypeReference MappedTypeReference(RawAST ast, Node node)
        {
            return new MappedTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MatchClause MatchClause(RawAST ast, Node node)
        {
            return new MatchClause(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MatchJunction MatchJunction(RawAST ast, Node node)
        {
            return new MatchJunction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MaybeNull MaybeNull(RawAST ast, Node node)
        {
            return new MaybeNull(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemberNameReflection MemberNameReflection(RawAST ast, Node node)
        {
            return new MemberNameReflection(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MemberTypeConstraint MemberTypeConstraint(RawAST ast, Node node)
        {
            return new MemberTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MembershipTest MembershipTest(RawAST ast, Node node)
        {
            return new MembershipTest(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Meta Meta(RawAST ast, DataNode<MetaFlag> node)
        {
            return new Meta(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MetaProperty MetaProperty(RawAST ast, Node node)
        {
            return new MetaProperty(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodDeclaration MethodDeclaration(RawAST ast, Node node)
        {
            return new MethodDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MethodSignature MethodSignature(RawAST ast, Node node)
        {
            return new MethodSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Modifier Modifier(RawAST ast, DataNode<string> node)
        {
            return new Modifier(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Multiplication Multiplication(RawAST ast, Node node)
        {
            return new Multiplication(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MultiplicationAssignment MultiplicationAssignment(RawAST ast, Node node)
        {
            return new MultiplicationAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MutatorDeclaration MutatorDeclaration(RawAST ast, Node node)
        {
            return new MutatorDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MutatorSignature MutatorSignature(RawAST ast, Node node)
        {
            return new MutatorSignature(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Mutex Mutex(RawAST ast, Node node)
        {
            return new Mutex(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamedTypeConstruction NamedTypeConstruction(RawAST ast, Node node)
        {
            return new NamedTypeConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamedTypeQuery NamedTypeQuery(RawAST ast, Node node)
        {
            return new NamedTypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamedTypeReference NamedTypeReference(RawAST ast, Node node)
        {
            return new NamedTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamespaceDeclaration NamespaceDeclaration(RawAST ast, Node node)
        {
            return new NamespaceDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamespaceReference NamespaceReference(RawAST ast, Node node)
        {
            return new NamespaceReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NonMembershipTest NonMembershipTest(RawAST ast, Node node)
        {
            return new NonMembershipTest(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Nop Nop(RawAST ast, Node node)
        {
            return new Nop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NotNull NotNull(RawAST ast, Node node)
        {
            return new NotNull(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NotNumber NotNumber(RawAST ast, Node node)
        {
            return new NotNumber(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Null Null(RawAST ast, Node node)
        {
            return new Null(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NullCoalescence NullCoalescence(RawAST ast, Node node)
        {
            return new NullCoalescence(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NumericConstant NumericConstant(RawAST ast, DataNode<string> node)
        {
            return new NumericConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ObjectTypeDeclaration ObjectTypeDeclaration(RawAST ast, Node node)
        {
            return new ObjectTypeDeclaration(ast, node);
        }


        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static OrderedGroup OrderedGroup(RawAST ast, Node node)
        // {
        //     return new OrderedGroup(ast, node);
        // }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParameterDeclaration ParameterDeclaration(RawAST ast, Node node)
        {
            return new ParameterDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParenthesizedTypeReference ParenthesizedTypeReference(RawAST ast, Node node)
        {
            return new ParenthesizedTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointerDereference PointerDereference(RawAST ast, Node node)
        {
            return new PointerDereference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PointerTypeReference PointerTypeReference(RawAST ast, Node node)
        {
            return new PointerTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PostDecrement PostDecrement(RawAST ast, Node node)
        {
            return new PostDecrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PostIncrement PostIncrement(RawAST ast, Node node)
        {
            return new PostIncrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PreDecrement PreDecrement(RawAST ast, Node node)
        {
            return new PreDecrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PreIncrement PreIncrement(RawAST ast, Node node)
        {
            return new PreIncrement(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PredicateFlat PredicateFlat(RawAST ast, Node node)
        {
            return new PredicateFlat(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PredicateJunction PredicateJunction(RawAST ast, Node node)
        {
            return new PredicateJunction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PrioritySymbolResolutionContext PrioritySymbolResolutionContext(RawAST ast, Node node)
        {
            return new PrioritySymbolResolutionContext(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PropertyDeclaration PropertyDeclaration(RawAST ast, Node node)
        {
            return new PropertyDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QualifiedAccess QualifiedAccess(RawAST ast, Node node)
        {
            return new QualifiedAccess(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RaiseError RaiseError(RawAST ast, Node node)
        {
            return new RaiseError(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReferenceAliasDeclaration ReferenceAliasDeclaration(RawAST ast, Node node)
        {
            return new ReferenceAliasDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RegularExpressionConstant RegularExpressionConstant(RawAST ast, DataNode<string> node)
        {
            return new RegularExpressionConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Remainder Remainder(RawAST ast, Node node)
        {
            return new Remainder(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RemainderAssignment RemainderAssignment(RawAST ast, Node node)
        {
            return new RemainderAssignment(ast, node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SafeCast SafeCast(RawAST ast, Node node)
        {
            return new SafeCast(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SmartCast SmartCast(RawAST ast, Node node)
        {
            return new SmartCast(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpreadDestructuring SpreadDestructuring(RawAST ast, Node node)
        {
            return new SpreadDestructuring(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictEquivalent StrictEquivalent(RawAST ast, Node node)
        {
            return new StrictEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictGreaterThan StrictGreaterThan(RawAST ast, Node node)
        {
            return new StrictGreaterThan(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictGreaterThanOrEquivalent StrictGreaterThanOrEquivalent(RawAST ast, Node node)
        {
            return new StrictGreaterThanOrEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictLessThan StrictLessThan(RawAST ast, Node node)
        {
            return new StrictLessThan(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictLessThanOrEquivalent StrictLessThanOrEquivalent(RawAST ast, Node node)
        {
            return new StrictLessThanOrEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StrictNonEquivalent StrictNonEquivalent(RawAST ast, Node node)
        {
            return new StrictNonEquivalent(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringConstant StringConstant(RawAST ast, DataNode<string> node)
        {
            return new StringConstant(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Subtraction Subtraction(RawAST ast, Node node)
        {
            return new Subtraction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SubtractionAssignment SubtractionAssignment(RawAST ast, Node node)
        {
            return new SubtractionAssignment(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SuperContextReference SuperContextReference(RawAST ast, Node node)
        {
            return new SuperContextReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TupleConstruction TupleConstruction(RawAST ast, Node node)
        {
            return new TupleConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TupleTypeReference TupleTypeReference(RawAST ast, Node node)
        {
            return new TupleTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeAliasDeclaration TypeAliasDeclaration(RawAST ast, Node node)
        {
            return new TypeAliasDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeInterrogation TypeInterrogation(RawAST ast, Node node)
        {
            return new TypeInterrogation(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeParameterDeclaration TypeParameterDeclaration(RawAST ast, Node node)
        {
            return new TypeParameterDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeQuery TypeQuery(RawAST ast, Node node)
        {
            return new TypeQuery(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TypeTest TypeTest(RawAST ast, Node node)
        {
            return new TypeTest(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UpperBoundedTypeConstraint UpperBoundedTypeConstraint(RawAST ast, Node node)
        {
            return new UpperBoundedTypeConstraint(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnionTypeReference UnionTypeReference(RawAST ast, Node node)
        {
            return new UnionTypeReference(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ViewConstruction ViewConstruction(RawAST ast, Node node)
        {
            return new ViewConstruction(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ViewDeclaration ViewDeclaration(RawAST ast, Node node)
        {
            return new ViewDeclaration(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WhilePredicateLoop WhilePredicateLoop(RawAST ast, Node node)
        {
            return new WhilePredicateLoop(ast, node);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static WildcardExportReference WildcardExportReference(RawAST ast, Node node)
        {
            return new WildcardExportReference(ast, node);
        }
    }


}