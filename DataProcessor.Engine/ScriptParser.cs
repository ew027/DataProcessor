using System;
using System.Collections.Generic;
using DataProcessor.Engine.Operations;

namespace DataProcessor.Engine
{
    // ── Token types ───────────────────────────────────────────────────────────
    internal enum TokenType
    {
        Keyword, Identifier, Integer, Double, String,
        LeftParen, RightParen, Equals, Dot, Slash, Comma, EOF
    }

    internal class Token
    {
        public TokenType Type { get; }
        public string Text { get; }
        public Token(TokenType type, string text) { Type = type; Text = text; }
        public override string ToString() => $"[{Type} '{Text}']";
    }

    // ── Parser ────────────────────────────────────────────────────────────────
    public class ScriptParser
    {
        // Reserved keywords
        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "RECODE", "INTO", "COMPUTE", "SUM", "SORT", "CASES", "BY",
            "KEEP", "VARIABLES", "RENAME", "VALUE", "LABELS", "VARIABLE",
            "SELECT", "IF", "SAVE", "OUTFILE", "AGGREGATE", "BREAK",
            "DATASET", "NAME", "IP", "CHECK", "FROM", "SPLIT", "MISSING",
            "BLANK", "SURVEY", "SIG", "TEST", "ABOVE", "BELOW", "GENDER",
            "CREATE", "SYSMIS", "LO", "HI", "THRU", "ELSE", "MEAN", "COUNT"
        };

        private string _text = "";
        private int _pos;
        private string _currentDataset = "";

        /// <summary>
        /// Parses SPSS-like script text into a list of Operation objects.
        /// Statements end with '.'; '*' starts a comment line.
        /// </summary>
        public List<object> ParseScript(string text, string? defaultDataset = null)
        {
            _text = text;
            _pos = 0;
            _currentDataset = defaultDataset ?? "";
            var ops = new List<object>();

            while (_pos < _text.Length)
            {
                SkipWhitespaceAndComments();
                if (_pos >= _text.Length) break;

                try
                {
                    var op = ParseStatement();
                    if (op != null) ops.Add(op);
                }
                catch (Exception ex)
                {
                    // Log parse error and skip to next '.'
                    ops.Add(new CommentOperation($"# PARSE ERROR: {ex.Message}"));
                    SkipToEndOfStatement();
                }
            }

            return ops;
        }

        // ── Statement dispatch ────────────────────────────────────────────────

        private object? ParseStatement()
        {
            if (_pos >= _text.Length) return null;

            string keyword = PeekKeyword();
            switch (keyword.ToUpperInvariant())
            {
                case "DATASET": return ParseDatasetName();
                case "RECODE":  return ParseRecode();
                case "COMPUTE": return ParseCompute();
                case "SORT":    return ParseSort();
                case "KEEP":    return ParseKeep();
                case "RENAME":  return ParseRename();
                case "VALUE":   return ParseValueLabels();
                case "VARIABLE": return ParseVariableLabel();
                case "SELECT":  return ParseSelectIf();
                case "SAVE":    return ParseSave();
                case "AGGREGATE": return ParseAggregate();
                case "IP":      return ParseIpCheck();
                case "SPLIT":   return ParseSplitVar();
                case "BLANK":   return ParseBlankSurvey();
                case "SIG":     return ParseSigTest();
                case "ABOVE":   return ParseAboveBelow();
                case "GENDER":  return ParseGenderRecode();
                case "CREATE":  return ParseCreateVariable();
                default:
                    SkipToEndOfStatement();
                    return null;
            }
        }

        // ── Individual parsers ────────────────────────────────────────────────

        private object ParseDatasetName()
        {
            Expect("DATASET"); Expect("NAME");
            _currentDataset = ReadIdentifier();
            ExpectDot();
            return new CommentOperation($"# DATASET NAME {_currentDataset}");
        }

        private object ParseRecode()
        {
            Expect("RECODE");
            string varname = ReadIdentifier();
            bool hasInto = PeekKeyword().Equals("INTO", StringComparison.OrdinalIgnoreCase);

            if (hasInto)
            {
                Expect("INTO");
                string targetVar = ReadIdentifier();
                var recodes = ReadRecodeMap();
                ExpectDot();
                // Decide between RecodeInto and RecodeRange based on content
                string args = BuildArgs(_currentDataset, targetVar, varname, RecodeMapToString(recodes));
                return new RecodeIntoOperation(args);
            }
            else
            {
                var recodes = ReadRecodeMap();
                ExpectDot();
                string args = BuildArgs(_currentDataset, varname, RecodeMapToString(recodes));
                return new RecodeOperation(args);
            }
        }

