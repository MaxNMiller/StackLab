using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SimulationManager : MonoBehaviour
{
    [Header("Simulation Settings")]
    public float simDuration = 1.5f;
    public float simTimeStep = 0.02f;
    public float stabilityDispThresh = 0.12f;
    public float stabilityTiltThresh = 20f;
    public int candidateSamples = 8;
    public float candidateXStep = 0.5f;
    public float[] candidateRotations = { -10, -5, 0, 5, 10 };
    public Material groundMaterial;

    private Scene simScene;
    private PhysicsScene simPhysics;

    public struct Candidate
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public struct CandidateScore
    {
        public float maxDisplacement;
        public float maxTilt;
        public bool isStable;
        public float score;
    }

    public struct SnapshotState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }

    void Awake()
    {
        CreateSimScene();
    }

    void CreateSimScene()
    {
        simScene = SceneManager.CreateScene("SimScene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        simPhysics = simScene.GetPhysicsScene();

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = Vector3.zero;
        ground.name = "SimGround";
        ground.GetComponent<MeshRenderer>().material = groundMaterial;
        SceneManager.MoveGameObjectToScene(ground, simScene);
    }


    public List<StackCube> SnapshotStack(List<StackCube> activeCubes, Pool<StackCube> simCubePool)
    {
        List<StackCube> simObjects = new List<StackCube>();
        foreach (var cube in activeCubes)
        {
            StackCube simCube = simCubePool.Get();
            SceneManager.MoveGameObjectToScene(simCube.gameObject, simScene);
            simCube.transform.position = cube.transform.position;
            simCube.transform.rotation = cube.transform.rotation;
            Rigidbody realRB = cube.GetComponent<Rigidbody>();
            Rigidbody simRB = simCube.GetComponent<Rigidbody>();
            simRB.velocity = realRB.velocity;
            simRB.angularVelocity = realRB.angularVelocity;
            simObjects.Add(simCube);
        }
        return simObjects;
    }

    public List<Candidate> GenerateCandidates(float dropHeight)
    {
        List<Candidate> list = new List<Candidate>();
        for (int i = 0; i < candidateSamples; i++)
        {
            float offsetX = Random.Range(-candidateXStep, candidateXStep) * (i + 1);
            float yaw = candidateRotations[Random.Range(0, candidateRotations.Length)];
            Candidate c = new Candidate
            {
                position = new Vector3(offsetX, dropHeight, 0),
                rotation = Quaternion.Euler(0, yaw, 0)
            };
            list.Add(c);
        }
        return list;
    }

    public CandidateScore SimulateCandidate(Candidate c, List<StackCube> simObjects, List<SnapshotState> initialStates, Pool<StackCube> simCubePool)
    {
        for (int j = 0; j < simObjects.Count; j++)
        {
            simObjects[j].transform.position = initialStates[j].position;
            simObjects[j].transform.rotation = initialStates[j].rotation;
            Rigidbody rb = simObjects[j].GetComponent<Rigidbody>();
            rb.velocity = initialStates[j].velocity;
            rb.angularVelocity = initialStates[j].angularVelocity;
        }

        StackCube simCube = simCubePool.Get();
        SceneManager.MoveGameObjectToScene(simCube.gameObject, simScene);
        simCube.transform.position = c.position;
        simCube.transform.rotation = c.rotation;
        Rigidbody simRB = simCube.GetComponent<Rigidbody>();
        simRB.velocity = Vector3.zero;
        simRB.angularVelocity = Vector3.zero;

        SnapshotState candState = new SnapshotState
        {
            position = simCube.transform.position,
            rotation = simCube.transform.rotation,
            velocity = simRB.velocity,
            angularVelocity = simRB.angularVelocity
        };

        float totalTime = 0f;
        while (totalTime < simDuration)
        {
            simPhysics.Simulate(simTimeStep);
            totalTime += simTimeStep;
        }

        float maxDisp = 0f;
        float maxTilt = 0f;

        for (int j = 0; j < simObjects.Count; j++)
        {
            Vector3 currPos = simObjects[j].transform.position;
            float hDisp = new Vector3(currPos.x - initialStates[j].position.x, 0, currPos.z - initialStates[j].position.z).magnitude;
            maxDisp = Mathf.Max(maxDisp, hDisp);
            float tilt = Vector3.Angle(simObjects[j].transform.up, Vector3.up);
            maxTilt = Mathf.Max(maxTilt, tilt);
        }

        Vector3 candPos = simCube.transform.position;
        float candHDisp = new Vector3(candPos.x - candState.position.x, 0, candPos.z - candState.position.z).magnitude;
        maxDisp = Mathf.Max(maxDisp, candHDisp);
        float candTilt = Vector3.Angle(simCube.transform.up, Vector3.up);
        maxTilt = Mathf.Max(maxTilt, candTilt);

        CandidateScore score = new CandidateScore
        {
            maxDisplacement = maxDisp,
            maxTilt = maxTilt,
            isStable = (maxDisp < stabilityDispThresh && maxTilt < stabilityTiltThresh),
            score = 100f - maxDisp * 50f - maxTilt
        };

        simCubePool.Release(simCube);

        return score;
    }

    public void setsamples(int samples) {
        candidateSamples = samples;
    }
    public void setxstep(float xstep)
    {
        candidateXStep = xstep;
    }
    public int getsamples()
    {
        return candidateSamples;
    }
    public float getxstep()
    {
        return candidateXStep;
    }
}
