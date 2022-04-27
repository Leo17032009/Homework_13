using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
	START,INPUT,GROWING,NONE
}


public class GameManager : MonoBehaviour 
{
	[SerializeField] private Vector3 _startPos;
	[SerializeField] private Vector2 _minMaxRange, _spawnRange;
	[SerializeField] private GameObject _pillarPrefab, _playerPrefab, _stickPrefab, _diamondPrefab, _currentCamera;
	[SerializeField] private Transform _rotateTransform, _endRotateTransform;
	[SerializeField] private GameObject _scorePanel, _startPanel, _losePanel;
	[SerializeField] private Text _scoreText, _scoreLoseText, _diamondsText, _hightScoreText;
	private GameObject _currentPillar, _nextPillar, _currentStick, _player;
	private int _score, _diamonds, _hightScore;
	private float _cameraOffsetX;
	private GameState _currentState;
	[SerializeField] private float _stickIncreaseSpeed, _maxStickSize;
	public static GameManager instance;

	private void Awake()
	{
		if (instance == null) 
		{
			instance = this;
		} else {
			Destroy (gameObject);
		}
		_currentState = GameState.START;
		_losePanel.SetActive (false);
		_scorePanel.SetActive (false);
		_startPanel.SetActive (true);
		_score = 0;
		_diamonds = PlayerPrefs.HasKey ("Diamonds") ? PlayerPrefs.GetInt ("Diamonds") : 0;
		_hightScore = PlayerPrefs.HasKey ("HighScore") ? PlayerPrefs.GetInt ("HighScore") : 0;
		_scoreText.text = _score.ToString ();
		_diamondsText.text = _diamonds.ToString ();
		_hightScoreText.text = _hightScore.ToString ();

		CreateStartObjects ();
		_cameraOffsetX = _currentCamera.transform.position.x - _player.transform.position.x;
		if (StateManager.instance.hasSceneStarted) 
		{
			GameStart ();
		}
	}

	private void Update()
	{
		if (_currentState == GameState.INPUT) 
		{
			if (Input.GetMouseButton (0)) {
				ScaleStick ();
			} else {
				StartCoroutine (FallStick ());
			}
		}
	}

	public void ScaleStick()
	{
		Vector3 tempScale = _currentStick.transform.localScale;
		tempScale.y += Time.deltaTime * _stickIncreaseSpeed;
		if (tempScale.y > _maxStickSize)
			tempScale.y = _maxStickSize;
		_currentStick.transform.localScale = tempScale;
    }

	IEnumerator FallStick()
	{
		_currentState = GameState.NONE;
		var x = Rotate (_currentStick.transform, _rotateTransform, 0.4f);
		yield return x;
		Vector3 movePosition = _currentStick.transform.position + new Vector3 (_currentStick.transform.localScale.y, 0, 0);
		movePosition.y = _player.transform.position.y;
		x = Move (_player.transform, movePosition, 0.5f);
		yield return x;
		var results = Physics2D.RaycastAll (_player.transform.position, Vector2.down);
		var result = Physics2D.Raycast (_player.transform.position, Vector2.down);
		foreach (var temp in results) 
		{
			if (temp.collider.CompareTag ("Platform")) 
			{
				result = temp;
			}
		}

		if (!result || !result.collider.CompareTag ("Platform")) {
			_player.GetComponent<Rigidbody2D> ().gravityScale = 1f;
			x = Rotate (_currentStick.transform, _endRotateTransform, 0.5f);
			yield return x;
			GameOver ();
		} else {
			UpdateScore ();
			movePosition = _player.transform.position;
			movePosition.x = _nextPillar.transform.position.x + _nextPillar.transform.localScale.x * 0.5f - 0.35f;
			x = Move (_player.transform, movePosition, 0.2f);
			yield return x;

			movePosition = _currentCamera.transform.position;
			movePosition.x = _player.transform.position.x + _cameraOffsetX;
			x = Move (_currentCamera.transform, movePosition, 0.5f);
			yield return x;

			CreatePlatform ();
			SetRandomSize (_nextPillar);
			_currentState = GameState.INPUT;
			Vector3 stickPosition = _currentPillar.transform.position;
			stickPosition.x += _currentPillar.transform.localScale.x * 0.5f - 0.05f;
			stickPosition.y = _currentStick.transform.position.y;
			stickPosition.z = _currentStick.transform.position.z;
			_currentStick = Instantiate(_stickPrefab, stickPosition, Quaternion.identity);
		}
	}

	
	public void CreateStartObjects()
	{
					CreatePlatform();
		Vector3 playerPos = _playerPrefab.transform.position;
		playerPos.x += (_currentPillar.transform.localScale.x * 0.5f - 0.35f);
		_player = Instantiate (_playerPrefab, playerPos, Quaternion.identity);
		_player.name = "Player";

		Vector3 stickPos = _playerPrefab.transform.position;
		stickPos.x += (_currentPillar.transform.localScale.x * 0.5f - 0.05f);
		_currentStick = Instantiate (_stickPrefab, stickPos, Quaternion.identity);
	}

