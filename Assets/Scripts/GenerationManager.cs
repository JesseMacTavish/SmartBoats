using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;


public class GenerationManager : MonoBehaviour
{
    [Header("Generators")]
    //Default generators
    [SerializeField]
    private GenerateObjectsInArea[] boxGenerators;
    [SerializeField]
    private GenerateObjectsInArea boatGenerator;
    [SerializeField]
    private GenerateObjectsInArea pirateGenerator;
    
    //Abled generators
    [SerializeField]
    private GenerateObjectsInArea abledBoatGenerator;
    [SerializeField]
    private GenerateObjectsInArea abledPirateGenerator;
    [SerializeField]
    private GenerateObjectsInArea powerupsGenerators;

    [Space(10)]
    [Header("Parenting and Mutation")]
    [SerializeField]
    private float mutationFactor;
    [SerializeField]
    private float mutationChance;
    [SerializeField]
    private int boatParentSize;
    [SerializeField]
    private int pirateParentSize;

    [Space(10)]
    [Header("Simulation Controls")]
    [SerializeField, Tooltip("Time per simulation (in seconds).")]
    private float simulationTimer;
    [SerializeField, Tooltip("Current time spent on this simulation.")]
    private float simulationCount;
    [SerializeField, Tooltip("Automatically starts the simulation on Play.")]
    private bool runOnStart;
    [SerializeField, Tooltip("Initial count for the simulation. Used for the Prefabs naming.")]
    private int generationCount;

    [Space(10)]
    [Header("Prefab Saving")]
    [SerializeField]
    private string savePrefabsAt;

    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")]
    //Default winners
    [SerializeField]
    private AgentData lastBoatWinnerData;
    [SerializeField]
    private AgentData lastPirateWinnerData;

    //Abled winners
    [SerializeField]
    private AgentData lastAbledBoatWinnerData;
    [SerializeField]
    private AgentData lastAbledPirateWinnerData;

    //Default agents
    private bool _runningSimulation;
    private List<BoatLogic> _activeBoats;
    private List<PirateLogic> _activePirates;
    private BoatLogic[] _boatParents;
    private PirateLogic[] _pirateParents;

    //Abled agents
    private List<BoatLogic> _activeAbledBoats;
    private List<PirateLogic> _activeAbledPirates;
    private BoatLogic[] _abledBoatParents;
    private PirateLogic[] _abledPirateParents;

    private void Start()
    {
        if (runOnStart)
        {
            StartSimulation();
        }
    }

    private void Update()
    {
        if (_runningSimulation)
        {
            //Creates a new generation.
            if (simulationCount >= simulationTimer)
            {
                ++generationCount;
                MakeNewGeneration();
                simulationCount = -Time.deltaTime;
            }
            simulationCount += Time.deltaTime;
        }
    }


    /// <summary>
    /// Generates the boxes on all box areas.
    /// </summary>
    public void GenerateBoxes()
    {
        foreach (GenerateObjectsInArea generateObjectsInArea in boxGenerators)
        {
            generateObjectsInArea.RegenerateObjects();
        }
    }

    public void GeneratePowerups()
    {
        powerupsGenerators.RegenerateObjects();
    }

    /// <summary>
    /// Generates boats and pirates using the parents list.
    /// If no parents are used, then they are ignored and the boats/pirates are generated using the default prefab
    /// specified in their areas.
    /// </summary>
    /// <param name="boatParents"></param>
    /// <param name="pirateParents"></param>
    public void GenerateObjects(BoatLogic[] boatParents = null, BoatLogic[] abledBoatParents = null, PirateLogic[] pirateParents = null, PirateLogic[] abledPirateParents = null)
    {
        GenerateBoats(boatParents);
        GeneratePirates(pirateParents);

        //Abled
        GenerateAbledBoats(abledBoatParents);
        GenerateAbledPirates(abledPirateParents);
    }

