﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerGameManager : MonoBehaviour {

	//variable
	public int grenadeStore = 5;
	public float health = 100;
	public float reloadAlertRate = 3.0f;
	public Text debugText;
	[HideInInspector] public int team = -1;

	//gun seeting
	private GameObject gun;
	private GunProperties gunProperties;
	private GameObject gunLight;
	private bool isShowGunEffect = false;

	public GameObject grenade;
	public float grenadeThrowForce = 10f;
	private GrenadeThrow grenadeProperties;
	private EllipsoidParticleEmitter gunFlashEmitter;

	//reload system
	private float reloadTimer = 0.0f;
	private float reloadAlertTimer = 0.0f;
	private bool isAlertReload = false;
	private bool isReloading = false;
	[HideInInspector] public bool isWalking = false;

	//fire system
	public GameObject bulletHole;
	public int bulletHoleMaxAmount;
	private ArrayList bulletHoleArray;
	private float fireTimer = 0.0f;
	private GameObject cardboardCamera;
	private CardboardHead cardboardHead;
	private Transform headPos;

	public bool forceAim;

	private Animator anim;
	[HideInInspector]
	public bool isInAimMode = false;
	private AudioSource footstepsAudio;

	//UI component
	private Transform healthBar;
	private TextMesh bulletText;
	private TextMesh reloadText;
	private TextMesh grenadeText;
	private GameObject HUD;

	//sound effect
	AudioSource[] gunAudio; 


	// Use this for initialization
	void Start () {
		ConsoleLog.SLog ("PlayerGameManager Start()");

		anim = GetComponent<Animator> ();
		cardboardCamera = GameObject.FindGameObjectWithTag("PlayerHead");
		cardboardHead = cardboardCamera.GetComponent<CardboardHead> ();
		headPos = GameObject.FindGameObjectWithTag ("CameraPos").transform;
		gun = GameObject.FindGameObjectWithTag ("MyGun");
		gunProperties = gun.GetComponent<GunProperties> ();
		gunAudio = gun.GetComponents<AudioSource> ();
		gunLight = GameObject.FindGameObjectWithTag ("GunLight");
		gunFlashEmitter = GameObject.FindGameObjectWithTag ("GunFlash").GetComponent<EllipsoidParticleEmitter>();
		gunFlashEmitter.emit = false;
		footstepsAudio = GetComponent<AudioSource> ();
		bulletHoleArray = new ArrayList (bulletHoleMaxAmount);

		//HUD
		HUD = GameObject.FindGameObjectWithTag("HUD");
		healthBar = HUD.transform.GetChild (0) as Transform;
		bulletText = HUD.transform.GetChild (1).GetComponent<TextMesh>();
		reloadText = HUD.transform.GetChild (2).GetComponent<TextMesh>();
		grenadeText = HUD.transform.GetChild (3).GetComponent<TextMesh>();
		bulletText.text = gunProperties.bulletLoadCurrent + "/" + gunProperties.bulletStoreCurrent;
		grenadeText.text = grenadeStore + "";
	}

	public void CheckNullComponents(){
		
		if (HUD == null) {ConsoleLog.SLog ("HUD null");}
		if (healthBar == null) {ConsoleLog.SLog ("healthBar null");}
		if (bulletText == null) {ConsoleLog.SLog ("bulletText null");}
		if (reloadText == null) {ConsoleLog.SLog ("reloadText null");}
		if (grenadeText == null) {ConsoleLog.SLog ("grenadeText null");}

		if (anim == null) {ConsoleLog.SLog ("anim null");}
		if (cardboardCamera == null) {ConsoleLog.SLog ("cardboardCamera null");}
		if (cardboardHead == null) {ConsoleLog.SLog ("cardboardHead null");}
		if (headPos == null) {ConsoleLog.SLog ("headPos null");}
		if (gun == null) {ConsoleLog.SLog ("gun null");}
		if (gunAudio == null) {ConsoleLog.SLog ("gunAudio null");}
		if (gunLight == null) {ConsoleLog.SLog ("gunLight null");}
		if (gunFlashEmitter == null) {ConsoleLog.SLog ("gunFlashEmitter null");}
		if (footstepsAudio == null) {ConsoleLog.SLog ("footstepsAudio null");}

		if(
			HUD == null ||
			healthBar == null ||
			bulletText == null ||
			reloadText == null ||
			grenadeText == null ||

			anim == null ||
			cardboardCamera == null ||
			cardboardHead == null ||
			headPos == null ||
			gun == null ||
			gunAudio == null ||
			gunLight == null ||
			gunFlashEmitter == null ||
			footstepsAudio == null
		){
			Start ();
		}
	}

	void FixedUpdate () {
		CheckNullComponents ();
	}

	// Update is called once per frame
	void Update () {
		fireTimer += Time.deltaTime;

		if (isShowGunEffect) {showGunEffect (false);}
		if (isAlertReload) { alertReload ();}
		if (isReloading) {reloadWithDelay ();}

		detectInput ();
	}

	public void detectInput(){
		//for keyboard

		//walking
		if (Input.GetButton ("Horizontal") || Input.GetButton ("Vertical")) {
			if (!isWalking) {
				footstepsAudio.Play ();
				isWalking = true;
			}
		} else if (!Input.GetButton ("Horizontal") && !Input.GetButton ("Vertical")) {
			footstepsAudio.Stop();
			isWalking = false;
		}

		//fire
		if (gunProperties.isAutomatic) {
			if (Input.GetButton("Fire1") || 
				Input.GetKey(KeyCode.Period) || 
				Input.GetKey(KeyCode.JoystickButton7)) {
					fireGun ();
			}

		} else {
			if (Input.GetButtonDown("Fire1") || 
				Input.GetKeyDown(KeyCode.Period) || 
				Input.GetKey(KeyCode.JoystickButton7)) {
					fireGun ();
			}
		}

		//reload
		if (Input.GetKeyDown (KeyCode.R) ||
			Input.GetKeyDown(KeyCode.JoystickButton2)) {

			isReloading = true;
			isAlertReload = false;
			reloadAlertTimer = 0f;
			reloadText.text = "RELOADING";
			AudioSource.PlayClipAtPoint (gunAudio [1].clip,gun.transform.position);
			//TODO play reload animation
		}

		//aim mode
		if ((Input.GetKey (KeyCode.Slash) ||
			Input.GetKey(KeyCode.JoystickButton4) || 
			Input.GetKey(KeyCode.JoystickButton5)) && !isInAimMode) {
			isInAimMode = true;
		} 
		if ((Input.GetKeyUp (KeyCode.Slash) || 
			Input.GetKeyUp(KeyCode.JoystickButton4) ||
			Input.GetKeyUp(KeyCode.JoystickButton5) && isInAimMode )) {
			isInAimMode = false;
		}
		if(forceAim) isInAimMode = true; // for debugging
		anim.SetBool ("Aim", isInAimMode);


		//throw grenade
		if(Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.JoystickButton3)){
			throwGrenade ();
		}
	}

	public void throwGrenade(){
		if (grenadeStore <= 0) { return; }

		//Update UI Text
		grenadeStore--;
		grenadeText.text = grenadeStore + "";

		//Instantiate and add Add Force
		GameObject grenadeClone = (GameObject) Instantiate(
			grenade, 
			headPos.position + cardboardCamera.transform.forward * 1f, 
			cardboardCamera.transform.rotation
		);
		grenadeClone.GetComponent<Rigidbody> ().AddForce (
			cardboardCamera.transform.forward * grenadeThrowForce, 
			ForceMode.Impulse
		);

		//Sync data with other player
		MultiplayerController.instance.SendHandGrenade (
			headPos.position + cardboardCamera.transform.forward * 1f,
			cardboardCamera.transform.rotation.eulerAngles,
			cardboardCamera.transform.forward * grenadeThrowForce
		);
	}

	public void fireGunNTimes(int times) {
		//random shoot ray to simulate gun inaccuracy
		float accuracy;
		if (isWalking && !isInAimMode) {
			accuracy = gun.GetComponent<GunProperties> ().walkingAccuracy;
		} else if (isWalking && isInAimMode) {
			accuracy = gun.GetComponent<GunProperties> ().walkingAimAccuracy;
		} else if (!isWalking && !isInAimMode) {
			accuracy = gun.GetComponent<GunProperties> ().accuracy;
		} else {
			accuracy = gun.GetComponent<GunProperties> ().aimAccuracy;
		}

		for (int i = 0; i < times; i++) {
			Vector2 randomXY = Random.insideUnitCircle * accuracy;
			Vector3 direction = cardboardHead.transform.forward;
			direction.x += randomXY.x;
			direction.y += randomXY.y;

			Ray randomRay = new Ray (headPos.position, direction);
			RaycastHit hit;

			if (Physics.Raycast (randomRay, out hit, gunProperties.gunRange)) {
				Debug.DrawRay (randomRay.origin, cardboardHead.transform.forward, Color.green, 10f); //actual sight
				if(!isInAimMode) Debug.DrawLine (randomRay.origin, hit.point,Color.blue,10f); //random gun ray (not aim)
				else Debug.DrawLine (randomRay.origin, hit.point,Color.cyan,10f); //random gun ray (aim)

				if (hit.transform.GetComponent<Hit> () != null) {
					hit.transform.GetComponent<Hit> ().Hited ();
				}

				if (hit.transform.GetComponent<MilitaryBarrel> () != null) {
					hit.transform.GetComponent<MilitaryBarrel> ().Hited ();
				}

				if (hit.transform.GetComponent<OilBarrel> () != null) {
					hit.transform.GetComponent<OilBarrel> ().Hited ();
				}

				if (hit.transform.GetComponent<SlimeBarrel> () != null) {
					hit.transform.GetComponent<SlimeBarrel> ().Hited ();
				}

				//hit remote player
				if(hit.transform.GetComponent<RemoteCharacterController>() != null) {
					RemoteCharacterController remoteController = hit.transform.GetComponent<RemoteCharacterController> ();
					ConsoleLog.SLog ("hit remote player " + remoteController.playerNum);
					remoteController.TakeGunDamage (gunProperties.firePower);
					return; //to ignore move object and bullet hole
				}

				//hit moveable object
				if (hit.rigidbody != null) {
					hit.rigidbody.AddForceAtPosition (
						cardboardCamera.transform.forward * gunProperties.firePower, 
						hit.point, 
						ForceMode.Impulse
					);
				}

				//bullet hole effect
				if (bulletHoleArray.Count >= bulletHoleMaxAmount) {
					Destroy ((GameObject)bulletHoleArray [0]);
					bulletHoleArray.RemoveAt (0);
				}

				GameObject tempBulletHole = (GameObject)Instantiate (bulletHole, hit.point, Quaternion.identity);
				tempBulletHole.transform.rotation = Quaternion.FromToRotation (tempBulletHole.transform.forward, hit.normal) * tempBulletHole.transform.rotation;
				bulletHoleArray.Add (tempBulletHole);
				tempBulletHole.transform.parent = hit.transform;

				//Send fire ray to everyone in the room
				//to interact with their object in their scene
				MultiplayerController.instance.SendFireRay (randomRay);
			}
		}
	}

	public void fireGun(){
		//TODO fire animation
		if (fireTimer < gunProperties.rateOfFire) { //has just fire, cooldown
			return;
		} if (isReloading) { //not finish reload, can't fire
			return;
		} if (gunProperties.bulletLoadCurrent <= 0) { //out of bullet, alert to reload
			isAlertReload = true;
		} else { //bullet left, fire!
			AudioSource.PlayClipAtPoint (gunAudio [0].clip, gun.transform.position);
			showGunEffect (true);
			gunFlashEmitter.Emit ();
			fireTimer = 0f;
			gunProperties.bulletLoadCurrent--;
			if (gunProperties.gunType != 3) {
				fireGunNTimes (1);
			} else {
				fireGunNTimes (5);
			}
			bulletText.text = gunProperties.bulletLoadCurrent + "/" + gunProperties.bulletStoreCurrent;
			if (gunProperties.bulletLoadCurrent == 0) { isAlertReload = true;}
		}
	}

	void showGunEffect(bool isTurnOn){
		if (isTurnOn) {
			isShowGunEffect = true;
			gunLight.GetComponent<Light> ().enabled = true;
		} else {
			isShowGunEffect = false;
			gunLight.GetComponent<Light> ().enabled = false;
		}
	}

	public void reloadWithDelay(){
		reloadTimer += Time.deltaTime;

		if (reloadTimer > gunProperties.reloadTime) {
			reloadTimer = 0.0f;
			isReloading = false;
			reloadText.text = "";
			reloadGun ();
		}
	}

	public void reloadGun() {
		if (gunProperties.bulletLoadCurrent == gunProperties.bulletLoadMax) {
			return;
		} else if (gunProperties.bulletStoreCurrent >= gunProperties.bulletLoadMax - gunProperties.bulletLoadCurrent) {
			//planty of bullet left

			gunProperties.bulletStoreCurrent -= (gunProperties.bulletLoadMax - gunProperties.bulletLoadCurrent);
			gunProperties.bulletLoadCurrent = gunProperties.bulletLoadMax;
			bulletText.text = gunProperties.bulletLoadCurrent + "/" + gunProperties.bulletStoreCurrent;
			isAlertReload = false;

			//TODO reload animation

		} else if (gunProperties.bulletStoreCurrent > 0) {
			//some bullet left, but not full mag

			gunProperties.bulletLoadCurrent = gunProperties.bulletStoreCurrent;
			gunProperties.bulletStoreCurrent = 0;
			bulletText.text = gunProperties.bulletLoadCurrent + "/" + gunProperties.bulletStoreCurrent;
			isAlertReload = false;

			//TODO reload animation

		} else {
			//no more bullet
			//TODO display alert
		}
	}

	public void alertReload (){
		reloadAlertTimer += Time.deltaTime;
		if (reloadAlertTimer > reloadAlertRate/2) {
			reloadText.text = "RELOAD";
		}
		if (reloadAlertTimer > reloadAlertRate) {
			reloadAlertTimer = 0f;
			reloadText.text = "";
		}
	}
		
	public void takeDamage(float damage){
		health -= damage;
		if (health < 0f) { health = 0;}

		Vector3 healthBarScale = healthBar.transform.localScale;
		healthBarScale = new Vector3(1f, health / 100f, 1f);
		healthBar.transform.localScale = healthBarScale;
	}

	public void addStoreBullet(int bulletCount){
		gunProperties.bulletStoreCurrent += bulletCount;
		bulletText.text = gunProperties.bulletLoadCurrent + "/" + gunProperties.bulletStoreCurrent;
	}

	public void addStoreGrenade(int grenadeCount){
		grenadeStore += grenadeCount;
		grenadeText.text = grenadeStore + "";
	}

	public void addHealth(float heal){
		health += heal;
		if (health > 100f) { health = 100f;}

		Vector3 healthBarScale = healthBar.transform.localScale;
		healthBarScale = new Vector3(1f, health / 100f, 1f);
		healthBar.transform.localScale = healthBarScale;
	}
}