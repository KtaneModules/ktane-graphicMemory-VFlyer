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
    private bool moduleSolved = false, requestForceSolve = false;

    void Awake()
    {
        moduleId = moduleIdCounter++;

        btns[0].OnInteract += delegate () { PressButton(1); return false; };
        btns[1].OnInteract += delegate () { PressButton(2); return false; };
        btns[2].OnInteract += delegate () { PressButton(3); return false; };
        btns[3].OnInteract += delegate () { PressButton(4); return false; };

        GetComponent<KMBombModule>().OnActivate += delegate { StartCoroutine(ButtonsAppear()); };

    }

    void Start()
    {
        SetUp();
        RandomizeAllButtons();
        Debug.LogFormat("[Graphic Memory #{0}] Press any button to start disarming the module.", moduleId);
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


        RandomizeAllButtons();
        Debug.LogFormat("[Graphic Memory #{0}] Press any button to start disarming the module.", moduleId);
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

        btnPressesRequired = 4;

        foreach(KMSelectable btn in btns)
            for(int i = 0; i < btn.transform.childCount; i++)
                for(int j = 0; j < btn.transform.GetChild(i).childCount; j++)
                    if(btn.transform.GetChild(i).transform.GetChild(j).name.Contains('r'))
                        btn.transform.GetChild(i).transform.GetChild(j).localScale = new Vector3(btn.transform.GetChild(i).transform.GetChild(j).localScale.x / 0.1f, 1f, btn.transform.GetChild(i).transform.GetChild(j).localScale.z / 0.1f);
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

    void RandomizeAllButtons()
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

        if (!correctButtons.Exists(x => x == btn) && btnPresses > 0)
        {
            Debug.LogFormat("[Graphic Memory #{0}] Strike! The {1}button was incorrectly pressed when the valid buttons were [ {2}].", moduleId, BtnToString(new List<int>(new int[] { btn })), BtnToString(correctButtons));

            GetComponent<KMBombModule>().HandleStrike();
            animating = true;
            StartCoroutine(ButtonsOnStrike());
        }
        else
        {
            btnPresses++;
            if (btnPresses > 1)
                Debug.LogFormat("[Graphic Memory #{0}] Correctly pressed the {1}button. {2} out of {3} button presses made.", moduleId, BtnToString(new List<int>(new int[] { btn })), btnPresses, btnPressesRequired);
            else
                Debug.LogFormat("[Graphic Memory #{0}] Pressed the {1}button. {2} out of {3} button presses made.", moduleId, BtnToString(new List<int>(new int[] { btn })), btnPresses, btnPressesRequired);
            if (btnPresses >= btnPressesRequired)
            {
                StartCoroutine(ButtonsDisappear());
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
                StartCoroutine(ButtonOnPress());
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
        statements = new bool[][]
        {
            new bool[] {
                FindMax(positionCount) == 1 ? true : false,
                FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 0, 5 }) ? true : false,
                dominantShapeCount > 0 ? true : false,
                AllEqual(colorCount) ? true : false
            },
            new bool[] {
                FindMax(positionCount) == 2 ? true : false,
                shapeCount[0] > shapeCount[1] ? true : false,
                (colorCount[1] + colorCount[2] + colorCount[3]) > (colorCount[0] + colorCount[4] + colorCount[5]) ? true : false,
                FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 0, 3 }) ? true : false
            },
            new bool[] {
                FindMax(positionCount) == 3 ? true : false,
                FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 1, 0 }) ? true : false,
                statements[2][2] = dominantShapeCount < 0 ? true : false,
                statements[2][3] = shapeCount[0] == shapeCount[1] ? true : false
            },
            new bool[] {
                FindMax(positionCount) == 0 ? true : false,
                shapeCount[0] < shapeCount[1] ? true : false,
                (colorCount[1] + colorCount[2] + colorCount[3]) < (colorCount[0] + colorCount[4] + colorCount[5]) ? true : false,
                FindMaxMatrix(colorShapeCount).SequenceEqual(new int[] { 1, 1 }) ? true : false
            },
        };
        /*
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
        */
        for (int x = 0; x < 4; x++)
        {
            int[] trueStatements = new[] { 0, 1, 2, 3 }.Where(a => statements[x][a]).ToArray();

            Debug.LogFormat("[Graphic Memory #{0}] True Statements for the {1}button: [ {2} ]", moduleId,
                BtnToString(new List<int>(new int[] { x + 1 })),
                trueStatements.Any() ? trueStatements.Select(a => a + 1).Join(", ") : "none");
        }
        /*
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 1 })), statements[0][0], statements[0][1], statements[0][2], statements[0][3]);
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 2 })), statements[1][0], statements[1][1], statements[1][2], statements[1][3]);
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 3 })), statements[2][0], statements[2][1], statements[2][2], statements[2][3]);
        Debug.LogFormat("[Graphic Memory #{0}] Statements for the button in the {1}are {2}, {3}, {4} and {5}", moduleId, BtnToString(new List<int>(new int[] { 4 })), statements[3][0], statements[3][1], statements[3][2], statements[3][3]);
        */
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
        Debug.LogFormat("[Graphic Memory #{0}] Valid buttons after {2} press(es): [ {1}].", moduleId, BtnToString(correctButtons), btnPresses);
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
                    btn.transform.localPosition = btn.transform.localPosition + new Vector3(0f, 0.0032f, 0f);
                }

            if (i < 5)
            {
                foreach (GameObject door in doors)
                {
                    door.transform.GetChild(0).transform.localScale -= new Vector3(0.0006f, 0, 0);
                    door.transform.GetChild(0).transform.localPosition = door.transform.GetChild(0).transform.localPosition + new Vector3(-0.0025f, 0, 0);
                    door.transform.GetChild(1).transform.localScale -= new Vector3(0.0006f, 0, 0);
                    door.transform.GetChild(1).transform.localPosition = door.transform.GetChild(1).transform.localPosition + new Vector3(0.0025f, 0, 0);
                }
            }

            yield return new WaitForSeconds(requestForceSolve ? 0f : 0.05f);
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
                    btn.transform.localPosition = btn.transform.localPosition + new Vector3(0f, -0.0032f, 0f);
                }

            if (i > 4)
            {
                foreach (GameObject door in doors)
                {
                    door.transform.GetChild(1).transform.localPosition = door.transform.GetChild(1).transform.localPosition + new Vector3(-0.0025f, 0, 0);
                    door.transform.GetChild(1).transform.localScale += new Vector3(0.0006f, 0, 0);
                    door.transform.GetChild(0).transform.localPosition = door.transform.GetChild(0).transform.localPosition + new Vector3(0.0025f, 0, 0);
                    door.transform.GetChild(0).transform.localScale += new Vector3(0.0006f, 0, 0);
                }
            }

            yield return new WaitForSeconds(requestForceSolve ? 0f : 0.05f);
        }

        sound.StopSound();
    }

    IEnumerator ButtonsOnStrike()
    {
        yield return ButtonsDisappear();

        Reset();

        yield return new WaitForSeconds(requestForceSolve ? 0f : 0.5f);

        yield return ButtonsAppear();
    }

    IEnumerator ButtonOnPress()
    {
        yield return ButtonDisappear();

        RandomizeButton(lastPress);

        yield return new WaitForSeconds(requestForceSolve ? 0f : 0.5f);

        yield return ButtonAppear();

    }

    IEnumerator ButtonAppear()
    {
        KMAudio.KMAudioRef sound = GetComponent<KMAudio>().PlayGameSoundAtTransformWithRef(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);

        for (int i = 0; i < 10; i++)
        {
            if (i > 4)
                btns[lastPress - 1].transform.localPosition = btns[lastPress - 1].transform.localPosition + new Vector3(0f, 0.0032f, 0f);

            if (i < 5)
            {
                doors[lastPress - 1].transform.GetChild(0).transform.localScale -= new Vector3(0.0006f, 0, 0);
                doors[lastPress - 1].transform.GetChild(0).transform.localPosition = doors[lastPress - 1].transform.GetChild(0).transform.localPosition + new Vector3(-0.0025f, 0, 0);
                doors[lastPress - 1].transform.GetChild(1).transform.localScale -= new Vector3(0.0006f, 0, 0);
                doors[lastPress - 1].transform.GetChild(1).transform.localPosition = doors[lastPress - 1].transform.GetChild(1).transform.localPosition + new Vector3(0.0025f, 0, 0);
            }

            yield return new WaitForSeconds(requestForceSolve ? 0f : 0.05f);
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
                btns[lastPress - 1].transform.localPosition = btns[lastPress - 1].transform.localPosition + new Vector3(0f, -0.0032f, 0f);

            if (i > 4)
            {
                doors[lastPress - 1].transform.GetChild(1).transform.localPosition = doors[lastPress - 1].transform.GetChild(1).transform.localPosition + new Vector3(-0.0025f, 0, 0);
                doors[lastPress - 1].transform.GetChild(1).transform.localScale += new Vector3(0.0006f, 0, 0);
                doors[lastPress - 1].transform.GetChild(0).transform.localPosition = doors[lastPress - 1].transform.GetChild(0).transform.localPosition + new Vector3(0.0025f, 0, 0);
                doors[lastPress - 1].transform.GetChild(0).transform.localScale += new Vector3(0.0006f, 0, 0);
            }

            yield return new WaitForSeconds(requestForceSolve ? 0f : 0.05f);
        }

        sound.StopSound();
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        requestForceSolve = true;
        Debug.LogFormat("[Graphic Memory #{0}] Force solve requested viva TP Handler.", moduleId);
        while (btnPresses < btnPressesRequired)
        {
            while (animating)
                yield return true;
            if (correctButtons.Any())
            {
                btns[correctButtons[rnd.Range(0, correctButtons.Count)] - 1].OnInteract();
                yield return null;
            }
            else
                yield break;

        }

        yield return null;
    }

    //Twitch Plays Handling
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = "Press the specified button with \"!{0} TL/top-right/bl/Bottom right\" Valid buttons are TL(topleft), TR(topright), BL(bottomleft), and BR(bottomright). \"press\" is optional. Reset the module with \"!{0} reset\". At least 1 button must be pressed in order to reset the module!";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Application.isEditor)
            command = command.Trim();

        if (Regex.IsMatch(command, @"^(press\s)?(TL|top(-|\s)left|topleft)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            do
                yield return "trycancel";
            while (animating);
            btns[2].OnInteract();
        }
        else if (Regex.IsMatch(command, @"^(press\s)?(TR|top(-|\s)right|topright)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            do
                yield return "trycancel";
            while (animating);
            btns[3].OnInteract();
        }
        else if (Regex.IsMatch(command, @"^(press\s)?(BL|bottom(-|\s)left|bottomleft)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            do
                yield return "trycancel";
            while (animating);
            btns[0].OnInteract();
        }
        else if (Regex.IsMatch(command, @"^(press\s)?(BR|bottom(-|\s)right|bottomright)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            do
                yield return "trycancel";
            while (animating);
            btns[1].OnInteract();
        }
        else if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (btnPresses <= 0)
            {
                yield return "sendtochaterror You must press at least 1 button in order to reset the module.";
                yield break;
            }
            yield return null;
            do
                yield return "trycancel";
            while (animating);
            Debug.LogFormat("[Graphic Memory #{0}] Requesting reset viva TP Handler.", moduleId);
            StartCoroutine(ButtonsOnStrike());
        }
    }
}