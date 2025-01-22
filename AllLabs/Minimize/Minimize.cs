using System.Text;

class MooreAutomata
{
    public Dictionary<string, string> Outputs;
    public List<List<string>> Transitions;
    public HashSet<string> States;
    public HashSet<string> Inputs;
}

class MealyAutomata
{
    public List<List<(string NextState, string Output)>> Transitions;
    public HashSet<string> States;
    public HashSet<string> Inputs;
}

class AutomataConverter
{
    //for prod
    static void Main( string[] args )
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: program <mealy|moore> <input.csv> <output.csv>");
            return;
        }

        var command = args[0];
        var inputFile = args[1];
        var outputFile = args[2];

        if (command == "mealy")
        {
            var mealyAut = ReadMealy(inputFile);
            mealyAut = RemoveUnreachableStatesMealy(mealyAut);
            var minimizeMealy = MinimizeMealy(mealyAut);
            PrintMealy(minimizeMealy, outputFile);
        }
        else if (command == "moore")
        {
            var mooreAut = ReadMoore(inputFile);
            mooreAut = RemoveUnreachableStatesMoore(mooreAut);
            var minimizeMoore = MinimizeMoore(mooreAut);
            PrintMoore(minimizeMoore, outputFile);
        }
        else
        {
            Console.WriteLine("Invalid command. Use 'mealy' or 'moore'.");
        }

        Console.WriteLine("Done");
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

    static MooreAutomata MinimizeMoore( MooreAutomata moore )
    {
        // Шаг 1: Создание начального разбиения по выходам
        var outputGroups = new Dictionary<string, HashSet<string>>();
        foreach (var state in moore.States)
        {
            var output = moore.Outputs[state];
            if (!outputGroups.ContainsKey(output))
            {
                outputGroups[output] = new HashSet<string>();
            }
            outputGroups[output].Add(state);
        }

        var partitions = outputGroups.Values.ToList();

        // Функция для уточнения разбиения
        List<HashSet<string>> Refine( List<HashSet<string>> currentPartitions )
        {
            var newPartitions = new List<HashSet<string>>();
            var stateToPartitionMap = moore.States.ToDictionary(
                state => state,
                state => currentPartitions.FindIndex(partition => partition.Contains(state))
            );

            foreach (var group in currentPartitions)
            {
                var subGroups = new Dictionary<string, HashSet<string>>();

                foreach (var state in group)
                {
                    var key = new StringBuilder();

                    foreach (var input in moore.Inputs)
                    {
                        string nextState = GetNextState(moore, state, input);

                        if (nextState != null && moore.States.Contains(nextState))
                        {
                            var partitionIndex = stateToPartitionMap[nextState];
                            key.Append($"{input}{partitionIndex}");
                        }
                    }

                    var keyString = key.ToString();
                    if (!subGroups.ContainsKey(keyString))
                    {
                        subGroups[keyString] = new HashSet<string>();
                    }
                    subGroups[keyString].Add(state);
                }

                newPartitions.AddRange(subGroups.Values);
            }

            return newPartitions;
        }

        // Функция для получения следующего состояния
        string GetNextState( MooreAutomata moore, string state, string input )
        {
            var inputIndex = moore.Inputs.ToList().IndexOf(input);
            var stateIndex = moore.States.ToList().IndexOf(state);

            // Предполагается, что Transitions имеет правильные размеры
            return moore.Transitions[inputIndex][stateIndex];
        }

        // Итеративное уточнение разбиения
        bool partitionsChanged;
        do
        {
            var newPartitions = Refine(partitions);
            partitionsChanged = newPartitions.Count != partitions.Count;
            partitions = newPartitions;
        } while (partitionsChanged);

        // Построение минимизированного автомата
        var stateMap = new Dictionary<string, string>();
        var minimizedStates = new HashSet<string>();
        var minimizedOutputs = new Dictionary<string, string>();
        var minimizedTransitions = new List<List<string>>();

        for (int i = 0; i < partitions.Count; i++)
        {
            var group = partitions[i];
            var newState = $"S{i}";
            foreach (var state in group)
            {
                stateMap[state] = newState;
            }
            minimizedStates.Add(newState);
            var representative = group.First();
            minimizedOutputs[newState] = moore.Outputs[representative];
        }

        foreach (var input in moore.Inputs)
        {
            var transitionsForInput = new List<string>();

            foreach (var group in partitions)
            {
                var representative = group.First();
                string nextState = GetNextState(moore, representative, input);

                transitionsForInput.Add(nextState != null && stateMap.ContainsKey(nextState) ? stateMap[nextState] : "");
            }

            minimizedTransitions.Add(transitionsForInput);
        }

        return new MooreAutomata
        {
            States = minimizedStates,
            Outputs = minimizedOutputs,
            Inputs = moore.Inputs,
            Transitions = minimizedTransitions
        };
    }


    static MealyAutomata MinimizeMealy( MealyAutomata mealy )
    {
        // Шаг 1: Создание начального разбиения по выходам
        var partition = new Dictionary<string, HashSet<string>>();

        foreach (var state in mealy.States)
        {
            var outputs = string.Join(";", mealy.Inputs.Select(( input, index ) =>
            {
                var stateIndex = mealy.States.ToList().IndexOf(state);
                var transition = mealy.Transitions[index][stateIndex];
                return transition.Output ?? "-"; // Используем "-" для обозначения пустого выхода
            }));

            if (!partition.ContainsKey(outputs))
            {
                partition[outputs] = new HashSet<string>();
            }
            partition[outputs].Add(state);
        }

        // Шаг 2: Итеративное разбиение на классы эквивалентности
        bool changed;
        do
        {
            changed = false;
            var newPartition = new Dictionary<string, HashSet<string>>();

            foreach (var group in partition.Values)
            {
                var subGroups = new Dictionary<string, HashSet<string>>();

                foreach (var state in group)
                {
                    var signature = string.Join(";", mealy.Inputs.Select(( input, index ) =>
                    {
                        var stateIndex = mealy.States.ToList().IndexOf(state);
                        var transition = mealy.Transitions[index][stateIndex];
                        var nextState = transition.NextState;
                        var output = transition.Output ?? "-"; // Используем "-" для обозначения пустого выхода

                        var nextStateClass = partition.First(part => part.Value.Contains(nextState)).Key;
                        return $"{nextStateClass}/{output}";
                    }));

                    if (!subGroups.ContainsKey(signature))
                    {
                        subGroups[signature] = new HashSet<string>();
                    }
                    subGroups[signature].Add(state);
                }

                foreach (var subGroup in subGroups.Values)
                {
                    var key = string.Join(",", subGroup);
                    newPartition[key] = subGroup;
                    if (subGroup.Count != group.Count)
                    {
                        changed = true;
                    }
                }
            }

            partition = newPartition;
        } while (changed);

        // Шаг 3: Построение минимизированного автомата с новыми именами состояний
        var minimizedTransitions = new List<List<(string NextState, string Output)>>();
        var minimizedStates = new HashSet<string>();
        var stateMapping = new Dictionary<string, string>();
        var index = 1;

        foreach (var group in partition.Values)
        {
            var newStateName = $"X{index++}";
            minimizedStates.Add(newStateName);

            foreach (var state in group)
            {
                stateMapping[state] = newStateName;
            }
        }

        foreach (var input in mealy.Inputs)
        {
            var transitionsForInput = new List<(string NextState, string Output)>();

            foreach (var group in partition.Values)
            {
                var representative = group.First();
                var stateIndex = mealy.States.ToList().IndexOf(representative);
                var transition = mealy.Transitions[mealy.Inputs.ToList().IndexOf(input)][stateIndex];
                var nextState = stateMapping[transition.NextState];
                transitionsForInput.Add((nextState, transition.Output ?? "-"));
            }

            minimizedTransitions.Add(transitionsForInput);
        }

        return new MealyAutomata
        {
            States = minimizedStates,
            Inputs = mealy.Inputs,
            Transitions = minimizedTransitions
        };
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
}
