using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PasCompiler.Token.E;

namespace PasCompiler {

    // Base abstract syntax tree node class that all the classes that represent 
    // nodes of different language constructs inherit from.
    abstract class ASTNode {
    }

    abstract class ExpressionNode : ASTNode {
    }

    // A constant value. Does not have children
    class ConstantNode<T> : ExpressionNode {

        // Value of the token. May be a number or string
        public T Val { get; set; }

        public ConstantNode(T value)
            => Val = value;
    }

    class UnaryNode : ExpressionNode {

        public UnaryNode(Token operation, ExpressionNode expr) {
            Op = operation;
            Expr = expr;
        }

        public Token Op { get; set; }

        public ExpressionNode Expr { get; set; }
    }

    // A binary node is an operator and two child subexpressions. This is the building block of an expression tree. 
    class BinaryNode : ExpressionNode {

        // Binary operator that represents the root of an expression
        public Token Op { get; set; }

        // Left subexpression subtree
        public ExpressionNode Left { get; set; }

        // Right subexpression subtree
        public ExpressionNode Right { get; set; }

        public BinaryNode(Token operation, ExpressionNode l, ExpressionNode r) {
            Op = operation;
            Left = l;
            Right = r;
        }
    }

    class IdentNode : ExpressionNode {

        // Identifier name
        public string Name { get; set; }

        public IdentNode(string name)
            => Name = name;
    }

    class VarDecl : ASTNode {

        public IdentNode Id { get; set; }

        public string Type { get; set; }

        public VarDecl(IdentNode id, string type) {
            Id = id;
            Type = type;
        }

    }

    abstract class StatementNode : ASTNode {

    }

    // Assignment of a right hand side value to a left hand side identifier. Value maybe constants, arrays, pointers, expressions etc.
    class AssignmentNode : StatementNode {

        // Left hand side identifier
        public IdentNode Id { get; set; }

        // Right hand side value
        public ExpressionNode Expr { get; set; }

        public AssignmentNode(IdentNode id, ExpressionNode expr) {
            Id = id;
            Expr = expr;
        }
    }

    class ConditionalStatementNode : StatementNode {

        public ExpressionNode Condition { get; set; }

        public StatementNode IfBody { get; set; }

        public StatementNode ElseBody { get; set; }

        public ConditionalStatementNode(ExpressionNode condition, StatementNode ifBody, StatementNode elseBody) {
            Condition = condition;
            IfBody = ifBody;
            ElseBody = elseBody;
        }
    }

    class CompoundStatementNode : StatementNode {

        public List<StatementNode> StatementList { get; set; }

        public CompoundStatementNode(List<StatementNode> statementList) {
            StatementList = statementList;
        }
    }

    class BlockNode : ASTNode {

        public List<VarDecl> VarDeclarations { get; set; }

        public CompoundStatementNode StatementList { get; set; }

        public BlockNode(List<VarDecl> varDeclarations, CompoundStatementNode statementList) {
            VarDeclarations = varDeclarations;
            StatementList = statementList;
        }

    }

    // The root node of an entire program.
    class ProgramNode : ASTNode {

        public BlockNode Block { get; set; }

        public ProgramNode(BlockNode block) {
            Block = block;
        }
    }
}
