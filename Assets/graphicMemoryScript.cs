using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class graphicMemoryScript : MonoBehaviour
{
    public KMBombInfo bomb;
    public KMAudio Audio;

    public Material[] colors;
    public Material black;
    public KMSelectable[] btns;
    public GameObject[] doors;

    private int[] labelSelected = new int[4];
    private List<List<KeyValuePair<string, int>>> labelShapes = new List<List<KeyValuePair<string, int>>>();
    private List<int> correctButtons = new List<int>();

    private int[] colorCount = { 0, 0, 0, 0, 0, 0 };
    private int[] shapeCount = { 0, 0 };
    private int[] positionCount = { 0, 0, 0, 0 };
    private int[][] colorShapeCount = { new int[] {0, 0, 0, 0, 0, 0},
                                        new int[] {0, 0, 0, 0, 0, 0} };
    private int dominantShapeCount = 0; //posivive -> more buttons with more squares

    private bool[][] statements = { new bool[] {false, false, false, false},
                                    new bool[] {false, false, false, false},
                                    new bool[] {false, false, false, false},
                                    new bool[] {false, false, false, false} };

    int btnPresses = 0;
    int btnPressesRequired;

    int lastPress;

    bool animating = true;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved = false;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        btns[0].OnInteract += delegate () { PressButton(1); return false; };
        btns[1].OnInteract += delegate () { PressButton(2); return false; };
        btns[2].OnInteract += delegate () { PressButton(3); return false; };
        btns[3].OnInteract += delegate () { PressButton(4); return false; };

        GetComponent<KMBombModule>().OnActivate += delegate { StartCoroutine("ButtonsAppear"); };

    }

    void Start()
    {
        SetUp();
        RandomizeButtons();
    }

    void Update()
    {

    }

    void Reset()
    {
        correctButtons.Clear();

        correctButtons.Add(1);
        correctButtons.Add(2);
        correctButtons.Add(3);
        correctButtons.Add(4);

        btnPresses = 0;

        colorCount = new int[] { 0, 0, 0, 0, 0, 0 };
        shapeCount = new int[] { 0, 0 };
        positionCount = new int[] { 0, 0, 0, 0 };
        colorShapeCount = new int[][] { new int[] {0, 0, 0, 0, 0, 0},
                                        new int[] {0, 0, 0, 0, 0, 0} };
        dominantShapeCount = 0;

        RandomizeButtons();
    }

    void SetUp()
    {
        correctButtons.Add(1);
        correctButtons.Add(2);
        correctButtons.Add(3);
        correctButtons.Add(4);

        labelShapes.Add(new List<KeyValuePair<string, int>>());
        labelShapes.Add(new List<KeyValuePair<string, int>>());
        labelShapes.Add(new List<KeyValuePair<string, int>>());
        labelShapes.Add(new List<KeyValuePair<string, int>>());

        btnPressesRequired = rnd.Range(4, 8);
    }

    void RandomizeButton(int btn)
    {
        for (int i = 0; i < 7; i++)
        {
            btns[btn - 1].transform.GetChild(i).gameObject.SetActive(false);
        }

        labelSelected[btn - 1] = rnd.Range(0, 7);
        Transform label = btns[btn - 1].transform.GetChild(labelSelected[btn - 1]);

        label.Rotate(0, ((rnd.Range(0, 4)) * 90), 0);
        label.gameObject.SetActive(true);
        labelShapes[btn - 1].Clear();

        for (int i = 0; i < label.childCount; i++)
        {
            int colorIdx = rnd.Range(0, 6);
            label.GetChild(i).GetComponentInChildren<Renderer>().material = colors[colorIdx];

            labelShapes[btn - 1].Add(new KeyValuePair<string, int>(label.GetChild(i).name, colorIdx));

            Debug.LogFormat("[Graphic Memory #{0}] {1}button has now a {2} {3}.", moduleId, BtnToString(new List<int>(new int[] { btn })), ColorToString(colorIdx), ShapeToString(label.GetChild(i).name));
        }
    }

    void RandomizeButtons()
    {
        int lblCnt = 1;
        foreach (KMSelectable btn in btns)
        {
            RandomizeButton(lblCnt);
            lblCnt++;
        }
    }

    void PressButton(int btn)
    {
        if (animating || moduleSolved)
            return;

        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        btns[btn - 1].AddInteractionPunch();

        if (!correctButtons.Exists(x => x == btn))
        {
            Debug.LogFormat("[Graphic Memory #{0}] Strike! Tried to press the button in the {1}when the correct buttons were [{2}].", moduleId, BtnToString(new List<int>(new int[] { btn })), BtnToString(correctButtons));

            GetComponent<KMBombModule>().HandleStrike();
            animating = true;
            StartCoroutine("ButtonsOnStrike");
        }
        else
        {
            btnPresses++;
            Debug.LogFormat("[Graphic Memory #{0}] Successfully pressed the button in the {1}. {2} out of {3} button presses made.", moduleId, BtnToString(new List<int>(new int[] { btn })), btnPresses, btnPressesRequired);

            if (btnPresses == btnPressesRequired)
            {
                StartCoroutine("ButtonsDisappear");
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
            }
            else
            {
                lastPress = btn;
                HandlePress(btn - 1);
                CalcStatements();
                CalCorrectButtons();
                animating = true;
                StartCoroutine("ButtonOnPress");
            }
        }
    }

    void HandlePress(int btn)
    {
        int squareTriangleDiff = 0;

        positionCount[btn]++;

        foreach (KeyValuePair<string, int> shape in labelShapes[btn])
        {
            colorCount[shape.Value]++;
            if (shape.Key[0] == 's')
            {
                shapeCount[0]++;
                colorShapeCount[0][shape.Value]++;
                squareTriangleDiff++;
            }
            else
            {
                shapeCount[1]++;
                colorShapeCount[1][shape.Value]++;
                squareTriangleDiff--;
            }
        }

        if (squareTriangleDiff > 0)
        {
            dominantShapeCount++;
        }
        else if (squareTriangleDiff < 0)
        {
            dominantShapeCount--;
        }

    }

    void CalcStatements()
    {
        statements[0][0] = FindMax(positionCount) == 1 ? true : false;
        statements[0][1] = FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 0, 5 }) ? true : false;
        statements[0][2] = dominantShapeCount > 0 ? true : false;
        statements[0][3] = AllEqual(colorCount) ? true : false;

        statements[1][0] = FindMax(positionCount) == 2 ? true : false;
        statements[1][1] = shapeCount[0] > shapeCount[1] ? true : false;
        statements[1][2] = (colorCount[1] + colorCount[2] + colorCount[3]) > (colorCount[0] + colorCount[4] + colorCount[5]) ? true : false;
        statements[1][3] = FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 0, 3 }) ? true : false;

        statements[2][0] = FindMax(positionCount) == 3 ? true : false;
        statements[2][1] = FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 1, 0 }) ? true : false;
        statements[2][2] = dominantShapeCount < 0 ? true : false;
        statements[2][3] = shapeCount[0] == shapeCount[1] ? true : false;

        statements[3][0] = FindMax(positionCount) == 0 ? true : false;
        statements[3][1] = shapeCount[0] < shapeCount[1] ? true : false;
        statements[3][2] = (colorCount[1] + colorCount[2] + colorCount[3]) < (colorCount[0] + colorCount[4] + colorCount[5]) ? true : false;
        statements[3][3] = FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 1, 1 }) ? true : false;

        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 1 })), statements[0][0], statements[0][1], statements[0][2], statements[0][3]);
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 2 })), statements[1][0], statements[1][1], statements[1][2], statements[1][3]);
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 3 })), statements[2][0], statements[2][1], statements[2][2], statements[2][3]);
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 4 })), statements[3][0], statements[3][1], statements[3][2], statements[3][3]);
    }

    void CalCorrectButtons()
    {
        int[] statementCount = { 0, 0, 0, 0 };

        correctButtons.Clear();

        for (int i = 0; i < statements.Length; i++)
        {
            for (int j = 0; j < statements[i].Length; j++)
            {
                if (statements[i][j])
                {
                    statementCount[i]++;
                }
            }
        }

        int max = -1;

        for (int i = 0; i < statementCount.Length; i++)
        {
            if (statementCount[i] > max)
            {
                max = statementCount[i];
                correctButtons.Clear();
                correctButtons.Add(i + 1);
            }
            else if (statementCount[i] == max)
            {
                correctButtons.Add(i + 1);
            }
        }
    }

    int FindMax(int[] a)
    {
        int max = a[0];
        int maxPos = 0;
        bool repeat = false;

        for (int i = 1; i < a.Length; i++)
        {
            if (a[i] > max)
            {
                max = a[i];
                maxPos = i;
                repeat = false;
            }
            else if (a[i] == max)
            {
                repeat = true;
            }
        }

        if (repeat)
            return -1;

        return maxPos;
    }

    int[] FindMaxMatrix(int[][] a)
    {
        int max = a[0][0];
        int maxPosRow = 0;
        int maxPosColumn = 0;
        bool repeat = false;

        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < a[i].Length; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                if (a[i][j] > max)
                {
                    max = a[i][j];
                    maxPosRow = i;
                    maxPosColumn = j;
                    repeat = false;
                }
                else if (a[i][j] == max)
                {
                    repeat = true;
                }
            }
        }

        if (repeat)
            return new int[] { -1, -1 };

        return new int[] { maxPosRow, maxPosColumn };
    }

    bool AllEqual(int[] a)
    {
        int val = a[0];

        for (int i = 1; i < a.Length; i++)
        {
            if (a[i] != val)
                return false;
        }

        return true;
    }

    string BtnToString(List<int> btns)
    {
        string ret = "";

        foreach (int btn in btns)
        {
            switch (btn)
            {
                case 1:
                    {
                        ret += "Bottom-Left ";
                        break;
                    }
                case 2:
                    {
                        ret += "Bottom-Right ";
                        break;
                    }
                case 3:
                    {
                        ret += "Top-Left ";
                        break;
                    }
                case 4:
                    {
                        ret += "Top-Right ";
                        break;
                    }
            }
        }

        return ret;
    }

    string ShapeToString(string shape)
    {
        if (shape[0] == 't')
            return "triangle";

        return "square";
    }

    string ColorToString(int color)
    {
        switch (color)
        {
            case 0:
                {
                    return "blue";
                }
            case 1:
                {
                    return "green";
                }
            case 2:
                {
                    return "orange";
                }
            case 3:
                {
                    return "purple";
                }
            case 4:
                {
                    return "red";
                }
            case 5:
                {
                    return "yellow";
                }
        }

        return null;
    }

    IEnumerator ButtonsAppear()
    {
        KMAudio.KMAudioRef sound = GetComponent<KMAudio>().PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        for (int i = 0; i < 10; i++)
        {
            if (i > 4)
                foreach (KMSelectable btn in btns)
                {
                    btn.transform.Translate(new Vector3(0f, 0f, 0.0032f));
                }

            if (i < 5)
            {
                foreach (GameObject door in doors)
                {
                    door.transform.GetChild(0).transform.localScale -= new Vector3(0.0006f, 0, 0);
                    door.transform.GetChild(0).transform.Translate(new Vector3(-0.0025f, 0, 0));
                    door.transform.GetChild(1).transform.localScale -= new Vector3(0.0006f, 0, 0);
                    door.transform.GetChild(1).transform.Translate(new Vector3(0.0025f, 0, 0));
                }
            }

            yield return new WaitForSeconds(0.05f);
        }

        sound.StopSound();

        foreach (KMSelectable btn in btns)
        {
            btn.transform.GetChild(7).gameObject.SetActive(true);
        }

        animating = false;
    }

    IEnumerator ButtonsDisappear()
    {
        KMAudio.KMAudioRef sound = GetComponent<KMAudio>().PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        foreach (KMSelectable btn in btns)
        {
            btn.transform.GetChild(7).gameObject.SetActive(false);
        }

        for (int i = 0; i < 10; i++)
        {
            if (i < 5)
                foreach (KMSelectable btn in btns)
                {
                    btn.transform.Translate(new Vector3(0f, 0f, -0.0032f));
                }

            if (i > 4)
            {
                foreach (GameObject door in doors)
                {
                    door.transform.GetChild(1).transform.Translate(new Vector3(-0.0025f, 0, 0));
                    door.transform.GetChild(1).transform.localScale += new Vector3(0.0006f, 0, 0);
                    door.transform.GetChild(0).transform.Translate(new Vector3(0.0025f, 0, 0));
                    door.transform.GetChild(0).transform.localScale += new Vector3(0.0006f, 0, 0);
                }
            }

            yield return new WaitForSeconds(0.05f);
        }

        sound.StopSound();
    }

    IEnumerator ButtonsOnStrike()
    {
        yield return StartCoroutine("ButtonsDisappear");

        Reset();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine("ButtonsAppear");
    }

    IEnumerator ButtonOnPress()
    {
        yield return StartCoroutine("ButtonDisappear");

        RandomizeButton(lastPress);

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine("ButtonAppear");

    }

    IEnumerator ButtonAppear()
    {
        KMAudio.KMAudioRef sound = GetComponent<KMAudio>().PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        for (int i = 0; i < 10; i++)
        {
            if (i > 4)
                btns[lastPress - 1].transform.Translate(new Vector3(0f, 0f, 0.0032f));

            if (i < 5)
            {
                doors[lastPress - 1].transform.GetChild(0).transform.localScale -= new Vector3(0.0006f, 0, 0);
                doors[lastPress - 1].transform.GetChild(0).transform.Translate(new Vector3(-0.0025f, 0, 0));
                doors[lastPress - 1].transform.GetChild(1).transform.localScale -= new Vector3(0.0006f, 0, 0);
                doors[lastPress - 1].transform.GetChild(1).transform.Translate(new Vector3(0.0025f, 0, 0));
            }

            yield return new WaitForSeconds(0.05f);
        }

        sound.StopSound();

        btns[lastPress - 1].transform.GetChild(7).gameObject.SetActive(true);

        animating = false;
    }

    IEnumerator ButtonDisappear()
    {
        KMAudio.KMAudioRef sound = GetComponent<KMAudio>().PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        btns[lastPress - 1].transform.GetChild(7).gameObject.SetActive(false);

        for (int i = 0; i < 10; i++)
        {
            if (i < 5)
                btns[lastPress - 1].transform.Translate(new Vector3(0f, 0f, -0.0032f));

            if (i > 4)
            {
                doors[lastPress - 1].transform.GetChild(1).transform.Translate(new Vector3(-0.0025f, 0, 0));
                doors[lastPress - 1].transform.GetChild(1).transform.localScale += new Vector3(0.0006f, 0, 0);
                doors[lastPress - 1].transform.GetChild(0).transform.Translate(new Vector3(0.0025f, 0, 0));
                doors[lastPress - 1].transform.GetChild(0).transform.localScale += new Vector3(0.0006f, 0, 0);
            }

            yield return new WaitForSeconds(0.05f);
        }

        sound.StopSound();
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <button> [Presses the specified button] | Valid buttons are TL(topleft), TR(topright), BL(bottomleft), and BR(bottomright)";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*TL\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            btns[2].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*TR\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            btns[3].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*BL\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            btns[0].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*BR\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            btns[1].OnInteract();
            yield break;
        }
    }
}