	public void CreatePlatform()
	{
		var currentPlatform = Instantiate (_pillarPrefab);
		_currentPillar = _nextPillar == null ? currentPlatform : _nextPillar;
		_nextPillar = currentPlatform;
		currentPlatform.transform.position = _pillarPrefab.transform.position + _startPos;
		Vector3 tempDistance = new Vector3 (Random.Range (_spawnRange.x, _spawnRange.y) + _currentPillar.transform.localScale.x * 0.5f, 0, 0);
		_startPos += tempDistance;

		if (Random.Range (0, 10) == 0) 
		{
			var tempDiamond = Instantiate (_diamondPrefab);
			Vector3 tempPos = currentPlatform.transform.position;
			tempPos.y = _diamondPrefab.transform.position.y;
			tempDiamond.transform.position = tempPos;
		}
	}

	public void SetRandomSize(GameObject pillar)
	{
		var newScale = pillar.transform.localScale;
		var allowedScale = _nextPillar.transform.position.x - _currentPillar.transform.position.x - _currentPillar.transform.localScale.x * 0.5f - 0.4f;
		newScale.x = Mathf.Max (_minMaxRange.x, Random.Range (_minMaxRange.x, Mathf.Min (allowedScale, _minMaxRange.y)));
		pillar.transform.localScale = newScale;
	}

	public void UpdateScore()
	{
		_score++;
		_scoreText.text = _score.ToString ();
	}

	public void GameOver()
	{
		_losePanel.SetActive (true);
		_scorePanel.SetActive (false);
		if (_score > _hightScore) 
		{
			_hightScore = _score;
			PlayerPrefs.SetInt ("HighScore", _hightScore);
		}

		_scoreLoseText.text = _score.ToString ();
		_hightScoreText.text = _hightScore.ToString ();
	}

	public void UpdateDiamonds()
	{
		_diamonds++;
		PlayerPrefs.SetInt ("Diamonds", _diamonds);
		_diamondsText.text = _diamonds.ToString ();
	}

	public void GameStart()
	{
		StateManager.instance.hasSceneStarted = true;
		UnityEngine.SceneManagement.SceneManager.LoadScene (0);
	}

	IEnumerator Move(Transform currentTransform, Vector3 target, float time)
	{
		var passed = 0f;
		var init = currentTransform.transform.position;
		while (passed < time) 
		{
			passed += Time.deltaTime;
			var normalized = passed / time;
			var current = Vector3.Lerp (init, target, normalized);
			currentTransform.position = current;
			yield return null;
		}
	}

	IEnumerator Rotate(Transform currentTransform, Transform target, float time)
	{
		var passed = 0f;
		var init = currentTransform.transform.rotation;
		while (passed < time) 
		{
			passed += Time.deltaTime;
			var normalized = passed / time;
			var current = Quaternion.Slerp (init, target.rotation, normalized);
			currentTransform.rotation = current;
			yield return null;
		}
	}
}
