using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using KModkit;

public class DIWindowScript : MonoBehaviour
{

	public KMBombInfo Bomb;
	public KMAudio Audio;

	public KMSelectable playButton;
	public KMSelectable subButton;
	public KMSelectable screen;
	public KMSelectable[] keys;

	public AudioClip[] clips;
	public Dictionary<String, AudioClip> clipDict = new Dictionary<String, AudioClip>();

	static int moduleIdCounter = 1;
	private int moduleId;
	private bool moduleSolved;

	public GameObject subBox;
	private Pair[] subStrings = new Pair[50];
	private int currSub;
	private int currInSub;

	public TextMesh inputText;
	String answer = "0";
	Speaker spoken = new Speaker();
	private bool uni;
	static bool isPlaying;
	private bool solved;

	private IDictionary<string, object> tpAPI;
	private bool TwitchPlaysActive;

	private String[] ops = new String[5] 
		{"none", "adda", "adds", "add", "loop"};

	private String[] restaurants = new String[16] 
		{"Cluckin' Bell", "Cow-Fil-A", "Burger Queen", "Mr. Donalds", "Panda Train", "KFC", 
		"Wienersmorgasbord", "Dairy King", "Coffeebucks", "Sbbuby", "Timothy Horton's",  
		"Weenie Hut Jr.", "805 Kitchen", "Inside and Out", "Seven Guys", "Jack in the Cube"};

	private String[] variables = new String[21]
		{"John F. Kennedy", "Jimmy Neutron", "Sonic the Hedgehog", "Funky Kong", "Owen Wilson",
		"Sans Undertale", "Garfield", "Microsoft Sam", "Dr. Bright", "Heavy from Team Fortress 2",
		"Mark Nutt", "Millhouse Manastorm", "Captain Alex", "Scatman John", "Ben Dover", 
		"Flynt Coal", "Piano Man", "Alex Mason", "Plankton", "Lil' Broomstick", "Robbie Rotten"};

	private String[] items = new String[19]
		{"A Large Meatball Sub", "A Knuckle Sandwich", "Leftovers", "A Midnight Snack", "Overnight Oats",
		"A Super Meat Boy", "A 6 Piece Nugget Meal", "A 9 Piece Nugget Meal", "A 20 Piece Nugget Meal",
		"Soylent Green", "A Humble Pie", "General Tso's Chicken", "Food for Thought", "An XXXL Soda", 
		"Literally Nothing", "A Small Mac", "Chowder", "A Half Stack of Flapjacks", "Dirt"};

	private String[] sides = new String[17]
		{"Bathroom Code", "Food Poisoning", "Sodium Chloride", "Happy Meal Toy", "Tater Tots",
		"Mercury", "Patty", "Chips", "Soup Nuts", "Top Banana", "Forbidden Fruit", "Utensils",
		"20 Sauce Packets", "Secret Sauce", "Animal Fries", "Instant Death", "Garlic Bread"};

	void Awake()
	{
		moduleId = moduleIdCounter++;
		foreach(KMSelectable key in keys)
		{
			KMSelectable pressedKey = key;
			key.OnInteract += delegate() { KeyPress(pressedKey); return false; };
		}
		subButton.OnInteract += delegate() { SubPress(); return false; };
		screen.OnInteract += delegate() { ScreenPress(); return false; };
		playButton.OnInteract += delegate() { PlayPress(); return false; };
		// This allows for faster searching for audio files based on their name
		foreach (AudioClip clip in clips) clipDict.Add(clip.name, clip);
		// Because static, need to reset variable between bombs in case blown up/solved before finished playing
		isPlaying = false;
		GetComponent<KMBombModule>().OnActivate += TPActive;
	}

	// Use this for initialization
	void Start()
	{
		UniCheck();
		GenerateCode();
	}

	// Code for when number keys are pressed
	void KeyPress(KMSelectable key)
	{
		if (!solved)
		{
			key.AddInteractionPunch();
			Audio.PlaySoundAtTransform("keyStroke", transform);
			// Gets the text on the button and adds it to the input
			if (inputText.text.Length < 15)
			{
				inputText.text += key.GetComponentInChildren<TextMesh>().text;
			}
		}
	}

	// Code for when play button is pressed
	void PlayPress()
	{
		playButton.AddInteractionPunch();
		if (uni)
		{
			Audio.PlaySoundAtTransform("bigSmoke", transform);
		} else
		{
			if (!isPlaying) StartCoroutine(playCode());
		}
	}

