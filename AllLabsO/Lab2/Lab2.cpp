#include <fstream>
#include <sstream>
#include <vector>
#include <string>
#include <unordered_map>
#include <iostream>
#include <map>
#include <set>
#include <unordered_set>
#include <algorithm>
#include <queue>

std::vector<std::string> split(const std::string& s, char delimiter) {
    std::vector<std::string> tokens;
    std::string token;
    std::istringstream tokenStream(s);
    while (std::getline(tokenStream, token, delimiter)) {
        if (token.empty())
        {
            token = "-";
        }
        tokens.push_back(token);
    }
    return tokens;
}



struct Trans
{
    std::string inputSym; // z1,z2
    std::string nextPos; // q0, q1
    std::string outSym; // y1, y2
};

struct TransMoore
{
    std::string inputSym; // z1,z2
    std::string nextPos; // q0, q1
};

struct MealyState
{
    std::string curr; // q0
    std::vector<Trans> transitions; // q0/y1
};

struct MooreState
{
    std::string state;
    std::string newState;
    std::string output;
    std::vector<TransMoore> transitions; // input -> next state
};

std::vector<MealyState> findReachableStates(const std::vector<MealyState>& mealyStates)
{
    if (mealyStates.empty()) return {};

    std::unordered_set<std::string> visited;
    std::queue<std::string> toVisit;
    std::vector<MealyState> reachableStates;

    visited.insert(mealyStates[0].curr);
    toVisit.push(mealyStates[0].curr);
    reachableStates.push_back(mealyStates[0]);

    while (!toVisit.empty())
    {
        std::string currentState = toVisit.front();
        toVisit.pop();

        for (const auto& state : mealyStates)
        {
            if (state.curr == currentState)
            {
                for (const auto& transition : state.transitions)
                {
                    if (visited.find(transition.nextPos) == visited.end())
                    {
                        visited.insert(transition.nextPos);
                        toVisit.push(transition.nextPos);

                        for (const auto& nextState : mealyStates)
                        {
                            if (nextState.curr == transition.nextPos)
                            {
                                reachableStates.push_back(nextState);
                                //std::cout << nextState.curr << "\n";
                                break;
                            }
                        }
                    }
                }
                break;
            }
        }
    }

    return reachableStates;
}


std::vector<MooreState> findMooreReachableStates(const std::vector<MooreState>& mooreStates)
{
    if (mooreStates.empty()) return {};

    std::unordered_set<std::string> visited;
    std::queue<std::string> toVisit;
    std::vector<MooreState> reachableStates;

    visited.insert(mooreStates[0].state);
    toVisit.push(mooreStates[0].state);
    reachableStates.push_back(mooreStates[0]);

    while (!toVisit.empty())
    {
        std::string currentState = toVisit.front();
        toVisit.pop();

        for (const auto& state : mooreStates)
        {
            if (state.state == currentState)
            {
                for (const auto& transition : state.transitions)
                {
                    if (visited.find(transition.nextPos) == visited.end())
                    {
                        visited.insert(transition.nextPos);
                        toVisit.push(transition.nextPos);

                        for (const auto& nextState : mooreStates)
                        {
                            if (nextState.state == transition.nextPos)
                            {
                                reachableStates.push_back(nextState);
                                break;
                            }
                        }
                    }
                }
                break;
            }
        }
    }

    return reachableStates;
}

