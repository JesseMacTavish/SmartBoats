﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// This struct helps to order the directions an Agent can take based on its utility.
/// Every Direction (a vector to where the Agent would move) has a utility value.
/// Higher utility values are expected to lead to better outcomes.
/// </summary>
struct AgentDirection : IComparable
{
    public Vector3 Direction { get; }
    public float utility;

    public AgentDirection(Vector3 direction, float utility)
    {
        Direction = direction;
        this.utility = utility;
    }

    /// <summary>
    /// Notices that this method is an "inverse" sorting. It makes the higher values on top of the Sort, instead of
    /// the smaller values. For the smaller values, the return line would be utility.CompareTo(otherAgent.utility).
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object obj)
    {
        if (obj == null) return 1;

        AgentDirection otherAgent = (AgentDirection)obj;
        return otherAgent.utility.CompareTo(utility);
    }
}

/// <summary>
/// This struct stores all genes / weights from an Agent.
/// It is used to pass this information along to other Agents, instead of using the MonoBehavior itself.
/// Also, it makes it easier to inspect since it is a Serializable struct.
/// </summary>
[Serializable]
public struct AgentData
{
    //Default boxes, boats and pirates
    public int steps;
    public int rayRadius;
    public float sight;
    public float movingSpeed;
    public Vector2 randomDirectionValue;
    public float boxWeight;
    public float distanceFactor;
    public float boatWeight;
    public float boatDistanceFactor;
    public float enemyWeight;
    public float enemyDistanceFactor;

    //Powerups weights
    public float speedWeight;
    public float speedDistanceFactor;
    public float speedEnvironmentFactor;
    public float pullWeight;
    public float pullDistanceFactor;
    public float pullEnvironmentFactor;
    public float multiplierWeight;
    public float multiplierDistanceFactor;
    public float multiplierEnvironmentFactor;


    public AgentData(int steps, int rayRadius, float sight, float movingSpeed, Vector2 randomDirectionValue,
                    float boxWeight, float distanceFactor,
                    float boatWeight, float boatDistanceFactor,
                    float enemyWeight, float enemyDistanceFactor,
                    float speedWeight, float speedDistanceFactor, float speedEnvironmentFactor,
                    float pullWeight, float pullDistanceFactor, float pullEnvironmentFactor,
                    float multiplierWeight, float multiplierDistanceFactor, float multiplierEnvironmentFactor)
    {
        //Variables initialization

        //Default boxes, boats and pirates
        this.steps = steps;
        this.rayRadius = rayRadius;
        this.sight = sight;
        this.movingSpeed = movingSpeed;
        this.randomDirectionValue = randomDirectionValue;
        this.boxWeight = boxWeight;
        this.distanceFactor = distanceFactor;
        this.boatWeight = boatWeight;
        this.boatDistanceFactor = boatDistanceFactor;
        this.enemyWeight = enemyWeight;
        this.enemyDistanceFactor = enemyDistanceFactor;

        //Powerups
        this.speedWeight = speedWeight;
        this.speedDistanceFactor = speedDistanceFactor;
        this.speedEnvironmentFactor = speedEnvironmentFactor;
        this.pullWeight = pullWeight;
        this.pullDistanceFactor = pullDistanceFactor;
        this.pullEnvironmentFactor = pullEnvironmentFactor;
        this.multiplierWeight = multiplierWeight;
        this.multiplierDistanceFactor = multiplierDistanceFactor;
        this.multiplierEnvironmentFactor = multiplierEnvironmentFactor;
    }
}

