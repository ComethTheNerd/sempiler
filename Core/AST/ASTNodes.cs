using System.Collections.Generic;
using System.Diagnostics;

namespace Sempiler.AST
{
    /// <summary>Node itself is agnostic of any AST, but this wrapper allows traversal of the Node's edges
    /// in the context of a particular AST</summary>
    public abstract class ASTNode 
    {
        public readonly RawAST AST;
        public readonly Node Node;
        public ASTNode(RawAST ast, Node node)
        {
            AST = ast;
            Node = node;
        }

        public string ID { get => Node.ID; }
        public SemanticKind Kind { get => ASTHelpers.GetNode(AST, Node.ID).Kind; }
        public INodeOrigin Origin { get => ASTHelpers.GetNode(AST, Node.ID).Origin; }
        public Node[] Meta { get => ASTNodeHelpers.GetMeta(AST, Node.ID); }
        public Node Parent { get => ASTHelpers.GetParent(AST, Node.ID); }

        // public Node[] Children { get => Node.Children; }
    } 

    public abstract class Declaration : ASTNode, IAnnotated, IModified {

        public Declaration(RawAST ast, Node node) : base(ast, node)
        {

        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier);
        }
    }


    // public interface IMetaFlagged
    // {
    //     NodeX Meta { get; set; }
    // }

    public interface ITemplated
    {
        Node[] Template { get; } // generics eg. OrderedGroup possible in C++
    }

    public interface IAnnotated
    {
        Node[] Annotations { get; } 
    }

    public interface IParametered
    {
        Node[] Parameters { get; } 
    }

    public interface IArgumented
    {
        Node[] Arguments { get; } 
    }



    public interface IModified
    {
        Node[] Modifiers { get; } 
    }

    // public interface IModifiable
    // {
    //     Modifier ModifierFlags { get; set; }
    // }

    // public interface IVisibile
    // {
    //     Visibility VisibilityFlags { get; set; }
    // }

    // public interface Nullable
    // {
    //     Nullability Nullability { get; set; }
    // }

    // public enum Nullability { None = 0, Safe = 1, Forced = 2 }


    // public class OrderedGroup : NodeWrapper
    // {
    //     public OrderedGroup(RawAST ast, Node node) : base(ast, node)
    //     {

    //     }

    //     public Node[] Members
    //     {
    //         get => ASTHelpers.GetEdgeNodes(AST, Node.ID);
    //     }
    // }

    public class Block : ASTNode
    {
        

        public Block(RawAST ast, Node node) : base(ast, node)
        {

        }

        public Node[] Content
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Content);
            // get { 
            //     var nodes = ASTHelpers.GetEdgeNodes(AST, Node.ID);

            //     return nodes.Length == 1 ? nodes[0] : null;
            // }
        }
    }

    public class ArtifactReference : ASTNode
    {
        public ArtifactReference(RawAST ast, DataNode<string> node) : base(ast, node)
        {

        }

        public string TargetArtifactName { 
            get => ((DataNode<string>)Node).Data; 
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
        }
    }

    public class Directive : ASTNode
    {    
        public Directive(RawAST ast, DataNode<string> node) : base(ast, node)
        {
        }

        public string Name { 
            get => ((DataNode<string>)Node).Data; 
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
        }
    }


    // public class IfDirective : NodeWrapper
    // {
        
        
        

    //     public IfDirective(RawAST ast, Node node) : base(ast, node)
    //     { 
    //     }

    //     public Node Predicate
    //     {
    //         get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Predicate);
    //     }

    //     public Node TrueBranch
    //     {
    //         get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.TrueBranch);
    //     }

    //     public Node FalseBranch
    //     {
    //         get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.FalseBranch);
    //     }
    // }

    // [dho] Equivalent in C# to `lock(...){ ... }` - 01/04/19
    public class Mutex : ASTNode
    {
        
        

        public Mutex(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }

        public Node Content
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Content);
        }
    }




    // public class NodeChildren
    // {
    //     private Node NodeX;
    //     private List<Node> Items;
    //     private object l = new object();

    //     public NodeChildren(Node node)
    //     {
    //         NodeX = node;
    //     }

    //     public int Count { get { return Items?.Count ?? 0; } }

    //     // public void Clear()
    //     // {
    //     //     for (int i = 0; i < Count; ++i)
    //     //     {
    //     //         Items[i].
    //     //     }

    //     //     
    //     // }

    //     public void Add(Node value)
    //     {
    //         if (value != null)
    //         {
    //             // detach value from existing parent
    //             Detach(value);

    //             lock(l)
    //                 value.Parent = NodeX;
                
    //         }

    //         lock(l)
    //             (Items ?? (Items = new List<Node>())).Add(value);
    //     }

    //     public bool Detach(Node value)
    //     {
    //         if (value == null)
    //         {
    //             return false;
    //         }


    //         Node parent = value.Parent;

    //         if(parent != null)
    //         {
    //             // check we found the node in the parent's children array
    //             // and removed the references
    //             var index = NodeInterrogation.IndexOfChild(parent, value.ID);

    //             if (index > -1)
    //             {
    //                 parent.Children[index] = null;

    //                 return true;
    //             }

    //             Debug.Assert(false);
    //         }

    //         return false;
    //     }

    //     public Node this[int index]
    //     {
    //         get => Items != null && Items.Count > index ? Items[index] : null;
    //         set
    //         {
    //             lock(this)
    //             {
    //                 if (Items == null)
    //                 {
    //                     Items = new List<Node>();
    //                 }

    //                 if (Items.Count > index)
    //                 {
    //                     Node existingItem = Items[index];

    //                     if (existingItem != null)
    //                     {
    //                         if (existingItem.ID == value?.ID)
    //                         {
    //                             return;
    //                         }

    //                         existingItem.
    //                         Items[index] = null;
    //                     }
    //                 }

    //                 if (value != null && value.Parent != NodeX)
    //                 {
    //                     // need to unlink from existing parent
    //                     Detach(value);

    //                     value.Parent = NodeX;
    //                 }

    //                 // [dho] grow list to index otherwise we get an ArgumentOutOfRangeException
    //                 // if index is not within current list bounds - 28/07/18
    //                 for (int i = Count; i <= index; ++i)
    //                 {
    //                     Items.Add(null);
    //                 }

    //                 Items[index] = value;
    //             }
    //         }
    //     }
    // }


    // public interface IRoot : NodeWrapper
    // {

    // }

    // public class Root : NodeWrapper, IRoot
    // {
    //     public Root(RawAST ast, Node node) : base(ast, node)
    //     {
    //     }
    // }


    public class Domain : ASTNode
    {

        public Domain(RawAST ast, Node node) : base(ast, node)
        {

        }

        public Node[] Components
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Component);
        }
    }

    public class Component : ASTNode
    {
        public Component(RawAST ast, DataNode<string> node) : base(ast, node)
        {
            
        }

        public string Name { get => ((DataNode<string>)Node).Data; }
    }


    public class Label : ASTNode
    {
        

        public Label(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }
    }

    /// <summary>
    ///  For expressing a type alias, import alias or some other remapping of symbols
    /// </summary>
    public abstract class Alias : ASTNode
    {
        
        

        protected Alias(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }

        public Node From
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.From);
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }
    }
    
    public class TypeAliasDeclaration : Alias, ITemplated, IAnnotated, IModified
    {
    
        public TypeAliasDeclaration(RawAST ast, Node node) : base(ast, node)
        {

        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier);
        }
    }

    public class ReferenceAliasDeclaration : Alias
    {
        public ReferenceAliasDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class ImportOrExportDeclaration : Declaration, IAnnotated, IModified
    {
        public ImportOrExportDeclaration(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Specifier
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Specifier);
        }

        public Node[] Clauses
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Clause);
        }
    }

    public class ImportDeclaration : ImportOrExportDeclaration
    {
        public ImportDeclaration(RawAST ast, Node node) : base(ast, node)
        {
            
        }
    }

    public class ExportDeclaration : ImportOrExportDeclaration
    {
        public ExportDeclaration(RawAST ast, Node node) : base(ast, node)
        {
            
        }
    }

    public class GlobalDeclaration : Declaration, IAnnotated, IModified
    {
        
        
        

        public GlobalDeclaration(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
        }

       
    }

    public class DefaultExportReference : ASTNode
    {
        public DefaultExportReference(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class WildcardExportReference : ASTNode
    {
        public WildcardExportReference(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Identifier : ASTNode
    {

        public Identifier(RawAST ast, DataNode<string> node) : base(ast, node)
        {
        }

        public string Lexeme { get => ((DataNode<string>)Node).Data; }
    }



    public abstract class Constant<T> : ASTNode
    {
        protected Constant(RawAST ast, DataNode<T> node) : base(ast, node)
        {
            
        }

        public T Value { get => ((DataNode<T>)Node).Data; }
    }

    public class BooleanConstant : Constant<bool>
    {
        public BooleanConstant(RawAST ast, DataNode<bool> node) : base(ast, node)
        {

        }
    }


    public class StringConstant : Constant<string>
    {
        public StringConstant(RawAST ast, DataNode<string> node) : base(ast, node)
        {

        }
    }

    // [dho] provides a way for inserting a literal block of code that is just emitted
    // as is, without parsing/transformation etc - 13/04/19
    public class CodeConstant : Constant<string>
    {
        public CodeConstant(RawAST ast, DataNode<string> node) : base(ast, node)
        {

        }
    }

    public class RegularExpressionConstant : Constant<string>
    {
        public RegularExpressionConstant(RawAST ast, DataNode<string> node) : base(ast, node)
        {

        }

        public Node[] Flags 
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Flag);
        }
    }

    public class InterpolatedString : ASTNode
    {
        

        

        public InterpolatedString(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
            
        }

        /// [dho] <summary>
        /// in the case when people use `TaggedTemplateExpressions` like in 
        /// TypeScript, eg. css`something here`
        /// We will set the `ParserName` to "css"
        /// </summary> - 06/10/18 (ported 23/03/19)
        public Node ParserName
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.ParserName);
            
        }
    }

    // [dho] this is a string constant inside an interpolated string,
    // eg. "World" in `Hello ${"World"}` - 06/10/18
    public class InterpolatedStringConstant : Constant<string>
    {
        public InterpolatedStringConstant(RawAST ast, DataNode<string> node) : base(ast, node)
        {

        }
    }

    public class NumericConstant : Constant<string>
    {
        public NumericConstant(RawAST ast, DataNode<string> node) : base(ast, node)
        {
        }
    }

    public abstract class UnaryLike : ASTNode
    {
        
        protected UnaryLike(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Operand
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
            
        }
    }


    public abstract class BinaryLike : ASTNode
    {

        protected BinaryLike(RawAST ast, Node node) : base(ast, node)
        {
        }


        public Node[] Operands
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Operand);
        }
    }

    public class NullCoalescence : BinaryLike
    {
        public NullCoalescence(RawAST ast, Node node) : base(ast, node)
        {
        }
    }


    public abstract class Comparison : BinaryLike
    {
        protected Comparison(RawAST ast, Node node) : base(ast, node)
        {
        }
    }


    // [dho] Equivalent in TypeScript to `==` - 23/03/19
    public class LooseEquivalent : Comparison
    {
        public LooseEquivalent(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    // [dho] Equivalent in TypeScript to `!=` - 23/03/19
    public class LooseNonEquivalent : Comparison
    {
        public LooseNonEquivalent(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    // [dho] Equivalent in TypeScript to `===` - 23/03/19
    public class StrictEquivalent : Comparison
    {
        public StrictEquivalent(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    // [dho] Equivalent in TypeScript to `!==` - 23/03/19
    public class StrictNonEquivalent : Comparison
    {
        public StrictNonEquivalent(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    public class StrictLessThan : Comparison
    {
        public StrictLessThan(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    public class StrictLessThanOrEquivalent : Comparison
    {
        public StrictLessThanOrEquivalent(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    public class StrictGreaterThan : Comparison
    {
        public StrictGreaterThan(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    public class StrictGreaterThanOrEquivalent : Comparison
    {
        public StrictGreaterThanOrEquivalent(RawAST ast, Node node) : base(ast, node)
        {
        }   
    }

    public abstract class BinaryArithmetic : BinaryLike
    {
        protected BinaryArithmetic(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    public class Addition : BinaryArithmetic
    {
        public Addition(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Subtraction : BinaryArithmetic
    {
        public Subtraction(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Multiplication : BinaryArithmetic
    {
        public Multiplication(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Division : BinaryArithmetic
    {
        public Division(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Remainder : BinaryArithmetic
    {
        public Remainder(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Exponentiation : ASTNode
    {
        
        

        public Exponentiation(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }

        public Node Base
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Base);
            
        }

        public Node Exponent
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Exponent);
            
        }
    }

    public class BitwiseAnd : BinaryArithmetic
    {
        public BitwiseAnd(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseOr : BinaryArithmetic
    {
        public BitwiseOr(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseExclusiveOr : BinaryArithmetic
    {
        public BitwiseExclusiveOr(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class BinaryLogic : BinaryLike
    {
        protected BinaryLogic(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class LogicalAnd : BinaryLogic
    {
        public LogicalAnd(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class LogicalOr : BinaryLogic
    {
        public LogicalOr(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Concatenation : ASTNode
    {

        public Concatenation(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }
        public Node[] Operands
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Operand);
        }
    }

    public abstract class UnaryArithmetic : UnaryLike
    {
        protected UnaryArithmetic(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class PostIncrement : UnaryArithmetic
    {
        public PostIncrement(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class PreIncrement : UnaryArithmetic
    {
        public PreIncrement(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class PostDecrement : UnaryArithmetic
    {
        public PostDecrement(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class PreDecrement : UnaryArithmetic
    {
        public PreDecrement(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseNegation : UnaryArithmetic
    {
        public BitwiseNegation(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class Identity : UnaryArithmetic
    {
        public Identity(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class ArithmeticNegation : UnaryArithmetic
    {
        public ArithmeticNegation(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class UnaryLogic : UnaryLike
    {
        protected UnaryLogic(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class LogicalNegation : UnaryLogic
    {
        public LogicalNegation(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class AssignmentLike : ASTNode
    {

        protected AssignmentLike(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }
        
        

        public Node Storage
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Storage);
            
        }

        public Node Value
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Value);
            
        }
    }

    public class Assignment : AssignmentLike
    {
        public Assignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class ArithmeticAssignment : AssignmentLike
    {
        protected ArithmeticAssignment(RawAST ast, Node node) : base(ast, node) { }
    }


    public class AdditionAssignment : ArithmeticAssignment
    {
        public AdditionAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class SubtractionAssignment : ArithmeticAssignment
    {
        public SubtractionAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class MultiplicationAssignment : ArithmeticAssignment
    {
        public MultiplicationAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class DivisionAssignment : ArithmeticAssignment
    {
        public DivisionAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class RemainderAssignment : ArithmeticAssignment
    {
        public RemainderAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class ExponentiationAssignment : ArithmeticAssignment
    {
        public ExponentiationAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class ConcatenationAssignment : AssignmentLike
    {
        public ConcatenationAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseAndAssignment : ArithmeticAssignment
    {
        public BitwiseAndAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }


    public class BitwiseOrAssignment : ArithmeticAssignment
    {
        public BitwiseOrAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseExclusiveOrAssignment : ArithmeticAssignment
    {
        public BitwiseExclusiveOrAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class BitwiseShift : ASTNode
    {
        
        

        public BitwiseShift(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }

        public Node Offset
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Offset);
            
        }
    }

    public class BitwiseLeftShift : BitwiseShift
    {
        public BitwiseLeftShift(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseRightShift : BitwiseShift
    {
        public BitwiseRightShift(RawAST ast, Node node) : base(ast, node)
        {
        }
    }


    public class BitwiseUnsignedRightShift : BitwiseShift
    {
        public BitwiseUnsignedRightShift(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseLeftShiftAssignment : ArithmeticAssignment
    {
        public BitwiseLeftShiftAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseRightShiftAssignment : ArithmeticAssignment
    {
        public BitwiseRightShiftAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BitwiseUnsignedRightShiftAssignment : ArithmeticAssignment
    {
        public BitwiseUnsignedRightShiftAssignment(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in TypeScript to `typeof` when used in a *type* - 23/03/19
    public class TypeQuery : ASTNode
    {
        

        public TypeQuery(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }
    }

    // [dho] Equivalent in TypeScript to `keyof T` - 23/03/19
    public class IndexTypeQuery : ASTNode
    {
        

        public IndexTypeQuery(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    // [dho] Equivalent in TypeScript to `infer T` - 23/03/19
    public class InferredTypeQuery : ASTNode
    {
        

        public InferredTypeQuery(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    // [dho] Equivalent in TypeScript to `T extends { attributes: infer A }` in 
    // `public attributes : T extends { attributes: infer A } ? A : undefined;` - 23/03/19
    public class NamedTypeQuery : ASTNode
    {
        
        

        public NamedTypeQuery(RawAST ast, Node node) : base(ast, node)
        { 
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Constraints
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Constraint);
            
        }
    }

    // [dho] Equivalent in TypeScript to `typeof` when used as an *expression* - 23/03/19
    public class TypeInterrogation : ASTNode
    {
        

        public TypeInterrogation(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }
    }

    public abstract class Test : ASTNode
    {
        
        

        public Test(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }

        public Node Criteria
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Criteria);
            
        }

    }

    public class MembershipTest : Test
    {
        public MembershipTest(RawAST ast, Node node) : base(ast, node)
        { }
    }

    public class NonMembershipTest : Test
    {
        public NonMembershipTest(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent in TypeScript to `instanceof` - 23/03/19
    public class TypeTest : Test
    {
        public TypeTest(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent to `x?` in `x?.y` - 23/03/19
    public class MaybeNull : ASTNode
    {
        

        public MaybeNull(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }
    }

    // [dho] Equivalent to `x` in `x!.y` - 23/03/19
    public class NotNull : ASTNode
    {
        

        public NotNull(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }
    }

    public abstract class Cast : ASTNode
    {
        
        

        protected Cast(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }

        public Node TargetType
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.TargetType);
            
        }
    }


    // [dho] languages like Swift conceptually differentiate the idea of an 
    // upcast from a downcast but without implementing typechecking ourselves
    // all we can be sure of is that it's a cast - the direction of which is unknown.
    // Even with typechecking the cast might reference a type that is just expected to be
    // defined at the emission location, so even that would not solve the problem.
    // Hence we just think of casts regardless of their direction - 02/10/18
    // [dho] Equivalent in TypeScript to `x as Foo` - 23/03/19
    public class SafeCast : Cast
    {
        public SafeCast(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    // [dho] Equivalent in TypeScript to `<Foo>x` - 23/03/19
    public class ForcedCast : Cast
    {
        public ForcedCast(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    // [dho] Equivalent in TypeScript to `x is string` in `function foo(x : any) : x is string` - 23/03/19
    public class SmartCast : Cast
    {

        public SmartCast(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public abstract class TypeConstraint : ASTNode
    {
        

        protected TypeConstraint(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    // [dho] Equivalent in Java to `T super Bar ` 
    // `T` must be of type `Bar` or a super type of `Bar` - 24/03/19 
    public class LowerBoundedTypeConstraint : TypeConstraint
    {
        public LowerBoundedTypeConstraint(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    // [dho] Equivalent in TypeScript to `T extends Bar` 
    // `T` must be of type `Bar` or a sub type of `Bar` - 24/03/19 
    public class UpperBoundedTypeConstraint : TypeConstraint
    {
        public UpperBoundedTypeConstraint(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    // [dho] Equivalent in TypeScript to `T implements Bar`
    // `T` must implement the interface `Bar`- 24/03/19
    public class IncidentTypeConstraint : TypeConstraint
    {
        public IncidentTypeConstraint(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    // [dho] Equivalent in TypeScript to `Foo in keyof Bar` 
    // `Foo` must be a member of `Bar` - 24/03/19 
    // See Mapped Types in https://www.typescriptlang.org/docs/handbook/advanced-types.html - 24/03/19
    public class MemberTypeConstraint : TypeConstraint
    {
        public MemberTypeConstraint(RawAST ast, Node node) : base(ast, node)
        {

        }
    }

    public class IntrinsicTypeReference : ASTNode, ITemplated
    {
        

        public IntrinsicTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        /// <summary>
        /// [dho] How should this type be interpreted by the compiler - 22/02/19 (ported: 23/03/19)
        /// </summary>
        public TypeRole Role { get; set; }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }
    }

    public class NamedTypeReference : ASTNode, ITemplated, IAnnotated, IModified
    {
        public NamedTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        // [dho] eg. Identifier or QualifiedAccess - 23/03/19
        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);  
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier); 
        }
    }

    public class NamespaceReference : ASTNode, ITemplated
    {

        public NamespaceReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

         public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }
    }

    public abstract class FunctionLikeTypeReference : ASTNode, ITemplated, IParametered
    {
        
        
        

        protected FunctionLikeTypeReference(RawAST ast, Node node) : base(ast, node)
        { }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }

        public Node[] Parameters
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Parameter);
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public class FunctionTypeReference : FunctionLikeTypeReference, ITemplated
    {
        public FunctionTypeReference(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent in TypeScript to `new (x: number, y: number): Point;` in a literal type definition - 26/04/19
    public class ConstructorTypeReference : FunctionLikeTypeReference, ITemplated
    {
        public ConstructorTypeReference(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent in TypeScript to `-1` in `field : -1;`  - 29/03/19
    public class LiteralTypeReference : ASTNode 
    {
        

        public LiteralTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Literal
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Literal);
            
        }
    }

    // [dho] Equivalent in TypeScript to `{ [P in keyof T]: T[P]; }`  - 24/03/19
    public class MappedTypeReference : ASTNode, IAnnotated, IModified 
    {

        public MappedTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node TypeParameter
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.TypeParameter);
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);
            
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier);
            
        }
    }

    // [dho] Equivalent in TypeScript to `T extends { attributes: infer A } ? A : undefined` - 23/03/19
    public class ConditionalTypeReference : ASTNode
    {
        
        
        

        public ConditionalTypeReference(RawAST ast, Node node) : base(ast, node)
        { 
            
        }

        public Node Predicate
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Predicate);
            
        }

        public Node TrueType
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.TrueType);
            
        }

        public Node FalseType
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.FalseType);
            
        }
    }

    public class ArrayTypeReference : ASTNode
    {
        

        public ArrayTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public class PointerTypeReference : ASTNode
    {
        

        public PointerTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public class TupleTypeReference : ASTNode
    {
        

        public TupleTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node[] Types
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public class ParenthesizedTypeReference : ASTNode
    {
        

        public ParenthesizedTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public class DictionaryTypeReference : ASTNode
    {
        
        

        public DictionaryTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
            
        }

        public Node KeyType
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.KeyType);
            
        }

        public Node StoredType
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.StoredType);
            
        }
    }

    public class IntersectionTypeReference : ASTNode
    {
        

        public IntersectionTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node[] Types
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public class UnionTypeReference : ASTNode
    {
        

        public UnionTypeReference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node[] Types
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Type);
            
        }
    }


    // [dho] NOTE type NOT value
    // var x : { foo : Foo, bar() : Bar };
    // NOT var x = { foo, bar(){...} }'
    // - 03/10/18
    public class DynamicTypeReference : ASTNode
    {
        
        // 

        public DynamicTypeReference(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Signatures
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
            
        }

        // public NodeX Template
        // {
        //     get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Template);
        //     
        // }
    }

    public class NamespaceDeclaration : Declaration, ITemplated
    {
        public NamespaceDeclaration(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
            
        }
    }

    public abstract class TypeDeclaration : Declaration, ITemplated
    {
        public TypeDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
            
        }

        public Node[] Supers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Super);
            
        }

        public Node[] Interfaces
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Interface);
            
        }
    }

    public class InterfaceDeclaration : TypeDeclaration
    {
        public InterfaceDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class ConstructorSignature : FunctionLikeSignature
    {
        public ConstructorSignature(RawAST ast, Node node) : base(ast, node)
        { }
    }

    public class DestructorSignature : FunctionLikeSignature
    {
        public DestructorSignature(RawAST ast, Node node) : base(ast, node)
        { }
    }

    public class MethodSignature : FunctionLikeSignature
    {
        public MethodSignature(RawAST ast, Node node) : base(ast, node)
        { }
    }

    public class FieldSignature : ASTNode, IAnnotated, IModified
    {
        
        
        
        
        

        public FieldSignature(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }

        public Node Initializer
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Initializer);
            
        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);
            
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier);
            
        }
    }

    public abstract class OperatorSignature : ASTNode, IAnnotated, IModified, IParametered
    {
        
        
        
        

        protected OperatorSignature(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Parameters
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Parameter);
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);
            
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier);
            
        }
    }

    public class IndexerSignature : OperatorSignature
    {
        public IndexerSignature(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class ObjectTypeDeclaration : TypeDeclaration
    {
        public ObjectTypeDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class EnumerationTypeDeclaration : TypeDeclaration
    {
        public EnumerationTypeDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class EnumerationMemberDeclaration : Declaration
    {
                
        

        public EnumerationMemberDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node Initializer
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Initializer);
            
        }
    }

    public interface IFunctionLike : ITemplated, IAnnotated, IModified, IParametered
    {
        Node Name { get ; }
        Node Type { get; }
    }

    public abstract class FunctionLikeDeclaration : Declaration, IFunctionLike
    {
        
        protected FunctionLikeDeclaration(RawAST ast, Node node) : base(ast, node)
        {   
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }

        public Node[] Parameters
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Parameter);
            
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }
    }

    public abstract class FunctionLikeSignature : ASTNode, IFunctionLike
    {
        protected FunctionLikeSignature(RawAST ast, Node node) : base(ast, node)
        { }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
        }

        public Node[] Parameters
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Parameter);
            
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
            
        }

        public Node[] Annotations
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Annotation);
        }

        public Node[] Modifiers
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Modifier);
        }
    }

    // [dho] Equivalent to `Foo { get => x }` - 23/03/19
    public class AccessorDeclaration : FunctionLikeDeclaration
    {
        public AccessorDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class AccessorSignature : FunctionLikeSignature
    {
        public AccessorSignature(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent to `Foo { set => x = value }` - 23/03/19
    public class MutatorDeclaration : FunctionLikeDeclaration
    {
        public MutatorDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }
    public class MutatorSignature : FunctionLikeSignature
    {
        public MutatorSignature(RawAST ast, Node node) : base(ast, node)
        { }
    }

    public class ConstructorDeclaration : FunctionLikeDeclaration
    {
        public ConstructorDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class DestructorDeclaration : FunctionLikeDeclaration
    {
        public DestructorDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class MethodDeclaration : FunctionLikeDeclaration
    {
        public MethodDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] function is distinct from a method and lambda, even though these are all `FunctionLike`- 23/03/19
    public class FunctionDeclaration : FunctionLikeDeclaration
    {
        public FunctionDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class LambdaDeclaration : FunctionLikeDeclaration
    {
        public LambdaDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class BridgeFunctionDeclaration : FunctionLikeDeclaration
    {
        public BridgeFunctionDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }
    }


    public class Invocation : ASTNode, ITemplated, IArgumented
    {
        public Invocation(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }

        public Node[] Arguments
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Argument);
            
        }
    }

    public class InvocationArgument : ASTNode
    {
        public InvocationArgument(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Value
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Value);
        }

        public Node Label
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Label);
            
        }
    }


    public class BridgeInvocation : ASTNode, IArgumented
    {
        public BridgeInvocation(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
            
        }

        public Node[] Arguments
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Argument);
        }
    }

    // [dho] because we do not type check, we consider any `new Foo()` usage
    // as named type construction because `Foo()` (stack allocation - C++ etc.) would
    // just be treated as an Invocation (because we cannot verify `Foo` is a type, rather
    // than just a function name) - 04/10/18 
    public class NamedTypeConstruction : ASTNode, ITemplated, IArgumented
    {
    
        public NamedTypeConstruction(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
            
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
            
        }

        public Node[] Arguments
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Argument);
            
        }
    }

    // [dho] Equivalent in TypeScript to an object literal like `{ foo, x : y }` - 23/03/19
    public class DynamicTypeConstruction : ASTNode
    {
        

        public DynamicTypeConstruction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
        }
    }

    // [dho] Equivalent in TypeScript to `[1, 2, 3]` - 23/03/19
    public class ArrayConstruction : ASTNode
    {
    
        public ArrayConstruction(RawAST ast, Node node) : base(ast, node)
        {

        }

        public Node Size
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Size);
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
        }

        public Node Type 
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
        }
    }


    public class KeyValuePair : ASTNode
    {
        public KeyValuePair(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Key
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Key);
        }

        public Node Value
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Value);
        }
    }

    public class DictionaryConstruction : ASTNode
    {
        

        public DictionaryConstruction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
        }
    }

    // [dho] Equivalent in C# to `(a, b)` - 23/03/19
    public class TupleConstruction : ASTNode
    {
        

        public TupleConstruction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
        }
    }


    // [dho] Equivalent in TypeScript to `delete` - 23/03/19
    public class Destruction : ASTNode
    {
        

        public Destruction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }


    public abstract class Destructuring : ASTNode
    {
        public Destructuring(RawAST ast, Node node) : base(ast, node)
        {
        }
    }


    // [dho] in TypeScript you can write `type X<T> = { [P in keyof T]?: T[P] }` which 
    // maps all the types of all the properties in `T` on to `X`, essentially copying the meta
    // structure of `T` in the target type of `X`.
    // eg. if `T` was `string` then `X.length : number` semantically speaking in TypeScript - 08/10/18 (ported : 23/03/19)
    // [dho] the template would be the identifier `P` in the above example - 08/10/18 (ported : 23/03/19)
    public class MappedDestructuring : ASTNode, ITemplated
    {
        
        

        public MappedDestructuring(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Value
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Value);
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
        }
    }

    // [dho] Equivalent in TypeScript to `...x` - 23/03/19
    public class SpreadDestructuring : Destructuring
    {
        

        public SpreadDestructuring(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }

    public abstract class SubsetDestructuring : Destructuring
    {
        

        public SubsetDestructuring(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Members
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Member);
        }
    }

    
    // [dho] Equivalent in TypeScript to `{ x, y } = foo` - 23/03/19
    public class EntityDestructuring : SubsetDestructuring
    {
        public EntityDestructuring(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in TypeScript to `[ x, y ] = foo` - 23/03/19
    public class CollectionDestructuring : SubsetDestructuring
    {
        public CollectionDestructuring(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class DestructuredMember : ASTNode
    {
        
        

        public DestructuredMember(RawAST ast, Node node) : base(ast, node)
        { 
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node Default
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Default);
        }
    }

    public class AddressOf : ASTNode
    {
        

        public AddressOf(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }

    public class PointerDereference : ASTNode
    {
        

        public PointerDereference(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }

    public class ParameterDeclaration : Declaration, IAnnotated, IModified
    {
        
        
        
        
        
        

        public ParameterDeclaration(RawAST ast, Node node) : base(ast, node)
        { }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
        }

        public Node Default
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Default);
        }

        public Node Label
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Label);
        }
    }

    public class TypeParameterDeclaration : Declaration
    {
        public TypeParameterDeclaration(RawAST ast, Node node) : base(ast, node)
        { }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node Default
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Default);
        }

        public Node[] Constraints
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Constraint);
        }
    }

    public class FieldDeclaration : Declaration, IAnnotated, IModified
    {

        public FieldDeclaration(RawAST ast, Node node) : base(ast, node)
        {
            
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
        }

        public Node Initializer
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Initializer);
        }
    }

    // [dho] a property is a type member that has either an accessor, mutator or both - 23/03/19
    public class PropertyDeclaration : Declaration
    {
        
        
        
        

        public PropertyDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
        }

        public Node Accessor
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Accessor);
        }

        public Node Mutator
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Mutator);
        }
    }


    // [dho] Equivalent in TypeScript to `var`, `let` or `const` - 23/03/19
    public class DataValueDeclaration : Declaration, IAnnotated, IModified
    {
        
        
        
        
        

        public DataValueDeclaration(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node Initializer
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Initializer);
        }

        public Node Type
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Type);
        }
    }


    // [dho] Equivalent in TypeScript to `await` - 23/03/19
    public class InterimSuspension : ASTNode
    {
        

        public InterimSuspension(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Operand
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
        }
    }


    // [dho] Equivalent in TypeScript to `if` - 23/03/19
    public class PredicateJunction : ASTNode
    {
        
        
        

        public PredicateJunction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Predicate
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Predicate);
        }

        public Node TrueBranch
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.TrueBranch);
        }

        public Node FalseBranch
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.FalseBranch);
        }
    }


    // [dho] Equivalent in TypeScript to a ternary expression like `x ? y : z` - 23/03/19
    public class PredicateFlat : ASTNode
    {
        
        
        

        public PredicateFlat(RawAST ast, Node node) : base(ast, node)
        { 
        }

        public Node Predicate
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Predicate);
        }

        public Node TrueValue
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.TrueValue);
        }

        public Node FalseValue
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.FalseValue);
        }
    }

    // [dho] Equivalent in TypeScript to `switch` - 23/03/19    
    public class MatchJunction : ASTNode
    {
        
        

        public MatchJunction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }

        public Node[] Clauses
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Clause);
        }
    }
  
    public abstract class Clause : ASTNode
    {
        
        

        protected Clause(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node[] Expression
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Pattern);
        }

        public Node[] Body
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Body);
        }
    }

    
    // [dho] Equivalent in TypeScript to `case` or `default` in `switch` - 23/03/19
    public class MatchClause : Clause
    {
        public MatchClause(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in TypeScript to `catch` - 23/03/19
    public class ErrorHandlerClause : Clause
    {
        public ErrorHandlerClause(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in TypeScript to `finally` - 23/03/19
    public class ErrorFinallyClause : Clause
    {
        public ErrorFinallyClause(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in Swift to `try!` - 23/03/19    
    public class DoOrDieErrorTrap : ASTNode
    {
        
    
        public DoOrDieErrorTrap(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }


    // [dho] Equivalent in Swift to `try?` - 23/03/19
    public class DoOrRecoverErrorTrap : ASTNode
    {
        
    
        public DoOrRecoverErrorTrap(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }

    // [dho] Equivalent in TypeScript to `throw` - 23/03/19
    public class RaiseError : ASTNode
    {
        
    
        public RaiseError(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Operand
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
        }
    }

    
    // [dho] Equivalent in TypeScript to `try` - 23/03/19
    public class ErrorTrapJunction : ASTNode
    {
        
        

        public ErrorTrapJunction(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }

        public Node[] Clauses
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Clause);
        }
    }

    // [dho] Equivalent in TypeScript to `super` - 23/03/19

    public class SuperContextReference : ASTNode
    {
        public SuperContextReference(RawAST ast, Node node) : base(ast, node)
        { }
    }

    
    // [dho] Equivalent in TypeScript to `this` - 23/03/19
    public class IncidentContextReference : ASTNode
    {
        public IncidentContextReference(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent in TypeScript to `break` in `case` or `default` - 23/03/19
    public class ClauseBreak : ASTNode
    {
        

        public ClauseBreak(RawAST ast, Node node) : base(ast, node)
        {
        }
        
        public Node Expression
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Label);
        }
    }

    // [dho] Equivalent in TypeScript to `break` in a loop - 23/03/19
    public class LoopBreak : ASTNode
    {
        

        public LoopBreak(RawAST ast, Node node) : base(ast, node)
        {
        }
        
        public Node Expression
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Label);
        }
    }


    // [dho] Equivalent in TypeScript to `with` - 23/03/19
    public class PrioritySymbolResolutionContext : ASTNode
    {
        
        

        public PrioritySymbolResolutionContext(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node SymbolProvider
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.SymbolProvider);
        }

        public Node Scope 
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Scope);
        }
    }


    public abstract class Access : ASTNode
    {
        
        

        protected Access(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Incident
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Incident);
        }

        public Node Member
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Member);
        }
    }

    // [dho] Equivalent in TypeScript to `x.y` - 23/03/19    
    public class QualifiedAccess : Access
    {

        public QualifiedAccess(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in TypeScript to `x[y]` - 23/03/19
    public class IndexedAccess : Access
    {
        public IndexedAccess(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    // [dho] Equivalent in TypeScript to `debugger` - 23/03/19
    public class Breakpoint : ASTNode
    {
        public Breakpoint(RawAST ast, Node node) : base(ast, node)
        { }
    }

    // [dho] Equivalent in TypeScript to `declare` - 27/03/19
    public class CompilerHint : ASTNode
    {
        
        public CompilerHint(RawAST ast, Node node) : base(ast, node)
        { 
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }


    #region loops

    
    // [dho] Equivalent in TypeScript to `for(const x of y)` - 23/03/19
    public class ForMembersLoop : ASTNode
    {
        
        
        

        public ForMembersLoop(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Handle
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Handle);
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
        }
    }

    // [dho] Equivalent in TypeScript to `for(let i = 0; i < x; ++i)` - 23/03/19    
    public class ForPredicateLoop : ASTNode
    {
        
        
        
        

        public ForPredicateLoop(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Initializer
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Initializer);
        }

        public Node Condition
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Condition);
        }

        public Node Iterator
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Iterator);
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
        }
    }

    // [dho] Equivalent in TypeScript to `for(const x in y)` - 23/03/19    
    public class ForKeysLoop : ASTNode
    {
        
        
        

        public ForKeysLoop(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Handle
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Handle);
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
        }
    }

    // [dho] Equivalent in TypeScript to `while` (without `do`) - 23/03/19
    public class WhilePredicateLoop : ASTNode
    {
        
        

        public WhilePredicateLoop(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Condition
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Condition);
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
        }
    }


    // [dho] Equivalent in TypeScript to `do { x  } while(y) - 23/03/19    
    public class DoWhilePredicateLoop : ASTNode
    {
        
        

        public DoWhilePredicateLoop(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Condition
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Condition);
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
        }
    }

    // [dho] Equivalent in TypeScript to `continue` - 23/03/19    
    public class JumpToNextIteration : ASTNode
    {
        

        public JumpToNextIteration(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Label
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Label);
        }
    }


    #endregion


    // [dho] Equivalent in TypeScript to `return` - 23/03/19    
    public class FunctionTermination : ASTNode
    {
        

        public FunctionTermination(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Value
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Value);
        }
    }

    // [dho] Equivalent in TypeScript to `yield` - 23/03/19    
    public class GeneratorSuspension : ASTNode
    {
        

        public GeneratorSuspension(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Value
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Value);
            
        }
    }

    // [dho] Equivalent in TypeScript to parentheses around an expression like `(x + y)` - 23/03/19    
    public class Association : ASTNode
    {
        

        public Association(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }

    // [dho] TODO check why we have this!? - 23/03/19
    public class MemberNameReflection : ASTNode
    {
        

        public MemberNameReflection(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Subject
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Subject);
        }
    }


    // [dho] an ornament is like some meta construct on a declaration, 
    // like annotation, modifier, question token etc - 23/03/19
    public class Meta : ASTNode
    {
        public Meta(RawAST ast, DataNode<MetaFlag> node) : base(ast, node)
        {
        }

        ///<summary>
        /// [dho] The inferred purpose of this meta on the node, eg. to make it 'private visibility' etc. - 21/02/19
        ///</summary>
        public MetaFlag Flags { 
            get => ((DataNode<MetaFlag>)Node).Data;
        }

        public Node Incident
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Incident);
        }
    }

    public class Annotation : ASTNode
    {
        

        public Annotation(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Expression
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
        }
    }

    public class Modifier : ASTNode
    {
        public Modifier(RawAST ast, DataNode<string> node) : base(ast, node)
        {
        }

        public string Lexeme { get => ((DataNode<string>)Node).Data; }
    }

    public class NotNumber : ASTNode
    {
        public NotNumber(RawAST ast, Node node) : base(ast, node)
        { 
        }
    }

    public class Null : ASTNode
    {
        public Null(RawAST ast, Node node) : base(ast, node)
        { 
        }
    }

    // [dho] Equivalent in TypeScript to `void x` - 23/03/19    
    public class EvalToVoid : ASTNode
    {
        

        public EvalToVoid(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Expression
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Operand);
        }
    }

    // [dho] Equivalent in TypeScript to `new.target` - 23/03/19
    // [dho] https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/new.target - 23/03/19
    public class MetaProperty : ASTNode
    {
        

        public MetaProperty(RawAST ast, Node node) : base(ast, node)
        {
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }
    }

    public class Nop : ASTNode
    {
        public Nop(RawAST ast, Node node) : base(ast, node)
        {
        }
    }

    public class ViewConstruction : ASTNode
    {
        
        public ViewConstruction(RawAST ast, Node node) : base(ast, node)
        {   
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node[] Properties
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Property);
            
        }

        public Node[] Children
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Child);
        }
    }

    public class ViewDeclaration : ASTNode, IParametered, ITemplated
    {
        public ViewDeclaration(RawAST ast, Node node) : base(ast, node)
        {   
        }

        public Node[] Previews
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.ViewPreview);
        }

        public Node Name
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Name);
        }

        public Node[] Template
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Template);
        }

        public Node[] Parameters
        {
            get => ASTHelpers.QueryEdgeNodes(AST, Node.ID, SemanticRole.Parameter);
        }

        public Node Body
        {
            get => ASTHelpers.GetSingleMatch(AST, Node.ID, SemanticRole.Body);
        }
    }
}