std::vector<MealyState> mealyMin(const std::vector<MealyState>& mealyAutomaton)
{
    std::map<std::string, std::string> newStateMap; //q0 -> A0;
    std::set<std::vector<std::string>> vecOutSet;
    std::map<std::vector<std::string>, std::string> vecOutNewStMap; // vecOut -> A0; mapOutGr
    std::vector<MealyState> newMealyAutomaton;
    std::map <std::string, std::vector<std::string>> StateInGroup; // A1,{q1,q2,q3}

    char ch = 'A';
    int stateCounter = 0;

    // Первый проход: группировка состояний по выходным переходам
    for (const auto& state : mealyAutomaton)
    {
        std::vector<std::string> outVector;
        for (const auto& transition : state.transitions)
        {
            outVector.push_back(transition.outSym);
        }

        if (vecOutSet.insert(outVector).second)
        {
            std::string newMinStateName = ch + std::to_string(stateCounter);
            vecOutNewStMap[outVector] = newMinStateName;  // mapOutGr
            newStateMap[state.curr] = newMinStateName;
            stateCounter++;
        }
        else
        {
            newStateMap[state.curr] = vecOutNewStMap[outVector];
        }

    }

    //for (const auto& pair : newStateMap) 
    //{
    //    const std::string& state = pair.first;  // q0
    //    const std::string& group = pair.second; // A0

    //    StateInGroup[group].push_back(state); // {A1, { q1,q2,q3 }}
    //}

    std::map <std::string, std::vector<MealyState>> NewMealyInGroup; // A1, {q1, {z1, A0, y1}, {z1, A0, y1}}

    // заполнение NewMealyInGroup
    for (const auto& state : mealyAutomaton)
    {
        MealyState tempState;
        std::vector<Trans> tempVec;
        for (const auto& transition : state.transitions)
        {
            Trans newTrans;
            newTrans.inputSym = transition.inputSym;
            newTrans.nextPos = newStateMap[transition.nextPos];

            newTrans.outSym = transition.outSym;
            tempVec.push_back(newTrans);
        }
        tempState.curr = state.curr; //q0
        tempState.transitions = tempVec;
        NewMealyInGroup[newStateMap[tempState.curr]].push_back(tempState);
        //std::cout << newStateMap[tempState.curr] << " " << tempState.curr << "\n";

    }

    auto prevSize = vecOutSet.size();
    size_t currSize = 0;
    bool equal = 0;
    while (prevSize != currSize)
    {
        ch++;
        int i = 0;
        std::map<std::string, std::string> tempNewStateMap; // q0 -> ch++
        std::map <std::string, std::vector<MealyState>> tempNewMealyInGroup; // A1, {q1, {z1, A0, y1}, {z1, A0, y1}}

        for (const auto& group : NewMealyInGroup)
        {
            std::map<std::vector<std::string>, std::string> vecNextNewStMap; // vecNext -> A0; mapOutGr
            std::set<std::vector<std::string>> vecTransSet;
            //std::cout << group.first << "\n";
            for (const auto& state : group.second)
            {
                //std::cout << group.first;
                std::vector<std::string> transVector;
                for (const auto& transition : state.transitions)
                {
                    transVector.push_back(transition.nextPos);

                }
                if (vecTransSet.insert(transVector).second)  // набор переходов A1, A2, A3
                {
                    std::string newMinStateName = ch + std::to_string(i);
                    vecNextNewStMap[transVector] = newMinStateName;  // mapOutGr
                    tempNewStateMap[state.curr] = newMinStateName;
                    i++;
                }
                else
                {
                    tempNewStateMap[state.curr] = vecNextNewStMap[transVector];
                }
                //std::cout << state.curr << " " << tempNewStateMap[state.curr] << "\n";
                currSize += vecTransSet.size();
            }
        }
        for (const auto& state : mealyAutomaton)
        {
            MealyState tempState;
            std::vector<Trans> tempVec;
            for (const auto& transition : state.transitions)
            {
                Trans newTrans;
                newTrans.inputSym = transition.inputSym;
                newTrans.nextPos = tempNewStateMap[transition.nextPos];
                newTrans.outSym = transition.outSym;
                tempVec.push_back(newTrans);
            }
            tempState.curr = state.curr; //q0
            tempState.transitions = tempVec;
            tempNewMealyInGroup[tempNewStateMap[tempState.curr]].push_back(tempState);
        }


        NewMealyInGroup = tempNewMealyInGroup;
        newStateMap = tempNewStateMap;

        if (prevSize == currSize)
        {
            break;
        }
        else
        {
            prevSize = currSize;
            currSize = 0;
        }


    }


    //for (const auto& state : mealyAutomaton)
    //{
    //    MealyState tempState;
    //    std::vector<Trans> tempVec;
    //    for (const auto& transition : state.transitions)
    //    {
    //        Trans newTrans;
    //        newTrans.inputSym = transition.inputSym;
    //        newTrans.nextPos = newStateMap[transition.nextPos];
    //        newTrans.outSym = transition.outSym;
    //        tempVec.push_back(newTrans);
    //    }
    //    tempState.curr = state.curr; //q0
    //    std::cout << state.curr;
    //    tempState.transitions = tempVec;
    //    NewMealyInGroup[newStateMap[tempState.curr]].push_back(tempState);
    //    //tempMealy.push_back(tempState);
    //}

    std::vector<MealyState> tempMealy;

    for (const auto& group : NewMealyInGroup)
    {
        std::vector<Trans> tempVec;
        MealyState tempState = group.second[0];

        for (const auto& trans : tempState.transitions)
        {
            Trans newTrans;
            newTrans.inputSym = trans.inputSym;
            newTrans.nextPos = trans.nextPos;
            //std::cout << trans.nextPos <<  " "  << newTrans.nextPos << "\n";
            newTrans.outSym = trans.outSym;
            tempVec.push_back(newTrans);
        }

        tempState.curr = group.first; //A, B, C
        tempState.transitions = tempVec;
        NewMealyInGroup[newStateMap[tempState.curr]].push_back(tempState);
        tempMealy.push_back(tempState);
    }

    return tempMealy;


}