	// Code for when submit is pressed; handles solve/strike and logging
	void SubPress()
	{
		if (!solved)
		{
			subButton.AddInteractionPunch();
			Debug.LogFormat("[Drive-In Window #{0}] You submitted {1}", moduleId, screen.GetComponentInChildren<TextMesh>().text);
			// Solve/Strike Handling
			if (inputText.text == answer)
			{
				Audio.PlaySoundAtTransform("cashRegister", transform);
				GetComponent<KMBombModule>().HandlePass();
				solved = true;
				Debug.LogFormat("[Drive-In Window #{0}] That was correct, module solved.", moduleId);
			} else
			{
				GetComponent<KMBombModule>().HandleStrike();
				inputText.text = "";
				Debug.LogFormat("[Drive-In Window #{0}] That was incorrect, clearing input.", moduleId);
			}
		}

	}

	// Code for when the screen is pressed; clears input
	void ScreenPress()
	{
		if (!solved)
		{
			screen.AddInteractionPunch();
			inputText.text = "";
		}
	}

	// Ran at startup, checks for unicorn and sets variable accordingly
	void UniCheck()
	{
		// Code that checks for unicorn
		int nines = 0;
		foreach (int num in Bomb.GetSerialNumberNumbers())
		{
			if (num == 9) nines++;
		}
		if (Bomb.IsIndicatorOn("CAR") && (nines > 1))
		{
			uni = true;
		}
	}

