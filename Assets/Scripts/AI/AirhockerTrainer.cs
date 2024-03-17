using System;
using UnityEngine;
using Random = UnityEngine.Random;


public class AirhockerTrainer : MonoBehaviour
{
    public string gateTagBlue = "gateBlue";
    public string gateTagRed = "gateRed";
    public string playerTagBlue = "playerBlue";
    public string playerTagRed = "playerRed";
    
    public TrainingType trainingType;
    public AirhockerAgent playerBlue;
    public AirhockerAgent playerRed;
    public Washer washer;
    public bool debug;


    private AirhockerAgent _agent;
    // private AirhockerAgent _enemy;
    private Rigidbody _rbAgent;
    private Rigidbody _rbEnemy;
    private Rigidbody _rbWasher;
    private Vector3 _initialGatePos;
    private Vector3 _initialEnemyGatePos;

    
    private void Start()
    {
        if (trainingType == TrainingType.OnlyBlue)
        {
            _agent = playerBlue;
            // _enemy = playerRed;
            _rbAgent = playerBlue.GetComponentInChildren<Rigidbody>();
            _rbEnemy = playerRed.GetComponentInChildren<Rigidbody>();   
        }
        else
        {
            _agent = playerRed;
            // _enemy = playerBlue;
            _rbAgent = playerRed.GetComponentInChildren<Rigidbody>();
            _rbEnemy = playerBlue.GetComponentInChildren<Rigidbody>();
        }
        _rbWasher = washer.GetComponent<Rigidbody>();
        _initialGatePos = _agent.gate.localPosition;
        _initialEnemyGatePos = _agent.enemyGate.localPosition;
    }
    
