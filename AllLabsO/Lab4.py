import re
from collections import defaultdict
import csv
import sys

def read_moore_to_list(positions, file, alphabet):
    alphabet_set = set()  
    lines = file.readlines()

    temp_row = lines[0].strip().split(';')
    temp_row_st = lines[1].strip().split(';')

    for i in range(1, len(temp_row)):
        state = {
            "state": temp_row_st[i],
            "output": temp_row[i],
            "transitions": []  
        }
        positions.append(state)

    for line in lines[2:]:
        temp_row = line.strip().split(';')
        input_sym = temp_row[0]

        if input_sym != "Оµ":
            alphabet_set.add(input_sym)
        #print(input_sym, end=" ")

        for i in range(1, len(temp_row)):
            transition = {
                "inputSym": input_sym,
                "nextPos": temp_row[i].split(',') if temp_row[i] else [] 
            }
            positions[i - 1]["transitions"].append(transition)

    # Преобразование множества в список
    alphabet = list(alphabet_set)

    return positions, alphabet

def export_moore_automaton_to_csv(moore_automaton, filename):
    all_input_symbols = set()
    for state in moore_automaton:
        for transition in state['transitions']:
            #if transition['nextPos']: 
            all_input_symbols.add(transition['inputSym'])

    all_input_symbols = sorted(all_input_symbols)

    with open(filename, mode='w', newline='', encoding='utf-8') as file:
        writer = csv.writer(file, delimiter=';')

        output_row = ['']
        for state in moore_automaton:
            output_row.append(state['output'])
        writer.writerow(output_row)

        headers = [''] + [state['state'] for state in moore_automaton]
        writer.writerow(headers)


        for symbol in all_input_symbols:
            row = [symbol]

            for state in moore_automaton:
                next_states = []
                for transition in state['transitions']:
                    if transition['inputSym'] == symbol:
                        next_states.append(transition['nextPos'])

                if not next_states:
                    row.append('')
                else:
                    output_str = ','.join(next_states)  #  все состояния
                    row.append(output_str)

            writer.writerow(row)

    print(f"Moore automaton exported to {filename}")


def epsilon_closure(states, moore_automaton, reachable_states=None):
    if reachable_states is None:
        reachable_states = set()

    next_states = set()
    for state in states:
        if state not in reachable_states:
            reachable_states.add(state)  # Mark state as visited
            for transition in moore_automaton[state]['transitions']:
                if transition['inputSym'] == "ε":  
                    next_states.update(transition['nextPos'])

    if not next_states:
        return reachable_states
    else:
        return epsilon_closure(next_states, moore_automaton, reachable_states)


def convert_nfa_to_dfa(moore_automaton, alphabet):
    dfa_automaton = [] 
    new_name_state_map = {} # {x0: S0; x1, x2: S2}
    eps_state_map = {} # {x1: x1,x2}
    queue = []

    alphabet = [symbol for symbol in alphabet if symbol != "ε"]
    for state in moore_automaton:
        eps_state_map[state] = frozenset(epsilon_closure({state}, moore_automaton))

    start_state = [list(moore_automaton.keys())[0]]
    frozStartState = frozenset(start_state)
    new_name_state_map[frozStartState] = "S0"
    queue.append(start_state)
    counter = 0

    while queue:
        print(queue)
        currState = queue.pop(0)
        currStateFrozen = frozenset(currState)
        dfa_stateNew = {
            "state": new_name_state_map[currStateFrozen],
            "output": "",
            "transitions": []
        }

        curr_state = frozenset().union(*(eps_state_map[state] for state in currState))
        #print("State: ", currState)

        for state in curr_state:
            if moore_automaton[state]['output'] == "F":
                dfa_stateNew['output'] = "F"

            for symbol in alphabet:
                for transition in moore_automaton[state]['transitions']:
                    if transition['inputSym'] == symbol:
                        if transition['nextPos']:
                            next_positions = list(transition['nextPos'])
                            existing_transition = next(
                                (t for t in dfa_stateNew['transitions'] if t['inputSym'] == symbol), None
                            )

                            if existing_transition:
                                existing_transition['nextPos'].extend(
                                    pos for pos in next_positions if pos not in existing_transition['nextPos']
                                )
                            else:
                                dfa_stateNew['transitions'].append({
                                    'inputSym': symbol,
                                    'nextPos': next_positions
                                })

        for transition in dfa_stateNew['transitions']:
            frozSet = frozenset(transition['nextPos'])

            if frozSet:
                if frozSet not in new_name_state_map.keys():
                    counter += 1
                    new_name_state_map[frozSet] = f"S{counter}"
                    queue.append(transition['nextPos'])
                    #print(frozSet)
                transition['nextPos'] = new_name_state_map[frozSet]

        dfa_automaton.append(dfa_stateNew)
    return dfa_automaton


def main():
    grammar_file = "Lab5Output.csv"
    output_file = "Lab4Output.csv"

    # grammar_file = "source_nfa.csv"
    # output_file = "out.csv"
    positions = []  # Список для состояний
    alphabet = []   # Алфавит входных символов


    with open(grammar_file, 'r', encoding='utf-8') as file:
        positions, alphabet = read_moore_to_list(positions, file, alphabet)

    moore_automaton = {}

    for state in positions:
        moore_automaton[state["state"]] = {
            "output": state["output"],
            "transitions": state["transitions"]
        }

    dfa_automaton = convert_nfa_to_dfa(moore_automaton, alphabet)

    export_moore_automaton_to_csv(dfa_automaton, output_file)
if __name__ == "__main__":
    main()