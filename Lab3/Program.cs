using System.Text.RegularExpressions;

namespace GrammarReader
{
    class Program
    {
        static void Main( string[] args )
        {
            string inputFilePath = args[0];
            string outputFilePath = args[1];

            try
            {
                string[] lines = File.ReadAllLines(inputFilePath);

                // Объединяем строки, относящиеся к одному нетерминалу
                List<string> combinedLines = CombineLines(lines);

                // Создаем объект грамматики
                var grammar = new Grammar();

                // Определяем тип грамматики на основе строк
                DetermineGrammarType(combinedLines.ToArray(), grammar);

                if (grammar.Type == GrammarType.LeftSided)
                {
                    ParseLeftHandedGrammar(combinedLines.ToArray(), grammar);
                }
                else if (grammar.Type == GrammarType.RightSided)
                {
                    ParseRightHandedGrammar(combinedLines.ToArray(), grammar);
                }
                else
                {
                    throw new Exception("Тип грамматики не определен");
                }
                // Парсинг грамматики

                WriteToFile(grammar, outputFilePath);


                Console.WriteLine("Grammar Type: " + grammar.Type);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static List<string> CombineLines( string[] lines )
        {
            var combinedLines = new List<string>();
            string currentLine = string.Empty;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (Regex.IsMatch(line, @"^\s*<(\w+)>\s*->"))
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        combinedLines.Add(currentLine.Trim());
                    }
                    currentLine = line.Trim();
                }
                else
                {
                    currentLine += " " + line.Trim();
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                combinedLines.Add(currentLine.Trim());
            }

            return combinedLines;
        }

        static void DetermineGrammarType( string[] lines, Grammar grammar )
        {
            bool isLeftSided = true;
            bool isRightSided = true;

            foreach (var line in lines)
            {
                var parts = line.Split("->", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) continue;

                var lhs = parts[0].Trim();
                var rhs = parts[1].Trim().Split('|').Select(p => p.Trim());

                foreach (var production in rhs)
                {
                    if (Regex.IsMatch(production, @"^<\w+>.*")) // Начинается с нетерминала
                    {
                        isRightSided = false;
                    }
                    if (Regex.IsMatch(production, @".*<\w+>$")) // Заканчивается на нетерминал
                    {
                        isLeftSided = false;
                    }
                }
            }

            if (isLeftSided)
            {
                grammar.Type = GrammarType.LeftSided;
            }
            else if (isRightSided)
            {
                grammar.Type = GrammarType.RightSided;
            }
            else
            {
                grammar.Type = GrammarType.Undefined;
            }
        }

        static void ParseRightHandedGrammar( string[] lines, Grammar grammar )
        {
            var grammarPattern = new Regex(@"^\s*<(\w+)>\s*->\s*([\wε](?:\s+<\w+>)?(?:\s*\|\s*[\wε](?:\s+<\w+>)?)*)\s*$");
            var transitionPattern = new Regex(@"^\s*([\wε]*)\s*(?:<(\w*)>)?\s*$");

            const string finalState = "F";

            foreach (string line in lines.Where(l => grammarPattern.IsMatch(l)))
            {
                var match = grammarPattern.Match(line);
                if (!match.Success) continue;

                string state = match.Groups[1].Value;
                string[] transitions = match.Groups[2].Value.Split('|');

                if (!grammar.Productions.ContainsKey(state))
                {
                    grammar.Productions[state] = new Dictionary<string, List<string>>();
                }

                foreach (string transition in transitions)
                {
                    var transMatch = transitionPattern.Match(transition.Trim());
                    if (!transMatch.Success) continue;

                    string symbol = transMatch.Groups[1].Value;
                    string nextState = transMatch.Groups[2].Success ? transMatch.Groups[2].Value : finalState;

                    if (!grammar.Productions[state].ContainsKey(symbol))
                    {
                        grammar.Productions[state][symbol] = new List<string>();
                    }
                    grammar.Productions[state][symbol].Add(nextState);
                }
            }

            // Добавляем конечное состояние без переходов
            grammar.Productions[finalState] = new Dictionary<string, List<string>>();
            grammar.FinaleState = "F";
        }

