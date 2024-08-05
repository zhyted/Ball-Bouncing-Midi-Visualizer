using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Playerscript : MonoBehaviour
{
    Dictionary<string, dynamic> Config;

    //DO NOT SET THESE YOURSELF, CHANGE IN simulate.cs {
        float XVelocity;
        float YVelocity = -0.2f;
        float Gravity;
        float YVelocityClamp;
    // }

    public float playerSize = 0.05f;

    private float timer = 0f;
    private float physicsfps;
    private float frametime;

    public bool ready = false;
    public AudioClip pianoClip;
    public AudioClip elecClip;
    public AudioClip bassClip;
    public AudioClip drumClip;

    private GameObject lastHitPlatform;



    public Camera camerax;

    private void Movement()
    {
        while (timer > 0 && timer >= frametime)
        {
            float timerMulti = Math.Min(timer / frametime, 1);

            YVelocity -= (Gravity * timerMulti);

            if (YVelocity > YVelocityClamp) YVelocity = YVelocityClamp;
            if (YVelocity < -YVelocityClamp) YVelocity = -YVelocityClamp;

            var prevpos = gameObject.transform.position;

            gameObject.transform.position = new Vector2(prevpos.x + (XVelocity * timerMulti), prevpos.y + (YVelocity * timerMulti));

            timer -= (frametime * timerMulti);
        }
    }


    private void Update()
    {
        if (ready)
        {
            ready = false;
            timer += (Time.deltaTime * 1000);

            Movement();
            ready = true;
        }
        if (Input.GetKeyDown(KeyCode.R) && lastHitPlatform) { YVelocity = 0f; gameObject.transform.position = lastHitPlatform.transform.position; };
    }

    private void Start()
    {

    }

    IEnumerator ObjectDestroyer(UnityEngine.Object a, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(a);
    }

    void AudioHandler(List<Dictionary<string, float>> NoteDict)
    {
        var length = Math.Min(NoteDict.Count, 8);

        for (int i=0; i<length; i++)
        {
            AudioSource audio = gameObject.AddComponent<AudioSource>();
            float pitch = NoteDict[i]["note"] - 60;

            if (pitch >= 0)
            {
                audio.clip = pianoClip;

            } else if (pitch <= -1)
            {
                audio.clip = drumClip;
                audio.volume = 0.2f;
                pitch = (float)Math.Round(0.5 * pitch);
            }

            audio.pitch = 1 * (float)Math.Pow(1.05946, pitch);

            audio.Play();
            StartCoroutine(ObjectDestroyer(audio, 1.3f));
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ready)
        {
            var pScript = collision.GetComponent<PlatformScript>();
            ready = false;
            if (collision.gameObject.tag == "platform" && !pScript.touched)
            {
                var notesDict = pScript.notes;
                var cLoc = collision.transform.position;
                var cSize = collision.transform.localScale;

                cSize.y += playerSize;

                AudioHandler(notesDict);

                

                float calculation = pScript.savedCalc;

                if (calculation < 0) cSize.y *= -1;

                var cTeleport = new Vector2(pScript.xTeleport, cLoc.y + (cSize.y / 2));

                

                gameObject.transform.position = cTeleport;


                YVelocity = calculation;
                if (lastHitPlatform != collision.gameObject) StartCoroutine(ObjectDestroyer(lastHitPlatform, 1.25f)); lastHitPlatform?.GetComponent<PlatformScript>().setTouched();
                lastHitPlatform = collision.gameObject;
            }
            ready = true;
        }
        
    }

    public void Run(GameObject teleport, Dictionary<string, dynamic> _Config)
    {
        ready = false;
        XVelocity = _Config["XVelocity"]; YVelocity = _Config["YVelocity"]; Gravity = _Config["gravity"]; YVelocityClamp = _Config["YVelocityClamp"]; physicsfps = _Config["physicsFps"]; Config = _Config;

        camerax.orthographicSize = Config["playbackCameraSize"];

        frametime = 1000 / physicsfps;

        var playerLocation = (Vector2)gameObject.transform.position + new Vector2(0, 0.1f);
        camerax.transform.position = new Vector3(playerLocation.x+1.5f, playerLocation.y-1, -10);

        camerax.GetComponent<CameraScript>().PlayerFollowStart(gameObject);
        
        gameObject.transform.position = teleport.transform.position;
        ready = true;
    }
}