std::vector<MooreState> mooreMin(const std::vector<MooreState>& mooreAutomaton)
{
    std::map<std::string, std::string> newStateMap; //q0 -> A0;
    std::set<std::string> outSet;
    std::map<std::string, std::string> outNewStMap; // y1 -> A0; mapOutGr
    std::map <std::string, std::vector<std::string>> StateInGroup; // A1,{q1,q2,q3}

    char ch = 'A';
    int stateCounter = 0;

    //группировка состояний по выходным переходам
    for (const auto& state : mooreAutomaton)
    {
        std::vector<std::string> outVector;
        //for (const auto& transition : state.transitions)
        //{
        //}

        //outVector.push_back(state.output);

        if (outSet.insert(state.output).second)
        {
            std::string newMinStateName = ch + std::to_string(stateCounter);
            outNewStMap[state.output] = newMinStateName;  // mapOutGr
            newStateMap[state.state] = newMinStateName;
            //std::cout << state.state << " " << newMinStateName << "\n";
            stateCounter++;
        }
        else
        {

            newStateMap[state.state] = outNewStMap[state.output];
            //std::cout << state.state << " " << newMinStateName << "\n";

        }
    }


    std::map <std::string, std::vector<MooreState>> NewMooreInGroup; // A1, {y1, q1, {z1, A0}, {z1, A0}}

    // заполнение NewMealyInGroup
    for (const auto& state : mooreAutomaton)
    {
        MooreState tempState;
        std::vector<TransMoore> tempVec;
        for (const auto& transition : state.transitions)
        {
            TransMoore newTrans;
            newTrans.inputSym = transition.inputSym;
            newTrans.nextPos = newStateMap[transition.nextPos];
            //newTrans.outSym = transition.outSym;
            tempVec.push_back(newTrans);
        }
        tempState.output = state.output;
        tempState.state = state.state; //q0
        tempState.transitions = tempVec;
        NewMooreInGroup[newStateMap[tempState.state]].push_back(tempState);
        //std::cout << newStateMap[tempState.curr] << " " << tempState.curr << "";

    }

    auto prevSize = outSet.size();
    size_t currSize = 0;
    bool equal = 0;
    while (prevSize != currSize)
    {
        ch++;
        int i = 0;
        currSize = 0;
        std::map<std::string, std::string> tempNewStateMap; // q0 -> ch++
        std::map <std::string, std::vector<MooreState>> tempNewMooreInGroup; // A1, {y1, q1, {z1, A0}, {z1, A0}}

        for (const auto& group : NewMooreInGroup)
        {
            std::map<std::vector<std::string>, std::string> vecNextNewStMap; // q1, q2 -> A0; mapOutGr
            std::set<std::vector<std::string>> vecTransSet;
            //std::cout << group.first;
            for (const auto& state : group.second)
            {
                //std::cout << group.first;
                std::vector<std::string> transVector;
                for (const auto& transition : state.transitions)
                {
                    transVector.push_back(transition.nextPos);
                }
                if (vecTransSet.insert(transVector).second)  // набор переходов A1, A2, A3
                {
                    std::string newMinStateName = ch + std::to_string(i);
                    vecNextNewStMap[transVector] = newMinStateName;  // mapOutGr
                    tempNewStateMap[state.state] = newMinStateName;
                    i++;
                }
                else
                {
                    tempNewStateMap[state.state] = vecNextNewStMap[transVector];
                }
                currSize += vecTransSet.size();
            }
        }
        for (const auto& state : mooreAutomaton)
        {
            MooreState tempState;
            std::vector<TransMoore> tempVec;
            for (const auto& transition : state.transitions)
            {
                TransMoore newTrans;
                newTrans.inputSym = transition.inputSym;
                newTrans.nextPos = tempNewStateMap[transition.nextPos];
                //newTrans.outSym = transition.outSym;
                tempVec.push_back(newTrans);
            }
            tempState.state = state.state; //q0
            tempState.output = state.output; //y1
            tempState.transitions = tempVec;
            tempNewMooreInGroup[tempNewStateMap[tempState.state]].push_back(tempState);
        }

        NewMooreInGroup = tempNewMooreInGroup;
        newStateMap = tempNewStateMap;

        if (prevSize == currSize)
        {
            break;
        }
        else
        {
            prevSize = currSize;
            currSize = 0;
        }
    }

    std::vector<MooreState> tempMealy;

    for (const auto& group : NewMooreInGroup)
    {
        std::vector<TransMoore> tempVec;
        MooreState tempState = group.second[0];

        for (const auto& trans : tempState.transitions)
        {
            TransMoore newTrans;
            newTrans.inputSym = trans.inputSym;
            newTrans.nextPos = trans.nextPos;
            //std::cout << trans.nextPos <<  " "  << newTrans.nextPos << "\n";
            //newTrans.outSym = trans.outSym;
            tempVec.push_back(newTrans);
        }
        tempState.state = group.first; //A, B, C
        tempState.output = group.second[0].output; //y1
        tempState.transitions = tempVec;
        NewMooreInGroup[newStateMap[tempState.state]].push_back(tempState);
        tempMealy.push_back(tempState);
    }

    return tempMealy;
}

