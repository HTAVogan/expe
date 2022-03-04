using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MappingBoneWeight
{
    public float fingers;
    public float toes;
    public float legs;
    public float arms;
    public float hips;
    public float spine;
    public float pelvis;
    public float hands;

    public Dictionary<string, float> weight = new Dictionary<string, float>();
    public MappingBoneWeight(Dictionary<string, float> dic)
    {
        weight = dic;
        weight.TryGetValue("Hips", out float val);
        val = hips;
    }





}
