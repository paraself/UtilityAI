﻿// This component is added to an actor. 
// It collects all actions and considerations and
// it evaluates which action has the highest utility score

using UnityEngine;
using System.Collections.Generic;

[AddComponentMenu("UtilityAI/Agent")]
public class UAI_Agent : MonoBehaviour {

	public string agentName;
	public bool consoleLogging = false;
	public int historyStates = 10;
	public float secondsBetweenEvaluations = 0.0f;
	public GameObject characterIndicator;

	[HideInInspector]
	public List<UAI_LinkedAction> linkedActions = new List<UAI_LinkedAction>();
	[HideInInspector]
	public List<string> actionHistory = new List<string>();
	[HideInInspector]
	public float actionTimer = 0.0f;
	[HideInInspector]
	public bool newAction;

	private float secondsSinceLastEvaluation = 0.0f;
	private UAI_Action previousAction, topAction;
	private float currentActionScore;
	private bool isTiming = false;
	private bool paused = false;

	void Start(){
		Evaluate ();
	}

	public void EnableAction(string actionName){
		for (int i = 0; i < linkedActions.Count; i++) {
			if(linkedActions[i].action.name == actionName){
				linkedActions[i].enabled = true;

				if (consoleLogging)
					Debug.Log (agentName + ". Action Enabled: " + actionName);
			}
		}
	}

	public void DisableAction(string actionName){
		for (int i = 0; i < linkedActions.Count; i++) {
			if(linkedActions[i].action.name == actionName)
			{
				linkedActions[i].enabled = false;
				linkedActions[i].action.SetActionScore(0.0f); 

				if (consoleLogging)
					Debug.Log (agentName + ". Action Disabled: " + actionName);
			}
		}
	}

	public void UpdateAI(){
		if (!paused) {
			if (actionTimer > 0.0f && isTiming) {
				actionTimer -= UtilityTime.time;
				GetTopAction ().handle ();
				if (GetTopAction ().interruptible) {
					if(secondsSinceLastEvaluation >= secondsBetweenEvaluations){
						if (EvaluateInteruption ()) {
							actionTimer = GetTopAction ().time;
						}
						secondsSinceLastEvaluation = 0.0f;
					} else {
						secondsSinceLastEvaluation += UtilityTime.time;
					}
				}
			} else if (actionTimer > 0.0f) {
				GetTopAction ().handle ();
			} else {
				StopTimer ();
				Evaluate ();
				actionTimer = GetTopAction ().time;
			}
		}
	}

	public void SetVoidActionDelegate(string name, UAI_Action.Del del)
	{
		for (int i = 0; i < linkedActions.Count; i++) {
			if (linkedActions[i].action.name == name)
			{
				linkedActions[i].action.handle = del;
				return;
			}
		}
	}

	public void StartTimer(){
		isTiming = true;
	}

	public void StopTimer(){
		isTiming = false;
	}

	public void Pause(){
		if (!paused)
			paused = true;
		else
			paused = false;
	}

	public bool IsPaused(){
		return paused;
	}

	public float Evaluate(){

		if(topAction != null)
			previousAction = topAction;

		float topActionScore = 0.0f;

		for (int i = 0; i < linkedActions.Count; i++) {
			if(linkedActions[i].enabled == true){
				linkedActions[i].action.EvaluateAction();
				if(linkedActions[i].action.GetActionScore() > topActionScore)
				{
					topAction = linkedActions[i].action;
					topActionScore = linkedActions[i].action.GetActionScore();
				}	
			}
		}
		if (topAction != previousAction)
			newAction = true;
		else
			StartTimer ();

		if (topAction.interruptible)
			secondsSinceLastEvaluation = 0.0f;

		actionHistory.Add (topAction.name);
		if (actionHistory.Count > historyStates){
			actionHistory.RemoveAt(0);
		}

		if (consoleLogging)
			Debug.Log (agentName + ". New topAction: " + topAction.name + ". With actionScore: " + topActionScore);

		currentActionScore = topActionScore;
		return topActionScore;
	}

	public bool EvaluateInteruption(){

		int topActionPriority = topAction.priorityLevel;
		float topActionScore = 0.0f;
		UAI_Action topInterruption = topAction;
		bool validInterruption = false;
		
		for (int i = 0; i < linkedActions.Count; i++) {
			if(linkedActions[i].enabled == true){
				if(linkedActions[i].action.priorityLevel < topActionPriority){
					linkedActions[i].action.EvaluateAction();
					if(linkedActions[i].action.GetActionScore() > currentActionScore && 
					   linkedActions[i].action.GetActionScore() > topActionScore)
					{
						topInterruption = linkedActions[i].action;
						topActionScore = linkedActions[i].action.GetActionScore();
						validInterruption = true;
					}	
				}
			}
		}

		if (validInterruption) {
			newAction = true;
			topAction = topInterruption;
			actionHistory.Add ("Interruption: " + topAction.name);
			if (actionHistory.Count > historyStates){
				actionHistory.RemoveAt(0);
			}
			currentActionScore = topActionScore;

			if (topAction.interruptible)
				secondsSinceLastEvaluation = 0.0f;

			if (consoleLogging)
				Debug.Log (agentName + ". Interruption: " + topAction.name + ". With actionScore: " + topActionScore);

			return true;
		}
		return false;
	}

	public UAI_Action GetTopAction()
	{
		return topAction;
	}
}