        private object ParseCompute()
        {
            Expect("COMPUTE");
            string targetVar = ReadIdentifier();
            ExpectChar('=');
            string expr = ReadUntilDot();
            ExpectDot();

            // Detect SUM(v1,v2,...) form
            if (expr.TrimStart().StartsWith("SUM(", StringComparison.OrdinalIgnoreCase))
            {
                string inner = expr.Substring(expr.IndexOf('(') + 1).TrimEnd().TrimEnd(')');
                string args = BuildArgs(_currentDataset, targetVar, inner);
                return new ComputeSumOperation(args);
            }

            string exprArgs = BuildArgs(_currentDataset, targetVar, expr.Trim());
            return new ComputeExprOperation(exprArgs);
        }

        private object ParseSort()
        {
            Expect("SORT"); Expect("CASES"); Expect("BY");
            string varname = ReadIdentifier();
            // Optional (A) or (D) — skip
            if (PeekChar() == '(') { SkipToChar(')'); Advance(); }
            ExpectDot();
            return new SortOperation(BuildArgs(_currentDataset, varname));
        }

        private object ParseKeep()
        {
            Expect("KEEP"); Expect("VARIABLES");
            var vars = ReadIdentifierList();
            ExpectDot();
            return new KeepVarsOperation(BuildArgs(_currentDataset, string.Join(",", vars)));
        }

        private object ParseRename()
        {
            Expect("RENAME"); Expect("VARIABLES");
            var pairs = new List<string>();
            while (PeekChar() == '(')
            {
                Advance(); // '('
                string oldName = ReadIdentifier();
                ExpectChar('=');
                string newName = ReadIdentifier();
                ExpectChar(')');
                pairs.Add($"{oldName}={newName}");
            }
            ExpectDot();
            return new RenameVarsOperation(BuildArgs(_currentDataset, string.Join(";", pairs)));
        }

        private object ParseValueLabels()
        {
            Expect("VALUE"); Expect("LABELS");
            string varname = ReadIdentifier();
            var labels = new List<string>();
            while (_pos < _text.Length && PeekChar() != '.')
            {
                SkipWhitespace();
                if (!char.IsDigit(Peek()) && Peek() != '-') break;
                int value = ReadInteger();
                SkipWhitespace();
                string label = ReadQuotedString();
                labels.Add($"{value}={label}");
            }
            ExpectDot();
            return new AddValueLabelsOperation(BuildArgs(_currentDataset, varname, string.Join(";", labels)));
        }

        private object ParseVariableLabel()
        {
            Expect("VARIABLE"); Expect("LABELS");
            string varname = ReadIdentifier();
            string label = ReadQuotedString();
            ExpectDot();
            return new AddVariableLabelOperation(BuildArgs(_currentDataset, varname, label));
        }

        private object ParseSelectIf()
        {
            Expect("SELECT"); Expect("IF");
            string condition = ReadParenthesizedExpr();
            ExpectDot();
            // Map to SetRemoveFlag pattern: parse "flagvar = 0" style condition
            // For now, surface as a comment — full implementation in Phase 3
            return new CommentOperation($"# SELECT IF {condition} — wire to SetRemoveFlagOperation manually");
        }

        private object ParseSave()
        {
            Expect("SAVE");
            ExpectKeyword("OUTFILE");
            ExpectChar('=');
            string path = ReadQuotedString();
            ExpectDot();
            // Determine file type from extension
            string fileType = path.EndsWith(".sav", StringComparison.OrdinalIgnoreCase) ? "Spss" : "Csv";
            return new ExportOperation(BuildArgs(_currentDataset, "New", fileType, path));
        }

        private object ParseAggregate()
        {
            // AGGREGATE /OUTFILE=newds /BREAK=bv /mean1=MEAN(m1) /count1=COUNT(c1)
            Expect("AGGREGATE");
            string outfile = ""; string breakVar = "";
            var meanVars = new List<string>(); var keepVars = new List<string>(); var countVars = new List<string>();

            while (_pos < _text.Length && PeekChar() != '.')
            {
                SkipWhitespace();
                if (PeekChar() != '/') break;
                Advance();
                string sub = ReadIdentifier().ToUpperInvariant();
                ExpectChar('=');
                if (sub == "OUTFILE") { outfile = ReadIdentifier(); }
                else if (sub == "BREAK") { breakVar = ReadIdentifier(); }
                else
                {
                    // target=MEAN(src) or target=COUNT(src)
                    string func = ReadIdentifier().ToUpperInvariant();
                    string inner = ReadParenthesizedExpr();
                    if (func == "MEAN") meanVars.Add(inner);
                    else if (func == "COUNT") countVars.Add(inner);
                }
            }
            ExpectDot();
            string args = BuildArgs(_currentDataset, outfile, breakVar,
                string.Join(",", keepVars), string.Join(",", meanVars), string.Join(",", countVars));
            return new AggregateOperation(args);
        }