/// <summary>
/// Main script for the Agent behaviour.
/// It is responsible for caring its genes, deciding its actions and controlling debug properties.
/// The agent moves by using its rigidBody velocity. The velocity is set to its speed times the movementDirection.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AgentLogic : MonoBehaviour, IComparable
{
    private Vector3 _movingDirection;
    private Rigidbody _rigidbody;

    [SerializeField]
    protected float points;

    private bool _isAwake;

    [Header("Genes")]
    [SerializeField, Tooltip("Steps for the area of sight.")]
    private int steps;
    [SerializeField, Range(0.0f, 360.0f), Tooltip("Divides the 360˚ view of the Agent into rayRadius steps.")]
    private int rayRadius = 16;
    [SerializeField, Tooltip("Ray distance. For the front ray, the value of 1.5 * Sight is used.")]
    private float sight = 10.0f;
    [SerializeField]
    private float movingSpeed;
    [SerializeField, Tooltip("All directions starts with a random value from X-Y (Math.Abs, Math.Min and Math.Max are applied).")]
    private Vector2 randomDirectionValue;

    //All visible objects container
    private Dictionary<GameObject, float> visibleObjects;
    private int actIteration;

    //Weights fields
    //Default boxes, boats and pirates
    [Space(10)]
    [Header("Default Weights")]
    [SerializeField]
    private float boxWeight;
    [SerializeField]
    private float boxDistanceFactor;
    [SerializeField]
    private float boatWeight;
    [SerializeField]
    private float boatDistanceFactor;
    [SerializeField]
    private float enemyWeight;
    [SerializeField]
    private float enemyDistanceFactor;

    [Header("Abled agent")]
    [SerializeField]
    private bool abled;

    //Powerups
    [Space(10)]
    [Header("Powerups Weights")]
    [SerializeField]
    private float speedWeight;
    [SerializeField]
    private float speedDistanceFactor;
    [SerializeField]
    private float speedEnvironmentFactor;
    [SerializeField]
    private float pullWeight;
    [SerializeField]
    private float pullDistanceFactor;
    [SerializeField]
    private float pullEnvironmentFactor;
    [SerializeField]
    private float multiplierWeight;
    [SerializeField]
    private float multiplierDistanceFactor;
    [SerializeField]
    private float multiplierEnvironmentFactor;

    //Weights that depend on the environment.
    private float speedEnvironmentWeight = 1.0f;
    private float pullEnvironmentWeight = 1.0f;
    private float multiplierEnvironmentWeight = 1.0f;

    //Factors that allow agents ignore powerups that are already activated. 
    //Doesn't apply for pull powerup, since it can be activated several times.
    private float speedActiveFactor = 1.0f;
    private float multiplierActiveFactor = 1.0f;

    //Powerups strength
    [Space(10)]
    [Header("Powerups power")]
    [SerializeField]
    private float speedMultiplierPower = 2.0f;
    [SerializeField]
    private float pullPower = 4.0f;
    [SerializeField]
    private float pointsMultiplierPower = 2.0f;

    protected float pointsMultiplier = 1.0f;
    protected float speedMultiplier = 1.0f;


    [Space(10)]
    [Header("Debug & Help")]
    [SerializeField]
    private Color visionColor;
    [SerializeField]
    private Color foundColor;
    [SerializeField]
    private Color directionColor;
    [SerializeField, Tooltip("Shows visualization rays.")]
    private bool debug;

    #region Static Variables
    //private static float _minimalSteps = 1.0f;
    //private static float _minimalRayRadius = 1.0f;
    //private static float _minimalSight = 0.1f;
    //private static float _minimalMovingSpeed = 1.0f;
    //private static float _speedInfluenceInSight = 0.1250f;
    //private static float _sightInfluenceInSpeed = 0.0625f;
    private static float _maxUtilityChoiceChance = 0.85f;
    #endregion

    private void Awake()
    {
        Initiate();
    }

    /// <summary>
    /// Initiate the values for this Agent, settings its points to 0 and recalculating its sight parameters.
    /// </summary>
    private void Initiate()
    {
        points = 0;
        steps = 360 / rayRadius;
        _rigidbody = GetComponent<Rigidbody>();
        visibleObjects = new Dictionary<GameObject, float>();
    }

    /// <summary>
    /// Copies the genes / weights from the parent.
    /// </summary>
    /// <param name="parent"></param>
    public void Birth(AgentData parent)
    {
        //Default boxes, boats and pirates
        steps = parent.steps;
        rayRadius = parent.rayRadius;
        sight = parent.sight;
        movingSpeed = parent.movingSpeed;
        randomDirectionValue = parent.randomDirectionValue;
        boxWeight = parent.boxWeight;
        boxDistanceFactor = parent.distanceFactor;
        boatWeight = parent.boatWeight;
        boatDistanceFactor = parent.boatDistanceFactor;
        enemyWeight = parent.enemyWeight;
        enemyDistanceFactor = parent.enemyDistanceFactor;

        //Powerups. If the agent is not abled, the value of powerups will always be 0.
        if (abled)
        {
            speedWeight = parent.speedWeight;
            speedDistanceFactor = parent.speedDistanceFactor;
            speedEnvironmentFactor = parent.speedEnvironmentFactor;
            pullWeight = parent.pullWeight;
            pullDistanceFactor = parent.pullDistanceFactor;
            pullEnvironmentFactor = parent.pullEnvironmentFactor;
            multiplierWeight = parent.multiplierWeight;
            multiplierDistanceFactor = parent.multiplierDistanceFactor;
            multiplierEnvironmentFactor = parent.multiplierEnvironmentFactor;
        }
    }

    /// <summary>
    /// Has a mutationChance ([0%, 100%]) of causing a mutationFactor [-mutationFactor, +mutationFactor] to each gene / weight.
    /// The chance of mutation is calculated per gene / weight.
    /// </summary>
    /// <param name="mutationFactor">How much a gene / weight can change (-mutationFactor, +mutationFactor)</param>
    /// <param name="mutationChance">Chance of a mutation happening per gene / weight.</param>
    public void Mutate(float mutationFactor, float mutationChance)
    {


        //DISABLED MOVEMENT SPEED, SIGHT MUTATION FOR BALANCED TESTING

        //if (Random.Range(0.0f, 100.0f) <= mutationChance)
        //{
        //    steps += (int)Random.Range(-mutationFactor, +mutationFactor);
        //    steps = (int)Mathf.Max(steps, _minimalSteps);
        //}
        //if (Random.Range(0.0f, 100.0f) <= mutationChance)
        //{
        //    rayRadius += (int)Random.Range(-mutationFactor, +mutationFactor);
        //    rayRadius = (int)Mathf.Max(rayRadius, _minimalRayRadius);
        //}
        //if (Random.Range(0.0f, 100.0f) <= mutationChance)
        //{
        //    float sightIncrease = Random.Range(-mutationFactor, +mutationFactor);
        //    sight += sightIncrease;
        //    sight = Mathf.Max(sight, _minimalSight);
        //    if (sightIncrease > 0.0f)
        //    {
        //        movingSpeed -= sightIncrease * _sightInfluenceInSpeed;
        //        movingSpeed = Mathf.Max(movingSpeed, _minimalMovingSpeed);
        //    }
        //}
        //if (Random.Range(0.0f, 100.0f) <= mutationChance)
        //{
        //    float movingSpeedIncrease = Random.Range(-mutationFactor, +mutationFactor);
        //    movingSpeed += movingSpeedIncrease;
        //    movingSpeed = Mathf.Max(movingSpeed, _minimalMovingSpeed);
        //    if (movingSpeedIncrease > 0.0f)
        //    {
        //        sight -= movingSpeedIncrease * _speedInfluenceInSight;
        //        sight = Mathf.Max(sight, _minimalSight);
        //    }
        //}

        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            randomDirectionValue.x += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            randomDirectionValue.y += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boxWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boxDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boatWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boatDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            enemyWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            enemyDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }

        //Powerups factors 
        if (!abled)
        {
            return;
        }

        //Speed
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            speedWeight += Random.Range(-mutationFactor, +mutationFactor);
            if (speedWeight <= 0)
            {
                speedWeight = 0;
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            speedDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
            if (speedDistanceFactor <= 0)
            {
                speedDistanceFactor = 0;
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            speedEnvironmentFactor += Random.Range(-mutationFactor, +mutationFactor) / 10;
            if (speedEnvironmentFactor <= 0)
            {
                speedEnvironmentFactor = 0;
            }
        }
        //Pull
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            pullWeight += Random.Range(-mutationFactor, +mutationFactor);
            if (pullWeight <= 0)
            {
                pullWeight = 0;
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            pullDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
            if (pullDistanceFactor <= 0)
            {
                pullDistanceFactor = 0;
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            pullEnvironmentFactor += Random.Range(-mutationFactor, +mutationFactor) / 10;
            if (pullEnvironmentFactor <= 0)
            {
                pullEnvironmentFactor = 0;
            }
        }
        //Multiplier
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            multiplierWeight += Random.Range(-mutationFactor, +mutationFactor);
            if (multiplierWeight <= 0)
            {
                multiplierWeight = 0;
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            multiplierDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
            if (multiplierDistanceFactor <= 0)
            {
                multiplierDistanceFactor = 0;
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            multiplierEnvironmentFactor += Random.Range(-mutationFactor, +mutationFactor) / 10;
            if (multiplierEnvironmentFactor <= 0)
            {
                multiplierEnvironmentFactor = 0;
            }
        }
    }

    private void Update()
    {
        if (_isAwake)
        {
            Act();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivatePullPowerup();
        }
    }

    //Collect the powerups. Universal for both boats and pirates.
    protected void OnTriggerEnter(Collider other)
    {
        //Work only for abled agents
        if (!abled)
        {
            return;
        }

        if (other.gameObject.tag.Equals("Speed"))
        {
            Destroy(other.gameObject);
            ActivateSpeedPowerup();
        }
        if (other.gameObject.tag.Equals("Pull"))
        {
            Destroy(other.gameObject);
            ActivatePullPowerup();
        }
        if (other.gameObject.tag.Equals("Multiplier"))
        {
            Destroy(other.gameObject);
            ActivateMultiplierPowerup();
        }
    }

    /// <summary>
    /// Calculate the best direction to move using the Agent properties.
    /// The agent shoots a ray in a area on front of itself and calculates the utility of each one of them based on what
    /// it did intersect or using a random value (uses a Random from [randomDirectionValue.x, randomDirectionValue.y]). 
    /// </summary>
    private void Act()
    {
        //Clear the visible objetcs with a small delay, so that the objects in actual range dont get ignored because the raycast moved
        if (actIteration >= 60)
        {
            visibleObjects.Clear();
            actIteration = 0;

            //Reset environment factor along with the visible objects
            speedEnvironmentWeight = 1f;
            pullEnvironmentWeight = 1f;
            multiplierEnvironmentWeight = 1f;
        }

        Transform selfTransform = transform;
        Vector3 forward = selfTransform.forward;
        //Ignores the y component to avoid flying/sinking Agents.
        forward.y = 0.0f;
        forward.Normalize();
        Vector3 selfPosition = selfTransform.position;

        //Initiate the rayDirection on the opposite side of the spectrum.
        Vector3 rayDirection = Quaternion.Euler(0, -1.0f * steps * (rayRadius / 2.0f), 0) * forward;

        //List of AgentDirection (direction + utility) for all the directions.
        List<AgentDirection> directions = new List<AgentDirection>();
        for (int i = 0; i <= rayRadius; i++)
        {
            //Add the new calculatedAgentDirection looking at the rayDirection.
            directions.Add(CalculateAgentDirection(selfPosition, rayDirection));

            //Rotate the rayDirection by _steps every iteration through the entire rayRadius.
            rayDirection = Quaternion.Euler(0, steps, 0) * rayDirection;
        }
        //Adds an extra direction for the front view with a extra range.
        directions.Add(CalculateAgentDirection(selfPosition, forward, 1.5f));

        directions.Sort();
        //There is a (100 - _maxUtilityChoiceChance) chance of using the second best option instead of the highest one. Should help into ambiguous situation.
        AgentDirection highestAgentDirection = directions[Random.Range(0.0f, 100.0f) <= _maxUtilityChoiceChance ? 0 : 1];

        //Rotate towards to direction. The factor of 0.1 helps to create a "rotation" animation instead of automatically rotates towards the target. 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(highestAgentDirection.Direction), 0.1f);

        //Sets the velocity using the chosen direction
        _rigidbody.velocity = highestAgentDirection.Direction * movingSpeed * speedMultiplier;

        actIteration++;

        if (debug)
        {
            Debug.DrawRay(selfPosition, highestAgentDirection.Direction * (sight * 1.5f), directionColor);
        }
    }

    private AgentDirection CalculateAgentDirection(Vector3 selfPosition, Vector3 rayDirection, float sightFactor = 1.0f)
    {
        if (debug)
        {
            Debug.DrawRay(selfPosition, rayDirection * sight, visionColor);
        }

        //Calculate a random utility to initiate the AgentDirection.
        float utility = Random.Range(Mathf.Min(randomDirectionValue.x, randomDirectionValue.y), Mathf.Max(randomDirectionValue.x, randomDirectionValue.y));

        //Create an AgentDirection struct with a random utility value [utility]. Ignores y component.
        AgentDirection direction = new AgentDirection(new Vector3(rayDirection.x, 0.0f, rayDirection.z), utility);

        //Raycast into the rayDirection to check if something can be seen in that direction.
        //The sightFactor is a variable that increases / decreases the size of the ray.
        //For now, the sightFactor is only used to control the long sight in front of the agent.
        if (Physics.Raycast(selfPosition, rayDirection, out RaycastHit raycastHit, sight * sightFactor))
        {
            if (debug)
            {
                Debug.DrawLine(selfPosition, raycastHit.point, foundColor);
            }

            //Calculate the normalized distance from the agent to the intersected object.
            //Closer objects will have distancedNormalized close to 0, and further objects will have it close to 1.
            float distanceNormalized = (raycastHit.distance / (sight * sightFactor));

            //Inverts the distanceNormalized. Closer objects will tend to 1, while further objects will tend to 0.
            //Thus, closer objects will have a higher value.
            float distanceIndex = 1.0f - distanceNormalized;

            GameObject visibleObject = raycastHit.collider.gameObject;

            //Calculate the utility of the found object according to its type.
            switch (visibleObject.tag)
            {
                case "Box":
                    utility = distanceIndex * boxDistanceFactor + boxWeight;
                    break;
                case "Boat":
                    utility = distanceIndex * boatDistanceFactor + boatWeight;
                    break;
                case "Enemy":
                    utility = distanceIndex * enemyDistanceFactor + enemyWeight;
                    break;
                case "Speed":
                    if (!abled)                    
                        break;                    
                    utility = distanceIndex * speedDistanceFactor + speedWeight * speedEnvironmentWeight * speedActiveFactor;
                    break;
                case "Pull":
                    if (!abled)                    
                        break;                    
                    utility = distanceIndex * pullDistanceFactor + pullWeight * pullEnvironmentWeight;
                    break;
                case "Multiplier":
                    if (!abled)                    
                        break;                    
                    utility = distanceIndex * multiplierDistanceFactor + multiplierWeight * multiplierEnvironmentWeight * multiplierActiveFactor;
                    break;
            }

            //Add or update the object in the dictionary.
            if (visibleObjects.ContainsKey(visibleObject))
            {
                visibleObjects[visibleObject] = utility;
            }
            else
            {
                visibleObjects.Add(visibleObject, utility);

                //Increase the probability of the agent to pick up a powerup, depending on how many objects are around.
                speedEnvironmentWeight += speedEnvironmentFactor;
                pullEnvironmentWeight += pullEnvironmentFactor;
                multiplierEnvironmentWeight += multiplierEnvironmentFactor;
            }
        }

        direction.utility = utility;
        return direction;
    }

    private void ActivateSpeedPowerup()
    {
        speedMultiplier = speedMultiplierPower;
        speedActiveFactor = 0.0f;
    }

    private void ActivatePullPowerup()
    {
        foreach (GameObject visibleObject in visibleObjects.Keys)
        {
            if (visibleObject == null)
            {
                continue;
            }

            //Pull only objects that you are interested in
            if (visibleObjects[visibleObject] >= 0)
            {
                PullObject pull = visibleObject.AddComponent<PullObject>();
                pull.SetTargetAndSpeed(gameObject, pullPower);
            }
        }
    }

    private void ActivateMultiplierPowerup()
    {
        pointsMultiplier = pointsMultiplierPower;
        multiplierActiveFactor = 0.0f;
    }

    /// <summary>
    /// Activates the agent update method.
    /// Does nothing if the agent is already awake.
    /// </summary>
    public void AwakeUp()
    {
        _isAwake = true;
    }

    /// <summary>
    /// Stops the agent update method and sets its velocity to zero.
    /// Does nothing if the agent is already sleeping.
    /// </summary>
    public void Sleep()
    {
        _isAwake = false;
        _rigidbody.velocity = Vector3.zero;
    }

    public float GetPoints()
    {
        return points;
    }

    /// <summary>
    /// Compares the points of two agents. When used on Sort function will make the highest points to be on top.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public int CompareTo(object obj)
    {
        if (obj == null) return 1;

        AgentLogic otherAgent = obj as AgentLogic;
        if (otherAgent != null)
        {
            return otherAgent.GetPoints().CompareTo(GetPoints());
        }
        else
        {
            throw new ArgumentException("Object is not an AgentLogic");
        }
    }

    /// <summary>
    /// Returns the AgentData of this Agent.
    /// </summary>
    /// <returns></returns>
    public AgentData GetData()
    {
        return new AgentData(steps, rayRadius, sight, movingSpeed, randomDirectionValue, boxWeight, boxDistanceFactor, boatWeight, boatDistanceFactor, enemyWeight, enemyDistanceFactor, speedWeight, speedDistanceFactor,speedEnvironmentFactor, pullWeight, pullDistanceFactor, pullEnvironmentFactor, multiplierWeight, multiplierDistanceFactor, multiplierEnvironmentFactor);
    }
}
