using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MidiParser;
using System;
using Unity.VisualScripting;
using Unity.IO;
using System.Data.SqlTypes;
using UnityEditor;
using static UnityEditor.Experimental.GraphView.GraphView;
using TMPro;

public class simulate : MonoBehaviour
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    

    Dictionary<string, dynamic> Config = new Dictionary<string, dynamic>()
    {
        //Beats Per Minute of the song
        {"bpm", 200f},
        
        //Midi Filepath in Assets/Songs directory
        {"songPath", "fireandflames.mid"},
        
        //initial position in world where simulation starts
        {"initialX", 0f},
        {"initialY", 0f},
        
        //The consistency and quality of the generation drops with higher values, Recommended values: (Slow- best gen: 1-4x, Fast- good gen: 5-9x, Very Fast- inconsistent gen: 10-15x, Superspeed- bad gen: 16-24x)
        //(8x is best for speed to generation ratio imo)
        {"simSpeed", 8f},
        
        //physics settings {
            {"XVelocity", 0.05f},
            {"YVelocity", -0.2f},
            {"gravity", 0.0014f},
            {"bounceMulti", 0.9f},
            {"YVelocityClamp", 0.18f},
        
            //the number of times the physics are enacted every second, set higher for x and y movement to increase this many times every second;
            {"physicsFps", 180f},
        // }

        //half of player object scale {
        {"collisionSize", 0.05f},

        {"playbackCameraSize", 6f },
    };


    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



    // (Danger Zone) Don't touch unless you know what you're doing {
    private float TPQ;
    private bool ready = false;
    private float curX;
    private float curY;
    private float globalTime = 0f;
    private float timer = 0f;
    private float frametime;
    private int startPlatformTeleport = 8;
    private GameObject playerStartTeleport;
    public TextMeshProUGUI generationText;
    private int generationsLeft = 0;

    float bpm;
    string songPath;
    float initialX;
    float initialY;
    float simSpeed;
    float simGravity;
    float simXVelocity;
    float simYVelocity;
    float bounceMulti;
    float YVelocityClamp;
    float physicsfps;
    float collisionSize;
    //
    //
        public int interpolationFramesCount = 60; 
        int elapsedFrames = 0;
        GameObject cameraGoalObject;
    //
    //
    // } (Danger Zone) END

    Playerscript playerScript;


    public GameObject platformObject;
    public GameObject playerObject;


    //midi data dictionary definition
    Dictionary<float, List<Dictionary<string, float>>> parsedData = new Dictionary<float, List<Dictionary<string, float>>>()
    {
        { -1f, //time
            new List<Dictionary<string, float>>() { //list of notes in that time

                new Dictionary<string, float>() { //the data of one of the notes in that time
                    { "velocity", 0f }, { "note", 0f }
                },

                new Dictionary<string, float>() { //the data of a different note in that time
                    { "velocity", 0f }, { "note", 0f }
                }

            }
        }
    };

    List<float> timesToActivate = new List<float>() { };
    private int simIndex = 0;


    void Start()
    {
        StartSimulation();
    }

    public void StartSimulation()
    {
        bpm = Config["bpm"]; simGravity = Config["gravity"]; songPath = Config["songPath"]; initialY = Config["initialY"]; initialX = Config["initialX"]; simSpeed = Config["simSpeed"]; simXVelocity = Config["XVelocity"]; simYVelocity = Config["YVelocity"]; bounceMulti = Config["bounceMulti"]; YVelocityClamp = Config["YVelocityClamp"]; physicsfps = Config["physicsFps"]; collisionSize = Config["collisionSize"];
        curX = initialX; curY = initialY;

        frametime = (1000 / physicsfps) / simSpeed;

        playerScript = playerObject.GetComponent<Playerscript>();

        var midiFile = new MidiFile($"Assets/Songs/{songPath}");

        TPQ = midiFile.TicksPerQuarterNote;

        //parsing the midi into dictionary
        foreach (var track in midiFile.Tracks)
        {
            foreach (var midiEvent in track.MidiEvents)
            {
                if (midiEvent.MidiEventType == MidiEventType.NoteOn)
                {
                    var note = midiEvent.Note;
                    var velocity = midiEvent.Velocity;
                    var time = midiEvent.Time;
                    Debug.Log(time);

                    float tempTime = (time / ((bpm * TPQ) / (60)));


                    if (!timesToActivate.Contains(tempTime))
                    {
                        timesToActivate.Add(tempTime);
                        parsedData[tempTime] = new List<Dictionary<string, float>> {
                            new Dictionary<string, float>() { { "velocity", velocity }, { "note", note } }
                        };
                        

                    }
                    else parsedData[tempTime].Add(new Dictionary<string, float>() { { "velocity", velocity }, { "note", note } });
                }
            }
        }
        generationsLeft = parsedData.Count;
        generationText.text = $"0/{generationsLeft}";
        ready = true;
    }



    private void Movement()
    {
        while (timer > 0 && timer >= frametime)
        {
            float timerMulti = Math.Min(timer/frametime, 1);

            curX += (simXVelocity * timerMulti);
            simYVelocity -= (simGravity * timerMulti);

            if (simYVelocity > YVelocityClamp) simYVelocity = YVelocityClamp;
            if (simYVelocity < -YVelocityClamp) simYVelocity = -YVelocityClamp;

            curY += simYVelocity * timerMulti;

            timer -= (frametime * timerMulti);
        }
    }


    private void Update()
    {
        if (elapsedFrames != 0 && ready)
        {
            float interpolationRatio = (float)elapsedFrames / interpolationFramesCount;

            gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, new Vector3(cameraGoalObject.transform.position.x, cameraGoalObject.transform.position.y, -10), interpolationRatio);

            elapsedFrames = (elapsedFrames + 1) % (interpolationFramesCount + 1);
        }

        if (Input.GetKey(KeyCode.Space)) { simulationFinished(); }

        if (ready)
        {
            ready = false;

            globalTime += Time.deltaTime * simSpeed;
            timer += (Time.deltaTime * 1000);

            Movement();

            if (globalTime >= timesToActivate[simIndex])
            {
                Simulation(simIndex);
                simIndex += 1;
            } else
            {
                ready = true;
            }
            
        }
    }

    void Simulation(int index)
    {
        ready = false;
        

        float activateTime = timesToActivate[index];

        float Intensity = 0.3f;

        var dict = parsedData[activateTime];

        foreach (var note in dict)
        {
            Intensity *= 1.75f;
        }

        float ySet;

        if (simYVelocity <= 0) { 
            ySet = (curY - collisionSize - 0.04f); 
        } else { 
            ySet = (curY + collisionSize + 0.04f); 
        };

        var platform = Instantiate(platformObject, new Vector2(curX+0.015f, ySet), Quaternion.identity);

        float calculation = (-simYVelocity) * (bounceMulti + ((1 - bounceMulti) * Intensity));

        if (Math.Abs(calculation) < 0.05f)
        {
            if (calculation < 0)
            {
                calculation = -0.05f;
            } else
            {
                calculation = 0.05f;
            }
        }

        

        simYVelocity = calculation;

        if (index == startPlatformTeleport) { playerStartTeleport = platform; };

        var pScript = platform.GetComponent<PlatformScript>();

        pScript.savedCalc = calculation;
        pScript.notes = dict;
        pScript.xTeleport = curX;

        //camera follow set
        if (elapsedFrames == 0) { cameraGoalObject = platform; elapsedFrames += 1; }

        generationText.text = $"{index}/{generationsLeft}";
        ready = true;
        if (index + 1 >= timesToActivate.Count) simulationFinished();
    }


    void simulationFinished()
    {
        if (ready)
        {
            Destroy(generationText); 
            ready = false;

            playerScript.Run(playerStartTeleport, Config);
        }
    }
}
