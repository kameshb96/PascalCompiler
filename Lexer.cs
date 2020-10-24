using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Token.E;

// A token is a pair of a kind and a value. The kind represents the type of token. The value is the actual string that the token is.
class Token {
    public Token (E kind, string value = "") {
        Kind = kind;
        Value = value;
    }
    // The various kinds of Pascal Tokens
    public enum E {
        Identifier, Integer, Real, String, KeyWord,
        OpenParen, ClosedParen, OpenBracket, ClosedBracket, OpenBrace, ClosedBrace,
        Plus, Minus, Mul, Div, Exp, Mod, Eq, PlusEq, MinEq, MulEq, DivEq,
        Greater, Less, Geq, Leq, Neq, LeftShift, RightShift,
        Colon, SemiColon, Assignment, Hash, Amp, Period, Comma,
        And, Or, Not, BasicType, EOF
    };

    // The kind of token
    public E Kind { get; }

    // The actual value of the token as a string
    public string Value { get; }

    public bool isArithmeticOperator => _arithmeticOperators.Contains(Kind);

    public override string ToString () => $"({Kind}, {Value})";

    static E[] _arithmeticOperators = { Plus, Minus, Mul, Div };   // Arithmetic Operators
}

// The class that tokenizes the source text into pascal tokens.
class Lexer {

    public Lexer (string source) => _text = source;

    // Property to Get current character
    char CH => _text[_index];

    bool isEOF => _index >= _text.Length;

    // Get and consume the next token. Throw exception if there is an error in tokenizing
    public Token nextToken () {
        try {
            return GetToken ();
        } catch (IndexOutOfRangeException) {
            throw new Exception("Unexpected end of file");
        } catch (Exception e) {
            int nLine = _text.Take (_index).Count (a => a == '\n') + 1;
            throw new Exception($"At line {nLine}: {e.Message}");
        }
    }

    // Get next token without consuming
    public Token peekToken () {
        int tmp = _index;
        Token tok = nextToken ();
        _index = tmp;
        return tok;
    }

    // Throw error if next token isn't what we expect
    public void Expect (Token tok) {
        if (nextToken ().Kind != tok.Kind) throw new Exception ($"Expected {tok.Kind} token");
    }

    //Top level method that skips over whitespaces and Gets the next token 
    Token GetToken () {
        while (!isEOF && char.IsWhiteSpace (CH)) _index++; // Skip whitespaces
        if (isEOF) return new Token (EOF, "");
        if ("0123456789".Contains (CH)) return GetNumber ();
        if (char.IsLetter (CH) || CH == '_') return GetWord ();
        if (CH == '\'') return GetString ();
        if ("+-*/<>:".Contains (CH)) return GetOperator ();
        char ch = _text[_index++];
        return ch switch {
            '{' => new Token (OpenBrace), '}' => new Token (ClosedBrace),
            '[' => new Token (OpenBracket), ']' => new Token (ClosedBracket),
            '(' => new Token (OpenParen), ')' => new Token (ClosedParen),
            '.' => new Token (Period),
            ',' => new Token (Comma),
            '=' => new Token (Eq),
            ';' => new Token (SemiColon),
            _ => throw new Exception ($"Unexpected character '{ch}'")
        };
    }

    // Gets an operator token
    Token GetOperator () {
        char ch = _text[_index++];
        Token.E kind = Plus;
        switch (ch) {
            case '+':
                if (CH == '=') { _index++; kind = PlusEq; } 
                else kind = Plus;
                break;
            case '-':
                if (CH == '=') { _index++; kind = MinEq; } 
                else kind = Minus;
                break;
            case '*':
                if (CH == '=') { _index++; kind = MulEq; } 
                else if (CH == '*') { _index++; kind = Exp; } 
                else kind = Mul;
                break;
            case '/':
                if (CH == '=') { _index++; kind = DivEq; } 
                else kind = Div;
                break;
            case ':':
                if (CH == '=') { _index++; kind = Assignment; } 
                else kind = Colon;
                break;
            case '>':
                if (CH == '=') { _index++; kind = Geq; } 
                else if (CH == '>') { _index++; kind = RightShift; } 
                else kind = Greater;
                break;
            case '<':
                if (CH == '=') { _index++; kind = Leq; } 
                else if (CH == '>') { _index++; kind = Colon; } 
                else if (CH == '<') { _index++; kind = LeftShift; } 
                else kind = Less;
                break;
            default:
                break;
        }
        return new Token (kind);
    }

    // Get a number token 
    Token GetNumber () {
        int start = _index; // Start of the number token
        int end = start;   // The index of the last digit we encounter
        Token.E kind = Integer;
        GetOneOrMoreDigits ();

        if (CH == '.') { _index++; kind = Real; GetOneOrMoreDigits (); }

        if ("eE".Contains (CH)) {
            _index++;
            if ("+-".Contains (CH)) _index++;
            GetOneOrMoreDigits ();
        }
        _index = end + 1;   // Setting the index to the end of the accumulated number token
        return new Token(kind, _text.Substring(start, _index - start));

        void GetOneOrMoreDigits () {
            while (char.IsDigit(CH)) end = _index++;
        }
    }

    // Get a word token. A word may be an identifier, reserved word or an operator
    Token GetWord () {
        int start = _index;
        Token.E kind = Identifier;
        _index++;    // Skip first character as we have already visited it
        while (char.IsLetterOrDigit (CH) || CH == '_') _index++;
        string word = _text.Substring (start, _index - start);
        if (_wordOperators.Contains (word)) return GetWordOperator ();   // Token is an operator
        if (_basicTypes.Contains (word)) kind = BasicType;   // Token is a basic type
        else if (_keyWords.Contains (word)) kind = KeyWord;   // Token is a reserved word
        return new Token (kind, word);

        // Local Function that Gets the operator that the accumulated word represents
        Token GetWordOperator () {
            return word switch
            {
                "and" => new Token (And),
                "or" => new Token (Or),
                "not" => new Token (Not),
                "mod" => new Token (Mod),
                _ => throw new Exception ("Unknown Identifier")
            };
        }
    }

    // Get a string token
    Token GetString () {
        _controlSeqRegEx ??= new Regex (@"'(#\d+)*'");  
        int start = _index;
        _index++;  // Skip the initial single quotes
        while (true) {
            if (CH == '\'') {  // If we see a a single-quote ('), we have to check for a control sequence to know if it is the end of the string.
                Match m = _controlSeqRegEx.Match (_text, _index);
                if (!m.Success) { _index++; break; }  // Match failed. Reached end of string.
                _index += m.Length;
                continue;
            }
            if (char.IsControl (CH)) throw new Exception ("Invalid string");
            _index++;
        }
        return new Token (Token.E.String, _text.Substring (start, _index - start));
    }
    static Regex _controlSeqRegEx;  // Regex to match control sequence inside the string

    string _text;  // Source Code
    int _index;   // Next character to read
    static string[] _keyWords = { "do", "while", "function", "procedure", "program", "begin", "end", "for", "switch", "case", "then", "if", "else", "type", "var" };   // Keywords in Pascal
    static string[] _wordOperators = { "and", "or", "not", "mod" };    // Pascal Operators that are words
    static string[] _basicTypes = { "integer", "real", "char", "Boolean" };    // Basic types in pascal
}

class Program { 
    static void Main (string[] args) {
        Lexer tokenizer = new Lexer (File.ReadAllText(args[0]));
        while (true) {
            Token tok = tokenizer.nextToken ();
            Console.WriteLine (tok);
            if (tok.Kind == EOF) break;
        }
    }
}



