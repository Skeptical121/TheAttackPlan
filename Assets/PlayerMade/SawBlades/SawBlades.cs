using UnityEngine;
using System.Collections;

public class SawBlades : Trap {

	GameObject[] sawBlades;
	GameObject[] sawBladeParents;
	float[] initialPositions;
	float[] yHeight;
	float[] sawEulerY;

	float sawSpeed = 1.5f; // Units per second.

	public override short getMaxHealth () {
		return 650;
	}

	public override float getLifeTime () {
		return 60f;
	}

	// Use this for initialization
	public override void InitStart(bool isThisMine)
	{
		base.InitStart (isThisMine);
		// Find the saws. (This is so the saws can be edited freely)
		Transform iterator = transform;
		int numSaws = 0;
		for (int i = 0; i < iterator.childCount; i++)
		{
			Transform t = iterator.GetChild(i);
			if (t.CompareTag("ShieldParent"))
			{
				numSaws++;
			}
		}
		sawBlades = new GameObject[numSaws / 2];
		sawBladeParents = new GameObject[numSaws / 2];
		initialPositions = new float[numSaws / 2];
		sawEulerY = new float[numSaws / 2];
		yHeight = new float[numSaws / 2];
		for (int i = 0; i < iterator.childCount; i++)
		{
			Transform t = iterator.GetChild(i);
			if (t.CompareTag("ShieldParent"))
			{
				int index = int.Parse(t.name.Substring(t.name.Length - 1));
				if (t.name.Contains("Line"))
				{
					sawBladeParents[index] = t.gameObject;
				} else {
					sawBlades[index] = t.gameObject; //t.Find("Saw").gameObject;
					sawEulerY[index] = sawBlades[index].transform.localEulerAngles.y;
				}
				if (sawBladeParents[index] && sawBlades[index])
				{


					float inverseSawRatio = sawBladeParents[index].transform.lossyScale.z / (sawBladeParents[index].transform.lossyScale.z - sawBlades[index].transform.lossyScale.z * 0.8f);
					
					
					initialPositions[index] = (sawBladeParents[index].transform.InverseTransformPoint(sawBlades[index].transform.position).z * inverseSawRatio + 0.5f);

					yHeight[index] = sawBlades[index].transform.position.y - sawBladeParents[index].transform.position.y;
				}
			}
		}
			
		// No prediction
	}

	public override int getBitChoicesLengthThis(bool isPlayerOwner) {
		return base.getBitChoicesLengthThis (isPlayerOwner) + 0;
	}

	public override object getObjectThis(int num, bool isPlayerOwner) {
		int childNum = num - base.getBitChoicesLengthThis (isPlayerOwner);

		switch (childNum)
		{
		default: return base.getObjectThis(num, isPlayerOwner);
		}
	}

	public override int SetInformation(object[] data, object[] a, object[] b, bool isThisFirstTick, bool isThisMine) {
		int previousTotal = base.SetInformation (data, a, b, isThisFirstTick, isThisMine);

		UpdateSaws(getLifeTimeInterp());

		return previousTotal + 0;
	}

	public override void ServerSyncFixedUpdate() {
		base.ServerSyncFixedUpdate ();
		UpdateSaws(getLifeTimeServer());
	}

	public void UpdateSaws(float netTime)
	{
		for (int i = 0; i < sawBlades.Length; i++) {
			float sawLength = sawBladeParents[i].transform.lossyScale.z - sawBlades[i].transform.lossyScale.z * 0.8f;

			float sawRatio = sawLength / sawBladeParents[i].transform.lossyScale.z;
			

			float destination = (initialPositions[i] + netTime * sawSpeed / (sawLength * 2)) % (2);
			destination *= sawRatio;
			
			if (destination > sawRatio)
			{
				destination = sawRatio * 2 - destination;
			}
			destination -= sawRatio / 2;

			sawBlades[i].transform.position = sawBladeParents[i].transform.TransformPoint(new Vector3(0, yHeight[i], destination));

			sawBlades[i].transform.localEulerAngles = new Vector3(Time.time * 360f, sawEulerY[i], 0); // Rotation is fairly arbitrary for the saws.

		}
	}
}
