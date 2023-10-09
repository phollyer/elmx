using System.Numerics;
using ElmX.Core.Console;
using ElmX.Core.Parser.Whitespace;

namespace ElmX.Core.Parser
{
    public class Parser
    {

        private readonly List<string> Tokens = new()
        {
            "--",
            "{-",
            "module",
            "import"
        };

        public List<Space> Spaces { get; set; } = new();

        public List<Newline> Newlines { get; set; } = new();

        public List<Comment> Comments { get; set; } = new();

        public ModuleStatement? ModuleStatement { get; set; }

        public List<ImportStatement> ImportStatements { get; set; } = new();

        public string Content { get; set; } = "";

        public Parser(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                Content = System.IO.File.ReadAllText(filePath).Trim();
            }
            else
            {
                Writer.WriteLine($"ElmX.Elm.Lexer:\tThe file '{filePath}' does not exist.");
                Environment.Exit(0);
            }
        }

        public void Parse()
        {
            int counter = 0;

            string token = "";

            while (counter < Content.Length)
            {
                counter = Parse(counter, Content, token);
            }

            if (ModuleStatement != null)
            {
                Writer.WriteLine(ModuleStatement.ToString());
            }

            foreach (ImportStatement importStatement in ImportStatements)
            {
                Writer.WriteLine(importStatement.ToString());
            }
        }