        private object ParseIpCheck()
        {
            Expect("IP"); Expect("CHECK");
            string targetVar = ReadIdentifier();
            Expect("FROM");
            string ipVar = ReadIdentifier();
            ExpectDot();
            return new IPCheckOperation(BuildArgs(_currentDataset, targetVar, ipVar));
        }

        private object ParseSplitVar()
        {
            Expect("SPLIT");
            string targetVar = ReadIdentifier();
            Expect("INTO");
            var values = ReadIdentifierList();
            string missing = "99";
            if (PeekKeyword().Equals("MISSING", StringComparison.OrdinalIgnoreCase))
            {
                Expect("MISSING"); ExpectChar('=');
                missing = ReadIdentifier();
            }
            ExpectDot();
            return new SplitVarOperation(BuildArgs(_currentDataset, targetVar,
                string.Join(",", values), missing));
        }

        private object ParseBlankSurvey()
        {
            Expect("BLANK"); Expect("SURVEY");
            string targetVar = ReadIdentifier();
            Expect("FROM");
            var vars = ReadIdentifierList();
            ExpectDot();
            return new BlankSurveyOperation(BuildArgs(_currentDataset, targetVar, string.Join(",", vars)));
        }

        private object ParseSigTest()
        {
            // Complex — surface as comment for now
            string rest = ReadUntilDot(); ExpectDot();
            return new CommentOperation($"# SIG TEST {rest} — use pipe format for SigTest");
        }

        private object ParseAboveBelow()
        {
            Expect("ABOVE"); Expect("BELOW");
            string targetVar = ReadIdentifier();
            Expect("FROM");
            string sourceVar = ReadIdentifier();
            string aboveVar = ""; string belowVar = "";
            while (_pos < _text.Length && PeekChar() != '.')
            {
                SkipWhitespace();
                if (!char.IsLetter(Peek())) break;
                string key = ReadIdentifier().ToUpperInvariant();
                ExpectChar('=');
                string val = ReadIdentifier();
                if (key == "ABOVE") aboveVar = val;
                else if (key == "BELOW") belowVar = val;
            }
            ExpectDot();
            return new AboveBelowOperation(BuildArgs(_currentDataset, sourceVar, aboveVar, belowVar));
        }

        private object ParseGenderRecode()
        {
            Expect("GENDER"); Expect("RECODE");
            string targetVar = ReadIdentifier();
            Expect("FROM");
            string genderVar = ReadIdentifier();
            int genderValue = 0;
            if (PeekKeyword().Equals("VALUE", StringComparison.OrdinalIgnoreCase))
            {
                Expect("VALUE"); ExpectChar('=');
                genderValue = ReadInteger();
            }
            ExpectDot();
            return new GenderRecodeOperation(BuildArgs(_currentDataset, targetVar, genderVar, genderValue.ToString()));
        }

        private object ParseCreateVariable()
        {
            Expect("CREATE"); Expect("VARIABLE");
            string varname = ReadIdentifier();
            ExpectChar('=');
            int value = ReadInteger();
            ExpectDot();
            return new CreateIntVarOperation(BuildArgs(_currentDataset, varname, value.ToString()));
        }

