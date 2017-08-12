using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Calibration : MonoBehaviour {

	public Text instructionText;

	void Start () {
		if (instructionText == null)
			throw new System.NullReferenceException("instructionText");

		StartCoroutine(Calibrate());
	}

	private IEnumerator Calibrate()
	{
		yield return UpdateInstruction("I need to know when to click");
		yield return UpdateInstruction("So you will need to relax");
		yield return UpdateInstruction("And then indicate with your face");
		yield return UpdateInstruction("You can either blink,");
		yield return UpdateInstruction("or open your eyes wide");
		yield return UpdateInstruction("Get ready...");
		yield return UpdateInstruction("Look at the camera");
		yield return UpdateInstruction("Relax your face");
		yield return UpdateInstruction("Now indicate");
		yield return UpdateInstruction("Relax your face");
		yield return UpdateInstruction("Now indicate");
		yield return UpdateInstruction("Relax your face");
		yield return UpdateInstruction("Now indicate");
		yield return UpdateInstruction("All done");
		yield return UpdateInstruction("");
	}

	IEnumerator UpdateInstruction(string instruction)
	{
		instructionText.text = instruction;
		yield return new WaitForSeconds(4);
	}

	
}