std::vector<MealyState> ReadMealyToVec(std::vector<MealyState>& positions, std::ifstream& file)
{

    std::string line;
    std::getline(file, line);
    std::vector<std::string> tempRow = split(line, ';');


    for (size_t i = 1; i < tempRow.size(); ++i) // Пропускаем первый элемент (пустой)
    {
        MealyState currPos;
        currPos.curr = tempRow[i]; // Текущая позиция - имя состояния
        positions.push_back(currPos);
    }

    std::vector<std::string> tRow;
    while (std::getline(file, line))
    {
        tempRow = split(line, ';');
        std::string input_sym = tempRow[0];
        for (int i = 0; i < positions.size(); i++)
        {

            Trans trans;
            tRow = split(tempRow[i + 1], '/');
            trans.inputSym = input_sym;
            trans.nextPos = tRow[0];
            trans.outSym = tRow[1];
            //std::cout << tRow[0] << " " << tRow[1] << "\n";
            positions[i].transitions.push_back(trans);
        }
    };
    file.close();

    return positions;
}

std::vector<MooreState> ReadMooreToVec(std::vector<MooreState>& positions, std::ifstream& file)
{
    std::string line, lineSt;
    std::getline(file, line);
    std::vector<std::string> tempRow = split(line, ';');
    std::getline(file, lineSt);
    std::vector<std::string> tempRowSt = split(lineSt, ';');


    for (size_t i = 1; i < tempRowSt.size(); ++i) // Пропускаем первый элемент 
    {
        MooreState state;
        if (i >= tempRow.size())
        {
            state.output = "-";
        }
        else
        {
            state.output = tempRow[i];
        }
        //state.output = tempRow[i];
        state.state = tempRowSt[i]; // Текущая позиция - имя состояния
        positions.push_back(state);
    }

    std::string tRow;
    while (std::getline(file, line))
    {
        tempRow = split(line, ';');

        std::string input_sym = tempRow[0];
        for (int i = 0; i < positions.size(); i++)
        {
            TransMoore trans;
            trans.inputSym = input_sym;
            if ((i + 1) >= tempRow.size())
            {
                trans.nextPos = "-";
            }
            else
            {
                trans.nextPos = tempRow[i + 1];
            }
            std::cout << trans.nextPos << " ";
            positions[i].transitions.push_back(trans);
        }
        std::cout << " \n";
    };
    file.close();

    return positions;
}


