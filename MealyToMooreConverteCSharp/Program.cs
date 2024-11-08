using System.Collections.Generic;

class AutomataConverter
{
    const string MOORE_STATE = "R";

    struct MooreAutomata
    {
        public Dictionary<string, string> Outputs;
        public List<List<string>> Transitions;
        public HashSet<string> States;
        public HashSet<string> Inputs;
    }

    struct MealyAutomata
    {
        public List<List<(string NextState, string Output)>> Transitions;
        public HashSet<string> States;
        public HashSet<string> Inputs;
    }

    static MooreAutomata ReadMoore( string inputFile )
    {
        var aut = new MooreAutomata
        {
            Outputs = new Dictionary<string, string>(),
            Transitions = new List<List<string>>(),
            States = new HashSet<string>(),
            Inputs = new HashSet<string>()
        };

        using (var file = new StreamReader(inputFile))
        {
            // Чтение выходных символов
            var line = file.ReadLine();
            var outputSymbols = line.Split(';').Skip(1).ToList();

            // Чтение имен состояний
            line = file.ReadLine();
            var stateNames = line.Split(';').Skip(1).ToList();
            for (int i = 0; i < stateNames.Count; i++)
            {
                var state = stateNames[i];
                aut.States.Add(state);
                aut.Outputs[state] = outputSymbols[i];
            }

            // Чтение переходов
            while ((line = file.ReadLine()) != null)
            {
                var parts = line.Split(';');
                var input = parts[0];
                aut.Inputs.Add(input);
                aut.Transitions.Add(parts.Skip(1).ToList());
            }
        }

        return aut;
    }

    static MealyAutomata ReadMealy( string inputFile )
    {
        var mealyAutomata = new MealyAutomata
        {
            Transitions = new List<List<(string NextState, string Output)>>(),
            States = new HashSet<string>(),
            Inputs = new HashSet<string>()
        };

        using (var file = new StreamReader(inputFile))
        {
            // Чтение заголовка (состояния)
            var line = file.ReadLine();
            var stateNames = line.Split(';').Skip(1).ToList();

            foreach (var state in stateNames)
            {
                mealyAutomata.States.Add(state);
            }

            // Чтение переходов
            while ((line = file.ReadLine()) != null)
            {
                var parts = line.Split(';');
                var input = parts[0];

                mealyAutomata.Inputs.Add(input);

                var transitions = parts.Skip(1)
                    .Select(transition =>
                    {
                        var parts = transition.Split('/');
                        return (NextState: parts[0], Output: parts[1]);
                    })
                    .ToList();

                mealyAutomata.Transitions.Add(transitions);
            }
        }

        return mealyAutomata;
    }

    //optimize logic
    static MooreAutomata RemoveUnreachableStatesMoore( MooreAutomata moore )
    {
        // Предполагаем, что первое состояние в списке является начальным
        var initialState = moore.States.First();

        // Множество достижимых состояний
        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(initialState);
        reachableStates.Add(initialState);

        // Построение быстрого доступа к индексам состояний
        var stateList = moore.States.ToList();
        var stateIndexMap = stateList.Select(( state, index ) => new { state, index })
                                     .ToDictionary(x => x.state, x => x.index);

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            if (!stateIndexMap.TryGetValue(currentState, out var currentIndex))
            {
                continue;
            }

            foreach (var transition in moore.Transitions)
            {
                var nextState = transition[currentIndex];
                if (reachableStates.Add(nextState))
                {
                    queue.Enqueue(nextState);
                }
            }
        }

        // Фильтрация переходов
        var filteredTransitions = new List<List<string>>();
        foreach (var transition in moore.Transitions)
        {
            var filteredStateTransitions = stateList
                .Where(reachableStates.Contains)
                .Select(state => transition[stateIndexMap[state]])
                .ToList();

            filteredTransitions.Add(filteredStateTransitions);
        }

        moore.States.IntersectWith(reachableStates);

        moore.Outputs = moore.Outputs
            .Where(o => reachableStates.Contains(o.Key))
            .ToDictionary(o => o.Key, o => o.Value);

