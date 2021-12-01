using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VRtist;
using Unity.Burst;


[BurstCompile(CompileSynchronously = true)]
public struct CurveEvalutationJobs : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<keyStruct> Keys;
    [ReadOnly]
    public NativeArray<int> CachedKeysIndices;
    [ReadOnly]
    public int StartFrame;
    [WriteOnly]
    public NativeArray<float> Value;

    public void Execute(int index)
    {
        if (Keys.Length == 0)
        {
            Value[index] = 0;
            return;
        }
        int prevIndex = CachedKeysIndices[index + StartFrame - 1];
        if (prevIndex == -1)
        {
            Value[index] = Keys[0].value;
            return;
        }
        if (prevIndex == Keys.Length - 1)
        {
            Value[index] = Keys[Keys.Length - 1].value;
            return;
        }

        keyStruct prevKey = Keys[prevIndex];
        switch (prevKey.interpolation)
        {
            case Interpolation.Constant:
                Value[index] = prevKey.value;
                break;
            case Interpolation.Linear:
                keyStruct nextKey = Keys[prevIndex + 1];
                float dt = (index - prevKey.frame) / (float)(nextKey.frame - prevKey.frame);
                float oneMinusDt = 1f - dt;
                Value[index] = prevKey.value * oneMinusDt + nextKey.value * dt;
                break;
            case Interpolation.Bezier:
                keyStruct nextKey1 = Keys[prevIndex + 1];
                Vector2 A = new Vector2(prevKey.frame, prevKey.value);
                Vector2 D = new Vector2(nextKey1.frame, nextKey1.value);

                Vector2 B = A + prevKey.outTangent;
                Vector2 C = D - nextKey1.inTangent;
                Value[index] = EvaluateBezier(A, B, C, D, index + StartFrame);

                break;
        }
    }

    private float EvaluateBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, int frame)
    {
        if ((float)frame == A.x)
            return A.y;

        if ((float)frame == D.x)
            return D.y;

        float pmin = 0;
        float pmax = 1;
        Vector2 avg = A;
        float dt = D.x - A.x;
        int safety = 0;
        while (dt > 0.1f)
        {
            float param = (pmin + pmax) * 0.5f;
            avg = CubicBezier(A, B, C, D, param);
            if (avg.x < frame)
            {
                pmin = param;
            }
            else
            {
                pmax = param;
            }
            dt = Mathf.Abs(avg.x - (float)frame);
            if (safety > 1000)
            {
                Debug.LogError("bezier job error");
                break;
            }
            else safety++;
        }
        return avg.y;
    }

    private Vector2 CubicBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, float t)
    {
        float invT1 = 1 - t;
        float invT2 = invT1 * invT1;
        float invT3 = invT2 * invT1;

        float t2 = t * t;
        float t3 = t2 * t;

        return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
    }

}

public struct keyStruct
{
    public int frame;
    public float value;
    public Vector2 inTangent;
    public Vector2 outTangent;
    public Interpolation interpolation;

    public static keyStruct GetKeyStruct(AnimationKey key)
    {
        return new keyStruct()
        {
            frame = key.frame,
            value = key.value,
            inTangent = key.inTangent,
            outTangent = key.outTangent,
            interpolation = key.interpolation
        };
    }
}
