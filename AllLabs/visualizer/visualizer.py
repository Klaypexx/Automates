import argparse
import csv
import sys
from pathlib import Path
from multiprocessing import Process, Queue
import graphviz


class MachineRendererWrapper:
    def __init__(self, comment):
        self.document = graphviz.Digraph(comment=comment)

    def add_state(self, name: str, label: str, start=False, is_finish=False):
        if start and is_finish:
            self.document.node(name, label, style='bold', penwidth='3',
                               peripheries='2')  # Double circle with thick border
        elif start:
            self.document.node(name, label, style='bold', penwidth='3')  # Thick border for the start node
        elif is_finish:
            self.document.node(name, label, peripheries='2')  # Double circle for finish states
        else:
            self.document.node(name, label)

    def add_transition(self, src: str, dest: str, label: str):
        self.document.edge(src, dest, label)

    def view(self):
        self.document.view()


class GenericTransition:
    def __init__(self, from_state, to_state, activator):
        self.from_state = from_state
        self.to_state = to_state
        self.signal = activator

    def __hash__(self):
        return hash((self.from_state, self.to_state, self.signal))

    def __eq__(self, other):
        return (self.from_state, self.to_state, self.signal) == (other.from_state, other.to_state, other.signal)

    def __repr__(self):
        return f"T({self.from_state} -> {self.to_state} on {self.signal})"


class MealyMachineStore:
    def __init__(self):
        self.in_signals = []
        self.states = []
        self.transitions = {}
        self.start_state = None  # Start state
        self.output_signals = set()

    def generate_renderable(self):
        render = MachineRendererWrapper("Mealy Machine")
        is_detector_mode = self.output_signals <= {'F', ''}

        finish_states = set()
        if is_detector_mode:
            for tr, out in self.transitions.items():
                if out == 'F':
                    finish_states.add(tr.to_state)

        for state in self.states:
            if is_detector_mode:
                render.add_state(state, state, start=(state == self.start_state), is_finish=(state in finish_states))
            else:
                render.add_state(state, state, start=(state == self.start_state))

        for tr, out in self.transitions.items():
            if is_detector_mode:
                label = tr.signal
            else:
                label = tr.signal + '/' + out
            render.add_transition(tr.from_state, tr.to_state, label)
        return render


class MooreMachineStore:
    def __init__(self):
        self.in_signals = []
        self.states = {}
        self.transitions = []
        self.start_state = None  # Start state
        self.output_signals = set()

    def generate_renderable(self):
        render = MachineRendererWrapper("Moore Machine")
        is_detector_mode = set(self.states.values()) <= {'F', ''}

        finish_states = {state for state, out in self.states.items() if out == 'F'} if is_detector_mode else set()

        for state, out_signal in self.states.items():
            if is_detector_mode:
                render.add_state(state, state, start=(state == self.start_state), is_finish=(state in finish_states))
            else:
                render.add_state(state, state + '/' + out_signal, start=(state == self.start_state))

        for tr in self.transitions:
            render.add_transition(tr.from_state, tr.to_state, tr.signal)
        return render


def read_mealy_csv(file) -> MealyMachineStore:
    with open(file) as f:
        reader = csv.reader(f, delimiter=';')
        machine = MealyMachineStore()
        machine.states = next(reader)[1:]  # ['a1', 'a2', 'a3']
        machine.start_state = machine.states[0]  # First state as the start state
        for line in reader:
            signal = line[0]  # Input signal
            transitions = line[1:]
            machine.in_signals.append(signal)
            for transition, from_state in zip(transitions, machine.states):
                to_state, out_signal = transition.split('/')
                machine.transitions[GenericTransition(from_state, to_state, signal)] = out_signal
                machine.output_signals.add(out_signal)
        return machine


def read_moore_csv(file) -> MooreMachineStore:
    with open(file) as f:
        reader = csv.reader(f, delimiter=';')
        machine = MooreMachineStore()
        out_signals = next(reader)[1:]
        states = next(reader)[1:]
        machine.states = dict(zip(states, out_signals))
        machine.output_signals = set(out_signals)
        machine.start_state = states[0]  # First state as the start state
        for line in reader:
            signal = line[0]
            to_states_line = line[1:]
            machine.in_signals.append(signal)
            for from_state, to_states_str in zip(machine.states, to_states_line):
                if to_states_str != '':
                    to_states = to_states_str.split(',')
                    for to_state in to_states:
                        machine.transitions.append(GenericTransition(from_state, to_state, signal))
        return machine


def render_csv(file, view_enabled=True, time=None):
    def render_with_timeout(document, file, renders_dir, result_queue):
        try:
            pdf_path = document.render(filename=file + '.pdf', directory=renders_dir, format='pdf')
            result_queue.put(pdf_path)
        except Exception as e:
            result_queue.put(e)

    renders_dir = Path("renders")
    renders_dir.mkdir(parents=True, exist_ok=True)

    with open(file, 'r') as f:
        lines = f.readlines()

    if len(lines) > 1 and lines[1].startswith(';'):
        machine = read_moore_csv(file)
    else:
        machine = read_mealy_csv(file)

    document = machine.generate_renderable().document

    if view_enabled:
        document.view(filename=Path(file).name, directory=renders_dir)
    else:
        if time is not None:
            result_queue = Queue()
            process = Process(target=render_with_timeout, args=(document, file, renders_dir, result_queue))
            process.start()
            process.join(timeout=time)

            if process.is_alive():
                process.terminate()
                process.join()
                print("Rendering exceeded the time limit.")
                return None
            else:
                result = result_queue.get()
                if isinstance(result, Exception):
                    print(f"An error occurred: {result}")
                    return None
                else:
                    return result
        else:
            pdf_path = document.render(filename=file + '.png', directory=renders_dir, format='png')
            return pdf_path


def main():
    parser = argparse.ArgumentParser(description='Render CSV files.')
    parser.add_argument('files', metavar='F', type=str, nargs='+', help='CSV files to render')

    args = parser.parse_args()

    for file in args.files:
        try:
            render_csv(file)
        except Exception as e:
            print(f"An error occurred while processing {file}: {e}", file=sys.stderr)


if __name__ == "__main__":
    main()