        // Обновление состояний и выходов
        moore.Transitions = filteredTransitions;

        return moore;
    }

    //optimize logic
    static MealyAutomata RemoveUnreachableStatesMealy( MealyAutomata mealy )
    {
        // Предполагаем, что первое состояние в списке является начальным
        var initialState = mealy.States.First();

        // Множество достижимых состояний
        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(initialState);
        reachableStates.Add(initialState);

        // Построение быстрого доступа к индексам состояний
        var stateList = mealy.States.ToList();
        var stateIndexMap = stateList.Select(( state, index ) => new { state, index })
                                     .ToDictionary(x => x.state, x => x.index);

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();

            if (!stateIndexMap.TryGetValue(currentState, out var currentIndex))
            {
                continue;
            }

            foreach (var transition in mealy.Transitions)
            {
                var nextState = transition[currentIndex].NextState;
                if (reachableStates.Add(nextState))
                {
                    queue.Enqueue(nextState);
                }
            }
        }

        // Фильтрация переходов
        var filteredTransitions = new List<List<(string NextState, string Output)>>();
        foreach (var transition in mealy.Transitions)
        {
            var filteredStateTransitions = stateList
                .Where(reachableStates.Contains)
                .Select(state => transition[stateIndexMap[state]])
                .ToList();

            filteredTransitions.Add(filteredStateTransitions);
        }

        // Обновление состояний
        mealy.States.IntersectWith(reachableStates);

        mealy.Transitions = filteredTransitions;

        return mealy;
    }

    static MealyAutomata ConvertMooreToMealy( MooreAutomata moore )
    {
        var mealy = new MealyAutomata
        {
            States = new HashSet<string>(moore.States),
            Inputs = new HashSet<string>(moore.Inputs),
            Transitions = new List<List<(string, string)>>()
        };

        var stateIndexMap = moore.States.Select(( state, index ) => new { state, index }).ToDictionary(x => x.state, x => x.index);

        for (int inputIndex = 0; inputIndex < moore.Inputs.Count; inputIndex++)
        {
            var mealyTransitions = new List<(string, string)>();
            for (int stateIndex = 0; stateIndex < moore.States.Count; stateIndex++)
            {
                var nextState = moore.Transitions[inputIndex][stateIndex];
                if (!string.IsNullOrEmpty(nextState))
                {
                    var output = moore.Outputs[nextState];
                    mealyTransitions.Add((nextState, output));
                }
            }
            mealy.Transitions.Add(mealyTransitions);
        }

        return mealy;
    }

    static MooreAutomata ConvertMealyToMoore( MealyAutomata mealy )
    {
        var moore = new MooreAutomata
        {
            Outputs = new Dictionary<string, string>(),
            Transitions = new List<List<string>>(),
            States = new HashSet<string>(),
            Inputs = new HashSet<string>(mealy.Inputs)
        };

        var uniqueStateOutputPairs = new HashSet<(string NextState, string Output)>();
        var sortedNewStateMap = new Dictionary<(string NextState, string Output), string>();
        int newStateIndex = 0;


        foreach (var state in mealy.States)
        {
            bool hasTransitions = false;
            foreach (var row in mealy.Transitions)
            {
                foreach (var (nextState, output) in row)
                {
                    if (nextState == state)
                    {
                        hasTransitions = true;
                        var combined = (nextState, output);
                        if (!uniqueStateOutputPairs.Contains(combined))
                        {
                            uniqueStateOutputPairs.Add(combined);
                            string newState = "q" + newStateIndex++;
                            sortedNewStateMap[combined] = newState;
                            moore.States.Add(newState);
                            moore.Outputs[newState] = output;
                        }
                    }
                }
            }

            // Если ни одно состояние не найдено, добавить новое состояние с пустым выходом
            if (!hasTransitions)
            {
                string newState = "q" + newStateIndex++;
                sortedNewStateMap[(state, "")] = newState;
                moore.States.Add(newState);
                moore.Outputs[newState] = ""; // Пустой выход
            }
        }

        // Заполнить переходы для автомата Мура
        int index = 0;
        for (int inputIndex = 0; inputIndex < mealy.Transitions.Count; inputIndex++)
        {
            int stateIndex = 0;
            var mooreTransitions = new List<string>(new string[sortedNewStateMap.Count]);

            foreach (string mealyState in mealy.States)
            {
                string newState = sortedNewStateMap[mealy.Transitions[inputIndex][stateIndex]];
                stateIndex++;
                foreach (var state in sortedNewStateMap.Keys)
                {
                    if (mealyState == state.NextState)
                    {
                        mooreTransitions[index] = newState;
                    }
                    index++;
                }
                index = 0;
            }
            moore.Transitions.Add(mooreTransitions);
            index = 0;
        }

        return moore;
    }

    static void PrintMealy( MealyAutomata mealy, string outputFile )
    {
        using (StreamWriter writer = new StreamWriter(outputFile))
        {
            var header = ";" + string.Join(";", mealy.States);
            writer.WriteLine(header);

            var inputs = mealy.Inputs.ToList();

            for (int i = 0; i < inputs.Count; i++)
            {
                var line = inputs[i];
                for (int j = 0; j < mealy.States.Count; j++)
                {
                    var transition = mealy.Transitions[i][j];
                    line += $";{transition.NextState}/{transition.Output}";
                }
                writer.WriteLine(line);
            }
        }
    }

    static void PrintMoore( MooreAutomata moore, string outputFile )
    {
        using (StreamWriter writer = new StreamWriter(outputFile))
        {
            var outputs = ";" + string.Join(";", moore.Outputs.Select(x => $"{x.Value}"));
            writer.WriteLine(outputs);

            var header = ";" + string.Join(";", moore.States.Select(x => $"{x}"));
            writer.WriteLine(header);

            var inputs = moore.Inputs.ToList();

            for (int i = 0; i < inputs.Count; i++)
            {
                var line = inputs[i];
                for (int j = 0; j < moore.States.Count; j++)
                {
                    var nextState = moore.Transitions[i][j];
                    line += $";{nextState}";
                }
                writer.WriteLine(line);
            }
        }
    }

    //for prod
    /*static void Main( string[] args )
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: program <mealy-to-moore|moore-to-mealy> <input.csv> <output.csv>");
            return;
        }

        var command = args[0];
        var inputFile = args[1];
        var outputFile = args[2];

        if (command == "mealy-to-moore")
        {
            var mealyAut = ReadMealy(inputFile);
            mealyAut = RemoveUnreachableStatesMealy(mealyAut);
            var mooreAut = ConvertMealyToMoore(mealyAut);
            PrintMoore(mooreAut, outputFile);
        }
        else if (command == "moore-to-mealy")
        {
            var mooreAut = ReadMoore(inputFile);
            mooreAut = RemoveUnreachableStatesMoore(mooreAut);
            var mealyAut = ConvertMooreToMealy(mooreAut);
            PrintMealy(mealyAut, outputFile);
        }
        else
        {
            Console.WriteLine("Invalid command. Use 'mealy-to-moore' or 'moore-to-mealy'.");
        }

        Console.WriteLine("Done");
    }*/

    //for testing
    static void Main()
    {
        var command = "moore-to-mealy";
        var inputFile = "moore.csv";
        var outputFile = "mealy.csv";

        if (command == "mealy-to-moore")
        {
            var mealyAut = ReadMealy(inputFile);
            mealyAut = RemoveUnreachableStatesMealy(mealyAut);
            var mooreAut = ConvertMealyToMoore(mealyAut);
            PrintMoore(mooreAut, outputFile);
        }
        else if (command == "moore-to-mealy")
        {
            var mooreAut = ReadMoore(inputFile);
            mooreAut = RemoveUnreachableStatesMoore(mooreAut);
            var mealyAut = ConvertMooreToMealy(mooreAut);
            PrintMealy(mealyAut, outputFile);
        }
        else
        {
            Console.WriteLine("Invalid command. Use 'mealy-to-moore' or 'moore-to-mealy'.");
        }

        Console.WriteLine("Done");
    }
}
