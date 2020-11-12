using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static PasCompiler.Token;
using static PasCompiler.Token.E;


namespace PasCompiler {
    class Parser {

        public Parser(string source) {
            _lexer = new Lexer(source);
        }

        // Property to get next token
        Token NextToken() {
            _currToken = _lexer.nextToken();
            return _currToken;
        }

        //Token NextToken => _lexer.nextToken ();

        // Property that peeks the next token
        Token PeekToken()
            => _lexer.peekToken();

        // Throw error if the kind of the next token isn't what we expect
        void ExpectTokenKind(E kind) {
            if (NextToken().Kind != kind) throw new Exception($"Expected {kind} token");
        }

        // Throw exception if the value of the next token isn't what we expect
        void ExpectTokenVal(string value) {
            if (NextToken().Value != value) throw new Exception($"Expected '{value}' token");
        }



        // Top level routine that parses the source text and returns an abstract syntax tree representing the program
        public ASTNode Parse() {
            ParseProgramHeader();
            BlockNode block = ParseBlock();
            return new ProgramNode(block);
        }


        void ParseProgramHeader() {
            ExpectTokenVal("program");   // First token must be the "program" keyword
            ExpectTokenKind(Identifier);   // The second token must be the name of the program: an identifier
            ExpectTokenKind(SemiColon);  // Program definition ends with a semicolon
        }

        BlockNode ParseBlock() {
            List<VarDecl> varDeclarations = ParseVarDecl();
            CompoundStatementNode statementList = ParseCompoundStatement();
            return new BlockNode(varDeclarations, statementList);
        }

        List<VarDecl> ParseVarDecl() {
            if (PeekToken().Value != "var") return null;         // Variable declatation section is empty (no variables declared)
            // Otherwise, we do have variable declarations
            List<VarDecl> varDeclarations = new List<VarDecl>();              // Final list of variable declarations
            List<string> varNamesPerGrouping = new List<string>();        // List of variable names declared together in a single grouping
            NextToken();            // Skip the var token 
            // Parse the variable declarations one line at a time 
            while (PeekToken().Kind == Identifier) {
                AddVarDeclarationsPerGroup();
                ExpectTokenKind(SemiColon);
            }
            return varDeclarations;

            // Helper method that creates VarDecl nodes for variables declared in a group.
            // A group declaration is (ident1, indent2, ... , identn : Type;) as opposed to a single variable declaration (ident : Type;)
            void AddVarDeclarationsPerGroup() {
                while (true) {
                    ExpectTokenKind(Identifier);         
                    varNamesPerGrouping.Add(_currToken.Value);              
                    if (PeekToken().Kind == Colon) break;            
                    ExpectTokenKind(Comma);          
                }
                NextToken();            // We exit the loop once we peek a Colon (':'). So, skip past the colon
                ExpectTokenKind(BasicType);               // The token after the colon is the type of the declared variables
                // Create a VarDecl node for each seperate variable declared in this group
                foreach (string varName in varNamesPerGrouping) {
                    varDeclarations.Add(new VarDecl(new IdentNode(varName), _currToken.Value));
                }
                varNamesPerGrouping.Clear();           // Clear the list of variable names
            }
        }

        StatementNode ParseStatement() {
            StatementNode statement;
            Token next = PeekToken();
            if (next.Kind == Identifier) // Could also be method calls (To be added later)
                statement = ParseAssignmentStatement();
            else if (next.Value == "if")
                statement = ParseConditional();
            else if (next.Value == "begin")
                statement = ParseCompoundStatement();
            else throw new Exception($"Unexpected {next} token");
            return statement;
        }

        CompoundStatementNode ParseCompoundStatement() {
            ExpectTokenVal("begin");  // Compound statements start with the 'begin' keyword
            List<StatementNode> statementList = new List<StatementNode>();
            while (PeekToken().Value != "end")
                statementList.Add(ParseStatement());
            NextToken(); // Skip the "end" token
            ExpectTokenKind(Period);
            return new CompoundStatementNode(statementList);
        }

        ConditionalStatementNode ParseConditional() {
            NextToken(); // Skip past the if
            ExpressionNode condition = ParseExpression();
            ExpectTokenVal("then");
            StatementNode ifBody = ParseStatement();
            StatementNode elseBody = null;
            if (PeekToken().Value == "else") {
                NextToken();
                elseBody = ParseStatement();
            }
            return new ConditionalStatementNode(condition, ifBody, elseBody);
        }

        AssignmentNode ParseAssignmentStatement() {
            IdentNode identNode = new IdentNode(NextToken().Value);  // Get the identifier
            ExpectTokenKind(Assignment);
            ExpressionNode expNode = ParseExpression();
            ExpectTokenKind(SemiColon);
            return new AssignmentNode(identNode, expNode);
        }


        ExpressionNode ParseExpression() {
            ExpressionNode expr = ParseNonConditionalExpression();
            if (PeekToken().IsConiditionalOperator) {
                expr = new BinaryNode(NextToken(), expr, ParseNonConditionalExpression());
            }
            return expr;
        }

        ExpressionNode ParseNonConditionalExpression() {
            ExpressionNode expr = Term();
            while (PeekToken().IsAdditiveOperator) {
                expr = new BinaryNode(NextToken(), expr, Term());
            }
            return expr;

            ExpressionNode Term() {
                ExpressionNode term = Factor();
                while (PeekToken().IsMultiplicativeOperator) {
                    term = new BinaryNode(NextToken(), term, Factor());
                }
                return term;
            }

            ExpressionNode Factor() {
                switch (NextToken().Kind) {
                    case OpenParen:
                        ExpressionNode factor = ParseExpression();
                        ExpectTokenKind(ClosedParen);
                        return factor;
                    case Minus:
                        return new UnaryNode(_currToken, Factor());
                    case Identifier:
                        return new IdentNode(_currToken.Value);
                    case Integer:
                        return new ConstantNode<int>(int.Parse(_currToken.Value));
                    case Real:
                        return new ConstantNode<double>(double.Parse(_currToken.Value));
                    default:
                        throw new Exception($"Unexpected {_currToken.Kind} token");
                }
            }
        }

        Lexer _lexer;  // The lexer that generates a stream of tokens from the source text
        Token _currToken;  // Current token
    }

    class Run {
        static void Main(string[] args) {
            Parser parser = new Parser(File.ReadAllText(args[0]));
            ASTNode ast = parser.Parse();
        }
    }
}
