using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StackManager : MonoBehaviour
{
    [Header("Component References")]
    public UIManager uiManager;
    public SimulationManager simManager;
    public CameraController cameraController;
    public InputManager inputManager;

    [Header("Prefabs")]
    public GameObject cubePrefab;
    public GameObject dotPrefab;

    [Header("Game Settings")]
    public int maxPresses = 20;
    public float baseDropOffset = 2f;

    [Header("Pools")]
    public int mainPoolSize = 25;
    public int simPoolSize = 25;
    public int dotPoolSize = 30;

    private Pool<StackCube> mainCubePool;
    private Pool<StackCube> simCubePool;
    private Pool<Transform> dotPool;
    private int pressCount = 0;
    private List<StackCube> activeCubes = new List<StackCube>();
    private bool cubesAreFrozen = false;

    [Header("Freeze")]
    public GameObject ppFreeze;
    public AudioSource freezeSound;



    void Awake()
    {
        mainCubePool = new Pool<StackCube>(cubePrefab.GetComponent<StackCube>(), mainPoolSize);
        simCubePool = new Pool<StackCube>(cubePrefab.GetComponent<StackCube>(), simPoolSize);
        dotPool = new Pool<Transform>(dotPrefab.transform, dotPoolSize);

        // Hook up input events
        inputManager.OnClickAction = OnButtonPressed;
        inputManager.OnPointerEnterAction = uiManager.OnButtonPointerEnter;
        inputManager.OnPointerExitAction = uiManager.OnButtonPointerExit;
        inputManager.OnPointerDownAction = uiManager.OnButtonPointerDown;
        inputManager.OnPointerUpAction = uiManager.OnButtonPointerUp;

        uiManager.OnFreezeClicked = ToggleFreezeState;
    }

    public void OnButtonPressed()
    {
        if (pressCount >= maxPresses) return;
        pressCount++;
        StartCoroutine(PlaceBestCandidateRoutine());
    }

    IEnumerator PlaceBestCandidateRoutine()
    {
        float currentMaxHeight = GetCurrentMaxHeight();
        float dropHeight = currentMaxHeight + baseDropOffset;

        List<StackCube> simObjects = simManager.SnapshotStack(activeCubes, simCubePool);

        List<SimulationManager.SnapshotState> initialStates = new List<SimulationManager.SnapshotState>();
        foreach (var cube in simObjects)
        {
            Rigidbody rbc = cube.GetComponent<Rigidbody>();
            initialStates.Add(new SimulationManager.SnapshotState
            {
                position = cube.transform.position,
                rotation = cube.transform.rotation,
                velocity = rbc.velocity,
                angularVelocity = rbc.angularVelocity
            });
        }

        List<SimulationManager.Candidate> candidates = simManager.GenerateCandidates(dropHeight);
        List<SimulationManager.CandidateScore> scores = new List<SimulationManager.CandidateScore>();
        foreach (var cand in candidates)
        {
            scores.Add(simManager.SimulateCandidate(cand, simObjects, initialStates, simCubePool));
        }

        foreach (var cube in simObjects)
        {
            simCubePool.Release(cube);
        }

        int bestIndex = 0;
        float bestScore = float.MinValue;
        for (int i = 0; i < scores.Count; i++)
        {
            if (scores[i].score > bestScore)
            {
                bestScore = scores[i].score;
                bestIndex = i;
            }
        }
        SimulationManager.Candidate chosen = candidates[bestIndex];

        for (int i = 0; i < candidates.Count; i++)
        {
            Transform dot = dotPool.Get();
            dot.position = candidates[i].position + Vector3.up * 0.2f;
            dot.GetComponent<Renderer>().material.color = (i == bestIndex) ? Color.green : Color.red;
            StartCoroutine(ReleaseAfter(dot.gameObject, 1f));
        }

        StackCube newCube = mainCubePool.Get();
        newCube.transform.position = chosen.position;
        newCube.transform.rotation = chosen.rotation;
        Rigidbody rb = newCube.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        activeCubes.Add(newCube);

        yield return new WaitForSeconds(2f);

        int tallest = ComputeTallestStack();
        uiManager.UpdateCounter(tallest);
        uiManager.UpdateAIMessage(tallest);

        if (pressCount >= maxPresses)
        {
            uiManager.DisableButton();
        }

        cameraController.AdjustCamera(tallest);
    }

    float GetCurrentMaxHeight()
    {
        float maxY = 0f;
        foreach (var cube in activeCubes)
        {
            float cubeTop = cube.transform.position.y + 0.5f;
            maxY = Mathf.Max(maxY, cubeTop);
        }
        return maxY;
    }

    int ComputeTallestStack()
    {
        if (activeCubes.Count == 0) return 0;

        // Sort cubes by height to process from bottom to top
        List<StackCube> sortedCubes = new List<StackCube>(activeCubes);
        sortedCubes.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

        // Dictionary to track which cubes belong to which stack
        Dictionary<StackCube, int> cubeStackMap = new Dictionary<StackCube, int>();
        List<List<StackCube>> stacks = new List<List<StackCube>>();

        float stackThreshold = 0.8f; // How much overlap needed to consider cubes part of same stack

        foreach (StackCube cube in sortedCubes)
        {
            bool addedToExistingStack = false;
            Vector3 cubePos = cube.transform.position;
            Bounds cubeBounds = new Bounds(cubePos, Vector3.one);

            // Check if this cube can be added to an existing stack
            for (int i = 0; i < stacks.Count; i++)
            {
                StackCube topCube = stacks[i][stacks[i].Count - 1];
                Vector3 topCubePos = topCube.transform.position;

                // Check if this cube is above the top cube of this stack
                if (cubePos.y > topCubePos.y)
                {
                    // Check if the cubes overlap sufficiently in the XZ plane
                    Vector2 topPosXZ = new Vector2(topCubePos.x, topCubePos.z);
                    Vector2 cubePosXZ = new Vector2(cubePos.x, cubePos.z);

                    if (Vector2.Distance(topPosXZ, cubePosXZ) < stackThreshold)
                    {
                        stacks[i].Add(cube);
                        cubeStackMap[cube] = i;
                        addedToExistingStack = true;
                        break;
                    }
                }
            }

            // If not added to existing stack, create a new stack
            if (!addedToExistingStack)
            {
                List<StackCube> newStack = new List<StackCube> { cube };
                stacks.Add(newStack);
                cubeStackMap[cube] = stacks.Count - 1;
            }
        }

        // Find the tallest stack
        int tallest = 0;
        foreach (List<StackCube> stack in stacks)
        {
            if (stack.Count > tallest) tallest = stack.Count;
        }

        return tallest;
    }

    IEnumerator ReleaseAfter(GameObject obj, float t)
    {
        yield return new WaitForSeconds(t);
        dotPool.Release(obj.transform);
    }

    void ToggleFreezeState()
    {
        freezeSound.Play();
        cubesAreFrozen = !cubesAreFrozen;

        if (cubesAreFrozen)
        {
            ppFreeze.SetActive(true);
        } else
        {
            ppFreeze.SetActive(false);
        }
        foreach (var cube in activeCubes)
        {
            Rigidbody rb = cube.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = cubesAreFrozen;
            }
        }
    }
}
