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

    static MooreAutomata RemoveUnreachableStatesMoore( MooreAutomata moore )
    {
        Console.WriteLine(moore.States.Count);

        var initialState = moore.States.First();

        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(initialState);
        reachableStates.Add(initialState);

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            var currentIndex = moore.States.ToList().IndexOf(currentState);

            if (currentIndex == -1)
            {
                continue;
            }

            for (int index = 0; index < moore.Inputs.Count; index++)
            {
                var nextState = moore.Transitions[index][currentIndex];

                if (!reachableStates.Contains(nextState))
                {
                    reachableStates.Add(nextState);
                    queue.Enqueue(nextState);
                }
            }
        }

        moore.States.IntersectWith(reachableStates);

        moore.Outputs = moore.Outputs
            .Where(o => reachableStates.Contains(o.Key))
            .ToDictionary(o => o.Key, o => o.Value);

        var filteredTransitions = new List<List<string>>();

        for (int index = 0; index < moore.Inputs.Count; index++)
        {
            var filteredStateTransitions = moore.Transitions[index]
                .Where(t => reachableStates.Contains(t))
                .ToList();

            filteredTransitions.Add(filteredStateTransitions);
        }

        moore.Transitions = filteredTransitions;

  /*      Console.WriteLine("\nStates after removal:");
        Console.WriteLine(moore.States.Count);*/

        return moore;
    }

    static MealyAutomata RemoveUnreachableStatesMealy( MealyAutomata mealy )
    {
        var initialState = mealy.States.First();

        var reachableStates = new HashSet<string>();
        var queue = new Queue<string>();

        queue.Enqueue(initialState);
        reachableStates.Add(initialState);

        while (queue.Count > 0)
        {
            var currentState = queue.Dequeue();
            var currentIndex = mealy.States.ToList().IndexOf(currentState);

            if (currentIndex == -1)
            {
                continue;
            }

            for (int index = 0; index < mealy.Inputs.Count; index++)
            {
                var nextState = mealy.Transitions[index][currentIndex].NextState;

                if (!reachableStates.Contains(nextState))
                {
                    reachableStates.Add(nextState);
                    queue.Enqueue(nextState);
                }
            }
        }

        mealy.States.IntersectWith(reachableStates);

        var filteredTransitions = new List<List<(string NextState, string Output)>>();

        for (int index = 0; index < mealy.Inputs.Count; index++)
        {
            var filteredStateTransitions = mealy.Transitions[index]
                .Where(t => reachableStates.Contains(t.NextState))
                .ToList();

            filteredTransitions.Add(filteredStateTransitions);
        }

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
            Inputs = new HashSet<string>(mealy.Inputs),
            Outputs = new Dictionary<string, string>(),
            States = new HashSet<string>(),
            Transitions = new List<List<string>>()
        };

        var statesCard = new Dictionary<string, SortedSet<string>>();
        for (int inputIndex = 0; inputIndex < mealy.Transitions.Count; inputIndex++)
        {
            for (int stateIndex = 0; stateIndex < mealy.States.Count; stateIndex++)
            {
                var (nextState, output) = mealy.Transitions[inputIndex][stateIndex];
                if (!statesCard.ContainsKey(nextState))
                {
                    statesCard[nextState] = new SortedSet<string>();
                }
                statesCard[nextState].Add(output);
            }
        }

        var newStatesCard = new Dictionary<(string, string), string>();
        int mooreStateNum = 0;
        foreach (var state in statesCard)
        {
            foreach (var output in state.Value)
            {
                var newState = MOORE_STATE + mooreStateNum.ToString();
                newStatesCard[(state.Key, output)] = newState;
                moore.Outputs[newState] = output;
                moore.States.Add(newState);
                mooreStateNum++;
            }
        }

        for (int inputIndex = 0; inputIndex < mealy.Transitions.Count; inputIndex++)
        {
            int transitionIndex = 0;
            var mooreTransitions = new List<string>();
            foreach (var state in statesCard)
            {
                string nextMealyState = mealy.Transitions[inputIndex][transitionIndex].NextState;
                foreach (var outputs in state.Value)
                {
                    mooreTransitions.Add(newStatesCard[mealy.Transitions[inputIndex][transitionIndex]]);
                }
                transitionIndex++;
            }
            moore.Transitions.Add(mooreTransitions);
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

    static void Main( string[] args )
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
    }
}
