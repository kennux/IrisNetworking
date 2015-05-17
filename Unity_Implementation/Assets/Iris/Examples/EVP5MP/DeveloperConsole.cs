using UnityEngine;
using System.Collections;

/// <summary>
/// This class is a base for every developer console in this project.
///	It implements rendering, input handling and some basic commands.
/// You can extend it like you want!
/// 
/// </summary>
public class DeveloperConsole : MonoBehaviour
{
	private static DeveloperConsole instance;

	public static DeveloperConsole GetInstance()
	{
		if (instance == null)
			instance = Component.FindObjectOfType<DeveloperConsole> ();

		return instance;
	}

	public KeyCode toggleConsoleKeycode;

	/// <summary>
	/// Gets set to true when the console gets opened by the user.
	/// </summary>
	private bool consoleOpen = false;

	/// <summary>
	/// The content of the console.
	/// </summary>
	private string consoleContent = "";

	/// <summary>
	/// The content of the console input field.
	/// </summary>
	private string inputContent = "";

	public void OnEnable()
	{
		instance = this;
		Input.eatKeyPressOnTextFieldFocus = false;
	}

	/// <summary>
	/// Update this instance.
	/// Toggles console activation.
	/// </summary>
	public void Update()
	{
		if (Input.GetKeyDown(this.toggleConsoleKeycode))
		{
			this.consoleOpen = !this.consoleOpen;
		}

		if (Input.GetKeyDown (KeyCode.Return))
		{
			this.HandleConsoleInput (this.inputContent);
			this.inputContent = "";
		}
	}

	/// <summary>
	/// Raises the GUI rendering event.
	/// This function will handle the GUI Rendering of the console.
	/// </summary>
	public void OnGUI()
	{
		if (!this.consoleOpen)
			return;

		float height = Screen.height / 4;

		GUI.Box (new Rect (0, 0, Screen.width, height), "");

		GUILayout.BeginArea (new Rect (10, 10, Screen.width - 20, height - 50));

		GUILayout.BeginScrollView(new Vector2(0,Mathf.Infinity), false, false);
		GUILayout.TextArea (this.consoleContent);
		GUILayout.EndScrollView ();

		GUILayout.EndArea ();

		this.inputContent = GUI.TextField (new Rect (10, height - 30, Screen.width - 130, 25), this.inputContent);

		if (GUI.Button(new Rect(Screen.width - 110, height - 30, 100, 25), "Send"))
		{
			this.HandleConsoleInput (this.inputContent);
			this.inputContent = "";
		}
	}

	/// <summary>
	/// Handles the console input.
	/// This function will call the virtual function InterpretConsoleCommand(string, string[]).
	/// </summary>
	/// <param name="input">Input.</param>
	private void HandleConsoleInput(string command)
	{
		// Split the command parts
		string[] commandParts = command.Split (' ');

		bool interpreted = false;

		// Try interpretation
		if (command.Length >= 2)
		{
			interpreted = this.InterpretConsoleCommand (commandParts [0], commandParts);
		}

		if (!interpreted)
		{
			this.LogToConsole("Console", "Couldn't interpret command '" + command + "'");
		}
	}
	
	/// <summary>
	/// Log the given string from the given section to the console.
	/// </summary>
	/// <param name="section">Section.</param>
	/// <param name="message">Message.</param>
	public void LogToConsole(string section, string message)
	{
		this.consoleContent += "{" + Time.time  + "} - [" + section + "]: " + message + "\r\n";
	}
	
	/// <summary>
	/// Log the given string from the given section to the console.
	/// </summary>
	/// <param name="section">Section.</param>
	/// <param name="message">Message.</param>
	public void LogErrorToConsole(string section, string message)
	{
		this.consoleContent += "{" + Time.time  + "} - [" + section + "]: " + message + "\r\n";
		Debug.LogError ("{" + Time.time + "} - [" + section + "]: " + message);
	}

	/// <summary>
	/// Interprets the console command.
	/// </summary>
	/// <returns><c>true</c>, if console command was interpreted, <c>false</c> otherwise.</returns>
	/// <param name="command">Command.</param>
	/// <param name="parameters">Parameters.</param>
	protected virtual bool InterpretConsoleCommand(string command, string[] parameters)
	{
		switch (command)
		{
		default:
			return false;
			break;
		}

		return true;
	}
}
