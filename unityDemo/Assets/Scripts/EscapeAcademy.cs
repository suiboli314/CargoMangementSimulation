using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class EscapeAcademy : Academy {

    public Brain PlayerBrian;
    public Brain NPCBrain;
    public Material BlueMaterial;
    public Material YellowMaterial;

    public float PlayerPunish; //if opponents scores, the striker gets this neg reward (-1)
    public float PlayerReward; //if team scores a goal they get a reward (+1)
    public float NPCPunish; //if opponents score, goalie gets this neg reward (-1)
    public float NPCReward; //if team scores, goalie gets this reward (currently 0...no reward. can play with this later)


}
