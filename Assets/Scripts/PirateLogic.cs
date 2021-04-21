using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class PirateLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 0.1f;
    private static float _boatPoints = 5.0f;
    #endregion

    protected void OnTriggerEnter(Collider other)
    {
        //Call the base trigger for powerups
        base.OnTriggerEnter(other);

        if (other.gameObject.tag.Equals("Box"))
        {
            points += _boxPoints * pointsMultiplier;
            Destroy(other.gameObject);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag.Equals("Boat"))
        {
            points += _boatPoints * pointsMultiplier;
            Destroy(other.gameObject);
        }
    }

}