	// Code ran at startup that generates the sequence of lines of code to be spoken; also handles unicorn answer
	void GenerateCode()
	{
		if (uni)
		{
			// Reference to https://www.youtube.com/watch?v=A6g0mPo-uJM
			Debug.LogFormat("[Drive-In Window #{0}] You gotta eat to keep your stremf up. Two 9's and a lit CAR present, your order is Big Smoke's order.", moduleId);
			answer = "999674545";
		} else
		{
			// Keeps track of the index of subStrings because I (once again) forgot there was no .append()
			int subIndex = 0;
			Debug.LogFormat("[Drive-In Window #{0}] Generated code is as follows:", moduleId);
			// Vars with R at the end are random variables for the named random aspects
			int restR = UnityEngine.Random.Range(0, 16);
			int[] namesR = diffRand(0, 21, 2);
			int[] itemsR = diffRand(0, 19, 3);
			int[] sidesR = diffRand(0, 17, 3);
			// Vars with RN at the end are random variables that are numbers
			int[] pricesRN = diffRand(1, 10, 3);
			int[] sidesRN = diffRand(1, 10, 3);
			// Arrays of Pair objects that store a name and value
			Pair[] namesT = new Pair[2];
			Pair[] itemsT = new Pair[3];
			Pair[] sidesT = new Pair[3];
			// Start of "code"
			spoken.append("welcome");
			spoken.append(restaurants[restR]);
			// Next two lines set up subtitles in the same way the log is formatted and iterates through the list each time
			// Subtitles are generated in GenerateCode() and shown in PlayCode()
			subStrings[subIndex] = (new Pair(String.Format("Welcome to {0}.", restaurants[restR]), 2));
			subIndex++;
			Debug.LogFormat("[Drive-In Window #{0}] Welcome to {1}.", moduleId, restaurants[restR]);
			// Constant definitions; adds items and sides to spoken as well as populates itemsT and sidesT
			spoken.append("menu");
			subStrings[subIndex] = (new Pair("Here is a menu.", 1));
			subIndex++;
			Debug.LogFormat("[Drive-In Window #{0}] Here is a menu.", moduleId);
			for (int i = 0; i < 3; i++)
			{
				spoken.append(items[itemsR[i]]);
				spoken.append("cost");
				spoken.append((pricesRN[i] * 10).ToString());
				spoken.append("dollars");
				itemsT[i] = new Pair(items[itemsR[i]], (pricesRN[i] * 10));
				subStrings[subIndex] = (new Pair(String.Format("{0} will cost {1} dollars.", items[itemsR[i]], (pricesRN[i] * 10)), 4));
				subIndex++;
				Debug.LogFormat("[Drive-In Window #{0}] {1} will cost {2} dollars.", moduleId, items[itemsR[i]], (pricesRN[i] * 10));
			}
			spoken.append("sides");
			subStrings[subIndex] = (new Pair("Here are your sides.", 1));
			subIndex++;
			Debug.LogFormat("[Drive-In Window #{0}] Here are your sides.", moduleId);
			for (int i = 0; i < 3; i++)
			{
				spoken.append(sides[sidesR[i]]);
				spoken.append("cost");
				spoken.append((sidesRN[i]).ToString());
				spoken.append("dollars");
				sidesT[i] = new Pair(sides[sidesR[i]], sidesRN[i]);
				subStrings[subIndex] = (new Pair(String.Format("{0} will cost {1} dollars.", sides[sidesR[i]], sidesRN[i]), 4));
				subIndex++;
				Debug.LogFormat("[Drive-In Window #{0}] {1} will cost {2} dollars.", moduleId, sides[sidesR[i]], sidesRN[i]);
			}
			// Start of actual "code"
			spoken.append("may");
			subStrings[subIndex] = (new Pair("May I take your order?", 1));
			subIndex++;
			Debug.LogFormat("[Drive-In Window #{0}] May I take your order?", moduleId);
			// Initializes variable names and puts them in namesT
			for (int i = 0; i < 2; i++) namesT[i] = new Pair(variables[namesR[i]], 0);
			// Create new String[] tempCode of operations to put in spoken; populates first one with a guarenteed add for multiple reasons
			// Reasons include different syntax for first add, prevents all "none"'s being chosen, and prevents loops that run zero times
			String[] tempCode = new String[7];
			tempCode[0] = "addf";
			// Spaghetti code that allows me to track how much money was gained from a loop add so I can properly multiply
			int before = 0;
			int after = 0;
			// Adds 2 random operations to tempCode to add to spoken
			// Choices are adda, adds, and none
			for (int i = 1; i < 3; i++)
			{
				tempCode[i] = ops[UnityEngine.Random.Range(0, 3)];
			}
			// Populates end of tempCode with a loop, because without a guaranteed loop the module is kinda boring
			// loop is not included in the random to keep the read time down (two loops could make a module )
			tempCode[3] = "loop";
			int tempRandI;
			int tempRandS;
			// Handles the random and guarenteed operations generated in tempCode
			for (int i = 0; i < 7; i++)
			{
				// Creates two random numbers to be used for the item and side index for each pass, respectively
				tempRandI = UnityEngine.Random.Range(0, 3);
				tempRandS = UnityEngine.Random.Range(0, 3);
				// Switch statement with all of the possibilities in ops
				switch (tempCode[i])
				{
					// Adds value of tempRandI's respective item to the first variable
					// Example: John F. Kennedy would also like a Knuckle Sandwhich
					case ("add"):
						spoken.append(namesT[0].Name);
						spoken.append("add");
						spoken.append(itemsT[tempRandI].Name);
						namesT[0].Val += itemsT[tempRandI].Val;
						spoken.append("stop");
						subStrings[subIndex] = (new Pair(String.Format("{0} would also like {1}.", namesT[0].Name, itemsT[tempRandI].Name), 4));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} would also like {2}.", moduleId, namesT[0].Name, itemsT[tempRandI].Name);
						break;
					// Adds value of tempRandI's respective item to the first variable for the first time
					// Example: John F. Kennedy would like a Knuckle Sandwhich
					case ("addf"):
						spoken.append(namesT[0].Name);
						spoken.append("addf");
						spoken.append(itemsT[tempRandI].Name);
						namesT[0].Val += itemsT[tempRandI].Val;
						spoken.append("stop");
						subStrings[subIndex] = (new Pair(String.Format("{0} would like {1}.", namesT[0].Name, itemsT[tempRandI].Name), 4));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} would like {2}.", moduleId, namesT[0].Name, itemsT[tempRandI].Name);
						break;
					// Adds value of tempRandI's respective item plus tempRandS' respective side to the first variable
					// Example: John F. Kennedy would also like a Knuckle Sandwhich with Sodium Chloride
					case ("adda"):
						spoken.append(namesT[0].Name);
						spoken.append("add");
						spoken.append(itemsT[tempRandI].Name);
						spoken.append("with");
						spoken.append(sidesT[tempRandS].Name);
						namesT[0].Val += itemsT[tempRandI].Val + sidesT[tempRandS].Val;
						spoken.append("stop");
						subStrings[subIndex] = (new Pair(String.Format("{0} would also like {1} with a side of {2}.", namesT[0].Name, itemsT[tempRandI].Name, sidesT[tempRandS].Name), 6));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} would also like {2} with a side of {3}.", moduleId, namesT[0].Name, itemsT[tempRandI].Name, sidesT[tempRandS].Name);
						break;
					// Adds value of tempRandI's respective item minus tempRandS' respective side to the first variable
					// Example: John F. Kennedy would also like a Knuckle Sandwhich, hold the Sodium Chloride
					case ("adds"):
						spoken.append(namesT[0].Name);
						spoken.append("add");
						spoken.append(itemsT[tempRandI].Name);
						spoken.append("hold");
						spoken.append(sidesT[tempRandS].Name);
						namesT[0].Val += itemsT[tempRandI].Val - sidesT[tempRandS].Val;
						spoken.append("stop");
						subStrings[subIndex] = (new Pair(String.Format("{0} would also like {1}, hold the {2}.", namesT[0].Name, itemsT[tempRandI].Name, sidesT[tempRandS].Name), 6));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} would also like {2}, hold the {3}.", moduleId, namesT[0].Name, itemsT[tempRandI].Name, sidesT[tempRandS].Name);
						break;
					// Begins loop, no math yet; adds an add and loopend to ops
					// Example: Jimmy Neutron would like what John F. Kennedy is having.
					// Example: Let's just do this until Jimmy Neutron has no more money!
					case ("loop"):
						spoken.append(namesT[1].Name);
						spoken.append("like");
						spoken.append(namesT[0].Name);
						spoken.append("having");
						spoken.append("stop");
						spoken.append("loop1");
						spoken.append(namesT[1].Name);
						spoken.append("loop2");
						tempCode[i + 1] = ops[UnityEngine.Random.Range(1, 3)];
						tempCode[i + 2] = "loopend";
						before = namesT[0].Val;
						spoken.append("stop");
						subStrings[subIndex] = (new Pair(String.Format("{0} would like what {1} is having.", namesT[1].Name, namesT[0].Name), 5));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} would like what {2} is having.", moduleId, namesT[1].Name, namesT[0].Name);
						subStrings[subIndex] = (new Pair(String.Format("Let's just do this until {0} has no more money!", namesT[1].Name), 4));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] Let's just do this until {1} has no more money!", moduleId, namesT[1].Name);
						break;
					// Ends loop, does math; adds how much was added between loop and loopend a number of times based on how many times a chosen side's price fits into the before price
					// Example: Jimmy Neutron would not like Sodium Chloride.
					// Example: Jimmy Neutron has no more money!
					case ("loopend"):
						spoken.append(namesT[1].Name);
						spoken.append("liken't");
						spoken.append(sidesT[tempRandS].Name);
						spoken.append("stop");
						spoken.append(namesT[1].Name);
						spoken.append("loop2");
						after = namesT[0].Val;
						// Multiplication for loop; ends at side price to make up for add during loop
						for (int j = before; j > sidesT[tempRandS].Val; j -= sidesT[tempRandS].Val) 
						{
							namesT[0].Val += after - before;
						}
						spoken.append("stop");
						subStrings[subIndex] = (new Pair(String.Format("{0} would not like {1}.", namesT[1].Name, sidesT[tempRandS].Name), 4));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} would not like {2}.", moduleId, namesT[1].Name, sidesT[tempRandS].Name);
						subStrings[subIndex] = (new Pair(String.Format("{0} has no more money!", namesT[1].Name), 3));
						subIndex++;
						Debug.LogFormat("[Drive-In Window #{0}] {1} has no more money!", moduleId, namesT[1].Name);
						break;
					// Code for if "none" is selected; does not add anything to spoken
					default:
						break;
				}
			}
			// Example: John F. Kennedy will pay for their order!
			// Example: OK, that will be $3.50. Thanks for coming!
			spoken.append(namesT[0].Name);
			spoken.append("fin");
			answer = namesT[0].Val.ToString();
			subStrings[subIndex] = (new Pair(String.Format("{0} will pay for their order!\nOK, that will be $3.50. Thanks for coming!", namesT[0].Name), 2));
			subIndex++;
			Debug.LogFormat("[Drive-In Window #{0}] {1} will pay for their order!", moduleId, namesT[0].Name);
			Debug.LogFormat("[Drive-In Window #{0}] OK, that will be $3.50. Thanks for coming!", moduleId);
			Debug.LogFormat("[Drive-In Window #{0}]", moduleId);
			Debug.LogFormat("[Drive-In Window #{0}] The answer to be submitted is {1}", moduleId, answer);
		}
	}

	// IEnumerator that plays the spoken code and disables use of the button during playing
	// Reason for IEnumerator is I need WaitForSeconds between each so it doesn't play every single audio file at once
	IEnumerator playCode()
	{
		isPlaying = true;
		currSub = 0;
		currInSub = 0;
		foreach(String word in spoken.Output)
		{
			if (String.IsNullOrEmpty(word)) break;
			Audio.PlaySoundAtTransform(word, transform);
			// Shows Subtitles in a text box at the base of the screen
			if (TwitchPlaysActive)
			{
				tpAPI["ircConnectionSendMessage"] = String.Format("Module {0} | {1}", GetModuleCode(), subStrings[currSub].Name);
			}
			else
			{
				subBox.GetComponent<Text>().text = subStrings[currSub].Name;
			}
			// Convuluted solution to having the subtitles last exactly as long as the voice clip
			// Essentially in each Pair declaration the val was how many voice lines play between full sentences
			// This increments a counter every time a voice line is played until it reaches the val, after which is goes to the next subtitle
			currInSub++;
			if (currInSub == subStrings[currSub].Val)
			{
				currSub++;
				currInSub = 0;
			}
			yield return new WaitForSeconds(clipDict[word].length);
		}
		// Turns off subtitles after the lines stop playing
		subBox.GetComponent<Text>().text = "";
		isPlaying = false;
	}

	// Class made for 2 reasons; tuple arrays don't work in C# 4, and it makes reading and keeping track in code easier (names instead of indices)
	class Pair
	{
		private String name;
		private int val;

		public Pair(String nameIn, int valueIn)
		{
			name = nameIn;
			val = valueIn;
		}

		public String Name
		{
			get { return name; }
			set { name = value; }
		}

		public int Val
		{
			get { return val; }
			set { val = value; }
		}

		public override String ToString()
		{
			return String.Format("Pair name is {0}, Pair val is {1}", name, val);
		}
	}

	// Class made because I forgot arrays don't have .append() and I didn't want to change it to a list
	class Speaker
	{
		private String[] output;
		private int count;

		public Speaker()
		{
			output = new String[200];
			count = 0;
		}

		public String[] Output
		{
			get { return output; }
		}

		public int Count
		{
			get { return count; }
		}

		public void append(String sin)
		{
			output[count] = sin;
			count++;
		}
	}

	// Generates multiple random numbers that are different from each other; least is minimum, most is maximum, num is the amount of numbers to generate
	int[] diffRand(int least, int most, int num)
	{
		int[] ret = new int[num];
		int tempRand = UnityEngine.Random.Range(least, most);
		for (int i = 0; i < num; i++)
		{
			ret[i] = tempRand;
			while (ret.Contains(tempRand)) tempRand = UnityEngine.Random.Range(least, most);
		}
		return ret;
	}

	// Twitch Plays Integration
