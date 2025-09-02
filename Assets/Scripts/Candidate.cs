using UnityEngine;

public struct Candidate
{
    public Vector3 position;
    public Quaternion rotation;
}

public struct CandidateScore
{
    public float score;
    public bool isStable;
    public float maxDisplacement;
    public float maxTilt;
}