    /// <summary>
    /// Generates the list of pirates using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created pirates will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="pirateParents"></param>
    private void GeneratePirates(PirateLogic[] pirateParents)
    {
        _activePirates = new List<PirateLogic>();
        List<GameObject> objects = pirateGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            PirateLogic pirate = obj.GetComponent<PirateLogic>();
            if (pirate != null)
            {
                _activePirates.Add(pirate);
                if (pirateParents != null)
                {
                    PirateLogic pirateParent = pirateParents[Random.Range(0, pirateParents.Length)];
                    pirate.Birth(pirateParent.GetData());
                }

                pirate.Mutate(mutationFactor, mutationChance);
                pirate.AwakeUp();
            }
        }
    }

    //Similarly generate abled pirates 
    private void GenerateAbledPirates(PirateLogic[] abledPirateParents)
    {
        _activeAbledPirates = new List<PirateLogic>();
        List<GameObject> objects = abledPirateGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            PirateLogic pirate = obj.GetComponent<PirateLogic>();
            if (pirate != null)
            {
                _activeAbledPirates.Add(pirate);
                if (abledPirateParents != null)
                {
                    PirateLogic pirateParent = abledPirateParents[Random.Range(0, abledPirateParents.Length)];
                    pirate.Birth(pirateParent.GetData());
                }

                pirate.Mutate(mutationFactor, mutationChance);
                pirate.AwakeUp();
            }
        }
    }

    /// <summary>
    /// Generates the list of boats using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created boats will go under mutation (MutationChances and MutationFactor will be applied).
    /// /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="boatParents"></param>
    private void GenerateBoats(BoatLogic[] boatParents)
    {
        _activeBoats = new List<BoatLogic>();
        List<GameObject> objects = boatGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            BoatLogic boat = obj.GetComponent<BoatLogic>();
            if (boat != null)
            {
                _activeBoats.Add(boat);
                if (boatParents != null)
                {
                    BoatLogic boatParent = boatParents[Random.Range(0, boatParents.Length)];
                    boat.Birth(boatParent.GetData());
                }

                boat.Mutate(mutationFactor, mutationChance);
                boat.AwakeUp();
            }
        }
    }

    //Similarly generate abled boats
    private void GenerateAbledBoats(BoatLogic[] abledBoatParents)
    {
        _activeAbledBoats = new List<BoatLogic>();
        List<GameObject> objects = abledBoatGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            BoatLogic boat = obj.GetComponent<BoatLogic>();
            if (boat != null)
            {
                _activeAbledBoats.Add(boat);
                if (abledBoatParents != null)
                {
                    BoatLogic boatParent = abledBoatParents[Random.Range(0, abledBoatParents.Length)];
                    boat.Birth(boatParent.GetData());
                }

                boat.Mutate(mutationFactor, mutationChance);
                boat.AwakeUp();
            }
        }
    }

    /// <summary>
    /// Creates a new generation by using GenerateBoxes and GenerateBoats/Pirates.
    /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
    /// The best parents (top 1) of the generation will be stored as a Prefab in the [savePrefabsAt] folder. Their name
    /// will use the [generationCount] as an identifier.
    /// </summary>
    public void MakeNewGeneration()
    {
        GenerateBoxes();
        GeneratePowerups();

        //Fetch parents
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.Sort();
        _activeAbledBoats.RemoveAll(item => item == null);
        _activeAbledBoats.Sort();

        if (_activeBoats.Count == 0)
        {
            GenerateBoats(_boatParents);
        }
        if (_activeAbledBoats.Count == 0)
        {
            GenerateAbledBoats(_abledBoatParents);
        }

        _boatParents = new BoatLogic[boatParentSize];
        _abledBoatParents = new BoatLogic[boatParentSize];

        for (int i = 0; i < boatParentSize; i++)
        {
            _boatParents[i] = _activeBoats[i];
            _abledBoatParents[i] = _activeAbledBoats[i];
        }

        BoatLogic lastBoatWinner = _activeBoats[0];
        BoatLogic lastAbledBoatWinner = _activeAbledBoats[0];
        lastBoatWinner.name += "Gen-" + generationCount;
        lastAbledBoatWinner.name += "Gen-" + generationCount;
        lastBoatWinnerData = lastBoatWinner.GetData();
        lastAbledBoatWinnerData = lastAbledBoatWinner.GetData();
        PrefabUtility.SaveAsPrefabAsset(lastBoatWinner.gameObject, savePrefabsAt + lastBoatWinner.name + ".prefab");
        PrefabUtility.SaveAsPrefabAsset(lastAbledBoatWinner.gameObject, savePrefabsAt + lastAbledBoatWinner.name + ".prefab");

        _activePirates.RemoveAll(item => item == null);
        _activePirates.Sort();
        _activeAbledPirates.RemoveAll(item => item == null);
        _activeAbledPirates.Sort();
        _pirateParents = new PirateLogic[pirateParentSize];
        _abledPirateParents = new PirateLogic[pirateParentSize];
        for (int i = 0; i < pirateParentSize; i++)
        {
            _pirateParents[i] = _activePirates[i];
            _abledPirateParents[i] = _activeAbledPirates[i];
        }

        PirateLogic lastPirateWinner = _activePirates[0];
        PirateLogic lastAbledPirateWinner = _activeAbledPirates[0];
        lastPirateWinner.name += "Gen-" + generationCount;
        lastAbledPirateWinner.name += "Gen-" + generationCount;
        lastPirateWinnerData = lastPirateWinner.GetData();
        lastAbledPirateWinnerData = lastAbledPirateWinner.GetData();
        PrefabUtility.SaveAsPrefabAsset(lastPirateWinner.gameObject, savePrefabsAt + lastPirateWinner.name + ".prefab");
        PrefabUtility.SaveAsPrefabAsset(lastAbledPirateWinner.gameObject, savePrefabsAt + lastAbledPirateWinner.name + ".prefab");

        //Winners:
        Debug.Log("Last winner boat had: " + lastBoatWinner.GetPoints() + " points!" + " Last winner pirate had: " + lastPirateWinner.GetPoints() + " points!");
        Debug.Log("Last winner ABLED boat had: " + lastAbledBoatWinner.GetPoints() + " points!" + " Last winner ABLED pirate had: " + lastAbledPirateWinner.GetPoints() + " points!");
        

        GenerateObjects(_boatParents, _abledBoatParents, _pirateParents, _abledPirateParents);
    }

    /// <summary>
    /// Starts a new simulation. It does not call MakeNewGeneration. It calls both GenerateBoxes and GenerateObjects and
    /// then sets the _runningSimulation flag to true.
    /// </summary>
    public void StartSimulation()
    {
        GenerateBoxes();
        GeneratePowerups();
        GenerateObjects();
        _runningSimulation = true;
    }

    /// <summary>
    /// Continues the simulation. It calls MakeNewGeneration to use the previous state of the simulation and continue it.
    /// It sets the _runningSimulation flag to true.
    /// </summary>
    public void ContinueSimulation()
    {
        MakeNewGeneration();
        _runningSimulation = true;
    }

    /// <summary>
    /// Stops the count for the simulation. It also removes null (Destroyed) boats from the _activeBoats list and sets
    /// all boats and pirates to Sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.ForEach(boat => boat.Sleep());
        _activePirates.ForEach(pirate => pirate.Sleep());
    }
}