    private void FixedUpdate()
    {
        switch (trainingType)
        {
            case TrainingType.OnlyBlue:
                playerBlue.AddReward(-0.01f);
                break;
            case TrainingType.OnlyRed:
                playerRed.AddReward(-0.01f);
                break;
            case TrainingType.Both:
                bool washerIsNotMoving = Mathf.Approximately(_rbWasher.velocity.x, 0) &&
                                         Mathf.Approximately(_rbWasher.velocity.z, 0);
                
                var washerPos = _rbWasher.transform.localPosition;
                var blueDist = Vector3.Distance(playerBlue.rb.transform.localPosition, washerPos);
                var redDist = Vector3.Distance(playerRed.rb.transform.localPosition, washerPos);
                switch (washerIsNotMoving)
                {
                    case true when _rbWasher.transform.localPosition.z < 0:
                        if (debug) print("Washer doesn't move on the BLUE SIDE. BLUE -0.05f and -" + blueDist);
                        playerBlue.AddReward(-0.05f - blueDist);
                        break;
                    case true when _rbWasher.transform.localPosition.z > 0:
                        if (debug) print("Washer doesn't move on the RED SIDE. RED -0.05f and -" + redDist);
                        playerRed.AddReward(-0.05f - redDist);
                        break;
                    case true when _rbWasher.transform.localPosition.z == 0:
                        if (debug) print("Washer doesn't move in the middle. RED AND BLUE GET -0.05f and RED: -" + redDist + " and BLUE: -" + blueDist);
                        playerRed.AddReward(-0.05f - redDist);
                        playerBlue.AddReward(-0.05f - blueDist);
                        break;
                    case false when _rbWasher.transform.localPosition.z < 0:
                        if (debug) print("Washer moves on the BLUE SIDE. BLUE -0.01f");
                        playerBlue.AddReward(-0.01f);
                        break;
                    case false when _rbWasher.transform.localPosition.z > 0:
                        if (debug) print("Washer moves on the RED SIDE. RED -0.01f");
                        playerRed.AddReward(-0.01f);
                        break;
                    case false when _rbWasher.transform.localPosition.z == 0:
                        if (debug)  print("Washer moves in the middle. RED AND BLUE GET -0.01f");
                        playerRed.AddReward(-0.01f);
                        playerBlue.AddReward(-0.01f);
                        break;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public void HitWasher(string playerTag)
    {
        if (trainingType != TrainingType.Both)
            _agent.AddReward(0.5f);
        else
        {
            if (playerTag == playerTagBlue)
                playerBlue.AddReward(0.5f);
            else if (playerTag == playerTagRed)
                playerRed.AddReward(0.5f);
        }
    }

    public void Goal(string goalGateTag)
    {
        if (goalGateTag == gateTagBlue)
        {
            switch (trainingType)
            {
                case TrainingType.OnlyRed:
                    playerRed.AddReward(50f);
                    playerRed.ResetGame();
                    break;
                case TrainingType.OnlyBlue:
                    playerBlue.AddReward(-10f);
                    playerBlue.ResetGame();
                    break;
                case TrainingType.Both:
                    playerRed.AddReward(250f);
                    playerBlue.AddReward(-50f);
                    // playerBlue.ResetGame();
                    // playerRed.ResetGame();
                    playerBlue.EndEpisode();
                    playerRed.EndEpisode();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        } 
        else if (goalGateTag == gateTagRed)
        {
            switch (trainingType)
            {
                case TrainingType.OnlyRed:
                    playerRed.AddReward(-10f);
                    playerRed.ResetGame();
                    break;
                case TrainingType.OnlyBlue:
                    playerBlue.AddReward(50f);
                    playerBlue.ResetGame();
                    break;
                case TrainingType.Both:
                    playerRed.AddReward(-50f);
                    playerBlue.AddReward(250f);
                    // playerBlue.ResetGame();
                    // playerRed.ResetGame();
                    playerBlue.EndEpisode();
                    playerRed.EndEpisode();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        ResetGame(goalGateTag);
    }

    public void ResetGame(string goalGateTag)
    {
        switch (trainingType)
        {
            case TrainingType.OnlyBlue:
                ResetBlue();
                break;
            case TrainingType.OnlyRed:
                ResetRed();
                break;
            case TrainingType.Both:
                ResetBoth(goalGateTag);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void ResetBlue()
    {
        var side = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        var transformAgent = _rbAgent.transform;
        var transformEnemy = _rbEnemy.transform;
        
        // Initiating new agent's parameters
        var newAgentPos = new Vector3(
            side * Random.Range(-5.2f, 5.2f), 
            0.0585f, side * Random.Range(-11.5f, -1.7f) * (trainingType == TrainingType.OnlyBlue ? 1 : -1)
            );
        float agentHitForce = Random.Range(0, 30000f);
        Quaternion newAgentRotation = Quaternion.AngleAxis(side * Random.Range(0.0f,  360.0f), Vector3.up);
        
        // Initiating new enemy's parameters
        var newEnemyPos = new Vector3(
            side * Random.Range(-5.2f, 5.2f), 
            0.0585f, side * Random.Range(-11.5f, -1.7f) * (trainingType == TrainingType.OnlyRed ? 1 : -1)
        );
        float enemyHitForce = Random.Range(0, 30000f);

        // Initiating new washer's parameters
        // var newWasherPos = new Vector3(side * Random.Range(-5.7f, 5.7f), -0.0301f, side * Random.Range(0, 9));
        Quaternion newWasherRotation = Quaternion.AngleAxis(Random.Range(-1f, 1f) * 30f + 90*(-1f + side), Vector3.up);
        float washerHitForce = Random.Range(500.0f, 1000.0f);
        
        // Setting new agent's parameters
        transformAgent.localPosition = newAgentPos;
        transformAgent.rotation = newAgentRotation;
        // _rbAgent.AddForce(transformAgent.forward * agentHitForce);
        
        // Setting new enemy's parameters
        transformEnemy.localPosition = newEnemyPos;
        _rbEnemy.transform.LookAt(_rbAgent.transform);
        _rbEnemy.AddForce(transformEnemy.forward * enemyHitForce);
        
        // Setting new washer's parameters
        if (Random.Range(0f, 1f) > 0.5f)
        {
            var newWasherPos = newEnemyPos + _rbEnemy.transform.forward * 2f;
            _rbWasher.transform.localPosition = newWasherPos;
            _rbWasher.AddForce(newWasherRotation * Vector3.back * washerHitForce);   
        }
        else
        {
            var newWasherPos = newAgentPos + _rbAgent.transform.forward * 2f;
            _rbWasher.transform.localPosition = newWasherPos;
        }

        // Setting appropriate gates' positions
        _agent.gate.localPosition = new Vector3(_initialGatePos.x, _initialGatePos.y, side * _initialGatePos.z);
        _agent.enemyGate.localPosition = new Vector3(_initialEnemyGatePos.x, _initialEnemyGatePos.y, side * _initialEnemyGatePos.z);
    }

    private void ResetRed()
    {
        
    }

    private void ResetBoth(string goalGateTag)
    {
        return;
        var newWasherPosZ = 0f;
        if (goalGateTag == gateTagBlue)
            newWasherPosZ = -4.5f;
        else if (goalGateTag == gateTagRed)
            newWasherPosZ = 4.5f;
        var washerPos = _rbWasher.transform.localPosition;
        _rbWasher.transform.localPosition = new Vector3(washerPos.x, washerPos.y, newWasherPosZ);
        // _rbWasher.transform.localPosition = new Vector3(Random.Range(-4.5f, 4.5f), washerPos.y, newWasherPosZ);
    }
}
public enum TrainingType
{
    OnlyBlue, 
    OnlyRed, 
    Both
};