void WriteMealyToFile(std::vector<MealyState>& mealyAutomaton, std::ofstream& outFile) {
    if (outFile.is_open()) {
        //outFile << ";";
        for (const auto& state : mealyAutomaton) {
            outFile << ";" << state.curr;
        }
        outFile << "\n";

        for (size_t i = 0; i < mealyAutomaton[0].transitions.size(); ++i) {
            const auto& inputSym = mealyAutomaton[0].transitions[i].inputSym;
            outFile << inputSym;

            for (const auto& state : mealyAutomaton) {
                const auto& trans = state.transitions[i];
                outFile << ";" << trans.nextPos << "/" << trans.outSym;
            }
            outFile << "\n";
        }

        outFile.close();
        std::cout << "CSV file has been created successfully.\n";
    }
    else {
        std::cerr << "Failed to open file for writing.\n";
    }
}

void WriteMooreToFile(std::vector<MooreState>& mooreAutomaton, std::ofstream& outFile)
{
    if (outFile.is_open())
    {

        //outFile << ";";
        for (const auto& state : mooreAutomaton)
        {
            outFile << ";" << state.output;
        }
        outFile << "\n";

        //outFile << ";";
        for (const auto& state : mooreAutomaton)
        {
            outFile << ";" << state.state;
        }
        outFile << "\n";

        //for (const auto& trans : mooreAutomaton[0].transitions)
        //{
        //    outFile << trans.inputSym << ";"; // Символ входа (inputSym)
        //    for (const auto& state : mooreAutomaton)
        //    {
        //        // Записываем соответствующие переходы для каждого состояния
        //        outFile << state.transitions.at(trans.inputSym) << ";";

        //    }
        //    outFile << "\n";
        //}
        for (size_t i = 0; i < mooreAutomaton[0].transitions.size(); ++i) {
            const auto& inputSym = mooreAutomaton[0].transitions[i].inputSym;
            outFile << inputSym;

            for (const auto& state : mooreAutomaton) {
                const auto& trans = state.transitions[i];
                outFile << ";" << trans.nextPos;
            }
            outFile << "\n";
        }

        outFile.close();
        std::cout << "CSV file has been created successfully.\n";
    }
    else
    {
        std::cerr << "Failed to open file for writing.\n";
    }

}

int main(int argc, char* argv[])
{
    std::string nameOperation;

    //nameOperation = "mealy-to-moore";
    //std::string inputFileName = "mealy_src.csv";
    //std::string outputFileName = "src.csv";

    nameOperation = "moore";
    std::string inputFileName = "input.csv";
    std::string outputFileName = "output.csv";


    std::ifstream file(inputFileName);
    std::ofstream outFile(outputFileName);

    if (!file.is_open())
    {
        std::cerr << "Ошибка при открытии файла!" << std::endl;
        return 1;
    }


    if (nameOperation == "mealy")
    {
        std::vector<MealyState> mealyStates;
        mealyStates = ReadMealyToVec(mealyStates, file);

        //std::vector<MealyState> reachableStates = findReachableStates(mealyStates);

        std::vector<MealyState> minimize = mealyMin(mealyStates);

        WriteMealyToFile(minimize, outFile);
        //WriteMooreToFile(mooreAutomaton, outFile);


    }
    else if (nameOperation == "moore")
    {
        std::vector<MooreState> mooreStates;
        mooreStates = ReadMooreToVec(mooreStates, file);

        std::vector<MooreState> reachableStates = findMooreReachableStates(mooreStates);

        std::vector<MooreState> minimize = mooreMin(reachableStates);
        WriteMooreToFile(minimize, outFile);
    }
    return 0;
}