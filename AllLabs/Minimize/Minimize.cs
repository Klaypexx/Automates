using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class AutomataConverter
{
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
                aut.Transitions.Add(parts.Skip(1).Select(x => string.IsNullOrEmpty(x) ? "-" : x).ToList());
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
                        if (string.IsNullOrEmpty(transition))
                            return ("-", "-");

                        var parts = transition.Split('/');
                        return (NextState: parts[0], Output: parts.Length > 1 ? parts[1] : "-");
                    })
                    .ToList();

                mealyAutomata.Transitions.Add(transitions);
            }
        }

        return mealyAutomata;
    }

    static MooreAutomata RemoveUnreachableStatesMoore( MooreAutomata moore )
    {
        var initialState = moore.States.First();
        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(initialState);
        reachableStates.Add(initialState);

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
                if (nextState != "-" && reachableStates.Add(nextState))
                {
                    queue.Enqueue(nextState);
                }
            }
        }

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

        moore.Transitions = filteredTransitions;

        return moore;
    }

    static MealyAutomata RemoveUnreachableStatesMealy( MealyAutomata mealy )
    {
        var initialState = mealy.States.First();
        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(initialState);
        reachableStates.Add(initialState);

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
                if (nextState != "-" && reachableStates.Add(nextState))
                {
                    queue.Enqueue(nextState);
                }
            }
        }

        var filteredTransitions = new List<List<(string NextState, string Output)>>();
        foreach (var transition in mealy.Transitions)
        {
            var filteredStateTransitions = stateList
                .Where(reachableStates.Contains)
                .Select(state => transition[stateIndexMap[state]])
                .ToList();

            filteredTransitions.Add(filteredStateTransitions);
        }

        mealy.States.IntersectWith(reachableStates);

        mealy.Transitions = filteredTransitions;

        return mealy;
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

    static MooreAutomata MinimizeMoore( MooreAutomata moore )
    {
        // Шаг 1: Создание начального разбиения по выходам
        var partition = new Dictionary<string, HashSet<string>>();

        foreach (var state in moore.States)
        {
            var output = moore.Outputs[state] ?? "-"; // Используем "-" для обозначения пустого выхода
            if (!partition.ContainsKey(output))
            {
                partition[output] = new HashSet<string>();
            }
            partition[output].Add(state);
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
                    var signature = string.Join(";", moore.Inputs.Select(input =>
                    {
                        var inputIndex = moore.Inputs.ToList().IndexOf(input);
                        var stateIndex = moore.States.ToList().IndexOf(state);
                        var nextState = moore.Transitions[inputIndex][stateIndex];

                        // Если состояние "-", используем его как отдельный класс
                        if (nextState == "-")
                        {
                            return "-";
                        }

                        // Ищем класс эквивалентности для следующего состояния
                        var nextStateClass = partition.FirstOrDefault(part => part.Value.Contains(nextState)).Key;
                        return nextStateClass ?? "-"; // Используем "-" для обозначения отсутствующего класса
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
        var minimizedStates = new HashSet<string>();
        var minimizedOutputs = new Dictionary<string, string>();
        var minimizedTransitions = new List<List<string>>();
        var stateMapping = new Dictionary<string, string>();
        var index = 1;

        foreach (var group in partition.Values)
        {
            var newStateName = $"X{index++}";
            minimizedStates.Add(newStateName);

            var representative = group.First();
            minimizedOutputs[newStateName] = moore.Outputs[representative] ?? "-";

            foreach (var state in group)
            {
                stateMapping[state] = newStateName;
            }
        }

        foreach (var input in moore.Inputs)
        {
            var transitionsForInput = new List<string>();

            foreach (var group in partition.Values)
            {
                var representative = group.First();
                var stateIndex = moore.States.ToList().IndexOf(representative);
                var nextState = moore.Transitions[moore.Inputs.ToList().IndexOf(input)][stateIndex];
                transitionsForInput.Add(stateMapping.ContainsKey(nextState) ? stateMapping[nextState] : "");
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
        var partition = new Dictionary<string, HashSet<string>>();

        foreach (var state in mealy.States)
        {
            var outputs = string.Join(";", mealy.Inputs.Select(( input, index ) =>
            {
                var stateIndex = mealy.States.ToList().IndexOf(state);
                var transition = mealy.Transitions[index][stateIndex];
                return transition.Output ?? "-";
            }));

            if (!partition.ContainsKey(outputs))
            {
                partition[outputs] = new HashSet<string>();
            }
            partition[outputs].Add(state);
        }

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
                        var output = transition.Output ?? "-";

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

    static void Main()
    {
        var command = "moore";
        var inputFile = "1_moore.csv";
        var outputFile = "moore.csv";

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
            Console.WriteLine("Invalid command. Use 'mealy-minimize' or 'moore-minimize'.");
        }

        Console.WriteLine("Done");
    }
}
