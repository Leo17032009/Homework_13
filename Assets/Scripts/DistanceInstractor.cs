using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceInstractor : MonoBehaviour 
{
	private GameObject _player;

	public void Start()
	{
		_player = GameObject.Find ("Player");
	}

	public void Update()
	{
		if (!_player) 
		{
			_player = GameObject.Find ("Player");
			return;
		}
		if (_player.transform.position.x - transform.position.x > 15f) 
		{
			Destroy (gameObject);
		}
	}
}
