using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PlayerMove : TacticsMove
{
    public int mode;   //0: training; 1: player mode
    public float speed;
    float last_dist = 14;
    float net_dist_change = 0;
    float net_dist_ratio = 1;
    bool twice = false;

    EscapeAcademy academy;
    GameObject map;
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
        academy = FindObjectOfType<EscapeAcademy>();
        player = gameObject;
        npc = GameObject.FindGameObjectWithTag("NPC");
        //map = GameObject.FindGameObjectWithTag("map");
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
        int[] g = { -1, -1, -1 };
        //int v = Random.Range(0, 2);

        //for(int i = 0; i < 3; i++)
        //{
        //    if (i == v)
        //        g[i] = 0;
        //    else
        //        g[i] = -1;
        //} 

        //Should intelligently choose one goal
        float goalXNPC;
        float goalZNPC;
        float manhattan;
        float man_temp = 100f;
        int i = 0;
        int target_i = 0;

        foreach (GameObject goal in goals)
        {
            goalXNPC = goal.transform.position.x - this.transform.position.x;
            goalZNPC = goal.transform.position.z - this.transform.position.z;
            manhattan = Mathf.Abs(Mathf.Round(goalXNPC)) + Mathf.Abs(Mathf.Round(goalZNPC));

            if (manhattan < man_temp)
                target_i = i;

            i++;
        }

        g[target_i] = 0;

        return g;
    }

    protected int[] pickTwo()
    {
        int[] g = new int[3];
        ArrayList value = new ArrayList(3);

        for(int i = 0; i < 3; i++)
            value.Add(i - 1);

        int r = Random.Range(0, 2);
        g[0] = (int) value[r];

        value.RemoveAt(r);
        //Debug.Log("removed:"+ g[0] + "\tvalue2:"+value);

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
                twice = !twice;

                if (!twice)
                   TurnManager.EndTurn();
            }
        }

    }

    public override void AgentReset()
    {
        this.transform.position = new Vector3(-2.5f, 1.4f, 2.5f);
        moving = false;
        goalSeq = pickOne();
        

        if (turn)
            TurnManager.EndTurn();

        //academy.Done();
    
    }

    //void CheckMouse()
    //{
    //    if (Input.GetMouseButtonUp(0))
    //    {
    //        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

    //        RaycastHit hit;
    //        if (Physics.Raycast(ray, out hit))
    //        {
    //            if (hit.collider.tag == "Tile")
    //            {
    //                Tile t = hit.collider.GetComponent<Tile>();

    //                if (t.selectable)
    //                {
    //                    MoveToTile(t);
    //                }
    //            }
    //        }
    //    }
    //}

    public override void CollectObservations()
    {
        //// Self position
        //AddVectorObs(this.transform.position.x);
        //AddVectorObs(this.transform.position.z);

        // Self position, didn't add?
        //float playerx = this.transform.position.x - map.transform.position.x;
        //float playerz = this.transform.position.z - map.transform.position.z;

        // player NPC vector
        float playerXNPC = curgoal.transform.position.x - this.transform.position.x;
        float playerZNPC = curgoal.transform.position.z - this.transform.position.z;
        
        float manhattan = Mathf.Abs(Mathf.Round(playerXNPC)) + Mathf.Abs(Mathf.Round(playerZNPC));

        AddVectorObs(playerXNPC);
        AddVectorObs(playerZNPC);
        AddVectorObs(manhattan);

        //// isGoalSelected
        //foreach (int i in goalSeq)
        //{
        //    AddVectorObs(i);
        //}
        //// Add A* distances to the goal
        //foreach (GameObject obj in goals)
        //{
        //    AddVectorObs(A_Star(obj));
        //}
        //// is Goal reached
        //foreach (bool x in isGoalReached)
        //{
        //    if (x)
        //        AddVectorObs(1f);
        //    else
        //        AddVectorObs(0f);
        //}
    }

    public int A_Star(GameObject goal)
    {
        Tile target_tile = GetTargetTile(goal);
        return FindPath(target_tile);
    }


    // public int[] A_Star()
    // {
    //     int[] path = new int[goals.Length];
    //     int i = 0;
    //     foreach (GameObject obj in goals)
    //     {
    //         Tile target_tile = GetTargetTile(obj);
    //         path[i] = FindPath(target_tile);
    //         i++;
    //     }

    //     return path;
    // }
    

    //void CalculatePath()
    //{
    //    Tile targetTile = GetTargetTile(nextTile);
    //    FindPath(targetTile);
    //}

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

    public bool checkObs(Vector3 change)
    {
        RaycastHit hit;
        Physics.Raycast(transform.position + change + new Vector3(0, 1, 0), -Vector3.up, out hit, 1f);

        if (hit.collider != null && hit.collider.CompareTag("Obstacle"))
        {
            return true;
        }

        return false;
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
            isRobReachGoal(player);

            //Update varibles taken for reward function
            int curdistance = A_Star(curgoal);
            net_dist_change = last_dist - curdistance;
            net_dist_ratio = -curdistance / last_dist;
            last_dist = curdistance;

            AddReward(0.4f * net_dist_change);

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
                    AddReward(-1f);
                    //Done();
                    //return;
                }
            }

            //reaches goals
            if (Vector3.Distance(transform.position, curgoal.transform.position) < 1.1f)
            {
                AddReward(1f);
                bool done = true;
                //for (int i = 0; i < 3; i++)
                //{
                //    if (goalSeq[i] == 0) //For current goal
                //    {
                //        isGoalReached[i] = true; //Set current goal to be "reached"
                //        goalSeq[i] = -1; //Remove the goal from available goals
                //    }

                //    if (goalSeq[i] == 1)  //For next goal
                //    {
                //        goalSeq[i] = 0;
                //        curgoal = goals[i]; //Set next goal to current goal
                //        done = false;
                //    }
                //}
                if (done)
                {
                    Done(); //if there is no further goal to reach
                    return;
                }
            }

            // Time penalty
            AddReward(-0.0005f);

            //Action
            int movement = Mathf.FloorToInt(vectorAction[0]);

            Vector3 change = new Vector3(0, 0, 0);
            if (movement == 0) { change = new Vector3(-1, 0, 0); }
            if (movement == 1) { change = new Vector3(1, 0, 0); }
            if (movement == 2) { change = new Vector3(0, 0, -1); }
            if (movement == 3) { change = new Vector3(0, 0, 1); }

            //Check if next step is moving out
            if (this.transform.position.x + change.x < -5.5 || this.transform.position.x + change.x > 5.5 || this.transform.position.z + change.z < -5.5 || this.transform.position.z + change.z > 5.5)
            {
                AddReward(-1f);
                return;
            }

            //Check if next step is obstacle
            if (!checkObs(change)) { nextTile.position += change; }
            else { AddReward(-1f); }
        }
    }
}