public class Program
{
    public static void Main( string[] args )
    {
        string outputFileName = args[0];
        string regex = args[1];

        var parser = new RegexParser();
        var regexTree = parser.ParseRegex(regex);
        /*PrintTree(regexTree);*/

        var nfaConstructor = new NFAConstructor();
        var nfa = nfaConstructor.BuildNFA(regexTree);

        NFAUtils.WriteNFA(nfa, outputFileName);
    }

    /*public static void PrintTree( RegexNode node, int level = 0 )
    {
        if (node != null)
        {
            PrintTree(node.Left, level + 1);
            Console.WriteLine(new string(' ', 4 * level) + "-> " + node.Value);
            PrintTree(node.Right, level + 1);
        }
    }*/
}

public class RegexNode
{
    public string Value { get; }
    public RegexNode Left { get; }
    public RegexNode Right { get; }

    public RegexNode( string value, RegexNode left = null, RegexNode right = null )
    {
        Value = value;
        Left = left;
        Right = right;
    }

    public override string ToString()
    {
        return $"RegexNode({Value})";
    }
}

public class RegexParser
{
    private Queue<char> tokens;
    private static readonly HashSet<char> NonLiteralCharacters = new HashSet<char> { '+', '*', '(', ')', '|' };

    public RegexNode ParseRegex( string expression )
    {
        tokens = new Queue<char>(expression);
        return ParseExpression();
    }

    private RegexNode ParseExpression()
    {
        var node = ParseTerm();
        while (tokens.Any() && tokens.Peek() == '|')
        {
            tokens.Dequeue();
            var right = ParseTerm();
            node = new RegexNode("or", node, right);
        }
        return node;
    }

    private RegexNode ParseTerm()
    {
        var node = ParseFactor();
        while (tokens.Any() && (IsLiteral(tokens.Peek()) || tokens.Peek() == '('))
        {
            var right = ParseFactor();
            node = new RegexNode("concat", node, right);
        }
        return node;
    }

    private RegexNode ParseFactor()
    {
        var node = ParsePrimary();
        while (tokens.Any() && (tokens.Peek() == '*' || tokens.Peek() == '+'))
        {
            var op = tokens.Dequeue() == '*' ? "multiply" : "add";
            node = new RegexNode(op, node);
        }
        return node;
    }

    private RegexNode ParsePrimary()
    {
        if (!tokens.Any())
            throw new InvalidOperationException("Unexpected end of expression");

        var token = tokens.Dequeue();
        if (token == '\\')
        {
            if (!tokens.Any())
                throw new InvalidOperationException("Unexpected end of expression after escape character");

            var escaped = tokens.Dequeue();
            if (IsLiteral(escaped))
            {
                tokens.Enqueue(escaped);
            }
            else
            {
                return new RegexNode(escaped.ToString());
            }
        }

        if (IsLiteral(token))
        {
            return new RegexNode(token.ToString());
        }
        else if (token == '(')
        {
            var node = ParseExpression();
            if (!tokens.Any() || tokens.Dequeue() != ')')
                throw new InvalidOperationException("Mismatched parentheses");
            return node;
        }

        throw new InvalidOperationException($"Unexpected token: {token}");
    }

    private bool IsLiteral( char value )
    {
        return !NonLiteralCharacters.Contains(value);
    }
}

public class State
{
    public Dictionary<char, List<State>> Transitions { get; } = new Dictionary<char, List<State>>();
    public List<State> EpsilonTransitions { get; } = new List<State>();

    public void AddTransition( char symbol, State state )
    {
        if (!Transitions.ContainsKey(symbol))
        {
            Transitions[symbol] = new List<State>();
        }
        Transitions[symbol].Add(state);
    }

    public void AddEpsilonTransition( State state )
    {
        EpsilonTransitions.Add(state);
    }
}

public class NFA
{
    public State Start { get; }
    public State Accept { get; }

    public NFA( State start, State accept )
    {
        Start = start;
        Accept = accept;
    }
}

public class NFAConstructor
{
    public NFA BuildNFA( RegexNode node )
    {
        switch (node.Value)
        {
            case "concat":
                return Concatenate(BuildNFA(node.Left), BuildNFA(node.Right));
            case "or":
                return Alternate(BuildNFA(node.Left), BuildNFA(node.Right));
            case "multiply":
                return Star(BuildNFA(node.Left));
            case "add":
                return Plus(BuildNFA(node.Left));
            default:
                return Literal(node.Value[0]);
        }
    }

