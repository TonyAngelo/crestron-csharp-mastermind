# crestron-csharp-mastermind

For whatever reason when learning new control system languages, in this case crestron c#, I like to recreate the game [Mastermind](https://en.wikipedia.org/wiki/Mastermind_\(board_game\)).


## The Program

Consists of ControlSystem.cs, Mastermind.cs and MastermindGUI.cs

- ControlSystem.cs - defualt entry file for Crestron C# programs. Instantiates an Xpanel, the Mastermind game and the MastermindGUI. Subscribes to their respective callbacks and coordinates their actions.

- Mastermind.cs - has a callback event (MastermindEvent) and two methods:
	- NewGame(), starts a new game. Will respond to the MastermindEvent with the new games parameters.
	- EvalAnswer(int[]), evaluates an answer (the int[]) to see if it's correct, will respond to the MastermindEvent with the results, one of: Winner (correct answer), Loser (incorrect answer and no more guesses available) or NewClues (incorrect answer but guesses still available).

- MastermindGUI.cs - has five methods:
	- SetColorSelection(uint), sets the color selection to the option chosen
	- SetGuessSpot(uint), sets the spot chosen to the Selected Color
	- ShowNewClues(int[], unit), reveals the clues based on the last answer and enables the next row of guess spots
	- EndGame(string, int[]), reveals the correct answer and displays a message to the user
	- NewGame(int, int, uint), takes three game parameters (number of colors, max guesses and size of the code) and sets up a new game


## The GUI

Is a VTPro Xpanel. MastermindGUI.cs is agnostic about which touch panel is used, but it's not agnostic about join numbers. When the MastermindGUI class is instantiated a "StartID" variable is passed, this is the Digital Join ID your numbering starts at. In the list below the StartID would be 1. Where you start the JoinIDs doesn't matter, as long as the JoinIDs are all in the same relative spots.

### Digital Joins

1-3: [PRESS] help, new game and submit answer
4-9: [PRESS] color selections
11-50: [PRESS] guess spots
51-90: [ENABLE] guess spots
91-130: [ENABLE] clue spots

### Analog Joins

11-50: [MODE] guess spots
51-90: [MODE] clue spots
91-94: [MODE] answer spots

### Serial Joins

1: [DYNAMIC] game message