        // ── Tokenizer helpers ─────────────────────────────────────────────────

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
        }

        private void SkipWhitespaceAndComments()
        {
            while (_pos < _text.Length)
            {
                SkipWhitespace();
                if (_pos < _text.Length && _text[_pos] == '*')
                {
                    // Skip to end of line
                    while (_pos < _text.Length && _text[_pos] != '\n') _pos++;
                }
                else break;
            }
        }

        private char Peek() => _pos < _text.Length ? _text[_pos] : '\0';
        private char PeekChar() { SkipWhitespace(); return Peek(); }
        private void Advance() => _pos++;

        private string PeekKeyword()
        {
            int saved = _pos;
            SkipWhitespace();
            var sb = new System.Text.StringBuilder();
            while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_'))
                sb.Append(_text[_pos++]);
            _pos = saved;
            return sb.ToString();
        }

        private void Expect(string keyword)
        {
            SkipWhitespace();
            foreach (char c in keyword)
            {
                if (_pos >= _text.Length || char.ToUpperInvariant(_text[_pos]) != char.ToUpperInvariant(c))
                    throw new Exception($"Expected '{keyword}' at position {_pos}");
                _pos++;
            }
        }

        private void ExpectKeyword(string keyword) => Expect(keyword);

        private void ExpectChar(char c)
        {
            SkipWhitespace();
            if (_pos >= _text.Length || _text[_pos] != c)
                throw new Exception($"Expected '{c}' at position {_pos}");
            _pos++;
        }

        private void ExpectDot()
        {
            SkipWhitespace();
            if (_pos < _text.Length && _text[_pos] == '.') _pos++;
        }

        private string ReadIdentifier()
        {
            SkipWhitespace();
            var sb = new System.Text.StringBuilder();
            while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_'))
                sb.Append(_text[_pos++]);
            return sb.ToString();
        }

        private int ReadInteger()
        {
            SkipWhitespace();
            bool negative = false;
            if (_pos < _text.Length && _text[_pos] == '-') { negative = true; _pos++; }
            var sb = new System.Text.StringBuilder();
            while (_pos < _text.Length && char.IsDigit(_text[_pos])) sb.Append(_text[_pos++]);
            int result = int.Parse(sb.ToString());
            return negative ? -result : result;
        }

        private string ReadQuotedString()
        {
            SkipWhitespace();
            if (_pos >= _text.Length || (_text[_pos] != '\'' && _text[_pos] != '"'))
                return ReadIdentifier();
            char quote = _text[_pos++];
            var sb = new System.Text.StringBuilder();
            while (_pos < _text.Length && _text[_pos] != quote) sb.Append(_text[_pos++]);
            if (_pos < _text.Length) _pos++; // closing quote
            return sb.ToString();
        }

        private List<string> ReadIdentifierList()
        {
            var list = new List<string>();
            SkipWhitespace();
            while (_pos < _text.Length && (char.IsLetter(_text[_pos]) || _text[_pos] == '_'))
            {
                list.Add(ReadIdentifier());
                SkipWhitespace();
                if (_pos < _text.Length && _text[_pos] == ',') { _pos++; SkipWhitespace(); }
            }
            return list;
        }

        private string ReadParenthesizedExpr()
        {
            SkipWhitespace();
            if (PeekChar() != '(') return ReadUntilDot();
            Advance();
            int depth = 1;
            var sb = new System.Text.StringBuilder();
            while (_pos < _text.Length && depth > 0)
            {
                char c = _text[_pos++];
                if (c == '(') depth++;
                else if (c == ')') { depth--; if (depth == 0) break; }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private string ReadUntilDot()
        {
            var sb = new System.Text.StringBuilder();
            while (_pos < _text.Length && _text[_pos] != '.') sb.Append(_text[_pos++]);
            return sb.ToString().Trim();
        }

        private void SkipToEndOfStatement()
        {
            while (_pos < _text.Length && _text[_pos] != '.') _pos++;
            if (_pos < _text.Length) _pos++;
        }

        private void SkipToChar(char target)
        {
            while (_pos < _text.Length && _text[_pos] != target) _pos++;
        }

        // ── Recode helpers ────────────────────────────────────────────────────

        private Dictionary<string, string> ReadRecodeMap()
        {
            var map = new Dictionary<string, string>();
            SkipWhitespace();
            while (_pos < _text.Length && _text[_pos] == '(')
            {
                _pos++; // '('
                SkipWhitespace();
                string from = ReadRecodeValue();
                SkipWhitespace();
                ExpectChar('=');
                SkipWhitespace();
                string to = ReadRecodeValue();
                SkipWhitespace();
                if (_pos < _text.Length && _text[_pos] == ')') _pos++;
                map[from] = to;
                SkipWhitespace();
            }
            return map;
        }

        private string ReadRecodeValue()
        {
            SkipWhitespace();
            if (_pos >= _text.Length) return "";
            // Check for SYSMIS, ELSE, LO, HI
            string kw = PeekKeyword().ToUpperInvariant();
            if (kw == "SYSMIS") { Expect("SYSMIS"); return "sysmis"; }
            if (kw == "ELSE")   { Expect("ELSE");   return "else"; }
            if (kw == "LO")     { Expect("LO");      return "lo"; }
            if (kw == "HI")     { Expect("HI");      return "hi"; }
            // Numeric
            bool neg = false;
            if (_pos < _text.Length && _text[_pos] == '-') { neg = true; _pos++; }
            var sb = new System.Text.StringBuilder();
            if (neg) sb.Append('-');
            while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
                sb.Append(_text[_pos++]);
            // Check for THRU
            SkipWhitespace();
            if (PeekKeyword().Equals("THRU", StringComparison.OrdinalIgnoreCase))
            {
                Expect("THRU");
                string high = ReadRecodeValue();
                return $"{sb}~{high}";
            }
            return sb.ToString();
        }

        private static string RecodeMapToString(Dictionary<string, string> map)
        {
            var parts = new List<string>();
            foreach (var kv in map) parts.Add($"{kv.Key}={kv.Value}");
            return string.Join(";", parts);
        }

        private static string BuildArgs(params string[] parts) =>
            string.Join("|", parts);
    }
}
