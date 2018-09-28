using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

// this doesn't need to be a struct anymore, can just be a variable for this app
public struct topinfo
{
    public int NATOM;
}

// coordinates
public struct atominfo
{
    public int resnum;
    public string resid, atomsym, element;
    public float x, y, z;
    public float charge;
}

public struct Bonds
{
    public int a1, a2;
    public double distance;

    public void Constructor(int atom1, int atom2)
    {
        a1 = atom1;
        a2 = atom2;
    }

    public double CalcDistance(Vector3 a, Vector3 b)
    {
        Vector3 vector = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        return Mathf.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
    }
}

public class Load : MonoBehaviour
{
    public GameObject MoleculeCollection, FilePrefab, SpherePrefabs;
    private GameObject ThisFile, NewLineCollection, Play, LowResSpherePrefabs;
    public atominfo[,] molecule; // frame, atom
    public topinfo top;

    public List<Bonds>[] Bonds;

    public int framenum;
    public bool dynamicBonds;
    public int FileNumber = 0;
    public int AtomNumber;

    private void Start()
    {
        FilePrefab.SetActive(false);

        GetComponent<OpenFileButton>().GetDataFromWeb();
    }

    public async Task LoadMoleculeAsync()
    {
        // Check if any file was loaded in
        if (GetComponent<OpenFileButton>().filestring == null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.position = new Vector3(0, 0, 1);

            goto NoFile; // Skips to the end of this function
        }

        // delete anything else in molecule collection (just one file at a time)









        // Create a new FileObject

        ThisFile = (GameObject)Instantiate(FilePrefab);
        ThisFile.transform.localScale = Vector3.one;
        ThisFile.SetActive(true);
        ThisFile.transform.parent = MoleculeCollection.transform;
        ThisFile.transform.name = String.Format("File {0}", FileNumber);

        // Get the AtomCollection object
        NewLineCollection = ThisFile.transform.GetChild(1).gameObject;

        //await loadDel[Manager.GetComponent<Dropdowns>().filetypes.IndexOf(Manager.GetComponent<Dropdowns>().filetypeSelected) - 1](ThisFile); 

        await LoadXYZ(ThisFile);

        // maybe do this w moleculecolleciton instead
        ThisFile.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        // Can we put this data directly in the right place instead of moving it around and then deleting it?
        GetComponent<OpenFileButton>().filestring = null;
        molecule = null;
        AtomNumber = 0;
        FileNumber++;

        NoFile:
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }

    async Task LoadXYZ(GameObject ThisFile)
    {
        // Parse file to load in molecular data
        GetComponent<LoadXYZ>().LoadMolecule();

        // Create an array (per frame) of a list of bonds
        ThisFile.GetComponent<ThisFileInfo>().Bonds = new List<Bonds>[framenum];

        // Get the bonds based on atom distances
        // Have to create lists for each element of array
        for (int g = 0; g < framenum; g++)
        {
            ThisFile.GetComponent<ThisFileInfo>().Bonds[g] = new List<Bonds>();
            GetBondsDynamic(g);
        }

        // Set this flag to true so our PlayScript/ViewLine components update bonds per frame
        ThisFile.GetComponent<ThisFileInfo>().dynamicBonds = true;

        // Determine the size and center of the molecule and ensure it is in the center of a box
        CenterMolecule();

        // Save information about this molecule in its FileObject
        ThisFile.GetComponent<ThisFileInfo>().FileNumber = FileNumber;
        ThisFile.GetComponent<ThisFileInfo>().molecule = molecule;
        ThisFile.GetComponent<ThisFileInfo>().top = top;
        ThisFile.GetComponent<ThisFileInfo>().TotalFrames = framenum;
        ThisFile.GetComponent<ThisFileInfo>().FileName = $"XYZ {FileNumber}";

        //  Set representation mode to line
        ThisFile.GetComponent<ViewLine>().Line();

        // Create the lines to represent bonds
        ThisFile.GetComponent<ViewLine>().CreateLines(molecule);

        // Tell the PlayScript how many frames were loaded in
        ThisFile.transform.GetChild(2).GetComponent<PlayScript>().framenum = framenum;

        // If there are less than 800 atoms (spheres to render), set the representation mode to Ball & Stick
        if (top.NATOM < 200)
        {
            ThisFile.GetComponent<BallAndStick>().SpherePrefabs = SpherePrefabs;
            ThisFile.GetComponent<BallAndStick>().BnS();
            ThisFile.GetComponent<BallAndStick>().SwitchToBnS();
        }
        else if (top.NATOM >= 200 && top.NATOM < 1600)
        {
            ThisFile.GetComponent<BallAndStick>().SpherePrefabs = LowResSpherePrefabs;
        }
        else
        {
            // create billboard circles ; 
            // maybe just give a warning in console about performance up to like 1500 atoms and then give an error "too many atoms to be drawn" if >1500
        }

        // Set the Play object
        Play = ThisFile.transform.GetChild(2).gameObject;

        // If multiple frames were loaded in, set Play as active so we can see the play button and associated sliders
        if (framenum > 1)
        {
            Play.SetActive(true);

            // Set it to play automatically
            ThisFile.transform.GetChild(2).gameObject.GetComponent<PlayScript>().PlayTraj();
        }
    }
    
    float leftmost = 999999, rightmost = -999999, bottommost = 999999, topmost = -999999, backmost = 999999, frontmost = -999999;
    float toptobottom, lefttoright;

