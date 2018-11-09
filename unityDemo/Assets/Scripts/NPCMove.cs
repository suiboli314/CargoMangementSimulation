using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class NPCMove : TacticsMove 
{
    public int mode;   //0: training; 1: player mode
    public float speed;
    float last_dist = 14;
    float net_dist_change = 0;
    float net_dist_ratio = 1;

    EscapeAcademy academy;
    GameObject player;
    GameObject npc;
    GameObject curgoal;
    
    bool[] isGoalReached = new bool[3];
    int[] goalSeq = new int[3];
    GameObject[] goals;
    GameObject[] obs;

    Transform nextTile;

    void Start()
    {
        Init();
        nextTile = this.transform;
    }

    // Use this for initialization
    public override void InitializeAgent()
    {
        Init();

        academy = FindObjectOfType<EscapeAcademy>();
        player = gameObject;
        npc = GameObject.FindGameObjectWithTag("NPC");

        goals = GameObject.FindGameObjectsWithTag("Goal");

        goalSeq = pickOne();

        for (int i = 0; i < 3; i++)
        {
            if (goalSeq[i] == 0)
                curgoal = goals[i];
        }

        obs = GameObject.FindGameObjectsWithTag("Obstacle");
    }

    protected int[] pickOne()
    {
        int[] g = new int[3];
        int v = Random.Range(0, 2);

        for(int i = 0; i < 3; i++)
        {
            if (i == v)
                g[i] = 0;
            else
                g[i] = -1;
        } 

        return g;
    }

    protected int[] pickTwo()
    {
        int[] g = new int[3];
        ArrayList value = new ArrayList(3);

        for (int i = 0; i < 3; i++)
            value.Add(i - 1);

        int r = Random.Range(0, 2);
        g[0] = (int)value[r];

        value.RemoveAt(r);
        //Debug.Log("removed:" + g[0] + "\tvalue2:" + value);

        r = Random.Range(0, 1);
        g[1] = (int)value[r];
        value.RemoveAt(r);

        g[2] = (int)value[r];

        return g;
    }

    void Update()
    {
        if (!turn)
            return;

        if (moving)
        {
            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, nextTile.position, step);
            if (Vector3.Distance(transform.position, nextTile.position) <= 0.1)
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
        goalSeq = pickOne();
        if (turn)
            TurnManager.EndTurn();

        //academy.Done();
    }

    public override void CollectObservations()
    {
        // Self position
        AddVectorObs(this.transform.position.x);
        AddVectorObs(this.transform.position.z);

        // isGoalSelected
        foreach (int i in goalSeq)
        {
            AddVectorObs(i);
        }
        // Add A* distances to the goal
        foreach (GameObject obj in goals)
        {
            AddVectorObs(A_Star(obj));
        }
        // is Goal reached
        foreach (bool x in isGoalReached)
        {
            if (x)
                AddVectorObs(1f);
            else
                AddVectorObs(0f);
        }
    }


    public int A_Star(GameObject goal)
    {
        Tile target_tile = GetTargetTile(goal);
        return FindPath(target_tile);
    }

    void isRobReachGoal(GameObject rob)
    {

        int i = 0;
        foreach (GameObject obj in goals)
        {
            float d = Vector3.Distance(rob.transform.position, obj.transform.position);

            //when reach one goal
            if (System.Math.Abs(d) < 0.1)
                isGoalReached[i] = true;

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
                SetReward(-1f);
                Done();
                return;
            }

            FindSelectableTiles();
            isRobReachGoal(player);

            //Update varibles taken for reward function
            int curdistance = A_Star(curgoal);
            net_dist_change = last_dist - curdistance;
            net_dist_ratio = -curdistance / last_dist;
            last_dist = curdistance;

            AddReward(0.8f * net_dist_change / 13 + 0.2f * Mathf.Exp(net_dist_ratio));

            ////punishment for reaching too close to player
            //if (Vector3.Distance(npc.transform.position, this.transform.position) <= 2)
            //AddReward(0.4f);

            // RaycastHit hit;
            // if (Physics.Raycast(transform.position, Vector3.up, out hit, 1))
            // {
            //     AddReward(-1f);
            //     Done();
            // }

            foreach (GameObject obj in obs)
            {
                Vector3 a = transform.position;
                Vector3 b = obj.transform.position;
                a.y = b.y;

                if (System.Math.Abs(Vector3.Distance(a, b)) < 0.1)
                {
                    SetReward(-1f);
                    Done();
                    return;
                }
            }

            //reaches goals
            if (Vector3.Distance(transform.position, curgoal.transform.position) < 0.1)
            {
                SetReward(1f);
                bool done = true;
                for (int i = 0; i < 3; i++)
                {
                    if (goalSeq[i] == 0) //For current goal
                    {
                        isGoalReached[i] = true; //Set current goal to be "reached"
                        goalSeq[i] = -1; //Remove the goal from available goals
                    }

                    if (goalSeq[i] == 1)  //For next goal
                    {
                        goalSeq[i] = 0;
                        curgoal = goals[i]; //Set next goal to current goal
                        done = false;
                    }
                }
                if (done)
                {
                    Done(); //if there is no further goal to reach
                    return;
                }
            }

            // Time penalty
            AddReward(-0.005f);

            //Action
            int movement = Mathf.FloorToInt(vectorAction[0]);

            Vector3 change = new Vector3(0, 0, 0);
            if (movement == 0) { change = new Vector3(-1, 0, 0); }
            if (movement == 1) { change = new Vector3(1, 0, 0); }
            if (movement == 2) { change = new Vector3(0, 0, -1); }
            if (movement == 3) { change = new Vector3(0, 0, 1); }

            nextTile.position += change;
        }
    }
}
