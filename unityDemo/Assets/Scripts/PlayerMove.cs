﻿using UnityEngine;
using MLAgents;

public class PlayerMove : TacticsMove
{
    public int mode;   //0: training; 1: player mode
    bool[] isGoalReached = new bool[3];
    GameObject target;
    public float speed;
    float last_dist = 14;
    float net_dist_change = 0;
    float net_dist_ratio = 1;

    // Use this for initialization
    void Start()
    {
        Init();
        target = new GameObject();
    }
    void Update()
    {

        if (!turn)
        {
            return;
        }

        if (!moving)
        {
            FindSelectableTiles();

            if (mode == 0)
            {
                RequestDecision();  //call for external training
                moving = true;
            }
            else
            {
                RequestAction(); //call for internal decision
                moving = true;
            }

        }else
        {

            float step = speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, step);
            if (Vector3.Distance(transform.position, target.transform.position) <= 0.001)
            {
                moving = false;
                TurnManager.EndTurn();
            }
        }

    }

    //// Update is called once per frame
    //void Update()
    //{
    //    Debug.DrawRay(transform.position, transform.forward);

    //    if (!turn)
    //    {
    //        return;
    //    }

    //    if (!moving)
    //    {

    //        if (mode == 1)
    //        {
    //            //FindSelectableTiles();
    //            //CheckMouse();
    //        }
    //        else
    //        {
    //            RequestDecision();
    //            CalculatePath();
    //            FindSelectableTiles();
    //            actualTargetTile.target = true;
    //        }

    //    }
    //    else
    //    {
    //        Move();
    //    }
    //}

    public override void AgentReset()
    {
        this.transform.position = new Vector3(-2.5f, 1.4f, 2.5f);
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
        GameObject npc = GameObject.FindGameObjectWithTag("NPC");
        AddVectorObs(Vector3.Distance(this.transform.position, npc.transform.position));

        GameObject[] targets = GameObject.FindGameObjectsWithTag("Goal");
        foreach (GameObject obj in targets)
        {
            float d = Vector3.Distance(this.transform.position, obj.transform.position);
            AddVectorObs(d);

        }
        // Calculate relative position
        Vector3 relativePosition = npc.transform.position - this.transform.position;

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


    void isNPCReachGoal()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Goal");
        GameObject npc = GameObject.FindGameObjectWithTag("NPC");

        int i = 0;
        foreach (GameObject obj in targets)
        {
            float d = Vector3.Distance(npc.transform.position, obj.transform.position);

            //when reach one goal
            if (System.Math.Abs(d) < 0.0001)
                isGoalReached[i] = true;

            i++;
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        isNPCReachGoal();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        GameObject npc = GameObject.FindGameObjectWithTag("NPC");
        float distance = 14;

        float d = Vector3.Distance(this.transform.position, npc.transform.position);
        if (System.Math.Abs(d) < 0.0001)
            distance = 0f;

        //reward
        net_dist_change = last_dist - distance;
        net_dist_ratio = -distance / last_dist;
        last_dist = distance;


        AddReward(0.8f * net_dist_change / 13 + 0.2f * Mathf.Exp(net_dist_ratio));

        //punishment for reaching too close to player
        if (Vector3.Distance(npc.transform.position, this.transform.position) <= 2)
            AddReward(0.4f);

        if (this.transform.position.x < -5.5 || this.transform.position.x > 5.5 || this.transform.position.z < -5.5 || this.transform.position.z > 5.5)
            AddReward(-1f);
        FindObjectOfType<Academy>().Done();

        if (GetTargetTile(player).walkable == false)
        {
            AddReward(-1f);
            FindObjectOfType<Academy>().Done();
        }

        //reaches goals
        if (System.Math.Abs(last_dist) < 0.0001)
        {
            AddReward(1f);
            FindObjectOfType<Academy>().Done();
        }

        // Time penalty
        AddReward(-0.05f);

        //Action
        int movement = Mathf.FloorToInt(vectorAction[0]);

        Vector3 change = new Vector3(0, 0, 0);
        if (movement == 0) { change = new Vector3(-1, 0, 0); }
        if (movement == 1) { change = new Vector3(1, 0, 0); }
        if (movement == 2) { change = new Vector3(0, 0, -1); }
        if (movement == 3) { change = new Vector3(0, 0, 1); }
        player.transform.position += change;
        target.transform.position = player.transform.position;
    }
}