    void CenterMolecule()
    {
        //double starttime = Time.realtimeSinceStartup;
        Vector3 midpoint = Vector3.zero;

        // Determine where the center of the molecule is for the first frame
        for (int atom = 0; atom < top.NATOM; atom++)
        {
            midpoint.x += molecule[0, atom].x;
            midpoint.y += molecule[0, atom].y;
            midpoint.z += molecule[0, atom].z;
        }
        midpoint = midpoint / top.NATOM;

        // Determine for all frames and all atoms where the outermost atoms are to ensure we can fit everything inside a box
        for (int atom = 0; atom < top.NATOM; atom++)
        {
            for (int frame = 0; frame < framenum; frame++)
            {
                molecule[frame, atom].x -= midpoint.x;
                molecule[frame, atom].y -= midpoint.y;
                molecule[frame, atom].z -= midpoint.z;
            }

            if (molecule[0, atom].x < leftmost)
                leftmost = molecule[0, atom].x;
            if (molecule[0, atom].x > rightmost)
                rightmost = molecule[0, atom].x;
            if (molecule[0, atom].y > topmost)
                topmost = molecule[0, atom].y;
            if (molecule[0, atom].y < bottommost)
                bottommost = molecule[0, atom].y;
            if (molecule[0, atom].z > frontmost)
                frontmost = molecule[0, atom].z;
            if (molecule[0, atom].z < backmost)
                backmost = molecule[0, atom].z;
        }

        // Find the 3D space the simulation occupies and add 0.2f padding
        toptobottom = Math.Abs(topmost) + Math.Abs(bottommost) + 0.2f;
        lefttoright = Math.Abs(rightmost) + Math.Abs(leftmost) + 0.2f;
        fronttoback = Math.Abs(backmost) + Math.Abs(frontmost) + 0.2f;

        // Rescale factors
        rescaleY = 0.5f / toptobottom;
        rescaleX = 0.5f / lefttoright;
        rescaleZ = 0.5f / fronttoback;

        max = Math.Max(rescaleY, Math.Max(rescaleX, rescaleZ));

        for (int atom = 0; atom < top.NATOM; atom++)
        {
            for (int frame = 0; frame < framenum; frame++)
            {
                molecule[frame, atom].x *= max * 2;
                molecule[frame, atom].y *= max * 2;
                molecule[frame, atom].z *= max * 2;
            }
        }

        // Reset these values for the next molecule loaded in
        leftmost = 999999; rightmost = -999999; bottommost = 999999; topmost = -999999; backmost = 999999; frontmost = -999999;

    }
    public float max;
    public GameObject AtomCollection;
    float rescaleY, rescaleX, rescaleZ, fronttoback;

    void GetBondsDynamic(int frame)
    {
        //ThisFile.GetComponent<ThisFileInfo>().Bonds[frame].Clear();

        for (int i = 0; i < top.NATOM; i++)
        {
            for (int j = 0; j < i; j++)
            {
                float distance = Vector3.Distance(new Vector3(molecule[frame, i].x, molecule[frame, i].y, molecule[frame, i].z),
                                                  new Vector3(molecule[frame, j].x, molecule[frame, j].y, molecule[frame, j].z)) * 100; // unit conversion

                bool bondExists = CheckBondDyn(molecule[frame, i].element,
                                               molecule[frame, j].element,
                                               distance);
                if (bondExists)
                {
                    Bonds temp = new Bonds();
                    temp.Constructor(i, j);
                    temp.distance = distance;
                    ThisFile.GetComponent<ThisFileInfo>().Bonds[frame].Add(temp);
                }
            }
        }
    }


    bool CheckBondDyn(string element1, string element2, double distance)
    {
        bool contained = false;
        string[] Bondz = new string[] {
        element1 + element2 + "s",
        element1 + element2 + "d",
        element1 + element2 + "t",
        element2 + element1 + "s",
        element2 + element1 + "d",
        element2 + element1 + "t",
        element1 + element2 + "b",  //benzene carbons
        element1 + element2,
        element2 + element1
    };

        for (int i = 0; i < Bondz.Length; i++)
        {
            if (BondLengths.ContainsKey(Bondz[i]))
            {
                if (BondLengths[Bondz[i]] + 15 > distance && BondLengths[Bondz[i]] - 15 < distance)
                {
                    contained = true;
                }
            }
        }

        if (contained)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    // in picometers
    // https://chem.libretexts.org/Textbook_Maps/Organic_Chemistry_Textbook_Maps/Map%3A_Organic_Chemistry_(Smith)/Chapter_01%3A_Structure_and_Bonding/1.6%3A_Determining_Molecular_Shape
    // https://pubs.acs.org/doi/abs/10.1021/ja00740a045
    // http://www.wiredchemist.com/chemistry/data/bond_energies_lengths.html

    Dictionary<string, float> BondLengths = new Dictionary<string, float>()
    {
        {"CCs", 154},
        {"CCb", 140},
        {"CCd", 134},
        {"CCt", 120},
        {"CSi", 185},
        {"CNs", 147},
        {"CNd", 129},
        {"CNt", 116},
        {"COs", 143},
        {"COd", 120},
        {"COt", 113},
        {"CF", 135},
        {"CCl", 177},
        {"CBr", 194},
        {"CI", 214},
        {"NNs", 145},
        {"NNd", 125},
        {"NNt", 110},
        {"NOs", 140},
        {"NOd", 121},
        {"PP", 221},
        {"POs", 163},
        {"POd", 150},
        {"HH", 74},
        {"HB", 119},
        {"HC", 109},
        {"HO", 96},
        {"OOs", 148},
        {"OOd", 121},
        {"NH", 99},
        {"CS", 187},
        {"HS", 134 }
    };
}