        private int Parse(int index, string content, string token)
        {
            for (int i = index; i < content.Length; i++)
            {
                if (content[i] == '\n')
                {
                    Newlines.Add(new Newline(i));
                    return i + 1;
                }

                if (content[i] == ' ')
                {
                    Spaces.Add(new Space(i));
                    return i + 1;
                }

                token += content[i];

                if (Tokens.Contains(token))
                {
                    index = ParseToken(token, i, content);
                    return index;
                }

                index = i;
            }
            return index + 1;
        }
        private int ParseToken(string token, int index, string content)
        {
            int endIndex = 0;

            switch (token)
            {
                case "--":
                case "{-":
                    (Comment comment, int commentEndIndex)? commentResult = Comment.Parse(token, index, content);

                    if (commentResult != null)
                    {
                        Comments.Add(commentResult.Value.comment);

                        endIndex = commentResult.Value.commentEndIndex;
                    }

                    break;

                case "module":
                    if (content[index + 1] == ' ' || content[index + 1] == '\n' || content[index + 1] == '{')
                    {
                        (ModuleStatement moduleStatement, int moduleStatementEndIndex) moduleStatementResult = ModuleStatement.Parse(index + 1, content);

                        ModuleStatement = moduleStatementResult.moduleStatement;
                        endIndex = moduleStatementResult.moduleStatementEndIndex;
                    }
                    break;

                case "import":
                    if (content[index + 1] == ' ' || content[index + 1] == '\n' || content[index + 1] == '{')
                    {
                        (ImportStatement importStatement, int importStatementEndIndex) = ImportStatement.Parse(index + 1, content);
                        ImportStatements.Add(importStatement);
                        endIndex = importStatementEndIndex;
                    }
                    break;

                default:
                    endIndex = index + 1;
                    break;
            }

            return endIndex;
        }
    }

    public class Evaluator
    {
        private bool Found { get; set; } = false;

        public bool AllImportsFound { get; set; } = false;
        public int Index { get; set; } = 0;
        public string Content { get; set; } = "";

        public Token? Token;

        public Evaluator(int index, bool found, string content)
        {
            Index = index;
            Found = found;
            Content = content;
        }

        // Module Statement

        public Evaluator ShouldBeModuleStatement()
        {
            if (Found)
            {
                return this;
            }

            string maybeWord = Content[Index..(Index + 7)];

            if (maybeWord == "module " || maybeWord == "module\n")
            {
                int endIndex = FindEndOfStatement(Index + 6);
                string value = Content[Index..endIndex];

                Token = new(value, TokenType.ModuleStatement, Index, endIndex);

                Found = true;
                Index = endIndex;
            }

            return this;

        }


        // Import Statements

        public Evaluator MaybeImportStatement()
        {
            if (Found)
            {
                return this;
            }

            string maybeWord = Content[Index..(Index + 7)];

            if (maybeWord == "import " || maybeWord == "import\n")
            {
                int endIndex = FindEndOfStatement(Index + 6);
                string value = Content[Index..endIndex];

                Token = new(value, TokenType.ImportStatement, Index, endIndex);

                Found = true;
                Index = endIndex;

                string maybeNextImport = Content[(endIndex + 1)..(endIndex + 8)];
                AllImportsFound = maybeNextImport != "import " && maybeNextImport != "import\n";
            }

            return this;
        }


        // Type Statements

        public Evaluator MaybeTypeStatement()
        {
            if (Found)
            {
                return this;
            }

            string maybeWord = Content[Index..(Index + 5)];

            if (maybeWord == "type " || maybeWord == "type\n")
            {
                int endIndex = FindEndOfStatement(Index + 4);
                string value = Content[Index..endIndex];

                TokenType type = TokenType.None;

                for (int i = 0; i < value.Length; i++)
                {
                    if (Char.IsUpper(value[i]))
                    {
                        type = TokenType.TypeEnum;
                        break;
                    }
                    else if (value[i] == 'a')
                    {
                        type = TokenType.TypeAlias;
                        break;
                    }
                }

                Token = new(value, type, Index, endIndex);

                Found = true;
                Index = endIndex;
            }

            return this;
        }


        // Functions

        public Evaluator ShouldBeAFunction()
        {
            if (Found)
            {
                return this;
            }

            int endIndex = FindEndOfStatement(Index);

            string value = Content[Index..endIndex];

            int startOfFuncBodyIndex = endIndex + 1;
            int endOfFunctionBodyIndex = startOfFuncBodyIndex;

            string func = "";

            if (IsFunctionAnnotation(value))
            {
                endOfFunctionBodyIndex = FindEndOfStatement(startOfFuncBodyIndex);

                string body = Content[startOfFuncBodyIndex..endOfFunctionBodyIndex];

                func = $"{value}\n{body}";
            }
            else if (IsFunctionHead(value))
            {
                func = value;

            }
            Token = new(func, TokenType.Function, Index, endOfFunctionBodyIndex);
            Found = true;
            Index = endOfFunctionBodyIndex;

            return this;
        }

        private bool IsFunctionAnnotation(string value)
        {
            bool isAnnotation = false;

            if (value.Contains(':'))
            {
                string funcName = value.Split(':')[0].Trim();

                if (Char.IsLower(funcName[0]))
                {
                    isAnnotation = true;
                }

                for (int i = 1; i < funcName.Length - 1; i++)
                {
                    char c = funcName[i];

                    if (!Char.IsLetterOrDigit(c) && c != '_')
                    {
                        isAnnotation = false;
                        break;
                    }
                }
            }

            return isAnnotation;
        }

        private bool IsFunctionHead(string value)
        {
            string head = value.Split('\n')[0];

            return head.EndsWith("=");

        }

        private int FindEndOfStatement(int index)
        {
            int endIndex = index;

            for (int i = index; i < Content.Length - 1; i++)
            {
                char c = Content[i];
                char next = Content[i + 1];

                if (c == '\n' && Char.IsLetter(next))
                {
                    endIndex = i;
                    break;
                }
                else if (i == Content.Length - 2)
                {
                    endIndex = Content.Length;
                    break;
                }
            }

            return endIndex;
        }

        // Comments
        public Evaluator MaybeInlineComment()
        {
            if (Found)
            {
                return this;
            }

            if (Content[Index + 1] == '-')
            {
                int endIndex = Content.IndexOf('\n', Index);

                if (endIndex == -1)
                {
                    endIndex = Content.Length;
                }

                string value = Content[Index..endIndex];

                Token = new(value, TokenType.InlineComment, Index, endIndex);

                Found = true;
                Index = endIndex - 1;
            }

            return this;
        }

        public Evaluator MaybeMultilineComment()
        {
            if (Found)
            {
                return this;
            }

            if (Content[Index + 1] == '-')
            {
                int startIndex = Index;
                int endIndex = FindEndOfMultilineComment((0, 0), startIndex);
                string value = Content[Index..endIndex];

                Token = new(value, TokenType.MultilineComment, Index, endIndex);

                Found = true;
                Index = endIndex - 1;
            }

            return this;
        }

        private int FindEndOfMultilineComment((int start, int end) counter, int index)
        {
            if (Content[index] == '{' && Content[index + 1] == '-')
            {
                counter.start++;
            }
            else if (Content[index] == '-' && Content[index + 1] == '}')
            {
                counter.end++;
            }

            if (counter.start == counter.end)
            {
                return index + 2;
            }

            return FindEndOfMultilineComment(counter, index + 1);
        }
    }

    public class Token
    {
        public string Value { get; set; } = "";
        public TokenType Type { get; set; } = TokenType.None;

        public int StartIndex { get; set; } = 0;
        public int EndIndex { get; set; } = 0;

        public Token(string value, TokenType type, int startIndex, int endIndex)
        {
            Value = value;
            Type = type;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    public enum TokenType
    {
        None,

        ImportStatement,
        CommentedImportStatement,
        InlineComment,
        ModuleStatement,
        MultilineComment,
        TypeAlias,

        TypeEnum,

        Function,

    }

}