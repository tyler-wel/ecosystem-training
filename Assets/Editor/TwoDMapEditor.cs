using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[CustomEditor (typeof (ThreeDTileMapGen))]
public class TwoDMapEditor : Editor {
    
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        ThreeDTileMapGen map = target as ThreeDTileMapGen;

        map.GenerateMap();
    }
}