#pragma warning disable 414
	private readonly String TwitchHelpMessage = @"Use !{0} play to play the code, and !{0} submit 12345 to submit an answer.";
#pragma warning restore 414

	// Processes Twitch Commands. Duh.
	KMSelectable[] ProcessTwitchCommand(string command)
	{
		command = command.Trim().ToLowerInvariant();
		if (command == "play")
		{
			// Presses the play button when !{0} play is used
			return new[] {playButton};
		} else if (Regex.IsMatch(command, @"submit +[0-9]"))
		{
			// Takes everything after "submit " and presses the keys at those indices
			String submitted = command.Substring(7).Trim();
			// Makes the array submitted length + 1 to make room for the submit button
			KMSelectable[] ret = new KMSelectable[submitted.Length + 1];
			for (int i = 0; i < submitted.Length; i++)
			{
				ret[i] = keys[int.Parse(submitted[i].ToString())];
			}
			ret[submitted.Length] = subButton;
			return ret;
		}
		return null;
	}

	// Runs when !solve is used; Changes the input text to the answer and makes the solve sounds, then solves the module
	void TwitchHandleForcedSolve()
	{
		inputText.text = answer;
		Audio.PlaySoundAtTransform("cashRegister", transform);
		GetComponent<KMBombModule>().HandlePass();
		solved = true;
	}

	void TPActive()
	{
		Debug.Log(TwitchPlaysActive);
		if (TwitchPlaysActive)
        {
            GameObject tpAPIGameObject = GameObject.Find("TwitchPlays_Info");
            //To make the module can be tested in test harness, check if the gameObject exists.
            if (tpAPIGameObject != null)
			{
                tpAPI = tpAPIGameObject.GetComponent<IDictionary<string, object>>();
			}
            else
			{
                TwitchPlaysActive = false;
			}
        }
		Debug.Log(TwitchPlaysActive);
	}

	// Gets the module code on Twitch Plays
	// I'd comment if I had any idea how it worked
	private string GetModuleCode()
    {
        Transform closest = null;
        float closestDistance = float.MaxValue;
        foreach (Transform children in transform.parent)
        {
            var distance = (transform.position - children.position).magnitude;
            if (children.gameObject.name == "TwitchModule(Clone)" && (closest == null || distance < closestDistance))
            {
                closest = children;
                closestDistance = distance;
            }
        }

        return closest != null ? closest.Find("MultiDeckerUI").Find("IDText").GetComponent<UnityEngine.UI.Text>().text : null;
    }

}