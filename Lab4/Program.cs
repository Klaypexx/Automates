class Program
{
    static void Main( string[] args )
    {
        string inputFilePath = args[0];
        string outputFilePath = args[1];

        List<State> inputStates = new();
        List<Epsilon> statesWithEpsilon = new();

        List<State> outputStates = new();

        inputStates = ReadFromCSV(inputFilePath);
        statesWithEpsilon = FindEpsilonStates(inputStates);
        outputStates = NewStates(inputStates, statesWithEpsilon);

        WriteToFile(outputFilePath, outputStates);
    }

    static List<State> ReadFromCSV( string inputFileName )
    {
        string[] lines = File.ReadAllLines(inputFileName);

        // Первая строка используется для определения конечных состояний
        List<string> finalStates = lines[0].Split(new char[] { ';' }, StringSplitOptions.None).ToList();

        // Вторая строка используется для определения имен состояний
        List<string> stateNames = lines[1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        // Создаем список состояний
        List<State> states = new List<State>();

        for (int i = 0; i < stateNames.Count; i++)
        {
            states.Add(new State
            {
                StateName = stateNames[i],
                IsFinit = !string.IsNullOrEmpty(finalStates[i + 1]),
                Transitions = new Dictionary<string, List<string>>()
            });
        }

        // Обрабатываем оставшиеся строки для заполнения переходов
        for (int i = 2; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split(new char[] { ';' }, StringSplitOptions.None);
            string transitionName = parts[0];

            for (int j = 1; j < parts.Length; j++)
            {
                if (!string.IsNullOrEmpty(parts[j]))
                {
                    string[] transitions = parts[j].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    states[j - 1].Transitions[transitionName] = new List<string>(transitions);
                }
                else
                {
                    states[j - 1].Transitions[transitionName] = new List<string>();
                }
            }
        }

        return states;
    }

    static List<Epsilon> FindEpsilonStates( List<State> inputStates )
    {
        List<Epsilon> epsilonStates = new List<Epsilon>();

        foreach (var state in inputStates)
        {
            if (state.Transitions.ContainsKey("ε"))
            {
                epsilonStates.Add(new Epsilon
                {
                    StateName = state.StateName,
                    TransitionsName = state.Transitions["ε"]
                });
            }
        }

        return epsilonStates;
    }

    static List<State> NewStates( List<State> inputStates, List<Epsilon> statesWithEpsilon )
    {
        List<State> newStates = [];
        Dictionary<string, List<string>> statesToIterate = new();
        int stateCounter = 0;

        // Инициализация начального состояния
        statesToIterate.Add("s0", [inputStates[0].StateName]);
        List<string> states = ["s0"];

        // Основной перебор
        for (int i = 0; i < statesToIterate.Count; i++)
        {
            string newState = states[i];
            HashSet<string> dependency = GetDependencies(newState, statesToIterate, statesWithEpsilon);

            bool isFinit = dependency.Any(t => inputStates.Any(s => s.StateName == t && s.IsFinit));
            State tempNewState = new()
            {
                StateName = newState,
                IsFinit = isFinit,
                Transitions = new()
            };

            foreach (string symbol in inputStates[0].Transitions.Keys.Where(name => name != "ε"))
            {
                List<string> transitions = inputStates
                    .Where(iState => dependency.Any(s => iState.StateName.Contains(s)))
                    .SelectMany(iState => iState.Transitions[symbol])
                    .ToList();

                string newStateKey = "";
                if (transitions.Count > 0)
                {
                    newStateKey = FindNewStateKey(statesToIterate, transitions);
                }

                if (newStateKey is "" && transitions.Count != 0)
                {
                    stateCounter++;
                    newStateKey = $"s{stateCounter}";

                    statesToIterate[newStateKey] = transitions;
                    states.Add(newStateKey);
                }

                tempNewState.Transitions[symbol] = new()
                {
                    { newStateKey }
                };
            }

            newStates.Add(tempNewState);
        }

        return newStates;
    }


    static HashSet<string> GetDependencies( string newState, Dictionary<string, List<string>> statesToIterate, List<Epsilon> statesWithEpsilon )
    {
        HashSet<string> dependency = [];
        foreach (var state in statesToIterate[newState])
        {
            dependency.Add(state);
            foreach (var epsilonState in statesWithEpsilon)
            {
                if (state == epsilonState.StateName)
                {
                    foreach (var transition in epsilonState.TransitionsName)
                    {
                        dependency.Add(transition);
                    }
                }
            }
        }

        return dependency;
    }

    static string FindNewStateKey( Dictionary<string, List<string>> iterateStates, List<string> transitionsToCheck )
    {
        foreach (var (key, value) in iterateStates)
        {
            // Если найдено совпадение, возвращаем true
            if (value.SequenceEqual(transitionsToCheck))
            {
                return key;
            }
        }

        return "";
    }

    static void WriteToFile( string outputFileName, List<State> outputStates )
    {
        using StreamWriter writer = new StreamWriter(outputFileName);

        // Заголовки для конечных состояний и имен состояний
        writer.Write(";"); // Начинаем с точки с запятой для заголовка конечных состояний
        writer.WriteLine(string.Join(";", outputStates.Select(s => s.IsFinit ? "F" : "")));

        writer.Write(";");
        writer.WriteLine(string.Join(";", outputStates.Select(s => s.StateName)));

        // Создаем строки для переходов
        var rows = new List<List<string>>();
        var symbols = outputStates[0].Transitions.Keys.ToList(); // Получаем символы переходов

        foreach (var symbol in symbols)
        {
            var row = new List<string> { symbol }; // Начинаем новую строку с символа перехода

            foreach (var state in outputStates)
            {
                if (state.Transitions.TryGetValue(symbol, out var nextStates))
                {
                    row.Add(string.Join(",", nextStates)); // Добавляем переходы в строку
                }
                else
                {
                    row.Add(""); // Если нет переходов - добавляем пустую строку
                }
            }

            rows.Add(row); // Добавляем сформированную строку в список строк
        }

        // Записываем строки переходов в файл
        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(";", row)); // Записываем каждую строку в файл
        }
    }

    class State
    {
        public bool IsFinit { get; set; } = false;
        public string StateName { get; set; }
        public Dictionary<string, List<string>> Transitions { get; set; }
    }

    class Epsilon
    {
        public string StateName { get; set; }
        public List<string> TransitionsName { get; set; } = [];
    }
}
