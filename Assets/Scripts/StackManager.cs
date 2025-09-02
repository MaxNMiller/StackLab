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

        List<StackCube> sortedCubes = new List<StackCube>(activeCubes);
        sortedCubes.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

        Dictionary<StackCube, int> cubeStackMap = new Dictionary<StackCube, int>();
        List<List<StackCube>> stacks = new List<List<StackCube>>();
        float stackThreshold = 0.8f;

        foreach (StackCube cube in sortedCubes)
        {
            bool addedToExistingStack = false;
            for (int i = 0; i < stacks.Count; i++)
            {
                StackCube topCube = stacks[i][stacks[i].Count - 1];
                if (cube.transform.position.y > topCube.transform.position.y &&
                    Vector2.Distance(new Vector2(cube.transform.position.x, cube.transform.position.z), new Vector2(topCube.transform.position.x, topCube.transform.position.z)) < stackThreshold)
                {
                    stacks[i].Add(cube);
                    addedToExistingStack = true;
                    break;
                }
            }

            if (!addedToExistingStack)
            {
                stacks.Add(new List<StackCube> { cube });
            }
        }

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
