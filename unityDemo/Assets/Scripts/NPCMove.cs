using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class NPCMove : TacticsMove 
{
    GameObject target;
    public int mode;   //0: training; 1: not training
    bool[] isGoalReached = new bool[3];
    float last_dist = 14;
    float net_dist_change = 0;
    float net_dist_ratio = 1;
    public float speed;
    // Use this for initialization
    void Start () 
	{
        Init();
        target = new GameObject();
    }

    void Update()
    {
        if (!turn)
            return;
        

        if (moving)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, step);
            
            if (Vector3.Distance(transform.position, target.transform.position) <= 0.1)
            {
                moving = false;
                TurnManager.EndTurn();
            }
        }

    }

    public override void AgentReset()
    {
        this.transform.position = new Vector3(2.5f, 1.4f, -0.5f);
        moving = false;
        if (turn)
            TurnManager.EndTurn();

        // FindObjectOfType<Academy>().Done();
    }

    public override void CollectObservations()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        AddVectorObs(Vector3.Distance(this.transform.position, player.transform.position));

        GameObject[] targets = GameObject.FindGameObjectsWithTag("Goal");
        foreach (GameObject obj in targets)
        {
            float d = Vector3.Distance(this.transform.position, obj.transform.position);
            AddVectorObs(d);

        }
        // Calculate relative position
        Vector3 relativePosition = player.transform.position - this.transform.position;

        // Relative position
        AddVectorObs(relativePosition.x / 5.5f);
        AddVectorObs(relativePosition.z / 5.5f);

        // is Goal reached
        foreach (bool x in isGoalReached)
        {
            if (x)
                AddVectorObs(1f);
            else
                AddVectorObs(0f);
        }
    }

    void CalculatePath()
    {
        Tile targetTile = GetTargetTile(target);
        FindPath(targetTile);
    }

    void FindNearestTarget()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Goal");

        float distance = 14;

        int i = 0;
        foreach (GameObject obj in targets)
        {
            float d = Vector3.Distance(this.transform.position, obj.transform.position);

            //when reach one goal
            if (System.Math.Abs(d) < 0.1)
            {
                isGoalReached[i] = true;
                distance = 0f;
            }

            if (d < distance && isGoalReached[i] != true)
            {
                distance = d;
            }

            net_dist_change = last_dist - distance;
            net_dist_ratio = -distance / last_dist;
            last_dist = distance;

            i++;
        }

    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        
        if (!turn)
            return;
        

        if (!moving)
        {
            moving = true;
            if (this.transform.position.x < -5.5 || this.transform.position.x > 5.5 || this.transform.position.z < -5.5 || this.transform.position.z > 5.5)
            {
                AddReward(-1f);
                Done();
                return;
            }
            
            FindSelectableTiles();

            FindNearestTarget();

            //reward
            AddReward(0.8f * net_dist_change / 13 + 0.2f * Mathf.Exp(net_dist_ratio));

            //punishment for reaching too close to player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            GameObject npc = GameObject.FindGameObjectWithTag("NPC");


            if (Vector3.Distance(transform.position, player.transform.position) <= 2)
                AddReward(-0.4f);

            // RaycastHit hit;
            // if (Physics.Raycast(transform.position, Vector3.up, out hit, 1))
            // {
            //     AddReward(-1f);
            //     Done();
            // }

            GameObject[] obs = GameObject.FindGameObjectsWithTag("Obstacle");
            foreach (GameObject obj in obs)
            {
                Vector3 a = transform.position;
                Vector3 b = obj.transform.position;
                a.y = b.y;

                if (System.Math.Abs(Vector3.Distance(a, b)) < 0.1)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }
            }

            //reaches goals
            int i = 0;
            int count = 0;
            foreach (bool x in isGoalReached)
            {
                if (x)
                {
                    if (System.Math.Abs(last_dist) < 0.1)
                        AddReward(1f);
                    last_dist = 12; //reset prev_dist
                    count++;
                }
                if (i == 3)
                    break;
                i++;
            }
            //two goals are reached
            if (count == 2)
            {
                Done();
                return;
            }
            // Time penalty
            AddReward(-0.02f);

            //Action
            int movement = Mathf.FloorToInt(vectorAction[0]);

            Vector3 change = new Vector3(0, 0, 0);
            if (movement == 0) { change = new Vector3(-1, 0, 0); }
            if (movement == 1) { change = new Vector3(1, 0, 0); }
            if (movement == 2) { change = new Vector3(0, 0, -1); }
            if (movement == 3) { change = new Vector3(0, 0, 1); }
            npc.transform.position += change;
            target.transform.position = npc.transform.position;


            transform.position =  target.transform.position;
            

        }
    }
}