        static void ParseLeftHandedGrammar( string[] lines, Grammar grammar )
        {
            var grammarPattern = new Regex(@"^\s*<(\w+)>\s*->\s*((?:<\w+>\s+)?[\wε](?:\s*\|\s*(?:<\w+>\s+)?[\wε])*)\s*$");
            var transitionPattern = new Regex(@"^\s*(?:<(\w*)>)?\s*([\wε]*)\s*$");

            foreach (string line in lines.Where(l => grammarPattern.IsMatch(l)))
            {
                var match = grammarPattern.Match(line);
                if (!match.Success) continue;

                string state = match.Groups[1].Value;
                string[] transitions = match.Groups[2].Value.Split('|');

                if (grammar.FinaleState == null)
                {
                    grammar.FinaleState = state;
                }

                if (!grammar.Productions.ContainsKey(state))
                {
                    grammar.Productions[state] = new Dictionary<string, List<string>>();
                }

                foreach (string transition in transitions)
                {
                    var transMatch = transitionPattern.Match(transition.Trim());
                    if (!transMatch.Success) continue;

                    string symbol = transMatch.Groups[2].Value;
                    string nextState = transMatch.Groups[1].Success ? transMatch.Groups[1].Value : "H";

                    if (!grammar.Productions.ContainsKey(nextState))
                    {
                        grammar.Productions[nextState] = new Dictionary<string, List<string>>();
                        grammar.Productions[nextState][symbol] = new List<string>([state]);
                    }
                    else
                    {
                        if (!grammar.Productions[nextState].ContainsKey(symbol))
                        {
                            grammar.Productions[nextState][symbol] = new List<string>([state]);
                        }
                        else
                        {
                            grammar.Productions[nextState][symbol].Add(state);
                        }
                    }
                }
            }
        }

        static void WriteToFile( Grammar grammar, string outputFileName )
        {
            // Определяем начальное состояние как первое состояние в Productions
            string initialState;
            if (grammar.Type == GrammarType.LeftSided)
            {
                initialState = "H";
            }
            else if (grammar.Type == GrammarType.RightSided)
            {
                initialState = grammar.Productions.Keys.First();
            }
            else
            {
                throw new InvalidOperationException("Grammar does not have any productions.");
            }

            // Собираем все состояния, начиная с начального
            var states = new List<string> { initialState };
            states.AddRange(grammar.Productions.Keys.Where(state => state != initialState));

            // Собираем все символы из грамматики
            var symbols = grammar.Productions.Values
                .SelectMany(transitions => transitions.Keys)
                .Distinct()
                .OrderBy(symbol => symbol)
                .ToList();

            // Создаем заголовки CSV
            var csvHeader1 = new List<string> { "" };
            csvHeader1.AddRange(states.Select(state => grammar.FinaleState == state ? "F" : ""));

            var csvHeader2 = new List<string> { "" };
            csvHeader2.AddRange(Enumerable.Range(0, states.Count).Select(i => $"q{i}"));

            var stateIndexMap = states.Select(( state, index ) => new { state, index })
                                      .ToDictionary(x => x.state, x => $"q{x.index}");

            // Создаем строки для CSV
            var csvRows = new List<List<string>>();

            foreach (var symbol in symbols)
            {
                var row = new List<string> { symbol };
                foreach (var state in states)
                {
                    if (grammar.Productions.TryGetValue(state, out var transitions) && transitions.TryGetValue(symbol, out var nextStates))
                    {
                        var mappedStates = nextStates.Select(nextState => stateIndexMap[nextState]);
                        row.Add(string.Join(",", mappedStates));
                    }
                    else
                    {
                        row.Add("");
                    }
                }
                csvRows.Add(row);
            }

            // Записываем в файл
            using (var writer = new StreamWriter(outputFileName))
            {
                writer.WriteLine(string.Join(";", csvHeader1));
                writer.WriteLine(string.Join(";", csvHeader2));
                foreach (var row in csvRows)
                {
                    writer.WriteLine(string.Join(";", row));
                }
            }
        }



        class Grammar
        {
            public GrammarType Type { get; set; } = GrammarType.Undefined;
            public string FinaleState = null;
            public Dictionary<string, Dictionary<string, List<string>>> Productions { get; } = new Dictionary<string, Dictionary<string, List<string>>>();
        }

        enum GrammarType
        {
            Undefined,
            LeftSided,
            RightSided
        }
    }
}