    private NFA Literal( char symbol )
    {
        var start = new State();
        var accept = new State();
        start.AddTransition(symbol, accept);
        return new NFA(start, accept);
    }

    private NFA Concatenate( NFA first, NFA second )
    {
        first.Accept.AddEpsilonTransition(second.Start);
        return new NFA(first.Start, second.Accept);
    }

    private NFA Alternate( NFA first, NFA second )
    {
        var start = new State();
        var accept = new State();
        start.AddEpsilonTransition(first.Start);
        start.AddEpsilonTransition(second.Start);
        first.Accept.AddEpsilonTransition(accept);
        second.Accept.AddEpsilonTransition(accept);
        return new NFA(start, accept);
    }

    private NFA Star( NFA nfa )
    {
        var start = new State();
        var accept = new State();
        start.AddEpsilonTransition(nfa.Start);
        start.AddEpsilonTransition(accept);
        nfa.Accept.AddEpsilonTransition(nfa.Start);
        nfa.Accept.AddEpsilonTransition(accept);
        return new NFA(start, accept);
    }

    private NFA Plus( NFA nfa )
    {
        var start = new State();
        var accept = new State();
        start.AddEpsilonTransition(nfa.Start);
        nfa.Accept.AddEpsilonTransition(nfa.Start);
        nfa.Accept.AddEpsilonTransition(accept);
        return new NFA(start, accept);
    }
}

public static class NFAUtils
{
    public static Dictionary<State, string> AssignIndices( State startState )
    {
        var stateIndex = new Dictionary<State, string>();
        var index = 0;
        var stack = new Stack<State>();
        stack.Push(startState);

        while (stack.Count > 0)
        {
            var state = stack.Pop();
            if (!stateIndex.ContainsKey(state))
            {
                stateIndex[state] = $"S{index++}";
                foreach (var states in state.Transitions.Values)
                {
                    foreach (var s in states)
                    {
                        if (!stateIndex.ContainsKey(s))
                        {
                            stack.Push(s);
                        }
                    }
                }
                foreach (var s in state.EpsilonTransitions)
                {
                    if (!stateIndex.ContainsKey(s))
                    {
                        stack.Push(s);
                    }
                }
            }
        }

        return stateIndex;
    }

    public static void WriteNFA( NFA nfa, string outputFileName )
    {
        var stateIndex = AssignIndices(nfa.Start);
        var finalState = stateIndex[nfa.Accept];

        var transitions = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        foreach (var state in stateIndex.Keys)
        {
            var name = stateIndex[state];
            transitions[name] = new Dictionary<string, HashSet<string>>();
            foreach (var kvp in state.Transitions)
            {
                var symbol = kvp.Key.ToString();
                if (!transitions[name].ContainsKey(symbol))
                {
                    transitions[name][symbol] = new HashSet<string>();
                }
                foreach (var s in kvp.Value)
                {
                    transitions[name][symbol].Add(stateIndex[s]);
                }
            }
            foreach (var s in state.EpsilonTransitions)
            {
                if (!transitions[name].ContainsKey("ε"))
                {
                    transitions[name]["ε"] = new HashSet<string>();
                }
                transitions[name]["ε"].Add(stateIndex[s]);
            }
        }

        var symbols = new HashSet<string>();
        foreach (var trans in transitions.Values)
        {
            foreach (var symbol in trans.Keys)
            {
                symbols.Add(symbol);
            }
        }

        using (var writer = new StreamWriter(outputFileName))
        {
            writer.WriteLine(";" + string.Join(";", stateIndex.Values.Select(s => s == finalState ? "F" : "")));
            writer.WriteLine(";" + string.Join(";", stateIndex.Values));

            foreach (var symbol in symbols)
            {
                var row = new List<string> { symbol };
                foreach (var state in stateIndex.Values)
                {
                    var targets = transitions[state].ContainsKey(symbol) ? transitions[state][symbol] : new HashSet<string>();
                    row.Add(string.Join(",", targets));
                }
                writer.WriteLine(string.Join(";", row));
            }
        }
    }
}
