using System.Collections.Generic;
using UnityEngine;


public class ThisFileInfo : MonoBehaviour
{
    public atominfo[,] molecule;
    public topinfo top;

    public List<Bonds>[] Bonds;

    public int FileNumber, TotalFrames;
    public string FileName;
    public bool dynamicBonds;
}